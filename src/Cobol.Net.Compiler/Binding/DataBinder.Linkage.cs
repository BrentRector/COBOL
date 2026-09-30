// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Runtime;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// One PROCEDURE DIVISION USING formal parameter (ISO §14.2.2 SR1 — a level-01/77 LINKAGE item) and its
/// caller-storage carrier (COBOLNET_INTERPROGRAM_DESIGN D1/D2).
/// </summary>
/// <param name="Item">The LINKAGE-section 01/77 item the header USING operand names.</param>
/// <param name="Position">The 0-based positional slot (ISO §14.2.3 GR2 — correspondence is positional, never by name).</param>
/// <param name="CarrierField">The emitted <c>ManagedPointer&lt;T&gt;</c> field name (<c>__lnk{Uid}</c> — Uid-keyed so a
/// contained program's ref-bridge of a GLOBAL formal cannot collide with its own formals; kb/Work PB1009).</param>
/// <param name="CarrierResident">True when the formal is CARRIER-RESIDENT: an elementary formal whose every
/// reference reads/writes the caller's storage through <c>carrier.Value</c> (the design's "one unavoidable
/// indirection" — per-access aliasing per ISO §14.2.3 GR8). False for a group formal or a REDEFINED elementary
/// formal: those keep a callee-local field and round-trip the caller's character image at the ACTIVATION
/// boundary (copy-in at entry, copy-out at return — the deep-dive group hard problem's whole-struct round trip,
/// realized at the call boundary).</param>
/// <param name="ByValue">True when the header names this formal in a BY VALUE phrase (ISO §14.2.2 using-phrase;
/// §14.2.3 GR4 transitivity resolved at bind). The activated element then operates on a VALUE COPY — a detached
/// cell conformed to the formal's description (§14.2.3 GR10: the argument is the sending operand of a COMPUTE
/// without ROUNDED into the allocated record); the callee's stores never reach the caller's storage.</param>
public sealed record LinkageFormal(DataItem Item, int Position, string CarrierField, bool CarrierResident,
    bool ByValue = false, bool Optional = false)
{
    /// <summary>The presence fact every consumer reads (§8.8.4.8.4 GR1) — the program arm's null carrier.</summary>
    public OmittedProbe Probe => new OmittedProbe.Carrier(CarrierField);
}

/// <summary>The synthesized run-unit backing of one EXTERNAL record (ISO §13.18.22 / §8.6.7): the emitter
/// renders <c>private ref string {BackingCsName} =&gt; ref ExternalStore.Cell({ExternalName}, «seed»).Ref;</c>
/// and every reference windows it through the Tier-B view machinery. <see cref="Record"/> carries the record's own
/// DataItem, which is ALL the emitter needs: the seed is composed by the ONE Tier-B seeder
/// (<c>GroupImageCodec.ImageInitOf</c>), with VALUEs suppressed for a plain external item — whose VALUE takes
/// effect only during INITIALIZE, not at initial state (§13.18.63 GR4a) — and honoured for a CONSTANT RECORD,
/// which §11.9.10.4 GR7 DOES initialize at initial state (the one external exception: external WS items are not
/// initialized at initial state "except for those with the CONSTANT RECORD clause"). ⛔ There is no precomputed
/// InitImage field any more: it was a SECOND seeder, hand-rolled in the binder from a char-fill model that
/// predated the pinned byte forms (kb/Work PB164).</summary>
/// <param name="CellCsName">The <c>StorageCell</c> property the backing is defined over
/// (<c>RedefinesClass.BackingCellCsName</c>) — the cell holds BOTH halves of the one shared area, its byte image
/// and its MANAGED SLOTS, so a pointer-class member of an external record addresses the same storage every other
/// describer does (kb/Work PB231).</param>
public sealed record CallExternalBacking(string BackingCsName, string CellCsName, string ExternalName, int Width,
                                         DataItem Record);

/// <summary>One FUNCTION-ID unit's activation signature (ISO §9.4 user-defined functions; M2-UDF-1): the
/// registered function name, its PROCEDURE DIVISION RETURNING item (whose description the caller-side result
/// temporary clones — §8.4.3.2.4 GR1), and its positional USING formals (§14.8.2). Built by the run-unit
/// emitter BETWEEN the DATA and PROCEDURE bind phases, so a <c>FUNCTION user-name(args)</c> reference binds
/// against signatures of function units defined ANYWHERE in the compilation group, including after the caller.
/// <paramref name="Returning"/> is null only for the ill-formed no-RETURNING function (COBOLNET1507 — already
/// diagnosed; call sites fail loud without re-reporting).</summary>
/// <param name="Name">user-function-name-1 as written — the key every <c>FUNCTION word(…)</c> reference
/// resolves against (ISO §8.4.6.6 / §8.4.6.7 scope the WORD, never the AS literal).</param>
/// <param name="Externalized">The name the function is externalized under (§11.5.4 GR1: "literal-1, if
/// specified, is the name of the function that is externalized to the operating environment"; else the word).
/// The activation the emitter writes names the callee THIS way, because that is the key the run-unit registry
/// holds it under — the two must not drift (kb/Work PB303).</param>
public sealed record UserFunctionSignature(
    string Name, string Externalized, DataItem? Returning, IReadOnlyList<LinkageFormal> Formals);

/// <summary>ONE Format-2 callee's activation signature: its positional USING formals and its PROCEDURE DIVISION
/// RETURNING item. §14.9.4.3 SR25 makes BOTH §14.8.2, Parameters and §14.8.3, Returning items apply to a Format-2
/// CALL, so wherever the callee is statically known the whole conformance regime is a BIND-time check — and the
/// signature carries both halves, exactly as <see cref="UserFunctionSignature"/> does.
/// <para>TWO producers reach the same shape, which is why this type is not named for either of them (kb/Work
/// PB237): <c>AS NESTED</c> resolves its callee through the containment tree (§14.9.4.3 SR15, PB131 + PB204), and
/// <c>program-prototype-name-1</c> resolves it through the REPOSITORY program-specifier and §12.3.8.4 GR10's
/// search of the compilation group. Every §14.8.2/§14.8.3 check in <c>CallBinder.BindCall</c> reads THIS record,
/// so the prototype arm inherited the whole set the day it was wired in.</para>
/// <paramref name="Returning"/> is null when the callee's header declares no returning item, which §14.8.3.1
/// makes a conformance fact in its own right ("if and only if").</summary>
public sealed record CalleeSignature(IReadOnlyList<LinkageFormal> Formals, DataItem? Returning);

/// <summary>One §12.3.8.2 program-specifier as WRITTEN — <c>PROGRAM program-prototype-name-1 [AS literal-3]</c>
/// (kb/Work PB237). <paramref name="ExternalizedName"/> is literal-3 when the AS phrase is present and the
/// prototype name itself otherwise; §12.3.8.4 GR10 NOTE 1 states that identity outright ("Literal-3, if specified,
/// is the externalized name of the program prototype; otherwise, the externalized name is
/// program-prototype-name-1"). Collected by <c>DataBinder.BindProgramSpecifier</c>; RESOLVED by
/// <c>BinderDriver.ProgramPrototypesOf</c>.</summary>
public sealed record ProgramSpecifier(string Name, string ExternalizedName);

/// <summary>One RESOLVED program prototype, as §12.3.8.4 GR10 defines it: the declared program-prototype-name, the
/// externalized name that identifies the program to be called, and — when GR10 a) found a program definition in
/// this compilation group — that definition's <see cref="CalleeSignature"/> (kb/Work PB237).
/// <para><paramref name="Signature"/> is null when the details come from GR10 c), "the external repository for the
/// program with the same name as the externalized name of the program prototype": this implementation's external
/// repository is the RUN UNIT's program registry, resolved at execution (§14.9.4.4 GR3 b), with
/// EC-PROGRAM-NOT-FOUND when the locate fails), so a separately-compiled callee is legal and simply carries no
/// compile-time signature for §14.8.2/§14.8.3 to check. That is the ONE difference between a prototype call and an
/// AS NESTED call, and it is the difference the standard itself draws.</para></summary>
public sealed record ProgramPrototype(string Name, string ExternalizedName, CalleeSignature? Signature);

/// <summary>
/// The LINKAGE SECTION / EXTERNAL / GLOBAL half of the data binder (COBOLNET_INTERPROGRAM_DESIGN D1–D5).
/// LINKAGE 01/77 items bind into the ordinary storage forest (so every verb, REDEFINES tier, level-88, and
/// OCCURS path works unchanged — ISO §13.7: linkage entries are ordinary record descriptions whose STORAGE
/// belongs to the caller); the PROCEDURE DIVISION header's USING/RETURNING operands then designate which roots
/// are formal parameters (§14.2.2 SR1) and how each is carried. EXTERNAL level-01/77 records become Tier-B
/// string windows over a run-unit <c>ExternalStore</c> cell (§8.6.7 — one storage copy per name per run unit);
/// GLOBAL level-01/77 roots are collected for the emitter's containment inheritance (§13.18.27 GR1–2).
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The LINKAGE SECTION's top-level (01/77) items, in source order (ISO §13.7).</summary>
    public List<DataItem> LinkageRoots { get; } = [];

    /// <summary>True when this binder binds a CONTAINED program unit (ISO §13.18.2.3 SR2/SR3 — the ANY LENGTH
    /// placement rules differ per unit kind). Set by <c>BinderDriver.BindUnitData</c> from the unit's Parent.</summary>
    public bool UnitIsContained { get; init; }

    /// <summary>True when this binder binds a FUNCTION-ID unit (ISO §13.18.2.3 SR2/SR4). Set by
    /// <c>BinderDriver.BindUnitData</c> from <c>BoundUnit.IsFunction</c>.</summary>
    public bool UnitIsFunction { get; init; }

    /// <summary>The unit's own name, for the SELF leg of the two prototype-name scope rules: ISO §8.4.6.6 admits
    /// "the user-function-name of the containing function definition" as a function-prototype-name, and §8.4.6.8
    /// admits "the program-name of a containing program definition" as a program-prototype-name — so a
    /// self-referential <c>USAGE FUNCTION-POINTER TO «this function»</c> declaration is conforming without a
    /// REPOSITORY entry. The PROCEDURE-phase twin is <c>StatementBinder.UdfSelfName</c>; this is the DATA-phase
    /// half, because the USAGE clause's restriction operand binds here. Set by <c>BinderDriver.BindUnitData</c>
    /// from <c>BoundUnit.Name</c>. kb/Work PB452/PB817.</summary>
    public string? UnitSelfName { get; init; }

    /// <summary>The PROCEDURE DIVISION USING formals, positional (ISO §14.2.3 GR2). (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlyList<LinkageFormal> LinkageFormals => _linkageFormals;
    private readonly List<LinkageFormal> _linkageFormals = [];

    /// <summary>The PROCEDURE DIVISION RETURNING item (a LINKAGE 01/77; ISO §14.2.3 GR6 — its storage is
    /// allocated in the activated element), or null. COBOL-2002+ (the grammar gates the clause).</summary>
    public DataItem? LinkageReturning { get; private set; }

    /// <summary>The C# names of class-level fields the <c>FieldEmitter</c> must NOT declare because another
    /// mechanism provides the member: a carrier-resident LINKAGE formal's "field" IS the carrier accessor
    /// (<c>__lnk{Uid}.Value</c> — the caller owns the storage, ISO §13.7.1 / §14.2.3 GR8), and an inherited
    /// GLOBAL table's index field is a <c>ref</c>-bridge to the containing instance (ISO §13.18.27 GR2 —
    /// global index-names are shared, never duplicated). (READ-ONLY view — P6 Step 5; written by the LINKAGE and
    /// OO binders. The inherited-index seeding member this comment once named was deleted by kb/Work PB919, whose
    /// per-table index declarations now carry an inherited GLOBAL index-name.)</summary>
    public IReadOnlySet<string> CallSuppressedRootFields => _callSuppressedRootFields;
    private readonly HashSet<string> _callSuppressedRootFields = new(StringComparer.Ordinal);

    /// <summary>Level-1 roots carrying an ADMITTED GLOBAL clause (ISO §13.18.27.3 SR1 b) — file, working-storage,
    /// local-storage or linkage section), plus the records of a GLOBAL FD (§13.18.30) — visible to every
    /// directly/indirectly contained program (GR1–2); the emitter injects them into contained units' binders and
    /// bridges their fields into the nested classes. (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlyList<DataItem> CallGlobalRoots => _callGlobalRoots;
    private readonly List<DataItem> _callGlobalRoots = [];

    /// <summary>⛔ THE ONE LIST OF THIS UNIT'S MEMBERS THAT A REFERENCE TO GLOBAL ROOT <paramref name="g"/> CAN
    /// RENDER (kb/Work PB1009) — each becomes a bridge in a contained program, re-anchored behind
    /// <paramref name="outer"/> (ISO §13.18.27.4 GR2: "A statement in a program contained directly or indirectly
    /// within a program that describes a global name may reference that name without describing it again" — the
    /// storage stays the container's). A reference renders the root's STORAGE member and, for the residences that
    /// have them, the members around it, so every residence §13.18.27.3 SR1 b) admits (file, working-storage,
    /// local-storage, linkage) is answered here:
    /// <list type="bullet">
    /// <item>a Tier-B class (REDEFINES / EXTERNAL / BASED / ADDRESS-OF-taken) → its string backing, plus the
    /// <c>StorageCell</c> behind it when the class is cell-backed (a pointer-class member's slot and an
    /// <c>ADDRESS OF</c> both name the cell), plus a BASED class's implicit data-address pointer (every
    /// reference displaces by it, and SET ADDRESS OF / ALLOCATE / FREE write it);</item>
    /// <item>a CARRIER-RESIDENT formal → its <c>ManagedPointer</c> carrier (its "field" IS
    /// <c>carrier.Value</c>, so the bridge is the carrier, never the accessor text);</item>
    /// <item>any other root → its field;</item>
    /// <item>a guarded formal → its omitted-presence member (kb/Work PB971); a table → its index fields.</item>
    /// </list>
    /// ⛔ A new residence adds its member HERE, and <c>GlobalBridgeResidenceDriftTests</c> is red until it does.</summary>
    internal IEnumerable<CallBridge> GlobalBridgesOf(DataItem g, string outer)
    {
        if (g.Class is { Tier: RedefinesTier.StringCanonical } cls)
        {
            yield return new CallBridge(cls.BackingCsName, outer + cls.BackingCsName, CallBridgeKind.Backing, null);
            if (cls.IsCellBacked)
                yield return new CallBridge(cls.BackingCellCsName, outer + cls.BackingCellCsName, CallBridgeKind.Cell, null);
            if (cls.BasedPointerField is { } addr)
                yield return new CallBridge(addr, outer + addr, CallBridgeKind.Address, null);
        }
        else if (_linkageFormals.FirstOrDefault(f => f.CarrierResident && ReferenceEquals(f.Item, g)) is { } rf)
            yield return new CallBridge(rf.CarrierField, outer + rf.CarrierField, CallBridgeKind.Carrier, g, rf);
        else
            yield return new CallBridge(g.CsName, outer + g.CsName, CallBridgeKind.Field, g);
        // A GLOBAL FORMAL PARAMETER of the container: its guarded references in the contained program read the
        // container's presence member under the same Uid-keyed name (kb/Work PB971).
        if (g.OmittedGuard is { } og)
            yield return new CallBridge(og.Presence, outer + og.Presence, CallBridgeKind.Presence, g);
        foreach (var idx in IndexDeclarationsUnder(g))
                yield return new CallBridge(idx.Cell, outer + idx.Cell, CallBridgeKind.Index, null);
    }

    /// <summary>⛔ THE NESTING DISTANCE OF EVERY INHERITED GLOBAL DECLARATION (kb/Work PB1047 / PB1243) — the tier
    /// ISO §8.4.6.2.1 3) selects on. Absent = 0: the item is this source element's own. n = the item is a global
    /// name of the n-th containing program. Keyed by identity, because the SAME container item is inherited into
    /// every contained program at a different distance.</summary>
    private readonly Dictionary<DataItem, int> _inheritedGlobalDepth = new(ReferenceEqualityComparer.Instance);

    /// <summary>How many containment levels out <paramref name="item"/> is declared: 0 for an item this source
    /// element declares (or any item of a unit that inherits nothing), n for a GLOBAL item of its n-th container.</summary>
    internal int DeclaringDepth(DataItem item) =>
        _inheritedGlobalDepth.Count != 0 && _inheritedGlobalDepth.TryGetValue(item, out int depth) ? depth : 0;

    /// <summary>True when this unit inherits any container's global data — the only case in which a candidate set
    /// can span tiers, so the nearest-tier filter is free for every other unit.</summary>
    internal bool InheritsGlobalData => _inheritedGlobalDepth.Count != 0;

    /// <summary>Make one container GLOBAL root's whole subtree visible here, <paramref name="depth"/> containment
    /// levels out (ISO §13.18.27.4 GR2 — "may reference that name without describing it again"): every data-name
    /// (§8.4.6.2.2: "All data-names and screen-names subordinate to a global name are global names"), every
    /// condition-name associated with one ("All condition-names associated with a global name are global names"),
    /// every level-66 RENAMES of the record, and — by §8.4.6.2.3, "the scope of an index-name is identical to that
    /// of the data-name that names the table" — every index-name (kb/Work PB919). Each item carries its depth, so
    /// a nearer declaration of the same spelling wins by §8.4.6.2.1 3) at REFERENCE time
    /// (<see cref="Model.SymbolTable.NearestDeclaring{T}"/>), not by being left out here. Called nearest container
    /// first, after this unit's own data division has bound, so each name list stays in nearest-first order. The
    /// cells stay the container's: this unit reaches them through the GLOBAL bridge
    /// (<see cref="GlobalBridgesOf"/>) and never emits them.</summary>
    internal void InheritGlobalSubtree(DataItem item, int depth)
    {
        _inheritedGlobalDepth[item] = depth;
        if (item.CobolName is { } name)
        {
            if (!ByName.TryGetValue(name, out var list)) ByName[name] = list = [];
            list.Add(item);
        }
        foreach (var cond in item.Own88s)
        {
            if (!Conditions.TryGetValue(cond.Name, out var conds)) Conditions[cond.Name] = conds = [];
            conds.Add(cond);
        }
        foreach (var idx in item.Indexes) IndexNames.Inherit(idx, depth);
        foreach (var child in item.Children) InheritGlobalSubtree(child, depth);
        foreach (var ren in item.Renames66) InheritGlobalSubtree(ren, depth);
    }

    /// <summary>⛔ EVERY C# MEMBER NAME THIS UNIT'S GLOBAL ROOT <paramref name="g"/> OCCUPIES IN A CONTAINED UNIT'S
    /// CLASS — the bridge members <see cref="GlobalBridgesOf"/> emits AND the root's own C# name (with its REDEFINES
    /// canonical's), because that name is the STEM every derived member is spelled from: a REDEFINES class's
    /// <c>_redef_</c> backing, its <c>_scell_</c> cell, a BASED class's <c>__addr_</c> pointer
    /// (<see cref="Model.RedefinesClass.BackingCsName"/>). A contained unit uniques its local roots against these, so
    /// a local stem never equals an inherited one and no derived member can re-spell a bridge (those prefixes cannot
    /// begin a COBOL word). Reserving only the bridge members left a local <c>01 G</c> + <c>01 K REDEFINES G</c> the
    /// stem <c>G</c>, and its class backing re-spelled the container's <c>_redef_G</c> bridge (CS0102; wave 69 Z,
    /// found by the train-68b review).</summary>
    internal IEnumerable<string> InheritedMemberNamesOf(DataItem g)
    {
        foreach (var bridge in GlobalBridgesOf(g, "")) yield return bridge.Field;
        yield return g.CsName;
        if (g.Class is { } cls) yield return cls.Canonical.CsName;
    }

    /// <summary>Reserve the C# member names a container's GLOBAL roots occupy in this unit's class
    /// (<see cref="InheritedMemberNamesOf"/>), BEFORE this unit's data division binds, so a local root spelled like
    /// a container's global root takes a distinct field name (<c>G_2</c>) — and every member derived from it a
    /// distinct name — instead of colliding with a bridge (kb/Work PB1047 — every global root is bridged now,
    /// including one a local declaration hides by §8.4.6.2.1 3) a), because its subordinate names remain
    /// visible).</summary>
    internal void ReserveInheritedMemberNames(IEnumerable<string> names) => _rootNames.UnionWith(names);

    /// <summary>The EXTERNAL records' synthesized run-unit backings (ISO §13.18.22; emitted as
    /// <c>ref</c>-properties over <c>ExternalStore</c>). (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlyList<CallExternalBacking> CallExternalBackings => _callExternalBackings;
    private readonly List<CallExternalBacking> _callExternalBackings = [];

    /// <summary>§14.9.4.4 GR3 d)'s ACTIVATED half (kb/Work PB133 wave C2b): this unit's EC-PROGRAM-ARG-MISMATCH
    /// enablement at its PROCEDURE DIVISION header (§7.3.25.4 GR6 / GR8; <c>TurnState.EnabledAtHeader</c>, kb/Work
    /// PB1381) — registered with the program table so a dynamic CALL's count check can apply the enabled-in-both
    /// gate.</summary>
    public bool ArgMismatchChecking { get; set; }

    /// <summary>Is automatic propagation of exception conditions enabled for this program or function unit (ISO
    /// §7.3.21.4 GR1/GR3; kb/Work PB1119)? Folded by <c>BinderDriver.BindUnitData</c> from the group's
    /// <see cref="PropagateState"/> at the unit's first line; read by <c>ProgramEmitter</c> into the emitter's
    /// per-element propagation state, which <c>EcEmitter.EmitSelection</c> applies (§14.6.13.1.3 6)).</summary>
    public bool AutomaticPropagation { get; set; }

    /// <summary>This unit's before-Environment-division EC-EXTERNAL enablement mask (ISO §14.8.4.1 — the
    /// ACTIVATED-element half of the both-elements rule; <c>ExternalChecks</c> bits). Computed by
    /// <c>BinderDriver.BindUnitData</c> from the group TurnState folded at the unit's first
    /// post-Identification division header; emitted as the selfMask of the activation-entry
    /// <c>ExternalStore.Describe</c> registrations.</summary>
    public int ExternalCheckMask { get; set; }

    /// <summary>True when any enabling <c>&gt;&gt;TURN</c> event anywhere in the compilation group covers an
    /// EC-EXTERNAL condition — the group-level gate for emitting the activation-entry
    /// <c>ExternalStore.Describe</c> registrations at all (an element with a zero
    /// <see cref="ExternalCheckMask"/> must still REGISTER its descriptions so a later-enabled element can
    /// check against them, §14.8.4; a group with no EC-EXTERNAL TURN emits nothing — zero-scaffolding).</summary>
    public bool ExternalDescribe { get; set; }

    /// <summary>True when any enabling <c>&gt;&gt;TURN</c> event anywhere in the compilation group covers
    /// EC-BOUND-ODO — the gate for attaching §13.18.38.4 GR7's element-reference check
    /// (<see cref="Model.OdoReferenceCheck"/>) to a subscripted OCCURS DEPENDING level. The run-time
    /// <c>BoundOdoChecking</c> flag still decides per statement; this only keeps a group that can never enable the
    /// condition free of the data-name-1 read at every element reference (zero-scaffolding).</summary>
    public bool OdoReferenceChecking { get; set; }

    /// <summary>
    /// Bind the LINKAGE SECTION and the PROCEDURE DIVISION header's USING/RETURNING operands. Runs inside
    /// <c>Bind</c> right after WORKING-STORAGE (so linkage items join the same forest, name index, and
    /// post-build passes — REDEFINES classification, SIGN inheritance, index resolution all apply, ISO §13.7.3).
    /// </summary>
    internal void CallBindLinkage(Core.ProgramUnitContext program, HashSet<string> rootNames)
    {
        if (program.dataDivision()?.linkageSection() is { } ls)
        {
            var entries = ls.linkageEntry()
                .Select(e => e.dataDescriptionEntry())
                .Where(e => e is not null)
                .Select(e => e!);
            LinkageRoots.AddRange(BindEntries(entries, rootNames, EntrySection.Linkage));
        }

        if (program.procedureDivision() is not { } pd) { AnyLengthValidateUnit(); return; }

        // The using-phrase modes (ISO §14.2.2 :23636 — { [BY REFERENCE] {[OPTIONAL] d}… | BY VALUE {d}… }…):
        // BY REFERENCE / BY VALUE are TRANSITIVE across the parameters that follow until the other phrase
        // appears; BY REFERENCE is assumed before the first phrase (§14.2.3 GR4). Threaded flat over the
        // grammar's one-parameter-per-node shape (the CALL callArgument precedent).
        int pos = 0;
        bool byValue = false;
        var header = new ProcedureHeaderScreen(Edition, "PROCEDURE DIVISION");   // §14.2.2 SR1/SR5/SR6 — the ONE screen (kb/Work PB1145)
        // The names any ADDRESS OF in this unit or a contained one takes (the SAME scan the pointer pass forces
        // cells from — a GLOBAL formal can be addressed from a contained program, kb/Work PB1009/PB1019).
        var addressed = DataBinder.PtrScanAddressOfSenders(pd)
            .Concat(program.nestedProgram().SelectMany(DataBinder.PtrScanAddressOfSenders))
            .Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var prm in pd.usingClause()?.usingParameter() ?? [])
        {
            using var _ = Edition.At(prm);
            Core.DataReferenceContext dref;
            bool optional = false;
            if (prm.usingByValue() is { } vb) { byValue = true; dref = vb.dataReference(); }
            else if (prm.usingByReference() is { } rb)
            {
                byValue = false;
                dref = rb.dataReference();
                optional = rb.OPTIONAL() is not null;
            }
            else { dref = prm.dataReference(); optional = prm.OPTIONAL() is not null; }
            // §14.2.2's OPTIONAL phrase now CARRIES (kb/Work PB133 wave C): the formal records the flag,
            // the SR24/count checks read it at the AS NESTED bind, and the §8.8.4.8 omitted-argument
            // condition tests the carrier's presence — the staged OptionalFormal rejection is lifted.

            string pname = dref.GetText();
            var item = FindLinkageRoot(pname);
            if (item is null)
            {
                // §14.2.2 SR1: each formal parameter shall be a level-01/77 entry in the LINKAGE SECTION.
                Edition.Error("COBOLNET0888",
                    $"PROCEDURE DIVISION USING parameter '{pname}' is not a level-01/77 LINKAGE SECTION item "
                    + "(ISO §14.2.2 SR1)");
                pos++;
                continue;
            }
            // SR1: uniqueness, and no REDEFINES / BASED clause — the ONE header screen. A BASED formal's flag clears
            // there: a carrier-resident formal's CsName rewrite would poison the based class's BackingCsName into
            // invalid C#, so the entry binds as an ordinary (already-diagnosed) formal. A REPEATED formal is not
            // admitted a second time (`USING A A` reached the backend as a duplicate carrier member).
            if (!header.AdmitFormal(pname, item)) { pos++; continue; }
            if (optional && byValue) header.OptionalNeedsByReference(pname);   // §14.2.1: OPTIONAL is a BY REFERENCE word

            if (byValue)
            {
                // §14.2.2 SR2 (:23664): "Each data-name-1 specified in a BY VALUE phrase shall be defined as a
                // data item of class numeric, message-tag, object, or pointer." Class message-tag is the MCS
                // module (not modeled — undeclarable here, so unreachable). Fixed-point class numeric and the
                // MANAGED classes (object / the three pointer categories) are the CARRIED legs of the §14.2.3
                // GR10 detached-cell value copy — GR10 names both: "a COMPUTE statement without the ROUNDED
                // phrase" for the numeric one, "a SET statement" for class object or pointer, which is the
                // reference copy CobolArgAdapt.SlotValue performs (kb/Work PB663). Only the FLOATING-POINT
                // usage remains loud, by name — never silently by-ref.
                if (!(item.IsElementary && item.Pic?.Category is PicCategory.Numeric or PicCategory.Pointer
                        or PicCategory.ProgramPointer or PicCategory.FunctionPointer
                        or PicCategory.ObjectReference))
                    Edition.Error("COBOLNET1553",
                        $"BY VALUE formal parameter '{pname}' shall be of class numeric, message-tag, object, "
                        + "or pointer (ISO §14.2.2 SR2)");
                else if (item.Pic is { Category: PicCategory.Numeric, IsFloat: true })
                    Edition.Error(DiagnosticCatalog.ByValueFormalCarrier,
                        $"BY VALUE formal parameter '{pname}': a FLOATING-POINT value-copy carrier "
                        + "(ISO §14.2.3 GR10) is not yet implemented — the fixed-point numeric and the "
                        + "object/pointer BY VALUE formals are carried");
            }

            // ⛔ THE CARRIER NAME IS Uid-KEYED, NOT POSITIONAL (kb/Work PB1009). A carrier-resident formal's
            // "field" IS `carrier.Value`, and a GLOBAL formal (§13.18.27.3 SR1 b) admits GLOBAL in the linkage
            // section) is referenced from every contained program by that same text through a ref-bridge — so
            // a positional `__lnkp0` collided with the contained program's OWN first formal: its references to
            // either item read one carrier, and the bridge declared a second member of the same name. The
            // Uid-keyed name (the `__omit{Uid}` presence member's rule) cannot collide within a group.
            string carrier = $"__lnk{item.Uid}";
            // Carrier-resident = per-access aliasing of the caller's storage (design D1: "refs to LK-CTR
            // read/write LK_CTR.Value — the one unavoidable indirection"). Available for an elementary
            // fixed-point/character formal NOT overlaid by another linkage entry; a group formal (a different
            // C# struct type than the caller's — every cross-program group IS a category reinterpretation) and
            // a redefined formal round-trip the character image at the activation boundary instead. A BY VALUE
            // formal is carrier-resident over its DETACHED value-copy cell (§14.2.3 GR10 — same per-access
            // mechanism, different carrier: CobolArgAdapt.NumValue instead of the aliasing Num view).
            bool redefined = LinkageRoots.Any(r => !ReferenceEquals(r, item)
                && r.RedefinesTargetName is { } t
                && string.Equals(t, item.CobolName, StringComparison.OrdinalIgnoreCase));
            // ⛔ The elementary/group test here is STRUCTURAL (no subordinate entries), not `IsElementary`
            // (Pic is not null). CallBindLinkage runs inside BindDeclarations, BEFORE the pipeline's
            // UsageInheritancePass, and a level-01 that wrote a PICTURE-less usage — `01 L USAGE BINARY-SHORT.
            // 05 LA.` — still carries at this moment the elementary profile entry bind synthesized for it,
            // because the subordinates were not parsed yet. §13.18.60.4 GR1 then sheds that profile ("it applies
            // only to each elementary item in the group … and not to the group itself"), so an `IsElementary`
            // verdict taken here went stale: the formal was recorded CARRIER-RESIDENT and ProgramEmitter's
            // resident arm dereferenced `f.Item.Pic!` on a group whose Pic was, correctly, null — a compiler
            // NullReferenceException on legal source. The structural test cannot go stale: the forest under this
            // root is complete when CallBindLinkage runs. kb/Work PB495.
            // ⛔ AN ADDRESSED FORMAL IS NOT RESIDENT (kb/Work PB1019). ISO §8.4.3.11.3 SR1 admits a linkage-section
            // item as identifier-1 of ADDRESS OF, and §8.4.3.11.4 GR1 makes the result "the address of
            // identifier-1" — an address the BASED machinery dereferences to a StorageCell. A resident formal's
            // storage is the caller's, reached only through the carrier's accessor, so it has no cell to address;
            // forcing one used to build the cell's name from the accessor text (`_scell___lnkp0.Value` — Roslyn
            // CS1003 on conforming source). An addressed formal therefore takes the round-trip arm every group
            // formal already takes: its storage is a callee-local cell (PtrBindBasedAndAddressables forces it),
            // the caller's image copied in at entry and out at return — §14.2.3 GR8's "as if the formal parameter
            // occupies the same storage area as the argument", realized at the activation boundary.
            bool resident = item.Children.Count == 0 && item.IsElementary
                && !redefined && item.Pic is { IsFloat: false }
                && !addressed.Contains(item.CobolName ?? "");
            if (resident)
            {
                // The item's C# "path" becomes the carrier's Value accessor: every Place built over it reads
                // and writes the caller's storage directly (MemberPlace path text — no new Place subtype).
                item.CsName = carrier + ".Value";
                _callSuppressedRootFields.Add(item.CsName);
            }
            _linkageFormals.Add(new LinkageFormal(item, pos, carrier, resident, byValue, optional));
            pos++;
        }

        // PROCEDURE DIVISION RETURNING (§14.2) + RAISING (§14.2.2) — the COBOL-2002 introduction gates are now
        // VersionConformancePass ParseArm.VisitReturning/RaisingClause (14g.4, recognition, InMethodDefinition-guarded
        // so only PROGRAM-unit PDs gate — this CallBindLinkage runs for program units only, matching that scope). The
        // RETURNING §14.2.3 GR6/SR1 resolution + the RAISING EC semantics (StatementBinder.Exceptions) stay bind-time.
        if (pd.returningClause()?.dataReference() is { } rref)
        {
            // §14.2.3 GR6: the returning item's storage is allocated IN THE ACTIVATED element — it stays an
            // ordinary callee-local field; its value transfers out at termination (GR7, the Call copy-out).
            LinkageReturning = FindLinkageRoot(rref.GetText());
            if (LinkageReturning is null)
                Edition.Error("COBOLNET0888",
                    $"PROCEDURE DIVISION RETURNING item '{rref.GetText()}' is not a level-01/77 LINKAGE SECTION "
                    + "item (ISO §14.2.2 SR1)");
            else
                header.CheckReturning(rref.GetText(), LinkageReturning);
        }

        AnyLengthValidateUnit();
    }

    /// <summary>The ANY LENGTH placement sweep of this unit's forest (ISO §13.18.2.3 SR2/SR3/SR4) — runs at the
    /// end of <see cref="CallBindLinkage"/>, when the unit's sections AND its PD-header formals are bound but no
    /// method data has (methods sweep in <c>OoBindMethodData</c>). SR2: an elementary level-1 LINKAGE entry of a
    /// FUNCTION or a CONTAINED program — never an outermost program (the §13.18.2.3 NOTE: a prototype-less CALL
    /// cannot associate arguments with an ANY LENGTH formal) and never the object/factory paragraph of a class.
    /// SR3 (contained program): referenced in the PD header as a formal WITH THE BY REFERENCE PHRASE (stated
    /// or implied — a BY VALUE formal does not qualify, SR3a) or as the RETURNING item. SR4 (function): as a
    /// BY REFERENCE formal only — a function's ANY LENGTH RETURNING is illegal.
    /// A violation clears <see cref="DataItem.IsAnyLength"/> (the IsBased discipline: the item binds as its
    /// ordinary one-character shape under an already-failed compile — never a half-varying state).</summary>
    private void AnyLengthValidateUnit()
    {
        foreach (var root in Roots)
        {
            if (!root.IsAnyLength) continue;
            using var _ = Edition.At(root);
            string where = $"data item '{root.CobolName ?? "FILLER"}'";
            if (!LinkageRoots.Contains(root))
                Edition.Error("COBOLNET1542", $"{where}: the ANY LENGTH clause may be specified only in an "
                    + "elementary level 1 entry in the LINKAGE SECTION (ISO §13.18.2.3 SR2)");
            else if (root.IsGroup)
                Edition.Error("COBOLNET1542", $"{where}: the subject of an ANY LENGTH clause shall be "
                    + "ELEMENTARY — this entry has subordinate items (ISO §13.18.2.3 SR2)");
            else if (OoIsClassUnit)
                // This binder's own LINKAGE is the OBJECT/FACTORY paragraph's, never a method's (method
                // linkage binds via OoBindMethodData into OoMethodBinding.LinkageRoots, swept there).
                Edition.Error("COBOLNET1542", $"{where}: the ANY LENGTH clause may be specified only in the "
                    + "linkage section of a function, a contained program, or a method that is not a property "
                    + "method — not in a factory or instance definition (ISO §13.18.2.3 SR2)");
            else if (!UnitIsContained && !UnitIsFunction)
                Edition.Error("COBOLNET1542", $"{where}: the ANY LENGTH clause may not be specified in an "
                    + "OUTERMOST program — only in a function, a contained program, or a method "
                    + "(ISO §13.18.2.3 SR2 and its NOTE)");
            else
            {
                // SR3a/SR4 require the BY REFERENCE phrase (stated or implied) — a BY VALUE formal never
                // qualifies (its length is fixed by the value copy, not the caller's argument).
                bool formal = LinkageFormals.Any(f => ReferenceEquals(f.Item, root) && !f.ByValue);
                bool returning = ReferenceEquals(LinkageReturning, root);
                if (UnitIsFunction ? !formal : !(formal || returning))
                    Edition.Error("COBOLNET1542", UnitIsFunction
                        ? $"{where}: in a FUNCTION the subject of an ANY LENGTH clause shall be referenced in "
                          + "the procedure division header as a formal parameter with the BY REFERENCE phrase "
                          + "(ISO §13.18.2.3 SR4)"
                        : $"{where}: in a contained program the subject of an ANY LENGTH clause shall be "
                          + "referenced in the procedure division header as a BY REFERENCE formal parameter or "
                          + "as the RETURNING item (ISO §13.18.2.3 SR3)");
                else if (returning && !formal)
                    // SR3b-legal, staged LOUD (never a silently-wrong length): the return crossing delivers a
                    // VALUE — it cannot carry the ACTIVATOR's receiver length that GR1 fixes n from. Deferred
                    // with the ANY-LENGTH-RETURNING wave; the formal-parameter leg is fully implemented.
                    Edition.Error(DiagnosticCatalog.AnyLengthReturning, $"{where}: ANY LENGTH on the PROCEDURE "
                        + "DIVISION RETURNING item is recognized (ISO §13.18.2.3 SR3b) but not yet implemented "
                        + "(the ANY-LENGTH-RETURNING wave); ANY LENGTH formal parameters are fully supported");
                else
                    continue;   // conformant — keep the flag
            }
            root.IsAnyLength = false;
        }
    }

    private DataItem? FindLinkageRoot(string name) =>
        LinkageRoots.FirstOrDefault(r => string.Equals(r.CobolName, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Collect the roots whose EXTERNAL / GLOBAL clause the clause-placement screen ADMITTED
    /// (<see cref="DataItem.HasExternalClause"/> / <see cref="DataItem.HasGlobalClause"/> — ISO §13.18.22.3 SR1,
    /// §13.18.27.3 SR1 b), §13.16.3 SR5–SR7) and re-base each EXTERNAL record onto a run-unit <c>ExternalStore</c>
    /// cell. Runs at the END of <c>Bind</c>
    /// (after REDEFINES classification) so an EXTERNAL record that is also a redefines anchor folds its whole
    /// class onto the shared backing. The mechanism is the existing Tier-B string-canonical machinery: the
    /// backing becomes a <c>ref</c>-property over the external cell, every member an (offset,width) window —
    /// ONE storage copy per name per run unit (§8.6.7), not reset by CANCEL (§14.9.5 GR8). FD-level EXTERNAL
    /// joins here too: the FD's record area is EXTERNAL record data (ISO §13.18.22.4 GR4b — one record area per
    /// run unit, shared by every program describing the file), re-based through the SAME mechanism with the cell
    /// keyed by the file's externalized name (GR5 — so two programs' differently-named records over one EXTERNAL
    /// FD still share the one area; IC227A). FD-level GLOBAL is handled by the emitter's containment merge
    /// (<c>CallBindUnit</c> — §13.18.30 makes the file-name and record-names global names).
    /// </summary>
    internal void CallBindExternalAndGlobal(Core.ProgramUnitContext program)
    {
        // ⛔ READ FROM THE BOUND ITEMS, NEVER FROM THE PARSE TREE (kb/Work PB518 / PB519). The clause-placement
        // screen (DataBinder.ClausePlacement.cs) has already ADMITTED or REFUSED every EXTERNAL / GLOBAL clause at
        // its entry — level 1 (§13.16.3 SR6, §13.18.22.3 SR1, §13.18.27.3 SR1 b)), the data-name format (SR7), the
        // section (EXTERNAL: working-storage only), and EXTERNAL×REDEFINES/BASED (SR5) — and only an admitted
        // clause sets HasExternalClause / HasGlobalClause. The parse-tree scan this replaces re-derived the rule
        // as `continue` filters, so a misplaced clause was accepted and silently annulled; it never read the FILE
        // SECTION, so a GLOBAL record there (legal per SR1 b)) was UNDEFINED in every containee; and it re-based
        // an EXTERNAL REDEFINES entry, folding the redefines ANCHOR onto the external cell and discarding its VALUE.
        // The population is every residence SR1 b) admits: working-storage, local-storage, linkage and file.
        foreach (var item in WorkingStorageRoots.Concat(LocalStorageRoots).Concat(LinkageRoots)
                     .Concat(Files.SelectMany(f => f.Records)))
        {
            if (item.HasGlobalClause && !_callGlobalRoots.Contains(item)) _callGlobalRoots.Add(item);
            if (!item.HasExternalClause) continue;
            // §13.16.3 SR5 makes this unreachable (the screen refuses EXTERNAL beside REDEFINES), and it must STAY
            // unreachable: re-basing a redefinER re-homes its whole class — the anchor included — onto the cell.
            if (item.RedefinesTargetName is not null)
                throw new InvalidOperationException(
                    $"EXTERNAL record '{item.CobolName}' also redefines '{item.RedefinesTargetName}' — the §13.16.3 SR5 "
                    + "clause-placement screen should have refused its EXTERNAL clause (DataBinder.ClausePlacement.cs)");
            CallMakeExternal(item);
        }

        // Records EXTERNAL BY TYPE (ISO §13.18.22 GR3; P10 Step 16): a record whose TYPE clause names an
        // EXTERNAL type declaration is itself external "subject to the same rules" — ExpandType marked it
        // (the record's own entry carries no EXTERNAL clause, so the parse-tree scan above cannot see it).
        // Re-based through the SAME cell mechanism, keyed by the record's own name (GR5). An entry that ALSO
        // carries its own EXTERNAL clause was already re-based by the scan above — skip the double registration.
        foreach (var item in Roots)
            if (item is { ExternalFromType: true, HasExternalClause: false, CobolName: not null })
                CallMakeExternal(item);

        // EXTERNAL FDs: the record area is run-unit storage keyed by the externalized FILE name (§13.18.22.4
        // GR4 b) / GR5 — the records of every describer alias it). The GR6 same-byte-count conformance check across
        // describers is EC-band work (§14.8.4) — ExternalStore.Describe.
        // ⛔ THE WHOLE AREA, BOTH HALVES (kb/Work PB1026; determination D-FRA). The CHARACTER half — every record
        // with a window, already one implicit-redefinition class (LinkImplicitRecordArea) — is ONE cell keyed
        // "FD::<name>", re-based through its anchor. Each OUT-OF-LINE record (FileModel.IsOutOfLineRecord — a
        // dynamic-length record, a variable-length group, a pointer-class record) is not part of that class, so it
        // gets a cell of its OWN, keyed "FD::<name>#<k>" by its ordinal k among the file's out-of-line records in
        // declaration order: the key a describer can compute from its own FD (GR5 externalizes the FILE name, never
        // a record name — two describers name their records as they like, IC227A), and the cell carries the
        // record's content and its dynamic-length items (StorageCell.DynAt) or its pointer value (SlotAt), so GR4 b)'s
        // "the data contained in all record description entries" is ONE copy per run unit. It used to be staged
        // loud as COBOLNET0899: the character half alone would have split the area in two.
        foreach (var file in Files)
            if (file is { IsExternal: true, ExternalName: { } extName } && file.Records.Count > 0)
            {
                if (file.CharacterAnchor is { } anchor) CallMakeExternal(anchor, "FD::" + extName);
                int k = 0;
                foreach (var record in file.Records.Where(FileModel.IsOutOfLineRecord))
                    CallMakeExternal(record, $"FD::{extName}#{++k}");
            }

        // GLOBAL FDs: the record-names of a GLOBAL FD are GLOBAL names (ISO §13.18.30 — the file-name and the
        // record-names described subordinate to the FD are global names): the records join the GLOBAL roots so
        // contained programs reach the OWNER's record area through the standard containment bridges
        // (§13.18.27 GR2 — container storage, contained visibility). The file-NAME half of the rule is the
        // emitter's FilesByName containment merge (CallBindUnit).
        foreach (var file in Files)
            if (file.IsGlobal)
                foreach (var rec in file.Records)
                    if (!CallGlobalRoots.Contains(rec))
                        _callGlobalRoots.Add(rec);

        CheckExternalizedNameUniqueness();
    }

    /// <summary>ISO §13.18.22.3 syntax rule 2: <i>"In the same source element, the externalized name of the
    /// subject of the entry that includes the EXTERNAL clause shall not be the same as the externalized name of
    /// any other entry that includes the EXTERNAL clause."</i>
    /// <para>⛔ THE RULE ONLY BECAME VIOLABLE WHEN THE `AS literal-1` PHRASE GAINED A GRAMMAR (kb/Work PB511):
    /// without it every externalized name was §13.18.22.4 GR5's default — the subject's own data-name or
    /// file-name — and two entries in one source element cannot share one of those (§8.4.2.2 already rejects
    /// the duplicate declaration). With the phrase in, two entries CAN name one cell, and the consequence is
    /// not a diagnostic-quality one: the run-unit <c>ExternalStore</c> keys by exactly this name, so two
    /// differently-shaped records would silently alias one another's storage — a wrong answer, not a
    /// rejection. That is why this is screened rather than assumed.</para>
    /// <para>The population is the entries GR5 gives an externalized name to — file connectors and records.
    /// A level-1 TYPE DECLARATION may also carry the EXTERNAL clause (SR1), but GR5 names only "the file
    /// connector or record", and GR3 externalizes the records that REFERENCE such a type under their own
    /// names, so a type declaration's literal-1 has no externalized name to collide with.</para>
    /// <para>⛔ ONE COMPARISON, THE RUN-UNIT STORE'S (kb/Work PB1236): case-INSENSITIVE. The default externalized name
    /// is the subject's data-name (§13.18.22.4 GR5), and a data-name's case never matters (§8.3.2), so
    /// <c>ExternalTable</c> keys its cells and describers with <see cref="StringComparer.OrdinalIgnoreCase"/> — that
    /// is what makes <c>01 XV EXTERNAL</c> in one program and <c>01 Xv EXTERNAL</c> in another ONE record. The
    /// uniqueness screen this method is protects exactly that store, so it must ask the same question: the
    /// ordinal comparison it used to make let <c>EXTERNAL</c> and <c>EXTERNAL AS "a"</c> pass the screen and then
    /// silently alias one cell of the first-created width. §8.3.2.2 2) leaves the formation of an externalized name
    /// to the implementor, which makes one comparison a choice — and the store's is the one already load-bearing.</para>
    /// </summary>
    private void CheckExternalizedNameUniqueness()
    {
        // Almost every source element declares no EXTERNAL subject at all; this pass runs once per unit,
        // so it costs nothing when there is nothing to compare.
        if (_callExternalBackings.Count == 0 && !Files.Any(f => f.IsExternal)) return;
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);   // the ExternalTable's own comparer
        void Claim(string externalized, string where, DiagnosticCursor? at)
        {
            if (seen.TryGetValue(externalized, out var prior))
            {
                using var _ = Edition.At(at ?? Edition.Cursor);
                Edition.Error(DiagnosticCatalog.ExternalizedNameNotUnique,
                    $"{where}: the externalized name '{externalized}' is already the externalized name of "
                    + $"{prior} in this source element; \"In the same source element, the externalized name of "
                    + "the subject of the entry that includes the EXTERNAL clause shall not be the same as the "
                    + "externalized name of any other entry that includes the EXTERNAL clause\" "
                    + "(ISO §13.18.22.3 SR2). The EXTERNAL clause's AS phrase names the run-unit cell the "
                    + "subject shares (§13.18.22.4 GR5), so two subjects claiming one name would share one "
                    + "storage area.");
                return;
            }
            seen[externalized] = where;
        }

        foreach (var file in Files)
            if (file is { IsExternal: true, ExternalName: { } extName })
                Claim(extName, $"file '{file.CobolName}'", null);
        foreach (var backing in _callExternalBackings)
            if (!backing.ExternalName.StartsWith("FD::", StringComparison.Ordinal))
                Claim(backing.ExternalName, $"data item '{backing.Record.CobolName}'", backing.Record.DeclaredAt);
    }

    /// <summary>Re-base one EXTERNAL record onto the run-unit external cell (see
    /// <see cref="CallBindExternalAndGlobal"/>). <paramref name="externalName"/> overrides the cell key (the
    /// FD-record case keys by the FILE's externalized name, §13.18.22.4 GR5; a WS record keys by its own name).
    /// A record with a national or pointer-class leaf cannot be carried by the shared byte cell — rejected
    /// loud, conformant-but-unimplemented. Every NUMERIC leaf rides it on its pinned byte form (kb/Work
    /// PB164) and every BOOLEAN leaf, USAGE BIT included, rides it on the §8.5.1.6.3 packing (kb/Work
    /// PB231). The population is <see cref="ByteWindowResidueOf"/>'s, never a second list.</summary>
    private void CallMakeExternal(DataItem item, string? externalName = null)
    {
        if (ForceStringCanonical(item, "EXTERNAL record") is not { } cls)
        {
            // The silent-skip left the record as ORDINARY storage — the program ran with its EXTERNAL
            // sharing semantics silently dropped (the W1-test finding, Phase 4a). Loud at bind: the
            // interpolated reason names the actual leaf (the RESIDUE-11 national layout, the bit-packing
            // residue, or a pointer-class leaf with no byte form).
            Edition.Error(DiagnosticCatalog.ExternalRecordNotCellBacked, $"EXTERNAL record '{item.CobolName}' cannot be cell-backed — "
                + $"{item.Class?.RejectReason ?? "unsupported leaf"} — recognized but not yet implemented");
            return;
        }
        // ISO §13.18.22.4 GR5, THE ONE PLACE ITS TWO SENTENCES ARE APPLIED (kb/Work PB511): the cell key is
        // literal-1 when the entry's EXTERNAL clause writes `AS literal-1`, and the record's own data-name
        // otherwise. `externalName` overrides both — it is the FD-record case, whose key is the FILE
        // connector's externalized name (GR4b/GR5's first alternative), already resolved at the FD entry.
        _callExternalBackings.Add(new CallExternalBacking(
            cls.BackingCsName, cls.BackingCellCsName,
            externalName ?? item.ExternalizedAs ?? item.CobolName!.ToUpperInvariant(),
            cls.Width, item));
    }

    /// <summary>The ONE cell-backing forcer (increment-2 factoring of the proven EXTERNAL re-basing —
    /// feedback_one_mechanism_per_job): make <paramref name="item"/>'s class Tier-B StringCanonical with NO stored
    /// member, so a heap-cell-backed <c>ref</c>-property with the class's <see cref="RedefinesClass.BackingCsName"/>
    /// can supply the storage (EXTERNAL records → the run-unit <c>ExternalStore</c> cell; ADDRESS-OF-taken items →
    /// a per-instance <c>StorageCell</c>; BASED items → the pointer-deref bridge). Every byte-form numeric
    /// leaf rides the cell since the Step D arm-1 dissolution (the gate reads THE ONE image predicate);
    /// a national or pointer-class leaf fails it — the class goes Rejected and every reference fails loud
    /// (the caller skips its bridge registration). Returns the forced class, or null on rejection.
    /// <para>The admitted/refused population is <see cref="ByteWindowResidueOf"/>'s — THE ONE byte-window
    /// carriage gate, shared with the REDEFINES classifier (kb/Work PB231).</para></summary>
    internal RedefinesClass? ForceStringCanonical(DataItem item, string what)
    {
        var cls = item.Class;
        if (cls is null)
        {
            cls = new RedefinesClass { Canonical = item };
            cls.Members.Add(item);
            item.Class = cls;
        }

        var leaves = cls.Members.SelectMany(LeavesOf).ToList();
        // ⛔ THE SAME gate as the main classifier — literally, now: ByteWindowResidueOf, ONE predicate for
        // both surfaces (kb/Work PB231). This site was a hand-written ALLOW-list facing ComputeTier's
        // hand-written DENY-list, and every widening had to be applied twice. It was not: the Step D arm-1
        // dissolution widened both (the eighth two-arm instance — ComputeTier admitted COMP/PACKED while this
        // gate refused every non-DISPLAY leaf), but PB203's USAGE BIT widening reached only ComputeTier, so a
        // BASED / EXTERNAL / ADDRESS-OF record with a bit leaf still rejected while its REDEFINES twin ran.
        // §13.18.5.3 SR1/SR2 restrict a BASED subject to "shall not be of class object" and "shall not be a
        // dynamic-length elementary item or a variable-length group"; §14.9.3.3 SR1 asks only for the BASED
        // clause; §13.18.22 conditions EXTERNAL on nothing subordinate at all. A bit leaf is admitted by all
        // three, so the reject was rejects-legal-source — the same argument that retired the negative fixture
        // pb151-based-comp-leaf at the PB164 landing. Its residue clause now names the ACTUAL leaf.
        if ((leaves.Select(ByteWindowResidueOf).FirstOrDefault(r => r is not null)
             ?? cls.Members.Select(VariableLengthCellResidueOf).FirstOrDefault(r => r is not null)) is { } residue)
        {
            cls.Classify(RedefinesTier.Rejected, cls.Width,
                $"{what} '{item.CobolName}' has {residue} — the shared byte cell cannot carry it");
            return null;
        }

        // The sanctioned RE-classification (see RedefinesClass.Classify): the cell re-base overrides any prior
        // stored-member verdict with the cell-backed Tier-B form (§13.18.22.4 GR5).
        // The cell's area is the members' maximum STORAGE extent, in BYTES — §14.9.3.4 GR3's "the amount of
        // storage to be allocated is the number of BYTES required to hold an item as described by data-name-1",
        // and §13.18.44.4 GR1's larger-of-the-two for a multi-member class. Character positions are NOT that
        // number for a NATIONAL leaf (two bytes per position, §13.18.60.4 GR8 / D-N1) — kb/Work PB231.
        cls.Classify(RedefinesTier.StringCanonical, cls.Members.Max(m => m.ByteWidth * (m.Occurs ?? 1)),
            rejectReason: null);
        // ⛔ THE CELL CLAIM, made HERE because this method IS the one cell-backing forcer and its three callers
        // ARE the three cell surfaces: from now on the class has a StorageCell behind its backing
        // (RedefinesClass.BackingCellCsName), which is what a pointer-class member's MANAGED SLOT addresses
        // (kb/Work PB231 — the pointer third; §14.9.3.4 GR9). A REDEFINES class never reaches this line and
        // never gets one, which is exactly why ComputeTier still refuses a pointer leaf.
        cls.IsCellBacked = true;
        foreach (var member in cls.Members)
        {
            AssignClassOffsets(member, 0, cls);
            member.IsCanonical = false;   // NO local stored field — the backing is the cell bridge
        }
        // ⛔ THE CELL'S DYNAMIC-LENGTH HALF (kb/Work PB1026): each dynamic-length leaf rides the cell's dynamic slot
        // numbered by its storage order among the class's dynamic-length leaves (Place.DynSlotWindow;
        // StorageCell.DynAt). It occupies zero bytes of the backing — its ByteWidth — so the class walk above has
        // already laid out the FIXED RUN every variable-length group view of the class composes over.
        int dynOrdinal = 0;
        foreach (var leaf in leaves.Where(DynSlotWindow.CarriedBySlot))
            leaf.ClassDynOrdinal = dynOrdinal++;
        // ⛔ EVERY byte-form numeric leaf windowed over the string backing decodes/encodes its IMAGE — the
        // same rule ClassifyRedefinesClasses applies to a Tier-B class, applied here for the synth class.
        // The predicate MUST be the one CellCapable admits above (THE ONE image predicate): the Step D
        // widening opened the GATE to COMP/PACKED/COMP-5/float/INDEX leaves while this consequence still
        // read the pre-wave DISPLAY-only union, so an admitted non-DISPLAY leaf got a NATIVE storage form
        // over a STRING window — the accessors then spliced a raw long/float into the cell and read window
        // TEXT back into CobolNum.Store, i.e. UNCOMPILABLE generated C# (CS1503/CS0019) on legal source,
        // strictly worse than the bind diagnostic it replaced. The two-arm shape, nine lines apart.
        foreach (var leaf in leaves)
            if (leaf.Pic is { Category: PicCategory.Numeric, HasImageByteForm: true })
                MarkImageForced(leaf);   // the collected image fact
        return cls;
    }

    /// <summary>The variable-length shapes a cell cannot carry (kb/Work PB1026), or null. A dynamic-length item
    /// rides a slot numbered once per ITEM, so one that repeats — under an OCCURS within the record — has no slot
    /// per occurrence; a dynamic-capacity table has no fixed run to window at all. Both are the same shapes a
    /// declared variable-length group's current-extent composer excludes (CONFORMANCE.md A.1 item 57's "a
    /// runtime-length item inside a table element"), so the cell refuses exactly what the struct refuses.</summary>
    private static string? VariableLengthCellResidueOf(DataItem member)
    {
        foreach (var d in DataItem.DescendantsOf(member).Prepend(member))
        {
            if (d.IsDynamicTable)
                return $"a dynamic-capacity table ('{d.CobolName ?? "FILLER"}', ISO §8.5.1.9) with no fixed extent";
            if (DynSlotWindow.CarriedBySlot(d))
                for (var a = d; a is not null && !ReferenceEquals(a, member.Parent); a = a.Parent)
                    if (a.Occurs is not null || a.OccursSpec is not null)
                        return $"a dynamic-length item inside a table element ('{d.CobolName ?? "FILLER"}', ISO "
                            + "§8.5.1.10) with no slot per occurrence";
        }
        return null;
    }

    /// <summary>Seed the unique-id counter so every unit of a multi-program compilation gets a disjoint
    /// <c>_T_n</c> / <c>_P_n</c> band — a nested program class would otherwise SHADOW its container's struct and
    /// profile names, silently re-typing inherited GLOBAL references (the emitter seeds one band per unit).</summary>
    internal void CallSeedUids(int start) => _uidCounter = start;
}
