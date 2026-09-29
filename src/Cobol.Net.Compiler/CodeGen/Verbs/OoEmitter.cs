// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

using CobolNet.Compiler.Oo;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The OO EMIT half (P7 Step 9m, BATCH-3b; direct-wired at 9n — the BIND half stays on the
/// CSharpEmitter bind-host facade behind the P6→P9 <c>IOoBindHost</c> seam): class/factory/interface unit
/// emission, INVOKE (typed · instance · D10 universal), SET object-ref, and the per-method LOCAL dispatcher.
/// RUN-UNIT scope — reads the per-unit collaborators through the LIVE <see cref="ProgramEmitter.Current"/>
/// root: class-unit emission RE-CREATES the whole per-unit set mid-run (<see cref="ProgramEmitter.BeginUnit"/>),
/// so captured copies would go stale (the coupling-census hazard). The class table and interface-data forests
/// arrive from the immutable <c>BoundCompilation</c> (never the bind host's session state).</summary>
internal sealed class OoEmitter(DispatchState dispatch, EcState ecState, CallUnitState callState,
    ProgramEmitter program,
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> ifaceData,
    IReadOnlyList<AdapterPair> adapters)
{
    /// <summary>A <see cref="Place"/> over a METHOD BOUNDARY ROOT — the ONE thing that lets the OO boundary join
    /// the group-image channel every other operand path already uses (kb/Work PB177 arm A).
    /// <para>A method's LINKAGE / LOCAL-STORAGE roots are C# LOCALS, not fields (§8.6.4 — re-initialized each
    /// activation), so there was no <see cref="Place"/> to hand <see cref="PlaceRenderer"/> and the three
    /// boundary sites spelled <c>.AsImage()</c> / <c>.FromImage(</c> themselves, with no capability guard and no
    /// window arm. A root local is structurally an <see cref="AccessPath"/> of exactly one
    /// <see cref="RootFieldSegment"/> — the same construction <c>AccessPath.Reroot</c> performs for a
    /// contained-program root — so building it here costs nothing and buys the ONE reader's whole arm list,
    /// including any arm added to it later.</para>
    /// <para>⛔ NEVER used for a Tier-B REDEFINES canonical: that root's storage IS its string backing local
    /// (<c>MethodRedefinesBackingDecl</c>), whose width is the CLASS width — a wider level-01 redefiner needs
    /// the full backing (§13.18.44.3 SR8) — not the canonical VIEW's width. Every caller tests
    /// <c>MethodRedefinesBackingDecl</c> first and takes that arm, so the place built here is always a plain
    /// struct root: never a <c>RedefViewPlace</c>, never an <c>OdoGroupPlace</c> (a method boundary carries the
    /// FULL allocation, §14.2.3 GR8, exactly as <c>CallEmitter.CallStringRead</c> derives for CALL).</para></summary>
    private static Place MethodRootPlace(DataItem root) =>
        new MemberPlace(new AccessPath([new RootFieldSegment(root.CsName)]), root);

    /// <summary>⛔ THE ONE VALUE A METHOD LINKAGE ROOT HANDS BACK ACROSS THE ACTIVATION BOUNDARY — the BY REFERENCE
    /// copy-out (§14.2.3 GR8) and the RETURNING delivery (§14.9.23.4 GR8) both read it, so the storage arms
    /// cannot drift between them: a Tier-B REDEFINES canonical's string backing local; an ADDRESS-OF-taken
    /// root's addressable cell (kb/Work PB1019 — <see cref="MethodCellFormalLoad"/>); a variable-length or fixed
    /// group's image; else the root's own local.</summary>
    private string MethodBoundaryValue(DataEmitter fields, DataItem root, string what) =>
        fields.MethodRedefinesBackingDecl(root) is { } bk ? bk.Name
        : root.Class is { Tier: RedefinesTier.StringCanonical, IsCellBacked: true } ? MethodCellFormalLoad(root)
        : OoVarGroupCarried(root) ? PlaceRenderer.VarGroupImage(MethodRootPlace(root), what + " of")
        // A bit / national group hands back its ELEMENTARY value, an alphanumeric group its image — the CALL
        // boundary's ONE read (kb/Work PB1166), so the method ABI and the program ABI cannot speak two alphabets.
        : root.IsAsIfElementary ? CallEmitter.CallStringRead(MethodRootPlace(root))
        : root.IsGroup ? PlaceRenderer.GroupImage(MethodRootPlace(root), what)
        : root.CsName;

    /// <summary>An ADDRESS-OF-taken method LINKAGE root (kb/Work PB1019, method arm) lives in its per-activation
    /// <c>StorageCell</c> (<see cref="ActivationPointerSeeds"/>), so its crossing value is read from that storage: a
    /// root that crosses as characters (<see cref="OoCrossingType"/> <c>string</c> — a group, or an elementary item
    /// stored as its image) is the cell's whole backing image; a typed crossing reads the item through its
    /// place (the window over the cell).</summary>
    private string MethodCellFormalLoad(DataItem root) =>
        OoCrossingType(root) == "string" ? root.Class!.BackingCsName : PlaceRenderer.Read(MethodCellPlace(root));

    /// <summary>The inverse of <see cref="MethodCellFormalLoad"/>: store the argument into the root's cell.</summary>
    private string MethodCellFormalStore(DataItem root, string value) =>
        OoCrossingType(root) == "string"
            ? $"{root.Class!.BackingCsName} = {RuntimeApi.StrStore(value, $"{root.Class!.Width}")};"
            : PlaceRenderer.Write(MethodCellPlace(root), value);

    private Place MethodCellPlace(DataItem root) =>
        Refs.ResolveItem(root) ?? throw new InvalidOperationException(
            $"OO method: the ADDRESS-OF-taken LINKAGE root '{root.CobolName}' resolved to no place");

    private UnitEmitters U => program.Current;
    private EmitContext Ctx => U.Ctx;
    private NumericRenderer Num => U.Num;
    private ConditionRenderer Cond => U.Cond;
    private ReferenceResolver Refs => U.Refs;

    /// <summary>Emit the EXTERNAL record-area backings for a data forest (ISO §13.18.22.4 GR4b / §8.6.7): each
    /// <c>FD … IS EXTERNAL</c> record 01 re-bases onto a run-unit <c>ExternalStore</c> cell keyed by the FD name, so
    /// every describer (a program AND an object/factory) sees ONE shared record area. Shared by the program emit path
    /// (<c>ProgramEmitter.EmitProgramClass</c>) and the OO type-half (M2-OO-1i inc 5) — a class EXTERNAL FD needs the same
    /// backing property, and <c>CallBindExternalAndGlobal</c> already populates <c>CallExternalBackings</c> on the
    /// class binder (it runs in <c>BindResolve</c>).</summary>
    public void EmitExternalBackings(DataBinder data, CodeWriter w)
    {
        foreach (var ext in data.CallExternalBackings)
        {
            // §13.18.63 GR4a: a PLAIN external item's VALUE takes effect only during INITIALIZE, so its cell seeds
            // with the category-DEFAULT initial image. §11.9.10.4 GR7: a CONSTANT RECORD is the ONE external item
            // initialized at initial state — its cell seeds with the VALUE-composed image. ⛔ ONE SEEDER, ONE FLAG
            // (GroupImageCodec.ImageInitOf — what RecordStructEmitter/ProgramEmitter use for every other string
            // backing): the plain arm used to take a SECOND, bind-time seeder whose char-fill model predated the
            // pinned byte forms, so a COMP-5/float/INDEX leaf's cell seeded with ASCII '0' characters where its
            // own codec writes the zero ENCODING. ISO mandates no initial value for a plain EXTERNAL at all
            // (§14.6.2.3.3 leaves it undefined), so this is a consistency fix, not a corrected answer — but two
            // seeders composing different bytes for one cell is exactly how the next one becomes wrong.
            var de = new DataEmitter(Ctx);
            string init = de.ExternalCellSeed(ext);
            // A dynamic-length item's VALUE seeds the cell's dynamic half ONCE, when the run unit creates the cell
            // (kb/Work PB1026) — only a CONSTANT RECORD has one here (§13.18.63.4 GR4 a) for the plain external item).
            string dynSeeds = de.CellDynSeeds(ext.Record, useValues: ext.Record.IsConstantRecord);
            if (dynSeeds.Length > 0) init = $"static () => new StorageCell {{ Ref = {init} }}{dynSeeds}";
            // ⛔ THE CELL FIRST, THE BACKING OVER IT (kb/Work PB231): the byte image and the area's MANAGED SLOTS
            // are two halves of ONE StorageCell, so naming the cell once and defining the backing as `ref
            // {cell}.Ref` makes that an identity rather than two expressions that happen to agree.
            w.Line($"private StorageCell {ext.CellCsName} => ExternalStore.Cell({CsLiteral(ext.ExternalName)}, "
                + $"{init});   // EXTERNAL — ONE storage copy per run unit (ISO §8.6.7); survives CANCEL (§14.9.5.4 GR8)");
            w.Line($"private ref string {ext.BackingCsName} => ref {ext.CellCsName}.Ref;");
        }
    }

    /// <summary>Emit the data-pointer members of a data forest (ISO §13.18.5 BASED / §8.4.3.11 ADDRESS OF — the
    /// Phase-4b increment-2 cell surfaces): each ADDRESS-OF-taken record's <c>StorageCell</c> + its backing
    /// bridge, and each BASED root's implicit data-address pointer + its deref cell + backing. ⛔ THE ONE RENDERER
    /// for the program class AND the OO type-halves (kb/Work PB956): the OO emitter rendered
    /// <c>FieldEmitter</c> output only, so a class had to REFUSE every BASED item and ADDRESS OF target (a whole-unit
    /// COBOLNET0899) — the members the bound places name did not exist.
    /// <para>Storage duration follows the owning section. A RECURSIVE unit's static-WS based root and a method
    /// WORKING-STORAGE root emit STATIC members (§13.5.4 GR1 / §8.6.4 static items — one copy on the class); a method
    /// LOCAL-STORAGE / LINKAGE root's member is per ACTIVATION, so it is not <c>readonly</c> — <see cref="EmitMethod"/>
    /// re-seeds it on entry (<see cref="ActivationPointerSeeds"/>) and restores the activator's on exit.</para></summary>
    public void EmitPointerBackings(DataBinder data, CodeWriter w)
    {
        foreach (var (backing, cellField, canonical, cellWidth) in data.PtrAddressableBackings)
        {
            bool isStatic = data.StaticAddressableCells.Contains(cellField);
            // A method's LOCAL-STORAGE / LINKAGE cell and a program's LOCAL-STORAGE cell are re-seeded at each activation
            // (ActivationPointerSeeds — kb/Work PB956 / PB1132), so neither is readonly.
            bool perActivation = !isStatic
                && (data.OoMethodScopedRoots.Contains(canonical) || data.LocalStorageRoots.Contains(canonical));
            string mod = isStatic ? "private static readonly" : perActivation ? "private" : "private readonly";
            string rmod = isStatic ? "private static" : "private";
            w.Line($"{mod} StorageCell {cellField} = {AddressableCellInit(canonical, cellWidth)};   // ADDRESS-OF-taken record — cell storage (ISO §8.4.3.11; Phase-4b inc 2)");
            w.Line($"{rmod} ref string {backing} => ref {cellField}.Ref;");
        }
        foreach (var (backing, cellProp, addrField, width) in data.PtrBasedBridges)
        {
            // A RECURSIVE unit's static-WS based root — and a method WORKING-STORAGE based root — emits its bridge
            // STATIC (§13.5.4 GR1 / §8.6.4 static items — one copy on the class); the RECURSIVE one is reset to NULL by
            // __ResetStatics (§14.6.2.3.2 action 5; kb/Work PB154).
            string mod = data.StaticBasedBridgeAddrs.Contains(addrField) ? "private static" : "private";
            w.Line($"{mod} ManagedPointer {addrField} = ManagedPointer.Null;   // implicit data-address pointer (ISO §13.18.5.4 GR2 — initially NULL)");
            // ⛔ THE CELL FIRST, THE BACKING OVER IT (kb/Work PB231): the byte image and the addressed area's
            // MANAGED SLOTS are two halves of ONE StorageCell, and the GR3/GR4 loud deref happens once, on
            // the cell, so both halves see the same null/bounds verdict.
            w.Line($"{mod} StorageCell {cellProp} => {RuntimeApi.PtrDeref(addrField, $"{width}")};   // BASED deref bridge (GR3/GR4 loud)");
            w.Line($"{mod} ref string {backing} => ref {cellProp}.Ref;");
        }
    }

    /// <summary>The fresh <c>StorageCell</c> of an ADDRESS-OF-taken record — seeded with the SAME VALUE-honoring
    /// image expression the Tier-B stored backing uses (the based_pointer first-run lesson: a default image loses
    /// VALUE). ONE composer for the member's declaration and a method's per-activation re-seed.</summary>
    private string AddressableCellInit(DataItem canonical, int cellWidth) =>
        $"new StorageCell {{ Ref = {RuntimeApi.StrStore(new DataEmitter(Ctx).ImageInitOf(canonical), $"{cellWidth}")} }}"
        + new DataEmitter(Ctx).CellDynSeeds(canonical);   // the cell's dynamic-length half (kb/Work PB1026)

    /// <summary>⛔ THE PER-ACTIVATION DATA-POINTER SEEDS — ONE function for the method arm (kb/Work PB956) AND the
    /// cached-singleton program arm (kb/Work PB1132, <c>ProgramEmitter.EmitCallMethod</c>): for each cell-backed root of
    /// <paramref name="roots"/> — the activation's LOCAL-STORAGE roots and its non-formal LINKAGE roots — the member and
    /// the fresh value an activation starts from. A BASED root's implicit pointer starts NULL (§13.18.5.4 GR2 "The implicit
    /// data-address pointer has an initial value of NULL"; §14.6.2.3.2 action 5 for local storage, and §8.6.5 ends a
    /// linkage-section association "at the end of the execution of the runtime element"); an ADDRESS-OF-taken record
    /// starts a fresh cell holding its initial image (§8.6.4 — local storage is "allocated and set to initial state each
    /// time the runtime element containing them is activated"). These are exactly the storage channels the root-field
    /// re-initialization loops skip (a cell-backed root has no root field), so every loop that re-initializes automatic
    /// data for an activation also emits these.</summary>
    internal IEnumerable<(string Member, string Type, string Fresh)> ActivationPointerSeeds(DataBinder data, IEnumerable<DataItem> roots)
    {
        foreach (var root in roots)
        {
            if (root.Class is not { IsCellBacked: true } cls || !ReferenceEquals(cls.Canonical, root)) continue;
            if (cls.BasedPointerField is { } addr)
                yield return (addr, "ManagedPointer", "ManagedPointer.Null");
            else if (data.PtrAddressableCellOf.TryGetValue(cls, out var cell))
                yield return (cell, "StorageCell", AddressableCellInit(root, cls.Width));
        }
    }

    /// <summary>True when this unit must emit a <c>DescribeExternals()</c> activation-entry registration
    /// (ISO §14.8.4): the compilation group has an enabling EC-EXTERNAL <c>&gt;&gt;TURN</c> somewhere AND the
    /// unit describes any external record or external file connector. False keeps the generated source
    /// byte-identical to a pre-VCR-15 build (zero-scaffolding).</summary>
    public static bool WantsExternalDescribes(DataBinder data) =>
        data.ExternalDescribe && (data.CallExternalBackings.Count > 0 || data.Files.Any(f => f.IsExternal));

    /// <summary>Emit the <c>DescribeExternals()</c> ABI method — one <c>ExternalStore.Describe</c> per external
    /// record (§14.8.4.3 / §13.18.22 GR6 facts: byte count, record-name VALUE clause spec, strong TYPE name,
    /// CONSTANT RECORD presence) and per external file connector (§14.8.4.2 file-referencing control-item
    /// identities + the §12.4.5.3 GR1 a–m entry fingerprint), each carrying the unit's before-Environment-
    /// division mask (§14.8.4.1). It is an <see cref="ICobolProgram"/> member, not a private callee-body step:
    /// the §14.9.4.4 GR3e check is part of the ACTIVATION ATTEMPT and precedes GR3g's transfer of control, so
    /// the boundary (<c>ProgramTable.CallProgram</c>) calls it before <c>Call</c> — which is what makes
    /// "escaped from <c>Call</c>" an exact test for GR3i's "the program was successfully called" (kb/Work
    /// PB233). The main-program entry (<c>Activate</c>) calls it itself. A complete-record REDEFINES
    /// contributes nothing (GR6's explicit exemption — the descriptor is built from the base record only).
    /// The file connector's §12.4.5.3 GR1 identity is <see cref="SelectFingerprint"/>.</summary>
    /// <param name="data">The describing element's data.</param>
    /// <param name="unitPath">The describer's run-unit identity (the program's path, or the class half's C# name).</param>
    /// <param name="signature">The emitted member's declaration: the program ABI's <c>public void DescribeExternals()</c>,
    /// or a class half's <c>private static void __DescribeExternals(int __self)</c>, which each method's prologue passes
    /// to <c>ExternalStore.DescribeAtMethodActivation</c> with the METHOD's own mask (kb/Work PB1138).</param>
    /// <param name="selfMask">The C# expression of the activated element's §14.8.4.1 mask.</param>
    /// <param name="w">The writer.</param>
    public void EmitExternalDescribes(DataBinder data, string unitPath, string signature, string selfMask, CodeWriter w)
    {
        using (w.Block(signature))
        {
            foreach (var ext in data.CallExternalBackings)
            {
                // §13.18.22 GR6: the VALUE identity is the RECORD NAME's own VALUE clause specification ("the
                // VALUE clause specification, if any, for each record name ... shall be identical") — the
                // record-level clause text, not the subordinate items' clauses.
                string valueSpec = ext.Record.RawValue is { } rv ? CsLiteral(rv.ToUpperInvariant()) : "null";
                string strongKey = ext.Record.StrongType && ext.Record.TypeName is { } tn ? CsLiteral(tn.ToUpperInvariant()) : "null";
                w.Line($"ExternalStore.Describe({CsLiteral(unitPath)}, {CsLiteral(ext.ExternalName)}, "
                    + $"new ExternalDescriptor(\"record\", ByteCount: {ext.Width}, ValueImage: {valueSpec}, "
                    + $"StrongTypeKey: {strongKey}, ConstantRecord: {(ext.Record.IsConstantRecord ? "true" : "false")}), "
                    + $"{selfMask});   // §14.8.4.3 / §13.18.22.4 GR6");
            }
            foreach (var f in data.Files.Where(f => f.IsExternal))
            {
                string ItemRef(string? clauseName, DataItem? item) => clauseName is null ? "null"
                    : CsLiteral(BinderDriver.ExternalItemIdentity(item) ?? "!");   // "!" = present but NOT an external item (§14.8.4.2 violation face)
                string linage = f.Linage is null ? "null"
                    : CsLiteral(string.Join(";", f.Linage.Operands.Select(op => op.DataName is null
                        ? $"={op.Literal}"
                        : BinderDriver.ExternalItemIdentity(op.Item) ?? "!")));
                w.Line($"ExternalStore.Describe({CsLiteral(unitPath)}, {CsLiteral(f.ExternalName!)}, "
                    + $"new ExternalDescriptor(\"file\", FileStatusRef: {ItemRef(f.FileStatusName, f.FileStatusItem)}, "
                    + $"RelativeKeyRef: {ItemRef(f.RelativeKeyName, f.RelativeKeyItem)}, LinageRef: {linage}, "
                    + $"SelectFingerprint: {CsLiteral(SelectFingerprint(f))}), {selfMask});   // §14.8.4.2 / §14.8.4.4 / §12.4.5.3 GR1");
            }
        }
    }

    /// <summary>⛔ THE ONE §12.4.5.3 GR1 IDENTITY of an external file connector's file control entry — the string two
    /// describers' <c>ExternalStore.Describe</c> registrations compare for EC-EXTERNAL-FILE-MISMATCH (§14.8.4.4: "the
    /// rules specified in 12.4.5, File control entry General rule 1 apply"). One segment per GR1 item that the entry
    /// can make differ: a) OPTIONAL, b) the ASSIGN operands, c) the RECORD DELIMITER phrase, d) RESERVE integer-1,
    /// e) organization, f) access mode, g) the COLLATING SEQUENCE clauses, j) the prime key's description and relative
    /// location, k) each alternate key's description, relative location, DUPLICATES and SUPPRESS WHEN phrase (and so
    /// their number), l) sharing mode, m) lock mode. h) RELATIVE KEY and i) FILE STATUS are the §14.8.4.2 external-item
    /// references the descriptor carries beside it (<c>RelativeKeyRef</c> / <c>FileStatusRef</c>). The split-key
    /// <c>SOURCE IS</c> operands of j)/k) (data-name-6 / data-name-3) never reach here — the form is declined
    /// (Annex A.3 item 40, kb/Work PB358). Every segment is built from the bound <see cref="FileModel"/>; kb/Work
    /// PB1079 measured c), d), g), j)'s and k)'s descriptions and locations, and k)'s SUPPRESS WHEN missing from it,
    /// so a mismatch in any of them ran the activated program with no condition.</summary>
    internal static string SelectFingerprint(FileModel f)
    {
        // §12.4.5.3 GR1 j)/k) "The same data description entry for data-name-… as well as their relative location
        // within the associated record": the key's name as written (part of its data description entry), its
        // elementary/group shape and byte extent, its §14.8 conformance description (category, usage, PICTURE clause
        // identity, sign) and its byte offset within the record area (RecordLayout.OffsetOf, the same offset the
        // connector registers the key window at).
        static string KeyEntry(string name, DataItem? item) => item is null ? name.ToUpperInvariant()
            : $"{name.ToUpperInvariant()}@{RecordLayout.OffsetOf(item)?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?"}"
              + $":{(item.IsGroup ? "G" : "E")}{item.ByteWidth}:{OoConformance.ConformanceDescriptor(item)}";
        // k) "the same SUPPRESS WHEN phrase" — the operand as the literal position read it (form, class, characters,
        // figurative kind), so `SUPPRESS WHEN "XX"` and `SUPPRESS WHEN SPACE` differ and an absent phrase is empty.
        static string Suppress(SuppressWhenOperand? s) => s is null ? ""
            : $"{s.Form}/{s.Class}/{s.FigurativeKind}/{s.Characters.Length}:{s.Characters}";
        // g) "The same specification of COLLATING SEQUENCE clauses": the file-level clause's alphabet-names, and each
        // key-level clause's alphabet-name-3 against the KEY POSITION it names (P = the prime key, A<i> = the i-th
        // alternate), so the rule compares what the clause says about the file's keys; sorted, because the clauses'
        // order in the entry is not part of their specification.
        // The operand is matched by the key ITEM the binder resolved it to (kb/Work PB1075 — a qualified operand).
        string KeyPosition(CollatingKeyOperand k)
        {
            if (k.Key is not null && ReferenceEquals(f.RecordKeyItem, k.Key)) return "P";
            int i = k.Key is null ? -1 : f.AlternateKeyNames.FindIndex(a => ReferenceEquals(a.Item, k.Key));
            return i >= 0 ? $"A{i}" : k.Name.ToUpperInvariant();
        }
        string fileColl = f.FileLevelCollating is { } fc ? $"{fc.Alnum?.ToUpperInvariant()}/{fc.Nat?.ToUpperInvariant()}" : "";
        string keyColl = string.Join(",", f.KeyLevelCollating
            .SelectMany(c => c.Keys.Select(k => $"{KeyPosition(k)}={c.Alphabet.ToUpperInvariant()}"))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        // b) "A consistent specification for data-name-1, device-name-1, and literal-1 in the ASSIGN clause" — all
        // THREE operands, so the USING data-name is part of the identity, not just the TO target. Consistency rule
        // (the implementor's, GR1 b's second sentence; docs/CONFORMANCE.md §7 DOC-A.1-72): the same data-name
        // spelling, qualifiers included.
        return $"OPT={f.Optional}|ASSIGN={f.AssignTarget.ToUpperInvariant()}"
            + $"|USING={string.Join(" OF ", new[] { f.AssignUsingName ?? "" }.Concat(f.AssignUsingQualifiers)).ToUpperInvariant()}"
            // c) "Either the STANDARD-1 phrase or a consistent value of feature-name-1" — consistency rule: the same
            // phrase (the clause is declined accept-inert, kb/Work PB292, so the written phrase is all there is).
            + $"|DELIM={f.RecordDelimiter}|RESERVE={f.ReserveAreas}"
            + $"|ORG={f.Organization}|ACC={f.AccessMode}|COLL={fileColl};{keyColl}"
            + $"|KEY={(f.RecordKeyName is { } rk ? KeyEntry(rk, f.RecordKeyItem) : "")}"
            + $"|ALT={string.Join(",", f.AlternateKeyNames.Select(a => $"{KeyEntry(a.Name, a.Item)}:{a.Duplicates}:{Suppress(a.SuppressWhen)}"))}"
            + $"|SHARE={f.Sharing}|LOCK={(f.LockMode is { } lm ? $"{lm.Kind},{lm.Multiple}" : "")}";
    }

    private void EmitFileMembers(string csName, DataBinder data, BoundProgram bound, CodeWriter w)
    {
        var hostFiles = data.Files.Where(f => !f.IsSortMerge).ToList();   // an SD is the in-memory sort store, never a host connector
        if (hostFiles.Count == 0) return;
        w.Line();
        // A per-object minted-key field for each instance file (§9.1.4 — one connector per object): initialized once
        // per object (field initializers run before the ctor body), so the ctor's Register/track see it live. A
        // factory / EXTERNAL file has a static literal key (InstanceKeyField null) and emits no field.
        foreach (var f in hostFiles.Where(f => f.InstanceKeyField is not null))
            w.Line($"private readonly string {f.InstanceKeyField} = {RuntimeApi.FileMintInstanceKey(CsLiteral(f.CobolName))};");
        using (w.Block($"public {csName}()"))
        {
            U.SeqIo.EmitFileRegistration(w);   // each file registers under FileKeyExpr(f): a factory literal, or this.__fkey_X
            // ⛔ Nothing else installs here. Dynamic file assignment (ISO §12.4.5.3 GR3 b / §9.1.21 — Annex
            // D.19.9.2's own worked example is an instance file, "SELECT EMPLOYEE-FILE ASSIGN USING FILE-REF")
            // and the LINAGE operand values (§13.18.34 GR6 b) now travel with the OPEN/WRITE statements that read
            // them, so this ctor and DispatchEmitter.__Activate cannot drift apart on them: the LINAGE half used
            // to be installed ONLY on the program path, leaving a class's LINAGE FD with no page model at all
            // (kb/Work PB673 — the second arm of this registration dispatch).
            // A REPORT SECTION in this object/factory (Report Writer is a complete subsystem — the class emit path
            // just has to CALL it, the same class-emit-gap shape as inc 3/5): the engines construct AFTER their FDs
            // register (COBOLNET_REPORT_WRITER_DESIGN §4). Early-returns when Reports.Count == 0.
            U.ReportWriter.EmitReportConstruction(w);
            foreach (var f in hostFiles.Where(f => f.InstanceKeyField is not null))
                w.Line($"__TrackInstanceFile({FileKeyExpr(f)});");   // closed + dropped when the object is deleted (§9.1.4)
        }
        w.Line();
    }

    /// <summary>
    /// Emit one COBOL class as a real C# class (deep-dive D1/D2/D3/D7): instance fields from OBJECT data
    /// (VALUE clauses become field initializers — the generated public parameterless ctor IS the predefined
    /// NEW factory, D4: C# runs base-then-derived initialization exactly like COBOL's inherited-then-own
    /// order), one <c>public virtual</c> method per METHOD-ID whose body runs its exit-bounded pc range, and
    /// ONE <c>__Dispatch</c> over the class's whole method-paragraph space (the same dispatcher body a program
    /// class gets — the emit-into-a-type reuse). Runs on the SAME per-unit emitter-state switch as
    /// <c>ProgramEmitter.EmitProgramClass</c>.
    /// </summary>
    public void EmitClassUnit(OoClassUnit cls, CodeWriter w)
    {
        // The INSTANCE class (D1/D2 + slice 3a: `: BASE` when the class INHERITS — single inheritance v1,
        // SSOT §18.18 — else the CobolObject runtime root; Roslyn needs no declaration ordering). The DIRECT
        // IMPLEMENTS list joins the base list (§11.8 — the closure arrives transitively at the C# level);
        // covariant-return conformances render as EXPLICIT interface implementations (D-I1's adapter cure:
        // C# forbids covariant interface implementations that §9.3.8.2.3 5a/5c2 permit).
        string instBase = string.Join(", ", new[] { cls.Symbol.Base?.CsName ?? "CobolObject" }
            .Concat(cls.Symbol.Implements.Select(i => i.CsName)));
        var instExtras = adapters
            .Where(a => !a.Factory && ReferenceEquals(a.Impl.Owner, cls.Symbol))
            .Select(a =>
            {
                var (protoRet, protoSig) = OoSignatureOf(a.Proto);
                string args = string.Join(", ", a.Proto.Binding!.Formals.Select(f => OoArgPair(f.ParamName, f.OmittedFlag)));
                return $"{protoRet} {a.Iface.CsName}.{a.Proto.CsName}({protoSig}) => this.{a.Impl.CsName}({args});   // covariant-return adapter (§9.3.8.2.3 5c2)";
            })
            .ToList();
        // §16.2.2.2 GR1 — FactoryObject "determines the class of the object": the runtime BASE reads it from this
        // override, and the most-derived class's override is the runtime class's factory. Only a class that has
        // BASE's object interface has one (§16.1: "This use is not required").
        bool lifeCycle = cls.Symbol.InheritsStandardBase;
        if (lifeCycle)
            instExtras.Add($"protected override BASE__FACTORY __FactoryOfClass => {cls.Symbol.FactoryCsName}."
                + $"{NamingConvention.FactoryInstanceField};   // FactoryObject (ISO §16.2.2.2 GR1)");
        EmitTypeHalf(cls.Name, cls.CsName, instBase,
            cls.Data, cls.Refs, cls.Bound, cls.Symbol.Methods, w,
            headerExtras: instExtras.Count > 0 ? instExtras : null,
            sealedType: cls.Symbol.IsFinal);

        // The FACTORY class (brief D11 — a REAL sibling singleton, NEVER statics: §8.6.4 per-class copies of
        // inherited factory data; SELF-in-factory polymorphism SR4f + GR2; §9.3.6 chain resolution). Every
        // CLASS-ID emits one — a class with no FACTORY paragraph still needs its own factory object and a
        // chain node for inherited factory methods.
        string facBase = string.Join(", ", new[] { cls.Symbol.Base?.FactoryCsName ?? "CobolObject" }
            .Concat(cls.Symbol.FactoryImplements.Select(i => i.CsName)));
        var extras = new List<string>
        {
            // The factory object (§9.3.14.2: "created before it is first referenced by a run unit" and "deleted
            // after it is last referenced by a run unit") is the CURRENT RUN UNIT's, created on first reference
            // (RunUnit.FactoryObject) — never a process-lifetime static, which outlived the run unit with all its
            // factory data (kb/Work PB1069). A factory whose superclass is a COBOL class needs `new` to shadow
            // that class's accessor; the runtime BASE__FACTORY declares none, so there is nothing to hide.
            $"public {(cls.Symbol.Base is { IsStandard: false } ? "new " : "")}static {cls.Symbol.FactoryCsName} "
                + $"{NamingConvention.FactoryInstanceField} => {RuntimeApi.FactoryObject(cls.Symbol.FactoryCsName)};",
        };
        // New's creation step (§16.2.1.2 GR1), exactly when the class has BaseFactoryInterface through INHERITS
        // (§16.2; §9.3.9): a covariant override of BASE__FACTORY.__Create, so New invoked on a subclass's factory —
        // or through SELF in an inherited factory method — creates the RUNTIME factory's class. The generated
        // constructor IS the initialization (deep-dive D4); BASE__FACTORY.__New wraps it with GR2's
        // resource-failure leg. A class that does not inherit BASE has no New at all (kb/Work PB1548).
        if (lifeCycle)
            extras.Add($"protected override {cls.CsName} __Create() => new {cls.CsName}();   // New (ISO §16.2.1.2 GR1)");
        // The class's METHOD working-storage is static data (OO deep-dive D3), so a new run unit cannot make it fresh
        // by construction: each half that has any emits __ResetStatics (the ONE predicate, DataBinder.EmitsStaticReset,
        // shared with RecordStructEmitter), and the factory ADOPTS both into the run unit that creates it — reset at
        // its start (§14.6.2.3.2 case 1) and released at its termination (§14.6.11 3/4/6; kb/Work PB1069). The base
        // call keeps a superclass's own adoption on the chain.
        var adopt = new[] { (cls.Data, cls.CsName), (cls.FactoryData, cls.Symbol.FactoryCsName) }
            .Where(h => h.Item1.EmitsStaticReset)
            .Select(h => $"runUnit.AdoptStaticStorage({h.Item2}.__ResetStatics);")
            .ToList();
        if (adopt.Count > 0)
            extras.Add("protected override void __AdoptRunUnitStorage(RunUnit runUnit) { base.__AdoptRunUnitStorage(runUnit); "
                + string.Join(" ", adopt) + " }   // method WORKING-STORAGE → this run unit (ISO §14.6.2.3.2 / §14.6.11)");
        EmitTypeHalf(cls.Name, cls.Symbol.FactoryCsName, facBase,
            cls.FactoryData, cls.FactoryRefs, cls.FactoryBound, cls.Symbol.FactoryMethods, w, extras,
            sealedType: cls.Symbol.IsFinal);
    }

    /// <summary>The emit-into-a-type parameterization, realized (deep-dive Summary): ONE routine renders
    /// fields + methods + dispatch into a named type — called for the instance class and the factory class
    /// of every CLASS-ID (identical machinery; only the type identity, base, data forest, roster, and header
    /// extras differ).</summary>
    private void EmitTypeHalf(string cobolName, string csName, string baseCsName,
        DataBinder data, ReferenceResolver refs, BoundProgram bound, IReadOnlyList<OoMethodSymbol> roster,
        CodeWriter w, IReadOnlyList<string>? headerExtras, bool sealedType = false)
    {
        program.BeginUnit(w, data, refs);
        callState.SelfPath = cobolName;       // a CALL from a method names the class as its calling path (§8.4.6.3)
        callState.ReturningPlace = null;      // methods deliver results via slice-2 RETURNING, never the program ABI
        callState.Formals = [];               // a class has no program-ABI formals — clear the last program's (GR1c recognition)
        ecState.UnitHasF3 = false;            // declaratives inside methods are staged loud (no __EcDispatch here)
        ecState.UnitHasF3Perform = false;     // an F3 PERFORM inside a method is loud-rejected (§9.1-B) — never emitted here
        dispatch.UseDecls = false;               // a class owns no USE declaratives — clear any bleed from a prior unit (M2-OO-1i review)
        dispatch.OuterGlobalUse = false;
        dispatch.BeforeReportingSelectors.Clear();   // no GR4 report selector crosses into a class unit (kb/Work PB369)
        dispatch.DebugActive = false;            // a class owns no USE FOR DEBUGGING facility — clear any bleed (VCR 7.17)
        dispatch.UnitHasResume = bound.Ec?.HasResume ?? false;   // a METHOD declarative's RESUME needs the PERFORM landing (kb/Work PB1010)
        callState.InheritedStatusPlace.Clear();

        using (w.Block($"public {(sealedType ? "sealed " : "")}class {csName} : {baseCsName}"))
        {
            foreach (string line in headerExtras ?? [])
                w.Line(line);
            var fields = new DataEmitter(Ctx);
            fields.Emit();   // WS → INSTANCE fields (D3/D11); method WS → statics; VALUE inits = field initializers (D4)
            // The class's OBJECT-COMPUTER members (ISO §12.3.6 — §11.3: a CLASS-ID's ENVIRONMENT DIVISION applies to its
            // methods): __COLLATE / __COLLATE_NAT as per-type constants, from the ONE helper the program emitter uses
            // (kb/Work PB111 — they were never declared here, a CS0103 on the first method that compared or cased). The
            // classification is NOT a field of a class: a method is re-entered on the same object, so each method body
            // resolves its own activation LOCAL (EmitMethod).
            ObjectComputerEmit.EmitMembers(data, w, classificationField: false);
            EmitExternalBackings(data, w);       // M2-OO-1i inc 5: a class EXTERNAL FD record → the shared run-unit cell
            // §14.9.23.4 GR7 d) (kb/Work PB1138): each METHOD activation checks the external items its statements can
            // reference — this factory's or object's — so the half renders their registrations once, and every method
            // prologue passes them to the activation boundary with its own §14.8.4.1 mask (EmitMethod).
            if (WantsExternalDescribes(data) && roster.Count > 0)
                EmitExternalDescribes(data, csName, "private static void __DescribeExternals(int __self)", "__self", w);
            EmitPointerBackings(data, w);        // BASED bridges + ADDRESS-OF cells of object/factory AND method data (kb/Work PB956)
            U.ReportWriter.EmitReportMembers(w);              // M2-OO-1i review: a class REPORT SECTION's engine fields + compose methods (Report Writer is complete)
            EmitFileMembers(csName, data, bound, w);   // M2-OO-1i: object/factory file connectors + report construction register in an emitted ctor
            // A method file verb under >>TURN EC-I-O … CHECKING emits an __IoCheckEc call (§9.1.13.1 fatal-status
            // default); the class type must declare it. A class has no USE declaratives (Declaratives == null), so
            // EcEmitIoCheckEc reduces to the status→EC bridge — no __RunUse/__EcDispatch needed (M2-OO-1i review).
            // C1 (design SSOT §9.10.1): when ANY method has an F3 PERFORM (bound.Ec.HasF3Perform is class-level true),
            // the class-member __IoCheckEc must carry its frame-first branch (GR17 — a WHEN preempts the USE) and the
            // class needs the __EcPerform raise-site funnel. Both read ecState.UnitHasF3Perform AT EMIT TIME, and it
            // was cleared for the class at BeginUnit (set true only PER METHOD, later) — so set it for the duration of
            // these two class-member emissions. Byte-identical when no method has F3 (HasF3Perform false ⇒ flag false).
            bool classHasF3Perform = bound.Ec is { HasF3Perform: true };
            bool savedUnitF3P = ecState.UnitHasF3Perform;
            ecState.UnitHasF3Perform = classHasF3Perform;
            // The class-member __IoCheckEc serves every method's I-O statements, so its fatal default takes the METHODS'
            // automatic propagation (kb/Work PB1119). §7.3.21.3 SR1 keeps the directive out of a class, so every method
            // of one class folds the same state; the raising types are not read here (they serve the INVOKE pickup,
            // which is emitted inside each method with that method's own header).
            var savedPropagation = ecState.Propagation;
            ecState.Propagation = AutomaticPropagation.Of(roster.Any(m => m.AutomaticPropagationHere), inMethod: true,
                [], ecState.OoClasses);
            if (bound.Ec is { HasIoChecked: true }) U.Ec.EmitIoCheckEc([], w, asLocal: false);
            if (classHasF3Perform) U.Ec.EmitEcPerformMember(w);   // the raise-site funnel, once per class (§9.10.1-C1)
            ecState.UnitHasF3Perform = savedUnitF3P;
            ecState.Propagation = savedPropagation;
            if (bound.Paragraphs.Count > 0)
                w.Line($"private const int __N = {bound.Paragraphs.Count};   // paragraph count (all methods — one pc space)");
            w.Line();
            foreach (var m in roster)
                EmitMethod(bound, m, fields, w);
            EmitCobolInvoke(cobolName, roster, w);   // D10: the universal-dispatch switch (BOTH halves —
                                                       // a universal reference can hold a factory object)
        }
        w.Line();
    }

    /// <summary>Render the §14.9.23.4 GR7c two-arm stop for one <c>__CobolInvoke</c> conformance check.
    ///
    /// <para>GR7c sets EC-OO-UNIVERSAL "if checking for it is enabled in BOTH the activated method and the
    /// activating runtime element". The activator's half is <c>ExceptionState.OoUniversalChecking</c>, set by the
    /// emitted statement guard around the INVOKE; the METHOD's half is a compile-time literal folded at bind time
    /// (<see cref="OoMethodSymbol.OoUniversalCheckingHere"/>) — it is a property of the callee's SOURCE, not of
    /// run-unit state, so it cannot be a flag.</para>
    ///
    /// <para>When it is not enabled in both, no exception condition exists and none may be attributed — but a
    /// nonconforming crossing still cannot proceed into typed-native code, so the stop is a
    /// <c>CobolImplementorFatalException</c>, which carries NO EC name and therefore cannot be selected by any
    /// statement guard's <c>EcName ==</c> match (§14.6.13.1.1 NOTE 3 undefined-results latitude).</para></summary>
    private static string OoUnivStop(OoMethodSymbol m, string cond, string detailExpr) =>
        $"if ({cond}) {{ {OoUnivThrow(m, detailExpr)} }}";

    /// <summary>The unconditional form of <see cref="OoUnivStop"/> — for a violation the bound method carries whatever
    /// the arguments (an ANY LENGTH formal or returning item, §14.9.23.4 GR7 c)).</summary>
    private static string OoUnivThrow(OoMethodSymbol m, string detailExpr)
    {
        string both = m.OoUniversalCheckingHere ? "ExceptionState.OoUniversalChecking" : "false";
        return $"if ({both}) throw new CobolFatalException(\"EC-OO-UNIVERSAL\", {detailExpr}); "
            + $"throw new CobolImplementorFatalException({detailExpr});";
    }

    /// <summary>Emit the class's <c>__CobolInvoke</c> override (D10/D-U2/D-U4): a switch over the methods
    /// this type DECLARES that are NOT overrides (an override needs no case — the BASE class's case calls
    /// <c>this.M(…)</c> and C# virtual dispatch delivers the override; 0829 guarantees identical
    /// descriptors), keyed by each method's EXTERNALIZED name (<see cref="OoMethodSymbol.DispatchKey"/>), and a
    /// method that is absent or does not MATCH falls out of the switch into <c>base.__CobolInvoke</c> — the chain IS
    /// §9.3.6 resolution order, and the CobolObject root raises EC-OO-METHOD (§9.3.6 6); GR7 b)). Each case first
    /// decides the §9.3.6 match — arity, RETURNING presence BOTH directions, per-argument conformance-descriptor
    /// equality (D-U3: the SAME rule as the compile-time strict check) — and only a bound method's residual
    /// violations (§14.9.23.4 GR7 c): an ANY LENGTH formal or returning item, a RETURNING description that differs)
    /// raise EC-OO-UNIVERSAL (<see cref="EmitCobolInvokeCase"/>; kb/Work PB1500). Box forms are CANONICAL BY DESCRIPTOR (D-U6a — never
    /// by either side's StoreAsImage — the read-only projection of the Storage the group-tail StorageFormPass computes): S:* → string; N:Display:* →
    /// the display IMAGE string (bridged by the FormatDisplay/StoreDisplay overload pair); other N:* →
    /// the native value; O:* → the CobolObject reference. A type declaring zero non-override methods
    /// emits no override.</summary>
    private void EmitCobolInvoke(string cobolName, IReadOnlyList<OoMethodSymbol> roster, CodeWriter w)
    {
        var cases = roster.Where(m => m.OverrideOf is null).ToList();
        if (cases.Count == 0) return;
        w.Line();
        using (w.Block("public override void __CobolInvoke(string __name, CobolInvokeArg[] __a, CobolInvokeArg? __ret)"))
        {
            using (w.Block("switch (__name)"))
            {
                foreach (var m in cases)
                    // ⛔ THE CASE LABEL IS THE METHOD'S DISPATCH KEY (kb/Work PB1405): §8.3.2.2 1) maps a universal INVOKE's
                    // method-name "to the externalized name of the method to be invoked", which is the roster key the TYPED
                    // path resolves by (PB303) — never the declared METHOD-ID word, which an AS phrase replaces.
                    using (w.Block($"case {Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(m.DispatchKey, quote: true)}:"))
                        EmitCobolInvokeCase(cobolName, m, w);
            }
            // §9.3.6 2)/4): a class that declares no method of this name, or whose method does not MATCH the invocation, hands
            // the search to the class it inherits from; the CobolObject root is step 6) — EC-OO-METHOD (§14.9.23.4 GR7 b)).
            w.Line("base.__CobolInvoke(__name, __a, __ret);");
        }
    }

    /// <summary>One method's arm of the universal dispatch switch, in the order §14.9.23.4 GR7 prescribes (kb/Work PB1500).
    ///
    /// <para><b>GR7 b) — does this method MATCH?</b> §9.3.6's match rules are conditions of METHOD RESOLUTION: rule 1 (an
    /// equal argument count, trailing OPTIONAL formals counting as equal; RETURNING present on both sides or neither),
    /// rule 3 (every universal argument is BY REFERENCE, §14.9.23.3 SR6: an OMITTED one needs an OPTIONAL formal, any
    /// other one the same class, category and description — the descriptor projection of the one strict-conformance
    /// rule, <see cref="OoConformance.ConformanceDescriptor"/>), and rules 6)/7) (the RETURNING items can meet in a SET
    /// or a MOVE at all — <see cref="OoConformance.ReturningMatchClass"/>). A method that does not match is not bound:
    /// the arm <c>break</c>s out of the switch into the inherited class's search, and when no class matches, §9.3.6 6)
    /// sets EC-OO-METHOD — never a conformance violation of a method that was never selected.</para>
    ///
    /// <para><b>GR7 c) — the bound method's conformance.</b> Once bound, "neither a formal parameter nor the returning
    /// item in the invoked method shall be described with the ANY LENGTH clause, and the rules for conformance specified
    /// in 14.8.2 … and 14.8.3 … apply": the residue §9.3.6 does not restate is an ANY LENGTH formal or returning item and
    /// a RETURNING pair that can meet in a MOVE/SET but whose descriptions differ (§14.8.3.3). Those raise EC-OO-UNIVERSAL
    /// through <see cref="OoUnivStop"/>.</para></summary>
    private void EmitCobolInvokeCase(string cobolName, OoMethodSymbol m, CodeWriter w)
    {
        var formalsList = m.Binding!.Formals;
        var returning = m.Binding!.Returning;
        int formals = formalsList.Count;
        // §14.8.2.1 and §9.3.6 match rule 1: fewer arguments than formals is an EQUAL number when every formal to the
        // right of the last argument is OPTIONAL — so the least admissible count is one past the last NON-optional
        // formal (kb/Work PB757).
        int minArgs = formalsList.FindLastIndex(f => !f.Optional) + 1;
        var noMatch = new List<string>
        {
            minArgs == formals ? $"__a.Length != {formals}" : $"__a.Length < {minArgs} || __a.Length > {formals}",
            returning is null ? "__ret is not null" : "__ret is null",
        };
        for (int i = 0; i < formals; i++)
        {
            var f = formalsList[i];
            string wantLit = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(OoConformance.ConformanceDescriptor(f.Item), quote: true);
            // A position past the supplied arguments is a trailing omission (§14.9.23.4 GR9); the arity term above has
            // already proved every such formal OPTIONAL.
            string present = i < minArgs ? "" : $"__a.Length > {i} && ";
            // §9.3.6 match rule 3 b): a spelled OMITTED argument needs an OPTIONAL formal and is then "considered to
            // match exactly" — exempt from the descriptor term; against a non-OPTIONAL formal its descriptor fails it.
            string exempt = f.Optional ? $"__a[{i}].Descriptor != {RuntimeApi.ObjOmittedDescriptor} && " : "";
            noMatch.Add($"{present}{exempt}__a[{i}].Descriptor != {wantLit}");
        }
        if (returning is not null)
            noMatch.Add(ReturningClassMismatch(OoConformance.ReturningMatchClass(OoConformance.ConformanceDescriptor(returning))));
        w.Line($"if ({string.Join(" || ", noMatch.Select(t => $"({t})"))}) break;   // not a §9.3.6 match — the search continues upward");

        // GR7 c): the bound method's ANY LENGTH formal or returning item is a violation whatever the argument.
        if (formalsList.Any(f => f.Item.IsAnyLength) || returning is { IsAnyLength: true })
        {
            w.Line(OoUnivThrow(m, $"\"INVOKE '{cobolName}' '{m.Name}': a method whose formal parameter or returning item is "
                + "described with the ANY LENGTH clause cannot be invoked through a universal object reference (ISO §14.9.23.4 GR7 c))\""));
            return;
        }
        if (returning is not null)
        {
            string rl = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(OoConformance.ConformanceDescriptor(returning), quote: true);
            w.Line(OoUnivStop(m, $"__ret!.Descriptor != {rl}",
                $"$\"INVOKE '{cobolName}' '{m.Name}': the RETURNING item (caller {{__ret!.Descriptor}}, method \" + {rl} + \") "
                + "does not conform (ISO §14.9.23.4 GR7 c)/§14.8.3.3)\""));
        }
        for (int i = 0; i < formals; i++)
        {
            w.Line($"bool __o{i} = {(i < minArgs ? "" : $"__a.Length <= {i} || ")}__a[{i}].Omitted;   // §14.9.23.4 GR9");
            w.Line($"var __p{i} = __o{i} ? default! : {OoUnivUnbox(formalsList[i].Item, $"__a[{i}].Value")};");
        }
        string argList = string.Join(", ", Enumerable.Range(0, formals).Select(i => OoArgPair($"__p{i}", $"__o{i}")));
        w.Line(returning is null ? $"this.{m.CsName}({argList});" : $"var __rv = this.{m.CsName}({argList});");
        for (int i = 0; i < formals; i++)
            w.Line($"if (!__o{i}) __a[{i}].Value = {OoUnivRebox(formalsList[i].Item, $"__p{i}")};   // SR6 BY REFERENCE write-back");
        if (returning is not null)
            w.Line($"__ret!.Value = {OoUnivRebox(returning, "__rv")};");
        w.Line("return;");
    }

    /// <summary>The C# test that the caller's RETURNING descriptor is OUTSIDE <paramref name="matchClass"/> (§9.3.6 match
    /// rules 6/7; <see cref="OoConformance.ReturningMatchClass"/>): an object-reference or pointer class admits exactly its
    /// own prefix, the data class admits everything that is neither.</summary>
    private static string ReturningClassMismatch(string matchClass) =>
        matchClass.Length > 0
            ? $"!__ret.Descriptor.StartsWith({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(matchClass, quote: true)}, StringComparison.Ordinal)"
            : string.Join(" || ", OoConformance.ReturningReferenceClasses.Select(c =>
                $"__ret.Descriptor.StartsWith({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(c, quote: true)}, StringComparison.Ordinal)"));

    /// <summary>D-U6a: true when the item's canonical UNIVERSAL box form is the display IMAGE string while
    /// its local crossing form is native — the FormatDisplay/StoreDisplay bridge applies both directions.</summary>
    private static bool OoUnivImageBridged(DataItem item) =>
        !OoStringCarried(item)
        && item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display };   // CARRIAGE, not image form (kb/Work PB646)

    /// <summary>D-U6a's OTHER bridge (kb/Work PB187): true when the item's canonical box is its NATIVE value — a
    /// byte-form numeric other than zoned DISPLAY boxes by value (N:Float / N:Binary / N:Packed …) — while its
    /// local crossing form is the IMAGE string, because the item is windowed (a REDEFINES / cell-backed class
    /// member) or was promoted by StorageFormPass.UnifyCrossing. The box is decided by DESCRIPTOR and never by
    /// either side's storage, so the side whose storage is the image converts through the item's own byte
    /// form; without this arm the callee cast a boxed float to <c>string</c> (InvalidCastException on legal
    /// source) and a windowed caller boxed its image where the callee expected the value.</summary>
    private static bool OoUnivNativeBoxOverImage(DataItem item) =>
        OoStringCarried(item) && !item.IsGroup && !OoUnivDisplayBoxed(item)
        && item.Pic is { HasImageByteForm: true } && item.Pic.Usage is not Usage.National;

    /// <summary>True when the descriptor's canonical box is the zoned display IMAGE (N:Display:*) — the item's
    /// usage, never its storage (D-U6a).</summary>
    private static bool OoUnivDisplayBoxed(DataItem item) =>
        item.Pic is { Category: PicCategory.Numeric, IsFloat: false } p && p.ByteForm is NumericByteForm.Zoned
        && p.Usage is not Usage.National;

    /// <summary>The callee-side unbox: box value → a local in the FORMAL's own crossing form.</summary>
    private static string OoUnivUnbox(DataItem item, string box) =>
        // The variable-length carrier boxes and unboxes VERBATIM — it is already the crossing form, and its
        // descriptor (V:…) is what the GR7c check compares (kb/Work PB204). Without this arm __CobolInvoke
        // spelled `(string)box!` into a `ref CobolVarGroup` parameter: CS1503 on a method merely DECLARED,
        // the PB177 arm-A shape exactly.
        OoVarGroupCarried(item) ? $"({RuntimeApi.VarGroupType}){box}!"
        : OoUnivNativeBoxOverImage(item) ? NumericRenderer.ImageOfCarrier($"({item.Pic!.ClrType}){box}!", item)
        : OoStringCarried(item) ? $"(string){box}!"
        : OoUnivImageBridged(item) ? RuntimeApi.NumStoreDisplay($"(string){box}!", item.ProfileName, $"({item.ElementType})0")
        : item.Pic is { Category: PicCategory.ObjectReference } p ? $"({p.ClrType}){box}"
        : $"({item.ElementType}){box}!";

    /// <summary>The callee-side re-box: a local in the formal's crossing form → the canonical box form.</summary>
    private static string OoUnivRebox(DataItem item, string local) =>
        OoUnivImageBridged(item) ? RuntimeApi.NumFormatDisplay(local, item.ProfileName)
        : OoUnivNativeBoxOverImage(item) ? $"(object?){NumericRenderer.CarrierOfImage(local, item)}"
        : $"(object?){local}";

    /// <summary>Caller-side universal dispatch (D-U6): box every argument per ITS OWN descriptor's canonical
    /// form, dispatch through the GR5 null guard with the bind-normalized literal or the runtime-normalized
    /// identifier-2 value, then copy out every argument (SR6 — all BY REFERENCE) and deliver RETURNING (GR8)
    /// through the receiver's own storage form. No direct-<c>ref</c> fast path BY DESIGN — the box IS the
    /// crossing (the abstract dispatch signature cannot take refs without per-signature generics).</summary>
    public void EmitUniversalInvoke(BoundInvokeUniversal u)
    {
        var w = Ctx.Writer;
        int id = Ctx.Names.NextStoreTmp();
        // A spelled OMITTED argument boxes as the omitted sentinel; a forwarded formal (§8.8.4.8.4 GR1c) boxes its
        // presence and is read only when present (§14.9.23.4 GR10's "except as an argument") — kb/Work PB757.
        string?[] fwd = u.Args.Select(a => a.Source is { } s && callState.WholeFormalProbe(s) is { } pr
            ? CallEmitter.OmittedTest(pr) : null).ToArray();
        string boxes = string.Join(", ", u.Args.Select((a, i) => a.Address is { } ao
            // An ADDRESS-IDENTIFIER (kb/Work PB1137) boxes its pointer VALUE — the ONE address-operand renderer the
            // typed path uses — under its class-pointer descriptor; SR19 makes it sending, so nothing copies back.
            ? $"new CobolInvokeArg({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(a.Descriptor, quote: true)}, (object?){U.Ptr.AddressOperandText(ao)})"
            : a.Source is not { } src
            ? RuntimeApi.ObjOmittedArgument
            : fwd[i] is { } t
                ? $"new CobolInvokeArg({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(a.Descriptor, quote: true)}, {t} ? null : {OoUnivCallerRead(src)}, {t})"
                : $"new CobolInvokeArg({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(a.Descriptor, quote: true)}, {OoUnivCallerRead(src)})"));
        w.Line($"var __ua{id} = new CobolInvokeArg[] {{ {boxes} }};");
        w.Line(u.Returning is not null
            ? $"var __ur{id} = new CobolInvokeArg({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(u.ReturningDescriptor!, quote: true)});"
            : $"CobolInvokeArg? __ur{id} = null;");
        string selector = u.MethodLiteral is { } lit
            ? Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(lit, quote: true)
            : RuntimeApi.ObjNormalizeMethodName(PlaceRenderer.Read(u.MethodSource!));
        w.Line($"{RuntimeApi.ObjRequireNonNull(PlaceRenderer.Read(u.Receiver))}.__CobolInvoke({selector}, __ua{id}, __ur{id});");
        for (int i = 0; i < u.Args.Count; i++)
        {
            if (u.Args[i].Source is not { } src) continue;   // OMITTED or an address-identifier (SR19) — nothing to copy out
            string copyOut = OoUnivCallerWrite(src, $"__ua{id}[{i}].Value");
            w.Line((fwd[i] is { } t ? $"if (!{t}) {{ {copyOut} }}" : copyOut) + "   // BY REFERENCE copy-out (SR6)");
        }
        if (u.Returning is { } ret)
            w.Line(OoUnivCallerWrite(ret, $"__ur{id}!.Value") + "   // RETURNING delivery (§14.9.23.4 GR8)");
        EmitInvokePickup(u);   // §14.6.13.1.5 / §14.9.18.4 GR1b — the universal path propagates identically (D-EO6)
    }

    /// <summary>SET Format 5 (D-U7; §14.9.39 GR9/GR10): copy the ONE sender reference into each target in
    /// order. The cast renders total: conformance was bind-checked (0867), and C# reference conversions
    /// cover the widening directions (typed→universal, subclass→base, null, this).</summary>
    public void EmitSetObjectRef(BoundSetObjectRef s)
    {
        var w = Ctx.Writer;
        if (s.FromExceptionObject)
        {
            // §8.4.3.6 — the register is implicitly UNIVERSAL: a universal target copies the reference;
            // a TYPED target takes the runtime narrow check (§9.3.8.2 :12291 — conformance through a
            // universal source is a RUNTIME question; failure = EC-OO-UNIVERSAL, Table 13).
            foreach (var tp in s.Targets)
            {
                var td = tp.Item.Pic!.ObjectRef ?? ObjectRefDescriptor.Universal;
                if (td.IsUniversal)
                {
                    w.Line(PlaceRenderer.Write(tp, RuntimeApi.ExceptionObjectRead) + "   // SET universal TO EXCEPTION-OBJECT (§8.4.3.6)");
                    continue;
                }
                string clr = tp.Item.Pic!.ClrType.TrimEnd('?');
                int id = Ctx.Names.NextStoreTmp();
                w.Line($"var __xo{id} = ExceptionState.ExceptionObject;");
                w.Line($"if (__xo{id} is not null && __xo{id} is not {clr}) throw new CobolFatalException(\"EC-OO-UNIVERSAL\", "
                    + $"\"SET {tp.Item.CobolName} TO EXCEPTION-OBJECT: the current exception object is not a "
                    + $"{td.Spelled} (ISO 9.3.8.2 runtime conformance; Table 13)\");");
                w.Line(PlaceRenderer.Write(tp, $"({clr}?)__xo{id}") + "   // SET typed TO EXCEPTION-OBJECT (runtime-narrowed)");
            }
            return;
        }
        // §14.9.39.4 GR9 — the reference is stored in each receiver "in the order specified", so the SENDER is
        // evaluated once (kb/Work PB394). NULL / SELF / a factory singleton are constants; only a data-item read
        // (which may be subscripted, and which a prior receiver may alias) needs the local.
        string src = s.SourceIsNull ? "null"
            : s.SourceIsSelf ? "this"
            : s.SourceFactoryCs is { } fac ? $"{fac}.__Instance"
            : Ctx.SendOnce(PlaceRenderer.Read(s.Source!), s.Targets.Count, "setOr");
        foreach (var tp in s.Targets)
            w.Line(PlaceRenderer.Write(tp, $"({tp.Item.Pic!.ClrType})({src})") + "   // SET F5 (ISO §14.9.39.4 GR9 — reference copy)");
    }

    /// <summary>The spans of a FIXED-length group argument's tables that correspond to a variable-length group
    /// formal's dynamic-capacity tables (kb/Work PB965) — null when the argument is itself variable-length (it
    /// composes its own carrier) or is not a group place the correspondence can be stated for.</summary>
    private static int[]? FixedArgumentSpans(Place arg, DataItem formal) =>
        arg is not RedefViewPlace and not RefModPlace
        && !CallEmitter.CallPlaceIsVarGroup(arg)
        && arg.DenotedItem is { IsImageCapable: true } item
            ? VariableLengthCompatibility.CorrespondingSpans(item, formal)
            : null;

    /// <summary>The mirror of <see cref="FixedArgumentSpans"/> (kb/Work PB965): the spans of a FIXED-length group
    /// <paramref name="fixedSide"/>'s tables that correspond to the dynamic-capacity tables of the VARIABLE-length
    /// group place <paramref name="varSide"/> — a variable-length argument into a fixed-length group formal, or a
    /// RETURNING pair with one side of each (§14.8.2.2 / §14.8.3.2: "shall be compatible, as described in
    /// 8.5.1.12"). Null for any other pair — including two variable-length groups, which cross component-wise.</summary>
    private static int[]? VarPlaceSpans(Place varSide, DataItem fixedSide) =>
        CallEmitter.CallPlaceIsVarGroup(varSide) && ItemCategory.IsGroupItem(fixedSide)
        && !VariableLengthCompatibility.IsVariableLength(fixedSide)
            ? VariableLengthCompatibility.CorrespondingSpans(fixedSide, varSide.Item)
            : null;

    private static string OoUnivCallerRead(Place p) =>
        p is RefModPlace ? PlaceRenderer.Read(p)
        : CallEmitter.CallPlaceIsVarGroup(p) ? PlaceRenderer.VarGroupImage(p, "INVOKE argument")
        : OoUnivImageBridged(p.Item) ? PlaceRenderer.Read(new NumericImagePlace(p))
        : OoUnivNativeBoxOverImage(p.Item) ? $"(object?){NumericRenderer.CarrierOfImage(PlaceRenderer.Read(p), p.Item)}"   // kb/Work PB187
        : PlaceRenderer.Read(p);

    private static string OoUnivCallerWrite(Place p, string box) =>
        p is RefModPlace ? PlaceRenderer.Write(p, $"(string){box}!")
        : CallEmitter.CallPlaceIsVarGroup(p)
            ? PlaceRenderer.WriteVarGroupImage(p, $"({RuntimeApi.VarGroupType}){box}!", "INVOKE copy-out into")
        : OoUnivNativeBoxOverImage(p.Item) ? PlaceRenderer.Write(p, NumericRenderer.ImageOfCarrier($"({p.Item.Pic!.ClrType}){box}!", p.Item))   // kb/Work PB187
        : OoStringCarried(p.Item) ? PlaceRenderer.Write(p, $"(string){box}!")
        : OoUnivImageBridged(p.Item) ? PlaceRenderer.Write(new NumericImagePlace(p), $"(string){box}!")
        : p.Item.Pic is { Category: PicCategory.ObjectReference } pic
            ? PlaceRenderer.Write(p, RuntimeApi.ObjNarrowUniversal(pic.ClrType.TrimEnd('?'), box,
                $"INVOKE (universal) delivery into '{p.Item.CobolName}'"))
        : PlaceRenderer.Write(p, $"({p.Item.ElementType}){box}!");

    /// <summary>
    /// Emit one METHOD-ID as a real typed C# method (slice 2 — deep-dive D3/D6/D7/D8): BY REFERENCE formals as
    /// <c>ref</c> parameters copied into CAPTURABLE locals (a local function cannot capture a by-ref parameter),
    /// LINKAGE/LOCAL-STORAGE roots as locals (LOCAL-STORAGE re-initializes each activation, §8.6.4), the
    /// method's paragraph slice as a LOCAL-FUNCTION dispatcher (<c>__MDispatch</c> — it captures the locals by
    /// reference, so PERFORM recursion and the implicitly-RECURSIVE method rule, §12032/:12032, are structural),
    /// the ref copy-out, and the RETURNING local as the C# return value (§14.9.23.4 GR8). D7: <c>virtual</c> by
    /// default. The exit-bounded slice is the trap-#4 guard; a group formal crosses as its character image
    /// (the CALL-boundary discipline — a caller's group struct TYPE differs from the method's).
    /// </summary>
    private void EmitMethod(BoundProgram bound, OoMethodSymbol m, DataEmitter fields, CodeWriter w)
    {
        var (retType, sig) = OoSignatureOf(m);
        if (m.PropertySubject is { } subject)
        {
            // A PROPERTY-clause-synthesized accessor (D-P1): a DIRECT field body — identical descriptions
            // make the spec's implicit MOVE a straight copy (§13.18.42 GR1/GR2 :21214-21229).
            string pmod = m.OverrideOf is not null
                ? (m.IsFinal && !m.Owner.IsFinal ? "sealed override" : "override")
                : (m.IsFinal || m.Owner.IsFinal) ? "" : "virtual";
            string pmods = pmod.Length == 0 ? "" : pmod + " ";
            // ⛔ THROUGH THE SUBJECT'S PLACE, never its C# name (kb/Work PB956): a subject whose record is
            // cell-backed (an ADDRESS OF target) or a Tier-B REDEFINES window has NO field of its own — its storage
            // is a window over the class backing — so `=> {CsName}` was a CS0103 on legal source. A plain field's
            // place renders as its name, so the ordinary accessor is unchanged.
            var subjPlace = Refs.ResolveItem(subject);
            string getExpr = subjPlace is null ? subject.CsName : PlaceRenderer.Read(subjPlace);
            // An accessor is a method activation too (§14.9.23.4 GR7 d) — its external-item check precedes its body.
            string check = MethodExternalCheck(m) is { } mx ? mx + " " : "";
            if (m.Accessor == 'G')
                w.Line(check.Length == 0
                    ? $"public {pmods}{retType} {m.CsName}() => {getExpr};   // PROPERTY {m.PropertyName} GET (§13.18.42.4 GR1)"
                    : $"public {pmods}{retType} {m.CsName}() {{ {check}return {getExpr}; }}   // PROPERTY {m.PropertyName} GET (§13.18.42.4 GR1)");
            else
            {
                // The setter's one formal (§11.7.3 SR7) crosses through the SAME signature builder as every
                // method, so a PROPERTY SET that overrides or implements a written SET method cannot drift from it.
                string param = m.Binding!.Formals[0].ParamName;
                string store = subjPlace is null ? $"{subject.CsName} = {param};" : PlaceRenderer.Write(subjPlace, param);
                w.Line($"public {pmods}void {m.CsName}({sig}) {{ {check}{store} }}   // PROPERTY {m.PropertyName} SET (GR2)");
            }
            w.Line();
            return;
        }
        // D7's TOTAL modifier table (the OVERRIDE/FINAL wave): virtual by default (§9.3.6 runtime-class
        // dispatch); an override emits `override` — `sealed override` when ITS FINAL and the class is not
        // already sealed; a FINAL root method (or ANY fresh slot in a FINAL class) emits NON-virtual — a
        // `virtual` member inside a `sealed` class is Roslyn CS0549 on EMITTED code (the loud-failure trap
        // this table exists for). COBOL never expresses C# `new`/hiding (SR4a), so the set is total.
        string modifier = m.OverrideOf is not null
            ? (m.IsFinal && !m.Owner.IsFinal ? "sealed override" : "override")
            : (m.IsFinal || m.Owner.IsFinal) ? ""
            : "virtual";
        using (w.Block($"public {(modifier.Length == 0 ? "" : modifier + " ")}{retType} {m.CsName}({sig})   // METHOD-ID {m.Name} (ISO §11.7)"))
        {
            // This body's formals, for the §8.8.4.8.4 GR1c forwarding recognition (CallUnitState.WholeFormalProbe)
            // that every CALL and INVOKE argument inside the body consults (kb/Work PB757). Cleared below.
            callState.MethodFormals = m.Binding!.Formals;
            // §14.9.23.4 GR7 d) BEFORE e): the external items are checked as part of the ACTIVATION ATTEMPT, so a
            // violation leaves before control is transferred — no module-stack frame, no storage seeded, no statement
            // run ("the method invocation is not successful"; kb/Work PB1138).
            if (MethodExternalCheck(m) is { } extCheck)
                w.Line(extCheck + "   // §14.9.23.4 GR7 d) — the method activation's §14.8.4 external-item check");
            // ⛔ THE METHOD IS A RUNTIME ELEMENT AND MUST APPEAR ON THE MODULE-NAME STACK (fix-queue PB36).
            // §15.65.4 r5 names the four activation mechanisms outright — "This may be by a CALL statement, an
            // INVOKE statement, a function reference, or an inline invocation" — and INVOKE was the one missing,
            // so inside a method ACTIVATING returned the SINGLE SPACE r5 reserves for a main program (claiming the
            // method WAS one), CURRENT returned the caller's name, and STACK omitted the method entirely.
            // ⚠ THE FORMER JUSTIFICATION CITED REAL RULES THAT DO NOT GOVERN: r3 is about elements that are NOT
            // COBOL runtime elements, and r4 is about the FORM of the name — it even lists "method-id" among the
            // forms an implementor may return, which presumes the element is THERE. Latitude over which name
            // string, never over whether the frame exists.
            // The push is HERE, not at the INVOKE site, because a method is reached by a typed direct call, by the
            // universal __CobolInvoke switch, and by an inline invocation — one mechanism, not three arms.
            // Frame = (method name, declaring class as the compilation unit's outermost element, not nested), so
            // r7 CURRENT yields the class, r5 ACTIVATING the invoker, and r9 STACK the full chain.
            string __mLit = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(m.Name.ToUpperInvariant(), quote: true);
            string __cLit = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(m.Owner.Name.ToUpperInvariant(), quote: true);
            w.Line($"var __ms = {RuntimeApi.ModuleStack()}; __ms.Push({__mLit}, {__cLit}, false);   // §15.65.4 r5 — INVOKE is an activation");
            // The per-ACTIVATION data-pointer members (kb/Work PB956): save the activator's, start this activation
            // fresh, and restore in the activation's finally — so a recursive INVOKE on the same object neither
            // inherits nor clobbers its caller's based address or LOCAL-STORAGE cell (§8.6.4; §8.6.5).
            var ptrSeeds = ActivationPointerSeeds(Ctx.Data, m.Binding!.LocalRoots.Concat(m.Binding!.LinkageRoots)).ToList();
            for (int i = 0; i < ptrSeeds.Count; i++)
                w.Line($"{ptrSeeds[i].Type} __ptrSv{i} = {ptrSeeds[i].Member}; {ptrSeeds[i].Member} = {ptrSeeds[i].Fresh};   // per-activation data-pointer storage (ISO §8.6.4)");
            // The ACTIVATION's nonfatal selector (kb/Work PB1010 — the method twin of ProgramTable's install): a
            // condition raised at a RUNTIME site while this method executes selects over THIS method's declaratives
            // (§14.9.49.4 GR4 a)), never its invoker's. Saved here, installed where the method's own selection is in
            // scope (below), restored in the activation's finally. Only an EC-model group raises through it.
            bool nfInstall = ecState.Active && (m.Binding!.EntryPc <= m.Binding!.EndPc || m.Binding!.Declaratives.Count > 0);
            if (nfInstall)
                w.Line("var __nfM = ExceptionState.NonfatalDispatcher;   // the invoker's selector (ISO §14.6.13.1.4 #3)");
            w.Line("try");
            w.Line("{");
            if (Ctx.Data.Classification is { } cls)
                // The class's CHARACTER CLASSIFICATION, resolved at THIS method's activation (ISO §12.3.6.4 GR8; §14.6.6 r2 —
                // a method is a runtime element) into a local the method's dispatch local function captures (kb/Work PB111).
                w.Line(ObjectComputerEmit.ClassificationLocal(cls));
            // LINKAGE roots → locals: a formal seeds from its parameter (copy-in; the copy-out below realizes
            // the BY REFERENCE write-through at the method boundary); the RETURNING item and unattached
            // entries start at their initial state (§14.2.3 GR6 — callee-allocated). An OMITTED formal
            // (its presence flag true — §14.9.23.4 GR9) has no argument to copy in: its local starts at its
            // initial state instead, and §14.9.23.4 GR10 makes any content a reference observes undefined.
            foreach (var root in m.Binding!.LinkageRoots)
            {
                // A Tier-A (alias) view root forwards to its canonical's field — no local (symmetry with
                // BuildPhysicals; COBOLNET_DESIGN §4.1; M2-OO-1h review C).
                if (root.Class is { Tier: RedefinesTier.Alias } && !root.IsCanonical) continue;
                // A method Tier-B REDEFINES canonical's storage is its string backing, not the root struct (M2-OO-1h
                // step 3) — emit that as the local; a LINKAGE formal seeds it from the caller's image, width-normalized
                // to the class width (a wider redefiner needs the full backing — review D), else from the initializer.
                if (fields.MethodRedefinesBackingDecl(root) is { } bkl)
                {
                    var formalB = m.Binding!.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root));
                    w.Line($"string {bkl.Name} = {(formalB is null ? bkl.Init : $"{formalB.OmittedFlag} ? {bkl.Init} : {RuntimeApi.StrStore(formalB.ParamName, $"{root.Class!.Width}")}")};   "
                        + $"// LINKAGE Tier-B REDEFINES backing for {root.CobolName}");
                    continue;
                }
                // A NON-canonical Tier-B member is a WINDOW over that backing, never a second storage — the same
                // skip ProgramEmitter's LOCAL-STORAGE loop has carried since M2-OO-1h and this arm did not
                // (kb/Work PB203's sibling sweep: one rule, three loops, one of them missing it — the two-arm
                // dispatch shape again). Without it a method-scoped view root declared its OWN local, splitting
                // one §13.18.44 storage area in two; and now that a collapsed GROUP emits no record-struct type
                // at all it would be a CS0246 on legal source.
                // An ADDRESS-OF-taken LINKAGE formal (kb/Work PB1019, method arm): its storage is the per-activation
                // StorageCell ActivationPointerSeeds just re-seeded with the initial image (§14.2.3 GR6 for the RETURNING
                // item and an omitted formal), so a PRESENT formal copies the argument into that cell — the same
                // boundary copy every other formal takes, into the one storage its ADDRESS OF names (§8.4.3.11.4 GR1).
                if (root.Class is { Tier: RedefinesTier.StringCanonical, IsCellBacked: true } cellCls
                    && ReferenceEquals(cellCls.Canonical, root))
                {
                    if (m.Binding!.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root)) is { } cf)
                        w.Line($"if (!{cf.OmittedFlag}) {{ {MethodCellFormalStore(root, cf.ParamName)} }}   "
                            + $"// LINKAGE formal {root.CobolName} (BY REFERENCE copy-in to its addressable cell, §14.2.3 GR8)");
                    continue;
                }
                if (root.Class is { Tier: RedefinesTier.StringCanonical }) continue;
                var (type, init) = fields.RootDecl(root);
                var formal = m.Binding!.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root));
                if (formal is null)
                    w.Line($"{type} {root.CsName} = {init};   // LINKAGE {root.CobolName} (§14.2.3 GR6)");
                else if (root.IsGroup)
                {
                    // The image crossing: construct (arrays allocated), then distribute the caller's image
                    // through ⛔ THE ONE GROUP-IMAGE STORE (kb/Work PB177 arm A). This site used to spell
                    // `{root}.FromImage({param})` itself with NO capability consult, so a method DECLARING
                    // `01 G. 05 P USAGE POINTER. 05 A PIC X(4).` as a formal failed BACKEND compilation with
                    // CS1061 — `RecordStructEmitter` emits the codec for exactly `ElementImageCapable` items and
                    // a POINTER leaf is in neither of its category lists. Nothing invoked the method: the bind
                    // side (`DataBinder.Oo`) applies no image screen and `OoConformance.DescriptionMismatch`'s
                    // `!formal.IsImageCapable` arm runs only when an INVOKE or an override/implements PAIR
                    // exists, so a merely-DECLARED method must not emit uncompilable C#.
                    w.Line($"{type} {root.CsName} = {init};   // LINKAGE formal {root.CobolName} (group — image crossing)");
                    w.Line($"if (!{formal.OmittedFlag}) {{ " + (OoVarGroupCarried(root)
                        // §8.5.1.12's component carrier (kb/Work PB204) — the variable-length twin of the
                        // image distribution, through the SAME ONE channel.
                        ? PlaceRenderer.WriteVarGroupImage(MethodRootPlace(root), formal.ParamName,
                            "OO method LINKAGE formal copy-in of")
                        // A bit / national group receives its ELEMENTARY value through the CALL boundary's ONE
                        // write (kb/Work PB1166 — the twin of MethodBoundaryValue's arm).
                        : root.IsAsIfElementary ? CallEmitter.CallStringWrite(MethodRootPlace(root), formal.ParamName)
                        : PlaceRenderer.WriteFullGroupImage(MethodRootPlace(root), formal.ParamName,
                            "OO method LINKAGE formal copy-in")) + " }");
                }
                else
                    w.Line($"{type} {root.CsName} = {formal.OmittedFlag} ? {init} : {formal.ParamName};   "
                        + $"// LINKAGE formal {root.CobolName} (BY REFERENCE copy-in; omitted → initial state)");
            }
            foreach (var root in m.Binding!.LocalRoots)
            {
                if (root.Class is { Tier: RedefinesTier.Alias } && !root.IsCanonical) continue;   // Tier-A view → no local (review C)
                if (fields.MethodRedefinesBackingDecl(root) is { } bkl)   // Tier-B canonical → the string backing local (M2-OO-1h step 3)
                {
                    w.Line($"string {bkl.Name} = {bkl.Init};   // LOCAL-STORAGE Tier-B REDEFINES backing for {root.CobolName} (§8.6.4)");
                    continue;
                }
                if (root.Class is { Tier: RedefinesTier.StringCanonical }) continue;   // a view — a window over the backing, no local (kb/Work PB203)
                var (type, init) = fields.RootDecl(root);
                w.Line($"{type} {root.CsName} = {init};   // LOCAL-STORAGE {root.CobolName} — re-initialized each activation (§8.6.4)");
            }
            // A method LOCAL/LINKAGE table's INDEXED BY cell is a per-activation local (§8.6.4; M2-OO-1h step 4) —
            // the method's own cell (§11.7.4 GR5), reset to 1 each activation, never the shared class index field.
            foreach (var root in m.Binding!.LocalRoots.Concat(m.Binding!.LinkageRoots))
                foreach (var idx in DataBinder.IndexDeclarationsUnder(root))
                    w.Line($"long {idx.Cell} = 1;   // INDEX-NAME {idx.Name} (LOCAL/LINKAGE table cell, §8.6.4)");
            // ⛔ THE METHOD IS ITS OWN SELECTION SCOPE (kb/Work PB1010; design SSOT §9.10). §14.9.49.4 GR3 selects
            // over "the USE statements in the source element" and GR4 a) makes that the element containing the
            // raising statement — this method. So the per-unit selection state is set to THIS method's for the
            // duration of its body and restored after: its USE declaratives (§14.2.2 SR10 admits them in a method
            // definition) and/or its Format-3 PERFORM handlers (whose pc-ranges were appended to the class pc space;
            // F3HandlerBasePc is the CLASS handler base, HandlerUseId = DeclCount + (pc − base)). A method with
            // neither keeps no selection machinery and is byte-identical.
            var mDecls = m.Binding!.Declaratives;
            bool methodF3 = m.Binding!.HandlerCount > 0;
            bool methodSelects = methodF3 || mDecls.Count > 0;
            var savedSel = (ecState.UnitHasF3Perform, ecState.UnitHasF3, ecState.UnitHasF4, dispatch.UseDecls,
                dispatch.DeclCount, dispatch.F3HandlerBasePc);
            // ⛔ UNCONDITIONAL: a method is contained in no source element with a procedure division (§14.2.2
            // SR12/SR13), so §14.9.18.4 GR6 has no GLOBAL declarative to be within the range of and the emitted
            // method must not inherit the last PROGRAM's slots from this run-unit-lifetime state object (kb/Work PB409).
            var savedGlobalDecls = dispatch.GlobalDeclIds;
            dispatch.GlobalDeclIds = [];
            // A method is a source element of its own for §7.3.21.4 GR1 too (kb/Work PB1119): ITS automatic
            // propagation and ITS header's RAISING classes drive the fatal default and the INVOKE pickup in its body.
            var savedPropagation = ecState.Propagation;
            ecState.Propagation = AutomaticPropagation.Of(m.AutomaticPropagationHere, inMethod: true, m.Raising,
                ecState.OoClasses);
            if (methodSelects)
            {
                ecState.UnitHasF3Perform = methodF3;                            // raise sites emit __EcPerform
                ecState.UnitHasF3 = mDecls.Any(d => d.EcEntries is not null);   // → the method's __EcDispatch
                ecState.UnitHasF4 = mDecls.Any(d => d.Eo is not null);          // → the method's __EcObjDispatch
                dispatch.UseDecls = mDecls.Count > 0;                            // I-O verbs call the method's __IoCheck
                dispatch.DeclCount = mDecls.Count;
                dispatch.F3HandlerBasePc = methodF3 ? bound.F3HandlerBasePc : null;
            }
            if (m.Binding!.EntryPc <= m.Binding!.EndPc || mDecls.Count > 0)
            {
                // The method's slice of the class's one pc space, as a LOCAL FUNCTION (captures the locals
                // above by reference — zero allocation for direct calls). The slice opens at the method's
                // declarative sections (DeclStartPc), entered only through the method's own __RunUse.
                string saved = dispatch.DispatchName;
                dispatch.DispatchName = "__MDispatch";
                if (methodF3)
                    U.Dispatch.EmitDispatchMethod(bound, w, "int __MDispatch(int __startPc, int __exitPc)",
                        m.Binding!.DeclStartPc, m.Binding!.EndPc,
                        m.Binding!.HandlerStartPc, m.Binding!.HandlerStartPc + m.Binding!.HandlerCount - 1);
                else
                    U.Dispatch.EmitDispatchMethod(bound, w, "int __MDispatch(int __startPc, int __exitPc)",
                        m.Binding!.DeclStartPc, m.Binding!.EndPc);
                if (methodSelects)
                    // The ONE selection-machinery emitter, as LOCAL FUNCTIONS of this method (they call the local
                    // __MDispatch and capture the method's data). __useActive is sized to the declaratives plus the
                    // CLASS total handler count (the ONE HandlerUseId formula).
                    U.Dispatch.EmitUseMachinery(mDecls,
                        methodF3 && bound.F3HandlerBasePc is int cb ? bound.Paragraphs.Count - cb : 0,
                        hasIoChecked: mDecls.Count > 0 && bound.Ec is { HasIoChecked: true },
                        hasF3Perform: methodF3, w, asLocal: true);
                dispatch.DispatchName = saved;
                // The ACTIVATION boundary's checking scope (kb/Work PB841 — the INVOKE twin of ProgramTable.CallProgram):
                // the method's statements are its own source text (§7.3.25.4 GR6), so they start from all-off whatever
                // guard the INVOKE ran under, and the activator's flags come back on return. Taken HERE, after
                // __CobolInvoke's §14.9.23.4 GR7c check has read the activator's EC-OO-UNIVERSAL half.
                string restore = "ExceptionState.RestoreChecking(__ckM);";
                w.Line("var __ckM = ExceptionState.PushAllCheckingOff();   // the method's checking baseline (§7.3.25.4 GR6)");
                if (nfInstall)
                    w.Line("ExceptionState.NonfatalDispatcher = " + (U.Ec.UnitHasDispatchFunnel
                        ? $"new NonfatalSelectorFn(__ec => {U.Ec.EcDispatchExpr("__ec", "\"\"")});"
                        : "NonfatalSelectorFn.None;")
                        + "   // this method's USE selection for a runtime-site nonfatal raise (§14.9.49.4 GR4 a))");
                if (methodF3)
                {
                    // The F3-method entry FLOOR (§9.10.1-C2): a method is a separate source element — its own unmatched
                    // raises must NOT be intercepted by the ACTIVATOR's WHEN. Raise the floor to the entry depth; on
                    // exit restore it and defensively balance the stack (matching the CALL boundary, ProgramTable).
                    w.Line("int __f3fl = ExceptionState.RaisePerformFloor(); int __f3pd = ExceptionState.PerformDepth;   // §9.10.1-C2 — isolate from the activator's frames");
                    w.Line($"try {{ __MDispatch({m.Binding!.EntryPc}, {m.Binding!.EndPc}); }} catch (MethodReturn) {{ }} "
                        + $"finally {{ ExceptionState.RestorePerformFloor(__f3fl); ExceptionState.TrimPerformTo(__f3pd); {restore} }}   "
                        + "// GOBACK returns HERE (§14.9.18.4 GR4)");
                }
                else
                    w.Line($"try {{ __MDispatch({m.Binding!.EntryPc}, {m.Binding!.EndPc}); }} catch (MethodReturn) {{ }} "
                        + $"finally {{ {restore} }}   // GOBACK / falling off the last paragraph returns HERE (§14.9.18.4 GR4; deep-dive D8)");
            }
            (ecState.UnitHasF3Perform, ecState.UnitHasF3, ecState.UnitHasF4, dispatch.UseDecls,
                dispatch.DeclCount, dispatch.F3HandlerBasePc) = savedSel;
            dispatch.GlobalDeclIds = savedGlobalDecls;
            ecState.Propagation = savedPropagation;
            // BY REFERENCE copy-out (§14.2.3 GR8) / RETURNING (§14.9.23.4 GR8). A Tier-B REDEFINES canonical's
            // storage IS its string backing (a width-correct image), not the suppressed root struct — write that
            // back / return that, else the generated C# names an undeclared local (review A/emission).
            foreach (var f in m.Binding!.Formals)
            {
                string src = MethodBoundaryValue(fields, f.Item, "OO method BY REFERENCE copy-out");
                // No copy-out for an omitted formal: there is no argument, and the caller's slot is a placeholder.
                w.Line($"if (!{f.OmittedFlag}) {f.ParamName} = {src};   // BY REFERENCE copy-out (§14.2.3 GR8)");
            }
            if (m.Binding!.Returning is { } r)
            {
                string src = MethodBoundaryValue(fields, r, "OO method RETURNING delivery");
                w.Line($"return {src};   // the invocation result (§14.9.23.4 GR8)");
            }
            // Close the PB36 activation try. The finally must cover every exit — the RETURNING `return` above, a
            // GOBACK unwinding as MethodReturn, and an exception propagating to the invoker — or the stack leaks a
            // frame and every later MODULE-NAME reads one element too deep.
            w.Line("}");
            string ptrRestore = string.Concat(ptrSeeds.Select((p, i) => $"{p.Member} = __ptrSv{i}; "));
            string nfRestore = nfInstall ? "ExceptionState.NonfatalDispatcher = __nfM; " : "";
            w.Line($"finally {{ {ptrRestore}{nfRestore}__ms.Pop(); }}   // §15.65.4 — the activation ends with the method");
            callState.MethodFormals = [];
        }
        w.Line();
    }

    /// <summary>The method activation's §14.8.4 external-item check (§14.9.23.4 GR7 d); kb/Work PB1138): the containing
    /// half's registrations (<c>__DescribeExternals</c>, emitted by <see cref="EmitTypeHalf"/> under the same
    /// <see cref="WantsExternalDescribes"/> test) run by the activation boundary with THIS method's §14.8.4.1 mask. Null
    /// when the half describes nothing — a group with no EC-EXTERNAL TURN, or a half with no external items.</summary>
    private string? MethodExternalCheck(OoMethodSymbol m) => WantsExternalDescribes(Ctx.Data)
        ? $"ExternalStore.DescribeAtMethodActivation(__DescribeExternals, {m.ExternalCheckMaskHere});"
        : null;

    /// <summary>The C# (return-type, parameter-list) of a method or prototype — ONE builder shared by class
    /// method emission, interface member emission, and the covariant adapters, so the three can never drift
    /// (the same reasoning as the ONE DescriptionMismatch).</summary>
    private static (string RetType, string Sig) OoSignatureOf(OoMethodSymbol m)
    {
        string retType = m.Binding!.Returning is { } ret ? OoCrossingType(ret) : "void";
        string sig = string.Join(", ", m.Binding!.Formals.Select(f =>
            $"ref {OoCrossingType(f.Item)} {f.ParamName}, bool {f.OmittedFlag}"));
        return (retType, sig);
    }

    /// <summary>⛔ THE METHOD ABI'S ARGUMENT PAIR — one formal crosses as its typed <c>ref</c> value AND its
    /// omitted-presence flag (kb/Work PB757; COBOLNET_OO_DESIGN D6). ISO §14.9.23.4 GR9: "If an OMITTED phrase is
    /// specified or a trailing argument is omitted, the omitted-argument condition for that parameter shall be
    /// true in the invoked method" — a C# <c>ref T</c> has no omitted state, so the pair IS the state. Every
    /// caller renders its pair here (the typed INVOKE, the covariant adapter, the universal switch), so the
    /// signature <see cref="OoSignatureOf"/> builds and the argument lists cannot drift apart.</summary>
    private static string OoArgPair(string refExpr, string omittedExpr) => $"ref {refExpr}, {omittedExpr}";

    /// <summary>Emit one INTERFACE-ID as a C# interface (§11.6; D-I1): members are the prototypes' signatures
    /// (the SAME builder class methods use); the prototypes' numeric profiles and group struct types emit as
    /// interface STATICS (C# 8+) so cross-unit CONTENT conversions can qualify them.</summary>
    public void EmitInterfaceUnit(OoInterfaceSymbol iface, CodeWriter w)
    {
        var data = ifaceData[iface];
        // Interface units emit no statements, so the per-unit resolver is never consulted — and none can
        // exist: interfaces emit FIRST, before any program/class unit begins (the pre-9n code passed the
        // host's null-or-stale _refs field here, equally unread). The fresh renderers are unused but harmless.
        program.BeginUnit(w, data, null!);
        string bases = iface.Inherits.Count > 0
            ? " : " + string.Join(", ", iface.Inherits.Select(b => b.CsName))
            : "";
        using (w.Block($"public interface {iface.CsName}{bases}"))
        {
            new DataEmitter(Ctx).Emit();   // profiles + struct types only (LINKAGE roots are suppressed)
            foreach (var proto in iface.Prototypes)
            {
                var (retType, sig) = OoSignatureOf(proto);
                w.Line($"{retType} {proto.CsName}({sig});   // METHOD-ID {proto.Name} (prototype, §10.6.2 SR4)");
            }
        }
        w.Line();
    }

    /// <summary>Thin forward to THE one crossing-form predicate, <see cref="OoClassTable.StringCarried"/>
    /// (relocated to Binding in P6 Step 5 — the bind-phase override harmonize in <c>StorageFormPass</c> and these
    /// emit-side signature/marshaling renders must consult the SAME definition).</summary>
    private static bool OoStringCarried(DataItem item) => OoClassTable.StringCarried(item);

    /// <summary>True when an OO boundary item crosses as the §8.5.1.12 VARIABLE-LENGTH carrier — the THIRD
    /// crossing form (kb/Work PB204), the exact twin of <c>CallEmitter.CallPlaceIsVarGroup</c> at the CALL
    /// boundary. §14.9.23.3 contains no prohibition on such an operand and §14.8.2.2 / §14.8.3.2 ADMIT it
    /// subject to compatibility, so an INVOKE crossing one is conforming source.</summary>
    private static bool OoVarGroupCarried(DataItem item) => item.CurrentExtentImageCapable;

    /// <summary>⛔ THE ONE C# CROSSING TYPE of an OO boundary item — the dispatch every signature, property
    /// accessor, box lane and marshaling arm reads, so the three forms cannot drift (kb/Work PB204 replaced
    /// SIX copies of the two-way ternary <c>OoStringCarried(x) ? "string" : x.ElementType</c>; adding a form
    /// to a ternary spelled six times is how the two-arm defect gets made).</summary>
    private static string OoCrossingType(DataItem item) =>
        OoVarGroupCarried(item) ? RuntimeApi.VarGroupType
        : OoStringCarried(item) ? "string"
        : item.ElementType;


    /// <summary>Emit one bound INVOKE (deep-dive D5/D6 — the binder already resolved the call form and
    /// validated §14.8.2 strict conformance; this renders the type-preserving marshaling).</summary>
    public void EmitInvoke(BoundInvoke inv)
    {
        var w = Ctx.Writer;
        switch (inv.Form)
        {
            case InvokeForm.New:
            case InvokeForm.NewSelf:
                EmitNew(inv);
                return;
            case InvokeForm.Instance:
            case InvokeForm.Self:
            case InvokeForm.Super:
            case InvokeForm.Factory:
                EmitInstanceInvoke(inv);
                return;
            default:
                w.Line(LoudStmt($"INVOKE call form '{inv.Form}'"));
                return;
        }
    }

    /// <summary>Every invocation of the standard class BASE's New (ISO §16.2.1) — through a class-name (§14.9.23.3
    /// SR3), a FACTORY OF reference (§14.9.23.3 SR4 a/c) or SELF/SUPER in a factory method (§14.9.23.3 SR4 f/h) —
    /// reaches the ONE body <c>BASE__FACTORY.__New</c> on the factory object the form names, so
    /// §16.2.1.2 GR1 (creation of the factory's RUNTIME class through its covariant <c>__Create</c>) and GR2 (NULL +
    /// EC-OO-RESOURCE when the object cannot be created) hold identically for all of them. The receiving item is
    /// set to NULL before the call because GR2 says "the returned object reference is set to NULL" and
    /// EC-OO-RESOURCE, when checking for it is enabled, is raised from inside New — before any value could be
    /// delivered. The result is narrowed to the receiving item's type: the binder's §14.8.3.3 check proved it
    /// conforms, and New's declared result is <c>BASE</c>.</summary>
    private void EmitNew(BoundInvoke inv)
    {
        var w = Ctx.Writer;
        var ret = inv.Returning!;
        string factory;
        if (inv.Form is InvokeForm.NewSelf)
            factory = "this";   // SELF|SUPER in a factory method: the RUNTIME factory (§14.9.23.3 SR4 f/h; GR3 finds the same New)
        else if (inv.Receiver is { } recv)
        {
            // A FACTORY OF reference: GR5's null-receiver test comes first, and "execution of the INVOKE statement
            // is terminated" there, so the receiving item is untouched when it raises.
            factory = $"__nf{Ctx.Names.NextStoreTmp()}";
            w.Line($"var {factory} = {RuntimeApi.ObjRequireNonNull(PlaceRenderer.Read(recv))};   // §14.9.23.4 GR5");
        }
        else
            factory = $"{inv.ClassCsName}{NamingConvention.FactorySuffix}.{NamingConvention.FactoryInstanceField}";
        w.Line(PlaceRenderer.Write(ret, "null") + "   // §16.2.1.2 GR2 — NULL unless New creates the object");
        w.Line(PlaceRenderer.Write(ret, $"({ret.Item.Pic!.ClrType}){factory}.{OoStandardClasses.NewCsName}()")
            + "   // INVOKE … \"New\" (ISO §16.2.1.2)");
    }

    /// <summary>The instance-call marshaling (D6; §14.9.23.4 GR6/GR7a/GR8): every formal is a <c>ref</c>
    /// parameter — a plain field of matching storage passes DIRECTLY (aliasing; subscripts evaluate once at
    /// the call, the GR7a once-only rule); anything else lowers to a copy-in temp, <c>ref</c> the temp, and —
    /// for BY REFERENCE identifier args — a copy-out. BY REFERENCE crossings are TYPE-PRESERVING (the strict
    /// §14.8.2.3.2 bind rules); BY CONTENT crossings CONVERT into the formal's description per §14.8.2.3.3
    /// (COMPUTE/MOVE/SET), composing the formal's value/image through the OWNER class's internal profiles
    /// (<c>{OWNER}._P_n</c>). Order per GR8: the call, the BY REFERENCE copy-outs, then the RETURNING
    /// delivery — identifier-4's store is the FINAL effect (the review's overlap finding).</summary>
    private void EmitInstanceInvoke(BoundInvoke inv)
    {
        var w = Ctx.Writer;
        int id = Ctx.Names.NextOoInvoke();
        var argExprs = new List<string>();
        var post = new List<string>();

        var args = inv.Args ?? [];
        for (int i = 0; i < args.Count; i++)
        {
            var a = args[i];
            bool stringCarried = OoStringCarried(a.Formal);
            string qualProfile = a.Formal.Pic is { Category: PicCategory.Numeric, IsFloat: false }
                ? $"{inv.OwnerCsName}{(inv.Form is InvokeForm.Factory ? NamingConvention.FactorySuffix : "")}.{a.Formal.ProfileName}" : "";

            // ⛔ THE OMITTED ARGUMENT (kb/Work PB757) — spelled, or trailing-omitted: its slot is a placeholder of the
            // formal's crossing type, never read by the callee, paired with TRUE (§14.9.23.4 GR9).
            if (a.Omitted)
            {
                string om = $"__iv{id}_{i}";
                w.Line($"{OoCrossingType(a.Formal)} {om} = default!;   // OMITTED argument placeholder (§14.9.23.4 GR9)");
                argExprs.Add(OoArgPair(om, "true"));
                continue;
            }
            // ⛔ A FORWARDED FORMAL (§8.8.4.8.4 GR1c): an argument that is itself a whole formal parameter of this
            // source element carries its presence on, and — because §14.9.23.4 GR10 / §14.9.4.4 GR12 exempt a
            // reference "as an argument" — its copy-in and copy-out run only when it is present. The recognition
            // is the ONE CallUnitState.WholeFormalProbe the CALL arm uses.
            string? fwdTest = a.Source is { } fsrc && callState.WholeFormalProbe(fsrc) is { } fprobe
                ? CallEmitter.OmittedTest(fprobe) : null;
            void Post(string line) => post.Add(fwdTest is null ? line : $"if (!{fwdTest}) {{ {line} }}");

            // The direct-ref fast path: a MemberPlace whose STORAGE form matches the parameter type exactly
            // (BY REFERENCE identifiers only — CONTENT always copies). A forwarded formal takes the guarded
            // copy path instead: its read must not happen when it is omitted.
            if (fwdTest is null && a.Source is MemberPlace mp && a.WriteBack
                && (stringCarried
                    ? !mp.Item.IsGroup && OoStringCarried(mp.Item)
                    : !OoStringCarried(mp.Item))
                && !a.Formal.IsGroup && !mp.Item.IsGroup)
            {
                argExprs.Add(OoArgPair(PlaceRenderer.Read(mp), "false"));
                continue;
            }

            string slot = $"__iv{id}_{i}";
            string tmp = slot;
            if (fwdTest is not null)
            {
                // The guarded copy-in: the arms below declare the INNER value, assigned to the slot only when the
                // forwarded formal is present.
                tmp = slot + "_in";
                w.Line($"{OoCrossingType(a.Formal)} {slot} = default!;");
                w.Line($"if (!{fwdTest}) {{");
            }
            // BY CONTENT boolean-expression-1 / boolean literal-2 (§14.9.23.2; fix-queue PB46) — its OWN value
            // channel (D-B1: a '0'/'1' bit string), so it is rendered by the BOOLEAN renderer and stored by the
            // string store, never through NumStore. FIRST in the chain because a boolean and an alphanumeric
            // formal are both string-CARRIED, and the string arm below reads a Source or a literal this
            // argument does not have.
            if (a.ContentBool is { } cb)
            {
                // §8.8.2 rule 10 — the value's length is the largest boolean ITEM referenced (literals only carry
                // no item width, so the receiver's store fits them). The same width §14.9.8.4 GR3 states for a
                // boolean COMPUTE, carried at run time by the ONE renderer EmitComputeBoolean uses (kb/Work PB589).
                string bv = BooleanRenderer.RenderAtItemWidth(cb, Num);
                int bw = Math.Max(1, a.Formal.Pic!.Length);
                // ⛔ ANY LENGTH IS TESTED FIRST, AND THE ORDER IS THE WHOLE POINT. §13.18.2.3 SR1 admits the
                // picture symbol '1' as well as 'N' and 'X', so a category-BOOLEAN formal can carry ANY LENGTH
                // — and §13.18.2.4 GR1b then makes n "the length of the corresponding argument", not the one
                // symbol its PICTURE spells. With the category test first, `01 AL PIC 1 ANY LENGTH` received
                // B"1000" as B"1" (measured: `AL=[1] LEN=1`), silently, while the alphanumeric twin beside it
                // was right — the same two-arm asymmetry this whole fix exists to remove, reproduced inside
                // the new arm. §14.8.2.3.3 rule 2c is the conformance half of the same rule.
                // Otherwise §14.8.2.3.3 rule 2d ⇒ the MOVE store for the formal's category: a BOOLEAN receiver
                // pads and truncates in boolean ZEROS (§14.6.8.6), an ALPHANUMERIC one in spaces with the
                // boolean characters moved as-is (§14.9.25.4 GR6a — "If the sending item is of class boolean,
                // its boolean value shall be moved").
                w.Line($"string {tmp} = " + (a.Formal.IsAnyLength ? bv
                    : a.Formal.Pic!.Category is PicCategory.Boolean
                        ? RuntimeApi.StrStoreBoolean(bv, $"{bw}", a.Formal.Justified)
                    : ReceivingStore.Characters(a.Formal, bv, $"{bw}")) + ";");   // the ONE elementary character store (kb/Work PB871)
            }
            // A figurative-constant / ALL-literal literal-2 (kb/Work PB1617): §14.2.3 GR9's MOVE into the method
            // formal's allocated record, filled to that record's character positions (§8.3.3.6.4 GR2) by the ONE
            // argument fill the CALL and function lanes use. A group formal is the case that makes it load-bearing:
            // the image arm below would space-pad one occurrence.
            else if (a.ContentFill is { } fill)
                w.Line($"string {tmp} = {CallEmitter.FigurativeArgumentImage(fill, a.Formal, Ctx.Data)};");
            else if (OoVarGroupCarried(a.Formal))
            {
                // §14.8.2.2's variable-length sentence at the INVOKE boundary (kb/Work PB204): the carrier is
                // the group's §8.5.1.12 components, not a width-fitted image — there is no width to fit, and
                // the receiving side's own FromVarImage re-fits both halves. Bind has already run the
                // compatibility relation (OoBinder → DescriptionMismatch), so the pairing is sound here.
                // ⛔ A FIXED-length group argument is admitted too (§8.5.1.12.1 "only one of the operands may be
                // a variable-length group"; kb/Work PB965): it decomposes at the spans of ITS tables that
                // correspond to the formal's dynamic-capacity tables — the ONE correspondence walk, run here at
                // compile time because both descriptions are in hand.
                w.Line($"{RuntimeApi.VarGroupType} {tmp} = {(a.Source is { } vgp
                    ? FixedArgumentSpans(vgp, a.Formal) is { } fs
                        ? RuntimeApi.VarGroupFromFixedImage(CallEmitter.CallStringRead(vgp), CallEmitter.LayoutArray(fs))
                        : PlaceRenderer.VarGroupImage(vgp, "INVOKE argument")
                    : RuntimeApi.VarGroupEmpty)};");
            }
            else if (a.Source is { } vsp && VarPlaceSpans(vsp, a.Formal) is { } vs)
                // ⛔ A VARIABLE-length group argument into a FIXED-length group formal (§14.8.2.2; kb/Work PB965):
                // the formal reads the argument's image through the pair's correspondence, each corresponding
                // table fitted to the formal's occurrence count (§8.5.1.12.3 sentence 3).
                w.Line($"string {tmp} = {RuntimeApi.VarGroupToFixedImage(PlaceRenderer.VarGroupImage(vsp, "INVOKE argument"), a.Formal.ImageWidth, CallEmitter.LayoutArray(vs))};");
            // ⛔ A NUMERIC VALUE INTO AN IMAGE-CARRIED FIXED-POINT FORMAL (kb/Work PB1064): a method formal is a character
            // channel (§14.2.3 GR8), so a numeric-DISPLAY formal is carried as its image — and a literal-2 or an
            // arithmetic-expression-1 has no image, only a VALUE. §14.2.3 GR9 fills the formal's record by "a COMPUTE
            // statement without the ROUNDED phrase", so the value is the SAME store the native arms below render
            // (MethodNumericContent — one rule, two carriers), and the image is that record's storage image.
            else if (stringCarried && a.Source is null && a.StringLiteral is null
                     && a.Formal.Pic is { Category: PicCategory.Numeric, IsFloat: false }
                     && MethodNumericContent(a, qualProfile) is { } imageValue)
                w.Line($"string {tmp} = {RuntimeApi.NumFormatImage(imageValue, qualProfile)};");
            else if (a.Formal.IsGroup || (stringCarried && a.Source?.Item.IsGroup == true))
            {
                // The image crossing. BY REFERENCE allows a SMALLER formal (§14.8.2.2 rule 1 — a PREFIX of
                // the argument): pass the leading formal-width characters; the write-back below splices the
                // prefix back, preserving the argument's tail. CONTENT pads/truncates per MOVE.
                int fw = a.Formal.IsGroup ? CallEmitter.BoundaryImageWidth(a.Formal) : CallEmitter.ElementaryFormalWindow(a.Formal);
                string read = a.Source is { } gsp
                    ? a.ByContent ? CallEmitter.CallContentRead(gsp) : CallEmitter.CallStringRead(gsp)
                    : CsLiteral(a.StringLiteral ?? "");
                w.Line($"string {tmp} = {RuntimeApi.StrStore(read, $"{fw}")};");
            }
            else if (stringCarried)
                w.Line(a.Source is { } sp
                    ? $"string {tmp} = {OoStringReadOf(sp, a, qualProfile)};"
                    : a.StringLiteral is { } slit
                    // An ANY LENGTH formal sees the literal AT ITS OWN length (§13.18.2 GR1) — no width-fit.
                    ? $"string {tmp} = {(a.Formal.IsAnyLength ? CsLiteral(slit) : RuntimeApi.StrStore(CsLiteral(slit), $"{CallEmitter.ElementaryFormalWindow(a.Formal)}"))};"
                    // A numeric literal into an image-stored formal the arm above does not take (a non-numeric
                    // category): compose the formal's STORAGE image (kb/Work PB970 — of its own byte form, not a
                    // zoned digit run) through the OWNER's internal profile (qualified, never bare).
                    : $"string {tmp} = {RuntimeApi.NumFormatImage(EmitText.UnscaledAtScale(a.NumericLiteral!, a.Formal.Pic!.Scale), qualProfile)};");
            // The PICTURE-less carriers (object reference, data pointer, program pointer) cross VERBATIM: they
            // have no picture, no scale and no character image, so the crossing is a reference/handle copy and
            // never a numeric store. Pointer/ProgramPointer joined this arm with the §14.8.2.3.2 class-pointer
            // conformance rule (fix-queue PB46) — before that they were unreachable, and the plain arm below
            // would have run Num.AsNum over a ManagedPointer.
            // An ADDRESS-IDENTIFIER argument (kb/Work PB1021) crosses the same verbatim carrier, its value
            // rendered by the ONE address-operand renderer — a detached pointer value (§14.9.23.3 SR19).
            else if (a.Address is { } ao)
                w.Line($"{a.Formal.ElementType} {tmp} = {U.Ptr.AddressOperandText(ao)};");
            // The predefined NULL (§8.4.3.7 / §8.4.3.10; kb/Work PB1137 + PB1630) — BY CONTENT, the FORMAL's own null:
            // its PicInfo.DefaultInitializer, the value INITIALIZE's implicit SET TO NULL stores (ManagedPointer.Null /
            // ProgramPointer.Null / FunctionPointer.Null / null). A bare `null` was right only for an object reference.
            else if (a.PredefinedNull)
                w.Line($"{a.Formal.ElementType} {tmp} = {a.Formal.Pic!.DefaultInitializer};");
            // SELF (§8.4.3.8; kb/Work PB1137) — the object the containing method runs on, the SET F5 rendering.
            else if (a.SelfObject)
                w.Line($"{a.Formal.ElementType} {tmp} = this;");
            else if (a.Formal.Pic is { Category: PicCategory.ObjectReference or PicCategory.Pointer
                                                 or PicCategory.ProgramPointer or PicCategory.FunctionPointer })
                w.Line($"{a.Formal.ElementType} {tmp} = {PlaceRenderer.Read(a.Source!)};");
            else if (a.Formal.Pic is { IsFloat: true })
                // Same-usage float (bind-enforced): read the float value directly — never through the
                // scaled-integer path (the review's silent-truncation finding) — and on the item's OWN carrier
                // (NumericRenderer.FloatCarrierRead): a same-usage transfer is §14.9.25.4 GR6 c)'s "without
                // change", which a windowed binary32 decoded through binary64 is not (kb/Work PB961).
                w.Line($"{a.Formal.ElementType} {tmp} = {NumericRenderer.FloatCarrierRead(a.Source!, SendingRef.SameUsageMove)};");
            // BY CONTENT arithmetic-expression-1 (§14.9.23.2; fix-queue PB46) — §14.8.2.3.3 2) a): "the
            // conformance rules are the same as for a COMPUTE statement with the argument as the sending operand",
            // and §14.2.3 GR9 fills the formal's record by "a COMPUTE statement without the ROUNDED phrase" — i.e.
            // rescale + truncate into the formal's
            // description through the OWNER's internal profile, exactly as the identifier CONTENT arm below
            // does. The binder proved the formal is fixed-point category numeric, so this is the one shape.
            // …through the ONE store (NumericRenderer.StoreExpr — kb/Work PB84): an SDIDI intermediate (a
            // STANDARD-DECIMAL expression, a native integer power) takes the CobolDec overload; this arm used to
            // spell the native store only, a Roslyn CS1503 on `INVOKE … BY CONTENT A ** 2`.
            else if (a.ContentExpr is not null && MethodNumericContent(a, qualProfile) is { } exprValue)
                w.Line($"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType}){exprValue};");
            else if (a.ByContent && a.Source is { } cp
                     && Num.AsNum(new BoundFieldOperand(cp), ReceiverContext.None) is var cx
                     && (cp.Item.Pic?.Digits != a.Formal.Pic!.Digits || cp.Item.Pic?.Scale != a.Formal.Pic.Scale
                         // …and the SIGN rule, which the digit/scale pair does not imply. §14.9.25.4 GR6d2b:
                         // "When an unsigned numeric item is the receiving item, the ABSOLUTE VALUE of the
                         // sending value is used, and no operational sign is generated for the receiving
                         // item." A signed argument whose description otherwise matches an UNSIGNED formal
                         // used to fall to the plain arm below, which copies the native value verbatim —
                         // ClrType is `long` for every fixed-point usage, so −7 arrived as −7. The reverse
                         // (unsigned → signed) needs no conversion: GR6d2a makes the sign positive and the
                         // value is unchanged, so testing `!=` here would convert a provable identity.
                         || (cp.Item.Pic is { Signed: true } && !a.Formal.Pic!.Signed)))
                // CONTENT numeric conversion (COMPUTE rules, §14.8.2.3.3 2a): rescale + truncate into the
                // formal's description through the OWNER's internal profile.
                w.Line($"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType}){NumericRenderer.StoreExpr(cx, a.Formal.Pic!.Scale, qualProfile, raiseOnSizeError: ecState.SizeTruncationChecking)};");
            else if (a.Source is { } np)
                w.Line($"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType})({Num.AsNum(new BoundFieldOperand(np), ReceiverContext.None).Expr});");
            else
            {
                // A numeric LITERAL argument: its exact value as the (unscaled, scale) pair, stored through the
                // formal's own profile (§14.8.2.3.3 rule 2a's COMPUTE regime, as the CONTENT arms above).
                // ⛔ THE THIRD ARM OF THE SAME LITERAL-RENDERING DISPATCH as the CALL lane's two (kb/Work PB263):
                // UnscaledLit used to hand back a binary64 `Real` NumX for the floating-point form, so
                // `INVOKE … USING BY CONTENT 1.5E+3` emitted a C# `double` into an Int128-typed store and the
                // generated code did not compile — a raw Roslyn CS1503 on conforming source. It now decomposes
                // BOTH notations to the exact scaled integer of ISO §8.3.3.3.3 rule 5 / §8.3.3.3.2 rule 4.
                // (Evaluated ONCE — this used to call UnscaledLit twice to read its two halves.)
                // ⛔ THROUGH THE SAME StoreExpr AS THE TWO IDENTIFIER/EXPRESSION ARMS (kb/Work PB640): it was
                // the bare RuntimeApi.NumStore, so the unsigned-wide lane (StoreU) and the raising kernel the
                // other two arms now select were both missing HERE — the third arm of one rule.
                w.Line($"{a.Formal.ElementType} {tmp} = ({a.Formal.ElementType}){MethodNumericContent(a, qualProfile)};");
            }
            if (fwdTest is not null)
            {
                w.Line($"{slot} = {tmp};");
                w.Line("}");
                tmp = slot;
            }
            argExprs.Add(OoArgPair(slot, fwdTest ?? "false"));

            if (!a.WriteBack || a.Source is not { } src) continue;
            // Copy-out to the CALLER's storage (BY REFERENCE — §14.2.3 GR8 at statement granularity).
            if (OoVarGroupCarried(a.Formal))
                // No prefix splice: a variable-length crossing carries whole components, so the write-back is
                // the exact inverse of the read (kb/Work PB204) — for a FIXED-length argument, the inverse of
                // its decomposition, each component fitted to its fixed table as §14.6.9.2 fits a dynamic
                // sender into a non-dynamic receiver (kb/Work PB965).
                Post(FixedArgumentSpans(src, a.Formal) is { } ws
                    ? CallEmitter.CallStringWrite(src,
                        RuntimeApi.VarGroupToFixedImage(tmp, src.Item.ImageWidth, CallEmitter.LayoutArray(ws)))
                    : PlaceRenderer.WriteVarGroupImage(src, tmp, "INVOKE copy-out into"));
            else if (VarPlaceSpans(src, a.Formal) is { } wv)
                // …and its write-back OVERLAYS the argument's storage (§14.2.3 GR8): the argument's tables keep
                // their current capacities and its material past the formal survives (kb/Work PB965).
                Post(PlaceRenderer.WriteVarGroupImage(src,
                    RuntimeApi.VarGroupOverlayFixedImage(PlaceRenderer.VarGroupImage(src, "INVOKE copy-out into"), tmp,
                        CallEmitter.LayoutArray(wv)),
                    "INVOKE copy-out into"));
            else if (a.Formal.IsGroup || src.Item.IsGroup)
            {
                int fw = a.Formal.IsGroup ? CallEmitter.BoundaryImageWidth(a.Formal) : CallEmitter.ElementaryFormalWindow(a.Formal);
                // The §14.8.2.2 rule-1 prefix: splice the formal's characters back over the argument's
                // LEADING positions, preserving the tail beyond the formal's width.
                Post(CallEmitter.CallStringWrite(src,
                    // to-the-end read from fw+1 — the OMITTED-length sentinel (NOT −1, which now denotes a specified
                    // negative length that raises EC-BOUND-REF-MOD; review C14).
                    $"{tmp} + {RuntimeApi.StrRefMod(CallEmitter.CallStringRead(src), $"{fw + 1}", RuntimeApi.OmittedRefModLength)}"));
            }
            else if (src is RefModPlace)
                Post(PlaceRenderer.Write(src, tmp));   // RefModPlace.Write splices the window (§8.4.3.3.4 GR6)
            else if (stringCarried)
                Post(OoStringCarried(src.Item) ? PlaceRenderer.Write(src, tmp) : PlaceRenderer.Write(new NumericImagePlace(src), tmp));
            else
                Post(src.Item.StoreAsImage
                    ? PlaceRenderer.Write(src, NumericRenderer.ImageOfCarrier(tmp, src.Item))
                    : PlaceRenderer.Write(src, tmp));
        }

        string target = inv.Form switch
        {
            InvokeForm.Self => "this",
            InvokeForm.Super => "base",
            // The factory singleton is never null — no GR5 guard (brief D11); virtual dispatch through the
            // factory hierarchy realizes §9.3.6 factory resolution.
            InvokeForm.Factory => $"{inv.ClassCsName}{NamingConvention.FactorySuffix}.{NamingConvention.FactoryInstanceField}",
            _ => RuntimeApi.ObjRequireNonNull(PlaceRenderer.Read(inv.Receiver!)),
        };
        string call = $"{target}.{inv.MethodCsName}(" + string.Join(", ", argExprs) + ")";

        if (inv.ReturningSource is { } rs && inv.Returning is { } recv)
        {
            // GR8 — capture the result AT RETURN, flush the BY REFERENCE copy-outs, and store into
            // identifier-4 LAST (the final effect of the INVOKE — a receiver overlapping a temp-lowered
            // argument must see the argument's write-back first).
            string tmp = $"__ivr{id}";
            bool retString = OoStringCarried(rs);
            w.Line($"var {tmp} = {call};   // INVOKE (§14.9.23.4; null receiver → EC-OO-NULL, GR5)");
            foreach (var pLine in post) w.Line(pLine);
            // ⛔ A RETURNING pair with ONE variable-length side (§14.8.3.2 "If either the sending or the receiving
            // operand is a variable length group, the sending operand and the receiving operand shall be
            // compatible, as described in 8.5.1.12"; kb/Work PB965): delivered through the pair's correspondence
            // — a dynamic table's occurrences fitted to the fixed table (§14.6.9.2), a fixed table crossing at its
            // occurrence count (§8.5.1.12.3 sentence 3). The same two legs the program ABI's StoreReturn takes.
            if (OoVarGroupCarried(rs) && recv is not RedefViewPlace && recv.DenotedItem is { } ri
                && !CallEmitter.CallPlaceIsVarGroup(recv) && ItemCategory.IsGroupItem(ri)
                && VariableLengthCompatibility.CorrespondingSpans(ri, rs) is { } rvs)
                w.Line(CallEmitter.CallStringWrite(inv.Returning,
                    RuntimeApi.VarGroupToFixedImage(tmp, ri.ImageWidth, CallEmitter.LayoutArray(rvs))));
            else if (OoVarGroupCarried(rs))
                w.Line(PlaceRenderer.WriteVarGroupImage(inv.Returning, tmp, "INVOKE RETURNING delivery into"));
            else if (ItemCategory.IsGroupItem(rs) && VarPlaceSpans(recv, rs) is { } fvs)
                w.Line(PlaceRenderer.WriteVarGroupImage(inv.Returning,
                    RuntimeApi.VarGroupFromFixedImage(tmp, CallEmitter.LayoutArray(fvs)), "INVOKE RETURNING delivery into"));
            else if (rs.IsGroup || recv.Item.IsGroup)
                w.Line(CallEmitter.CallStringWrite(inv.Returning, tmp));
            else if (recv is RefModPlace)
                w.Line(PlaceRenderer.Write(recv, tmp));
            else if (recv.Item.Pic is { Category: PicCategory.ObjectReference } orp)
                // An object reference delivers "as if a SET statement were performed" (§14.8.3.3 rule 1), and the
                // binder proved the SET conformance — including the one sender whose C# type is WIDER than its
                // COBOL description: an ACTIVE-CLASS result, such as the standard class BASE's FactoryObject,
                // whose runtime member returns CobolObject (ISO §16.2). The narrowing is therefore total.
                w.Line(PlaceRenderer.Write(recv, $"({orp.ClrType}){tmp}"));
            else if (retString == OoStringCarried(recv.Item))
                // ANY LENGTH at the delivery boundary (ISO §14.8.3.3 rules 4/5; §13.18.2 GR1): a varying-length
                // SENDER delivers width-fitted into a fixed receiver (rule 5 — its length "considered to match");
                // a varying-length RECEIVER stores at its own current length (its n is fixed by ITS activation).
                w.Line(PlaceRenderer.Write(recv,
                    recv.Item.IsAnyLength
                        ? RuntimeApi.StrStore(tmp, $"{PlaceRenderer.Read(recv)}.Length")
                    : rs.IsAnyLength && recv.Item.Pic is { } rvp
                        ? RuntimeApi.StrStore(tmp, $"{Math.Max(1, rvp.Length)}")
                    : tmp));
            else if (retString)   // string-carried result into native-numeric storage
                w.Line(PlaceRenderer.Write(recv, NumericRenderer.CarrierOfImage(tmp, recv.Item)));
            else                  // native result into image-stored numeric storage
                w.Line(PlaceRenderer.Write(recv, NumericRenderer.ImageOfCarrier(tmp, recv.Item)));
        }
        else
        {
            w.Line($"{call};   // INVOKE (§14.9.23; null receiver → EC-OO-NULL, §14.9.23.4 GR5)");
            foreach (var pLine in post) w.Line(pLine);
        }
        EmitInvokePickup(inv);   // §14.6.13.1.5 / §14.9.18.4 GR1b — a method GOBACK … RAISING is consumed HERE (after GR8)
    }

    /// <summary>The copy-in read of an identifier argument for a STRING-CARRIED formal: a reference-modified
    /// place reads its window verbatim (§8.4.3.3.4 GR6 — the operand IS elementary alphanumeric); a string-stored
    /// item reads directly; a native display-numeric item formats through its OWN profile (caller-side). A
    /// CONTENT crossing normalizes to the formal's width (MOVE pad/truncate).</summary>
    /// <summary>The INVOKE-site propagation pickup (D-EO6): a method GOBACK/EXIT … RAISING stages; the
    /// ACTIVATING site consumes — after the RETURNING delivery and copy-outs (GR1b ordering). Instance/
    /// Self/Super/Factory + UNIVERSAL dispatches all pick up; NEW needs none (the generated ctor runs no
    /// user statements, D4). Gated on <c>EcState.Active</c>, which spans class units.</summary>
    private void EmitInvokePickup(IActivatingStatement site) => U.Call.EmitPropagationPickup(site);

    /// <summary>The VALUE a literal-2 or arithmetic-expression-1 argument stores into a fixed-point numeric method formal
    /// (kb/Work PB1064): §14.2.3 GR9 fills the formal's record by "a COMPUTE statement without the ROUNDED phrase", so
    /// it is the ONE numeric store (<see cref="NumericRenderer.StoreExpr"/> — PB84's SDIDI overloads, PB640's raising
    /// kernel) into the formal's scale through the OWNER's profile, and the literal is the exact scaled integer of both
    /// notations (PB263). A native formal casts it to its element type; an image-carried one formats it to its storage
    /// image. Null when the argument is neither.</summary>
    private string? MethodNumericContent(BoundInvokeArg a, string qualProfile)
    {
        NumX? value = a.ContentExpr is { } cex ? Num.AsNum(new BoundComputedOperand(cex), ReceiverContext.None)
            : a.NumericLiteral is { } lit ? UnscaledLit(lit)
            : null;
        return value is not { } v ? null
            : NumericRenderer.StoreExpr(v, a.Formal.Pic!.Scale, qualProfile, raiseOnSizeError: ecState.SizeTruncationChecking);
    }

    private string OoStringReadOf(Place sp, BoundInvokeArg a, string qualProfile)
    {
        string read = sp is RefModPlace ? PlaceRenderer.Read(sp)
            : OoStringCarried(sp.Item) ? PlaceRenderer.Read(sp)
            : PlaceRenderer.Read(new NumericImagePlace(sp));
        // An ANY LENGTH formal takes the argument's characters AT the argument's length (ISO §13.18.2 GR1 —
        // n = the length of the corresponding argument), so the CONTENT copy must NOT width-normalize to the
        // formal's Pic.Length (1); the raw read IS the width-correct crossing.
        if (!a.ByContent || a.Formal.IsAnyLength || a.Formal.Pic is not { } fp) return read;
        // ⛔ A FIXED-POINT NUMERIC formal carried as its image (a redefined formal — kb/Work PB970): §14.2.3 GR9
        // fills the formal's record by "a COMPUTE statement without the ROUNDED phrase" (§14.8.2.3.3 2) a) states
        // the matching conformance rule), so the argument's VALUE is stored into the formal's description and the carrier is that record's STORAGE image — through the
        // OWNER's profile, qualified, exactly as the native CONTENT arm does. The MOVE store below rendered the
        // formal's profile BARE in the activating class, a Roslyn CS0103 on conforming source.
        if (fp is { Category: PicCategory.Numeric, IsFloat: false }
            && sp.DenotedItem is { Pic: { Category: PicCategory.Numeric, IsFloat: false } })
        {
            return RuntimeApi.NumFormatImage(NumericRenderer.StoreExpr(
                Num.AsNum(new BoundFieldOperand(sp), ReceiverContext.None), fp.Scale, qualProfile,
                raiseOnSizeError: ecState.SizeTruncationChecking), qualProfile);
        }
        // ⭐ THE CROSSING TAKES THE RECEIVING CATEGORY'S MOVE STORE, REACHED RATHER THAN RE-DERIVED
        // (fix-queue PB53). §14.8.2.3.3 rule 2d makes a BY CONTENT crossing conform "as for a MOVE statement",
        // and the store discipline that rule implies already exists, written once, in
        // <see cref="MoveEmitter.ConvertSource"/>: a BOOLEAN receiver pads and truncates in boolean ZEROS
        // (§14.6.8.6), an ALPHANUMERIC one in spaces, a NATIONAL one in national spaces, a numeric-EDITED one
        // EDITS into its mask (§14.9.25.4 GR5).
        // ⛔ ONLY ALPHANUMERIC WAS HANDLED HERE, AND THAT WAS SAFE ONLY BECAUSE THE BINDER OVER-REJECTED. The
        // screen demanded §14.8.2.3.2 strict IDENTITY for every other string-carried formal, so widths and
        // categories always matched and no store discipline was needed. Table 16 admits differing categories,
        // so the moment the screen was corrected this line became load-bearing.
        return fp.Category is PicCategory.Alphanumeric && sp.Item.Pic?.Category == PicCategory.Alphanumeric
                && fp.EditMask is null
            // The proven identical-category fast path, byte-for-byte as before: a plain width fit.
            ? RuntimeApi.StrStore(read, $"{Math.Max(1, fp.Length)}")
            : U.Move.ConvertSource(new BoundFieldOperand(sp), a.Formal);
    }

}
