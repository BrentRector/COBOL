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
        : OoVarGroupCarried(root) ? PlaceRenderer.VarGroupBoundaryImage(MethodRootPlace(root), what + " of")
        // A bit / national group hands back its ELEMENTARY value, an alphanumeric group its image — the CALL
        // boundary's ONE read (kb/Work PB1166), so the method ABI and the program ABI cannot speak two alphabets.
        : root.IsAsIfElementary ? CallEmitter.CallStringRead(MethodRootPlace(root))
        // A strong group with no character image hands back its leaf vector (kb/Work PB1116).
        : OoClassTable.LeafCarried(root) ? PlaceRenderer.GroupLeaves(MethodRootPlace(root))
        : root.IsGroup ? PlaceRenderer.GroupImage(MethodRootPlace(root), what)
        : root.CsName;

    /// <summary>An ADDRESS-OF-taken method LINKAGE root (kb/Work PB1019, method arm) lives in its per-activation
    /// <c>StorageCell</c> (<see cref="ActivationPointerSeeds"/>), so its crossing value is read from that storage: a
    /// root that crosses as characters (<see cref="OoCrossingType"/> <c>string</c> — a group, or an elementary item
    /// stored as its image) is the cell's whole backing image; a typed crossing reads the item through its
    /// place (the window over the cell).</summary>
    private string MethodCellFormalLoad(DataItem root) =>
        // A character crossing through the CALL boundary's ONE reader (kb/Work PB1166): a group's image, a bit / national
        // group's ELEMENTARY value — never the cell's raw backing, whose packed bits are not the crossing's alphabet
        // (kb/Work PB2087: a bit-group area formal over an argument with no cell read `0011` for B"1010").
        OoCrossingType(root) == "string" ? CallEmitter.CallStringRead(MethodCellPlace(root))
        // A variable-length group crosses as its §8.5.1.12 component carrier (kb/Work PB2094: its area is a cell now).
        : OoVarGroupCarried(root) ? PlaceRenderer.VarGroupBoundaryImage(MethodCellPlace(root), "OO method boundary value of")
        // A strong group with no character image crosses as its leaf vector (kb/Work PB1116) — the cell's managed slots.
        : OoClassTable.LeafCarried(root) ? PlaceRenderer.GroupLeaves(MethodCellPlace(root))
        : PlaceRenderer.Read(MethodCellPlace(root));

    /// <summary>The inverse of <see cref="MethodCellFormalLoad"/>: store the argument into the root's cell.</summary>
    private string MethodCellFormalStore(DataItem root, string value) =>
        OoCrossingType(root) == "string" ? CallEmitter.CallStringWrite(MethodCellPlace(root), value)
        : OoVarGroupCarried(root) ? PlaceRenderer.WriteVarGroupImage(MethodCellPlace(root), value, "OO method formal copy-in of",
            formalStorage: true)
        : OoClassTable.LeafCarried(root) ? PlaceRenderer.WriteGroupLeaves(MethodCellPlace(root), value)
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
            // The storage duration (§8.6.4) is classified ONCE, by DataBinder.LifetimeOfCell — the same answer decides who
            // ENDS the cell's life (kb/Work PB1216). A method's LOCAL-STORAGE / LINKAGE cell and a program's LOCAL-STORAGE
            // cell are re-seeded at each activation (ActivationPointerSeeds — kb/Work PB956 / PB1132), so neither is readonly.
            var lifetime = data.LifetimeOfCell(cellField, canonical);
            bool isStatic = lifetime == CellLifetime.Static;
            bool perActivation = lifetime == CellLifetime.Activation;
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
    internal IEnumerable<(string Member, string Type, string Fresh, string? End)> ActivationPointerSeeds(DataBinder data, IEnumerable<DataItem> roots)
    {
        foreach (var root in roots)
        {
            if (root.Class is not { IsCellBacked: true } cls || !ReferenceEquals(cls.Canonical, root)) continue;
            if (cls.BasedPointerField is { } addr)
                yield return (addr, "ManagedPointer", "ManagedPointer.Null", null);
            else if (data.PtrAddressableCellOf.TryGetValue(cls, out var cell))
                // End = the statement that ends THIS activation's life of the cell at its exit (§8.6.4: LOCAL-STORAGE "persists
                // while that instance of the runtime element is in active state"; kb/Work PB1216). A BASED pointer owns no
                // storage, so it has none.
                yield return (cell, "StorageCell", AddressableCellInit(root, cls.Width), $"{cell}.End(StorageEnd.ActivationEnded);");
        }
    }

    /// <summary>True when this unit must emit a <c>DescribeExternals()</c> activation-entry registration
    /// (ISO §14.8.4): the compilation group has an enabling EC-EXTERNAL <c>&gt;&gt;TURN</c> somewhere AND the
    /// unit describes any external record or external file connector. False keeps the generated source
    /// byte-identical to a pre-VCR-15 build (zero-scaffolding).</summary>
    public static bool WantsExternalDescribes(DataBinder data) =>
        data.ExternalDescribe && (data.CallExternalBackings.Count > 0 || data.Files.Any(f => f.IsExternal));

    // ⛔ THE VALUE-IDENTITY KEY OF A RECORD NAME'S VALUE CLAUSE (kb/Work PB1236). §13.18.22.4 GR6 compares the
    // specification, and a literal's CHARACTERS are its content: "ABCD" and "abcd" are two different VALUE clauses.
    // Only the WORDS of the clause are case-insensitive — a figurative constant's keyword (SPACE / space, ALL,
    // the N / B / X literal prefix) and a hexadecimal literal's digits (X"4a" is X"4A") — so those are folded and a
    // quoted literal's text is kept exactly.
    internal static string ValueSpecificationKey(string raw)
    {
        int q = raw.IndexOfAny(['"', (char)39]);
        if (q < 0) return raw.ToUpperInvariant();                    // no literal text at all: keywords and numbers
        string head = raw[..q].ToUpperInvariant();
        string body = raw[q..];
        return head.TrimEnd().EndsWith('X') ? head + body.ToUpperInvariant() : head + body;   // X"…" hex digits: any case
    }

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
                string valueSpec = ext.Record.RawValue is { } rv ? CsLiteral(ValueSpecificationKey(rv)) : "null";
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
              + $":{(item.IsGroup ? "G" : "E")}{item.ByteWidth}:"
              + (ActivationDescriptions.Of(item) is { } d ? $"{d.Category}/{d.Clauses}/{d.Positions}" : "-");
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
                string args = string.Join(", ", a.Proto.Binding!.Formals.Select(f => OoArgPair(f.ParamName, f.AreaParam, f.OmittedFlag))
                    .Concat(OoReturnsAnyLength(a.Proto) ? [RetLenParam] : []));
                // The conversion to the PROTOTYPE's type is explicit whenever the two return types differ: rule 5 c) 2. is a
                // class (implicit upcast, the cast is redundant) but rule 5 a) admits an INTERFACE-typed return for a
                // universal prototype, which C# converts to the universal class only by a cast (kb/Work PB1499).
                string conv = protoRet == "void" ? "" : $"({protoRet})";
                return $"{protoRet} {a.Iface.CsName}.{a.Proto.CsName}({protoSig}) => {conv}this.{a.Impl.CsName}({args});   // covariant-return adapter (§9.3.8.2.3 5a/5c2)";
            })
            .ToList();
        // §16.2.2.2 GR1 — FactoryObject "determines the class of the object": the runtime BASE reads it from this
        // override, and the most-derived class's override is the runtime class's factory. Only a class that has
        // BASE's object interface has one (§16.1: "This use is not required").
        bool lifeCycle = cls.Symbol.InheritsStandardBase;
        // §9.3.6 match rule 3 d) 4./5. — "the class specified in the invocation" of a universal INVOKE is the class of the
        // object it runs on; each half names the other half's type, so the run-time relation can ask an ACTIVE-CLASS
        // formal's question of either (CobolObject.__FactoryClassType / __InstanceClassType; kb/Work PB1112).
        instExtras.Add($"protected override System.Type? __FactoryClassType => typeof({cls.Symbol.FactoryCsName});");
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
            // factory data (kb/Work PB1069). A factory with ANY superclass hides that superclass's accessor — a COBOL
            // class's, or the runtime BASE__FACTORY's, which declares the same singleton so that BASE's own factory is
            // reached as every other is (kb/Work PB2489).
            $"public {(cls.Symbol.Base is not null ? "new " : "")}static {cls.Symbol.FactoryCsName} "
                + $"{NamingConvention.FactoryInstanceField} => {RuntimeApi.FactoryObject(cls.Symbol.FactoryCsName)};",
            $"protected override System.Type? __InstanceClassType => typeof({cls.CsName});",
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
            // of one class folds the same state; the header RAISING census (EcState.PdRaisingObjectCsTypes) is not read
            // here (it serves GOBACK … RAISING LAST and the INVOKE pickup, which are emitted inside each method with
            // that method's own header).
            var savedPropagation = ecState.Propagation;
            ecState.Propagation = AutomaticPropagation.Of(roster.Any(m => m.AutomaticPropagationHere), inMethod: true);
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
    /// descriptions), keyed by each method's EXTERNALIZED name (<see cref="OoMethodSymbol.DispatchKey"/>), and a
    /// method that is absent or does not MATCH falls out of the switch into <c>base.__CobolInvoke</c> — the chain IS
    /// §9.3.6 resolution order, and the CobolObject root raises EC-OO-METHOD (§9.3.6 6); GR7 b)). Each case asks the
    /// run-time relations (<see cref="ActivationRelations"/>, kb/Work PB480) of the caller's
    /// <see cref="ActivationDescription"/>s and the method's, which are <c>static readonly</c> fields of the type
    /// (<see cref="EmitCobolInvokeCase"/>). Box forms are CANONICAL BY DESCRIPTION (D-U6a — never by either side's
    /// StoreAsImage): a string-carried item → string; a zoned numeric → its display IMAGE string (bridged by the
    /// FormatDisplay/StoreDisplay overload pair); another numeric → the native value; an object reference → the
    /// reference; a variable-length group → its carrier; a strong group with no image → its leaf vector. A type
    /// declaring zero non-override methods emits no override.</summary>
    private void EmitCobolInvoke(string cobolName, IReadOnlyList<OoMethodSymbol> roster, CodeWriter w)
    {
        var cases = roster.Where(m => m.OverrideOf is null).ToList();
        if (cases.Count == 0) return;
        w.Line();
        // The method side's descriptions, built once per type (ActivationDescriptions — the ONE builder the caller's
        // side is built by too).
        for (int c = 0; c < cases.Count; c++)
        {
            var b = cases[c].Binding!;
            if (b.Formals.Any(f => f.ByValue)) continue;   // never a match (EmitCobolInvokeCase), so never asked
            for (int i = 0; i < b.Formals.Count; i++)
                if (ActivationDescriptions.OfFormal(b.Formals[i]) is { } fd)
                    w.Line($"private static readonly {nameof(ActivationDescription)} {CaseDescription(c, i)} = {RuntimeApi.ActivationDescriptionNew(fd)};");
            if (b.Returning is { } r && ActivationDescriptions.Of(r) is { } rd)
                w.Line($"private static readonly {nameof(ActivationDescription)} {CaseDescription(c, -1)} = {RuntimeApi.ActivationDescriptionNew(rd)};");
        }
        using (w.Block("public override void __CobolInvoke(string __name, CobolInvokeArg[] __a, CobolInvokeArg? __ret)"))
        {
            using (w.Block("switch (__name)"))
            {
                for (int c = 0; c < cases.Count; c++)
                    // ⛔ THE CASE LABEL IS THE METHOD'S DISPATCH KEY (kb/Work PB1405): §8.3.2.2 1) maps a universal INVOKE's
                    // method-name "to the externalized name of the method to be invoked", which is the roster key the TYPED
                    // path resolves by (PB303) — never the declared METHOD-ID word, which an AS phrase replaces.
                    using (w.Block($"case {Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(cases[c].DispatchKey, quote: true)}:"))
                        EmitCobolInvokeCase(cobolName, cases[c], c, w);
            }
            // §9.3.6 2)/4): a class that declares no method of this name, or whose method does not MATCH the invocation, hands
            // the search to the class it inherits from; the CobolObject root is step 6) — EC-OO-METHOD (§14.9.23.4 GR7 b)).
            w.Line("base.__CobolInvoke(__name, __a, __ret);");
        }
    }

    /// <summary>The name of the <c>static readonly</c> description of case <paramref name="c"/>'s formal
    /// <paramref name="i"/> (−1: its returning item).</summary>
    private static string CaseDescription(int c, int i) => i < 0 ? $"__ad{c}_r" : $"__ad{c}_{i}";

    /// <summary>One method's arm of the universal dispatch switch, in the order §14.9.23.4 GR7 prescribes (kb/Work PB1500,
    /// PB480).
    ///
    /// <para><b>GR7 b) — does this method MATCH?</b> §9.3.6's match rules are conditions of METHOD RESOLUTION: rule 1 (an
    /// equal argument count, trailing OPTIONAL formals counting as equal; RETURNING present on both sides or neither),
    /// rule 3 for every argument (all BY REFERENCE, §14.9.23.3 SR6 — <see cref="ActivationRelations.Matches"/>: an
    /// OMITTED one needs an OPTIONAL formal, an object reference the 3 d) description or, for an ACTIVE-CLASS formal, an
    /// object of the class the method is invoked on; any other the same class, category and 3 e) clauses), and rules
    /// 6)/7) for the RETURNING items (<see cref="ActivationRelations.ReturningMatches"/>). A method that does not match is
    /// not bound: the arm <c>break</c>s out of the switch into the inherited class's search, and when no class matches,
    /// §9.3.6 6) sets EC-OO-METHOD.</para>
    ///
    /// <para><b>GR7 c) — the bound method's conformance.</b> "neither a formal parameter nor the returning item in the
    /// invoked method shall be described with the ANY LENGTH clause, and the rules for conformance specified in 14.8.2 …
    /// and 14.8.3 … apply" — <see cref="ActivationRelations.ParameterViolation"/> and
    /// <see cref="ActivationRelations.ReturningViolation"/>, raised as EC-OO-UNIVERSAL through
    /// <see cref="OoUnivStop"/>.</para>
    ///
    /// <para>A GROUP formal whose argument has another shape the relations admitted — a smaller formal (§14.8.2.2 rule
    /// 1's prefix) or a fixed / variable-length pair (§8.5.1.12) — is carried by <see cref="UniversalGroupCarrier"/>,
    /// which the RETURNING delivery uses too.</para></summary>
    private void EmitCobolInvokeCase(string cobolName, OoMethodSymbol m, int c, CodeWriter w)
    {
        var formalsList = m.Binding!.Formals;
        var returning = m.Binding!.Returning;
        int formals = formalsList.Count;
        // §9.3.6 match rule 3 a): every argument of a universal invocation is BY REFERENCE (§14.9.23.3 SR6), and "for each
        // parameter of the invocation that is passed by reference there shall be a corresponding parameter in the invoked
        // method that is specified with the BY REFERENCE phrase" — a method with a BY VALUE formal never matches one, so
        // the search continues upward and ends in EC-OO-METHOD (§9.3.6 resolution step 6; kb/Work PB1051).
        if (formalsList.Any(f => f.ByValue))
        {
            w.Line("break;   // a BY VALUE formal: not a §9.3.6 match for a universal (all BY REFERENCE) invocation — the search continues upward");
            return;
        }
        // A formal or returning item with no universal crossing form (a Tier-C group) can match no argument: the binder
        // refuses every such argument (COBOLNET0866), so no description of the caller's can be its.
        if (formalsList.Any(f => ActivationDescriptions.OfFormal(f) is null)
            || returning is not null && ActivationDescriptions.Of(returning) is null)
        {
            w.Line("break;   // a formal or returning item with no universal crossing form matches no invocation");
            return;
        }
        // §14.8.2.1 and §9.3.6 match rule 1: fewer arguments than formals is an EQUAL number when every formal to the
        // right of the last argument is OPTIONAL — so the least admissible count is one past the last NON-optional
        // formal (kb/Work PB757).
        int minArgs = formalsList.FindLastIndex(f => !f.Optional) + 1;
        var noMatch = new List<string>
        {
            minArgs == formals ? $"__a.Length != {formals}" : $"__a.Length < {minArgs} || __a.Length > {formals}",
            returning is null ? "__ret is not null" : "__ret is null",
        };
        // A position past the supplied arguments is a trailing omission (§14.9.23.4 GR9); the arity term above has
        // already proved every such formal OPTIONAL.
        string Present(int i) => i < minArgs ? "" : $"__a.Length > {i} && ";
        for (int i = 0; i < formals; i++)
            noMatch.Add($"{Present(i)}!{nameof(ActivationRelations)}.{nameof(ActivationRelations.Matches)}(__a[{i}].Description, "
                + $"__a[{i}].Value, {CaseDescription(c, i)}, this)");
        if (returning is not null)
            noMatch.Add($"!{nameof(ActivationRelations)}.{nameof(ActivationRelations.ReturningMatches)}(__ret!.Description, "
                + $"{CaseDescription(c, -1)})");
        w.Line($"if ({string.Join(" || ", noMatch.Select(t => $"({t})"))}) break;   // not a §9.3.6 match — the search continues upward");

        // GR7 c): the bound method's conformance — §14.8.2 per argument, §14.8.3 for the returning item.
        var violations = Enumerable.Range(0, formals)
            .Select(i => $"({(i < minArgs ? "" : $"__a.Length <= {i} ? null : ")}{nameof(ActivationRelations)}."
                + $"{nameof(ActivationRelations.ParameterViolation)}(__a[{i}].Description, {CaseDescription(c, i)}))")
            .Concat(returning is null ? [] : [$"{nameof(ActivationRelations)}.{nameof(ActivationRelations.ReturningViolation)}("
                + $"__ret!.Description, {CaseDescription(c, -1)})"])
            .ToList();
        if (violations.Count > 0)
        {
            w.Line($"string? __viol = {string.Join(" ?? ", violations)};");
            w.Line(OoUnivStop(m, "__viol is not null",
                $"$\"INVOKE '{cobolName}' '{m.Name}': {{__viol}} (ISO §14.9.23.4 GR7 c))\""));
        }
        // An ANY LENGTH formal or returning item is a violation whatever the arguments (GR7 c) bans the DESCRIPTION) —
        // including an OPTIONAL one whose argument is OMITTED or trailing-omitted, which no relation is asked about — so
        // the bound method's stop is unconditional here: no activation of the method is emitted (its ANY LENGTH
        // signature takes lengths a box cannot supply).
        if (formalsList.Any(f => f.Item.IsAnyLength) || returning is { IsAnyLength: true })
        {
            w.Line(OoUnivThrow(m, Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(
                $"INVOKE '{cobolName}' '{m.Name}': a formal parameter or the returning item is described with the ANY "
                + "LENGTH clause, which a method invoked through a universal object reference shall not have "
                + "(ISO §14.9.23.4 GR7 c))", quote: true)));
            return;
        }
        // ⛔ EACH FORMAL'S CARRIER IS A VIEW OVER ITS ARGUMENT'S BOX, AND THE BOX IS THE ARGUMENT'S STORAGE (ISO §14.2.3
        // GR8; kb/Work PB2087): every argument of a universal invocation is BY REFERENCE (§14.9.23.3 SR6), the caller's
        // box reads and writes the argument itself (CobolInvokeArg's live accessors), and this view converts between
        // the box's canonical form and the formal's crossing form on every access — so a store through the formal is a
        // store to the argument at once, and an area formal is laid over the argument's own cell (__a[i].Area).
        for (int i = 0; i < formals; i++)
        {
            var fi = formalsList[i].Item;
            w.Line($"bool __o{i} = {(i < minArgs ? "" : $"__a.Length <= {i} || ")}__a[{i}].Omitted;   // §14.9.23.4 GR9");
            string unbox = OoUnivGroupCarried(fi) is { } g
                ? g ? $"{nameof(UniversalGroupCarrier)}.{nameof(UniversalGroupCarrier.VariableCarrier)}(__a[{i}].Value, __a[{i}].Description, {CaseDescription(c, i)})"
                    : $"{nameof(UniversalGroupCarrier)}.{nameof(UniversalGroupCarrier.FixedImage)}(__a[{i}].Value, __a[{i}].Description, {CaseDescription(c, i)})"
                : OoUnivUnbox(fi, $"__a[{i}].Value");
            string rebox = OoUnivGroupCarried(fi) is { } rg
                ? rg ? $"{nameof(UniversalGroupCarrier)}.{nameof(UniversalGroupCarrier.WriteBackVariable)}(__a[{i}].Value, __a[{i}].Description, {CaseDescription(c, i)}, __v)"
                    : $"{nameof(UniversalGroupCarrier)}.{nameof(UniversalGroupCarrier.WriteBackFixed)}(__a[{i}].Value, __a[{i}].Description, {CaseDescription(c, i)}, __v)"
                : OoUnivRebox(fi, "__v");
            string crossing = OoFormalCrossingType(fi);
            w.Line($"var __p{i} = __o{i} ? ManagedPointer<{crossing}>.Cell(default!) : ManagedPointer<{crossing}>.OverField(() => {unbox}, __v => __a[{i}].Value = {rebox});");
        }
        string argList = string.Join(", ", Enumerable.Range(0, formals).Select(i => OoArgPair($"__p{i}", $"__o{i} ? null : __a[{i}].Area", $"__o{i}")));
        w.Line(returning is null ? $"this.{m.CsName}({argList});" : $"var __rv = this.{m.CsName}({argList});");
        if (returning is not null)
            w.Line(OoUnivGroupCarried(returning) is not null
                ? $"__ret!.Value = {nameof(UniversalGroupCarrier)}.{nameof(UniversalGroupCarrier.Deliver)}({OoUnivRebox(returning, "__rv")}, "
                  + $"__ret!.Description, {CaseDescription(c, -1)});   // GR8 delivery in the receiving item's form"
                : $"__ret!.Value = {OoUnivRebox(returning, "__rv")};");
        w.Line("return;");
    }

    /// <summary>Whether a group formal or returning item crosses through <see cref="UniversalGroupCarrier"/> — true for
    /// a VARIABLE-length group, false for a FIXED-length alphanumeric group (the two shapes §14.8.2.2 / §8.5.1.12 pair
    /// with an argument of another shape), null for every other item, whose argument the relations admit only in its own
    /// shape and form.</summary>
    private static bool? OoUnivGroupCarried(DataItem item) =>
        item.IsGroup && !item.IsAsIfElementary && !StrongTypeModel.IsStrongGroup(item)
            ? OoVarGroupCarried(item) ? true : item.IsImageCapable ? false : null
            : null;

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
        // A strong group's leaf vector (kb/Work PB1116): the relations admit only an argument of the same type, which
        // the caller boxes as the same vector (OoUnivCallerRead).
        : OoClassTable.LeafCarried(item) ? $"(object?[]){box}!"
        : OoUnivNativeBoxOverImage(item) ? NumericRenderer.ImageOfCarrier($"({item.Pic!.ClrType}){box}!", item)
        : OoStringCarried(item) ? $"(string){box}!"
        : OoUnivImageBridged(item) ? RuntimeApi.NumStoreDisplay($"(string){box}!", item.ProfileName, $"({item.ElementType})0")
        : OoIsActiveClassFormal(item) ? $"(CobolObject?){box}"   // the universal crossing (OoFormalCrossingType)
        : item.Pic is { Category: PicCategory.ObjectReference } p ? $"({p.ClrType}){box}"
        : $"({item.ElementType}){box}!";

    /// <summary>The callee-side re-box: a local in the formal's crossing form → the canonical box form.</summary>
    private static string OoUnivRebox(DataItem item, string local) =>
        OoUnivImageBridged(item) ? RuntimeApi.NumFormatDisplay(local, item.ProfileName)
        : OoUnivNativeBoxOverImage(item) ? $"(object?){NumericRenderer.CarrierOfImage(local, item)}"
        : $"(object?){local}";

    /// <summary>Caller-side universal dispatch (D-U6): box every argument per ITS OWN descriptor's canonical
    /// form, dispatch through the GR5 null guard with the bind-normalized literal or the runtime-normalized
    /// identifier-2 value, and deliver RETURNING (GR8) through the receiver's own storage form. Every identifier
    /// argument's box is LIVE over the argument (SR6 — all BY REFERENCE; §14.2.3 GR8, kb/Work PB2087): the callee's
    /// formal is a view over it and its area is the argument's cell, so there is no copy-out.</summary>
    public void EmitUniversalInvoke(BoundInvokeUniversal statement)
    {
        var w = Ctx.Writer;
        var u = IdentifyOperands(statement);
        int id = Ctx.Names.NextStoreTmp();
        // A spelled OMITTED argument boxes as the omitted sentinel; a forwarded formal (§8.8.4.8.4 GR1c) boxes its
        // presence and is read only when present (§14.9.23.4 GR10's "except as an argument") — kb/Work PB757.
        string?[] fwd = u.Args.Select(a => a.Source is { } s && callState.WholeFormalProbe(s) is { } pr
            ? CallEmitter.OmittedTest(pr) : null).ToArray();
        string boxes = string.Join(", ", u.Args.Select((a, i) => a.Address is { } ao
            // An ADDRESS-IDENTIFIER (kb/Work PB1137) boxes its pointer VALUE — the ONE address-operand renderer the
            // typed path uses — under its class-pointer descriptor; SR19 makes it sending, so nothing copies back.
            ? $"new CobolInvokeArg({RuntimeApi.ActivationDescriptionNew(a.Description)}, (object?){U.Ptr.AddressOperandText(ao)})"
            : a.Source is not { } src
            ? RuntimeApi.ObjOmittedArgument
            // ⛔ THE BOX IS LIVE OVER THE ARGUMENT (§14.2.3 GR8; kb/Work PB2087): its accessors read and write the
            // argument's own storage, so the method's formal (a view over the box) is the argument, and there is no
            // copy-out. A forwarded formal that is omitted boxes as omitted and is never read (GR1c / GR10).
            : (fwd[i] is { } t ? $"{t} ? new CobolInvokeArg({RuntimeApi.ActivationDescriptionNew(a.Description)}, null, true) : " : "")
              // A reference-modified argument's length is its EVALUATED length (§8.4.3.3.4 GR5 c)): the runtime measures
              // the slice the box reads, whatever the modifier's form (a literal one measures to its static length).
              + (src is RefModPlace
                ? RuntimeApi.ObjReferenceModifiedArgument(RuntimeApi.ActivationDescriptionNew(a.Description),
                    $"(object?){OoUnivCallerRead(src)}", OoUnivCallerWrite(src, "__v"), U.Call.ArgumentArea(src))
                : RuntimeApi.ObjLiveArgument(RuntimeApi.ActivationDescriptionNew(a.Description),
                    $"(object?){OoUnivCallerRead(src)}", OoUnivCallerWrite(src, "__v"), U.Call.ArgumentArea(src)))));
        w.Line($"var __ua{id} = new CobolInvokeArg[] {{ {boxes} }};");
        w.Line(u.Returning is not null
            ? $"var __ur{id} = new CobolInvokeArg({RuntimeApi.ActivationDescriptionNew(u.ReturningDescription!)});"
            : $"CobolInvokeArg? __ur{id} = null;");
        string selector = u.MethodLiteral is { } lit
            ? Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(lit, quote: true)
            // identifier-2's CHARACTER VALUE through the one operand-image reader (§14.9.23.4 GR2 a): "the content
            // of the data item referenced by identifier-2"), so an alphanumeric or national GROUP sends its
            // positions as an elementary item does — PlaceRenderer.Read handed the group's struct to a string
            // parameter (CS1503; kb/Work PB1136).
            : RuntimeApi.ObjNormalizeMethodName(OperandText.FieldImage(u.MethodSource!));
        w.Line($"{RuntimeApi.ObjRequireNonNull(PlaceRenderer.Read(u.Receiver))}.__CobolInvoke({selector}, __ua{id}, __ur{id});");
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

    /// <summary>An object-view's pre-op (ISO §8.4.3.5.4; kb/Work PB1425): store identifier-1's reference into the
    /// view's temporary under the AS phrase's description. GR7's UNIVERSAL view checks nothing — every object is a
    /// <c>CobolObject</c>, so the reference converts up; every other description takes the run-time conformance
    /// check (GR2–GR6, EC-OO-CONFORMANCE).</summary>
    public void EmitObjectView(BoundObjectView v)
    {
        string source = v.Source is { } s ? PlaceRenderer.Read(s) : "this";
        string viewed = v.View.IsUniversal
            ? $"({v.Target.Item.Pic!.ClrType})({source})"
            : RuntimeApi.ObjObjectView(v.View.ClrTypeName, source, exactClass: v.View.Only, v.Written);
        Ctx.Writer.Line(PlaceRenderer.Write(v.Target, viewed) + "   // object-view (ISO §8.4.3.5.4)");
    }

    /// <summary>SELF as an identifier operand (ISO §8.4.3.8.4 GR1; kb/Work PB1425): store the reference to the object the
    /// method was invoked on into the ACTIVE-CLASS temporary. The object is of that class by GR1, so nothing is
    /// checked.</summary>
    public void EmitSelfReference(BoundSelfReference s) =>
        Ctx.Writer.Line(PlaceRenderer.Write(s.Target, $"({s.Target.Item.Pic!.ClrType})(this)") + "   // SELF (ISO §8.4.3.8.4 GR1)");

    /// <summary>The spans of a FIXED-length group argument's tables that correspond to a variable-length group
    /// formal's dynamic-capacity tables (kb/Work PB965) — null when the argument is itself variable-length (it
    /// composes its own carrier) or is not a group place the correspondence can be stated for. A redefinition or cell
    /// VIEW of a group is that group's storage and has its layout like any other (a group passed BY REFERENCE is claimed
    /// onto a cell, kb/Work PB2087/PB2089 — the INVOKE twin of <c>CallEmitter.BoundaryAtoms</c>); a reference-modified
    /// operand denotes no item, so it has none.</summary>
    private static int[]? FixedArgumentSpans(Place arg, DataItem formal) =>
        !CallEmitter.CallPlaceIsVarGroup(arg)
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

    /// <summary>The §8.5.1.12 atoms of the variable-length group place <paramref name="place"/> and of the
    /// variable-length group <paramref name="other"/> it crosses to or from, when the two are of DIFFERENT shapes —
    /// compatible (bind proved it) but not carried alike, so the carrier is rebuilt (<c>CobolVarGroup.Reshape</c>) and a
    /// store overlaid back (<c>CobolVarGroup.Overlay</c>; kb/Work PB480). Null when the shapes are the same (the carrier
    /// crosses as it is) or either side is not a variable-length group.</summary>
    private static (GroupAtom[] PlaceShape, GroupAtom[] OtherShape)? VarGroupShapes(Place place, DataItem other) =>
        CallEmitter.CallPlaceIsVarGroup(place) && place.DenotedItem is { } item
        && VariableLengthCompatibility.IsVariableLength(other)
        && VariableLengthCompatibility.GroupAtoms(item) is { } placeShape
        && VariableLengthCompatibility.GroupAtoms(other) is { } otherShape
        && !GroupCompatibility.SameShape(placeShape, otherShape)
            ? (placeShape, otherShape)
            : null;

    /// <summary>⛔ THE CALLER'S BOX OF ONE UNIVERSAL ARGUMENT — D10's canonical box, in the ARGUMENT's own form (the
    /// callee converts a group box it admits in another shape, <see cref="UniversalGroupCarrier"/>): a string-carried
    /// item → string, a variable-length group → its carrier, a strong group with no image → its leaf vector (kb/Work
    /// PB1116 — the vector its typed crossing uses), an object reference → the reference, a numeric → per
    /// <see cref="OoUnivImageBridged"/> / <see cref="OoUnivNativeBoxOverImage"/>. Any other GROUP is string-carried, so
    /// its box is its character IMAGE — read through <see cref="CallEmitter.CallStringRead"/>, the ONE boundary reader
    /// the CALL and the typed INVOKE lanes use (the full image of an alphanumeric group, the elementary alphabet of a bit
    /// / national group, kb/Work PB1166; kb/Work PB1781). <see cref="OoUnivCallerWrite"/> carries the twin of every arm
    /// here; <c>UniversalCrossingShapeDriftTests</c> holds the pair together shape by shape.</summary>
    private static string OoUnivCallerRead(Place p) =>
        p is RefModPlace ? PlaceRenderer.Read(p)
        : CallEmitter.CallPlaceIsVarGroup(p) ? PlaceRenderer.VarGroupBoundaryImage(p, "INVOKE argument")
        : OoClassTable.LeafCarried(p.Item) ? PlaceRenderer.GroupLeaves(p)
        : p.Item.IsGroup ? CallEmitter.CallStringRead(p)
        : OoUnivImageBridged(p.Item) ? PlaceRenderer.Read(new NumericImagePlace(p))
        : OoUnivNativeBoxOverImage(p.Item) ? $"(object?){NumericRenderer.CarrierOfImage(PlaceRenderer.Read(p), p.Item)}"   // kb/Work PB187
        : PlaceRenderer.Read(p);

    /// <summary>The caller's copy-out / RETURNING delivery of a universal box — the twin of <see cref="OoUnivCallerRead"/>,
    /// arm for arm. The box comes back in the argument's own form (a smaller formal group's write-back is spliced over the
    /// argument's image by the callee, §14.8.2.2 rule 1), so a fixed group takes its whole image back through
    /// <see cref="CallEmitter.CallStringWrite"/>, the boundary writer that distributes the FULL image (the elementary
    /// alphabet for a bit / national group).</summary>
    private static string OoUnivCallerWrite(Place p, string box) =>
        p is RefModPlace ? PlaceRenderer.Write(p, $"(string){box}!")
        : CallEmitter.CallPlaceIsVarGroup(p)
            ? PlaceRenderer.WriteVarGroupImage(p, $"({RuntimeApi.VarGroupType}){box}!", "INVOKE copy-out into")
        : OoClassTable.LeafCarried(p.Item) ? PlaceRenderer.WriteGroupLeaves(p, $"(object?[]){box}!")
        : p.Item.IsGroup ? CallEmitter.CallStringWrite(p, $"(string){box}!")
        : OoUnivNativeBoxOverImage(p.Item) ? PlaceRenderer.Write(p, NumericRenderer.ImageOfCarrier($"({p.Item.Pic!.ClrType}){box}!", p.Item))   // kb/Work PB187
        : OoStringCarried(p.Item) ? PlaceRenderer.Write(p, $"(string){box}!")
        : OoUnivImageBridged(p.Item) ? PlaceRenderer.Write(new NumericImagePlace(p), $"(string){box}!")
        : p.Item.Pic is { Category: PicCategory.ObjectReference } pic
            ? PlaceRenderer.Write(p, RuntimeApi.ObjNarrowUniversal(pic.ClrType.TrimEnd('?'), box,
                $"INVOKE (universal) delivery into '{p.Item.CobolName}'"))
        : PlaceRenderer.Write(p, $"({p.Item.ElementType}){box}!");

    /// <summary>
    /// Emit one METHOD-ID as a real typed C# method (slice 2 — deep-dive D3/D6/D7/D8): every formal arrives as its
    /// argument's carrier, area and presence flag (<see cref="OoSignatureOf"/>) and OCCUPIES the argument's storage
    /// (§14.2.3 GR8; kb/Work PB2087) — a resident formal through its carrier, an area formal laid over the argument's
    /// cell; the other LINKAGE/LOCAL-STORAGE roots are locals (LOCAL-STORAGE re-initializes each activation, §8.6.4), the
    /// method's paragraph slice is a LOCAL-FUNCTION dispatcher (<c>__MDispatch</c> — it captures the locals by
    /// reference, so PERFORM recursion and the implicitly-RECURSIVE method rule, §12032/:12032, are structural), and
    /// the RETURNING local is the C# return value (§14.9.23.4 GR8). D7: <c>virtual</c> by default. The exit-bounded
    /// slice is the trap-#4 guard.
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
                // An ACTIVE-CLASS property's setter formal crosses as the universal type (OoFormalCrossingType); the
                // subject is the containing class's reference, so the store narrows it (GR22 e)).
                string value = OoIsActiveClassFormal(m.Binding!.Formals[0].Item) ? $"({subject.ElementType}){param}.Value" : $"{param}.Value";
                string store = subjPlace is null ? $"{subject.CsName} = {value};" : PlaceRenderer.Write(subjPlace, value);
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
            string __mLit = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(m.Name.ToUpperInvariant(), quote: true);
            string __cLit = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(m.Owner.Name.ToUpperInvariant(), quote: true);
            // §14.9.23.4 GR7 b) (kb/Work PB2659): "If the method is not found or the resources necessary to execute the
            // method are not available, the EC-OO-METHOD exception condition is set to exist, the method invocation is not
            // successful" — the resource is the stack the activation needs (ActivationStack), asked first, as GR7 b)
            // precedes d). Here, in the body, for the reason the frame push below is: a method is reached by a typed direct
            // call, by __CobolInvoke and by an inline invocation, and this is the one place all three pass through.
            w.Line(RuntimeApi.MethodActivationResources(__mLit, __cLit) + ";   // §14.9.23.4 GR7 b) — the activation's stack");
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
                // ⛔ A FORMAL OCCUPIES ITS ARGUMENT'S STORAGE (ISO §14.2.3 GR8: "If the argument is passed by reference, the
                // activated runtime element operates as if the formal parameter occupies the same storage area as the
                // argument"; kb/Work PB2087, the method arm). A RESIDENT formal reads and writes through the argument's
                // carrier on every access (its C# path IS `{CarrierLocal}.Value`); an AREA formal's description is laid
                // over the argument's own cell, so a second formal passed the same argument, the activating element's own
                // references and a callback all see every store at once. Only an argument with no cell area (BY CONTENT,
                // a BY VALUE formal, storage that is not a cell) gives an area formal a fresh cell, filled here from the
                // carrier and, BY REFERENCE, stored back at return.
                if (m.Binding!.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root)) is { } bf)
                {
                    if (bf.CarrierResident)
                    {
                        // An ACTIVE-CLASS formal crosses as the universal type (OoFormalCrossingType) and is viewed as the
                        // containing class — GR22 e) guarantees the invoker's class is that class or a subclass.
                        var (rType, rInit) = fields.RootDecl(root);
                        string adopt = OoIsActiveClassFormal(root)
                            ? $"ManagedPointer<{rType}>.OverField(() => ({rType}){bf.ParamName}.Value, __v => {bf.ParamName}.Value = __v)"
                            : bf.ParamName;
                        // An OMITTED formal (§14.9.23.4 GR9) has no argument: its storage starts at its initial state,
                        // which an unchecked reference reads (GR10 leaves it undefined — the documented leniency).
                        w.Line($"ManagedPointer<{rType}> {bf.CarrierLocal} = {bf.OmittedFlag} ? ManagedPointer<{rType}>.Cell({rInit}) : {adopt};   "
                            + $"// LINKAGE formal {root.CobolName} — the argument's carrier, per-access (§14.2.3 GR8)");
                        continue;
                    }
                    if (bf.IsArea)
                    {
                        var (shape, fresh) = fields.AreaFormalShape(root);
                        w.Line($"{bf.CarrierLocal} = {RuntimeApi.ArgAdaptAreaOf(bf.ByValue ? "null" : bf.AreaParam, $"!{bf.OmittedFlag}", root.Class!.Width, shape, fresh)};   "
                            + $"// LINKAGE formal {root.CobolName} — laid over the argument's area (§14.2.3 GR8)");
                        w.Line($"if (!{bf.OmittedFlag} && !ReferenceEquals({bf.CarrierLocal}, {bf.AreaParam})) {{ {MethodCellFormalStore(root, $"{bf.ParamName}.Value")} }}   "
                            + "// no area to share: the fresh cell takes the argument's value (§14.2.3 GR9 / GR10)");
                        continue;
                    }
                }
                // A method Tier-B REDEFINES canonical's storage is its string backing, not the root struct (M2-OO-1h
                // step 3) — emit that as the local; a LINKAGE formal seeds it from the caller's image, width-normalized
                // to the class width (a wider redefiner needs the full backing — review D), else from the initializer.
                if (fields.MethodRedefinesBackingDecl(root) is { } bkl)
                {
                    var formalB = m.Binding!.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root));
                    w.Line($"string {bkl.Name} = {(formalB is null ? bkl.Init : $"{formalB.OmittedFlag} ? {bkl.Init} : {RuntimeApi.StrStore($"{formalB.ParamName}.Value", $"{root.Class!.Width}")}")};   "
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
                        w.Line($"if (!{cf.OmittedFlag}) {{ {MethodCellFormalStore(root, $"{cf.ParamName}.Value")} }}   "
                            + $"// LINKAGE formal {root.CobolName} (BY REFERENCE copy-in to its addressable cell, §14.2.3 GR8)");
                    continue;
                }
                if (root.Class is { Tier: RedefinesTier.StringCanonical }) continue;
                var (type, init) = fields.RootDecl(root);
                var formal = m.Binding!.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, root));
                if (formal is null && root is { IsAnyLength: true, Pic: { } anyPic })
                    // An ANY LENGTH RETURNING item starts as n repetitions of its picture symbol, n being the activator's
                    // receiver length (§13.18.2.4 GR1 b)) — OoSignatureOf's trailing parameter.
                    w.Line($"{type} {root.CsName} = new string({RuntimeApi.AnyLengthFill(anyPic)}, {RetLenParam});   "
                        + $"// LINKAGE {root.CobolName} (ANY LENGTH RETURNING — n is the activator's, §13.18.2.4 GR1 b))");
                else if (formal is null)
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
                        ? PlaceRenderer.WriteVarGroupImage(MethodRootPlace(root), $"{formal.ParamName}.Value",
                            "OO method LINKAGE formal copy-in of", formalStorage: true)
                        // A bit / national group receives its ELEMENTARY value through the CALL boundary's ONE
                        // write (kb/Work PB1166 — the twin of MethodBoundaryValue's arm).
                        : root.IsAsIfElementary ? CallEmitter.CallStringWrite(MethodRootPlace(root), $"{formal.ParamName}.Value")
                        // A strong group with no character image is rebuilt from its leaf vector (kb/Work PB1116).
                        : OoClassTable.LeafCarried(root) ? PlaceRenderer.WriteGroupLeaves(MethodRootPlace(root), $"{formal.ParamName}.Value")
                        : PlaceRenderer.WriteFullGroupImage(MethodRootPlace(root), $"{formal.ParamName}.Value",
                            "OO method LINKAGE formal copy-in")) + " }");
                }
                else
                    // An ACTIVE-CLASS formal crosses as the universal type (OoFormalCrossingType) and is viewed as the
                    // containing class here — GR22 e) guarantees the invoker's class is that class or a subclass.
                    w.Line($"{type} {root.CsName} = {formal.OmittedFlag} ? {init} : "
                        + $"{(OoIsActiveClassFormal(root) ? $"({type})" : "")}{formal.ParamName}.Value;   "
                        + $"// LINKAGE formal {root.CobolName} (BY REFERENCE copy-in; omitted → initial state)");
            }
            // ⛔ THE METHOD'S OWN OPTIONS AND LOCAL-STORAGE ROOTS GOVERN ITS LOCAL-STORAGE INITIAL STATE (kb/Work PB1215):
            // §13.6.4 GR2 sends LOCAL-STORAGE to §11.9.10, whose GR2 applies the OPTIONS INITIALIZE clause of the source
            // element — this method's own OPTIONS paragraph, else the class's (§11.9.4 GR1) — to "the storage allocated
            // for" the sections it names. The class half's data model carries the class-level clause only and no method
            // roots, so both are swapped in for these initializers and restored after.
            var savedOptions = Ctx.Data.Options;
            var savedActivationRoots = Ctx.ActivationLocalRoots;
            if (m.MethodOptions is { } methodOptions) Ctx.Data.Options = methodOptions;
            Ctx.ActivationLocalRoots = m.Binding!.LocalRoots;
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
            Ctx.Data.Options = savedOptions;   // an emission error abandons the whole compile, so no finally is owed
            Ctx.ActivationLocalRoots = savedActivationRoots;
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
            var savedRaisingTypes = ecState.PdRaisingObjectCsTypes;
            ecState.Propagation = AutomaticPropagation.Of(m.AutomaticPropagationHere, inMethod: true);
            ecState.PdRaisingObjectCsTypes = EcState.RaisingObjectCsTypes(m.Raising, ecState.OoClasses);   // §14.6.13.1.5 items 1 and 3 (kb/Work PB1121)
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
                // The method's GR4 a) USE BEFORE REPORTING selectors (kb/Work PB1044) — declared BEFORE the dispatch
                // local function whose GENERATE/TERMINATE statements capture the cached-delegate slot.
                U.ReportWriter.EmitMethodBeforeReportingSelectors(mDecls, w);
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
                dispatch.BeforeReportingSelectors.Clear();   // the selectors are this method's locals — no other unit's statement may name them
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
            ecState.PdRaisingObjectCsTypes = savedRaisingTypes;
            // BY REFERENCE copy-out (§14.2.3 GR8) / RETURNING (§14.9.23.4 GR8). A Tier-B REDEFINES canonical's
            // storage IS its string backing (a width-correct image), not the suppressed root struct — write that
            // back / return that, else the generated C# names an undeclared local (review A/emission).
            foreach (var f in m.Binding!.Formals)
            {
                // A BY VALUE formal is a detached copy: "stores must never reach the caller" (§14.2.3 GR10), so there is
                // no copy-out — the activator's slot is a temporary it discards (kb/Work PB1051). A RESIDENT formal has
                // nothing to copy: every store already went through the argument's carrier (kb/Work PB2087).
                if (f.ByValue || f.CarrierResident) continue;
                // An AREA formal laid over its argument's own cell has no copy; one that took a fresh cell stores it back.
                // No copy-out for an omitted formal: there is no argument, and the caller's slot is a placeholder.
                if (f.IsArea)
                    w.Line($"if (!{f.OmittedFlag} && !ReferenceEquals({f.CarrierLocal}, {f.AreaParam})) {f.ParamName}.Value = {MethodCellFormalLoad(f.Item)};   "
                        + "// BY REFERENCE store-back of a fresh area (§14.2.3 GR8)");
                else
                    w.Line($"if (!{f.OmittedFlag}) {f.ParamName}.Value = {MethodBoundaryValue(fields, f.Item, "OO method BY REFERENCE copy-out")};   "
                        + "// BY REFERENCE copy-out of a formal whose area no cell carries (see DataBinder.CellCanCarry)");
            }
            if (m.Binding!.Returning is { } r)
            {
                string src = MethodBoundaryValue(fields, r, "OO method RETURNING delivery");
                w.Line($"return {OoReturnConversion(m, r)}{src};   // the invocation result (§14.9.23.4 GR8)");
            }
            // Close the PB36 activation try. The finally must cover every exit — the RETURNING `return` above, a
            // GOBACK unwinding as MethodReturn, and an exception propagating to the invoker — or the stack leaks a
            // frame and every later MODULE-NAME reads one element too deep.
            w.Line("}");
            // This activation's cells end BEFORE the activator's are restored (§8.6.4 / §8.6.5; kb/Work PB1216): a pointer
            // taken into the method's LOCAL-STORAGE is not a valid address once the method returns.
            string ptrRestore = string.Concat(ptrSeeds.Select((p, i) => $"{(p.End is null ? "" : p.End + " ")}{p.Member} = __ptrSv{i}; "));
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
        string retType = OoReturnClrType(m);
        var parameters = m.Binding!.Formals.Select(f =>
            $"ManagedPointer<{OoFormalCrossingType(f.Item)}> {f.ParamName}, CellPointer? {f.AreaParam}, bool {f.OmittedFlag}").ToList();
        // ⛔ AN ANY LENGTH RETURNING ITEM'S LENGTH IS THE ACTIVATOR'S (kb/Work PB1167): §13.18.2.4 GR1 b) makes n "the
        // length of the corresponding … returning item of the activating runtime element", and a C# return value
        // carries no receiver length, so the activator passes it as one trailing parameter — only for a method whose
        // returning item needs it, so every other signature is unchanged.
        if (OoReturnsAnyLength(m)) parameters.Add($"int {RetLenParam}");
        return (retType, string.Join(", ", parameters));
    }

    /// <summary>⛔ THE C# RETURN TYPE OF A METHOD (kb/Work PB1499) — <see cref="OoCrossingType"/> of its RETURNING item,
    /// except for the one rule-5 pair C# cannot override covariantly. §9.3.8.2.3 rule 5 a) admits ANY object reference
    /// in interface-1 where interface-2's returning item is universal, and an OVERRIDE is such an interface-1 (§11.7.3
    /// SR9): a method whose RETURNING item is described with an INTERFACE-name overriding a method that crosses as the
    /// universal type has no C# spelling of its own — C# covariant returns need an implicit reference conversion to the
    /// overridden type, and an interface type has none to a class (CS0508). So that override keeps the overridden
    /// method's C# return type and converts at the <c>return</c> (<see cref="OoReturnConversion"/>); the question is
    /// asked of the overridden method's PROJECTED type, so a chain of such overrides stays on one type. The covariant
    /// adapters (IMPLEMENTS) take the same conversion.</summary>
    private static string OoReturnClrType(OoMethodSymbol m)
    {
        if (m.Binding!.Returning is not { } ret) return "void";
        if (m.OverrideOf is { Binding.Returning: not null } baseM
            && ret.Pic is { ObjectRef: { IsClrInterface: true } }
            && OoReturnClrType(baseM) is var baseClr && baseClr.TrimEnd('?') == ObjectRefDescriptor.UniversalClrName)
            return baseClr;   // the crossing type spells its nullability ("CobolObject?"); keep the overridden method's spelling
        return OoCrossingType(ret);
    }

    /// <summary>The C# conversion to the method's projected return type (<see cref="OoReturnClrType"/>) for a value of
    /// the RETURNING item's own crossing type — the explicit cast exactly when the two differ (an interface-typed
    /// value returned as the universal type, which is total for every emitted COBOL object), else nothing.</summary>
    private static string OoReturnConversion(OoMethodSymbol m, DataItem ret) =>
        OoReturnClrType(m) is var projected && projected != OoCrossingType(ret) ? $"({projected})" : "";

    /// <summary>The trailing parameter that carries the INVOKE receiver's length to an ANY LENGTH RETURNING item
    /// (<see cref="OoSignatureOf"/>; ISO §13.18.2.4 GR1 b)).</summary>
    private const string RetLenParam = "__retLen";

    /// <summary>True when the method's RETURNING item is described with ANY LENGTH — the one test that adds
    /// <see cref="RetLenParam"/> to the signature, the typed INVOKE, the covariant adapter and the seed.</summary>
    private static bool OoReturnsAnyLength(OoMethodSymbol m) => m.Binding!.Returning is { IsAnyLength: true };

    /// <summary>⛔ THE METHOD ABI'S ARGUMENT PAIR — one formal crosses as its typed <c>ref</c> value AND its
    /// omitted-presence flag (kb/Work PB757; COBOLNET_OO_DESIGN D6). ISO §14.9.23.4 GR9: "If an OMITTED phrase is
    /// specified or a trailing argument is omitted, the omitted-argument condition for that parameter shall be
    /// true in the invoked method" — a C# <c>ref T</c> has no omitted state, so the pair IS the state. Every
    /// caller renders its pair here (the typed INVOKE, the covariant adapter, the universal switch), so the
    /// signature <see cref="OoSignatureOf"/> builds and the argument lists cannot drift apart.</summary>
    private static string OoArgPair(string carrier, string area, string omitted) => $"{carrier}, {area}, {omitted}";

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
            // §11.6.3 SR5: a name inherited from SEVERAL interfaces is ONE method of this interface (the prototype the
            // others conform to). C# sees two base-interface members and every call through this interface is ambiguous
            // (CS0121), so the presented prototype is declared here, hiding both; a class's one public method still
            // implements all three (kb/Work PB1502).
            foreach (var presented in iface.PresentedInherited)
            {
                var (retType, sig) = OoSignatureOf(presented);
                w.Line($"new {retType} {presented.CsName}({sig});   // METHOD-ID {presented.Name} inherited from several interfaces (§11.6.3 SR5)");
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
        : OoClassTable.LeafCarried(item) ? "object?[]"   // the strong group's leaf vector (kb/Work PB1116)
        : OoStringCarried(item) ? "string"
        : item.ElementType;

    /// <summary>True for a method formal described <c>USAGE OBJECT REFERENCE [FACTORY OF] ACTIVE-CLASS</c>.</summary>
    private static bool OoIsActiveClassFormal(DataItem formal) =>
        formal.Pic is { Category: PicCategory.ObjectReference, ObjectRef: { IsActiveClass: true } };

    /// <summary>⛔ THE C# CROSSING TYPE OF A METHOD FORMAL (kb/Work PB1497 + PB1112's override leg). An ACTIVE-CLASS
    /// formal names "the same class as the object that was used to invoke the method" (§13.18.60.4 GR22 e)) — a
    /// property of the ACTIVATION, not of the description (§9.3.8.2.3 rule 2 d) compares only the phrase and the
    /// FACTORY presence) — so it crosses the method ABI as the universal object type, whatever class the method is
    /// written in: one wire type for the method, for every override of it (a <c>ref</c> parameter admits no
    /// subclass) and for the interface prototype that has no class at all. The body views the formal as its
    /// containing class through the checked cast of its copy-in (<see cref="EmitMethod"/>); the caller's temporary is
    /// the same universal type, narrowed back into the argument on copy-out. Every other formal crosses as
    /// <see cref="OoCrossingType"/>.</summary>
    private static string OoFormalCrossingType(DataItem formal) =>
        OoIsActiveClassFormal(formal) ? "CobolObject?" : OoCrossingType(formal);


    /// <summary>⛔ ITEM IDENTIFICATION, ONCE, AT THE BEGINNING (ISO §14.9.23.4 GR7 a: "Arithmetic-expression-1,
    /// boolean-expression-1, identifier-1, identifier-2, identifier-3, and identifier-5 are evaluated and item
    /// identification is done for identifier-4 at the beginning of the execution of the INVOKE statement"; §14.6.4 7;
    /// kb/Work PB1123): the receiver, every BY REFERENCE / BY CONTENT identifier argument and the RETURNING
    /// identifier have their run-time address fragments frozen into locals by <see cref="PlaceIdentification"/>
    /// before the call, so a method that changes the subscript of identifier-4 through a BY REFERENCE argument
    /// (<c>INVOKE O "M" USING I RETURNING T(I)</c>) still delivers into the occurrence the statement identified.
    /// The CALL lane owns the same rule (§14.9.4.4 GR3a) through the same mechanism.</summary>
    private BoundInvoke IdentifyOperands(BoundInvoke inv)
    {
        var hoist = PlaceIdentification.Hoister(Ctx);
        var receiver = inv.Receiver is null ? null : PlaceIdentification.Freeze(inv.Receiver, hoist);
        var args = inv.Args?.Select(a => a.Source is { } src ? a with { Source = PlaceIdentification.Freeze(src, hoist) } : a).ToList();
        var returning = inv.Returning is null ? null : PlaceIdentification.Freeze(inv.Returning, hoist);
        return inv with { Receiver = receiver, Args = args, Returning = returning };
    }

    /// <summary>The universal-receiver INVOKE's item identification — the same GR7 a) rule, the same mechanism.</summary>
    private BoundInvokeUniversal IdentifyOperands(BoundInvokeUniversal u)
    {
        var hoist = PlaceIdentification.Hoister(Ctx);
        var receiver = PlaceIdentification.Freeze(u.Receiver, hoist);
        var methodSource = u.MethodSource is null ? null : PlaceIdentification.Freeze(u.MethodSource, hoist);
        var args = u.Args.Select(a => a.Source is { } src ? a with { Source = PlaceIdentification.Freeze(src, hoist) } : a).ToList();
        var returning = u.Returning is null ? null : PlaceIdentification.Freeze(u.Returning, hoist);
        return u with { Receiver = receiver, MethodSource = methodSource, Args = args, Returning = returning };
    }

    /// <summary>Emit one bound INVOKE (deep-dive D5/D6 — the binder already resolved the call form and
    /// validated §14.8.2 strict conformance; this renders the type-preserving marshaling).</summary>
    public void EmitInvoke(BoundInvoke statement)
    {
        var w = Ctx.Writer;
        var inv = IdentifyOperands(statement);
        switch (inv.Form)
        {
            case InvokeForm.New:
            case InvokeForm.NewSelf:
            case InvokeForm.NewSuper:
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
            factory = "this";   // SELF in a factory method: the RUNTIME factory, virtually (§14.9.23.3 SR4 f) — an override runs
        else if (inv.Form is InvokeForm.NewSuper)
            factory = "base";   // SUPER: BASE's own New, non-virtually (§8.4.3.8.4 GR3; SR4 h) — never the caller's override (kb/Work PB1582)
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

    /// <summary>The instance-call marshaling (D6; §14.9.23.4 GR6/GR7a/GR8): every formal crosses as its argument's
    /// CARRIER, its AREA and its omitted flag (<see cref="OoArgPair"/>). ⛔ A BY REFERENCE identifier argument's carrier
    /// is a VIEW over the argument itself (ISO §14.2.3 GR8 — "the activated runtime element operates as if the formal
    /// parameter occupies the same storage area as the argument"; kb/Work PB2087): each access reads the argument in the
    /// formal's crossing form (the arm chain below) and each store reaches it back (<see cref="InvokeArgumentStore"/>),
    /// so there is no copy-out, and its area is the cell it lives in (an area formal is laid over it). Subscripts
    /// evaluate once at the beginning (GR7a — <see cref="IdentifyOperands"/> froze them). Every other argument is a
    /// detached cell: BY CONTENT crossings CONVERT into the formal's description per §14.8.2.3.3 (COMPUTE/MOVE/SET),
    /// composing the formal's value/image through the OWNER class's internal profiles (<c>{OWNER}._P_n</c>). The
    /// RETURNING delivery follows the call — identifier-4's store is the FINAL effect (GR8).</summary>
    private void EmitInstanceInvoke(BoundInvoke inv)
    {
        var w = Ctx.Writer;
        int id = Ctx.Names.NextOoInvoke();
        var argExprs = new List<string>();

        var args = inv.Args ?? [];
        for (int i = 0; i < args.Count; i++)
        {
            var a = args[i];
            bool stringCarried = OoStringCarried(a.Formal);
            string qualProfile = a.Formal.Pic is { Category: PicCategory.Numeric }
                ? $"{inv.OwnerCsName}{(inv.Form is InvokeForm.Factory ? NamingConvention.FactorySuffix : "")}.{a.Formal.ProfileName}" : "";

            string crossing = OoFormalCrossingType(a.Formal);
            // ⛔ THE OMITTED ARGUMENT (kb/Work PB757) — spelled, or trailing-omitted: its carrier is a placeholder cell of
            // the formal's crossing type, never read by the callee, paired with TRUE (§14.9.23.4 GR9).
            if (a.Omitted)
            {
                argExprs.Add(OoArgPair($"ManagedPointer<{crossing}>.Cell(default!)", "null", "true"));
                continue;
            }
            // ⛔ A FORWARDED FORMAL (§8.8.4.8.4 GR1c): an argument that is itself a whole formal parameter of this
            // source element carries its presence on, and — because §14.9.23.4 GR10 / §14.9.4.4 GR12 exempt a
            // reference "as an argument" — it is read only when it is present (a view reads lazily; a detached cell's
            // value is taken under the presence test). The recognition is the ONE CallUnitState.WholeFormalProbe the
            // CALL arm uses.
            string? fwdTest = a.Source is { } fsrc && callState.WholeFormalProbe(fsrc) is { } fprobe
                ? CallEmitter.OmittedTest(fprobe) : null;
            // The arm chain states the argument's VALUE in the formal's crossing form, once; the declared type each arm
            // names is that crossing form (the C# signature takes exactly it).
            string? argValue = null;
            void Decl(string type, string value) => argValue = value;
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
                Decl("string", CallEmitter.BooleanArgumentRecord(BooleanRenderer.RenderAtItemWidth(cb, Num), a.Formal));
            }
            // A figurative-constant / ALL-literal literal-2 (kb/Work PB1617): §14.2.3 GR9's MOVE into the method
            // formal's allocated record, filled to that record's character positions (§8.3.3.6.4 GR2) by the ONE
            // argument fill the CALL and function lanes use. A group formal is the case that makes it load-bearing:
            // the image arm below would space-pad one occurrence.
            // An ELEMENTARY non-numeric formal's record is the MOVE's (kb/Work PB2587's sweep), so an edited formal's
            // insertion positions keep their own characters (`SPACE` into PIC XX/XX is "  /  ", §14.9.25.4 GR6).
            else if (a.ContentFill is { } fill)
                Decl("string", CallEmitter.IsMovedFormal(a.Formal) ? U.Move.RecordValue(fill, a.Formal)
                    : CallEmitter.FigurativeArgumentImage(fill, a.Formal, Ctx.Data));
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
                // Two variable-length groups of DIFFERENT shapes (kb/Work PB480): the argument's carrier is rebuilt in
                // the formal's (§8.5.1.12 constrains only where their variable-length items lie).
                Decl(RuntimeApi.VarGroupType, a.Source is { } vgp
                    ? FixedArgumentSpans(vgp, a.Formal) is { } fs
                        ? RuntimeApi.VarGroupFromFixedImage(CallEmitter.CallStringRead(vgp), CallEmitter.LayoutArray(fs))
                        : VarGroupShapes(vgp, a.Formal) is (var argShape, var formalShape)
                            ? RuntimeApi.VarGroupReshape(PlaceRenderer.VarGroupBoundaryImage(vgp, "INVOKE argument"),
                                argShape, formalShape)
                            : PlaceRenderer.VarGroupBoundaryImage(vgp, "INVOKE argument")
                    : RuntimeApi.VarGroupEmpty);
            }
            else if (a.Source is { } vsp && VarPlaceSpans(vsp, a.Formal) is { } vs)
                // ⛔ A VARIABLE-length group argument into a FIXED-length group formal (§14.8.2.2; kb/Work PB965):
                // the formal reads the argument's image through the pair's correspondence, each corresponding
                // table fitted to the formal's occurrence count (§8.5.1.12.3 sentence 3).
                Decl("string", RuntimeApi.VarGroupToFixedImage(PlaceRenderer.VarGroupBoundaryImage(vsp, "INVOKE argument"), a.Formal.ImageWidth, CallEmitter.LayoutArray(vs)));
            // ⛔ A BY CONTENT VALUE INTO A FLOATING-POINT FORMAL OF ANOTHER DESCRIPTION (kb/Work PB1114). §14.8.2.3.3 2)
            // a) — "the same as for a COMPUTE statement" — and §14.2.3 GR9's "a COMPUTE statement without the ROUNDED
            // phrase" have no floating-point exemption, so a fixed-point or other-usage float sender, a literal-2 or an
            // arithmetic-expression-1 lands through FloatResultant, THE one transfer of an arithmetic value into a float
            // resultant identifier (implied TRUNCATION; the CALL lane's CobolArgAdapt.LandForFormal is the same store).
            // FIRST in the chain, before the image-carried arms, because an image-carried float formal takes the landed
            // value in its STORAGE image. A same-usage float identifier keeps the verbatim read below (§14.9.25.4 GR6 c).
            else if (IsFloatLanding(a))
            {
                var fpF = a.Formal.Pic!;
                NumX fv = a.Source is { } lsrc ? Num.AsNum(new BoundFieldOperand(lsrc), ReceiverContext.None)
                    : a.ContentExpr is { } fex ? FormalValue(fex, fpF)
                    : UnscaledLit(a.NumericLiteral!);
                string landed = ecState.SizeTruncationChecking
                    ? RuntimeApi.FloatResultantStoreOrRaise(fv, CobolRounding.Truncation, fpF.IsSingle)
                    : RuntimeApi.FloatResultantStore(fv, CobolRounding.Truncation, fpF.IsSingle);
                string algebraic = $"({fpF.ClrType})({landed})";
                if (stringCarried) Decl("string", RuntimeApi.NumFormatImageFloat(algebraic, qualProfile, fpF.IsSingle));
                else Decl(fpF.ClrType, algebraic);
            }
            // ⛔ A NUMERIC VALUE INTO AN IMAGE-CARRIED FIXED-POINT FORMAL (kb/Work PB1064): a method formal is a character
            // channel (§14.2.3 GR8), so a numeric-DISPLAY formal is carried as its image — and a literal-2 or an
            // arithmetic-expression-1 has no image, only a VALUE. §14.2.3 GR9 fills the formal's record by "a COMPUTE
            // statement without the ROUNDED phrase", so the value is the SAME store the native arms below render
            // (MethodNumericContent — one rule, two carriers), and the image is that record's storage image.
            else if (stringCarried && a.Source is null && a.StringLiteral is null
                     && a.Formal.Pic is { Category: PicCategory.Numeric, IsFloat: false }
                     && MethodNumericContent(a, qualProfile) is { } imageValue)
                Decl("string", RuntimeApi.NumFormatImage(imageValue, qualProfile));
            // ⛔ A STRONG GROUP WITH NO CHARACTER IMAGE (kb/Work PB1116): the argument is of the formal's type (bind
            // proved it), so it crosses as its leaf vector — a fresh vector, so BY CONTENT is a copy and BY REFERENCE
            // is copied back below. FIRST among the group arms: StringCarried is true of every group.
            else if (OoClassTable.LeafCarried(a.Formal))
                Decl("object?[]", PlaceRenderer.GroupLeaves(a.Source!));
            else if (a.Formal.IsGroup || (stringCarried && a.Source?.Item.IsGroup == true))
            {
                // The image crossing. BY REFERENCE allows a SMALLER formal (§14.8.2.2 rule 1 — a PREFIX of
                // the argument): pass the leading formal-width characters; the write-back below splices the
                // prefix back, preserving the argument's tail. CONTENT pads/truncates per MOVE.
                int fw = a.Formal.IsGroup ? CallEmitter.BoundaryImageWidth(a.Formal) : CallEmitter.ElementaryFormalWindow(a.Formal);
                string read = a.Source is { } gsp
                    ? a.ByContent ? CallEmitter.CallContentRead(gsp) : CallEmitter.CallStringRead(gsp)
                    : CsLiteral(a.StringLiteral ?? "");
                // A GROUP argument BY CONTENT into an ELEMENTARY formal is §14.2.3 GR9's MOVE into the formal's record,
                // and a group sender makes it §14.9.25.4 GR4's group move (kb/Work PB2587's sweep): the receiver's
                // JUSTIFIED still applies and a national or bit formal decodes the group's bytes — the CALL lane's one
                // record value (MoveEmitter.RecordValue), not a plain width fit.
                Decl("string", a.ByContent && CallEmitter.IsMovedFormal(a.Formal) && a.Source is { } gms
                    ? U.Move.RecordValue(new BoundFieldOperand(gms), a.Formal)
                    : RuntimeApi.StrStore(read, $"{fw}"));
            }
            else if (stringCarried)
                Decl("string", a.Source is { } sp
                    ? OoStringReadOf(sp, a, qualProfile)
                    : a.StringLiteral is { } slit
                    // An ANY LENGTH formal sees the literal AT ITS OWN length (§13.18.2 GR1) — no width-fit.
                    ? a.Formal.IsAnyLength ? CsLiteral(slit)
                        // §14.2.3 GR9's MOVE into a non-numeric formal's record (kb/Work PB2587's sweep): a JUSTIFIED formal
                        // right-justifies the literal and an edited one edits it (§14.9.25.4 GR6), as the CALL lane does.
                        : CallEmitter.IsMovedFormal(a.Formal)
                            ? U.Move.RecordValue(new BoundStringLiteral(slit), a.Formal)
                            : RuntimeApi.StrStore(CsLiteral(slit), $"{CallEmitter.ElementaryFormalWindow(a.Formal)}")
                    // A numeric literal into a formal of ANOTHER category (alphanumeric, numeric-edited, national):
                    // §14.8.2.3.3 rule 2d's MOVE, stored by the receiving category's ONE MOVE store — the store the
                    // identifier arm reaches through OoStringReadOf (kb/Work PB1113: the sign is not moved into an
                    // alphanumeric receiver, §14.9.25.4 GR6; a numeric-edited receiver edits).
                    : a.ContentExpr is { } cexpr
                    // An arithmetic expression likewise: its VALUE is the numeric sender (kb/Work PB1946, verdict PB1936),
                    // so it crosses through the same receiving-category MOVE store, never as a digit image of its own.
                    ? U.Move.ConvertSource(new BoundComputedOperand(cexpr), a.Formal)
                    : U.Move.ConvertSource(new BoundNumericLiteral(a.NumericLiteral!), a.Formal));
            // The PICTURE-less carriers (object reference, data pointer, program pointer) cross VERBATIM: they
            // have no picture, no scale and no character image, so the crossing is a reference/handle copy and
            // never a numeric store. Pointer/ProgramPointer joined this arm with the §14.8.2.3.2 class-pointer
            // conformance rule (fix-queue PB46) — before that they were unreachable, and the plain arm below
            // would have run Num.AsNum over a ManagedPointer.
            // An ADDRESS-IDENTIFIER argument (kb/Work PB1021) crosses the same verbatim carrier, its value
            // rendered by the ONE address-operand renderer — a detached pointer value (§14.9.23.3 SR19).
            else if (a.Address is { } ao)
                Decl(a.Formal.ElementType, U.Ptr.AddressOperandText(ao));
            // The predefined NULL (§8.4.3.7 / §8.4.3.10; kb/Work PB1137 + PB1630) — BY CONTENT, the FORMAL's own null:
            // its PicInfo.DefaultInitializer, the value INITIALIZE's implicit SET TO NULL stores (ManagedPointer.Null /
            // ProgramPointer.Null / FunctionPointer.Null / null). A bare `null` was right only for an object reference.
            else if (a.PredefinedNull)
                Decl(OoFormalCrossingType(a.Formal), a.Formal.Pic!.DefaultInitializer);
            // SELF (§8.4.3.8; kb/Work PB1137) — the object the containing method runs on, the SET F5 rendering.
            else if (a.SelfObject)
                Decl(OoFormalCrossingType(a.Formal), "this");
            else if (a.Formal.Pic is { Category: PicCategory.ObjectReference or PicCategory.Pointer
                                                 or PicCategory.ProgramPointer or PicCategory.FunctionPointer })
                Decl(OoFormalCrossingType(a.Formal), PlaceRenderer.Read(a.Source!));
            else if (a.Formal.Pic is { IsFloat: true })
                // Same-usage float — a BY REFERENCE pairing (§14.8.2.3.2, bind-enforced), or a BY CONTENT identifier
                // of the formal's own usage (every other BY CONTENT shape took IsFloatLanding above): read the float
                // value directly — never through the
                // scaled-integer path (the review's silent-truncation finding) — and on the item's OWN carrier
                // (NumericRenderer.FloatCarrierRead): a same-usage transfer is §14.9.25.4 GR6 c)'s "without
                // change", which a windowed binary32 decoded through binary64 is not (kb/Work PB961).
                Decl(a.Formal.ElementType, NumericRenderer.FloatCarrierRead(a.Source!, SendingRef.SameUsageMove));
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
                Decl(a.Formal.ElementType, $"({a.Formal.ElementType}){exprValue}");
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
                Decl(a.Formal.ElementType, $"({a.Formal.ElementType}){NumericRenderer.StoreExpr(cx, a.Formal.Pic!.Scale, qualProfile, raiseOnSizeError: ecState.SizeTruncationChecking)}");
            else if (a.Source is { } np)
                Decl(a.Formal.ElementType, $"({a.Formal.ElementType})({Num.AsNum(new BoundFieldOperand(np), ReceiverContext.None).Expr})");
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
                Decl(a.Formal.ElementType, $"({a.Formal.ElementType}){MethodNumericContent(a, qualProfile)}");
            }
            if (a.WriteBack && a.Source is { } src)
            {
                argExprs.Add(OoArgPair($"ManagedPointer<{crossing}>.OverField(() => {argValue}, __v => {{ {InvokeArgumentStore(a, src, "__v")} }})",
                    U.Call.ArgumentArea(src), fwdTest ?? "false"));
                continue;
            }
            string tmp = $"__iv{id}_{i}";
            w.Line($"{crossing} {tmp} = {(fwdTest is null ? argValue : $"{fwdTest} ? default! : {argValue}")};");
            argExprs.Add(OoArgPair($"ManagedPointer<{crossing}>.Cell({tmp})", "null", fwdTest ?? "false"));
        }

        // The trailing __retLen of an ANY LENGTH RETURNING item (OoSignatureOf; §13.18.2.4 GR1 b)): the length of the
        // item the result is delivered to — and, when the statement has no RETURNING phrase, GR1 is silent, so the
        // declared PICTURE length (docs/CONFORMANCE.md; the program ABI's CobolArgAdapt.ReturningSeed agrees).
        if (inv.ReturningSource is { IsAnyLength: true, Pic: { } anyRetPic })
            argExprs.Add(inv.Returning is { } anyRecv ? ReceiverLength(anyRecv) : $"{Math.Max(1, anyRetPic.Length)}");

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
            // GR8 — capture the result AT RETURN and store into identifier-4 LAST (the final effect of the INVOKE;
            // every BY REFERENCE argument already holds the method's stores — its carrier was a view over it).
            string tmp = $"__ivr{id}";
            bool retString = OoStringCarried(rs);
            w.Line($"var {tmp} = {call};   // INVOKE (§14.9.23.4; null receiver → EC-OO-NULL, GR5)");
            // ⛔ A RETURNING pair with ONE variable-length side (§14.8.3.2 "If either the sending or the receiving
            // operand is a variable length group, the sending operand and the receiving operand shall be
            // compatible, as described in 8.5.1.12"; kb/Work PB965): delivered through the pair's correspondence
            // — a dynamic table's occurrences fitted to the fixed table (§14.6.9.2), a fixed table crossing at its
            // occurrence count (§8.5.1.12.3 sentence 3). The same two legs the program ABI's StoreReturn takes. A cell or
            // redefinition VIEW of a fixed group is that group's storage (a receiver also passed BY REFERENCE somewhere is
            // claimed onto a cell, kb/Work PB2087/PB2089 — the FixedArgumentSpans twin); a reference-modified receiver
            // denotes no item.
            if (OoVarGroupCarried(rs) && recv.DenotedItem is { } ri
                && !CallEmitter.CallPlaceIsVarGroup(recv) && ItemCategory.IsGroupItem(ri)
                && VariableLengthCompatibility.CorrespondingSpans(ri, rs) is { } rvs)
                w.Line(CallEmitter.CallStringWrite(inv.Returning,
                    RuntimeApi.VarGroupToFixedImage(tmp, ri.ImageWidth, CallEmitter.LayoutArray(rvs))));
            else if (OoVarGroupCarried(rs))
                // A variable-length receiver of another shape takes the result rebuilt in its own (kb/Work PB480).
                w.Line(PlaceRenderer.WriteVarGroupImage(inv.Returning,
                    VarGroupShapes(recv, rs) is (var recvShape, var sendShape)
                        ? RuntimeApi.VarGroupReshape(tmp, sendShape, recvShape)
                        : tmp,
                    "INVOKE RETURNING delivery into"));
            else if (ItemCategory.IsGroupItem(rs) && VarPlaceSpans(recv, rs) is { } fvs)
                w.Line(PlaceRenderer.WriteVarGroupImage(inv.Returning,
                    RuntimeApi.VarGroupFromFixedImage(tmp, CallEmitter.LayoutArray(fvs)), "INVOKE RETURNING delivery into"));
            else if (OoClassTable.LeafCarried(rs))
                w.Line(PlaceRenderer.WriteGroupLeaves(inv.Returning, tmp));   // §14.8.3.2 same type — the leaf vector (kb/Work PB1116)
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
        }
        EmitInvokePickup(inv);   // §14.6.13.1.5 / §14.9.18.4 GR1b — a method GOBACK … RAISING is consumed HERE (after GR8)
    }

    /// <summary>⛔ HOW A STORE THROUGH A METHOD FORMAL REACHES ITS BY REFERENCE ARGUMENT (ISO §14.2.3 GR8; kb/Work PB2087):
    /// <paramref name="value"/>, in the formal's crossing form, written into the argument's own storage — the inverse of
    /// the read the typed INVOKE's arm chain states, so the formal's carrier (a view over the argument) round-trips on
    /// every access. It is the write half of the view, never a copy-out after the call.</summary>
    private string InvokeArgumentStore(BoundInvokeArg a, Place src, string value)
    {
        bool stringCarried = OoStringCarried(a.Formal);
        if (OoVarGroupCarried(a.Formal))
            // No prefix splice: a variable-length crossing carries whole components, so the write-back is
            // the exact inverse of the read (kb/Work PB204) — for a FIXED-length argument, the inverse of
            // its decomposition, each component fitted to its fixed table as §14.6.9.2 fits a dynamic
            // sender into a non-dynamic receiver (kb/Work PB965).
            return FixedArgumentSpans(src, a.Formal) is { } ws
                ? CallEmitter.CallStringWrite(src,
                    RuntimeApi.VarGroupToFixedImage(value, src.Item.ImageWidth, CallEmitter.LayoutArray(ws)))
                // …and of a variable-length argument of another shape, the formal's store overlaid on the argument's
                // storage: its material past the formal and its components the formal does not reach survive (kb/Work
                // PB480; §14.2.3 GR8).
                : VarGroupShapes(src, a.Formal) is (var argShape, var formalShape)
                    ? PlaceRenderer.WriteVarGroupImage(src,
                        RuntimeApi.VarGroupOverlay(PlaceRenderer.VarGroupBoundaryImage(src, "INVOKE copy-out into"), value,
                            argShape, formalShape),
                        "INVOKE copy-out into")
                : PlaceRenderer.WriteVarGroupImage(src, value, "INVOKE copy-out into");
        else if (VarPlaceSpans(src, a.Formal) is { } wv)
            // …and its write-back OVERLAYS the argument's storage (§14.2.3 GR8): the argument's tables keep
            // their current capacities and its material past the formal survives (kb/Work PB965).
            return PlaceRenderer.WriteVarGroupImage(src,
                RuntimeApi.VarGroupOverlayFixedImage(PlaceRenderer.VarGroupBoundaryImage(src, "INVOKE copy-out into"), value,
                    CallEmitter.LayoutArray(wv)),
                "INVOKE copy-out into");
        else if (OoClassTable.LeafCarried(a.Formal))
            return PlaceRenderer.WriteGroupLeaves(src, value);   // the leaf vector's copy-back (kb/Work PB1116)
        else if (a.Formal.IsGroup || src.Item.IsGroup)
        {
            int fw = a.Formal.IsGroup ? CallEmitter.BoundaryImageWidth(a.Formal) : CallEmitter.ElementaryFormalWindow(a.Formal);
            // The §14.8.2.2 rule-1 prefix: splice the formal's characters back over the argument's
            // LEADING positions, preserving the tail beyond the formal's width.
            return CallEmitter.CallStringWrite(src,
                // to-the-end window from fw+1 — the OMITTED-length sentinel; empty when the argument is no wider than
                // the formal (an image WINDOW the compiler chose, not a program's reference modification — it
                // neither raises nor terminates; kb/Work PB1707).
                $"{value} + {RuntimeApi.StrWindow(CallEmitter.CallStringRead(src), $"{fw + 1}", RuntimeApi.OmittedRefModLength)}");
        }
        else if (OoIsActiveClassFormal(a.Formal))
            // The universal crossing narrows back into the argument: §14.8.2.3.2 rule 4 / §14.8.2.3.3 proved the
            // argument is of the active class, which is the class of the argument's own description (ONLY) or an
            // ACTIVE-CLASS reference of the invoking class — so the cast cannot fail (kb/Work PB1497).
            return PlaceRenderer.Write(src, $"({src.Item.ElementType}){value}");
        else if (src is RefModPlace)
            return PlaceRenderer.Write(src, value);   // RefModPlace.Write splices the window (§8.4.3.3.4 GR6)
        else if (stringCarried)
            return OoStringCarried(src.Item) ? PlaceRenderer.Write(src, value) : PlaceRenderer.Write(new NumericImagePlace(src), value);
        else
            return src.Item.StoreAsImage
                ? PlaceRenderer.Write(src, NumericRenderer.ImageOfCarrier(value, src.Item))
                : PlaceRenderer.Write(src, value);
    }

    /// <summary>A RETURNING receiver's length in character positions, as a C# int expression — the n of §13.18.2.4 GR1 b)
    /// when the method's RETURNING item is ANY LENGTH. A varying receiver (ANY LENGTH, DYNAMIC LENGTH, a
    /// reference-modified window) is read at run time; a group is its image width; any other elementary item is its
    /// PICTURE's length.</summary>
    private static string ReceiverLength(Place recv) =>
        recv is RefModPlace || recv.Item is { IsAnyLength: true } or { IsDynamicLength: true }
            ? $"{PlaceRenderer.Read(recv)}.Length"
            : recv.Item.IsGroup ? $"{CallEmitter.BoundaryImageWidth(recv.Item)}"
            : $"{CallEmitter.ElementaryFormalWindow(recv.Item)}";

    /// <summary>The copy-in read of an identifier argument for a STRING-CARRIED formal: a reference-modified
    /// place reads its window verbatim (§8.4.3.3.4 GR6 — the operand IS elementary alphanumeric); a string-stored
    /// item reads directly; a native display-numeric item formats through its OWN profile (caller-side). A
    /// CONTENT crossing normalizes to the formal's width (MOVE pad/truncate).</summary>
    /// <summary>The INVOKE-site propagation pickup (D-EO6): a method GOBACK/EXIT … RAISING stages; the
    /// ACTIVATING site consumes — after the RETURNING delivery and copy-outs (GR1b ordering). Instance/
    /// Self/Super/Factory + UNIVERSAL dispatches all pick up; NEW needs none (the generated ctor runs no
    /// user statements, D4). Gated on <c>EcState.Active</c>, which spans class units.</summary>
    private void EmitInvokePickup(IActivatingStatement site) => U.Call.EmitPropagationPickup(site);

    /// <summary>True when a BY CONTENT argument lands in a FLOATING-POINT method formal by the §14.2.3 GR9 COMPUTE
    /// (kb/Work PB1114): the formal is floating-point, and the argument is a literal-2, an arithmetic expression or an
    /// identifier that is NOT a same-usage float — a same-usage float identifier crosses verbatim, which is §14.9.25.4
    /// GR6 c)'s "without change" (a windowed binary32 decoded through binary64 is not). One predicate, for the
    /// emitter arm that lands and the arms that must leave the same-usage crossing alone.</summary>
    private static bool IsFloatLanding(BoundInvokeArg a) =>
        a.ByContent && !a.Formal.IsGroup && a.Formal.Pic is { Category: PicCategory.Numeric, IsFloat: true } fp
        && !(a.Source?.Item.Pic is { IsFloat: true } sp && sp.Usage == fp.Usage);

    /// <summary>⛔ AN ARITHMETIC-EXPRESSION-1 ARGUMENT EVALUATED FOR THE FORMAL'S DESCRIPTION (kb/Work PB289, the INVOKE twin
    /// of <c>CallEmitter.ArgText</c>'s computed-operand arm). §14.2.3 GR9 fills the formal's record by "a COMPUTE statement
    /// without the ROUNDED phrase", so the expression is the sending operand and the formal the resultant: it renders as the
    /// FINAL TRANSFER into a resultant of the formal's description (<see cref="ReceiverContext.Of"/> — the arithmetic
    /// statements' own rule), never receiver-less at the 6-digit working scale that dropped the digits the formal could hold.
    /// Checking is the ambient EC-SIZE state, as for every receiver-less render in this statement.</summary>
    private NumX FormalValue(BoundExpr expr, PicInfo formalPic) =>
        Num.Render(expr, ReceiverContext.Of(formalPic, CobolRounding.Truncation, ecState.SizeChecking),
            outermost: true);

    /// <summary>The VALUE a literal-2 or arithmetic-expression-1 argument stores into a fixed-point numeric method formal
    /// (kb/Work PB1064): §14.2.3 GR9 fills the formal's record by "a COMPUTE statement without the ROUNDED phrase", so
    /// it is the ONE numeric store (<see cref="NumericRenderer.StoreExpr"/> — PB84's SDIDI overloads, PB640's raising
    /// kernel) into the formal's scale through the OWNER's profile, and the literal is the exact scaled integer of both
    /// notations (PB263). A native formal casts it to its element type; an image-carried one formats it to its storage
    /// image. Null when the argument is neither.</summary>
    private string? MethodNumericContent(BoundInvokeArg a, string qualProfile)
    {
        NumX? value = a.ContentExpr is { } cex ? FormalValue(cex, a.Formal.Pic!)
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
        // ⛔ THE SENDER IS ANY NUMERIC ONE (kb/Work PB1114's review finding): a COMPUTE's sending operand may be a
        // FLOATING-POINT item too (§14.2.3 GR9 has no float exemption on the sender), and a float sender fell to the
        // MOVE store below — the same bare-profile CS0103, and a MOVE where §14.7.5 r4 owes the EC-SIZE raise.
        // StoreExpr tells the lanes apart (a Real intermediate lands through ToScaled, CHECKED when it raises).
        if (fp.IsClassNumericFixedPoint
            && sp.DenotedItem is { Pic.IsClassNumeric: true })
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
            // The identical-category fast path: the read itself, stored by the ONE elementary character store — which is
            // what right-justifies a JUSTIFIED formal (§14.9.25.4 GR6 a) / §14.6.8; kb/Work PB2587's sweep: a plain width
            // fit here left-justified `ABCD` in a PIC X(6) JUSTIFIED RIGHT method formal).
            ? ReceivingStore.Characters(a.Formal, read, $"{Math.Max(1, fp.Length)}")
            : U.Move.ConvertSource(new BoundFieldOperand(sp), a.Formal);
    }

}
