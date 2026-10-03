// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Passes;
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;
using CobolEdit = CobolNet.Runtime.CobolEdit;
using CobolDynString = CobolNet.Runtime.CobolDynString;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// Builds the bound DATA DIVISION model (a forest of <see cref="DataItem"/> trees, one per 01/77 item) from the
/// parse tree, and indexes every named item for reference resolution. Pure syntactic/semantic analysis — no byte
/// layout; the .NET type IS the storage. (Slice scope: WORKING-STORAGE groups + elementary items with fixed
/// OCCURS recorded; FILE/LINKAGE/LOCAL-STORAGE, level-66/88, and REDEFINES follow in later slices.)
/// </summary>
public sealed partial class DataBinder(EditionContext? edition = null)
{
    private int _fillerCounter;
    private int _uidCounter;

    /// <summary>The targeted-edition context (digit caps, bind-time rejection diagnostics). Defaults to the
    /// latest edition for direct test construction; <c>CompilerDriver</c> always supplies the CLI's
    /// <c>--std</c>.</summary>
    public EditionContext Edition { get; } = edition ?? new EditionContext(2023);

    /// <summary>The group's compile-time <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> resolution (ISO §7.3.23) — queried by
    /// <see cref="ReferenceResolver"/> when it builds a reference-modification Place, to set
    /// <c>RefModPlace.AllowZeroLength</c> from the directive fold at the ref-mod's source line (§8.4.3.3.4 item 5c).
    /// Defaults to <see cref="RefModZeroLengthState.Empty"/> (the OFF default) for direct test construction.</summary>
    public RefModZeroLengthState RefModZeroLength { get; init; } = RefModZeroLengthState.Empty;

    /// <summary>The group's <c>&gt;&gt;COBOL-WORDS</c> override (ISO §7.3.10) — the intrinsic binder resolves a
    /// function-name synonym / removal (EQUATE/UNDEFINE/SUBSTITUTE of an intrinsic-function-name) through it.
    /// Defaults to <see cref="Editions.CobolWordsMap.Empty"/> (no directive) for direct test construction.</summary>
    public Editions.CobolWordsMap CobolWords { get; init; } = Editions.CobolWordsMap.Empty;

    /// <summary>The post-lex token decisions the tree was parsed under (<see cref="BindSession.Retypes"/>; kb/Work
    /// PB655) — every fragment re-parse applies them. <c>None</c> for direct test construction.</summary>
    public Frontend.Parsing.TokenRetypes Retypes { get; init; } = Frontend.Parsing.TokenRetypes.None;

    /// <summary>The group's <c>&gt;&gt;LEAP-SECOND</c> state (ISO §7.3.17; kb/Work PB65) — the intrinsic renderer
    /// passes it to every §15.3 date/time runtime function that reads a seconds subfield or a standard numeric
    /// time form (SECONDS-FROM-FORMATTED-TIME, TEST-FORMATTED-DATETIME, INTEGER-OF-FORMATTED-DATE, FORMATTED-TIME,
    /// FORMATTED-DATETIME, COMBINED-DATETIME).</summary>
    public bool LeapSecond { get; init; }

    /// <summary>The top-level (01/77) items of WORKING-STORAGE, in source order. (READ-ONLY view — P6 Step 5:
    /// the emitter consumes the bound model without a write channel; the binder populates the private backing.)</summary>
    public IReadOnlyList<DataItem> Roots => _roots;
    private readonly List<DataItem> _roots = [];

    /// <summary>True when this binder binds a RECURSIVE-and-not-INITIAL PROGRAM unit or a FUNCTION unit
    /// (functions "are always recursive", ISO §8.6.6 :8821 / §9.4 :12529) that has NO contained programs — the
    /// unit whose WORKING-STORAGE emits STATIC (set once by <c>BinderDriver.BindUnitData</c>, init-only).
    /// <para><b>The §-derivation this flag realizes</b> (storage class × unit kind → copy semantics):
    /// §13.5.4 GR1 — WS of "a program that does not have the initial attribute, a function, a factory, or an
    /// object" is STATIC data; §14.6.2.3.3 — "Static and external data are the only data that are in the
    /// last-used state", so a recursive unit's WS is ONE copy shared across ALL concurrent and successive
    /// activations, placed in initial state only per the §14.6.2.3.2 triggers (first activation in the run
    /// unit; first activation after an activation of an INITIAL container; first activation after CANCEL).
    /// §13.6.4 GR1 — LOCAL-STORAGE is AUTOMATIC data → §14.6.2.3.2 "Automatic data and initial data is placed
    /// in the initial state every time the … program … is activated" (a separate copy per activation).
    /// LINKAGE formals are the activator's storage (§13.7.1), per-activation by construction; EXTERNAL data is
    /// run-unit storage on the ExternalStore (§8.6.7, last-used per §14.6.2.3.3), never per-class static.
    /// An INITIAL program's WS is INITIAL data (§13.5.4 GR2) → per-activation initial state, so the flag keys
    /// on Recursive AND NOT Initial (the two attributes are mutually exclusive anyway, §11.10.3 SR5–6).
    /// A NON-recursive NON-initial program's WS is equally static data, but such a program can never be
    /// concurrently active (§8.6.6 :8823) — the registry's cached singleton instance realizes its last-used
    /// state without statics, byte-identically to the pre-slice emission.</para></summary>
    public bool UnitStaticWs { get; init; }

    /// <summary>True when this unit's internal FILE CONNECTORS are unit-scoped — ONE registration per run
    /// unit, LAST-USED across activations: a RECURSIVE-and-not-INITIAL unit, every FUNCTION included
    /// (§8.6.6). ISO §14.6.2.3.2 action 3 sets internal connectors "to not be in any open mode" only when
    /// data is placed in the INITIAL state, which for a non-INITIAL unit's static data is cases 1–3 (run-unit
    /// start / an INITIAL container's activation / after CANCEL); §14.6.2.3.3 keeps them last-used otherwise.
    /// UNLIKE <see cref="UnitStaticWs"/> this carries NO childless conjunct: the static registration flag is
    /// referenced only from the unit's OWN class (never through an <c>__outer</c> bridge), so contained
    /// programs do not constrain it. kb/Work PB168 — the per-INSTANCE flag re-ran registration on every
    /// fresh RECURSIVE activation and <c>FileRegistry.Register</c> silently REPLACED the live connector
    /// (the depth-2 WRITE answered '42'; the displaced writer's buffer was lost, its handle leaked).</summary>
    public bool UnitStaticFiles { get; init; }

    /// <summary>⛔ THE ONE condition for both EMITTING <c>__ResetStatics</c> (RecordStructEmitter) and
    /// REGISTERING it as the unit's initial-state hook (ProgramEmitter → ProgramTable): static WS storage
    /// exists, or the unit-scoped file-registration flag does — the flag must return to false on the
    /// §14.6.2.3.2 initial-state cases so a run-unit re-run and a post-CANCEL activation re-register their
    /// connectors. The two emitter sites previously mirrored this predicate BY HAND with a comment warning
    /// that divergence is a CS0103 in generated code; reading it here retires the mirroring (kb/Work PB168;
    /// the one-rule-one-place discipline).
    /// <para>The static channel is populated for one kind of storage — a <see cref="UnitStaticWs"/> unit's
    /// WORKING-STORAGE (<see cref="RouteStaticUnitStorage"/>) — so a non-empty channel IS the condition, with no
    /// unit-kind conjunct. (A class's METHOD had a second producer until kb/Work PB1308: a method owns no
    /// WORKING-STORAGE at any edition, §13.5.3 SR1.)</para></summary>
    public bool EmitsStaticReset =>
        StaticRootFields.Count > 0 || StaticBasedBridgeAddrs.Count > 0
        || StaticAddressableCells.Count > 0 || StaticIndexCells.Count > 0
        || (UnitStaticFiles && Files.Count > 0);

    /// <summary>The unit's WORKING-STORAGE SECTION roots, in source order — the subset of <see cref="Roots"/>
    /// whose storage class is decided by §13.5.4 (static/initial data), captured at bind so the static-WS
    /// routing (<see cref="RouteStaticUnitStorage"/>) and the emitter's <c>__ResetStatics</c> never guess from
    /// the mixed forest (FILE records and compiler temps share <see cref="Roots"/>). The ONE walk
    /// <c>__ResetStatics</c> emits from; each arm there is gated on the static-channel membership, so an
    /// automatic-storage root in this list contributes nothing.</summary>
    public IReadOnlyList<DataItem> WorkingStorageRoots => _workingStorageRoots;
    private readonly List<DataItem> _workingStorageRoots = [];

    /// <summary>The unit's LOCAL-STORAGE SECTION roots, in source order (ISO §13.6 — automatic data,
    /// §13.6.4 GR1). Emitted as ordinary INSTANCE fields: for an INITIAL or RECURSIVE unit the fresh instance
    /// per activation IS the §14.6.2.3.2 per-activation initial state; for a cached-singleton unit the emitted
    /// <c>Call</c> entry re-initializes them (ProgramEmitter) — automatic data is in initial state on EVERY
    /// activation regardless of the unit's attributes.</summary>
    public IReadOnlyList<DataItem> LocalStorageRoots => _localStorageRoots;
    private readonly List<DataItem> _localStorageRoots = [];

    /// <summary>
    /// Every named item, keyed by COBOL name (case-insensitive) → the list of items with that name. COBOL permits
    /// duplicate data-names disambiguated only by qualification (OF/IN), so this is a MULTIMAP — a single-valued
    /// dictionary would silently drop all but the last (a latent wrong-item bug; COBOLNET_DESIGN §3.5).
    /// </summary>
    public Dictionary<string, List<DataItem>> ByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>THE UNIT'S INDEX-NAME NAMESPACE (kb/Work PB919): every INDEXED BY declaration of this unit's own
    /// data division (<see cref="IndexNameRegistry.Own"/> — the <c>long</c> cells it emits, 1-based occurrence
    /// numbers, COBOLNET_DESIGN §3.5), plus the GLOBAL tables' declarations of every containing program
    /// (§8.4.6.2.3), registered beside their tables' data-names by the GLOBAL-root inheritance. A reference resolves
    /// through <see cref="Model.SymbolTable.IndexCandidates"/> — counted, never keyed by spelling alone.</summary>
    public IndexNameRegistry IndexNames { get; } = new();

    /// <summary>Declare the index-names of <paramref name="item"/>'s INDEXED BY phrase in the scope being bound —
    /// the method overlay while a METHOD's data binds (§11.7.4 GR5 privacy), else this unit — each with its OWN
    /// cell, named off the table's session-unique <see cref="DataItem.Uid"/> so it can never collide with a
    /// container's bridged cell or with a sibling declaration of the same spelling (kb/Work PB919). Called from
    /// <see cref="RegisterName"/>, the one funnel every referenceable item (a TYPE clone included) passes through,
    /// so a copied table can never share its original's cell.</summary>
    private void RegisterIndexes(DataItem item)
    {
        for (int k = 0; k < item.IndexNames.Count; k++)
        {
            var decl = new IndexDeclaration(item.IndexNames[k], item, $"_IX_{item.Uid}" + (k == 0 ? "" : $"_{k}"));
            item.Indexes.Add(decl);
            if (_bindingMethodScope is { } ms) ms.IndexNames.Declare(decl);
            else IndexNames.Declare(decl);
        }
    }

    /// <summary>Level-88 condition-names (case-insensitive) → the conditions with that name (a list, since names
    /// may be duplicated under different parents and disambiguated by qualification).</summary>
    public Dictionary<string, List<Condition88>> Conditions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>OCCURS DYNAMIC <c>CAPACITY IN data-name-3</c> register-names (case-insensitive) → the owning
    /// dynamic-table <see cref="DataItem"/> (ISO §13.18.38 GR15 / §8.5.1.9.1; data-model D9). The register is
    /// IMPLICITLY defined at the OCCURS entry (SR30) — it is NOT in <see cref="ByName"/>; the resolver consults
    /// this map to build a <see cref="CapacityRegisterPlace"/> (a view over the table's <c>Capacity</c>). Populated
    /// by the post-build <see cref="DynamicResolve"/> pass. (READ-ONLY view — P6 Step 5; the getter carries the
    /// P6 Step-6 watermark gate — a read before <see cref="Passes.PassPhase.OccursResolved"/> throws loud, the
    /// "read a null CapacityRegister" silent-miscompile class made structural.)</summary>
    public IReadOnlyDictionary<string, DataItem> CapacityRegisters
    {
        get { Require(PassPhase.OccursResolved, "CapacityRegisters"); return _capacityRegisters; }
    }
    private readonly Dictionary<string, DataItem> _capacityRegisters = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>REPORT SECTION <b>sum counter</b> names (case-insensitive) → every counter that carries the name,
    /// with its owning report (ISO §13.18.54.4 GR5 — "If a data-name immediately follows the level number in the
    /// entry containing the SUM clause, the data-name is the name of the sum counter, not the name of the
    /// associated printable item"). The counter is IMPLICITLY defined by its entry and is NOT in
    /// <see cref="ByName"/>; <see cref="ReferenceResolver"/> consults this map to build a
    /// <see cref="Model.ReportSumCounterPlace"/> so §13.18.54.4 GR12 — "It is permissible for procedure division
    /// statements to alter the content of sum counters" — has somewhere to land (kb/Work PB840).
    /// <para>⛔ A LIST PER NAME, NOT ONE ENTRY (kb/Work PB882). GR1 gives every ENTRY its own counter and nothing
    /// makes two entries' data-names distinct: §8.4.2.2.1's uniqueness requirement bites on a REFERENCE, not on a
    /// declaration nobody names. A scalar column here would be the same last-write-wins defect the runtime just
    /// shed; instead a duplicated name is legal to DECLARE and, unless a report-name qualifier resolves it,
    /// diagnosed where it is REFERENCED.</para>
    /// (READ-ONLY view — P6 Step 5; the getter carries the P6 Step-6 watermark gate.)</summary>
    public IReadOnlyDictionary<string, List<(ReportModel Report, ReportSumModel Sum)>> SumCounters
    {
        get { Require(PassPhase.FilesResolved, "SumCounters"); return _sumCounters; }
    }
    private readonly Dictionary<string, List<(ReportModel Report, ReportSumModel Sum)>> _sumCounters
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>TYPEDEF type declarations (case-insensitive) → the template root <see cref="DataItem"/> (ISO
    /// §13.18.58; data-model D17). The template is built by <see cref="BindEntries"/> but kept OFF <see cref="Roots"/>
    /// and <see cref="ByName"/> (it allocates no storage; its subordinate names are not globally referenceable,
    /// GR1/GR2). A <c>TYPE IS type-name</c> reference clones this subtree into the referencing entry in the post-build
    /// <see cref="ExpandTypes"/> pass — which runs before <see cref="BindResolve"/>, so every resolution pass sees the
    /// clone (the invariant the OO compiler-temp clone already relies on).</summary>
    public Dictionary<string, DataItem> TypeDecls { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Group items used as a WHOLE character-image operand (MOVE/DISPLAY/compare/ACCEPT/record I-O/whole-group
    /// CORRESPONDING pair/boundary formal) — collected AFTER binding by <see cref="Passes.UsageCollectionPass"/>'s
    /// typed walk of the bound tree (+ the structural FILE-record and boundary-formal sources), NOT by
    /// <see cref="ReferenceResolver"/> mid-resolve (which over-collected every RESOLVED group; PHASE-05 Step 5). The
    /// <c>StorageFormPass</c> consults this to decide which numeric-DISPLAY leaves must store their character
    /// image (ISO §14.9.25.4 MOVE GR4 — a whole-group move fills without conversion; see <see cref="DataItem.StoreAsImage"/>).
    /// </summary>
    public HashSet<DataItem> WholeGroupReferenced { get; } = [];

    /// <summary>ELEMENTARY items a CHARACTER CHANNEL can write — collected after binding by
    /// <see cref="Passes.UsageCollectionPass"/> beside <see cref="WholeGroupReferenced"/>, and promoted by
    /// <c>StorageFormPass</c> exactly as a whole-referenced group's leaves are (kb/Work PB992). A channel that
    /// delivers CHARACTERS rather than a value — §14.9.25.4 GR4's group move into an elementary receiver, and
    /// §14.2.3 GR8's shared storage between a BY REFERENCE argument and its formal parameter, where §14.8.2.3.2
    /// rule 1 admits a formal of any description "of the same length", and the CALL RETURNING receiver — can
    /// deposit content that is not a numeric value, and a native carrier holds only a value.</summary>
    public HashSet<DataItem> CharacterChannelItems { get; } = [];

    /// <summary>The COLLECTED image-storage facts (PHASE-05 Step 7 — the flag-write sites become fact records):
    /// every elementary item some bind-time rule forces to store its CHARACTER IMAGE, recorded at the exact
    /// moment the legacy <c>StoreAsImage = true</c> writes fired — Tier-B/CALL-cell REDEFINES leaves (resolve),
    /// FILE-record leaves (resolve), report print faces (resolve), figurative-fill and ref-mod-store receivers
    /// (procedure bind). <see cref="Passes.StorageFormPass"/> unions this with the compiler-temp re-sync and the
    /// whole-group promotion to compute <see cref="DataItem.Storage"/> WITHOUT reading the mutable flag; the
    /// bind-time <c>NumericImagePlace</c> wrap decision reads it with the same mid-bind timing the flag had.</summary>
    public IReadOnlySet<DataItem> ImageForcedItems => _imageForcedItems;
    private readonly HashSet<DataItem> _imageForcedItems = [];

    /// <summary>Record one image-storage fact (the ONE write channel for <see cref="ImageForcedItems"/>).</summary>
    internal void MarkImageForced(DataItem item) => _imageForcedItems.Add(item);

    /// <summary>The BIND-TIME image-storage query (P5.7): true when <paramref name="item"/> is already known —
    /// at THIS point of binding — to store its character image. Reads the collected facts with EXACTLY the
    /// mid-bind timing the deleted mutable flag had (a fact recorded by an earlier statement's bind is visible
    /// to a later statement's; one recorded later is not — the pre-P5.7 order dependency, preserved verbatim
    /// and noted for the P7 lazy-place redesign). Post-bind consumers read <see cref="DataItem.StoreAsImage"/>
    /// (the Storage projection) instead.</summary>
    internal bool IsImageBackedEarly(DataItem item) => _imageForcedItems.Contains(item);

    /// <summary>The fully-parsed OPTIONS paragraph (ISO §11.9), program-level context for every later pass — the
    /// binder applies DEFAULT ROUNDED today (a bare ROUNDED phrase uses <see cref="OptionsModel.DefaultRounding"/>);
    /// the remaining clauses are captured for the features that will consume them. Defaults when no OPTIONS.</summary>
    public OptionsModel Options { get; internal set; } = OptionsModel.Default;

    /// <summary>Seed the §11.9.4 GR1 baseline for a CLASS-CHANNEL bind (kb/Work PB135): OoDriver computes the
    /// class/object/factory-level model from the skeleton paragraphs and hands it here before BindDeclarations,
    /// whose own Bind over the synthetic unit (no identification body) then folds it as the baseline.</summary>
    internal void CallInheritOptions(OptionsModel m) => _inheritedOptions = m;

    /// <summary>All SELECTed files (the SELECT clause joined with its FD records), in source order.
    /// (READ-ONLY view — P6 Step 5. The bind-phase file-connector qualification mutates the FileModel ELEMENTS,
    /// which a read-only list does not prevent — element immutability is a later data-model-track item.)</summary>
    public IReadOnlyList<FileModel> Files => _files;
    private readonly List<FileModel> _files = [];

    /// <summary>The files keyed by COBOL file-name (case-insensitive), for the binder to resolve OPEN/READ/CLOSE
    /// targets and to map a WRITE/REWRITE record-name back to its owning file.</summary>
    public Dictionary<string, FileModel> FilesByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The files this unit's OWN file control entries select, by file-name — the subjects of ISO §12.4.5.2
    /// SR2 ("A given file-name may be specified in only one SELECT clause within a factory, function, object, or
    /// program") and SR3 (each needs an FD or SD in the same file section). It is not <see cref="FilesByName"/>,
    /// which also holds the models an FD or SD synthesizes, and it leaves out a class-level FILE-CONTROL, which
    /// §12.4.3 SR1 forbids outright (kb/Work PB1076) and which belongs to no factory or object (kb/Work PB1077).</summary>
    private readonly Dictionary<string, FileModel> _selectedFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every file of every CONTAINING program that is NOT a global name here — its FD has no GLOBAL clause
    /// (ISO §13.18.27.3 SR1 d)), so its file-name is not in <see cref="FilesByName"/>. It is still reachable through a
    /// record: a file-section level-1 entry may carry its own GLOBAL clause (SR1 b)), and the record-name is then
    /// visible here. Read ONLY by <c>StatementValidation.ResolveRecordName</c>, to refuse WRITE / REWRITE of such a
    /// record by §14.9.51.3 SR21 / §14.9.35.3 SR3 (kb/Work PB1193); filled by <c>BinderDriver.BindUnitData</c> beside
    /// the GLOBAL FD merge.</summary>
    public List<FileModel> ContainerLocalFiles { get; } = [];

    /// <summary>Names DECLARED in the SCREEN SECTION, which is REFUSED as the declined Annex A.4.2 module
    /// (COBOLNET1560; <see cref="ScreenFacility"/>). Two consumers, both still needed after the refusal:
    /// <list type="number">
    /// <item>kb/Work R32 — a reference to such a name is exempt from the §8.4.2.1 UNDEFINED diagnostic
    /// (COBOLNET1639). The compile already fails on the section; adding "SG is not defined" on top would send
    /// the user hunting a declaration that is right there. "Not defined" and "declared in a refused section"
    /// are different verdicts, and only the second is true.</item>
    /// <item>It is how <c>ACCEPT screen-name-1</c> / <c>DISPLAY screen-name-1</c> are told apart from their
    /// token-identical DEVICE formats (AcceptDisplayBinder.ScreenFormatOf) — the operand's DECLARATION is the
    /// only thing that distinguishes them.</item></list>
    /// ⚠ The posture used to be compile-ACCEPT with a staged runtime loud; kb/Work PB260 measured what that
    /// actually did (a screen record printed to standard output) and made it a refusal.</summary>
    public HashSet<string> ScreenNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when SOURCE-COMPUTER declares WITH DEBUGGING MODE (the X3.23-1985 compile-time debug
    /// switch) — consumed by the declaratives binder to decide the USE FOR DEBUGGING posture: with the switch the
    /// debugging section is compiled AND its procedure-trigger leg is modeled (fires; the DEBUG-ITEM register
    /// resolves), without it the section is comment-treated (VCR Table 7 rows 7.9/7.17).</summary>
    public bool DebuggingModeDeclared { get; private set; }

    /// <summary>The X3.23-1985 <c>DEBUG-ITEM</c> special register and its members (DEBUG-LINE / DEBUG-NAME /
    /// DEBUG-SUB-1/2/3 / DEBUG-CONTENTS), keyed by COBOL name (case-insensitive) → the synthesized alphanumeric
    /// <see cref="DataItem"/> VIEW + its read-only runtime read expression. The register is IMPLICITLY described
    /// (no DATA DIVISION entry, §ISO absent — 1985 debug module, VCR Table 7 row 7.17) — kept OFF <see cref="ByName"/>
    /// / <see cref="Roots"/>; the resolver consults this to build a <see cref="DebugRegisterPlace"/>. Populated by
    /// <see cref="ActivateDebugRegisters"/> when a procedure-subject debugging declarative is collected under
    /// WITH DEBUGGING MODE (empty otherwise — a non-debug program's resolution is unchanged).</summary>
    public IReadOnlyDictionary<string, (DataItem Item, DebugRegisterMember Member)> DebugRegisters => _debugRegisters;
    private readonly Dictionary<string, (DataItem, DebugRegisterMember)> _debugRegisters = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The implicit description of the predefined object reference <c>EXCEPTION-OBJECT</c>
    /// (ISO §8.4.3.6.3 SR2 — "EXCEPTION-OBJECT is implicitly described as class object and category object
    /// reference, as an external data item, and as a universal object reference"; kb/Work PB922), synthesized on
    /// first reference and shared thereafter: §8.4.3.6.4 GR2 gives the run unit ONE instance of it.
    /// <para>Kept OFF <see cref="ByName"/> / <see cref="Roots"/>, exactly like <see cref="DebugRegisters"/> and
    /// the CAPACITY register — it is not storage this program declares or emits; the resolver builds an
    /// <see cref="ExceptionObjectPlace"/> VIEW over the runtime's one instance. It is a DESCRIPTION carrier only,
    /// so every class/category screen (§14.9.25.3 SR1's "shall not be … object", a relation condition's operand
    /// class, an argument's class) reads it as the object-reference item SR2 says it is.</para></summary>
    public DataItem ExceptionObjectRegister => _exceptionObjectRegister ??= new DataItem
    {
        Level = 1,
        CobolName = "EXCEPTION-OBJECT",
        CsName = "__exceptionObject",
        Pic = PicInfo.ObjectReferenceItem(ObjectRefDescriptor.Universal),
        // ⚠ SR2's "as an external data item" is NOT carried as HasExternalClause: that flag drives the
        // §13.18.22 EXTERNAL-store re-basing of a DECLARED record, and this item is never declared, laid out or
        // emitted. What the phrase asserts — one instance shared by the whole run unit (§8.4.3.6.4 GR2) — is
        // already realized by the runtime's single ExceptionState, which is what the place reads.
    };
    private DataItem? _exceptionObjectRegister;

    /// <summary>Register the X3.23-1985 DEBUG-ITEM special-register family (idempotent) — called by the procedure
    /// table builder when it collects a <c>USE FOR DEBUGGING</c> procedure-subject declarative under WITH DEBUGGING
    /// MODE, BEFORE statement binding resolves the DEBUG-* references inside the debugging section. Each member is a
    /// synthesized elementary VIEW of its fixed 1985 width over the program-instance <c>__dbgItem</c>
    /// (<see cref="CobolNet.Runtime.DebugItem"/>) — carried as a STRUCTURAL <see cref="DebugRegisterMember"/> selector
    /// (the C# read text lives in the renderer); the whole-group DEBUG-ITEM reads the concatenated image. DEBUG-SUB-n
    /// carries the S9(4) SIGN LEADING SEPARATE image WIDTH (5) but as an alphanumeric character-image view: the
    /// procedure-trigger leg only ever renders it SPACES, and the subscripted numeric-value population + the
    /// §14.9.25.4 GR6a "sign not moved" MOVE semantics ride the staged data-name leg (COBOLNET1571).</summary>
    internal void ActivateDebugRegisters()
    {
        if (_debugRegisters.Count > 0) return;
        void Reg(string name, int width, DebugRegisterMember member) =>
            _debugRegisters[name] = (new DataItem
            {
                Level = 49,
                CsName = NamingConvention.DebugRegisterName(name),
                CobolName = name,
                Pic = new PicInfo(PicCategory.Alphanumeric, Usage.Display, Length: width, Digits: 0, Scale: 0, Signed: false),
                Uid = _uidCounter++,
            }, member);
        Reg("DEBUG-ITEM", Runtime.DebugItem.GroupWidth, DebugRegisterMember.Item);
        Reg("DEBUG-LINE", Runtime.DebugItem.LineWidth, DebugRegisterMember.Line);
        Reg("DEBUG-NAME", Runtime.DebugItem.NameWidth, DebugRegisterMember.Name);
        Reg("DEBUG-SUB-1", Runtime.DebugItem.SubWidth, DebugRegisterMember.Sub1);
        Reg("DEBUG-SUB-2", Runtime.DebugItem.SubWidth, DebugRegisterMember.Sub2);
        Reg("DEBUG-SUB-3", Runtime.DebugItem.SubWidth, DebugRegisterMember.Sub3);
        Reg("DEBUG-CONTENTS", Runtime.DebugItem.ContentsWidth, DebugRegisterMember.Contents);
    }

    /// <summary>The compilation group's pass-1 class symbol table (OO deep-dive D1) — set by the run-unit
    /// emitter BEFORE <see cref="Bind"/> so a typed <c>USAGE OBJECT REFERENCE class-name</c> validates its
    /// declared class (§13.18.60.4) against classes defined anywhere in the group. Null only in unit-test
    /// direct construction, which then behaves as an empty group (every typed reference is unknown-class).</summary>
    public OoClassTable? OoClasses { get; set; }

    /// <summary>The unit's REPOSITORY PROPERTY specifier names (§12.3.8) — the §8.4.3.9.3 SR1 gate for
    /// object-property references (case-insensitive per §8.3.2).</summary>
    /// <para>Keyed by property-name-1 → the name the property is known by in the declared classes: literal-4 when
    /// the specifier writes <c>AS literal-4</c> (§12.3.8.2, kb/Work PB974), otherwise property-name-1 itself.</para>
    internal Dictionary<string, string> OoRepositoryProperties { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The unit's REPOSITORY PROGRAM specifiers (ISO §12.3.8.2's program-specifier, <c>PROGRAM
    /// program-prototype-name-1 [AS literal-3]</c>) keyed by program-prototype-name — the ONE declaration surface
    /// for a program-prototype-name, and therefore the §14.9.4.3 SR16 / §14.9.5.3 SR3 precondition for every
    /// Format-2 CALL and prototype CANCEL (kb/Work PB237). §8.3.2 case-insensitive. The SPECIFIER is collected
    /// here (syntax); §12.3.8.3 SR15's self/containing-program ignore and §12.3.8.4 GR10's resolution against the
    /// compilation group need the unit's identity and its siblings, so they run in
    /// <c>BinderDriver.ProgramPrototypesOf</c>, exactly as the function twin resolves in BuildUserFunctionTable.</summary>
    internal Dictionary<string, ProgramSpecifier> ProgramSpecifiers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The unit's REPOSITORY user-function specifiers (§12.3.8 — <c>FUNCTION function-prototype-name</c>
    /// WITHOUT the INTRINSIC phrase): the precondition for a user-function reference, and per §12.3.8.2 GR12
    /// (:14885) the declaration that makes the name refer to the USER-DEFINED function "and not to an intrinsic
    /// function of the same name" — so the binder's user-function dispatch precedes the intrinsic catalog.</summary>
    internal HashSet<string> UserFunctionNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The unit's user-defined-function specifiers by function-prototype-name-1 → EXTERNALIZED name:
    /// literal-5 when <c>AS literal-5</c> is written (§12.3.8.4 GR11 NOTE 2 — "Literal-5, if specified, is the
    /// externalized name of the function prototype; otherwise, the externalized name is function-prototype-name-1"),
    /// otherwise the name itself. GR11's search of the compilation group runs in
    /// <c>BinderDriver.UserFunctionsOf</c>, the twin of <c>ProgramPrototypesOf</c> (kb/Work PB974).</summary>
    internal Dictionary<string, string> FunctionSpecifiers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The unit's REPOSITORY intrinsic-function specifiers by name (§12.3.8 —
    /// <c>FUNCTION intrinsic-function-name INTRINSIC</c>): the §8.4.3.2 SR2 precondition that lets the word
    /// FUNCTION be OMITTED when referencing that intrinsic (GR13). §8.3.2 case-insensitive.</summary>
    internal HashSet<string> RepositoryIntrinsics { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when the unit's REPOSITORY specifies <c>FUNCTION ALL INTRINSIC</c> (§12.3.8 GR14): the word
    /// FUNCTION may be omitted for EVERY §8.11 intrinsic-function-name in this scope (SR2/GR13). Read only through
    /// <see cref="IsRepositoryIntrinsic"/> — the membership that applies GR14's >>COBOL-WORDS clauses and the edition
    /// — which the SR13 user-word prohibition (<see cref="DeclareUserWord"/>) asks too.</summary>
    internal bool RepositoryAllIntrinsic { get; set; }

    /// <summary>Bind a program unit's DATA DIVISION + the FILE-CONTROL paragraph: the OPTIONS paragraph, the SELECT
    /// clauses, the FILE SECTION records (which share storage with the WORKING-STORAGE roots — they emit as Program
    /// fields), and WORKING-STORAGE; then classify the shared-storage (REDEFINES) classes over the whole forest and
    /// resolve each file's FILE STATUS item.</summary>
    public void Bind(Core.ProgramUnitContext program)
    {
        BindDeclarations(program);
        BindResolve(program);
    }

    /// <summary>The declaration half of <see cref="Bind"/>: OPTIONS/SPECIAL-NAMES/FILE-CONTROL, the FILE /
    /// WORKING-STORAGE / LINKAGE sections, and the PD header formals — everything that ADDS items to the
    /// forest. Split from <see cref="BindResolve"/> so a CLASS unit can bind its METHODS' data sections
    /// (OO deep-dive D3/D6 — <c>DataBinder.Oo.cs</c>) into the same forest BEFORE the post-build passes run
    /// over it (a method item participates in USAGE/SIGN inheritance and object-reference resolution exactly
    /// like program data).</summary>
    internal void BindDeclarations(Core.ProgramUnitContext program)
    {
        // §11.9.4 GR1: a contained program's OPTIONS start from its container's model and override clause by
        // clause (InheritConfiguration set the baseline); an outermost unit starts from the all-defaults model.
        Options = OptionsBinder.Bind(program, Edition, _inheritedOptions);   // captured even when there is no WORKING-STORAGE

        // §12.3.3 SR1: "The configuration section shall not be specified in a program that is contained within
        // another program" — the container's applies (§12.3.4 GR1, InheritConfiguration). Diagnosed, then the
        // section is still walked below so a second diagnostic never masks this one.
        if (UnitIsContained && EnvDivisions(program).Any(env => env.configurationSection() is not null))
            Edition.Error(DiagnosticCatalog.ConfigurationSectionInContainedProgram,
                "a CONFIGURATION SECTION is specified in a program contained within another program; the "
                + "containing program's configuration section applies to it (ISO §12.3.3 SR1 / §12.3.4 GR1)");

        // ARITHMETIC mode validity (§11.9.5 / §8.8.1): NATIVE, STANDARD-DECIMAL, and plain STANDARD are
        // implemented. STANDARD arithmetic (the 2002 mode; obsolete 2014, removed 2023 — Annex E.2 item 21)
        // performs operations in the standard intermediate data item, which for its reachable operands IS the
        // standard DECIMAL form, so STANDARD routes to the same CobolDec engine as STANDARD-DECIMAL
        // (NumericRenderer.StandardDecimal). Floating-point operands participate under BOTH modes through the
        // §8.8.1.5.1 implementor-defined float→SDIDI conversion (CobolDec.FromDouble — the shortest-round-trip
        // decimal identity of the IEEE value); the operations themselves are SDIDI (P10 Step 12).
        // STANDARD was DROPPED by ISO/IEC 1989:2023 (§8.8.1 names only NATIVE/STANDARD-BINARY/STANDARD-DECIMAL)
        // → the pass's arithmetic-standard-2002 dual-window row rejects it at --std 2023 (0807).
        // STANDARD-BINARY is DECLINED — but the screen is NOT here any more: it moved to OptionsBinder's
        // ArithmeticOf, the single point every unit kind's OPTIONS clause is read through. This binder does not
        // see a METHOD's or an INTERFACE's options paragraph, so the twin that stood here left both of those
        // arms silent (kb/Work PB197, measured). Its 2014 introduction edge remains the pass's
        // arithmetic-standard-binary-2014 row.
        // arithmetic-standard-2002 (dual-window; dropped 2023): the pass owns the edition gate (Exec Step E).

        // The '85 debug facility's compile-time switch (X3.23-1985 SOURCE-COMPUTER … WITH DEBUGGING MODE; the
        // clause itself is 0902-gated ≥2002 by the version-conformance pass): its presence decides whether a USE FOR
        // DEBUGGING declarative section is COMPILED (switch present — the object-time switch is permanently off
        // here, so it never triggers) or treated as comment lines (switch absent — the '85 rule). It
        // reads the MODELLED clause (kb/Work PB830 — it used to be a token-text scan of the computerAttributes sink; VCR
        // Table 7 rows 7.9/7.17).
        // ⛔ OR-ed into the value InheritConfiguration copied, never assigned over it: §12.3.5.4 GR1 "All clauses of
        // the SOURCE-COMPUTER paragraph apply to the source unit in which they are explicitly or implicitly specified
        // and to any source unit contained within that source unit", and a contained program cannot restate the
        // clause (§12.3.3 SR1), so inheritance is its only route. An assignment here reset every contained unit to
        // false and compiled its USE FOR DEBUGGING section as comment lines (kb/Work PB1088).
        DebuggingModeDeclared |= EnvDivisions(program).Any(env => env.configurationSection()?.configurationParagraph()
            .Any(p => p.sourceComputerParagraph()?.debuggingModeClause() is not null)
            ?? false);

        // REPOSITORY PROPERTY specifiers (§12.3.8 :14727-14729) — §8.4.3.9.3 SR1 makes a property-specifier
        // a PRECONDITION of every object-property reference in the unit; captured here, checked at the
        // property-reference desugar (0843). FUNCTION specifiers WITHOUT the INTRINSIC phrase declare
        // user-defined functions (§12.3.8.2 GR12 — the name then refers to the user function, never a
        // same-named intrinsic; the `FUNCTION ALL INTRINSIC` alternative carries no functionName and the
        // per-name INTRINSIC form is excluded by its phrase). CLASS/INTERFACE specifiers stay declarative
        // (names resolve through the group-wide pass-1 table).
        // The REPOSITORY's own user-defined words, declared AFTER the loop: a specifier written before
        // `FUNCTION ALL INTRINSIC` is inside its scope all the same (§12.3.8.3 SR13; kb/Work PB1083).
        var specifierWords = new List<(Core.RepositoryEntryContext At, string Word, UserWordKind Kind)>();
        foreach (var re in EnvDivisions(program).SelectMany(env => env.configurationSection()?.configurationParagraph()
                     .Select(p => p.repositoryParagraph()).FirstOrDefault(r => r is not null)
                     ?.repositoryEntry() ?? []))
        {
            // REPOSITORY PROPERTY/INTERFACE/CLASS (§12.3.8, OO): the COBOL-2002 introduction gates are now
            // VersionConformancePass ParseArm.VisitRepositoryEntry (14g.5). The PROPERTY name still registers here for
            // reference resolution; INTERFACE/CLASS names are declarative-only in this loop (the pass-1 table resolves
            // them), so they no longer need a branch.
            // Every specifier's `[ AS literal-n ]` (§12.3.8.2) is screened HERE, once — §12.3.8.3 SR2 (the literal's
            // class) and SR1 (a repeated name shall be specified identically) — by BindSpecifierExternalizedName;
            // what each externalized name RESOLVES to is its consumer's (kb/Work PB974).
            if (re.PROPERTY() is not null && re.propertyName() is { } pn)
            {
                specifierWords.Add((re, pn.GetText(), UserWordKind.PropertyName));
                OoRepositoryProperties[pn.GetText()] =
                    BindSpecifierExternalizedName(re, "PROPERTY", pn.GetText(), "literal-4") ?? pn.GetText();
            }
            // §12.3.8.2's program-specifier (kb/Work PB237): `PROGRAM program-prototype-name-1 [AS literal-3]`.
            else if (re.PROGRAM() is not null && re.programPrototypeName() is { } ppn)
            {
                specifierWords.Add((re, ppn.GetText(), UserWordKind.ProgramPrototypeName));
                BindProgramSpecifier(re, ppn.GetText());
            }
            else if (re.CLASS() is not null && re.className() is { } cn)
            {
                specifierWords.Add((re, cn.GetText(), UserWordKind.ObjectClassName));
                BindSpecifierExternalizedName(re, "CLASS", cn.GetText(), "literal-1");
            }
            else if (re.INTERFACE() is not null && re.interfaceName() is { } inm)
            {
                specifierWords.Add((re, inm.GetText(), UserWordKind.InterfaceName));
                BindSpecifierExternalizedName(re, "INTERFACE", inm.GetText(), "literal-2");
            }
            // §12.3.8.2 user-defined-function-specifier `FUNCTION function-prototype-name-1 [AS literal-5]`.
            else if (re.FUNCTION() is not null && re.INTRINSIC() is null && re.functionName() is [var fn])
            {
                specifierWords.Add((re, fn.GetText(), UserWordKind.FunctionPrototypeName));
                UserFunctionNames.Add(fn.GetText());
                FunctionSpecifiers[fn.GetText()] =
                    BindSpecifierExternalizedName(re, "FUNCTION", fn.GetText(), "literal-5") ?? fn.GetText();
            }
            // FUNCTION … INTRINSIC (§12.3.8): `ALL` (GR14) or a LIST of intrinsic-function-names (§12.3.8.2's
            // `{ intrinsic-function-name-1 } …`, kb/Work PB974) — the §8.4.3.2 SR2 keyword-omission enabler
            // (M2-UDF-4). `FUNCTION ALL INTRINSIC` carries no functionName.
            else if (re.FUNCTION() is not null && re.INTRINSIC() is not null)
            {
                if (re.ALL() is not null) RepositoryAllIntrinsic = true;
                else foreach (var inf in re.functionName())
                {
                    // An intrinsic-function-name-1 is one of the names §12.3.8.3 SR1 compares (kb/Work PB1017).
                    using (Edition.At(re))
                        CheckRepositorySpecification(inf.GetText(), new RepositorySpecification(IntrinsicKind, null, null),
                            inf.GetText());
                    // The CANONICAL name (an EQUATE / SUBSTITUTE synonym names its intrinsic), so every spelling of
                    // the function is a member (RepositoryIntrinsicSpecifier; kb/Work PB1083).
                    RepositoryIntrinsics.Add(FunctionWord.OfToken(inf.Start, CobolWords).Name);
                }
            }
        }
        // ⛔ A specifier that names THIS unit is IGNORED (§12.3.8.3 SR11 for a function-specifier, SR15 for a
        // program-specifier — "references … are to the named … definition and this … specifier is ignored"), so it
        // declares nothing: the unit's own PROGRAM-ID / FUNCTION-ID word is the one declaration of that name
        // (kb/Work PB990 — declaring both made one word a program-prototype-name AND a program-name).
        string? ownName = UnitOwnName(program)?.Word;
        foreach (var (at, word, kind) in specifierWords)
        {
            if (kind is UserWordKind.ProgramPrototypeName or UserWordKind.FunctionPrototypeName
                && ownName is not null && ownName.Equals(word, StringComparison.OrdinalIgnoreCase)) continue;
            using (Edition.At(at)) DeclareUserWord(word, kind);
        }
        DeclareUnitName(program);

        SwitchBindSpecialNames(program);           // SPECIAL-NAMES switch clauses → the external-switch registry (ISO §12.3.7)
        BindFileControl(program);                  // SELECT clauses → FileModels (before the FD records bind)
        BindFileSection(program, _rootNames);      // FD records → Roots + FileModel.Records + the shared-area REDEFINES
        ScreenSelectsHaveDescriptions();           // §12.4.5.2 SR3 — after every FD and SD has attached
        BindReportSection(program);                // RD entries → ReportModels (ISO §13.14; DataBinder.Reports.cs)
        BindIoControl(program);                    // I-O-CONTROL: SAME RECORD AREA → cross-file shared record area (§12.4.6.4 GR2)

        // SCREEN SECTION (ISO §13.9) is an OPTIONAL element of Annex A.4.2, the module docs/CONFORMANCE.md §5
        // records as Not claimed. A.4.1 admits an optional element's syntax ONLY when support is claimed, so the
        // section, every entry in it and every clause inside those entries are refused BY NAME (COBOLNET1560) —
        // ScreenFacility is the one funnel and names the construct the user actually wrote. The declared
        // screen-names are recorded there as well, so a later reference reports "declared in a refused section"
        // rather than the §8.4.2.1 UNDEFINED verdict (kb/Work R32; the differential's syn_screen:221 flip).
        // ⚠ This was a WARNING until kb/Work PB260: the program compiled, the screen behavior was simply absent,
        // and a DISPLAY of the screen record printed its characters — a declined facility that produced output.
        if (program.dataDivision()?.screenSection() is { } scr)
        {
            ScreenFacility.ReportSection(Edition, scr, ScreenNames);
            // The section is refused, but its screen-names are declared all the same (§8.3.2.2; kb/Work PB1083).
            foreach (var sn in ScreenNames) DeclareUserWord(sn, UserWordKind.ScreenName);
        }

        if (program.dataDivision()?.workingStorageSection() is { } ws)
            _workingStorageRoots.AddRange(BindEntries(ws.dataDescriptionEntry(), _rootNames));

        // LOCAL-STORAGE SECTION (ISO §13.6 — automatic data, legal in a program or function definition per
        // §13.6.3 SR1; the method leg binds in OoBindMethodData): the SAME BindEntries path as WS, so VALUE
        // clauses, CONSTANT entries (§13.10 — WS/LS-legal), REDEFINES classes, and INDEXED BY cells all bind
        // identically; only the ACTIVATION-STATE model differs (§13.6.4 GR1 automatic → §14.6.2.3.2 initial
        // state on every activation — realized at emit, see LocalStorageRoots).
        if (program.dataDivision()?.localStorageSection() is { } ls)
            _localStorageRoots.AddRange(BindEntries(ls.dataDescriptionEntry(), _rootNames, EntrySection.LocalStorage));

        // LINKAGE SECTION roots + the PROCEDURE DIVISION header's USING/RETURNING formals (ISO §13.7 / §14.2.2;
        // COBOLNET_INTERPROGRAM_DESIGN D1/D3 — bound into the same forest so every verb works unchanged).
        CallBindLinkage(program, _rootNames);
    }

    /// <summary>The C#-field-name scope at the class level, shared by FILE SECTION records, WORKING-STORAGE
    /// roots, LINKAGE roots — and, in a CLASS unit, every METHOD's data roots (an emitted field/local name is
    /// unique across the whole class, so sibling methods' same-named items can never cross-wire — the legacy
    /// trap-#6 guard at the NAME level).</summary>
    private readonly HashSet<string> _rootNames = new(StringComparer.Ordinal);

    /// <summary>The resolution half of <see cref="Bind"/> — the post-build passes over the COMPLETE forest, driven by
    /// the DECLARED <see cref="BindPipeline"/> (rearchitecture PHASE 05 Step 3 / PHASE 06 Step 3; DESIGN-data-model
    /// §2.5). The order is IDENTICAL to the former comment-ordered call sequence — now EXPLICIT and asserted at
    /// startup by <see cref="BindPipeline.ValidateFullChainOnce"/> (each pass declares Requires/Produces phases),
    /// which structurally kills the implicit-pass-ordering smell with ZERO reorder / ZERO behavior change.
    /// <see cref="BindPipeline.Build"/> IS the per-unit resolve prefix; the whole-group middle-end tail (procedure
    /// binding → UsageCollection → StorageForm) is <see cref="BindPipeline.GroupTail"/>, run by
    /// <see cref="BinderDriver.Bind"/> once per compilation, and the two are validated as ONE chain.
    /// <para>Post-build the forest is complete: fix up USAGE INDEX entries (children weren't known at entry bind);
    /// apply group-level SIGN clauses (must precede the REDEFINES classification — a SEPARATE sign adds a character
    /// position to the item's image width, which feeds the class-max width); then resolve REDEFINES/RENAMES targets,
    /// group overlaid items into shared-storage classes and assign each a tier (ISO §13.18.44/§13.18.45;
    /// COBOLNET_DESIGN §4). This now covers the FILE SECTION records too (their multi-01 area sharing is a synthesized
    /// REDEFINES). Finally resolve each file's FILE STATUS data item.</para></summary>
    internal void BindResolve(Core.ProgramUnitContext program)
    {
        BindPipeline.ValidateFullChainOnce();
        foreach (var pass in BindPipeline.Build(program))
        {
            Require(pass.Requires, pass.Name);   // watermark gate: the declared prerequisite actually RAN (P6 Step 6)
            pass.Run(this);
            MarkProduced(pass.Produces);
        }
    }

    // ── The completion-phase WATERMARK gate (P6 Step 6). The construction-time DAG assert
    //    (BindPipeline.ValidateFullChainOnce) guards the declared LIST order; the watermark guards what actually
    //    RAN on THIS binder — "read a fact before its producing pass" becomes a loud, located error rather than
    //    a silent miscompile (a null Tier / missing CapacityRegister / unset StorageForm). ──

    /// <summary>The highest <see cref="PassPhase"/> whose producing pass has COMPLETED on this binder's forest.
    /// Advanced by <see cref="BindResolve"/> (the per-unit prefix) and by <c>BinderDriver.Bind</c> (the group
    /// tail, which marks every binder of the group after each group pass).</summary>
    internal PassPhase Watermark { get; private set; }

    /// <summary>Advance the watermark to <paramref name="produced"/> (never regresses).</summary>
    internal void MarkProduced(PassPhase produced)
    {
        if (produced > Watermark) Watermark = produced;
    }

    /// <summary>The ALWAYS-ON completion gate: assert <paramref name="required"/> has been produced on this
    /// binder before <paramref name="fact"/> is read/run — a mis-ordered pass in a production compiler is a
    /// silent miscompile, exactly when the loud throw matters most. Cost is a per-pass-entry integer compare
    /// (immaterial). Was <c>[Conditional("DEBUG")]</c> at first landing — CI's RELEASE test leg stripped the
    /// call sites and failed the throw-expecting WatermarkTests ("no exception was thrown"), proving the
    /// Debug-only design created a whole Release-divergence class for zero real saving (DEVLOG 774).</summary>
    internal void Require(PassPhase required, string fact)
    {
        if (Watermark < required)
            throw new InvalidOperationException(
                $"BindPipeline watermark violation: '{fact}' requires phase {required}, but this binder has only "
                + $"reached {Watermark} — a consumer ran before the producing pass (P6 Step 6 gate).");
    }

    /// <summary>The LAST resolve pass (<see cref="BindPipeline"/>): every FILE record area is filled WITHOUT conversion
    /// by READ/RETURN (ISO §9.1.2 — the record area is one character image), so its numeric-DISPLAY leaves store their
    /// images exactly like a whole-referenced group's — even when the PROCEDURE DIVISION never names the record as a
    /// whole (ST103A reads then tests only a child). <c>StorageFormPass</c> consumes <see cref="WholeGroupReferenced"/>
    /// after binding.</summary>
    internal void MarkFileRecordImageLeaves()
    {
        foreach (var f in Files)
            foreach (var rec in f.Records)
                if (rec.IsGroup)
                {
                    WholeGroupReferenced.Add(rec);
                    // Flag the leaves NOW (not at the group-tail whole-group pass): the bind-time
                    // NumericImagePlace wrap decision consults the early image facts (a ref-mod/RENAMES base
                    // under a record area must route through its character image at resolve time; ST102A's
                    // all-DISPLAY S-RECORD must not read as a Tier-C island at bind time).
                    MarkImageLeaves(rec);
                }

        void MarkImageLeaves(DataItem item)
        {
            foreach (var child in item.Children)
            {
                if (child.IsGroup) MarkImageLeaves(child);
                // ⛔ THE CARRIAGE QUESTION IS NOT THE IMAGE-FORM QUESTION, and this arm asks the CARRIAGE one
                // (kb/Work PB646). Promotion REPLACES the leaf's native carrier with a string of its
                // ImageWidth CHARACTER positions; for usage DISPLAY that string IS the leaf's storage, so every
                // width the model holds stays one number. A national-form numeric's carrier positions and its
                // STORAGE are two numbers (D-N1 — two bytes per position), and promoting one was MEASURED to
                // corrupt a plain group-to-group MOVE between two such leaves. Its group-image contribution is
                // the D-N7 COMPOSITION instead (GroupImageCodec.CarrierImageOf + the national byte transform),
                // which needs no promotion; the price is that a group move depositing characters incompatible
                // with the leaf's description leaves it holding §14.6.13.2's value rather than those
                // characters, which is the same posture every other non-DISPLAY numeric usage already takes.
                else if (child.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                    MarkImageForced(child);      // the collected image fact (same rule as the whole-group union, §14.9.25.4 MOVE GR4)
            }
        }
    }

    /// <summary>Route a RECURSIVE unit's WORKING-STORAGE onto the ONE static-field channel (the mechanism the
    /// method-WS D3 pattern established — <see cref="StaticRootFields"/> / <see cref="StaticIndexCells"/>,
    /// consumed by <c>RecordStructEmitter</c>): a no-op unless <see cref="UnitStaticWs"/>. ISO basis on the
    /// flag's doc — §13.5.4 GR1 (a non-initial program's / a function's WS is STATIC data) + §14.6.2.3.3 (static
    /// data is in LAST-USED state: one copy shared across all activations), while the per-activation instance
    /// keeps carrying the §13.6.4 GR1 automatic data and the formals. Runs AFTER the pointer pass (the last
    /// tier-overwrite seam) so Tier-B backings, EXTERNAL re-basing, and BASED/ADDRESS-OF forcing are settled:
    /// <list type="bullet">
    /// <item>a Tier-B StringCanonical WS root → its ONE string backing goes static (the OO
    ///   <see cref="OoRouteMethodRedefinesBackings"/> twin), plus the root's own name (harmless when the
    ///   physical is the backing — membership is tested per emitted field name);</item>
    /// <item>an EXTERNAL WS record stays OFF the channel — it is run-unit storage on the ExternalStore
    ///   (§8.6.7; §14.6.2.3.3 external data is ALWAYS last-used), reached through a ref-bridge, never a
    ///   per-class static;</item>
    /// <item>a WS table's INDEXED BY cell goes static with its table (the M2-OO-1h step-4 mirror — a
    ///   last-used table with per-activation indexes would silently lose SET positions);</item>
    /// <item>a BASED WS root / an ADDRESS-OF-taken WS record stages LOUD (0899) — their cell/bridge storage
    ///   is per-instance (<see cref="PtrAddressableBackings"/>/<see cref="PtrBasedBridges"/> emit instance
    ///   members), and §14.6.2.3.2 #5 makes a based item's address part of the unit's static state; a silent
    ///   instance-resident cell would re-initialize per activation (§1.4 — never silent-wrong).</item>
    /// </list></summary>
    internal void RouteStaticUnitStorage()
    {
        if (!UnitStaticWs) return;
        foreach (var root in _workingStorageRoots)
            RouteStaticRoot(root, "WORKING-STORAGE");
        // THE FILE SECTION ROUTES WITH IT (kb/Work PB168 — the review fleet's §8.6.4 finding): "Data items
        // and file connectors defined in the working-storage or file section of a source element that is not
        // an initial program are static items … For static items that are not object data, there is one copy
        // in a run unit." The connector half is UnitStaticFiles/the static registration guard; the RECORD
        // AREA half is this — without it, the shared connector reads into a per-activation area and the
        // resumed outer activation sees stale record content (a silent wrong answer where the standard has
        // ONE area). EXTERNAL files stay off the channel (run-unit ExternalStore, §8.6.7); a REPORT FD has
        // no record description to route (§13.18.46). ⚠ __ResetStatics deliberately does NOT re-seed these:
        // §14.6.2.3.2 action 2 names only the working-storage and local-storage sections.
        foreach (var file in Files)
            if (!file.IsExternal)
                foreach (var record in file.Records)
                    RouteStaticRoot(record, "FILE SECTION");
    }

    /// <summary>Route ONE root of a <see cref="UnitStaticWs"/> unit onto the static channel — shared by the
    /// WORKING-STORAGE walk and the kb/Work PB168 FILE SECTION walk (<paramref name="section"/> names the
    /// section in the staged-loud message).</summary>
    private void RouteStaticRoot(DataItem root, string section)
    {
        if (root.Class is { Tier: RedefinesTier.StringCanonical } cls
            && CallExternalBackings.Any(b => b.BackingCsName == cls.BackingCsName))
            return;   // an EXTERNAL-backed class (canonical AND views) — run-unit cell storage (§14.6.2.3.3), not a class static
        if (root.IsBased)
        {
            // kb/Work PB154 — §14.6.2.3.2 action 5: a BASED root in a RECURSIVE unit's static WS routes
            // its `__addr_X` bridge as a STATIC field, reset to NULL by __ResetStatics (the old code
            // REJECTED this legal source). The DATA lives in the allocated cell, so no static root field
            // joins the value-reset set. A based root whose class the substrate screen rejected
            // (COBOLNET1695) was already diagnosed — skip quietly.
            if (root.Class?.BasedPointerField is { } bp && !PtrAddressableCellOf.ContainsKey(root.Class))
                _staticBasedBridgeAddrs.Add(bp);
            // An INDEXED BY cell under a BASED table is an ordinary emitted field (allocated in
            // RegisterIndexes regardless of BASED-ness) and §13.5.4 GR1 makes it static like the rest of
            // the unit's WS — the bridge routes the ADDRESS, the index cells still route themselves.
            foreach (var idx in IndexDeclarationsUnder(root))
                _staticIndexCells.Add(idx.Cell);
            return;
        }
        if (root.Class is { } ac && PtrAddressableCellOf.TryGetValue(ac, out var addrCell))
        {
            // kb/Work PB234 — an ADDRESS-OF-taken record of a RECURSIVE unit's static WS. §13.5.4 GR1 makes the
            // data STATIC — one copy shared by every activation — and its storage IS the forced StorageCell (the
            // backing is a ref-bridge over it), so the CELL is what goes static; §14.6.2.3.2 action 2 then
            // re-seeds it in place from __ResetStatics. Nothing in §8.4.3.11, §11.10.4 or §14.9.5.4 forbids the
            // source this used to refuse (COBOLNET0899): a static item's address is simply the one copy's.
            // Every root of the class (the canonical and each REDEFINES view) names the ONE cell.
            _staticAddressableCells.Add(addrCell);
            foreach (var idx in IndexDeclarationsUnder(root))
                _staticIndexCells.Add(idx.Cell);
            return;
        }
        if (root.Class is { } c && c.BasedPointerField is not null)
        {
            // A non-BASED root of a BASED item's class — a level-01 REDEFINES of it (§13.18.44 does not forbid
            // one, and §13.18.5.3 names only class object and dynamic length). Its storage is the based item's
            // ALLOCATED cell reached through the ONE bridge the BASED root above already routed static, so it
            // has no storage of its own to route; only its index cells do (kb/Work PB234 — this arm used to
            // refuse the source with the same COBOLNET0899 as the ADDRESS OF arm).
            foreach (var idx in IndexDeclarationsUnder(root))
                _staticIndexCells.Add(idx.Cell);
            return;
        }
        if (root.Class is { Tier: RedefinesTier.StringCanonical } c2 && ReferenceEquals(c2.Canonical, root))
            _staticRootFields.Add(c2.BackingCsName);   // Tier-B: the ONE string backing IS the storage
        _staticRootFields.Add(root.CsName);
        foreach (var idx in IndexDeclarationsUnder(root))
            _staticIndexCells.Add(idx.Cell);
    }

    /// <summary>Bind a run of data-description entries (a WORKING-STORAGE section or one FD's records) into the
    /// storage forest: a level-number stack attaches each entry under the nearest open item of a lower level; level-88
    /// becomes a condition-name and level-66 a RENAMES alias. Returns the new top-level (01/77) items, in order — the
    /// caller (an FD) needs them to model the shared record area. <paramref name="section"/> names the DATA DIVISION
    /// section the run belongs to — consumed by the section-scoped placement rules (CONSTANT RECORD §13.18.15.3 SR1).</summary>
    private List<DataItem> BindEntries(IEnumerable<Core.DataDescriptionEntryContext> entries, HashSet<string> rootNames,
        EntrySection section = EntrySection.WorkingStorage)
    {
        var newRoots = new List<DataItem>();
        var stack = new Stack<DataItem>();
        bool rootIsTemplate = false;   // true while the current level-1 subtree is a TYPEDEF template (D17)
        // ⛔ THE ENTRY A LEVEL-88 ENTRY IMMEDIATELY FOLLOWS — ISO §13.16.3 SR24's own subject, and NOT
        // `stack.Peek()`. The two coincide for every entry that opens or extends the level hierarchy and DIVERGE
        // for the entries that never enter the stack: a level-66 RENAMES alias (SR24 exclusion b) and a CONSTANT
        // entry (§13.10, which describes no data item). `stack.Peek()` walked BACK PAST those to an unrelated
        // item, so `66 R1 RENAMES A THRU B.` / `88 COND-R VALUE "CD".` silently bound COND-R to the preceding
        // `05 B` and answered about it (kb/Work PB488 — a wrong answer, not a permissiveness). Null = the 88
        // follows no entry describing a data item; the screen reports that instead of the old silent drop.
        DataItem? lastDescribed = null;
        foreach (var entry in entries)
        {
            using var _ = Edition.At(entry);   // the entry cursor (kb/Work PB82): every diagnostic below names this entry
            // A CONSTANT entry (ISO §13.10; the constantEntryBody alternative) is a COMPILE-TIME substitution,
            // not storage: fold it into the constant table and produce NO DataItem. Checked BEFORE the 66/88
            // early-outs so a mis-leveled constant entry gets its §13.10.2 level diagnostic, never a RENAMES/
            // condition-name misbind. (P10 Step 15; DataBinder.Constants.cs.)
            if (entry.dataDescriptionBody().constantEntryBody() is { } constBody)
            {
                BindConstantEntry(entry.levelNumber(), entry.dataName(), constBody);
                // A constant entry describes NO data item, so a level-88 entry written after one has no
                // conditional variable to associate with (ISO §13.16.3 SR24 — "the entry describing the item").
                lastDescribed = null;
                continue;
            }
            // The level-number arrives PRE-SCREENED: LevelNumberPass ran over the whole parse tree before any
            // binding and rejected every value outside the set §13.18.33.3 permits for this entry's section, so
            // `lvl` here is 1–49, 66, 77 or 88 and the TryParse cannot fail in a compile that reaches emit. Do
            // NOT re-decide the set here — there are four grammar arms that spell a level-number and only one of
            // them is this one (kb/Work PB485; LevelNumberArmDriftTests holds the list honest).
            int.TryParse(entry.levelNumber().GetText(), out int lvl);
            // A level-88 entry is a condition-name on the immediately superior item — not a node in the tree.
            if (lvl == 88)
            {
                // The association is RECORDED here — source order is a fact only this walk has — and ADJUDICATED
                // by CheckConditionNameAssociations once the forest is complete, because four of §13.16.3 SR24's
                // eight exclusions ask about the conditional variable's SUBORDINATES, which are written AFTER the
                // 88 and so do not exist yet (DataBinder.ConditionName.cs; kb/Work PB488). ⛔ `lastDescribed` is
                // deliberately LEFT ALONE by this arm: SR24 exclusion a) ("Another level 88 entry") is what makes
                // a run of consecutive 88s all associate with the one entry they follow.
                // ⛔ NO CONDITION-NAME, NO ASSOCIATION RULE. SR24 speaks throughout of "each condition-name" and
                // "the condition-name entries", so an entry that names none has nothing to associate and this
                // screen must not invent a subject for a message. That is the SAME precondition BindCondition
                // has always had, now stated once instead of twice. (A nameless `88 VALUE "AB".` is Format 3
                // without its required condition-name — §13.16.2 prints `88 condition-name-1 value-clause .`,
                // brackets only in Format 4 — and the compiler accepts it today. That is a §13.16.2 FORMAT
                // defect, reported separately; it is not this rule's to fix and must not be mis-reported as one.)
                if (entry.dataName()?.GetText() is not { } condName) continue;
                RecordConditionAssociation(condName, lastDescribed);
                if (lastDescribed is { } condVar)
                {
                    // §13.18.57.3 SR2 (review DEVLOG 664 fix #2): a TYPE-clause entry shall not be followed
                    // immediately by a level-88 entry.
                    if (condVar.TypeRefName is not null)
                        Edition.Error("COBOLNET1537", $"condition-name '{condName}': a data "
                            + "description entry that specifies a TYPE clause shall not be followed immediately by a "
                            + "level-88 entry (ISO §13.18.57.3 SR2)");
                    // A TYPEDEF template's condition-names are NOT globally referenceable (§13.18.58.4 GR1) — bind
                    // them onto the item (so CloneItem can carry them into each TYPE reference) but keep them off
                    // the global by-name index; a non-template 88 registers globally as usual (D17 inc 3). Bound
                    // even when SR24 excludes the variable: the entry has been reported, and binding it keeps the
                    // condition-name resolvable so the procedure division adds no cascade of "unknown name".
                    BindCondition(entry, condVar, registerGlobal: !rootIsTemplate);
                }
                continue;
            }
            // A level-66 RENAMES entry is a re-grouping alias on the owning record — not a node in the storage tree.
            if (lvl == 66)
            {
                // A RENAMES INSIDE a TYPEDEF template is part of the type (§13.18.58.4 GR1) but CloneItem does not
                // carry Renames66 into a TYPE reference — so it would be silently dropped. Staged loud (D17 inc 4).
                if (rootIsTemplate)
                    Edition.Error(DiagnosticCatalog.TypedefRenamesStaged, "a level-66 RENAMES inside a TYPEDEF "
                        + "(part of the type per ISO §13.18.58.4 GR1) is recognized but not yet cloned into TYPE "
                        + "references (data-model D17 residue)");
                // The alias IS "the entry describing the item" for a level-88 entry written after it — and
                // ISO §13.16.3 SR24 b) then excludes it. Recording it here is what replaces the silent steal.
                lastDescribed = BindRenames(entry);
                continue;
            }

            // A malformed entry describes no item (the level-number is unreadable — LevelNumberPass has already
            // reported it), so a following 88 has no conditional variable rather than the previous entry's.
            if (BindEntry(entry, section) is not { } item) { lastDescribed = null; continue; }
            item.Uid = _uidCounter++;
            if (item.Level is 1 or 77) item.RootSection = section;   // DataItem.Section — a subordinate reads its root's

            // Level 77 is an INDEPENDENT elementary item (ISO §13.18.38): always top-level, like 01, regardless of its
            // numeric value. Treat it as level 1 for the nesting pop so it attaches as a ROOT — never nested under an
            // open subordinate item just because 77 > that item's level (which would mis-qualify every later reference).
            int nestLevel = item.Level == 77 ? 1 : item.Level;
            while (stack.Count > 0 && stack.Peek().Level >= nestLevel)
                stack.Pop();

            if (stack.Count == 0)
            {
                // A TYPEDEF entry is a type DECLARATION (ISO §13.18.58; D17): a named level-01 template that
                // allocates NO storage — registered in TypeDecls, kept OFF Roots (and, below, off ByName).
                if (item.IsTypedef)
                {
                    rootIsTemplate = true;
                    RegisterTypeDecl(item);
                }
                else
                {
                    rootIsTemplate = false;
                    // A 01/77 emits as a Program-level static field — its C# name must be unique across every root
                    // (FILE SECTION records and WORKING-STORAGE alike), so record it in the shared scope.
                    item.CsName = Unique(item.CsName, rootNames);
                    rootNames.Add(item.CsName);
                    _roots.Add(item);
                    newRoots.Add(item);
                    _lastRoot = item;
                }
            }
            else
            {
                if (item.IsTypedef)   // §13.18.58 SR — a TYPEDEF is a level-01 entry only, never a subordinate.
                    Edition.Error(DiagnosticCatalog.TypeDeclarationShape, $"TYPEDEF on '{item.CobolName ?? "FILLER"}': the TYPEDEF clause "
                        + "shall be specified only in a level-01 record-description entry (ISO §13.18.58)");
                var parent = stack.Peek();
                // §13.18.57.3 SR2 (review DEVLOG 664 fix #2): a TYPE-clause entry shall not be followed immediately by
                // a subordinate entry — the entry IS the whole type (§13.18.57.4 GR1). Without this the explicit
                // subordinate merges ahead of the cloned members (a silent-wrong record image for a group type; a
                // member-on-a-string CS1061 leak for an elementary type).
                if (parent.TypeRefName is not null)
                    Edition.Error("COBOLNET1537", $"'{item.CobolName ?? "FILLER"}': a data description entry that "
                        + "specifies a TYPE clause shall not be followed immediately by a subordinate entry "
                        + "(ISO §13.18.57.3 SR2)");
                // A member name need only be unique within its containing struct (the parent's children).
                item.CsName = Unique(item.CsName, parent.Children.Select(c => c.CsName));
                item.Parent = parent;
                parent.Children.Add(item);
            }
            // CONSTANT RECORD placement + subtree conflicts (P10 Step 15). §13.18.15.3 SR1: the clause may be
            // specified only in the local-storage or working-storage sections. §13.16.3 SR13: BLANK WHEN ZERO /
            // SYNCHRONIZED (and the level-checked BASED / ANY LENGTH / TYPEDEF) shall not appear in any entry
            // subordinate to a CONSTANT RECORD entry. (The same-entry SR13/SR3/SR6 conflicts are checked in
            // BindEntry, where the flags are local — the IsBased discipline.)
            if (item.IsConstantRecord && section is not (EntrySection.WorkingStorage or EntrySection.LocalStorage))
                Edition.Error(DiagnosticCatalog.ConstantRecordRule, $"'{item.CobolName ?? "FILLER"}': the "
                    + "CONSTANT RECORD clause may be specified only in the local-storage or working-storage "
                    + "sections (ISO §13.18.15.3 SR1)");
            else if (!item.IsConstantRecord && IsConstantRecordItem(item)
                && (item.BlankWhenZero || item.Synchronized || item.IsBased || item.IsAnyLength || item.IsTypedef))
                Edition.Error(DiagnosticCatalog.ConstantRecordRule, $"'{item.CobolName ?? "FILLER"}': the "
                    + "ANY LENGTH, BASED, BLANK WHEN ZERO, SYNCHRONIZED, and TYPEDEF clauses shall not be "
                    + "specified in any entry subordinate to a CONSTANT RECORD entry (ISO §13.16.3 SR13)");
            // §13.18.40.3 SR32 (subordinate half — the parent chain exists only here): a format 2 PICTURE clause
            // shall not be specified in any data item subordinate to a CONSTANT RECORD item.
            if (!item.IsConstantRecord && IsConstantRecordItem(item) && item.Pic is { LocaleEdit: not null })
                Edition.Error(DiagnosticCatalog.PictureLocaleFormat2Violation, $"'{item.CobolName ?? "FILLER"}': a "
                    + "format 2 PICTURE clause shall not be specified in any data item subordinate to a data item "
                    + "described with the CONSTANT RECORD clause (ISO §13.18.40.3 SR32)");
            // §13.18.63.3 SR12/SR16 — the VALUE clause's PLACEMENT, asked here for the same reason as the two
            // screens above: the ancestor chain exists only once the entry is attached (DataBinder.ValuePlacement;
            // its format-3 twin, SR25, is asked by BindCondition).
            ScreenItemValuePlacement(item);
            stack.Push(item);
            lastDescribed = item;   // ISO §13.16.3 SR24's "the entry describing the item", for a following 88
            // A TYPEDEF template's items (root + subordinates) are NOT globally referenceable (ISO §13.18.58.4 GR1) —
            // keep them off ByName; the clones ExpandTypes produces ARE registered.
            if (!rootIsTemplate) RegisterName(item);
            // Every named entry is DECLARED here (§8.3.2.2; kb/Work PB1083) — a template's subordinate data-names
            // too, although they stay off ByName; the template root's type-name is declared by RegisterTypeDecl, and
            // the clones ExpandTypes registers are the same words, already declared here.
            if (item.CobolName is { } declared && !item.IsTypedef)
                DeclareUserWord(declared, section == EntrySection.File && item.Level == 1
                    ? UserWordKind.RecordName : UserWordKind.DataName);
        }
        return newRoots;
    }

    /// <summary>Register a TYPEDEF type declaration (ISO §13.18.58; data-model D17): a named level-01 template.
    /// SR checks (→ COBOLNET1529): the entry shall be level-01 and named (not FILLER), and the type-name unique.</summary>
    private void RegisterTypeDecl(DataItem item)
    {
        if (item.Level != 1)
            Edition.Error(DiagnosticCatalog.TypeDeclarationShape, $"TYPEDEF '{item.CobolName ?? item.CsName}': a type declaration shall be a "
                + "level-01 record-description entry (ISO §13.18.58 / §13.16)");
        if (item.CobolName is null)
        {
            Edition.Error(DiagnosticCatalog.TypeDeclarationShape, "a TYPEDEF entry shall be named (not FILLER) — it defines a type-name "
                + "(ISO §13.18.58.4 GR2)");
            return;
        }
        if (item.RedefinesTargetName is not null)
            Edition.Error(DiagnosticCatalog.TypeDeclarationShape, $"TYPEDEF '{item.CobolName}': the TYPEDEF and REDEFINES clauses are "
                + "mutually exclusive (ISO §13.16)");
        // ⛔ §8.5.3.1: "Elementary type definitions shall not be specified with the STRONG phrase." — with
        // §8.5.3.3, "The only kind of items that may be strongly typed are group items." Unenforced until
        // kb/Work PB153: `01 T TYPEDEF STRONG PIC 9(3).` bound clean, and the illegal shape had already reached
        // the corpus — tests/version-matrix/constructs.json's usage-pointer-to-type-2014 row carried exactly it
        // as the construct's canonical witness. A type declaration nothing ever compiled is a declaration nothing
        // ever contradicted.
        // ⛔ THE TEST IS `IsElementary`, NOT `!IsGroup`, AND THAT IS NOT A STYLE CHOICE. RegisterTypeDecl runs
        // while the forest is still being BUILT — the level-01 is registered before its subordinate entries are
        // attached — so `IsGroup` (Pic is null AND Children.Count > 0) is FALSE for every group typedef at this
        // moment and the check would reject all of them. `Pic`, by contrast, is already assigned by BindEntry
        // before the item is pushed, so `IsElementary` is exactly the settled fact this rule needs.
        if (item.TypedefStrong && item.IsElementary)
            Edition.Error(DiagnosticCatalog.TypeDeclarationShape, $"TYPEDEF '{item.CobolName}': an ELEMENTARY type definition shall not be "
                + "specified with the STRONG phrase (ISO §8.5.3.1) — §8.5.3.3 makes group items the only kind of "
                + "item that may be strongly typed");
        DeclareUserWord(item.CobolName, UserWordKind.TypeName);   // the template is off ByName, so RegisterName never sees it
        if (!TypeDecls.TryAdd(item.CobolName, item))
            Edition.Error(DiagnosticCatalog.TypeDeclarationShape, $"duplicate type-name '{item.CobolName}' — a type-name shall be unique "
                + "(ISO §13.18.58)");
    }

    // ── FILE-CONTROL + FILE SECTION (ISO §12.4.5 / §13.18; COBOLNET_DESIGN §8) ─────────────────────────────────

    /// <summary>Every ENVIRONMENT DIVISION of the unit, OUTERMOST (class-level) FIRST. A real program unit
    /// carries at most ONE; an OO synthetic reparent unit (OoDriver's CallReparent pattern) carries
    /// [half's-own-env, class-env] nearer-first, so iterating REVERSED binds the class scope first and the
    /// nearer half's registrations override per-clause — the ISO §10.6 scoping (the class definition's
    /// environment division applies to the factory and object definitions). This is the DEVLOG-738 latent-bug
    /// fix: the former SINGULAR environmentDivision() read silently DROPPED the class-level env whenever the
    /// half carried its own (the shadow-0× case).</summary>
    internal static IReadOnlyList<Core.EnvironmentDivisionContext> EnvDivisions(Core.ProgramUnitContext program) =>
        [.. program.GetRuleContexts<Core.EnvironmentDivisionContext>().Reverse()];

    /// <summary>Bind the FILE-CONTROL paragraph's SELECT clauses into <see cref="FileModel"/>s (assign target,
    /// organization, access mode, OPTIONAL, FILE STATUS). The FD records attach in <see cref="BindFileSection"/>.</summary>
    private void BindFileControl(Core.ProgramUnitContext program)
    {
        foreach (var env in EnvDivisions(program)) BindFileControl(env);
    }

    private void BindFileControl(Core.EnvironmentDivisionContext env)
    {
        var fc = env.inputOutputSection()?.fileControlParagraph();
        if (fc is null) return;
        // A class-level input-output section (the OO binder hands it to both the factory and the object half) is
        // outside §12.4.5.2's "factory, function, object, or program" and is forbidden by §12.4.3 SR1 (kb/Work
        // PB1076), so SR2 and SR3 do not screen its entries.
        bool screened = env.Parent is not Core.ClassDefinitionContext;
        foreach (var grp in fc.fileControlClauseGroup())
        {
            using var _ = Edition.At(grp);
            if (grp.fileName()?.GetText() is not { } name) continue;
            // ISO §12.4.5.2 SR2 — one SELECT per file-name. The first entry stands: binding the second used to
            // REPLACE it in FilesByName, so the program silently ran against the last ASSIGN target (kb/Work PB1077).
            if (screened && _selectedFiles.ContainsKey(name))
            {
                Edition.Error(DiagnosticCatalog.FileNameSelectedTwice, $"file-name '{name}' is specified in a second "
                    + "SELECT clause; ISO §12.4.5.2 SR2: \"A given file-name may be specified in only one SELECT clause "
                    + "within a factory, function, object, or program.\"");
                continue;
            }
            var file = new FileModel { CobolName = name, SelectName = name, AssignTarget = name, Optional = grp.OPTIONAL() is not null };
            // The entry's OWN position: §12.4.5.1's required members and §12.4.5.2 SR10 are violated by the
            // ABSENCE of a clause, so there is nothing else to point at (kb/Work PB699).
            file.EntryAt = Edition.Cursor;
            Core.AccessModeClauseContext? accessAt = null;   // the ACCESS MODE clause cursor for §12.4.5.5.2 SR2, below
            foreach (var clauses in grp.fileControlClauses())
            {
                // ISO §12.4.5.3 GR3 — BOTH phrases of the ASSIGN clause, on both general-format arms
                // (`ASSIGN TO? assignTarget (USING dataReference)?` and the bare `ASSIGN USING dataReference`).
                // GR3 a is the static TO-phrase association; GR3 b is dynamic file assignment (§9.1.21) and applies
                // "when the USING phrase of the ASSIGN clause is specified" with NO condition on the TO phrase, so
                // both are captured and the runtime lets the USING content win at each OPEN/SORT/MERGE. Reading only
                // assignTarget() dropped the USING operand entirely AND left a bare-USING file's target defaulted to
                // the FILE-NAME, so its records landed in `<file-name>.txt` (kb/Work PB324).
                if (clauses.assignClause() is { } asg)
                {
                    // The TO phrase is a LIST (§12.4.5.1's ellipsis); which lists are allowable and what one names
                    // is the §12.4.5.2 SR5 determination, read in ONE place (AssignTargetRule, DOC-A.1-71).
                    file.AssignTarget = asg.assignTarget() is { Length: > 0 } tgts
                        ? AssignTargetRule.Resolve(Edition, LiteralEnv, tgts, name)
                        : "";   // bare `ASSIGN USING …`: no device-name-1/literal-1 exists — UNASSOCIATED until an OPEN
                    if (asg.dataReference() is { } dyn)
                    {
                        (file.AssignUsingName, file.AssignUsingQualifiers) = ClauseDataName(dyn, "ASSIGN … USING");
                        using var __ = Edition.At(dyn);
                        file.AssignUsingAt = Edition.Cursor;   // §12.4.5.2 SR7 reports here, from ResolveFiles
                    }
                }
                else if (clauses.organizationClause() is { } org) { file.Organization = MapOrganization(org); file.OrganizationWritten = true; }
                else if (clauses.accessModeClause() is { } acc) { file.AccessMode = MapAccessMode(acc); accessAt = acc; }
                // The base word PLUS its IN/OF qualifiers (`SQ-FS4-STATUS OF STATUS-GROUP`, SQ133A): a raw
                // GetText() would glue the qualifier into the lookup key, and keeping the base word ALONE
                // resolved the first same-named declaration in order (kb/Work PB489).
                else if (clauses.fileStatusClause()?.dataReference() is { } fs)
                {
                    (file.FileStatusName, file.FileStatusQualifiers) = ClauseDataName(fs, "FILE STATUS");
                    using var __ = Edition.At(fs);
                    file.FileStatusAt = Edition.Cursor;   // where its post-build resolution reports
                }
                else if (clauses.recordKeyClause() is { } rkc)                                                       // ISO §12.4.5.12
                {
                    // ⛔ THE CLAUSE HAS TWO KEY FORMS AND ONLY ONE IS PROVIDED — take the declined arm FIRST, so
                    // it can never fall through this chain unread (kb/Work PB358). Before 2026-09-09 this arm
                    // read `clauses.recordKeyClause()?.dataReference() is { } rk`, which was safe only because
                    // the SOURCE form had no grammar at all; with the surface added, a null dataReference()
                    // would have skipped the clause silently and left the file KEYLESS.
                    file.RecordKeyClauseWritten = true;   // §12.4.5.1 Format 1 requiredness asks PRESENCE, not what it named
                    if (rkc.recordKeySourcePhrase() is { } rks)
                        DeclineRecordKeySource(rks, "RECORD KEY", name);
                    else if (rkc.dataReference() is { } rk)
                    {
                        (file.RecordKeyName, file.RecordKeyQualifiers) = ClauseDataName(rk, "RECORD KEY");
                        using var __ = Edition.At(rk);
                        file.RecordKeyAt = Edition.Cursor;   // §12.4.5.12.3 reports here, from ResolveFiles
                    }
                }
                else if (clauses.alternateKeyClause() is { } ak)                                                     // ISO §12.4.5.6
                {
                    // The SAME two-form choice as its prime-key twin above, taken in the SAME order and for the
                    // same reason — §12.4.5.6.2 prints the identical brace group, and Annex A.3 item 40 names
                    // both clauses in one sentence, so one omission here would be the two-arm defect shape
                    // (feedback_two_arm_dispatch) on a clause that literally IS the other arm.
                    if (ak.recordKeySourcePhrase() is { } aks)
                        DeclineRecordKeySource(aks, "ALTERNATE RECORD KEY", name);
                    else if (ak.dataReference() is { } akRef)
                    {
                        var (an, aq) = ClauseDataName(akRef, "ALTERNATE RECORD KEY");
                        using var __ = Edition.At(akRef);
                        var clause = new AlternateKeyClause
                        {
                            Name = an, Qualifiers = aq, Duplicates = ak.DUPLICATES() is not null,
                            At = Edition.Cursor,   // §12.4.5.6.3 reports here, from ResolveFiles
                        };
                        file.AlternateKeyNames.Add(clause);
                        // §12.4.5.6.2 SUPPRESS WHEN literal-1 — captured AS WRITTEN and read post-build by
                        // ReadSuppressWhen: a constant-name operand is declared in the DATA DIVISION, after this entry.
                        if (ak.alternateKeySuppressWhen()?.valueClauseOperand() is { } sw)
                            _suppressWhenOperands[clause] = sw;
                    }
                }
                else if (clauses.relativeKeyClause()?.dataReference() is { } rlk)
                {
                    (file.RelativeKeyName, file.RelativeKeyQualifiers) = ClauseDataName(rlk, "RELATIVE KEY");   // ISO §12.4.5.13.3 SR3 — outside the record
                    using var __ = Edition.At(rlk);
                    file.RelativeKeyAt = Edition.Cursor;   // §12.4.5.13.3 reports here, from ResolveFiles
                }
                else if (clauses.sharingClause() is { } sh)   // §12.4.5.15 — edition gate: VersionConformancePass ParseArm.VisitSharingClause (14g.4, recognition — fires on the clause's presence, drop-proof on a SELECT error)
                    file.Sharing = MapSharing(sh.sharingMode());
                else if (clauses.lockModeClause() is { } lm)   // §12.4.5.9 — edition gate: VersionConformancePass ParseArm.VisitLockModeClause (14g.4)
                    file.LockMode = MapLockMode(lm);
                else if (clauses.fileCollatingSequenceClause() is { } col)   // §12.4.5.7 — introduction-gated post-bind; resolved in ResolveFileCollating
                    CaptureFileCollating(file, col);
                // RESERVE clause (ISO §12.4.5.14) — integer-1 is captured because §12.4.5.3 GR1 d) makes "The same
                // value for integer-1 in the RESERVE clause" part of an external file connector's entry identity
                // (kb/Work PB1079): FileModel.ReserveAreas is read by the EC-EXTERNAL-FILE-MISMATCH fingerprint.
                else if (clauses.fileReserveClause() is { } res)
                    file.ReserveAreas = CobolNet.Validation.IntegerOperandRules.HostValue(res.integerLiteral());
                // RECORD DELIMITER clause (ISO §12.4.5.11) — DECLINED, ACCEPT-INERT, and diagnosed by name at
                // EVERY edition on BOTH arms of its required choice (kb/Work PB292). STANDARD-1 is Annex A.3
                // item 26, a processor-dependent element whose §12.4.5.11.4 GR2 medium is a tape drive; a
                // feature-name-1 names nothing because §12.4.5.11.3 SR2's available-name set is this
                // implementation's to specify and is EMPTY (Annex A.1 item 150 is optional — docs/CONFORMANCE.md
                // §7). The clause does not change the I-O: §12.4.5.11.4 GR5's implementor method (the 4-byte
                // length prefix) frames every variable-length record whatever the clause says. §4.2.6 ¶3 makes
                // the WARNING the obligation this discharges; §12.4.5.11.4 GR1 keeps the framing out of the record
                // area, so the accept is inert in the program's own terms. The phrase written IS captured
                // (FileModel.RecordDelimiter), for the one reader that needs it: §12.4.5.3 GR1 c) makes "Either
                // the STANDARD-1 phrase or a consistent value of feature-name-1" part of an external file
                // connector's entry identity (kb/Work PB1079). This arm is LAST because every preceding clause
                // has a distinct leading token and `recordKeyClause` already back-tracks past `RECORD DELIMITER`.
                else if (clauses.recordDelimiterClause() is { } rd)
                {
                    file.RecordDelimiter = rd.STANDARD_1() is not null ? "STANDARD-1" : rd.cobolWord().GetText().ToUpperInvariant();
                    Edition.Declined(DiagnosticCatalog.RecordDelimiterUnsupported,
                        $"the RECORD DELIMITER clause on file '{name}' ({Spelled(rd)})");
                }
            }
            // §12.4.5.5.2 SR2 — "The DYNAMIC and RANDOM phrases shall not be specified for a sequential file."
            // A FILE CONTROL ENTRY rule, so it belongs HERE, beside the clauses it relates, and not on any verb:
            // it constrains the ENTRY whether or not the file is ever read, written or started (kb/Work PB692).
            // The general format says the same thing structurally — Format 3 (sequential) admits only
            // `[ ACCESS MODE IS SEQUENTIAL ]` — but a format is not a check, and nothing screened this.
            // ⛔ THE ORGANIZATION MAY BE UNWRITTEN. §12.4.5.10.3 GR6: "When the ORGANIZATION clause is not
            // specified, sequential organization with the RECORD SEQUENTIAL phrase is implied", so the bare
            // `SELECT F ASSIGN … ACCESS MODE IS RANDOM.` is under this rule too and is the shape a screen keyed
            // on a written clause would miss; `FileModel.Organization` already defaults to Sequential, so
            // `IsSequential` covers the omitted clause, RECORD SEQUENTIAL and LINE SEQUENTIAL in one predicate.
            // That LINE SEQUENTIAL is under "a sequential file" is DERIVED, not assumed: §12.4.5.10.3 GR2/GR3
            // put both sequential phrases in the ORGANIZATION clause, that clause is written only in the
            // Format-3 file control entry, and §12.4.5.2 SR11 says "Format 3 shall be specified only for a
            // sequential file or a report file".
            // Edition-invariant (all four): no `docs/VERSION_CHANGE_REFERENCE.md` row touches §12.4.5.5, and the
            // 1985 sequential-I-O file control entry likewise offered no ACCESS MODE but SEQUENTIAL.
            // WHY THIS MATTERS DOWNSTREAM: without it, `IsSequential && AccessMode != Sequential` is a REACHABLE
            // state, and the verbs then read a file that has no keys through their keyed rules — §14.9.30.3
            // SR8/SR9 imply no NEXT under RANDOM, so §14.9.30.4 GR19 turns the READ into a Format-2 random read
            // on a keyless connector. Every statement-level screen that tests `AccessMode == Random` on a
            // SEQUENTIAL-organization file is defence-in-depth from here on, never the only guard.
            // The predicate NAMES THE TWO PHRASES the rule names, rather than negating SEQUENTIAL: the rule is
            // "DYNAMIC and RANDOM shall not be specified", not "only SEQUENTIAL may be", and a mode the standard
            // adds later would then be screened by a rule that does not mention it.
            if (file.IsSequential && file.AccessMode is FileAccessMode.Random or FileAccessMode.Dynamic)
            {
                using var acc2 = Edition.At(accessAt);   // the ACCESS MODE clause itself (null ⇒ the entry cursor)
                Edition.Error(DiagnosticCatalog.AccessModeNotSequentialOnSequentialFile,
                    $"file '{name}': ACCESS MODE IS {file.AccessMode.ToString().ToUpperInvariant()} may not be "
                    + $"specified for a sequential file — this entry's organization is "
                    + file.OrganizationFace
                    + " (ISO §12.4.5.5.2 SR2)");
            }
            // §12.4.5.9 SR2: WITH LOCK ON MULTIPLE RECORDS shall not be specified for a sequentially-accessed
            // or sequential-organization file. The organization half is `FileModel.IsSequential` — the ONE
            // spelling of "this file has sequential organization" (§12.4.5.10.3 GR2/GR3/GR6), shared with the
            // §12.4.5.5.2 SR2 screen above and with every verb-side reroute, rather than a second open-coded
            // copy of the enum pair that a future organization would have to be added to twice.
            if (file.LockMode is { Multiple: true }
                && (file.AccessMode is FileAccessMode.Sequential || file.IsSequential))
                Edition.Error("COBOLNET1512", $"file '{name}': LOCK MODE … WITH LOCK ON MULTIPLE RECORDS may "
                    + "not be specified for a sequential-access or sequential-organization file (ISO §12.4.5.9.3 SR2)");
            // ⛔ §14.9.27.3 SR8 IS **NOT** CHECKED HERE, AND MUST NEVER BE RE-ADDED. It is a syntax rule of the
            // OPEN STATEMENT about file-name-1, which §14.9.27.2's general format defines as the OPEN
            // statement's operand: BOTH disjuncts of its antecedent presuppose an OPEN ("if the sharing phrase
            // is omitted from the OPEN statement and the ALL phrase is specified in the SHARING clause of the
            // file control entry for file-name-1 or if the ALL phrase is specified on the OPEN statement"), and
            // its leading conjunct ("When file-name-1 is not subject to an APPLY COMMIT clause") keys on the
            // I-O-CONTROL paragraph, which is bound LATER — so this site could not evaluate the antecedent even
            // if the rule reached it. The SHARING clause's own clause imposes nothing of the kind: §12.4.5.15.3
            // has exactly ONE general rule ("The SHARING clause specifies the sharing mode to be used for the
            // file unless it is overridden by the SHARING phrase of the OPEN statement") and NO syntax rules, so
            // §14.9.27.3 SR8 is the standard's only source of a LOCK MODE requirement anywhere.
            // A second copy here dropped the antecedent and rejected legal source — a file whose OPEN supplies
            // its own non-ALL sharing phrase, and a file never opened at all — while duplicating the diagnostic
            // for programs that DO violate it. THE ONE HOME IS StatementValidation.CheckOpenSharingAllOther,
            // called from SequentialIoBinder.BindOpen per file-name (kb/Work PB319, feedback_one_rule_one_place;
            // conformance:OpenSharingLockModeTests pins the single-diagnostic count).
            _files.Add(file);
            DeclareUserWord(name, UserWordKind.FileName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB65, PB1083)
            FilesByName[name] = file;
            if (screened) _selectedFiles[name] = file;
        }
    }

    /// <summary>THE one site that refuses the <c>record-key-name-1 SOURCE IS { data-name-2 } …</c> key form of the
    /// RECORD KEY (ISO §12.4.5.12.2) and ALTERNATE RECORD KEY (§12.4.5.6.2) clauses — Annex A.3 item 40's
    /// processor-dependent element, declined at docs/CONFORMANCE.md §2 row 40 (kb/Work PB358).
    /// <para>ONE helper for BOTH clauses because Annex A.3 item 40 names both in ONE sentence and the two general
    /// formats print the SAME brace group; the caller supplies only the clause's own spelling, exactly as
    /// <see cref="ScreenFacility"/> and <c>SequentialIoBinder.DeclinedFilePhrase</c> do for their modules. Nothing
    /// record-key-name is captured on the <see cref="FileModel"/> and none may be: the clause is refused, so a
    /// stored record-key-name would be a field nothing reads (feedback_a_dead_lookup_is_also_unverified).
    /// <see cref="FileModel.RecordKeyClauseWritten"/> is set by the CALLER instead, and is a different fact —
    /// that the clause APPEARED — which is what §12.4.5.1 Format 1's requiredness rule asks; without it the
    /// program was told, in the same compile, both that this key form is not provided and that its indexed file
    /// "has no RECORD KEY clause" (MEASURED on the probe, not assumed).</para>
    /// <para>The spelling uses <see cref="Spelled"/>, not <see cref="AsWritten"/>: a source slice of a clause
    /// continued onto a second line carries the RAW NEWLINE AND ITS INDENT into the message, which was measured
    /// on the ALTERNATE RECORD KEY probe. The direct-children join is exactly the clause's tokens, one space
    /// apart — the same choice the RECORD DELIMITER decline above makes.</para></summary>
    private void DeclineRecordKeySource(Core.RecordKeySourcePhraseContext src, string clause, string fileName)
    {
        using var _ = Edition.At(src);
        // The phrase is declined, but record-key-name-1 is declared by it all the same (§8.3.2.2; kb/Work PB1083).
        DeclareUserWord(src.recordKeyName().GetText(), UserWordKind.RecordKeyName);
        Edition.Declined(DiagnosticCatalog.RecordKeySourcePhraseUnsupported,
            $"the {clause} clause on file '{fileName}' ({Spelled(src)})");
    }

    /// <summary>Capture one file-control COLLATING SEQUENCE clause as written (ISO §12.4.5.7) — resolved to per-key
    /// weight tables post-build in <see cref="ResolveFileCollating"/> (the keys are not yet bound here). Format 2 is
    /// OF-led; Format 1 is the FOR-split or the IS alphabet-name-1 [alphabet-name-2] form.</summary>
    private void CaptureFileCollating(FileModel file, Core.FileCollatingSequenceClauseContext ctx)
    {
        using var _ = Edition.At(ctx);
        if (ctx.OF() is not null)   // Format 2 (key-level): OF {key}… IS alphabet-name-3
        {
            // Each key through the ONE data-name-n capture (kb/Work PB1075): qualified names are legal (§8.4.2.2.1),
            // and SR6's "shall not be subscripted" is refused by name there.
            var keys = ctx.dataReference()
                .Select(d => ClauseDataName(d, "COLLATING SEQUENCE OF"))
                .Select(k => new CollatingKeyOperand { Name = k.Base, Qualifiers = k.Quals })
                .ToList();
            file.KeyLevelCollating.Add(new KeyLevelCollatingClause(keys, ctx.cobolWord(0).GetText(), Edition.Cursor));
            return;
        }
        if (file.FileLevelCollatingCount++ == 0) file.FileLevelCollatingAt = Edition.Cursor;   // §12.4.5.7.3 SR3 — at most one file-level clause
        file.FileLevelCollating = ChoiceIndicators.AlphabetPair(Edition, ctx.collatingForPhrase(),
            f => f.NATIONAL() is not null, f => f.cobolWord().GetText(), ctx.cobolWord(),
            $"file '{file.CobolName}' COLLATING SEQUENCE", "12.4.5.7.2 Format 1");
    }

    /// <summary>Resolve each INDEXED key's collating-weight table from the file's §12.4.5.7 COLLATING SEQUENCE
    /// clauses (post-build — the keys are bound by now). Per §12.4.5.7.4 the sequence for a key is, in order: (GR6)
    /// a Format-2 clause naming it; else (GR2/GR3) the Format-1 default for the key's CLASS; else (GR4/GR5) the
    /// native sequence of that class (null weights = ordinal) — see <see cref="ResolveKeyCollating"/>. The clause's
    /// syntax rules are screened HERE, as written and before any key is resolved: SR3 (one file-level clause),
    /// SR1/SR2 (the Format-1 alphabets' classes), SR4/SR5 (Format-2 names are declared keys), SR7 (alphabet-name-3
    /// against each named key's class) and SR8 (a key in at most one clause).
    /// <para>⛔ SR1/SR2 ARE RULES ABOUT THE CLAUSE, NOT ABOUT THE KEYS IT REACHES (kb/Work PB1074). They used to be
    /// checked lazily, inside the per-key resolution, so an alphabet-name-1 that every key's Format-2 clause
    /// shadowed was never looked at (`COLLATING SEQUENCE IS NOSUCH` compiled clean) and alphabet-name-2 was never
    /// read at all.</para></summary>
    private void ResolveFileCollating(FileModel file)
    {
        if (file.FileLevelCollatingCount > 1)
            Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': at most one file-level "
                + "COLLATING SEQUENCE clause may be specified in one file control entry (ISO §12.4.5.7.3 SR3)");
        if (file.FileLevelCollating is null && file.KeyLevelCollating.Count == 0) return;   // no clause → native

        // ⛔ THIS IS §12.4.5.2 SR8's FIRST SENTENCE over the collating-sequence-clause operand, not §12.4.5.7.1.
        // The clause appears ONLY in §12.4.5.1's Format 1 (indexed), so writing it specifies Format 1, and SR8 —
        // "Format 1 shall be specified only for an indexed file" — is the sentence that forbids it elsewhere.
        // §12.4.5.7.1 is the clause's GENERAL paragraph ("…for an indexed file"), which is descriptive prose and
        // states no obligation; the citation shipped here was inherited from it (CLAUDE.md rule 1, kb/Work
        // PB742). The rule's other operands — the RECORD KEY, ALTERNATE RECORD KEY and RELATIVE KEY clauses —
        // are rows of FileControlKeyRules; this one stays here because the test is also the GUARD that stops the
        // rest of this method (there are no keys on a non-indexed file to resolve alphabets for), and splitting
        // a guard from its report would leave the resolution running on an entry it has already refused.
        if (file.Organization != FileOrganization.Indexed)
        {
            Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': a file-control COLLATING "
                + "SEQUENCE clause is a Format-1 clause and shall be specified only for an indexed file — this "
                + $"file is {file.OrganizationFace} (ISO §12.4.5.2 SR8)");
            return;
        }

        // SR4/SR5: every Format-2 operand shall name a declared RECORD KEY or ALTERNATE RECORD KEY of this file. The
        // operand is a qualified data-name (kb/Work PB1075), so it is RESOLVED — §8.4.2.2 over the file's key items —
        // and compared by ITEM: two files' keys may share a name, and only qualification tells them apart.
        var keyItems = new List<DataItem>();
        if (file.RecordKeyItem is { } pkItem) keyItems.Add(pkItem);
        keyItems.AddRange(file.AlternateKeys.Select(a => a.Item));
        // SR8: "Neither data-name-1 nor record-key-name-1 shall be specified in more than one COLLATING SEQUENCE
        // clause." The boundary is the CLAUSE, and the screen is per OPERAND *within* it: §12.4.5.7.2's Format-2
        // figure is `OF { data-name-1 | record-key-name-1 } … IS alphabet-name-3`, and by §5.2.7 the ellipsis
        // applies to the brace group, so ONE clause may list the same key twice — and one clause is never "more
        // than one". Hence the shared ConstructOperandRegister (the clause is the construct), never a HashSet
        // hoisted out of the clause loop: that screened per operand across ALL clauses at once and rejected
        // `COLLATING SEQUENCE OF IX-KEY IX-KEY IS ALPHA-X` with a diagnostic the program itself falsified
        // (kb/Work PB703; PB364 is the same shape on §14.9.49.3's USE rules, the register's first consumer).
        // GR6 is unaffected by the repeat: ResolveKeyCollating takes the FIRST clause naming the key, and a key
        // listed twice in one clause is named by that one clause, so it resolves to that clause's alphabet-name-3.
        var namedIn = new ConstructOperandRegister<DataItem>(ReferenceEqualityComparer.Instance);
        foreach (var (keys, alphabet3, at) in file.KeyLevelCollating)
        {
            using var _ = Edition.At(at);
            foreach (var k in keys)
            {
                string written = WrittenQualified(k.Name, k.Qualifiers);
                if (_refusedClauseOperands.Contains(k.Name)) continue;   // SR6 / shape — refused at capture
                k.Key = CollatingKeyNamed(file, k, keyItems, written);
                if (k.Key is null) continue;
                if (namedIn.Register(k.Key) is ConstructOperand.Duplicate)   // an EARLIER clause named it
                    Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': key '{written}' is "
                        + "named in more than one COLLATING SEQUENCE clause (ISO §12.4.5.7.3 SR8)");
            }
            namedIn.EndConstruct();   // the clause is complete — a repeat from here on is across clauses
            // SR7 — alphabet-name-3 against the class of EACH key the clause names: "When the class of data-name-1
            // or record-key-name-1 is national, alphabet-name-3 shall reference an alphabet that defines a national
            // collating sequence; otherwise, alphabet-name-3 shall reference an alphabet that defines an
            // alphanumeric collating sequence." An undeclared alphabet breaks both halves and is said once.
            if (!Alphabets.ContainsKey(alphabet3) && !NationalAlphabets.ContainsKey(alphabet3))
            {
                Edition.Error(DiagnosticCatalog.FileCollatingAlphabet, $"file '{file.CobolName}': COLLATING SEQUENCE "
                    + $"OF … IS '{alphabet3}' — alphabet-name-3 names no alphabet declared in SPECIAL-NAMES "
                    + "(ISO §12.4.5.7.3 SR7)");
                continue;
            }
            foreach (var k in keys.Where(k => k.Key is not null).DistinctBy(k => k.Key!, ReferenceEqualityComparer.Instance))
                if (k.Key is { } key && ItemCategory.IsAlphanumericOrNational(key)
                    && CollatingAlphabetFault(alphabet3, IsNationalKey(key)) is { } fault)
                    Edition.Error(DiagnosticCatalog.FileCollatingAlphabet, $"file '{file.CobolName}': COLLATING "
                        + $"SEQUENCE OF '{WrittenQualified(k.Name, k.Qualifiers)}' IS '{alphabet3}' — "
                        + $"'{WrittenQualified(k.Name, k.Qualifiers)}' is of class "
                        + $"{(IsNationalKey(key) ? "national" : "alphanumeric")}, so alphabet-name-3 shall reference an "
                        + $"alphabet that defines {(IsNationalKey(key) ? "a national" : "an alphanumeric")} collating "
                        + $"sequence; {fault} (ISO §12.4.5.7.3 SR7)");
        }

        // SR1/SR2 — the Format-1 alphabet names, screened as WRITTEN, whatever keys they end up reaching.
        if (file.FileLevelCollating is var (alnum, nat))
        {
            using var _ = Edition.At(file.FileLevelCollatingAt);
            if (alnum is not null && CollatingAlphabetFault(alnum, national: false) is { } f1)
                Edition.Error(DiagnosticCatalog.FileCollatingAlphabet, $"file '{file.CobolName}': COLLATING SEQUENCE "
                    + $"'{alnum}' — alphabet-name-1 shall reference an alphabet that defines an alphanumeric collating "
                    + $"sequence; {f1} (ISO §12.4.5.7.3 SR1)");
            if (nat is not null && CollatingAlphabetFault(nat, national: true) is { } f2)
                Edition.Error(DiagnosticCatalog.FileCollatingAlphabet, $"file '{file.CobolName}': COLLATING SEQUENCE "
                    + $"'{nat}' — alphabet-name-2 shall reference an alphabet that defines a national collating "
                    + $"sequence; {f2} (ISO §12.4.5.7.3 SR2)");
        }

        file.PrimeKeyCollation = ResolveKeyCollating(file, file.RecordKeyItem);
        foreach (var alt in file.AlternateKeys)
            file.AlternateKeyCollations.Add(ResolveKeyCollating(file, alt.Item));
    }

    /// <summary>Why <paramref name="alphabet"/> does NOT define a collating sequence of the class asked for, as the
    /// clause's diagnostics print it — or null when it does. ⛔ THE ONE CLASS TEST for the three alphabet operands
    /// of §12.4.5.7.3 (SR1 alphabet-name-1 alphanumeric, SR2 alphabet-name-2 national, SR7 alphabet-name-3 of the
    /// key's class). The two alphabet domains are disjoint (§12.3.7: an ALPHABET clause defines one class), so the
    /// lookup IS the class test; a national alphabet naming a coded character set only (UTF-8 / UTF-16 — §12.3.7.4
    /// Table 6's empty collating-sequence column) defines no sequence at all.
    /// <para>⛔ ASKED BY EVERY SITE THAT NAMES AN ALPHABET AS A COLLATING SEQUENCE (kb/Work PB1082, PB1074): the
    /// file-control COLLATING SEQUENCE clause, the OBJECT-COMPUTER PROGRAM COLLATING SEQUENCE clause
    /// (<c>ResolveProgramCollating</c>) and the SORT/MERGE COLLATING SEQUENCE phrase (<c>SortBinder</c>). The program
    /// collating sequence kept its own copy of this test and made alphabet-name-1's undeclared arm inert — its
    /// alphabet-name-2 slot refused the same shapes (the two-arm dispatch).</para>
    /// <para>A word whose ALPHABET clause DID declare it but failed its own screens has no fault of its own: that
    /// clause reported already, and a second "not declared" would be false about the source (one fault, one
    /// verdict) — the caller then finds no definition to apply and continues on the native order, in a compilation
    /// that cannot produce a program.</para></summary>
    internal string? CollatingAlphabetFault(string alphabet, bool national)
    {
        if (!Alphabets.ContainsKey(alphabet) && !NationalAlphabets.ContainsKey(alphabet)
            && _wordTypes.TryGetValue(alphabet, out var declaredAs) && declaredAs == UserWordKind.AlphabetName)
            return null;
        if (national)
            return NationalAlphabets.TryGetValue(alphabet, out var nd)
                ? nd.HasCollatingSequence ? null
                    : $"'{alphabet}' ({nd.Phrase}) names a national coded character set only (ISO §12.3.7.4 Table 6)"
                : Alphabets.ContainsKey(alphabet) ? $"'{alphabet}' is an alphanumeric alphabet"
                : $"'{alphabet}' is not an alphabet declared in SPECIAL-NAMES";
        return Alphabets.ContainsKey(alphabet) ? null
            : NationalAlphabets.ContainsKey(alphabet) ? $"'{alphabet}' is defined FOR NATIONAL"
            : $"'{alphabet}' is not an alphabet declared in SPECIAL-NAMES";
    }

    /// <summary>The message of a COLLATING SEQUENCE alphabet that does not define the class its slot asks for —
    /// one text for the two statement-level sites that share <see cref="CollatingAlphabetFault"/> (PROGRAM COLLATING
    /// SEQUENCE and the SORT/MERGE phrase), alphabet-name-1 being the alphanumeric slot and alphabet-name-2 the
    /// national one. <paramref name="rule"/> names the slot's own syntax rule, never its sibling's.</summary>
    internal static string CollatingAlphabetViolation(string site, string name, bool national, string fault, string rule) =>
        $"{site} '{name}': alphabet-name-{(national ? 2 : 1)} shall reference an alphabet that defines "
        + $"{(national ? "a NATIONAL" : "an ALPHANUMERIC")} collating sequence — {fault} ({rule})";

    /// <summary>A record key's CLASS for §12.4.5.7 — national when the key is category national (a PIC N item or a
    /// GROUP-USAGE NATIONAL group), alphanumeric otherwise. Only keys §12.4.5.12.3 SR2 / §12.4.5.6.3 SR2 admit
    /// (alphanumeric or national) reach a caller.</summary>
    private static bool IsNationalKey(DataItem key) => !ItemCategory.IsAlphanumeric(key);

    /// <summary>The RECORD KEY or ALTERNATE RECORD KEY item one key-level COLLATING SEQUENCE operand names — ISO
    /// §12.4.5.7.3 SR4/SR5: "shall be a name specified as the data-name in an ALTERNATE RECORD KEY clause or in a
    /// RECORD KEY clause in the file control entry" — or null, REPORTED, when it names none (kb/Work PB1075).
    /// <para>The operand is a qualified data-name, resolved by §8.4.2.2 like every other clause operand and narrowed
    /// to this file's key items: qualification is what separates two files' same-named keys (the case that used to
    /// be a parse error), and a reference that still leaves two key items is ambiguous (§8.4.2.2.3 SR1). A key clause
    /// whose own operand failed to resolve has already been reported, so an operand spelled like it is not reported
    /// a second time as "not a key" (one fault, one verdict).</para></summary>
    private DataItem? CollatingKeyNamed(FileModel file, CollatingKeyOperand k, IReadOnlyList<DataItem> keyItems,
                                        string written)
    {
        var named = QualifiedCandidates(k.Name, k.Qualifiers, Model.Scope.Program)
            .Where(i => keyItems.Contains(i, ReferenceEqualityComparer.Instance));
        if (named.Count > 0)
            return UniqueOrReportAmbiguous(named, $"file '{file.CobolName}': COLLATING SEQUENCE OF", written, out _);
        bool unresolvedKeyOfThatName =
            file.RecordKeyItem is null && string.Equals(file.RecordKeyName, k.Name, StringComparison.OrdinalIgnoreCase)
            || file.AlternateKeyNames.Any(a => a.Item is null && a.Name.Equals(k.Name, StringComparison.OrdinalIgnoreCase));
        if (!unresolvedKeyOfThatName)
            Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': COLLATING SEQUENCE OF "
                + $"'{written}' — '{written}' is not a RECORD KEY or ALTERNATE RECORD KEY of this file "
                + "(ISO §12.4.5.7.3 SR4/SR5)");
        return null;
    }

    /// <summary>Resolve one key's collating sequence BY THE KEY'S CLASS (§12.4.5.7.4): a Format-2 alphabet naming
    /// the key wins (GR6); else the Format-1 alphabet of the key's class — alphabet-name-1 for a key of class
    /// alphanumeric (GR2), alphabet-name-2 for a key of class national (GR3); else that class's native sequence
    /// (GR4/GR5 — null, ordinal). An alphabet whose class the screen above refused resolves to native (the compile
    /// has already failed).
    /// <para>⛔ KEY-CLASS-BLIND UNTIL kb/Work PB1074: every key took <c>keyLevel ?? FileLevelCollating.Alnum</c>, so an
    /// alphanumeric alphabet re-ordered — and re-judged the uniqueness of — a NATIONAL key (a spurious '22' on a
    /// legal WRITE), and <c>FOR NATIONAL IS alphabet-name-2</c> was stored and never read, silently.</para>
    /// <para>An ALPHANUMERIC alphabet resolves to its <see cref="AlphabetDef"/> — a literal-phrase table or, per
    /// owner decision Q3 (determination L8), a LOCALE sequence. A NATIONAL alphabet that is the native national
    /// sequence (NATIVE, UCS-4 — <see cref="NationalAlphabetDef.IsIdentity"/>) resolves to native, which is exactly
    /// GR3. One defined by literals or LOCALE is Annex A.3 item 41's processor-dependent capability, which this
    /// implementation does not provide: it is DECLINED by name (COBOLNET1584), and refused rather than accepted
    /// inert because an inert compile would order the key — and judge its uniqueness — by a different sequence
    /// than the one written (docs/CONFORMANCE.md §2 rows 41–42).</para></summary>
    private AlphabetDef? ResolveKeyCollating(FileModel file, DataItem? key)
    {
        if (key is null || !ItemCategory.IsAlphanumericOrNational(key)) return null;   // SR2 has refused it
        bool national = IsNationalKey(key);
        string? alphabet = null;
        var where = file.FileLevelCollatingAt;
        // GR6 — the FIRST key-level clause naming this key ITEM (operands resolved in ResolveFileCollating, kb/Work
        // PB1075; they were compared by spelling, so a qualified operand could not name its key).
        foreach (var clause in file.KeyLevelCollating)
            if (clause.Keys.Any(k => ReferenceEquals(k.Key, key))) { alphabet = clause.Alphabet; where = clause.At; break; }
        alphabet ??= national ? file.FileLevelCollating?.Nat : file.FileLevelCollating?.Alnum;   // GR3 / GR2
        if (alphabet is null) return null;                                                       // GR5 / GR4

        if (!national)
            return Alphabets.TryGetValue(alphabet, out var def) && !def.IsIdentity ? def : null;
        using var _ = Edition.At(where);
        if (NationalAlphabets.TryGetValue(alphabet, out var nd) && nd.HasCollatingSequence && !nd.IsIdentity)
            Edition.Declined(DiagnosticCatalog.FileCollatingNationalUnsupported, $"file '{file.CobolName}': national "
                + $"record key '{key.CobolName}' under COLLATING SEQUENCE '{alphabet}' ({nd.Phrase})");
        return null;
    }

    /// <summary>Map the <c>sharingMode</c> production to the model enum. ONE mapper for BOTH of its writers —
    /// the file control entry's SHARING clause (ISO §12.4.5.15) and the OPEN statement's sharing phrase
    /// (§14.9.27.2), which are the same grammar production and therefore the same three words. It was written
    /// twice, byte-identically, until kb/Work PB319 pulled the second copy out of
    /// <c>SequentialIoBinder</c> (feedback_one_rule_one_place): a fourth mode word would otherwise have to be
    /// added in two places, and only one of them would have been.</summary>
    internal static SharingMode MapSharing(Core.SharingModeContext m) =>
        m.READ() is not null ? SharingMode.ReadOnly
        : m.NO() is not null ? SharingMode.NoOther
        : SharingMode.AllOther;   // ALL OTHER

    /// <summary>Map the LOCK MODE clause (ISO §12.4.5.9): MANUAL / AUTOMATIC + single/MULTIPLE granularity.</summary>
    private static LockModeInfo MapLockMode(Core.LockModeClauseContext lm) =>
        new(lm.AUTOMATIC() is not null ? LockKind.Automatic : LockKind.Manual,
            lm.lockOnPhrase()?.MULTIPLE() is not null);

    /// <summary>A FILE-CONTROL key reference: the base word plus its IN/OF qualifier words in written order
    /// (innermost first, ISO §8.4.2.2). A raw <c>GetText()</c> would glue qualifiers into the lookup key
    /// (<c>IX-FD3-KEYINIX-FD3-RECKEY-AREA</c>) and the name could never resolve — the FILE STATUS / RENAMES
    /// capture pattern, applied to keys.
    /// <para>⛔ IT KEEPS THE QUALIFICATION AND DROPS EVERYTHING ELSE — a subscript or a reference modification
    /// written on the operand is DISCARDED here. That is why it is not the entry point for a <i>data-name-n</i>
    /// operand: <see cref="ClauseDataName"/> is, and it REFUSES those spellings first (kb/Work PB489), so the
    /// drop is now unreachable for every clause whose rules make them illegal. Until then, dropping them
    /// ACCEPTED illegal source instead of rejecting it (kb/Work PB205, measured on a reference-modified RECORD
    /// KEY). A clause that EXPRESSLY permits a ref-mod — §13.18.16.3 SR4's CONTROL operand and its §13.18.57.3
    /// SR10 / §13.18.54.3 SR8 twins — must NOT use this helper alone either: it binds as if the operand were
    /// unmodified, which is a silent wrong answer. Those three go through
    /// <c>DataBinder.Reports.ControlOperandRef</c>, which keeps the whole written reference.</para></summary>
    internal static (string Base, IReadOnlyList<string> Quals) KeyReference(Core.DataReferenceContext dref)
    {
        string baseWord = dref.cobolWord()?.GetText() ?? dref.GetText();
        var quals = new List<string>();
        foreach (var s in dref.dataReferenceSuffix())
            if (s.qualification()?.cobolWord() is { } q)
                quals.Add(q.GetText());
        return (baseWord, quals);
    }

    /// <summary>The capture for an operand written where a general format prints <i>data-name-n</i>: the base
    /// word plus its IN/OF qualifiers (<see cref="KeyReference"/>), SCREENED first by
    /// <see cref="ScreenClauseOperandShape"/> against the shapes §8.4.2.2.2 Format 1 does not admit.
    /// <para>⚠ NOT every clause operand goes through here, and the exceptions are principled rather than
    /// historical: §13.18.16.3 SR4's CONTROL operand and its §13.18.57.3 SR10 / §13.18.54.3 SR8 twins EXPRESSLY
    /// permit a reference-modifier, so the data-name-n screen would reject source their own rules allow. Those
    /// three capture with <see cref="KeyReference"/> and apply their own operand rules (kb/Work PB205).</para>
    /// </summary>
    private (string Base, IReadOnlyList<string> Quals) ClauseDataName(Core.DataReferenceContext dref, string clauseFace)
    {
        if (ScreenClauseOperandShape(dref, clauseFace)) return KeyReference(dref);
        // REFUSED. The operand is recorded AS WRITTEN — never reduced to a word the programmer did not use as a
        // data-name, which is the mis-report PB489 measured (`LINAGE IS LINAGE-COUNTER OF LPF LINES` named the
        // FILE) — and registered so its post-build resolution stays SILENT: the refusal is the cause and the
        // "references no data item" verdict would be its consequence, not a second fault (cascade control, the
        // ReportUnidentified precedent). A written reference always contains a space or a parenthesis where a
        // data-name cannot, so the key can never collide with a real one.
        string written = WrittenText(dref);
        _refusedClauseOperands.Add(written);
        return (written, []);
    }

    /// <summary>Clause operands whose written shape <see cref="ScreenClauseOperandShape"/> already refused, keyed
    /// by their source text — read by <see cref="ResolveClauseOperand"/> so one fault draws one verdict.</summary>
    private readonly HashSet<string> _refusedClauseOperands = new(StringComparer.Ordinal);

    /// <summary>A reference AS THE PROGRAMMER WROTE IT. <c>GetText()</c> concatenates tokens with no separators
    /// (<c>LINAGE-COUNTEROFLPF</c>), which is unreadable in a diagnostic about the spelling itself.
    /// <para>⛔ ONE DEFINITION for every binder that quotes source back at the programmer — the EVALUATE
    /// selection-object screens quote a WHEN phrase and a range-expression, and <c>WHEN1ALSO2</c> /
    /// <c>"A"THRU5</c> are not what anybody wrote (kb/Work PB399), and neither is the qualified reference
    /// <c>WS-CINNOGRP</c> that COBOLNET1639 used to echo for <c>WS-C IN NOGRP</c> (kb/Work PB983).</para>
    /// <para>The body is the frontend's <see cref="CobolNet.Frontend.Parsing.WrittenSource.Of"/>, which a frontend
    /// formation rule quotes source through as well (kb/Work PB1413) — one definition, two layers.</para></summary>
    internal static string WrittenText(Antlr4.Runtime.ParserRuleContext ctx) => CobolNet.Frontend.Parsing.WrittenSource.Of(ctx);

    /// <summary>⛔ THE SHAPES A <i>data-name-n</i> OPERAND MAY NOT BE WRITTEN IN (kb/Work PB489). Where a general
    /// format prints <c>data-name-n</c>, the reference is a QUALIFIED-DATA-NAME — ISO §8.4.2.2.2 Format 1,
    /// <c>data-name-1 [ data-qualifier ] … [ file-report-qualifier ]</c> — and nothing else. The grammar feeds
    /// these clauses the general <c>dataReference</c> nonterminal, which is shared with every procedure-division
    /// identifier position and therefore also admits the three shapes this screen refuses:
    /// <list type="bullet">
    /// <item>a SPECIAL REGISTER — <c>LINAGE-COUNTER</c>, <c>LINE-COUNTER</c> or <c>PAGE-COUNTER</c>. These are
    /// §8.4.3.1 Format 10 / Format 11 IDENTIFIERS (qualified-linage-counter, qualified-report-counter), not
    /// qualified-data-names, and each carries its own placement rule besides: §8.4.3.14.3 SR1 —
    /// <i>"LINAGE-COUNTER may be referenced only in procedure division statements"</i>; §8.4.3.15.3 SR1 restricts
    /// PAGE-COUNTER and LINE-COUNTER to a report-section SOURCE clause and the procedure division. A file
    /// description entry is neither.</item>
    /// <item>a SUBSCRIPT. Subscripting belongs to §8.4.2.3's qualified-data-name-WITH-subscripts, an identifier
    /// form; Format 1 has no subscript, and each of these clauses independently forbids an operand
    /// <i>"subject to any OCCURS clauses"</i> (§13.18.34.3 SR1 for LINAGE, §12.4.5.12.3 SR1 / §12.4.5.6.3 SR1 /
    /// §12.4.5.13.3 SR1 for the keys), so a subscript can never be meaningful here.</item>
    /// <item>a REFERENCE-MODIFIER. §8.4.3.3.3's closing NOTE: <i>"Because the references to data items are
    /// restricted to identifiers, where data-name-n is used in a general format or syntax rule, then
    /// reference-modification is not permitted."</i></item>
    /// </list>
    /// <para>⛔ IT IS ONE SCREEN FOR ALL OF THEM because it is one obligation — "this operand is not a
    /// qualified-data-name" — and the alternative measured badly: the capture used to keep the FIRST cobolWord
    /// and discard the rest, so <c>LINAGE IS LINAGE-COUNTER OF LPF LINES</c> recorded the FILE NAME as the
    /// data-name and died at OPEN naming a word the programmer never wrote as a data item, while
    /// <c>LINAGE IS T-LINES (2) LINES</c> dropped the subscript and crashed the same way (kb/Work PB489). The
    /// key clauses' own silent drop of a subscript or ref-mod (kb/Work PB205) is the same sentence and ends
    /// here too.</para></summary>
    private bool ScreenClauseOperandShape(Core.DataReferenceContext dref, string clauseFace) =>
        ScreenDataNameShape(dref, clauseFace, Edition);

    /// <summary>The body of <see cref="ScreenClauseOperandShape"/>, static over the <see cref="EditionContext"/>
    /// so a PROCEDURE-DIVISION operand written where a general format prints <i>data-name-n</i> is refused by
    /// the SAME screen: the SORT (§14.9.40.2, both formats) and MERGE (§14.9.24.2) KEY phrases print
    /// <c>KEY { data-name-1 } …</c>, and §8.4.3.3.3's NOTE and §14.9.40.3 SR14 b) (<i>"Key data names shall not
    /// be subscripted"</i>) are the same obligation this screen discharges for the file clauses. Those keys used
    /// to reach the one reference resolver (Format 1) or <see cref="KeyReference"/> (Format 2), and BOTH kept
    /// the base item and dropped a reference-modifier — <c>SORT E ON ASCENDING KEY K(4:3)</c> sorted on all six
    /// characters of K (kb/Work PB481, measured).</summary>
    internal static bool ScreenDataNameShape(Core.DataReferenceContext dref, string clauseFace, EditionContext edition)
    {
        using var _ = edition.At(dref);
        string? register =
            dref.LINAGE_COUNTER() is not null ? "LINAGE-COUNTER"
            : dref.LINE_COUNTER() is not null ? "LINE-COUNTER"
            : dref.PAGE_COUNTER() is not null ? "PAGE-COUNTER" : null;
        if (register is not null)
        {
            edition.Error(DiagnosticCatalog.ClauseOperandNotADataName,
                $"{clauseFace} '{WrittenText(dref)}': the clause's general format prints data-name-n, which is a "
                + "qualified-data-name (ISO §8.4.2.2.2 Format 1); "
                + $"{register} is a special register — an identifier of §8.4.3.1 "
                + $"Format {(register == "LINAGE-COUNTER" ? "10" : "11")} — and "
                + (register == "LINAGE-COUNTER"
                    ? "\"LINAGE-COUNTER may be referenced only in procedure division statements\" (ISO §8.4.3.14.3 SR1)"
                    : "may be referenced only in a report-section SOURCE clause or in the procedure division "
                      + "(ISO §8.4.3.15.3 SR1)"));
            return false;   // one true sentence: the operand is not a data-name, so its suffixes say nothing more
        }
        // A subscript or a reference-modifier may be written on the base word OR on a qualifier (`qualification`
        // carries both suffix kinds too), and it is the same violation in either place: every name in
        // §8.4.2.2.2 Format 1 — data-name-1 and each data-name-2 — is a bare data-name.
        // ⛔ THE SUFFIXES ARE READ BY THE ONE LEXICAL READER, ReferenceResolver.ReadOperandSuffixes (kb/Work
        // PB481). A reference-modifier reaches the binder through TWO carriers frozen at lex time — the parsed
        // `refModPart` and the SUBSCRIPT-mode captured group with a depth-0 colon — and this screen used to walk
        // the parse tree itself and count EVERY `subscriptPart` as a subscript, so `RECORD KEY IS IX-KEY(1:3)`
        // (the captured carrier, the usual one) was refused as "written with a subscript". The refusal was right
        // and the sentence was false; the reader that knows both carriers is the only one that can say which.
        var sfx = ReferenceResolver.ReadOperandSuffixes(dref);
        bool subscripted = sfx.Subscripts > 0, refModified = sfx.RefMods > 0;
        if (subscripted)
            edition.Error(DiagnosticCatalog.ClauseOperandNotADataName,
                $"{clauseFace} '{WrittenText(dref)}' is written with a subscript; the clause's general format prints "
                + "data-name-n, which is a qualified-data-name (ISO §8.4.2.2.2 Format 1) and carries no subscript "
                + "— subscripting is §8.4.2.3's qualified-data-name-with-subscripts, an identifier form, and the "
                + "operand shall not be subject to any OCCURS clauses");
        else if (refModified)
            edition.Error(DiagnosticCatalog.ClauseOperandNotADataName,
                $"{clauseFace} '{WrittenText(dref)}' is reference-modified; \"where data-name-n is used in a general "
                + "format or syntax rule, then reference-modification is not permitted\" (ISO §8.4.3.3.3 NOTE)");
        return !subscripted && !refModified;
    }

    /// <summary>⛔ THE ONE LINKER of a file's implicitly shared record area — §13.18.33.4 GR3: "Multiple level 1
    /// entries subordinate to a FD or SD entry represent implicit redefinitions of the same area". Every record
    /// with a CHARACTER WINDOW becomes an implicit redefinition of the first such record (the FD and SD arms used
    /// to spell this loop twice). An OUT-OF-LINE record (<see cref="FileModel.IsOutOfLineRecord"/> — a
    /// dynamic-length record, a variable-length group, a pointer-class record) is NOT linked: it has no window
    /// over the character area (§8.5.1.10.3 lets its storage be "located elsewhere"; a pointer has no character
    /// image, CONFORMANCE.md A.1 item 216), and it shares the area at the transfer boundary instead
    /// (<see cref="FileModel.OutOfLineRecords"/> — determination D-FRA, kb/Work PB981). Linking it is what used to
    /// stage such an FD loud as COBOLNET0899: the class model has no shape for a member without a window.</summary>
    private static void LinkImplicitRecordArea(FileModel file)
    {
        if (file.CharacterAnchor is not { } anchor) return;
        foreach (var record in file.Records)
            if (!ReferenceEquals(record, anchor) && record.RedefinesTarget is null
                && !FileModel.IsOutOfLineRecord(record))
                record.SetRedefinition(anchor, RedefinitionKind.ImplicitFileRecord);
    }

    /// <summary>ISO §12.4.5.2 SR3 — <i>"For each file-name specified in a SELECT clause, there shall be a file
    /// description entry or a sort-merge file description entry in the file section of the factory, function,
    /// object, or program in which the SELECT clause is specified."</i> Asked once every FD and SD has attached
    /// (both set <see cref="FileModel.HasFd"/>), and separately from <see cref="BindFileSection"/> because a unit
    /// with no FILE SECTION at all has the same obligation. A SELECT with no description used to compile clean,
    /// and its first OPEN aborted the run unit as "a compiler defect" (kb/Work PB1077). The opposite direction, an
    /// FD or SD that no SELECT names, is reported where the entry binds.</summary>
    private void ScreenSelectsHaveDescriptions()
    {
        foreach (var (name, file) in _selectedFiles)
        {
            if (file.HasFd) continue;
            using var _ = Edition.At(file.EntryAt);
            Edition.Error(DiagnosticCatalog.SelectWithoutFileDescription, $"file '{name}' is specified in a SELECT "
                + "clause but has no file description entry (FD) or sort-merge file description entry (SD); ISO "
                + "§12.4.5.2 SR3");
        }
    }

    /// <summary>Bind the FILE SECTION's FD records into the storage forest (they emit as Program fields, like
    /// WORKING-STORAGE), attach them to their <see cref="FileModel"/>, and model the shared record area: multiple
    /// <c>01</c>s under one FD occupy ONE area (ISO §9.1.2), so each secondary record is synthesized as a REDEFINES of
    /// the first — the existing tier machinery then makes them alias one backing (the singular-pattern rule).</summary>
    private void BindFileSection(Core.ProgramUnitContext program, HashSet<string> rootNames)
    {
        var fs = program.dataDivision()?.fileSection();
        if (fs is null) return;
        foreach (var fd in fs.fileDescriptionEntry())
        {
            using var _ = Edition.At(fd);
            if (fd.fileName()?.GetText() is not { } name) continue;
            var records = BindEntries(fd.dataDescriptionEntry(), rootNames, EntrySection.File);
            if (!FilesByName.TryGetValue(name, out var file))
            {
                // ISO §13.4.5.3 SR1 — "File-name-1 shall be specified in a file control entry." The model is still
                // built, so the entry's records resolve and the compile reports this one error rather than a
                // cascade of undefined names (kb/Work PB1237; it used to compile clean and run I-O against an
                // empty assignment).
                Edition.Error(DiagnosticCatalog.FileDescriptionWithoutSelect, $"file description entry '{name}' names "
                    + "a file that no file control entry specifies; ISO §13.4.5.3 SR1: \"File-name-1 shall be "
                    + "specified in a file control entry.\"");
                file = new FileModel { CobolName = name, SelectName = name };
                _files.Add(file);
                FilesByName[name] = file;
            }
            file.HasFd = true;
            file.Records.AddRange(records);
            // §13.18.33.4 GR3: "Multiple level 1 entries subordinate to a FD or SD entry represent implicit
            // redefinitions of the same area" — an IMPLICIT redefinition, not a REDEFINES clause (kb/Work PB836).
            LinkImplicitRecordArea(file);
            // §13.4.5.3 SR5/SR7/SR8: the entry's format is decided ONCE, before any clause binds — from the SELECT's
            // organization and whether a REPORT clause is written ANYWHERE in the entry (SR2: any order). A clause
            // its format does not contain is refused and NOT bound (kb/Work PB1238).
            var clauses = fd.fileDescriptionClauses()?.fileDescriptionClause() ?? [];
            var format = FileDescriptionFormats.Of(file.Organization, clauses.Any(c => c.reportClause() is not null));
            foreach (var clause in clauses)
                if (!FileDescriptionClauseAdmitted(clause, format, file, name))
                    continue;
                else if (clause.recordClause() is { } rc)
                    BindRecordClause(rc, file);   // RECORD VARYING / m TO n → FileModel.Varying (ISO §13.18.43)
                else if (clause.codeSetClause() is { } cs)
                    BindCodeSetClause(cs, file, records);   // ISO §13.18.13 (kb/Work PB110)
                else if (clause.formatClause() is { } fmt)
                    // FORMAT clause (ISO §13.18.24) — Annex A.4.8 item 1), an OPTIONAL element this
                    // implementation does not claim (docs/CONFORMANCE.md §5). Recognized so it can be REFUSED
                    // BY NAME rather than parse-erroring or, worse, binding inert: §13.18.24.4 GR1 makes the
                    // clause change the on-medium representation, so an inert compile writes the wrong bytes.
                    // Every occurrence reports (this loop runs per clause per FD).
                    Edition.Declined(DiagnosticCatalog.FormatSelectWhenUnclaimed,
                        $"the FORMAT clause on file description entry '{name}' ({Spelled(fmt)})");
                else if (clause.linageClause() is { } lc)
                    BindLinageClause(lc, file);   // LINAGE logical-page model → FileModel.Linage (ISO §13.18.34)
                else if (clause.reportClause() is { } rep)
                    // REPORT(S) clause (ISO §13.18.46): the FD hosts these reports — a report FILE (§9.1.22,
                    // legally record-less). Names resolve to ReportModels post-build (ResolveReports).
                    foreach (var rn in rep.reportName())
                        file.ReportNames.Add(rn.GetText());
                else if (clause.fileGlobalExternalClause() is { } ge)
                {
                    // FD IS EXTERNAL / IS GLOBAL (ISO §13.18.22 / §13.18.30): EXTERNAL ⇒ one run-unit file
                    // connector + external record data (GR4a/GR4b), externalized as the FD name (GR5); GLOBAL ⇒
                    // the file-name and record-names are global names, inherited by contained programs.
                    if (ge.EXTERNAL() is not null)
                    {
                        file.IsExternal = true;
                        // GR5's two sentences, in order: literal-1 when the AS phrase is written, the FD's own
                        // file-name otherwise (kb/Work PB511). The literal is a character-string, not a COBOL
                        // word, so its case is kept, while the file-name is uppercased because §8.3.2 makes a
                        // user-defined word case-insensitive. §13.18.22.3 SR3 screens it through the ONE shared
                        // externalized-name screen (COBOLNET2156), which also FORMS it — leading and trailing
                        // spaces removed (DOC-A.1-68, kb/Work PB1539).
                        file.ExternalName =
                            (ge.externalizedNamePhrase() is { } asPhrase
                                ? ExternalizedName.Screen(asPhrase.literal(), Edition,
                                    DiagnosticCatalog.ExternalClauseAsLiteral,
                                    $"file '{name}' IS EXTERNAL AS {asPhrase.literal().GetText()}", "literal-1",
                                    "ISO §13.18.22.3 SR3", LiteralEnv, rejectZeroLength: true)
                                : null)
                            ?? name.ToUpperInvariant();
                    }
                    if (ge.GLOBAL() is not null) file.IsGlobal = true;
                }
            // §13.4.5.3 SR8 — "Format 3 is the file description entry for a report file. No record description
            // entries or constant entries shall be associated with the file description entry for a report file."
            // Asked after the clause loop because the REPORT clause may follow any other clause (kb/Work PB1171:
            // such a record used to compile, and a WRITE of it injected a line into the report output).
            if (file.IsReportFile && fd.dataDescriptionEntry() is { Length: > 0 } entries)
                Edition.Error(DiagnosticCatalog.ReportFileRecordEntry, $"file description entry '{name}' specifies a "
                    + $"REPORT clause and has {entries.Length} record description or constant "
                    + $"entr{(entries.Length == 1 ? "y" : "ies")} subordinate to it — ISO §13.4.5.3 SR8: \"No record "
                    + "description entries or constant entries shall be associated with the file description entry "
                    + "for a report file.\" Remove them; the report's lines come from its REPORT SECTION description.");
            // §13.16.3 SR7's FD half — every WRITTEN record of an EXTERNAL / GLOBAL FD has a data-name
            // (DataBinder.ClausePlacement.cs; kb/Work PB518). Before the implied record, which is not written.
            ScreenFileRecordEntryNames(file);
            // ⛔ LAST, because it reads the RECORD, REPORT(S) and (via the SELECT) ORGANIZATION clauses: a file
            // description entry with NO record description entries still has a record area, and it is created
            // HERE rather than worked around at each of its consumers (kb/Work PB345).
            if (file.Records.Count == 0) MaterializeImpliedRecord(file, name, rootNames);
        }

        // SD entries (ISO §13.4.6): a sort-merge file's records bind through the SAME entry path as FD records —
        // they emit as Program fields and multi-01 records share ONE area (synthesized REDEFINES, ISO §9.1.2).
        // The SD format admits only the record clause (§13.4.6); DATA RECORDS is an obsolete '85 element DELETED
        // by ISO/IEC 1989:2002 — accepted-inert at 85 (every NIST SD writes it), rejected ≥2002.
        foreach (var sd in fs.sortMergeDescriptionEntry())
        {
            using var _ = Edition.At(sd);
            if (sd.fileName()?.GetText() is not { } sdName) continue;
            var sdRecords = BindEntries(sd.dataDescriptionEntry(), rootNames, EntrySection.File);
            if (!FilesByName.TryGetValue(sdName, out var sdFile))
            {
                // ISO §13.4.6.3 SR1 — the SD twin of the FD arm above (kb/Work PB1290).
                Edition.Error(DiagnosticCatalog.FileDescriptionWithoutSelect, $"sort-merge file description entry "
                    + $"'{sdName}' names a file that no file control entry specifies; ISO §13.4.6.3 SR1: \"File-name-1 "
                    + "shall be specified in a file control entry.\"");
                sdFile = new FileModel { CobolName = sdName, SelectName = sdName };
                _files.Add(sdFile);
                FilesByName[sdName] = sdFile;
            }
            sdFile.HasFd = true;
            sdFile.IsSortMerge = true;   // referenced only by SORT/MERGE/RELEASE/RETURN (§13.4.6 SR3/SR4)
            // ⛔ The SD arm does NOT inherit §13.4.5.3 SR3's permission to omit the record descriptions — that is a
            // FORMATS 1 AND 2 rule, and §13.4.6.3 SR2 requires them here. Until kb/Work PB345 a record-less SD
            // compiled and its SORT announced itself a COMPILER GAP (COBOLNET1756, "SORT 'S1' without an SD
            // record — not implemented") before aborting the run unit: the compiler apologising for the source's
            // error. It reads THIS entry's own records, so it needs nothing the clause loop below sets — unlike
            // the FD arm's MaterializeImpliedRecord, which reads the RECORD and REPORT(S) clauses and runs last.
            if (sdRecords.Count == 0)
                Edition.Error(DiagnosticCatalog.FileDescriptionRecordRequired, $"sort-merge file description entry "
                    + $"'{sdName}': one or more record description entries shall be associated with the sort-merge "
                    + "file description entry (ISO §13.4.6.3 SR2) — a SORT or MERGE key is a data item within a "
                    + "record of this file (§14.9.40.3 SR6 a), so there is nothing for the KEY phrase to name.");
            sdFile.Records.AddRange(sdRecords);
            LinkImplicitRecordArea(sdFile);   // §13.18.33.4 GR3 — the SD twin of the FD arm (kb/Work PB836)
            foreach (var clause in sd.sortMergeDescriptionClauses()?.sortMergeDescriptionClause() ?? [])
            {
                if (clause.recordClause() is { } rc)
                    BindRecordClause(rc, sdFile);
                // (The SD DATA RECORDS 0873 gate MIGRATED to the version-conformance pass parse-arm (VisitDataRecordsClause) — one
                // enforcement site covering FD AND SD via the shared grammar rule; P2.6 / Table-7 row 7.1.)
            }
        }
    }

    /// <summary>Whether one written FD clause belongs in this entry, reporting it when it does not (kb/Work PB1238).
    /// Two rules, both keyed on facts decided before the clause loop: the §13.4.5.2 format's clause list
    /// (<see cref="FileDescriptionFormats"/>, COBOLNET2604 — including the file control entry's clauses, which no
    /// FD format contains) and §13.4.5.3 SR4's LINE SEQUENTIAL ban on BLOCK CONTAINS and RECORD CONTAINS
    /// (COBOLNET2605). The <c>unrecognizedClause</c> error production has no row and is admitted here: its own pass
    /// refuses it by name (COBOLNET1970).</summary>
    private bool FileDescriptionClauseAdmitted(
        Core.FileDescriptionClauseContext clause, FileDescriptionFormat format, FileModel file, string fdName)
    {
        if (FileDescriptionFormats.RowOf(clause) is not { } row) return true;
        using var _ = Edition.At(clause);
        if (row.HomeEntry is { } home)
        {
            Edition.Error(DiagnosticCatalog.FileDescriptionClauseFormat, $"the {row.Name} ({WrittenText(clause)}) is "
                + $"written in the file description entry for '{fdName}', but it is a clause of {home}, and no ISO "
                + "§13.4.5.2 file description format contains it — write it in the SELECT entry for "
                + $"'{fdName}'. It is not bound here, so it has no effect on the file.");
            return false;
        }
        if ((row.Formats & format) == 0)
        {
            Edition.Error(DiagnosticCatalog.FileDescriptionClauseFormat, $"the {row.Name} ({WrittenText(clause)}) is not "
                + $"a clause of {FileDescriptionFormats.Caption(format)} of the file description entry: file "
                + $"'{fdName}' is {file.OrganizationFace}"
                + (format == FileDescriptionFormat.Report ? ", and its entry specifies a REPORT clause" : "")
                + $", and {FileDescriptionFormats.BindingRule(format)}. §13.4.5.2 prints that format with only "
                + $"the {FileDescriptionFormats.Admitted(format)}.");
            return false;
        }
        // §13.4.5.3 SR4 (FORMATS 1 AND 3 — the formats a LINE SEQUENTIAL file can be written in): "neither the
        // BLOCK CONTAINS clause nor the RECORD CONTAINS clause shall be specified". RECORD CONTAINS is the record
        // clause's Format 1 and Format 3 (§13.18.43.2, the arms spelled RECORD CONTAINS); the VARYING arm stays
        // legal, as §14.9.51.4 GR22 describes a line sequential file whose RECORD clause has the DEPENDING phrase.
        if (file.Organization == FileOrganization.LineSequential
            && (clause.blockContainsClause() is not null || clause.recordClause() is { } rc && rc.VARYING() is null))
        {
            Edition.Error(DiagnosticCatalog.LineSequentialBlockOrRecordContains, $"file '{fdName}' is LINE SEQUENTIAL "
                + $"and its file description entry specifies {WrittenText(clause)} — ISO §13.4.5.3 SR4: \"If the LINE "
                + "SEQUENTIAL phrase of the ORGANIZATION clause … is specified neither the BLOCK CONTAINS clause nor "
                + "the RECORD CONTAINS clause shall be specified.\" A line sequential record ends at its line "
                + "delimiter; remove the clause (RECORD IS VARYING remains available).");
            return false;
        }
        return true;
    }

    /// <summary>ISO §14.9.30.4 GR6's IMPLIED record description, made a REAL record at bind time (kb/Work PB345).
    /// <para>§13.4.5.3 SR3 expressly contemplates a file description entry with no record description entries —
    /// "When no record description entries are specified: a) a RECORD clause shall be specified …" — and SR7
    /// confines the <i>one or more record description entries</i> requirement to INDEXED files, so a sequential or
    /// relative FD written without a level-01 is legal source. GR6 then says what its record area IS: a READ INTO
    /// on such a file "proceeds as though there were one record description entry describing an alphanumeric group
    /// item of the maximum size established by the RECORD clause". This method materializes exactly that entry —
    /// an unnamed level-01 group over one FILLER of the RECORD clause's maximum size.</para>
    /// <para>⛔ IT IS SYNTHESIZED HERE, NOT WORKED AROUND AT THE CONSUMERS, and that is the whole fix. Before it,
    /// <see cref="FileModel.AreaRecord"/> answered null and FIVE consumers each carried their own null arm:
    /// <c>SequentialIoEmitter.EmitFileRegistration</c> registered the connector only when the FD hosted a REPORT,
    /// <c>KeyedIoEmitter.EmitRegistration</c> returned outright, and the READ record-area store and implicit INTO
    /// move were both guarded on it — so every I-O verb on a legal record-description-less file either aborted the
    /// run unit ("file connector … was never registered") or, had registration alone been repaired, would have
    /// read '00' and left identifier-1 unchanged. With the record real, registration, the area store and the INTO
    /// move all fall out of the ordinary path, for BOTH organizations, from ONE place.</para>
    /// <para>A GROUP and not an elementary PIC X(n): the distinction is observable. §14.9.25.4 makes a MOVE whose
    /// sending operand is a group item a group (alphanumeric) move, so <c>READ … INTO</c> a numeric receiver copies
    /// bytes; an elementary alphanumeric sender would instead convert. GR6 says "group item", so a group it is.</para>
    /// <para>The record is kept OFF <see cref="ByName"/>: the entry is IMPLIED, so it has no record-name and the
    /// program cannot reference it (which is also why §13.4.5.3 SR3 b/c require the FILE … FROM and INTO phrases on
    /// the verbs). It IS on <see cref="Roots"/>, because it is real storage that has to emit.</para></summary>
    private void MaterializeImpliedRecord(FileModel file, string fdName, HashSet<string> rootNames)
    {
        // §13.4.5.3 SR8: "No record description entries or constant entries shall be associated with the file
        // description entry for a report file." — a report file's area is the report engine's line, and
        // §13.18.43.3 SR1 excludes it from the RECORD-clause requirement by name. Nothing to synthesize.
        if (file.IsReportFile) return;
        // §13.4.5.3 SR7 withdraws SR3's permission for an INDEXED file. Rejecting is the whole repair: a prime
        // RECORD KEY has to be locatable in a record image (§12.4.5.12.3 SR2), which is why the emitter's
        // fallback for an unlocatable key was a run-time abort — the compiler announcing the SOURCE's error.
        if (file.Organization == FileOrganization.Indexed)
        {
            Edition.Error(DiagnosticCatalog.FileDescriptionRecordRequired, $"file description entry '{fdName}': "
                + "for an indexed file, one or more record description entries shall be associated with the file "
                + "description entry (ISO §13.4.5.3 SR7) — §13.4.5.3 SR3's permission to omit them is withdrawn "
                + "here, because the prime RECORD KEY shall reference a data item within a record description "
                + "entry associated with this file (§12.4.5.12.3 SR2).");
            return;
        }
        // "the maximum size established by the RECORD clause" (§14.9.30.4 GR6): format 1's integer-1, format 3's
        // integer-5, or format 2's integer-3. A format-2 clause with no TO phrase establishes none — §13.18.43.4
        // GR10 defers to "the greatest number of bytes described for a record in that file" and this file
        // describes none — and neither does an absent clause, which §13.4.5.3 SR3 a) / §13.18.43.3 SR1 forbid.
        int? max = file.Varying?.Max ?? file.RecordContains;
        if (max is not > 0)
        {
            Edition.Error(DiagnosticCatalog.RecordLessFdNoRecordSize, $"file description entry '{fdName}': it "
                + "specifies no record description entries, so a RECORD clause establishing a maximum record size "
                + "shall be specified (ISO §13.4.5.3 SR3 a / §13.18.43.3 SR1) — §14.9.30.4 GR6 sizes this file's "
                + "implied record description by \"the maximum size established by the RECORD clause\", and "
                + $"{(max is null ? "no clause here establishes one" : $"the maximum established here is {max}")}. "
                + "Write RECORD CONTAINS n CHARACTERS, or RECORD IS VARYING IN SIZE TO n CHARACTERS.");
            return;
        }
        var record = new DataItem
        {
            Level = 1,
            DeclaredAt = Edition.Cursor,
            CobolName = null,   // implied, so unnamed and unreferenceable — never RegisterName'd
            CsName = Unique($"_impliedRecord{DataItem.Sanitize(fdName)}", rootNames),
        };
        record.Uid = _uidCounter++;
        record.RootSection = EntrySection.File;   // §14.9.30.4 GR6's implied record is a FILE SECTION record
        rootNames.Add(record.CsName);
        var area = new DataItem
        {
            Level = 2,
            DeclaredAt = Edition.Cursor,
            CobolName = null,
            CsName = $"_filler{_fillerCounter++}",
            Pic = new PicInfo(PicCategory.Alphanumeric, Usage.Display, Length: max.Value, Digits: 0, Scale: 0,
                Signed: false),
            Parent = record,
        };
        area.Uid = _uidCounter++;
        record.Children.Add(area);
        _roots.Add(record);
        file.Records.Add(record);
    }

    /// <summary>Bind a RECORD clause's variable-length forms into <see cref="FileModel.Varying"/> (ISO §13.18.43:
    /// <c>RECORD IS VARYING [FROM m] [TO n] [DEPENDING ON d]</c> and <c>RECORD CONTAINS m TO n</c> describe
    /// variable-length records; the fixed Format-1 <c>RECORD CONTAINS n</c> leaves it null). Shared by the FD and
    /// SD loops — ONE binding for the clause. The DEPENDING operand is a qualified-data-name like every other
    /// clause operand (<see cref="ClauseDataName"/>) and resolves post-build in <see cref="ResolveFiles"/>; it
    /// kept only the base word until kb/Work PB489, so a qualified one bound another item of that name.</summary>
    private void BindRecordClause(Core.RecordClauseContext rc, FileModel file)
    {
        // ⛔ The clause's OWN position, captured here for §13.18.43.3's syntax rules, which are screened
        // post-build by RecordClauseRules (SR3/SR4 compare against §13.18.43.4 GR8 byte counts over a forest
        // that is not complete yet). The RecordKeyAt pattern; set for EVERY format, on BOTH the FD and SD arms,
        // because it is set HERE and not at the two call sites.
        using var _ = Edition.At(rc);
        file.RecordClauseAt = Edition.Cursor;
        if (rc.VARYING() is null && rc.TO() is null)
        {
            // The fixed Format-1 RECORD CONTAINS integer-1 (ISO §13.18.43.2). It SIZES THE FILE — §13.18.43.4
            // GR6: "Integer-1 specifies the number of bytes contained in each record in the file" — through
            // FileModel.RecordWidth, for an FD and an SD alike (kb/Work PB1276), and a report file's line width
            // prefers it (COBOLNET_REPORT_WRITER_DESIGN §4).
            if (rc.integerLiteral() is { Length: > 0 } fixedLits && CobolNet.Validation.IntegerOperandRules.HostValue(fixedLits[0]) is var n0)
                file.RecordContains = n0;
            return;
        }
        var lits = rc.integerLiteral();
        int? lo = lits.Length > 0 ? CobolNet.Validation.IntegerOperandRules.HostValue(lits[0]) : null;
        int? hi = lits.Length > 1 ? CobolNet.Validation.IntegerOperandRules.HostValue(lits[1]) : null;
        if (rc.TO() is not null && lits.Length == 1) { hi = lo; lo = null; }
        string? dep = null;
        IReadOnlyList<string> depQuals = [];
        Editions.DiagnosticCursor depAt = default;
        if (rc.dataReference() is { } d)
        {
            (dep, depQuals) = ClauseDataName(d, "RECORD … DEPENDING ON");
            // The OPERAND's own position, inside the clause cursor kb/Work PB721 pushed at the method top.
            using var _dep = Edition.At(d);
            depAt = Edition.Cursor;
        }
        // FORMAT 2 vs FORMAT 3: the WORD VARYING is the discriminator, and it is load-bearing — §13.18.43.4 GR16
        // (the READ/RETURN INTO sending size) sits under the FORMAT 2 heading and §14.9.30.4 GR4 b) designates
        // the implied move an alphanumeric group move only for a RECORD IS VARYING clause (kb/Work PB339).
        file.Varying = new VaryingRecordInfo(lo, hi, dep, VaryingClause: rc.VARYING() is not null)
        {
            DependingQualifiers = depQuals, DependingAt = depAt,
        };
    }

    /// <summary>Bind a LINAGE clause into <see cref="FileModel.Linage"/> (ISO §13.18.34: <c>LINAGE IS
    /// {data-name-1 | integer-1} LINES [WITH FOOTING AT {data-name-2 | integer-2}] [LINES AT TOP
    /// {data-name-3 | integer-3}] [LINES AT BOTTOM {data-name-4 | integer-4}]</c>). Each operand is a fixed
    /// literal (GR6a) or a data-name (GR6b — read at the evaluation points), resolved post-build by
    /// <see cref="ResolveLinage"/>. Absent FOOTING/TOP/BOTTOM phrases stay null (GR1 — margins zero; no footing
    /// ⇒ no end-of-page condition independent of page overflow).
    /// <para>⛔ A DATA-NAME OPERAND KEEPS ITS WHOLE WRITTEN REFERENCE. It used to keep
    /// <c>d.cobolWord()?.GetText()</c> — the FIRST word — "the FILE STATUS capture pattern", which is wrong
    /// wherever the name can be QUALIFIED: §13.18.34.2 prints <i>data-name-1</i>, a qualified-data-name
    /// (§8.4.2.2.2 Format 1), so <c>LINAGE IS SZ OF GRP-B LINES</c> is legal and — with two SZs — the only legal
    /// spelling, and the IN/OF qualifier was thrown away and the first same-named item bound instead. The whole
    /// logical page was then built on another item's value with no diagnostic at any stage (kb/Work PB489,
    /// measured: EOP / LINAGE-COUNTER 1 where the standard requires NO-EOP / 5). For the special-register
    /// alternative the first cobolWord is the FILE-NAME QUALIFIER, so the clause recorded the file name as its
    /// data-name; <see cref="KeyReference"/> refuses that spelling outright.</para></summary>
    private void BindLinageClause(Core.LinageClauseContext lc, FileModel file)
    {
        LinageOperand Operand(Core.DataReferenceContext? d, Core.IntegerLiteralContext? i)
        {
            if (i is not null)
            {
                // A literal operand keeps its cursor too: §13.18.34.3 SR3 reports AT integer-2 (ResolveLinage).
                using var lit = Edition.At(i);
                return new LinageOperand(CobolNet.Validation.IntegerOperandRules.HostValue(i), null) { At = Edition.Cursor };
            }
            var (name, quals) = ClauseDataName(d!, "LINAGE clause operand");
            using var _ = Edition.At(d!);
            return new LinageOperand(null, name) { Qualifiers = quals, At = Edition.Cursor };
        }
        file.Linage = new LinageInfo(
            Operand(lc.dataReference(), lc.integerLiteral()),
            lc.linageFootingPhrase() is { } f ? Operand(f.dataReference(), f.integerLiteral()) : null,
            lc.linageLinesAtTopPhrase() is { } t ? Operand(t.dataReference(), t.integerLiteral()) : null,
            lc.linageLinesAtBottomPhrase() is { } b ? Operand(b.dataReference(), b.integerLiteral()) : null);
    }

    /// <summary>Bind the I-O-CONTROL paragraph (ISO §12.4.6). A record-area SAME clause (Format 2) makes the listed
    /// files "share a memory area for processing the current logical record … equivalent to an implicit redefinition
    /// of the area with records aligned on the leftmost byte position" (§12.4.6.4 GR2) — modeled by chaining each
    /// listed file's FIRST record as a synthesized REDEFINES of the first LISTED file's first record, exactly the
    /// multi-01-under-one-FD mechanism (the singular-pattern rule): the tier machinery then aliases every record of
    /// every listed file over ONE backing, and READ/WRITE/RELEASE image distribution gives the
    /// record-of-the-most-recently-read-file semantics for free. A sort/merge file may appear in a record-area
    /// clause (SR6 — ST131A's <c>READ FILE3</c> then <c>RELEASE S3</c> with no FROM relies on it). The file-area
    /// (Format 1) and sort-merge-area (Format 3) formats are storage-economy permissions (GR1/GR4 — shared/reusable
    /// ALLOCATION plus open-mode constraints on the program, nothing a typed-native runtime must alias) — bound as
    /// conformant no-ops; MULTIPLE FILE TAPE is obsolete and parsed-and-ignored (grammar note), and so is the
    /// X3.23-1985 RERUN clause (a checkpoint HINT with no program-visible effect — a null rerun facility is
    /// conforming; deleted by ISO 2002, 0902-gated ≥2002 by the version-conformance pass, VCR Table 7 row 7.15) —
    /// both skip through the non-SAME `continue` below by design. The APPLY COMMIT clause (§12.4.6.3) is the one
    /// other arm that is not a no-op here: the facility is declined (COBOLNET1709) but the clause still records
    /// WHICH FILES the source made "subject to an APPLY COMMIT clause" on <see cref="FileModel.SubjectToApplyCommit"/>,
    /// because §14.9.27.3 SR8's leading conjunct keys on exactly that and is reachable under <c>--permissive</c>
    /// (kb/Work PB319). EVERY SAME format also records its MEMBERSHIP on its files
    /// (<see cref="FileModel.SameClauses"/>, kb/Work PB1139), which the SORT and MERGE statements read for
    /// §14.9.40.3 SR10 / §14.9.24.3 SR11 (<c>SortBinder.ScreenSameClauses</c>). The remaining SR2–SR11 static
    /// legality checks of the clause itself (report/sort/file-area cross-membership consistency) are the
    /// diagnose-correctly track — staged with the version-conformance pass phase, not silently absent by oversight.</summary>
    private void BindIoControl(Core.ProgramUnitContext program)
    {
        foreach (var env in EnvDivisions(program)) BindIoControl(env);
    }

    private void BindIoControl(Core.EnvironmentDivisionContext env)
    {
        var io = env.inputOutputSection()?.ioControlParagraph();
        if (io is null) return;
        foreach (var clause in io.ioControlClause())
        {
            using var _ = Edition.At(clause);
            // The APPLY COMMIT clause (ISO §12.4.6.3) — the facility is DECLINED by name (COBOLNET1709,
            // DeclinedFacilityPass), but the clause still tells the binder WHICH FILES the source made "subject
            // to an APPLY COMMIT clause", and two syntax rules key on exactly that: §14.9.27.3 SR8's leading
            // conjunct (enforced at StatementValidation.CheckOpenSharingAllOther) and §12.4.5.9.3 SR1. The fact
            // is LIVE, not speculative: COBOLNET1709 is PermissiveInert, so under --permissive the clause is
            // accepted with a warning and the program compiles (kb/Work PB319). §12.4.6.3.2's operand list is a
            // repetition of an all-optional [file-name-1][identifier-1] pair, so a name that resolves to a file
            // IS file-name-1 and anything else is identifier-1 — the symbol table is the only discriminator.
            if (clause.applyCommitClause() is { } apply)
            {
                foreach (var operand in apply.dataReference())
                    if (FilesByName.TryGetValue(operand.GetText(), out var subject))
                        subject.SubjectToApplyCommit = true;
                continue;
            }
            if (clause.sameClause() is not { } same) continue;
            // ⛔ EVERY SAME FORMAT IS RECORDED ON ITS FILES (kb/Work PB1139): the clause's membership is what
            // §14.9.40.3 SR10 and §14.9.24.3 SR11 ask the SORT and MERGE statements about — "No pair of file-names in
            // the same SORT statement may be specified in the same SAME SORT AREA or SAME SORT-MERGE AREA clause" —
            // and until now only the RECORD AREA format was modelled (as a peer list), so those rules had nothing to read.
            var kind = same.RECORD() is not null ? SameClauseKind.RecordArea
                : same.SORT_MERGE() is not null ? SameClauseKind.SortMergeArea
                : same.SORT() is not null ? SameClauseKind.SortArea
                : SameClauseKind.Area;
            var members = same.fileName()
                .Select(fn => FilesByName.TryGetValue(fn.GetText(), out var named) ? named : null)
                .OfType<FileModel>().Distinct().ToList();
            var recorded = new SameClause(kind, members);
            foreach (var m in members) m.SameClauses.Add(recorded);
            // Only SAME RECORD AREA shares storage (Format 2) — SORT/SORT-MERGE AREA is Format 3, and SAME AREA
            // (Format 1) gives no record-area effect either (docs/CONFORMANCE.md §7, A.1 items 168-169).
            if (kind != SameClauseKind.RecordArea) continue;
            // ⛔ THE GROUP IS ONE STORAGE AREA FOR THE WHOLE RUNTIME ELEMENT, and that is also §14.9.6.4 GR7's
            // answer for CLOSE. GR7: "If file-name-1 is specified in a SAME RECORD AREA clause, the record area
            // is available to the runtime element if any of the file connectors referenced by the other
            // file-names in that SAME RECORD AREA clause are open." The area emitted here is a live typed field
            // of the program class, so a CLOSE of one member cannot take it away from a still-open sibling —
            // GR7's availability branch holds STRUCTURALLY, not by omission (pinned by
            // conformance:85/pb235_same_record_area_close). GR7's other branch — a successful CLOSE with no open
            // member left makes the area UNAVAILABLE — carries no obligation a conforming program can observe:
            // the standard defines nothing about referencing an unavailable record area (and Annex A.2 item 5
            // makes the unsuccessful case outright undefined), so WiseOwl COBOL's determination is that the storage
            // KEEPS ITS LAST CONTENT, documented at docs/CONFORMANCE.md §7, A.1 item 24 (kb/Work PB235).
            DataItem? anchor = null;
            foreach (var f in members)
            {
                if (f.Records.Count == 0) continue;
                // The area's CHARACTER half is linked here; an out-of-line record (FileModel.IsOutOfLineRecord —
                // D-FRA, kb/Work PB981) has no window over it and reaches the shared area through
                // FileModel.OutOfLineRecords, which is why every sharing file knows its peers
                // (FileModel.SameRecordAreaPeers, derived from the clause just recorded).
                if (f.CharacterAnchor is not { } fAnchor) continue;
                if (anchor is null) { anchor = fAnchor; continue; }
                // §12.4.6.4.4 GR2: "equivalent to an implicit redefinition of the area with records aligned on the
                // leftmost byte position" — implicit, like the FD's own records (kb/Work PB836).
                if (!ReferenceEquals(fAnchor, anchor) && fAnchor.RedefinesTarget is null)
                    fAnchor.SetRedefinition(anchor, RedefinitionKind.SameRecordArea);
            }
        }
    }

    /// <summary>Post-build (once the forest is indexed), resolve every file's data-name clause operands — FILE
    /// STATUS, the keys, ASSIGN … USING, RECORD … DEPENDING ON and the LINAGE operands — and run the syntax
    /// rules of the file control entry and of the LINAGE clause that need a RESOLVED data item (design D19/D25).
    /// Every operand goes through <see cref="ResolveClauseOperand"/> or <see cref="QualifiedCandidates"/>: ONE
    /// ISO §8.4.2.2 resolution, never a per-clause first-match lookup (kb/Work PB489).</summary>
    internal void ResolveFiles()
    {
        foreach (var file in Files)
        {
            if (file.FileStatusName is { } sn)
            {
                file.FileStatusItem = ResolveClauseOperand(sn, file.FileStatusQualifiers, "FILE STATUS", file.FileStatusAt);
                ScreenFileStatusItem(file);   // §12.4.5.8.3 — the clause's own syntax rules over the resolved item
            }
            // Keyed organizations: RECORD KEY / ALTERNATE RECORD KEY name items WITHIN the file's record
            // descriptions (ISO §12.4.5.12 SR2 / §12.4.5.6 SR2), possibly IN/OF-qualified (§8.4.2.2 — same-named
            // keys under different areas, IX215A); RELATIVE KEY is OUTSIDE the record (ISO §12.4.5.13 SR3) —
            // a plain name lookup.
            // ⛔ THE KEY CLAUSES KEEP THEIR OWN SELECTION, and it is not the uniqueness rule: SR2 puts the key
            // INSIDE one of this file's record descriptions, so where §8.4.2.2 leaves several candidates the one
            // in a record of THIS file is the one the clause means, and an operand that resolves outside them is
            // the case SR2's own FileControlKeyRules row reports. What changed with kb/Work PB489 is only WHERE
            // the candidates come from — the one §8.4.2.2 resolver, so the file-name qualifier (§8.4.2.2.2
            // Format 1's file-report-qualifier) works here too, instead of a private record-subtree walk.
            // ⛔ AND THE SELECTION IS COUNTED (kb/Work PB978): SR2 narrows the set to this file's records when any
            // survivor lies there, and what remains must be ONE — two same-named keys in this file's records, or
            // two outside them with none inside, are §8.4.2.2.3 SR1's ambiguity, never the first declared.
            HashSet<string> refusedKeys = new(StringComparer.OrdinalIgnoreCase);
            DataItem? InRecords(string keyName, IReadOnlyList<string> quals, string face, Editions.DiagnosticCursor at)
            {
                // ⛔ ALREADY REFUSED AT CAPTURE — one fault, one verdict (kb/Work PB481). ClauseDataName records a
                // refused operand AS WRITTEN (`IX-KEY(1:3)`), which no data item is named, so the key screen's SR2
                // row then added "references nothing described in this program" under COBOLNET0863 — a false
                // second verdict about an operand whose only fault is its shape. The RELATIVE KEY arm below never
                // had the cascade because ResolveClauseOperand already asks this set; the two record-key arms
                // resolve through their own SR2 selection and now ask it too (feedback_two_arm_dispatch).
                if (_refusedClauseOperands.Contains(keyName)) { refusedKeys.Add(keyName); return null; }
                var cands = QualifiedCandidates(keyName, quals, Model.Scope.Program);
                var inFile = cands.Where(i => RecordLayout.IsInRecordOfFile(file, i));
                using var __ = Edition.At(at);
                var hit = UniqueOrReportAmbiguous(inFile.Count > 0 ? inFile : cands, face,
                    WrittenQualified(keyName, quals), out bool ambiguous);
                if (ambiguous && hit is null) refusedKeys.Add(keyName);
                return hit;
            }
            if (file.RecordKeyName is { } rk)
                file.RecordKeyItem = InRecords(rk, file.RecordKeyQualifiers, "RECORD KEY", file.RecordKeyAt);
            foreach (var clause in file.AlternateKeyNames)
            {
                clause.SuppressWhen = ReadSuppressWhen(clause);
                if ((clause.Item = InRecords(clause.Name, clause.Qualifiers, "ALTERNATE RECORD KEY", clause.At)) is { } alt)
                    file.AlternateKeys.Add((alt, clause.Duplicates, KeySuppressionOf(alt, clause.SuppressWhen)));
            }
            ResolveFileCollating(file);   // §12.4.5.7 — per-key collating weights (needs the resolved keys)
            if (file.RelativeKeyName is { } rl)
                file.RelativeKeyItem = ResolveClauseOperand(rl, file.RelativeKeyQualifiers, "RELATIVE KEY", file.RelativeKeyAt);
            ResolveAssignUsing(file);   // §12.4.5.3 GR3 b + §12.4.5.2 SR7
            // ⛔ THE FILE CONTROL ENTRY'S OWN SYNTAX RULES, screened HERE — at the ENTRY, for EVERY declared file,
            // whatever the procedure division does with it. They used to run from KeyedIoBinder on the first keyed
            // VERB that named the file, so a program that only OPENed and CLOSEd a file whose keys break them
            // compiled clean (kb/Work PB699). One table, one screen: FileControlKeyRules.
            FileControlKeyRules.Screen(file, Edition, refusedKeys);
            // ⛔ The RECORD clause's own size syntax rules (ISO §13.18.43.3 SR3/SR4/SR5/SR9), screened HERE for
            // the same reason and by the same shape: they are rules of the file description ENTRY, and SR3/SR4
            // compare the clause's integers against §13.18.43.4 GR8's byte counts of the record descriptions,
            // which are settled only once the forest is bound. Until kb/Work PB721 nothing checked them at all
            // and `RECORD IS VARYING IN SIZE FROM 20 TO 5` compiled clean at every edition.
            RecordClauseRules.Screen(file, Edition);
            // RECORD VARYING … DEPENDING ON names an integer item outside the record (ISO §13.18.43 SR — the
            // length register WRITE/REWRITE/RELEASE read per GR13a and READ/RETURN set per GR15).
            if (file.Varying?.DependingName is { } vn)
                file.VaryingDependingItem = ResolveClauseOperand(
                    vn, file.Varying.DependingQualifiers, "RECORD … DEPENDING ON", file.Varying.DependingAt);
            ResolveLinage(file);   // §13.18.34 — the logical-page operands and §13.18.34.3's syntax rules
        }
    }

    /// <summary>⭐ ONE OPERAND OF A FILE'S data-name CLAUSE, RESOLVED (kb/Work PB489) — FILE STATUS, RELATIVE KEY,
    /// RECORD … DEPENDING ON, and each LINAGE operand. Every one of these clauses prints <i>data-name-n</i>, so
    /// every one of them means a qualified-data-name resolved by ISO §8.4.2.2, and the number of survivors is the
    /// answer: §8.4.2.2.1 — <i>"uniqueness shall be established through qualification for each user-defined name
    /// explicitly referenced"</i> — and §8.4.2.2.3 SR1 repeats it. Exactly one resolves; zero or several is a
    /// resolution failure REPORTED HERE.
    /// <para>⛔ THE FIRST-MATCH LOOKUP THIS REPLACES WAS THE SAME LINE IN FOUR PLACES:
    /// <c>ByName.TryGetValue(n, out var l) &amp;&amp; l.Count &gt; 0 ? l[0] : null</c>, which answered an ambiguous
    /// reference with the first declaration in source order and an undeclared one with silence. On the LINAGE
    /// clause that was a measured silent wrong answer (<c>LINAGE IS SZ LINES</c> with two SZs built the whole
    /// page model on the first), and on the other three it was never probed at all.</para>
    /// <para>It reports under the SAME descriptor as the procedure division's own unidentified reference
    /// (COBOLNET1639): the rule broken is §8.4.2.1/§8.4.2.2, not a rule of the clause, and a user who mis-qualifies
    /// a name in a WRITE and in a LINAGE clause has broken one rule and should read one verdict.</para></summary>
    private DataItem? ResolveClauseOperand(string name, IReadOnlyList<string> quals, string clauseFace,
                                           Editions.DiagnosticCursor at)
    {
        var survivors = QualifiedCandidates(name, quals, Model.Scope.Program);
        if (survivors.Single is { } one) return one;
        if (_refusedClauseOperands.Contains(name)) return null;   // already refused at capture — one fault, one verdict
        using var __ = Edition.At(at);
        string written = WrittenQualified(name, quals);
        if (survivors.Count > 1)
            return UniqueOrReportAmbiguous(survivors, clauseFace, written, out _);
        Edition.Error(DiagnosticCatalog.UndefinedReference,
            $"{clauseFace} '{written}': the clause's data-name references no data item — "
              + (Symbols.TryResolve(name, Model.Scope.Program, out _)
                  ? $"'{name}' is declared, but not under the given qualifier{(quals.Count == 1 ? "" : "s")} "
                    + "(ISO §8.4.2.2 — qualification shall establish uniqueness)"
                  : $"no declaration in this source element gives the name '{name}' (ISO §8.4.2.1: \"a statement "
                    + "shall contain a reference that uniquely identifies that resource\")"));
        return null;
    }

    /// <summary>Screen the FILE STATUS clause's data-name-1 against the clause's own syntax rules (ISO §12.4.5.8.3) —
    /// the FILE-CONTROL twin of <see cref="ResolveLinage"/>'s operand screen, run post-build for the same reason: an
    /// operand's OCCURS ancestry, category, size and section are known only once the data forest is bound.
    /// <para>⛔ NONE OF THE FOUR RULES HAD A SITE (kb/Work PB1080), so every item compiled, and what happened next was
    /// the emitter's accident: an OCCURS item died at OPEN ("not resolvable to storage"), a <c>PIC 99</c> item
    /// crashed the C# backend (CS0029), a BASED item died EC-DATA-PTR-NULL at OPEN, and <c>PIC X</c> /
    /// <c>PIC X(3)</c> / <c>PIC N(2)</c> / an FD record / a dynamic-length or variable-length item ran with a
    /// truncated, padded or wrong-category status.</para>
    /// <list type="bullet">
    /// <item>SR1 — <i>"Data-name-1 shall not be subject to any OCCURS clauses"</i>: <see cref="RecordLayout.IsSubjectToOccurs"/>,
    /// the reader §13.18.34.3 SR1 and the key clauses' SR1 share.</item>
    /// <item>SR4 — <i>"shall not be subject to a BASED clause in its data description"</i>: the item or a group it is
    /// subordinate to is BASED.</item>
    /// <item>SR3 — <i>"shall not reference a dynamic-length elementary item or a variable-length group"</i> (the one
    /// screen of that pair, <see cref="VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup"/>).</item>
    /// <item>SR2 — <i>"a two-character data item of the category alphanumeric, defined in the working-storage,
    /// local-storage, or linkage section"</i>. The category is <see cref="ItemCategory.IsAlphanumeric"/>, so a
    /// two-character alphanumeric GROUP qualifies — §13.18.29.4 GR3 makes a group with no GROUP-USAGE clause "an
    /// alphanumeric group item", the reading §12.4.5.2 SR7's ASSIGN USING operand already takes. "Two-character" is
    /// the item's size in character positions, fixed: a group whose size an OCCURS DEPENDING ON subordinate varies
    /// is not a two-character data item. The section half reads <see cref="DataItem.Section"/>, the fact the item
    /// carries from its root — so a GLOBAL item a contained program sees keeps the section its own program described
    /// it in.</item>
    /// </list>
    /// One verdict per item, in the order a fix would address them (the first rule that fails).</summary>
    private void ScreenFileStatusItem(FileModel file)
    {
        if (file.FileStatusItem is not { } item) return;   // unresolved — reported by the resolution, one verdict
        using var _ = Edition.At(file.FileStatusAt);
        string face = WrittenQualified(file.FileStatusName!, file.FileStatusQualifiers);
        string? fault =
            RecordLayout.IsSubjectToOccurs(item)
                ? "is subject to an OCCURS clause; data-name-1 shall not be subject to any OCCURS clauses "
                  + "(ISO §12.4.5.8.3 SR1)"
            : SubjectToBased(item)
                ? "is subject to a BASED clause; data-name-1 shall not be subject to a BASED clause in its data "
                  + "description (ISO §12.4.5.8.3 SR4)"
            : VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup(item) is { } variableShape
                ? $"is {variableShape}; data-name-1 shall not reference a dynamic-length elementary item or a "
                  + "variable-length group (ISO §12.4.5.8.3 SR3)"
            : FileStatusShapeFault(item) is { } shape
                ? $"is {shape}; data-name-1 shall reference a two-character data item of the category alphanumeric, "
                  + "defined in the working-storage, local-storage, or linkage section (ISO §12.4.5.8.3 SR2)"
            : null;
        if (fault is not null)
            Edition.Error(DiagnosticCatalog.FileStatusItemRule, $"file '{file.SelectName}': FILE STATUS '{face}' {fault}");
    }

    /// <summary>Which conjunct of §12.4.5.8.3 SR2 the FILE STATUS item breaks, as the phrase the diagnostic prints;
    /// null when it satisfies all three (category, size, section) — asked in the rule's own word order.</summary>
    private string? FileStatusShapeFault(DataItem item) =>
        !ItemCategory.IsAlphanumeric(item) ? $"{ItemCategory.Face(item)}, not of the category alphanumeric"
        : DataItem.HasOdoOnOrBeneath(item) ? "a group whose size an OCCURS DEPENDING ON clause varies, not a two-character data item"
        : item.ByteWidth != 2 ? $"{item.ByteWidth} character position{(item.ByteWidth == 1 ? "" : "s")} long, not a two-character data item"
        : item.Section is { } s and not (EntrySection.WorkingStorage or EntrySection.LocalStorage or EntrySection.Linkage)
            ? $"defined in the {SectionWords(s)}"
        : null;

    /// <summary>The item, or a group it is subordinate to, carries a BASED clause (§13.18.5 — "subject to").</summary>
    private static bool SubjectToBased(DataItem item)
    {
        for (DataItem? p = item; p is not null; p = p.Parent)
            if (p.IsBased) return true;
        return false;
    }

    /// <summary>The SUPPRESS WHEN operands as written, keyed by their clause (captured in BindFileControl, read by
    /// <see cref="ReadSuppressWhen"/> once the data division's constant-names are declared).</summary>
    private readonly Dictionary<AlternateKeyClause, Core.ValueClauseOperandContext> _suppressWhenOperands = [];

    /// <summary>Read an ALTERNATE RECORD KEY clause's SUPPRESS WHEN literal-1 (ISO §12.4.5.6.2) — null when the
    /// clause has none, or when the operand is no literal at all (reported here, once).
    /// <para>⛔ ONE READING, THROUGH THE ONE LITERAL-POSITION CHOKEPOINT (kb/Work PB1072). The phrase used to be
    /// read as <c>CobolLiteral.Decode(literal.GetText())</c>, which returns a figurative constant or an ALL literal
    /// UNCHANGED as its source spelling: <c>SUPPRESS WHEN SPACES</c> suppressed the six-letter word "SPACES" and
    /// indexed every blank key, and a constant-name or symbolic-character — both legal literal-1 spellings
    /// (§13.10.3 SR2; §8.3.3.6.3 SR1 with §8.3.3.6.2 Format 7) — did not parse at all. <see cref="RawValueOperandText"/>
    /// is the VALUE clause's and the EDITING phrase's own reader: it screens the literal position, substitutes a
    /// constant-name's and a symbolic-character's literal, and folds a concatenation expression (§8.8.3.3 GR3), so
    /// what reaches <see cref="FigurativeConstants.Classify"/> here is always ONE literal's raw text.</para>
    /// <para>What is recorded is the operand's SHAPE, which is what §12.4.5.6.3 SR7 screens
    /// (<c>FileControlKeyRules</c>); the key-sized value is <see cref="KeySuppressionOf"/>'s.</para></summary>
    private SuppressWhenOperand? ReadSuppressWhen(AlternateKeyClause clause)
    {
        if (!_suppressWhenOperands.TryGetValue(clause, out var op)) return null;
        using var _ = Edition.At(op);
        if (RawValueOperandText(op, $"ALTERNATE RECORD KEY '{clause.Name}'", LiteralPosition.SuppressWhen) is not { } raw)
            return null;
        string written = AsWritten(op);
        var form = CobolNet.CodeGen.FigurativeConstants.Classify(raw, includeNull: false);
        if (form.Kind is { } kind)
            return new(written, SuppressWhenForm.Figurative, null, "", kind);
        if (form.AllLiteral is { } literal1)   // ALL literal-1, and a symbolic-character's ALL "c" substitution
            return new(written, SuppressWhenForm.AllLiteral, CobolLiteral.ClassOf(literal1), CobolLiteral.Decode(literal1), null);
        return CobolLiteral.ClassOf(raw) is { } cls
            ? new(written, SuppressWhenForm.Literal, cls, CobolLiteral.Decode(raw), null)
            // A numeric literal, or the figurative constant NULL, which has no character value.
            : new(written, SuppressWhenForm.NotAlphanumericOrNational, null, "", null);
    }

    /// <summary>The §12.4.5.6.4 GR6 key suppression value of <paramref name="key"/>, in the key's own character
    /// positions — the operand the relation condition compares the key with (§12.4.5.6.4 GR4: equality "based on
    /// the collating sequence used for the file according to the rules for a relation condition"). Null when
    /// there is no SUPPRESS WHEN phrase or the key is no key §12.4.5.6.3 SR2 admits (already refused).
    /// <list type="bullet">
    /// <item>A keyword figurative is repeated to the key's size (§8.3.3.6.4 GR2 — a figurative "compared with" a
    /// fixed-length data item is associated with it, NOTE 1), its character taken in the key's category through
    /// <see cref="FigurativeConstants.FillChar"/> — the fill a <c>MOVE</c> of the same figurative into the key
    /// stores, so a program that moves SPACES into the key is exactly the program that suppressed it.</item>
    /// <item>ALL literal-1 is repeated the same way (GR2 again).</item>
    /// <item>A literal SHORTER than the key is extended with spaces (§8.8.4.2.7 2) — national spaces for a national
    /// key, which the encoder writes as the byte pair <c>00 20</c>); a LONGER one is cut back to the key's size when
    /// every excess position is a space (equal under the same rule) and otherwise kept whole — no key value can
    /// equal it, so it never suppresses.</item>
    /// </list>
    /// An operand §12.4.5.6.3 SR7 refuses still gets a value: the screen's error has already failed the compile.</summary>
    private KeySuppression? KeySuppressionOf(DataItem key, SuppressWhenOperand? operand)
    {
        if (operand is null || !ItemCategory.IsAlphanumericOrNational(key)) return null;
        bool national = !ItemCategory.IsAlphanumeric(key);
        int positions = national ? key.ByteWidth / CobolNet.Runtime.CobolBits.BytesPerNational : key.ByteWidth;
        var category = national ? PicCategory.National : PicCategory.Alphanumeric;
        string value = operand.Form switch
        {
            SuppressWhenForm.Figurative => new string(CobolNet.CodeGen.FigurativeConstants.FillChar(
                operand.FigurativeKind!.Value, Collating, category, NationalCollating), positions),
            SuppressWhenForm.AllLiteral when operand.Characters.Length > 0 =>
                string.Concat(Enumerable.Repeat(operand.Characters, positions / operand.Characters.Length + 1))[..positions],
            _ when operand.Characters.Length <= positions => operand.Characters.PadRight(positions),
            _ when operand.Characters.AsSpan(positions).IndexOfAnyExcept(' ') < 0 => operand.Characters[..positions],
            _ => operand.Characters,
        };
        return new KeySuppression(value, national);
    }

    /// <summary>Resolve the LINAGE clause's data-name operands and screen the clause's own syntax rules
    /// (ISO §13.18.34.3) — the FD-clause twin of <see cref="ResolveFileCollating"/>, run post-build because an
    /// operand's OCCURS ancestry is unknown until the data forest is indexed.
    /// <para>SR1, <i>"Data-name-1, data-name-2, data-name-3, and data-name-4 shall not be subject to any OCCURS
    /// clauses"</i>, is screened here. It had NO site at all: <c>LINAGE IS T-LINES LINES</c> over a table element
    /// compiled clean and died at OPEN OUTPUT with a runtime "not resolvable to storage" throw, which is a
    /// process kill where the standard requires a compile-time rejection (kb/Work PB489/PB524, measured). The
    /// WRITTEN subscript form — <c>LINAGE IS T-LINES (2) LINES</c> — is refused earlier, at the capture, because
    /// §13.18.34.2 prints data-name-1 and a subscript is not part of a qualified-data-name.</para>
    /// <para>SR2, <i>"Data-name-1, data-name-2, data-name-3, and data-name-4 shall reference elementary unsigned
    /// numeric integer data items"</i>, and SR3, <i>"Integer-2 shall not be greater than integer-1"</i>, are the
    /// other two tests of the SAME screen (kb/Work PB524). Neither had a site: the emitter ASSUMED SR2
    /// (<c>SequentialIoEmitter</c>'s "scale 0 by SR2's elementary-unsigned-integer rule, with a defensive
    /// rescale"), so an alphanumeric "007" was read as page size 7, a PIC 9V9 5.5 was truncated to 5, and a signed
    /// or SR3-breaking page reached run time and threw a CLR exception out of the page evaluation. SR2's
    /// "integer" is <see cref="PicInfo.IsIntegerDescription"/> — §5.5's fixed-point, no-fraction reading, the one
    /// predicate every integer-data-item rule reads, which also refuses a floating-point item and an index item
    /// (class index, never numeric). SR3 compares the two LITERALS only: with a data-name on either side the
    /// relation is a value at run time and §13.18.34.4 GR3 is its rule.</para>
    /// <para>SR4 (integer-3 / integer-4 may be zero) is a permission, honoured by the §5.5 1) integer-n screen
    /// (<c>IntegerOperandPass</c>), which is where a ZERO integer-1 or integer-2 is refused.</para>
    /// </summary>
    private void ResolveLinage(FileModel file)
    {
        if (file.Linage is not { } lin) return;
        foreach (var op in lin.Operands)
        {
            if (op.DataName is not { } name) continue;   // GR6 a) — a literal operand has no data item
            op.Item = ResolveClauseOperand(name, op.Qualifiers, "LINAGE clause operand", op.At);
            if (op.Item is not { } item) continue;   // unresolved — reported by the resolution, one verdict
            using var _ = Edition.At(op.At);
            if (RecordLayout.IsSubjectToOccurs(item))
                Edition.Error(DiagnosticCatalog.LinageClauseOperandRule,
                    $"LINAGE clause operand '{name}' is subject to an OCCURS clause; data-name-1, data-name-2, "
                    + "data-name-3, and data-name-4 shall not be subject to any OCCURS clauses "
                    + "(ISO §13.18.34.3 SR1)");
            else if (LinageOperandShapeFault(item) is { } fault)
                Edition.Error(DiagnosticCatalog.LinageClauseOperandRule,
                    $"LINAGE clause operand '{name}' is {fault}; data-name-1, data-name-2, data-name-3, and "
                    + "data-name-4 shall reference elementary unsigned numeric integer data items "
                    + "(ISO §13.18.34.3 SR2)");
        }
        // SR3 — both operands are literals, so the relation is known now and is a syntax rule.
        if (lin.Body.Literal is int page && lin.Footing is { Literal: int footing } foot && footing > page)
        {
            using var _ = Edition.At(foot.At);
            Edition.Error(DiagnosticCatalog.LinageClauseOperandRule,
                $"LINAGE clause: FOOTING AT {footing} is greater than the page size {page}; integer-2 shall not be "
                + "greater than integer-1 (ISO §13.18.34.3 SR3)");
        }
    }

    /// <summary>Which conjunct of §13.18.34.3 SR2 — "elementary unsigned numeric integer" — the item breaks, as
    /// the phrase the diagnostic prints; null when it satisfies all four. Asked in the rule's own word order so
    /// the FIRST failing word is the one named.</summary>
    private static string? LinageOperandShapeFault(DataItem item) =>
        item.IsGroup ? "a group item, not an elementary item"
        : item.Pic is not { Category: PicCategory.Numeric } pic ? $"{ItemCategory.Face(item)}, not a numeric data item"
        : pic.Usage is Usage.Index ? "an index data item (class index), not a numeric data item"
        : !pic.IsIntegerDescription
            ? (pic.IsFloat ? "a floating-point numeric data item, not an integer data item"
                : "a numeric data item with digits to the right of the decimal point, not an integer data item")
        : pic.Signed ? "a signed numeric data item, not an unsigned one"
        : null;

    /// <summary>
    /// Resolve the ASSIGN … USING data-name-1 (ISO §12.4.5.3 GR3 b — dynamic file assignment, §9.1.21) and enforce
    /// §12.4.5.2 SR7, <i>"Data-name-1 shall reference an alphanumeric data item and shall not be subordinate to the
    /// file description entry for file-name-1."</i> Both halves are checked, and the SECOND is the one a program can
    /// silently lose to: an operand inside the file's own record area is overwritten by every READ, so its content at
    /// the next OPEN would be file data rather than a file name.
    /// <para>Category: §9.1.21 says the same thing in the concepts ("The USING phrase references an alphanumeric data
    /// item"). An ALPHANUMERIC GROUP item qualifies — §13.18.29.4 GR3 makes a group with no GROUP-USAGE clause "an
    /// alphanumeric group item" — while a bit or national group does not, and neither does a category-ALPHABETIC
    /// (PIC A) item, which §8.8.4.2.4 keeps a class of its own ("A class alphabetic operand shall be treated as
    /// though it were an operand of class alphanumeric" is a COMPARISON rule, not a category identity).</para>
    /// </summary>
    private void ResolveAssignUsing(FileModel file)
    {
        if (file.AssignUsingName is not { } dynName) return;
        using var _ = Edition.At(file.AssignUsingAt);
        // The §8.4.2.2 candidate set (kb/Work PB489 — one resolver); SR7's own verdict below speaks for a
        // reference that survives nothing, so this site reports the category sentence rather than a second
        // uniqueness one.
        // Counted (kb/Work PB978): two survivors are §8.4.2.2.3 SR1's ambiguity, reported once by the ONE verdict.
        DataItem? item = UniqueOrReportAmbiguous(
            QualifiedCandidates(dynName, file.AssignUsingQualifiers, Model.Scope.Program), "ASSIGN … USING",
            WrittenQualified(dynName, file.AssignUsingQualifiers), out bool ambiguous);
        if (ambiguous && item is null) return;
        if (item is null || !ItemCategory.IsAlphanumeric(item))
        {
            Edition.Error(DiagnosticCatalog.AssignUsingNotAlphanumeric,
                $"file '{file.SelectName}': ASSIGN … USING '{dynName}' shall reference an alphanumeric data item "
                + (item is null ? "and no such data item is described" : $"— '{dynName}' is {ItemCategory.Face(item)}")
                + " (ISO §12.4.5.2 SR7)");
            return;
        }
        // "shall not be subordinate to the file description entry for file-name-1" — subordinate means anywhere
        // under one of THIS file's record descriptions (a record description entry is subordinate to the FD).
        for (DataItem? a = item; a is not null; a = a.Parent)
            if (file.Records.Any(r => ReferenceEquals(r, a)))
            {
                Edition.Error(DiagnosticCatalog.AssignUsingInOwnRecord,
                    $"file '{file.SelectName}': ASSIGN … USING '{dynName}' shall not be subordinate to the file "
                    + $"description entry for '{file.SelectName}' (ISO §12.4.5.2 SR7)");
                return;
            }
        file.AssignUsingItem = item;
    }

    // ⛔ The category predicate and its English face MOVED to Model/ItemCategory — the ONE reader of an item's
    // §8.5.2 category for a category-worded syntax rule. §12.4.5.12.3 SR2 and §12.4.5.6.3 SR2 (the record keys,
    // kb/Work PB743) are worded the same way one category wider ("alphanumeric or national"), and a second copy
    // of the walk over Pic / AsIfPic / GroupUsage is exactly how the two arms of one rule drift apart.

    /// <summary>The ORGANIZATION clause (ISO §12.4.5.10.3 GR2–GR5) → the bound organization. The clause is
    /// optional and so is the absent-clause case: §12.4.5.10.3 GR6 — "When the ORGANIZATION clause is not
    /// specified, sequential organization with the RECORD SEQUENTIAL phrase is implied" — is the null arm.
    /// <para>THE ONE RECOGNITION SITE for line sequential, which is a COBOL-2023 introduction (kb/Work PB688);
    /// its edition gate is <c>VersionConformancePass.ParseArm.VisitOrganizationClause</c>, never here — this
    /// binder is edition-agnostic.</para></summary>
    private static FileOrganization MapOrganization(Core.OrganizationClauseContext org)
    {
        var t = org.organizationType();
        if (t is null) return FileOrganization.Sequential;
        if (t.LINE() is not null) return FileOrganization.LineSequential;
        if (t.RELATIVE() is not null) return FileOrganization.Relative;
        if (t.INDEXED() is not null) return FileOrganization.Indexed;
        // `RECORD? SEQUENTIAL` — with or without the optional word RECORD (§12.4.5.10.3 GR3), record sequential.
        return FileOrganization.Sequential;
    }

    private static FileAccessMode MapAccessMode(Core.AccessModeClauseContext acc)
    {
        var m = acc.accessMode();
        if (m?.RANDOM() is not null) return FileAccessMode.Random;
        if (m?.DYNAMIC() is not null) return FileAccessMode.Dynamic;
        return FileAccessMode.Sequential;
    }

    /// <summary>VALUE-clause literal/category conformance for national and boolean receivers (ISO §13.18.63,
    /// the COBOLNET0898 band). SR5: a category-national item takes a national literal or a figurative constant
    /// (SPACE / QUOTE / HIGH-VALUE / LOW-VALUE / ZERO, §8.3.3.6.4 GR1/GR6/GR7). §13.18.63.3 SR10: a category-boolean item
    /// takes a boolean literal or figurative ZERO (no boolean SPACE/QUOTE/HIGH/LOW exists — the §14.9.25.3 SR7
    /// posture). Both directions: an <c>N"…"</c>/<c>B"…"</c> literal seeds no OTHER category. Size: the decoded
    /// content shall not exceed the item's positions (SR5/SR10; alphanumeric receivers keep their historical
    /// truncating store — only the new categories get the strict check).</summary>
    /// <summary>True when <paramref name="raw"/> is a numeric literal (optional sign, digits, one decimal point —
    /// the floating-point form's significand likewise, ISO §8.3.3.3.3: the exponent cannot make a zero significand
    /// nonzero) whose value is NOT zero — the VCR 86 gate subject (ISO §13.18.63 SR6 exempts the literal-zero forms
    /// at all editions, so <c>0</c>/<c>0.00</c> return false; a quoted/national/figurative VALUE is not numeric).</summary>
    private static bool IsNonZeroNumericLiteral(string raw)
    {
        string t = raw.Trim();
        if (t.IndexOfAny(['E', 'e']) is > 0 and var ePos && NumericLiteral.IsFloatingPointForm(t)) t = t[..ePos];
        if (t.StartsWith('+') || t.StartsWith('-')) t = t[1..];
        bool sawDigit = false, anyNonZero = false, sawDot = false;
        foreach (char c in t)
        {
            if (c == '.') { if (sawDot) return false; sawDot = true; continue; }
            if (!char.IsAsciiDigit(c)) return false;
            sawDigit = true;
            if (c != '0') anyNonZero = true;
        }
        return sawDigit && anyNonZero;
    }

    /// <summary>The numeric-literal FORM test (§8.3.3.3.2 fixed-point: an optional sign, digits, at most one decimal
    /// point, at least one digit) — the shape a permissive digits-only alphanumeric VALUE must have to be stored as a
    /// number (kb/Work PB94).</summary>
    private static bool IsNumericLiteralForm(string text)
    {
        string t = text.Trim();
        if (t.StartsWith('+') || t.StartsWith('-')) t = t[1..];
        bool digit = false, dot = false;
        foreach (char c in t)
        {
            if (c == '.') { if (dot) return false; dot = true; continue; }
            if (!char.IsAsciiDigit(c)) return false;
            digit = true;
        }
        return digit;
    }

    /// <summary>⛔ THE ONE VALUE-CLAUSE LITERAL SCREEN — every literal of every VALUE format passes through here,
    /// and the raw text it returns is what gets STORED (unchanged, or the --permissive rewrite of a class-mismatched
    /// literal on a numeric subject).
    /// <para>ISO §13.18.63.3 SR2 ("If the category of the subject of the entry is numeric, all literals in the VALUE
    /// clause shall be numeric and shall be permissible values within the range indicated by the PICTURE clause or
    /// the USAGE clause") is an <b>ALL FORMATS</b> rule, and SR16 carries SRs 10–15 into FORMAT 2 as well
    /// ("Syntax rules 10, 11,12,13,14,and 15 above apply") — so the format-2 (table) literals are governed exactly
    /// as the format-1 one is. They were not: the four checks below sat INLINE at the format-1 call site, and
    /// <see cref="BuildTableValueSpecs"/>' per-occurrence literals reached the emitter unscreened, so
    /// <c>05 B PIC 9(4) COMP OCCURS 2 VALUE "0012" FROM (1) TO (2)</c> compiled clean at strict 2023 where its
    /// format-1 twin is COBOLNET1657. The <c>feedback_two_arm_dispatch</c> shape on the VALUE clause's own formats;
    /// extracting the screen is the fix, so a rule added here reaches every format BY CONSTRUCTION (kb/Work PB208).</para>
    /// <para>⛔ <b>And format 3 (kb/Work PB586).</b> The level-88 literals — every literal-2, both ends of a
    /// literal-2 THROUGH literal-3 range, and the WHEN SET TO FALSE literal-4 — called
    /// <see cref="ValidateValueCategory"/> directly, SR2's CLASS half only, so `01 W PIC 9(2). 88 C-BIG VALUE
    /// 12345.` compiled clean at every edition (a condition-name that can never be true) while its format-1 twin
    /// `01 W2 PIC 9(2) VALUE 12345.` was COBOLNET1625. SR2 and SR3 open "ALL FORMATS" and speak of "all literals
    /// in the VALUE clause", so the third arm of the dispatch PB208 unified now takes the same funnel, with the
    /// CONDITIONAL VARIABLE's picture as the subject whose range the literal must fit (§13.18.63.4 GR19 gives the
    /// condition-name its conditional variable's characteristics). The funnel's one SIZE rule below stays dark
    /// for it, because <see cref="ValueSubject.ForConditionName"/> carries no size (kb/Work PB598). The report
    /// section's format 4 printable-item literals were the fourth arm, reached by NO part of this funnel but SR6's
    /// edition gate: `03 COLUMN 1 PIC 9(2) VALUE 12345.` printed `45`. They take it too (DataBinder.Reports).</para></summary>
    private string ScreenValueLiteral(PicInfo pic, string raw, string where, ValueSubject subject)
    {
        // VALUE-clause literal/category conformance for the string-stored 2002 categories (ISO §13.18.63
        // SR5 national / SR10 boolean — the 0898 band, both directions) and SR2's CLASS half.
        raw = ValidateValueCategory(pic, raw, where, subject);
        // VALUE-clause range/sign conformance for a fixed-point NUMERIC subject (ISO §13.18.63.3 SR2/SR3): a numeric
        // VALUE literal must be a permissible value in the PICTURE range, representable WITHOUT truncation of a
        // leading/trailing nonzero digit (SR2), and a negative literal requires a signed subject (SR3). The
        // initializer path (ValueInitializer → EmitText.UnscaledAtScale) does NO high-order modulo and NO
        // unsigned-magnitude, so absent this bind-time gate an out-of-range / wrong-sign VALUE silently seeds an
        // out-of-range native field (COBOLNET1625). A NUMERIC-EDITED subject's numeric literal rides the same check
        // (SR6 — converted per the MOVE rules "such that no truncation of digits or sign is required"; kb/Work PB97).
        if (pic is { Category: PicCategory.Numeric, IsFloat: false } or { Category: PicCategory.NumericEdited })
            ValidateNumericValue(pic, raw, where);
        // (⛔ ALL of §13.18.63.3 SR6 — the literal-FORM rule AND the edition on which a numeric literal became
        // writable at all — is <see cref="ScreenNumericEditedNumericLiteral"/>'s, reached from
        // ValidateValueCategory above, which is the funnel EVERY format's literal passes through: the item VALUE
        // (formats 1 and 2), the level-88 set (format 3) and the group-level VALUE. It used to be split, and the
        // split WAS the defect — see that method's remarks; D21/PB66, kb/Work PB97 + PB921.)
        // VCR 34 (ISO §13.18.63 SR4/SR5; Annex E.2 item 27): at >=2023 an alphanumeric edited-image literal VALUE on
        // a numeric-edited item is checked against the PICTURE size — a literal LONGER than the edited width is
        // rejected (below 2023 it was stored truncated — the "unclear value"). Under --permissive the check
        // downgrades to a warning (a removed-capability posture). The national-class-mismatch leg is already
        // COBOLNET0898 (ValidateValueCategory); only a plain alphanumeric literal (leading '"') reaches this size check.
        // ⛔ THE SIZE IS THE SUBJECT'S, not pic.Length — the SAME discipline the SR4/SR5/SR10 size arms follow
        // (kb/Work PB598). This IS a size rule, so a subject that indicates no size has none to be measured
        // against; today the two quantities coincide for every subject that reaches here (a numeric-edited
        // picture is neither DYNAMIC LENGTH — §13.18.19.3 SR1 admits only one 'N' or 'X' — nor a group), and a
        // CONDITION-NAME subject, which this funnel also serves (kb/Work PB586), carries no size at all, so the
        // check stays dark for it exactly as §13.18.63.3 SR4/SR5's size sentences do (kb/Work PB598).
        if (pic is { Category: PicCategory.NumericEdited } && Edition.DialectLevel >= 2023
            && raw.StartsWith('"') && subject.SizePositions is { } editedWidth
            && CobolLiteral.Decode(raw).Length > editedWidth)
            Edition.Sink.Report(new EditionDiagnostic("COBOLNET1570",
                EditionSeverityPolicy.For(ConstructAvailability.Removed, Edition.Edition), "value-numeric-edited-oversize",
                $"{where}: the VALUE literal ({CobolLiteral.Decode(raw).Length} characters) exceeds the "
                + $"numeric-edited item's {editedWidth}-character edited size (ISO §13.18.63.3 SR4/SR5; COBOL-2023, "
                + "Annex E.2 item 27)", where, "ISO §13.18.63.3 SR4/SR5; Annex E.2 item 27"));
        return raw;
    }

    /// <summary>⛔ THE ONE §13.18.63.3 SR6 EDITION SCREEN — from which edition a NUMERIC literal may seed a
    /// NUMERIC-EDITED subject, asked once for EVERY general format of the VALUE clause that can carry one.
    ///
    /// <para><b>The rule.</b> §13.18.63.3 SR6 — "If the item is of category numeric-edited, then, subject to
    /// Syntax rules 2 and 3, literals in formats 1, 2, and 4 of the VALUE clause may be numeric when they shall
    /// be converted to their numeric-edited forms according to the rules for the MOVE statement" — is the ONLY
    /// rule that admits a numeric literal on a numeric-edited subject, and Annex E.3.3 item 43 DATES it: "VALUE
    /// clause, numeric-edited items and numeric literals. It is now permitted to allow numeric-edited data items
    /// to be assigned values specified as numeric literals." A COBOL-2023 addition, so below 2023 a numeric-edited
    /// VALUE required an alphanumeric or national edited-image literal (§13.18.63.3 SR7). SR6 exempts "the
    /// figurative constant ZERO or ZEROES and the integer and decimal forms of the literal zero" at ALL editions
    /// (Annex E.2 item 28 / VCR 35), so only a NON-ZERO numeric literal is gated.</para>
    ///
    /// <para>⛔ <b>Why it is a method and not four call sites.</b> SR6 was written down TWICE: its literal-FORM
    /// half (fixed-point vs floating-point) sat in <see cref="ValidateValueCategory"/>, which the item VALUE, the
    /// level-88 set and the group-level VALUE all funnel through; its EDITION half sat inline in
    /// <see cref="ScreenValueLiteral"/>, which only the item VALUE reaches. ONE rule, two places, and the two
    /// places covered different formats — the [[two_arm_dispatch]] shape, measured: `01 X PIC ZZ9.99 VALUE 10.`
    /// (format 1) was refused at --std 85 while `01 X PIC ZZ9.99. 88 X-TEN VALUE 10.` (format 3) was ACCEPTED
    /// there and stored the 2023 edited image ` 10.00`, and a report-section printable item
    /// `10 COLUMN 1 PIC ZZ9.99 VALUE 10.` (format 4 — a format SR6 NAMES) was accepted there too and printed it.
    /// Asking the question in one place, from the funnel every format's literal already passes through, is what
    /// makes the next format automatic (kb/Work PB921).</para>
    ///
    /// <para><b>DETERMINATION — format 3 (condition-name) rides the same gate.</b> SR6's own text enumerates
    /// "formats 1, 2, and 4", and format 3 is not in the list; read strictly that would forbid a numeric literal
    /// on a condition-name over a numeric-edited conditional variable at EVERY edition. The standard does not
    /// sustain that reading on its own terms: §13.18.63.4 GR19 gives a condition-name "the characteristics of ...
    /// its conditional variable", §14.9.39.4 GR6 places the literal "according to the rules for the VALUE clause"
    /// — i.e. SR6's conversion — and §13.18.63.3 SR36 carries SR11, the numeric-edited EDITING rule, into format 5,
    /// whose subject is a level-88 condition-name exactly as format 3's is. So the enumeration is read as naming
    /// the DATA-ITEM formats rather than as excluding the condition-name one, and the edition axis is applied to
    /// format 3 as well. That choice is edition-neutral below 2023 — both readings reject there, which is the
    /// half this fix closes — and at 2023 it takes the reading that does not reject source. Recorded as a
    /// determination on kb/Work PB921 so it can be revisited with an owner decision rather than rediscovered.</para></summary>
    /// <param name="pic">The SUBJECT's picture — the screen asks the category itself rather than trusting a
    /// caller to have asked, because "is this subject numeric-edited" is half the rule.</param>
    /// <param name="raw">The folded literal text, as the one literal-position reader returned it. A literal that
    /// is not numeric at all — an alphanumeric or national edited image, or a figurative — answers no here, so a
    /// caller never has to pre-classify it.</param>
    /// <param name="where">The diagnostic subject phrase (an item, a condition-name, or a report entry).</param>
    private void ScreenNumericEditedNumericLiteral(PicInfo pic, string raw, string where)
    {
        if (pic.Category is PicCategory.NumericEdited && IsNonZeroNumericLiteral(raw))
            ConstructRegistry.Check(Edition.Edition, Edition.Sink,
                Constructs.ValueNumericLiteralNumericEdited2023, where);
    }

    /// <summary>⛔ THE §13.18.63.3 VALUE-OPERAND SCREEN — the ONE place that decides whether a VALUE clause's
    /// literal may seed its subject, for EVERY subject the rules name: an elementary item, a condition-name's
    /// conditional variable, and (kb/Work PB206) a GROUP carrying a group-level VALUE. Returns the raw VALUE text
    /// to STORE — the input unchanged, or (kb/Work PB94, --permissive only) the representable numeric rewrite of a
    /// class-mismatched literal on a numeric subject.
    ///
    /// <para><b>Why a group subject enters HERE rather than a second screen.</b> SR4, SR5 and SR10 each say "the
    /// item" / "the subject of the entry", never "the elementary item": §8.5.2.1 gives a group a class and a
    /// category of its own ("an alphanumeric group item has class and category alphanumeric"; "a bit group item
    /// has class and category boolean"; "a national group item has class and category national"), and
    /// §13.18.29.4 GR1b/GR2b give a bit / national group an as-if PICTURE. So the group is a subject with a
    /// category and a size exactly as an elementary item is, and SR13 sentence 1 — "If the VALUE clause is
    /// specified at the group level, literal-1 shall be of the same category as the group item or shall be a
    /// figurative constant that is permitted in a MOVE statement to a receiving item of that category" — is
    /// those same three rules restated for it. Writing it twice is how the two answers drift.</para>
    ///
    /// <para>⚠ <b>Reached through <see cref="ScreenValueLiteral"/></b> for the item VALUE of formats 1 and 2
    /// (kb/Work PB208), the level-88 literals of format 3 and the report-section literals of format 4 (kb/Work
    /// PB586 — the first called here directly and the second not at all, and that is how SR2's range half never
    /// reached either), and directly by exactly one other kind of site: the
    /// group-level VALUE (<c>DataBinder.GroupValue</c>). A NEW direct caller inherits only ONE of SR2's
    /// two halves — the CLASS half here, never the RANGE half <see cref="ValidateNumericValue"/> carries —
    /// so route it through the funnel unless its subject cannot be of category numeric at all, which is the
    /// group arm's standing reason (§8.5.2.1 gives a group item category alphanumeric, national or
    /// boolean, never numeric).</para>
    ///
    /// <para>⛔ <b>WHICH SUBJECT it is decides which SENTENCES bind</b>, and that decision is
    /// <see cref="ValueSubject"/>'s, never a call site's. SR4/SR5/SR10 are each a sentence PAIR: sentence 1 is a
    /// CLASS rule over "the item" / "the subject of the entry", and sentences 2–3 are a SIZE rule that names only
    /// two subjects — "an elementary item" (bounded by "an <b>explicit</b> PICTURE clause") and a group item
    /// (bounded by "the size of the group item"). A CONDITION-NAME is neither, so it carries no size and the size
    /// arms below stay dark for it (kb/Work PB598; the derivation is on
    /// <see cref="ValueSubject.ForConditionName"/>). Passing the CONDITIONAL VARIABLE's size here was a measured
    /// rejection of legal source: `01 XV PIC X. 88 XC VALUE "cd".`</para></summary>
    /// <param name="subject">Which of §13.18.63.3's three subjects this VALUE clause has, and the size — if any —
    /// its SR4/SR5/SR10 size sentences measure the literal against. Built ONLY by
    /// <see cref="ValueSubject.ForElementary"/>, <see cref="ValueSubject.ForGroup"/> or
    /// <see cref="ValueSubject.ForConditionName"/>, each of which states its size rule with its citation.</param>
    private string ValidateValueCategory(PicInfo pic, string raw, string where, ValueSubject subject)
    {
        // ⛔ THE LITERAL'S CLASS COMES FROM THE ONE CLASSIFIER (CobolLiteral.ClassOf — kb/Work PB71): the former
        // raw-text tests (`raw[1] is '"'`) refused the Format-2 hexadecimal spellings NX"…" / BX"…" (§8.3.3.5.2 /
        // §8.3.3.4.2 — the SAME class as N"…" / B"…") and every `ALL literal` figurative, whose class is
        // literal-1's (§8.3.3.6.3 SR2 / §14.9.25.4 GR7 Table 17). `ALL "AB"` (an alphanumeric literal-1) stays
        // illegal for a national or boolean item; `ALL SPACES` / `ALL ZEROS` is the figurative WORD (legal).
        LiteralClass? lit = CobolLiteral.ClassOf(raw);
        // THE one §8.3.3.6.2 operand classifier (kb/Work PB461) decides which FORMAT this operand is, so the
        // validator, the VALUE initializer, the SET store and the condition test all part the same text the same
        // way. NULL/NULLS stays out (includeNull: false) — it is not a character figurative and this rule's
        // national/alphanumeric allowance never covered it.
        var figOp = CobolNet.CodeGen.FigurativeConstants.Classify(raw, includeNull: false);
        string? allRaw = figOp.AllLiteral;
        LiteralClass? allLit = allRaw is null ? null : CobolLiteral.ClassOf(allRaw);
        bool isNatLit = lit is LiteralClass.National || allLit is LiteralClass.National;
        bool isBoolLit = lit is LiteralClass.Boolean || allLit is LiteralClass.Boolean;
        bool isPlainString = lit is LiteralClass.Alphanumeric;
        bool isNumeric = raw.Length >= 1 && (char.IsAsciiDigit(raw[0]) || raw[0] is '+' or '-' or '.');
        bool isFloatLit = isNumeric && CobolNet.Common.NumericLiteral.IsFloatingPointForm(raw);
        bool isAllQuoted = allLit is LiteralClass.Alphanumeric;
        // Formats 1-5 — the KEYWORD figuratives, each of which admits the optional word ALL, both in the spelling
        // the parse tree glues ("ALLSPACES") and in the one it preserves. The word table is FigurativeConstants'
        // (it was written out a second time here, and its ALL strip a fifth time, kb/Work PB461).
        bool isZeroWord = figOp.Kind is 'Z';
        bool isNationalFigurative = figOp.Kind is not null;
        // ⛔ SR6's EDITION half, asked HERE rather than at one caller, so it reaches every VALUE format this
        // funnel serves — the item VALUE (formats 1 and 2), the level-88 set (format 3) and the group-level
        // VALUE (kb/Work PB921). See ScreenNumericEditedNumericLiteral for the rule and the determination.
        ScreenNumericEditedNumericLiteral(pic, raw, where);
        switch (pic.Category)
        {
            // ── The numeric literal's FORM vs the subject's (kb/Work PB97; the floating-point form is ISO §8.3.3.3.3):
            //    a FIXED-POINT numeric subject takes a floating-point literal at its EXACT value (§8.3.3.3.3 GR5 —
            //    significand × 10^exponent; SR2's "numeric ... representable exactly", checked on the expanded text
            //    downstream by ValidateNumericValue) — the raw text is REWRITTEN to that fixed-point value here, once,
            //    so no later reader (the carrier initializer, a level-88 membership test) meets an E-form it cannot
            //    scale (before this the emitter wrote `15EL` and Roslyn failed with CS0595). A FLOAT-usage subject
            //    keeps the floating form (its initializer is a binary64 literal).
            case PicCategory.Numeric when isFloatLit && !pic.IsFloat:
                return CobolNet.Common.NumericLiteral.ExpandFloatingPoint(raw);
            //    A numeric literal (either form) seeding a FLOAT-SHORT / FLOAT-LONG / FLOAT-BINARY-32/64 subject shall be
            //    "a permissible value within the range indicated by ... the USAGE clause" (§13.18.63.3 SR2) — the
            //    receiver's binary form (kb/Work PB99: a value beyond it became an overflowing C# double literal, CS0594).
            case PicCategory.Numeric when isNumeric && pic.IsFloat && !CobolNet.Common.NumericLiteral.FitsBinaryFloat(raw, pic.IsSingle):
                Edition.Error(DiagnosticCatalog.FloatingLiteral, $"{where}: the VALUE literal {raw} lies outside the range of the "
                    + $"item's {(pic.IsSingle ? "binary32 (FLOAT-SHORT / FLOAT-BINARY-32)" : "binary64 (FLOAT-LONG / FLOAT-BINARY-64)")} "
                    + "usage — a numeric VALUE shall be a permissible value within the range the USAGE indicates (ISO §13.18.63.3 SR2; "
                    + "the exponent range is implementor-defined, §8.3.3.3.3 r3)");
                return raw;
            //    §13.18.63.3 SR6 — a numeric-edited subject's numeric literal shall be of ITS form: "literals for
            //    fixed-point formats shall be specified as fixed-point, while literals for floating-point formats
            //    shall be specified as floating-point, though the figurative constant ZERO or ZEROES and the integer
            //    and decimal forms of the literal zero may also be specified for either format" (D21/PB66). The rule
            //    reaches formats 1, 2 and 4 of the VALUE clause — this funnel serves the item VALUE and the level-88 set.
            case PicCategory.NumericEdited when isFloatLit && !pic.IsFloatEdited:
                Edition.Error(DiagnosticCatalog.ValueEditedLiteralForm, $"{where}: the VALUE literal {raw} is a floating-point "
                    + "numeric literal but the item is a fixed-point numeric-edited item — literals for fixed-point formats "
                    + "shall be specified as fixed-point (ISO §13.18.63.3 SR6)");
                return CobolNet.Common.NumericLiteral.ExpandFloatingPoint(raw);
            case PicCategory.NumericEdited when pic.IsFloatEdited && isNumeric && !isFloatLit && IsNonZeroNumericLiteral(raw):
                Edition.Error(DiagnosticCatalog.ValueEditedLiteralForm, $"{where}: the VALUE literal {raw} is a fixed-point "
                    + "numeric literal but the item is a floating-point numeric-edited item — its VALUE shall be a "
                    + "floating-point literal, ZERO, or the literal zero (ISO §13.18.63.3 SR6)");
                return raw;
            // National: an N"…" literal or a figurative constant (§8.3.3.6 GR1/GR6/GR7 — SPACE/QUOTE/HIGH/
            // LOW/ZERO, incl. their ALL-prefixed forms). Plain strings, B"…", numeric, and ALL "literal" are
            // illegal.
            case PicCategory.National when isPlainString || isBoolLit || isNumeric || isAllQuoted
                    || !(isNatLit || isNationalFigurative):
                Edition.Error("COBOLNET0898", $"{where}: the VALUE of a national data item shall be a national "
                    + "literal (N\"…\") or a figurative constant (ISO §13.18.63.3 SR5)");
                break;
            // SR5 sentences 2 and 3 — ONE sentence pair, ONE arm: "National literals in the VALUE clause of an
            // elementary item shall not exceed the size indicated by an explicit PICTURE clause. National
            // literals in the VALUE clause of a national group item shall not exceed the size of the group item."
            // The limit is the SUBJECT's (ValueSubject.SizePositions), never pic.Length: a DYNAMIC LENGTH item's
            // picture indicates no size, and a CONDITION-NAME subject indicates none at all (the derivations are
            // on ValueSubject.ForElementary and .ForConditionName — both were live rejections of legal source).
            case PicCategory.National when lit is LiteralClass.National
                    && subject.SizePositions is { } natLimit && CobolLiteral.Decode(raw).Length > natLimit:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE national literal exceeds the item's "
                    + $"{natLimit} national positions (ISO §13.18.63.3 SR5)");
                break;
            // Boolean: a B"…" literal or figurative ZERO (incl. ALL ZEROS) — no boolean SPACE/QUOTE/HIGH/LOW
            // exists (§14.9.25.3 SR7 posture).
            case PicCategory.Boolean when !isBoolLit && !isZeroWord:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE of a boolean data item shall be a boolean "
                    + "literal (B\"…\") or the figurative constant ZERO (ISO §13.18.63.3 SR10)");
                break;
            // SR10 sentences 2 and 3 — the same sentence pair as SR5's, over boolean positions: an elementary
            // item's explicit PICTURE, or "the size of the group item" for a bit group item (whose positions are
            // §8.5.1.6.3's bit extent, DataItem.AsIfPic).
            case PicCategory.Boolean when lit is LiteralClass.Boolean
                    && subject.SizePositions is { } boolLimit && CobolLiteral.Decode(raw).Length > boolLimit:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE boolean literal exceeds the item's "
                    + $"{boolLimit} boolean positions (ISO §13.18.63.3 SR10)");
                break;
            // ⛔ A NUMERIC-EDITED SUBJECT'S CHARACTER LITERAL IS §13.18.63.3 SR7's, AND SR7 IS NOT ASKED HERE
            //    (kb/Work PB646). Every other arm of this screen reads the subject's CATEGORY, which entry bind
            //    has settled; SR7 reads its CLASS, and §8.5.2.1 Table 2 makes a numeric-edited item's class its
            //    USAGE ("Numeric-edited (if usage is display)" under class alphanumeric, "Numeric-edited (if
            //    usage is national)" under class national) — a fact §13.18.60.4 GR1 group inheritance can still
            //    change after this point. The verdict therefore belongs to
            //    <see cref="CheckValueLiteralClasses"/>, which runs once the usage is resolved. Excluding the
            //    category from the cross-category arm below is what keeps a CONFORMING `PIC ZZ9 USAGE NATIONAL
            //    VALUE N"  1"` from being refused here as "a national literal may seed only its own category".
            case not (PicCategory.National or PicCategory.Boolean)
                    when isBoolLit || isNatLit && pic.Category is not PicCategory.NumericEdited:
                Edition.Error("COBOLNET0898", $"{where}: a {(isNatLit ? "national (N\"…\")" : "boolean (B\"…\")")} "
                    + "literal may seed only a data item of its own category (ISO §13.18.63.3 SR5/SR10)");
                break;
            // ── kb/Work PB94 — §13.18.63.3 SR2: a NUMERIC subject takes numeric literals (and figurative ZERO) only.
            //    A digits-only alphanumeric literal (or a digits-only ALL "literal" repeated to the digit width) is
            //    the representable vendor leniency: error strict, warning + the number under --permissive; a
            //    character figurative (SPACE / QUOTE / HIGH-VALUE / LOW-VALUE) is stored as ZERO under --permissive
            //    (a native numeric holds no character fill — said in the warning); anything else is an error on
            //    both axes (the former path rendered it as a numeric initializer and the C# backend crashed).
            case PicCategory.Numeric when isPlainString || isAllQuoted:
            {
                string content = CobolLiteral.Decode(isAllQuoted ? allRaw! : raw);
                if (isAllQuoted && content.Length > 0 && content.All(char.IsAsciiDigit))
                    // ALL "digits" repeats to the receiver's DIGIT positions — §8.3.3.6.4 GR2, through THE one
                    // fold (EmitText.RepeatToWidth → the runtime's CobolString.FigToWidth), which is where
                    // MoveEmitter.AllDigitFill takes the same rule from. It was open-coded here (kb/Work PB461):
                    // a second copy of GR2 that could drift from the runtime the emitted code measures against.
                    content = CobolNet.CodeGen.Emit.EmitText.RepeatToWidth(content, Math.Max(pic.Digits, 1));
                if (IsNumericLiteralForm(content))
                {
                    Edition.Removed(DiagnosticCatalog.ValueLiteralClass.Code, $"{where}: the VALUE literal "
                        + $"{raw} is alphanumeric but the item is numeric — all literals in the VALUE clause of a "
                        + "numeric item shall be numeric (ISO §13.18.63.3 SR2); under --permissive it is stored as "
                        + $"the number {content.Trim()}");
                    return Edition.Permissive ? content.Trim() : raw;
                }
                Edition.Error(DiagnosticCatalog.ValueLiteralClass, $"{where}: the VALUE literal {raw} is not a "
                    + "numeric literal and the item is numeric (ISO §13.18.63.3 SR2)");
                return raw;
            }
            case PicCategory.Numeric when isNationalFigurative && !isZeroWord:
                Edition.Removed(DiagnosticCatalog.ValueLiteralClass.Code, $"{where}: the figurative constant "
                    + $"{raw} is not a numeric literal — the VALUE of a numeric item shall be numeric or ZERO (ISO "
                    + "§13.18.63.3 SR2); under --permissive the item is initialized to ZERO (a native numeric item "
                    + "holds no character fill)");
                return Edition.Permissive ? "0" : raw;
            // ── SR4 sentence 1: an alphabetic / alphanumeric / alphanumeric-edited subject takes alphanumeric
            //    literals only. On an ELEMENTARY subject a numeric literal is the vendor leniency (the digits,
            //    left-justified, as MOVE would store them): error strict, warning + that store under --permissive.
            //    On a GROUP subject there is no such store — §13.18.63.4 GR5 deposits the operand's CHARACTERS
            //    over the group area and a numeric literal has none, so SR13 sentence 1's "shall be of the same
            //    category as the group item" is an error on both axes (kb/Work PB206; measured on 1d949007:
            //    `01 GN VALUE 1234. 05 N1 PIC X(2). 05 N2 PIC X(2).` compiled clean and seeded SPACES).
            case PicCategory.Alphanumeric when isNumeric && IsNumericLiteralForm(raw) && subject.IsGroup:
                Edition.Error(DiagnosticCatalog.ValueLiteralClass, $"{where}: the VALUE literal {raw} is numeric "
                    + "but the subject is an alphanumeric group item — a group-level VALUE literal shall be of the "
                    + "same category as the group item, or a figurative constant permitted in a MOVE statement to "
                    + "a receiving item of that category (ISO §13.18.63.3 SR13; §8.5.2.1 — an alphanumeric group "
                    + "item has class and category alphanumeric)");
                break;
            case PicCategory.Alphanumeric when isNumeric && IsNumericLiteralForm(raw):
                Edition.Removed(DiagnosticCatalog.ValueLiteralClass.Code, $"{where}: the VALUE literal {raw} is "
                    + "numeric but the item is alphabetic / alphanumeric / alphanumeric-edited — its VALUE literals "
                    + "shall be alphanumeric (ISO §13.18.63.3 SR4); under --permissive the literal's characters are "
                    + "stored as MOVE would store them");
                break;
            // ── SR4 sentences 2 and 3 — THE THIRD ARM of the size rule, which had no implementation at all while
            //    its national (SR5) and boolean (SR10) twins had theirs above: "Alphanumeric literals in the VALUE
            //    clause of an elementary item shall not exceed the size indicated by an explicit PICTURE clause.
            //    Alphanumeric literals in the VALUE clause of an alphanumeric group item shall not exceed the size
            //    of the group item." Measured on 1d949007, both silently TRUNCATED: `01 E1 PIC X(2) VALUE "ABCD".`
            //    displayed `AB`, and `01 GZ VALUE "ABCDEF". 05 O1 PIC X(2). 05 O2 PIC X(2).` displayed `ABCD`
            //    (kb/Work PB206). Scoped to a plain alphanumeric literal: a FIGURATIVE constant is repeated and
            //    truncated to the subject's size BY RULE (§8.3.3.6.4 GR2, which names the VALUE clause), so
            //    `01 GH VALUE ALL "XYZ".` over four positions is conforming source seeding `XYZX`.
            case PicCategory.Alphanumeric when lit is LiteralClass.Alphanumeric
                    && subject.SizePositions is { } alnumLimit && CobolLiteral.Decode(raw).Length > alnumLimit:
                Edition.Error(DiagnosticCatalog.ValueLiteralOversize, $"{where}: the VALUE alphanumeric literal "
                    + $"({CobolLiteral.Decode(raw).Length} characters) exceeds the "
                    + $"{(subject.IsGroup ? $"group item's {alnumLimit} character positions"
                                          : $"item's {alnumLimit} character positions indicated by its PICTURE clause")}"
                    + " (ISO §13.18.63.3 SR4)");
                break;
        }
        return raw;
    }

    /// <summary>⛔ THE §13.18.63.3 SR7 SCREEN — the ONE VALUE-literal rule that reads the subject's CLASS
    /// instead of its category, asked ONCE per literal, after §13.18.60.4 GR1 usage inheritance has settled
    /// (kb/Work PB646). <i>"If the item is of category numeric-edited and the literal is of class alphanumeric or
    /// national, the class of the literal shall conform to that of the data item, if the class is otherwise
    /// undefined, then the class of the data item shall be that of the literal."</i>
    /// <para>§8.5.2.1 Table 2 is what makes it a USAGE test and what makes it bite in BOTH directions: category
    /// numeric-edited is class ALPHANUMERIC when its usage is display and class NATIONAL when its usage is
    /// national. The national-usage half was unreachable while §13.18.60.3 SR12's national-form numeric-edited
    /// item was staged loud (COBOLNET0899).</para>
    /// <para>A NUMERIC literal is SR2/SR6's business, not this rule's, and a figurative constant takes the
    /// receiver's class (§8.3.3.6), so neither reaches the test. ⚠ §13.18.63.3 SR7's third clause — "if the class is
    /// otherwise undefined, then the class of the data item shall be that of the literal" — has NO SITE in this
    /// model and cannot acquire one: §13.18.60.3 SR13 implies a usage for every item given none (NATIONAL for an
    /// 'N'-bearing picture character-string, else DISPLAY), so a numeric-edited item's usage — and with it its
    /// Table 2 class — is always defined.</para></summary>
    private void ScreenNumericEditedLiteralClass(PicInfo pic, string raw, string where)
    {
        if (pic.Category is not PicCategory.NumericEdited) return;
        LiteralClass? lit = CobolLiteral.ClassOf(raw);
        LiteralClass? allLit = CobolLiteral.AllLiteralRaw(raw) is { } allRaw ? CobolLiteral.ClassOf(allRaw) : null;
        bool isNatLit = lit is LiteralClass.National || allLit is LiteralClass.National;
        bool isAlnumLit = lit is LiteralClass.Alphanumeric || allLit is LiteralClass.Alphanumeric;
        if (!isNatLit && !isAlnumLit) return;   // a numeric literal, a boolean literal or a figurative — not SR7's
        if (isNatLit == (pic.Usage is Usage.National)) return;
        Edition.Error("COBOLNET0898", $"{where}: the VALUE literal {raw} is of class "
            + $"{(isNatLit ? "national" : "alphanumeric")}, but the numeric-edited item is of usage "
            + $"{(pic.Usage is Usage.National ? "NATIONAL and therefore of class national" : "DISPLAY and therefore of class alphanumeric")}"
            + " — the class of the literal shall conform to that of the data item (ISO §13.18.63.3 SR7; "
            + "§8.5.2.1 Table 2)");
    }

    /// <summary>The §13.18.63.3 SR7 pass — every VALUE literal of every format, over the RESOLVED usage
    /// (kb/Work PB646). Format 1's item literal and format 2's per-occurrence literals hang off the item; formats
    /// 3 and 5's hang off the condition-names, whose "item" SR7 speaks of is the conditional variable (the same
    /// reading <c>ValidateValueCategory</c>'s condition-name call site already uses).
    /// <para>⛔ It is a PASS and not an entry-bind check because the rule reads §8.5.2.1 Table 2's CLASS, which
    /// for a numeric-edited item is its usage — and §13.18.60.4 GR1 lets a group's USAGE clause supply that
    /// usage after the entry has bound. Asking at entry bind gave the GR1 spelling the opposite verdict from its
    /// written-clause twin in BOTH directions, which is exactly what the inheritance pass's invariant forbids
    /// ("same syntax-rule verdicts, same diagnostics").</para></summary>
    internal void CheckValueLiteralClasses()
    {
        foreach (var root in Roots) Walk(root);
        foreach (var list in Conditions.Values)
            foreach (var cond in list)
            {
                if (cond.Parent.OperandPic is not { } cp) continue;
                using var _ = Edition.At(cond.Parent);
                foreach (var (lo, hi) in cond.Values)
                {
                    ScreenNumericEditedLiteralClass(cp, lo, $"condition-name '{cond.Name}'");
                    if (hi is not null) ScreenNumericEditedLiteralClass(cp, hi, $"condition-name '{cond.Name}'");
                }
            }

        void Walk(DataItem item)
        {
            if (item.Pic is { } pic && !pic.IsRecovery)
            {
                using var _ = Edition.At(item);
                string where = $"data item '{item.CobolName ?? "FILLER"}'";
                if (item.RawValue is { } raw) ScreenNumericEditedLiteralClass(pic, raw, where);
                if (item.TableValuePlan is { } plan)
                    foreach (var lit in plan.Literals.Values) ScreenNumericEditedLiteralClass(pic, lit, where);
            }
            foreach (var c in item.Children) Walk(c);
        }
    }

    /// <summary>ISO §13.18.63.3 SR2/SR3: a numeric VALUE literal on a fixed-point numeric subject shall be a
    /// permissible value within the range the PICTURE indicates, representable EXACTLY in the subject "without
    /// truncation of leading or trailing nonzero digits" (SR2), and a signed numeric literal requires a signed
    /// subject (SR3). Both are syntax rules ('shall'), so the only conforming response to a violation is a
    /// compile-time diagnostic (COBOLNET1625) — never the legacy lenient truncating/sign-dropping store, which the
    /// typed-native initializer would otherwise perform (ValueInitializer emits the literal verbatim through
    /// <c>EmitText.UnscaledAtScale</c>, which does NO high-order modulo and NO unsigned magnitude).
    /// <para>The NUMERIC-EDITED subject rides the same check (kb/Work PB97): SR6 converts its numeric literal "according
    /// to the rules for the MOVE statement, such that no truncation of digits or sign is required" and SR3 admits a
    /// signed literal only for "a numeric-edited data item with a representation of a sign". A fixed-point edited
    /// picture's stored span is its DIGIT POSITIONS (9 / Z / * / the floating-insertion digit positions) at the
    /// MASK's scale (the '.' or V, P-scaled — <c>CobolEdit.MaskScale</c>, the same scale every edited store aligns
    /// to); a FLOATING-POINT numeric-edited picture (D21/PB66) holds a literal iff its significand's nonzero digit
    /// run fits the mask's significand digits and its normalized exponent lies within the exponent field's range
    /// (<c>CobolEdit.FloatMask</c>, the ONE parser of the form).</para></summary>
    /// <remarks>
    /// The subject's stored ('9') digit positions are modeled by their power-of-ten exponents. Uniformly across an
    /// implied point (V), a leading-P, and a trailing-P scaled picture — given <see cref="PicInfo.Digits"/> stored
    /// digits and the signed <see cref="PicInfo.Scale"/> (trailing P ⇒ negative, leading P ⇒ &gt; Digits; ISO
    /// §13.18.40.4) — the lowest stored exponent is <c>-Scale</c> and the highest is <c>Digits - Scale - 1</c>. The
    /// literal is representable exactly iff every NONZERO digit of the literal lands within <c>[low, high]</c>; a
    /// nonzero digit above the high exponent is a leading-nonzero truncation, one below the low exponent a
    /// trailing-nonzero truncation. Zero (and a literal whose only nonzero digits are outside the tested span) is
    /// handled naturally: a literal of value zero is always representable and skipped. A picture-less numeric item
    /// (USAGE INDEX, Digits 0) has no digit positions to test and is skipped; SR3's negative-into-unsigned test is
    /// independent of the digit range. A COMP-5 / fixed-width binary item's Digits equals the decimal width of its
    /// range's maximum magnitude, so the digit test never false-rejects a capacity-valid value (its full
    /// binary-capacity range is a separate discipline, not SR2's PICTURE range).
    /// </remarks>
    private void ValidateNumericValue(PicInfo pic, string raw, string where)
    {
        bool edited = pic.Category is PicCategory.NumericEdited;
        string mask = pic.EditMask ?? "";
        if (edited ? pic.DigitPositions <= 0 && !pic.IsFloatEdited : pic.Digits <= 0) return;   // picture-less numeric (INDEX) — no digit positions
        string t = raw.Trim();
        if (t.Length == 0) return;
        bool neg = t[0] == '-';
        if (neg || t[0] == '+') t = t[1..];
        // A plain numeric literal is a digit run with at most one decimal point (the floating-point form of
        // §8.3.3.3.3 — significand E exponent — reaches only a floating-point numeric-edited subject here: a
        // fixed-point subject's ValidateValueCategory expanded it to its exact value first); figurative words, quoted
        // alphanumeric, and national/boolean literals are validated on their own paths and are out of scope here.
        int ePos = t.IndexOfAny(['E', 'e']);
        int exp10 = 0;
        if (ePos >= 0)
        {
            if (!NumericLiteral.IsFloatingPointForm(t)) return;
            exp10 = int.Parse(t[(ePos + 1)..], System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture);
            t = t[..ePos];
        }
        int dot = t.IndexOf('.');
        if (dot != t.LastIndexOf('.')) return;
        string digits = dot < 0 ? t : t.Remove(dot, 1);
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit)) return;

        // SR3 (ISO §13.18.63.3, ISO_COBOL.md:22914): a signed numeric literal requires a signed numeric subject or "a
        // numeric-edited data item with a representation of a sign" (an editing sign symbol +, -, CR or DB — the
        // floating-point form's significand sign included). Scoped to a NEGATIVE literal (the sign-losing case) — a
        // leading '+' into an unsigned item is the common harmless idiom (+5 == 5, no data loss) and is not rejected.
        // (a floating-point edited mask's exponent '+' is NOT a representation of the value's sign — only the
        // significand's own sign symbol is, CobolEdit.FloatMask.SigSign.)
        bool signBearing = pic.IsFloatEdited ? CobolEdit.FloatMask.Parse(mask, DecimalPointIsComma).SigSign != '\0'
            // A format-2 (LOCALE) item has no mask: its sign representation is the picture's '+' (§13.18.40.4
            // GR18 — Signed carries it; PB64 T6). Reading the empty mask here falsely rejected a legal negative
            // VALUE on a '+'-bearing format-2 picture.
            : pic.LocaleEdit is not null ? pic.Signed
            : edited
            ? mask.Any(c => c is '+' or '-') || mask.Contains("CR", StringComparison.OrdinalIgnoreCase) || mask.Contains("DB", StringComparison.OrdinalIgnoreCase)
            : pic.Signed;
        if (neg && !signBearing)
        {
            Edition.Error("COBOLNET1625", $"{where}: a signed (negative) numeric literal in the VALUE clause "
                + "requires a signed numeric or sign-bearing numeric-edited subject (ISO §13.18.63.3 SR3)");
            return;
        }

        int litScale = dot < 0 ? 0 : t.Length - dot - 1;    // the literal's fractional-digit count
        int firstNonZero = -1, lastNonZero = -1;
        for (int i = 0; i < digits.Length; i++)
            if (digits[i] != '0') { if (firstNonZero < 0) firstNonZero = i; lastNonZero = i; }
        if (firstNonZero < 0) return;                       // the literal is zero — always representable (SR2 / SR6)
        int last = digits.Length - 1;
        int highLitExp = exp10 - litScale + (last - firstNonZero); // exponent of the most-significant nonzero digit
        int lowLitExp = exp10 - litScale + (last - lastNonZero);   // exponent of the least-significant nonzero digit

        // A FLOATING-POINT numeric-edited subject (SR6, D21/PB66): the nonzero digit run is the significand the mask
        // must hold whole (no truncation of digits), and the value normalizes to a leading-nonzero significand
        // (§14.6.8.4 GR1) whose exponent shall fit the exponent field: value = 0.d1d2… × 10^(highLitExp + 1) →
        // E = highLitExp + 1 − (integer significand digits).
        if (pic.IsFloatEdited)
        {
            var fm = CobolEdit.FloatMask.Parse(mask, DecimalPointIsComma);
            int run = lastNonZero - firstNonZero + 1;
            int normExp = highLitExp + 1 - (fm.SigDigits - fm.SigScale);
            if (run > fm.SigDigits || normExp > fm.MaxExp || normExp < -fm.MaxExp)
                Edition.Error("COBOLNET1625", $"{where}: the numeric literal {raw.Trim()} in the VALUE clause is not "
                    + $"representable in the floating-point numeric-edited PICTURE {mask} without truncation — the "
                    + $"significand holds {fm.SigDigits} digit(s) (the literal has {run} significant digits) and the "
                    + $"exponent ranges over -{fm.MaxExp}..+{fm.MaxExp} (the normalized exponent is {normExp:+0;-0}) (ISO §13.18.63.3 SR6)");
            return;
        }

        // SR2 (ISO §13.18.63.3, ISO_COBOL.md:22906): the literal shall be a permissible value in the PICTURE range,
        // representable without truncation of a leading or trailing nonzero digit. Compare the literal's nonzero
        // digit-exponent span against the subject's stored-digit exponent span [-Scale, Digits-Scale-1] —
        // StoredShapeOf, the ONE fixed-point (digits, scale) geometry (kb/Work PB155 lifted it so the §14.7.7
        // composite aligns on the same shape).
        var (storedDigits, storedScale) = StoredShapeOf(pic);
        int lowStoredExp = -storedScale;
        int highStoredExp = storedDigits - storedScale - 1;
        if (highLitExp > highStoredExp || lowLitExp < lowStoredExp)
            Edition.Error("COBOLNET1625", edited
                ? $"{where}: the numeric literal {raw.Trim()} in the VALUE clause is not representable in the numeric-edited "
                    + $"PICTURE {mask} without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR6)"
                : $"{where}: the numeric literal {raw.Trim()} in the VALUE clause is not a "
                    + "permissible value in the range the PICTURE indicates — it is not representable without truncation "
                    + "of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)");
    }

    /// <summary>The stored digit-position span and P-signed scale of a FIXED-POINT item — plain numeric
    /// (Digits/Scale), numeric-edited (the MASK's geometry: DigitPositions less its P positions, at
    /// <c>CobolEdit.MaskScale</c> — the '.' or V, P-signed; SR6 "no truncation of digits or sign" spans the
    /// digit positions, P excluded because P scales and stores nothing), or locale-edited format 2
    /// (DigitPositions at the analyzer's Scale — there is no mask for MaskScale to read, and 'P' does not
    /// exist in format 2). The ONE (digits, scale) shape the §13.18.63.3 SR2/SR6 VALUE-fit check and the
    /// §14.7.7 r2 composite of operands both align on (kb/Work PB155). Callers exclude floating-point forms
    /// first (<see cref="PicInfo.IsFloat"/> / <see cref="PicInfo.IsFloatEdited"/> — no fixed decimal
    /// alignment exists for them).</summary>
    internal (int Digits, int Scale) StoredShapeOf(PicInfo pic)
    {
        bool edited = pic.Category is PicCategory.NumericEdited;
        string mask = pic.EditMask ?? "";
        return (edited ? pic.DigitPositions - mask.Count(c => c == 'P') : pic.Digits,
                pic.LocaleEdit is not null ? pic.Scale
                    : edited ? CobolEdit.MaskScale(mask, '$', DecimalPointIsComma,
                        pic.EditingRules as CobolEdit.EditRule[]) : pic.Scale);
    }

    // (The former private DecodeString twin is retired — all callers use CobolNet.Common.CobolLiteral.Decode,
    // the one ISO §8.3.3.1/§8.3.3.2 literal codec, PHASE-05 Step 1.)

    /// <summary>The most-recently-opened 01/77 record, so a following level-66 RENAMES attaches to its owner.</summary>
    private DataItem? _lastRoot;

    /// <summary>Index a named item in the <see cref="ByName"/> multimap (COBOL allows duplicate names disambiguated
    /// only by qualification).</summary>
    private void RegisterName(DataItem item)
    {
        RegisterIndexes(item);   // before the name test: a FILLER table's index-names are referenceable (PB919)
        if (item.CobolName is not { } name) return;
        if (!ByName.TryGetValue(name, out var list)) ByName[name] = list = [];
        list.Add(item);
    }

    private readonly HashSet<string> _repositoryNameReported = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>⛔ THE ONE DECLARATION FUNNEL OF A USER-DEFINED WORD (ISO §8.3.2.2; kb/Work PB1083): every
    /// declaration of every §8.3.2.2 type announces its word here, and the rules about a user-defined word AS
    /// DECLARED are asked here once. Today that is §8.3.2.1 rule 5 — intrinsic-function-names may be user-defined
    /// words "except for … intrinsic function names identified in a function-specifier in the REPOSITORY paragraph"
    /// — which §12.3.8.3 restates for the specifier's scope (SR12 for <c>FUNCTION name INTRINSIC</c>, SR13 for
    /// <c>FUNCTION ALL INTRINSIC</c>); the membership is <see cref="IsRepositoryIntrinsic"/>. Reported once per
    /// word (a TYPE expansion re-registers a clone). Returns true when the word is reserved.
    /// <para>kb/Work PB65 (FMT-15.43.2 / FMT-15.58.2) made the prohibition bite at all — the REPOSITORY sets were
    /// consulted by nothing at declaration time, so a table named HIGHEST-ALGEBRAIC compiled and the keyword-omitted
    /// reference `HIGHEST-ALGEBRAIC(A1)` silently read the table. kb/Work PB1083 made it reach every type: it was
    /// called from six funnels (data-, condition-, index-, file-, paragraph- and section-names), so an alphabet-name,
    /// a class-name, a symbolic-character, a program-name or a function-prototype-name spelling a REPOSITORY
    /// intrinsic compiled clean. <c>UserWordDeclarationDriftTests</c> keeps every §8.3.2.2 type declared here.</para>
    /// </summary>
    internal bool DeclareUserWord(string word, UserWordKind kind)
    {
        CheckOneTypePerWord(word, kind);
        CheckLetter(word, kind);
        if (RepositoryIntrinsicSpecifier(FunctionWord.OfWrittenWord(word, CobolWords)) is not { } specifier) return false;
        bool named = specifier != AllIntrinsics;   // SR12 (a named specifier) or SR13 (ALL)
        if (_repositoryNameReported.Add(word))
            Edition.Error(DiagnosticCatalog.RepositoryIntrinsicNameAsUserWord,
                $"{kind.Spelling()} '{word}': the intrinsic-function-name is identified in a function-specifier of the "
                + $"REPOSITORY paragraph (FUNCTION {specifier} INTRINSIC), so it shall not be used as a user-defined "
                + $"word within the scope of that paragraph (ISO §8.3.2.1 rule 5; §12.3.8.3 SR{(named ? "12" : "13")})");
        return true;
    }

    /// <summary>The TYPE each user-defined word of THIS source element was first declared as — the census behind
    /// §8.3.2.2's "<i>a given user-defined word may be used as only one type of user-defined word</i>" (kb/Work
    /// PB990). One instance of the binder is one source element, so the map is per element by construction; a name a
    /// containing element's SPECIAL-NAMES passes down is inherited, not declared here, and a contained source element
    /// may re-declare it as another type (§8.4.6.1: each element "may use identical user-defined words"). Keyed
    /// case-insensitively, like every COBOL word.</summary>
    private readonly Dictionary<string, UserWordKind> _wordTypes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The words already reported as two types, so a third declaration does not repeat the refusal.</summary>
    private readonly HashSet<string> _wordTypeConflictReported = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>⛔ §8.3.2.2: "<i>Within a source element, a given user-defined word may be used as only one type of
    /// user-defined word</i>", except as <see cref="UserWordKinds.MayBeOneWord"/> says. Asked at the ONE declaration funnel
    /// (<see cref="DeclareUserWord"/>), the second declaration being the one reported — the first type stands.
    /// Before kb/Work PB990 each declaring construct kept its own registry and nothing crossed them, so
    /// <c>ALPHABET ZQ IS NATIVE</c> beside <c>01 ZQ PIC X</c> compiled and ran. The same TYPE declared twice is no
    /// conflict here (a TYPE expansion re-registers a clone of its template's words); whether two declarations of
    /// one type may coexist is §8.4.2's uniqueness, decided where the type's own registry is.</summary>
    private void CheckOneTypePerWord(string word, UserWordKind kind)
    {
        if (_wordTypes.TryAdd(word, kind)) return;
        var first = _wordTypes[word];
        if (UserWordKinds.MayBeOneWord(first, kind) || !_wordTypeConflictReported.Add(word)) return;
        Edition.Error(DiagnosticCatalog.UserWordTypeConflict,
            $"'{word}' names two types of user-defined word in this source element ({first.Spelling()} and "
            + $"{kind.Spelling()}) — a given user-defined word may be used as only one type of user-defined word "
            + "(ISO §8.3.2.2); the "
            + "exceptions are a compilation-variable-name, a level-number that matches a paragraph-name or "
            + "section-name, and the constant-name / data-name / property-name / record-key-name / record-name group");
    }

    /// <summary>The letterless words already reported, so a repeated declaration of one (a TYPE expansion re-registers
    /// a clone of its template's words) draws one refusal.</summary>
    private readonly HashSet<string> _letterlessReported = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>⛔ §8.3.2.2: "<i>With the exception of section-names, paragraph-names, and level-numbers, each
    /// user-defined word shall contain at least one basic letter or extended letter</i>" (kb/Work PB1403). The lexer
    /// cannot ask it — a paragraph-name <c>1-2</c> is legal and a letterless word is the same IDENTIFIER token — so it
    /// is asked here, at the ONE declaration funnel, for every type not in <see cref="UserWordKinds.MayBeLetterless"/>:
    /// <c>01 1-2 PIC X</c> compiled and ran. A letter is any Unicode letter: the basic letters, and the extended
    /// letters the implementor admits (§8.3.2.2 leaves them to the implementor; Annex A.1).</summary>
    private void CheckLetter(string word, UserWordKind kind)
    {
        if (UserWordKinds.MayBeLetterless.Contains(kind) || word.Any(char.IsLetter) || !_letterlessReported.Add(word)) return;
        Edition.Error(DiagnosticCatalog.UserWordWithoutLetter,
            $"the {kind.Spelling()} '{word}' contains no letter — with the exception of section-names, paragraph-names "
            + "and level-numbers, each user-defined word shall contain at least one basic letter or extended letter "
            + "(ISO §8.3.2.2)");
    }

    /// <summary>Declare the unit's OWN name — the program-name of a PROGRAM-ID paragraph or the user-function-name of
    /// a FUNCTION-ID paragraph (§8.3.2.2) — once the REPOSITORY in scope is known: the unit's own paragraph and, for a
    /// contained program, its container's (inherited by <c>InheritConfiguration</c>; §12.3.4 GR1 — "the entries
    /// explicitly or implicitly specified in the configuration section of a source unit that contains other source
    /// units apply to each directly or indirectly contained source unit"). A class, interface or method
    /// is declared by the OO driver, whose units reach this binder as synthetic contexts with no identification
    /// division.</summary>
    private void DeclareUnitName(Core.ProgramUnitContext program)
    {
        if (UnitOwnName(program) is not var (at, word, kind)) return;
        using (Edition.At(at)) DeclareUserWord(word, kind);
    }

    /// <summary>The user-defined word a unit's own identification paragraph declares, and its type — null for a
    /// unit with no PROGRAM-ID / FUNCTION-ID paragraph (a class, interface or method reaches this binder as a synthetic
    /// context; the OO driver declares those). The IS PROTOTYPE format declares a prototype-name instead (§11.10.2
    /// Format 2, §11.5.2 Format 2). ONE reading, asked by <see cref="DeclareUnitName"/> and by the REPOSITORY
    /// walk, which must not declare a specifier that names the unit itself (§12.3.8.3 SR11 / SR15: "this specifier
    /// is ignored").</summary>
    private static (Antlr4.Runtime.ParserRuleContext At, string Word, UserWordKind Kind)? UnitOwnName(Core.ProgramUnitContext program)
    {
        var id = program.identificationDivision()?.identificationBody();
        if (id?.programIdParagraph() is { } pid && pid.programName() is { } pgm)
            return (pgm, pgm.GetText(), pid.prototypePhrase() is null
                ? UserWordKind.ProgramName : UserWordKind.ProgramPrototypeName);
        if (id?.functionIdParagraph() is { } fid && fid.programName() is { } fn)
            return (fn, fn.GetText(), fid.prototypePhrase() is null
                ? UserWordKind.UserFunctionName : UserWordKind.FunctionPrototypeName);
        return null;
    }

    /// <summary>⛔ THE ONE ANSWER to "does the REPOSITORY paragraph in scope identify this word as an
    /// intrinsic-function-name?" (kb/Work PB1083) — asked by the declaration screen (<see cref="DeclareUserWord"/>),
    /// the keyword-omitted function reference (<c>IntrinsicBinder.KeywordOmittedFunction</c>) and the subscript
    /// renderer's function detection (<c>ReferenceResolver.IsFunctionBearing</c>), which used to ask
    /// <c>IntrinsicCatalog.TryGet(written word)</c> three times over and so ignored both halves below.
    /// <list type="bullet">
    /// <item>&gt;&gt;COBOL-WORDS, through the ONE resolution (<c>CobolWordsMap.Resolve</c>, kb/Work PB250): §12.3.8.4
    /// GR14 — ALL is "as if each of the intrinsic-function-names defined in 8.11 … except for those that may have
    /// been undefined by the COBOL-WORDS directive, were specified"; a SUBSTITUTE literal-4 is replaced in the list by
    /// its literal-5 and an EQUATE literal-2 is added to it. So an UNDEFINE'd or SUBSTITUTE'd-away name is no longer
    /// a member, and a synonym is one exactly when its canonical intrinsic is.</item>
    /// <item>The edition: the §8.11 list is the targeted edition's — a name outside its function's D8 window is no
    /// intrinsic-function-name there (Annex E.2 item 13: BASECONVERT, CONCAT, … are prohibited under ALL only from
    /// 2023).</item>
    /// </list></summary>
    internal bool IsRepositoryIntrinsic(FunctionWord word) => RepositoryIntrinsicSpecifier(word) is not null;

    private const string AllIntrinsics = "ALL";

    /// <summary>The membership behind <see cref="IsRepositoryIntrinsic"/>, naming the specifier that makes
    /// <paramref name="word"/> a member: its canonical intrinsic-function-name when a <c>FUNCTION name INTRINSIC</c>
    /// specifier identifies it (§12.3.8.3 SR12), <see cref="AllIntrinsics"/> when only <c>FUNCTION ALL INTRINSIC</c>
    /// does (SR13), null when neither. <see cref="RepositoryIntrinsics"/> holds each named specifier's CANONICAL
    /// name, so every spelling of one function answers alike.</summary>
    private string? RepositoryIntrinsicSpecifier(FunctionWord word)
    {
        if (!RepositoryAllIntrinsic && RepositoryIntrinsics.Count == 0) return null;   // no intrinsic specifier in scope
        if (!TryIntrinsicOfThisCompilation(word, out var sig)) return null;
        if (RepositoryIntrinsics.Contains(sig.Name)) return sig.Name;
        return RepositoryAllIntrinsic ? AllIntrinsics : null;
    }

    /// <summary>Is <paramref name="word"/> an intrinsic-function-name OF THIS COMPILATION — through &gt;&gt;COBOL-WORDS
    /// (an EQUATE / SUBSTITUTE synonym names its canonical intrinsic; an UNDEFINE'd or SUBSTITUTE'd-away name names
    /// none: §7.3.10.4 GR2-GR4) and inside its function's D8 edition window (the §8.11 list is the targeted
    /// edition's)? The one answer the REPOSITORY membership and <c>IntrinsicBinder.KeywordOmittedFunction</c> both
    /// ask (kb/Work PB1083).</summary>
    internal bool TryIntrinsicOfThisCompilation(FunctionWord word, out IntrinsicSig sig)
    {
        sig = default;
        return word.Canonical is { } canonical
            && IntrinsicCatalog.TryGet(canonical, out sig) && sig.IsDefinedAt(Edition.DialectLevel);
    }

    // ── TYPEDEF / the TYPE clause (ISO §13.18.58 / §13.18.57; data-model D17) ──────────────────────────────────

    /// <summary>Post-build TYPE-expansion pass: every <c>TYPE IS type-name</c> reference (a real item in the forest)
    /// gets the referenced type declaration's subtree cloned in. Runs at the top of <see cref="BindResolve"/> — AFTER
    /// all <see cref="BindEntries"/> (so forward references resolve; every TYPEDEF is in <see cref="TypeDecls"/>),
    /// AFTER <see cref="SynthesizeImpliedPictures"/> (so a template's §13.16.3 SR9 VALUE-implied PICTURE is part of
    /// the description it hands over — §13.18.57.4 GR3; kb/Work PB1300) and BEFORE the resolution passes (so the
    /// clone is a normal part of the forest they walk).</summary>
    internal void ExpandTypes()
    {
        foreach (var item in AllItems().Where(i => i.TypeRefName is not null).ToList())
            ExpandType(item, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        // SAME AS (ISO §13.18.49; P10 Step 16) — expanded AFTER every TYPE reference, so a data-name-1 that
        // was itself declared with a TYPE clause copies its fully-EXPANDED description (GR1 — "as though the
        // data description identified by data-name-1 had been coded in place"). Chains (a target with its own
        // pending SAME AS) recurse inside ExpandSameAs; cycles are the §13.18.49.3 SR3 rejection.
        foreach (var item in AllItems().Where(i => i.SameAsName is not null).ToList())
            ExpandSameAs(item, []);
        ResolveRestrictedTypes();
    }

    /// <summary>Attach the TYPE DECLARATION a restricted data-pointer's <c>USAGE POINTER TO type-name-1</c> names
    /// (ISO §13.18.60.4 GR23) to its <see cref="PicInfo.RestrictedTypeDecl"/>. Runs at the TAIL of
    /// <see cref="ExpandTypes"/>, so every template's own nested TYPE / SAME AS references are already expanded
    /// and the declaration it attaches is the complete one.
    /// <para>The restriction's carried identity used to be the NAME alone, which decides correctly WITHIN a
    /// source element (§13.18.58 makes a type-name unique there) and wrongly ACROSS them: two elements may each
    /// declare a non-equivalent type under one name, and §8.5.3.1 makes equivalence — not the spelling — the
    /// test §14.9.39.3 SR19, §14.9.3.3 SR4/SR5 and §14.8.2.3.2 spend (kb/Work PB427). No diagnostic is
    /// raised for an unresolved name: §13.18.60.3 SR18 already requires such an entry to be a TYPEDEF, and a
    /// restriction whose declaration is not in hand falls back to the name comparison rather than rejecting.</para></summary>
    private void ResolveRestrictedTypes()
    {
        foreach (var item in AllItems())
            if (item.Pic is { Category: PicCategory.Pointer, RestrictedTypeName: { } tn, RestrictedTypeDecl: null }
                && TypeDecls.TryGetValue(tn, out var decl))
                item.Pic = item.Pic with { RestrictedTypeDecl = decl };
    }

    /// <summary>Clone the referenced TYPEDEF template into <paramref name="item"/> (ISO §13.18.57.4 GR1/GR2): an
    /// ELEMENTARY type copies its PICTURE onto the item (a leaf); a GROUP type clones each subordinate under the item
    /// (a group). The subject's OWN OCCURS (an array-of-type, §13.16 SR14) and VALUE (GR3) are already on the item and
    /// preserved. <paramref name="expanding"/> is the set of type-names on the current expansion chain — a nested TYPE
    /// reference back to one is a recursive type declaration (§13.18.58.3 SR2 → COBOLNET1530).</summary>
    private void ExpandType(DataItem item, HashSet<string> expanding)
    {
        using var _ = Edition.At(item);
        string typeName = item.TypeRefName!;
        item.TypeRefName = null;   // mark expanded (idempotent; also stops a cloned nested ref being re-processed)
        string subject = item.CobolName ?? item.CsName;
        if (!TypeDecls.TryGetValue(typeName, out var template))
        {
            Edition.Error("COBOLNET1530", $"TYPE '{typeName}' on '{subject}': the type-name is not defined by any "
                + "TYPEDEF entry (ISO §13.18.57 / §13.18.58)");
            return;
        }
        if (!expanding.Add(typeName))
        {
            Edition.Error("COBOLNET1530", $"TYPE '{typeName}' on '{subject}': a type declaration shall not directly "
                + "or indirectly reference itself (ISO §13.18.58.3 SR2)");
            return;
        }
        // ⛔ A CHAINED TYPE DECLARATION EXPANDS FIRST — the TYPE arm's copy of the hop ExpandSameAs has always
        // had for its target (kb/Work PB513's sibling sweep; the two-arm dispatch shape). §13.18.58.3 SR2 is
        // written for exactly this shape — "The description of the subject of the entry, including its
        // subordinate items, shall not contain a TYPE clause that directly or indirectly references this type
        // definition" — so a type declaration whose OWN description is a TYPE clause is legal, and
        // §13.18.58.4 GR3 then hands "all other data description clauses and subordinate data descriptions" of
        // it to data defined using the type-name. Without this hop the template was copied UNEXPANDED: it owns
        // no PICTURE and no children of its own, so `01 T IS TYPEDEF PIC X(3). 01 TT IS TYPEDEF TYPE T.
        // 01 F TYPE TT.` made F a synthesized one-character item instead of the PIC X(3) the chain declares —
        // a wrong answer, from the same missing arm PB513's SR14 guard was. `expanding` travels, so a cyclic
        // chain is the SR2 rejection above rather than an unbounded recursion; ExpandType is idempotent
        // through the TypeRefName-null mark, so the template expands once however many references it has.
        if (template.TypeRefName is not null) ExpandType(template, expanding);
        // Record the TYPE identity BEFORE cloning children (review DEVLOG 664 fix #5): a nested TYPE reference is
        // expanded inside CloneItem, and its SR6 strong-placement check walks THIS item's ancestor chain — so the
        // enclosing strong item's StrongType must already be set, else legal strong-in-strong nesting is falsely
        // rejected. TypeName also anchors the §8.5.3 same-type test (see DataItem.TypeAnchor).
        item.TypeName = typeName;
        item.StrongType = template.TypedefStrong;
        // §13.18.57.4 GR2 d): a GROUP type's subject "is aligned as though it were a level 1 item" — so a bit
        // group typed after a same-level bit item starts at the first bit of a byte (§8.5.1.6.3), not inside the
        // predecessor's byte (kb/Work PB1569). An ELEMENTARY type has no GR2, and its subject is placed by
        // §8.5.1.6.3 exactly as written in place. Read by BitLayout.SharesByteWith, the one placement predicate.
        item.AlignedAsLevelOne = template.IsGroup;

        // An EXTERNAL type declaration's effect lands on its REFERENCES (ISO §13.18.22; P10 Step 16 — the
        // former COBOLNET1534 stage is lifted): GR2 — a data description containing an external type shall be
        // at level-number 1; GR3 — such a record is itself EXTERNAL (marked here; CallBindExternalAndGlobal
        // re-bases it onto the run-unit ExternalStore cell exactly like an explicitly-EXTERNAL record).
        if (template.IsExternalTypedef)
        {
            if (item.Level != 1)
                Edition.Error(DiagnosticCatalog.ExternalTypeRule, $"'{subject}' references EXTERNAL type "
                    + $"'{typeName}': a data description containing an external type declaration shall be at "
                    + "level-number 1 (ISO §13.18.22.4 GR2)");
            else
                item.ExternalFromType = true;   // §13.18.22 GR3
        }
        // §13.18.22 SR5: when a record description is an external item, an associated type declaration that is
        // strongly typed shall also be external.
        if (item.HasExternalClause && template.TypedefStrong && !template.IsExternalTypedef)
            Edition.Error(DiagnosticCatalog.ExternalTypeRule, $"'{subject}': an EXTERNAL record described with "
                + $"STRONG type '{typeName}' requires that type declaration to be external too "
                + "(ISO §13.18.22.3 SR5)");
        // VCR 16, the STRENGTH half (§13.16.3 SR13 ¶2; Annex E.2 item 10; the P13 review finding C9): "If the
        // CONSTANT RECORD clause is specified with the EXTERNAL clause, there shall also be a TYPE clause that
        // specifies a STRONGLY typed definition." The declaration-site check (BindEntry) can verify only TYPE
        // PRESENCE — strength is known HERE, where the template resolved. ≥2023 like its presence half (the
        // requirement is the 2023 flip; below 2023 a weak-TYPE external constant record was legal). Scoped to the
        // literal EXTERNAL-clause co-occurrence SR13 ¶2 names (an item external only via the TYPE's own EXTERNAL —
        // GR3 — has no EXTERNAL clause, and its external typedef is separately gated by VCR 63 when strong).
        if (item.IsConstantRecord && item.HasExternalClause && !template.TypedefStrong
            && Edition.DialectLevel >= 2023)
            Edition.Error(DiagnosticCatalog.ConstantRecordRule, $"'{subject}': a CONSTANT RECORD clause specified "
                + $"with the EXTERNAL clause requires a TYPE clause naming a STRONGLY typed definition — "
                + $"'{typeName}' is a weak (non-STRONG) typedef (ISO §13.16.3 SR13 ¶2; Annex E.2 item 10; ≥2023)");

        // §13.18.57.3 SR7 (review fix #1): a level-77 subject requires an ELEMENTARY type — a level-77 item is an
        // independent elementary item (§13.18.38). Version- AND strength-invariant (this applies to WEAK types too;
        // the old SR6 branch caught only the STRONG case).
        if (item.Level == 77 && template.IsGroup)
            Edition.Error("COBOLNET1536", $"'{subject}': a level-77 item referencing TYPE '{typeName}' requires an "
                + "elementary type — a level-77 item is an independent elementary item (ISO §13.18.57.3 SR7)");

        // §13.18.57.3 SR5: "No group item to which the subject of the entry is subordinate shall contain a
        // GROUP-USAGE, SIGN, or USAGE clause" — any of the three would override the type declaration's fixed
        // representation. The set is the ONE predicate SAME AS's SR9 twin reads (kb/Work PB889).
        if (SuperordinateWithRepresentationClause(item) is { } tp)
            Edition.Error("COBOLNET1538", $"'{subject}': a group item to which a TYPE entry is subordinate shall "
                + $"not contain a GROUP-USAGE, SIGN, or USAGE clause — '{tp.CobolName ?? "FILLER"}' does "
                + "(ISO §13.18.57.3 SR5)");

        // §13.18.57.3 SR6: a STRONG type may be referenced only at level 1 or by an item subordinate to another
        // strongly-typed group — strong typing always covers a WHOLE record, never a lone field in an ordinary group.
        if (template.TypedefStrong)
        {
            bool underStrong = false;
            for (var p = item.Parent; p is not null; p = p.Parent)
                if (p.StrongType) { underStrong = true; break; }
            if (item.Level != 1 && !underStrong)
                Edition.Error("COBOLNET1532", $"'{subject}' references STRONG type '{typeName}': a strongly-typed "
                    + "item shall be specified only at level 1 or subordinate to a strongly-typed group "
                    + "(ISO §13.18.57.3 SR6)");
        }

        // Clone the template's structure in (children / the entry description / the type's root-level 88s)
        // AFTER the flags above. The entry-description copy (§13.18.58.4 GR3 — "all other data description
        // clauses ... are assumed by data defined using the type-name") shares CopyEntryDescription with the
        // SAME AS expansion; scope TypeSubject — §13.18.57.4 GR1 EXCLUDES alignment (contrast §13.18.49.4 GR1,
        // which copies it). The subject's own VALUE wins (§13.18.57.4 GR3 — RawValue ??=).
        if (template.IsGroup)
            foreach (var child in template.Children)
                item.Children.Add(CloneItem(child, item, expanding));
        bool wroteBased = item.IsBased;
        CopyEntryDescription(template, item, DescriptionCopyScope.TypeSubject);
        if (!wroteBased && item.IsBased) ScreenComposedBased(item, $"TYPE '{typeName}'");
        // §13.18.57.4 GR3 — "If a VALUE clause is specified in the data description of the subject of the entry,
        // the content of the literal associated with that VALUE clause is used for the initial value associated
        // with the subject of the entry", and the subject IS the composed item, so its OWN literal answers to the
        // §13.18.63.3 literal screen against the COMPOSED description — the template's PICTURE, written or
        // SR9-implied (kb/Work PB1300). BindEntry could not ask it: the entry had no PICTURE until this copy, so
        // `01 W TYPE U VALUE "QRSTUV".` over a two-character U, and `01 M TYPE N VALUE 12345.` over a PIC 9(3)
        // N, compiled clean and stored a truncated value. A COPIED value (ValueIsCopied) is the template's, which
        // its own entry already answered for; a GROUP subject's own VALUE is the group-value screen's
        // (CheckGroupValueDeclarations), which reads the composed forest already.
        if (!item.ValueIsCopied && item.Pic is { } composedPic && !composedPic.IsRecovery)
        {
            string valueWhere = $"data item '{subject}'";
            if (item.RawValue is { } ownValue)
                item.RawValue = ScreenValueLiteral(composedPic, ownValue, valueWhere,
                    ValueSubject.ForElementary(composedPic, item.IsDynamicLength, item.IsAnyLength,
                        item.PicIsValueImplied));
            ScreenTableValueLiterals(item, valueWhere);
        }
        foreach (var c88 in template.Own88s) CloneConditionOnto(item, c88);   // the type's ROOT-level 88s (GR1; D17 inc 3)
        expanding.Remove(typeName);
    }

    /// <summary>The nearest group SUPERORDINATE to <paramref name="subject"/> whose entry contains a GROUP-USAGE,
    /// SIGN, or USAGE clause, or null. ⛔ ONE PREDICATE FOR ONE SET: §13.18.49.3 SR9 (SAME AS) and §13.18.57.3 SR5
    /// (TYPE) each name exactly these three clauses, and the two ancestor walks that screened them each tested
    /// two, so GROUP-USAGE — the clause both diagnostics already named — was screened by neither (kb/Work PB889).
    /// <para>It reads <see cref="DataItem.GroupUsage"/>, which at <c>ExpandTypes</c> — the FIRST bind pass, and
    /// the only caller — holds only a WRITTEN (or description-copied) clause: the §13.16.4 GR1/GR2 implication
    /// onto subordinate groups is applied later by the usage-inheritance walk, and an implied value can only
    /// come from an ancestor that wrote the clause, which this walk reaches anyway. So the answer is the rule's
    /// "contain a GROUP-USAGE clause" in either order of passes.</para></summary>
    private static DataItem? SuperordinateWithRepresentationClause(DataItem subject)
    {
        for (var p = subject.Parent; p is not null; p = p.Parent)
            if (p.GroupUsage is not GroupUsage.None || p.OwnSign is not null || p.OwnUsage is not null)
                return p;
        return null;
    }

    /// <summary>ISO §13.16.3 SR16's LEVEL sentence — "The level number of such data description entries shall be
    /// 1 or 77" — as ONE predicate for both ways an entry comes to carry a BASED clause: written
    /// (<c>BindEntry</c>'s BASED block) and composed by a TYPE / SAME AS copy (<see cref="ScreenComposedBased"/>).
    /// ⚠ SR16's SECTION sentence ("only in data description entries in the linkage section, in the
    /// working-storage section, and in the local-storage section") is NOT asked yet by either arm — kb/Work PB516
    /// owns it, and its fix belongs HERE so both arms acquire it at once.</summary>
    private static bool BasedLevelAdmitted(int level) => level is 1 or 77;

    /// <summary>The placement screen for a BASED clause a TYPE or SAME AS clause COMPOSED into
    /// <paramref name="subject"/> (kb/Work PB1300). §13.18.57.4 GR1 and §13.18.49.4 GR1 make the clause's effect
    /// "as though the data description identified by type-name-1 [data-name-1] had been coded in place", and
    /// neither excludes BASED, so the composed entry is held to the rules a written BASED clause is: §13.16.3 SR16
    /// (level 1 or 77), SR5 ("The EXTERNAL clause shall not be specified in the same data description entry as
    /// the REDEFINES or BASED clause") and SR13 (BASED "shall not be specified in the same data description entry
    /// with the CONSTANT RECORD clause"). §13.16.3 SR3's REDEFINES arm cannot arise — SR12 and SR14 already refuse
    /// REDEFINES beside SAME AS and TYPE. The formal-parameter ban (§14.2.2 SR1) and the class-object rule
    /// (§13.18.5.3 SR1) read <see cref="DataItem.IsBased"/> in later passes and so judge the composed entry
    /// without help. A violation reports once and clears the flag, so the entry binds as ordinary storage under
    /// an already-failed compile — the written arm's discipline.</summary>
    private void ScreenComposedBased(DataItem subject, string via)
    {
        string? rule =
            !BasedLevelAdmitted(subject.Level)
                ? $"the BASED clause may be specified only in a level-01 or level-77 entry, and this entry is at "
                  + $"level {subject.Level:00} (ISO §13.16.3 SR16)"
            : subject.HasExternalClause
                ? "the EXTERNAL clause shall not be specified in the same data description entry as the BASED "
                  + "clause (ISO §13.16.3 SR5)"
            : subject.IsConstantRecord
                ? "the BASED clause shall not be specified in the same data description entry with the CONSTANT "
                  + "RECORD clause (ISO §13.16.3 SR13)"
            : null;
        if (rule is null) return;
        Edition.Error(DiagnosticCatalog.ComposedBasedPlacement, $"'{subject.CobolName ?? "FILLER"}': {via} brings "
            + "a BASED clause into this entry — the effect is as though the referenced description had been coded "
            + $"in place (ISO §13.18.57.4 GR1 / §13.18.49.4 GR1) — but {rule}");
        subject.IsBased = false;
    }

    /// <summary>⛔ THE ONE DATA-DESCRIPTION COPY — every clause one entry's description hands to another, for
    /// all five carriers: <see cref="ExpandType"/> (ISO §13.18.58.4 GR3 / §13.18.57.4 GR1),
    /// <see cref="ExpandSameAs"/> (§13.18.49.4 GR1), <see cref="CloneItem"/> (§13.18.58.4 GR1 /
    /// §13.18.49.4 GR2a — a reproduced SUBORDINATE), and the compiler temporary's root and subtree
    /// (<see cref="CreateCompilerTemp"/> / <see cref="CloneTempNode"/> — §8.4.3.2.4 GR1's "the description,
    /// class, and category of the temporary data item is that specified by the description in the linkage
    /// section of the item specified in the RETURNING phrase", and its §8.4.3.4.4 / §14.9.25.4 twins; until
    /// kb/Work PB888 those two spelled a FIVE-clause hand list of their own, so a function result typed as a
    /// GROUP-USAGE NATIONAL group bound as an alphanumeric group and FUNCTION LENGTH counted it in the wrong
    /// unit). The receiver's OWN clause always wins (<c>??=</c> /
    /// <c>is None</c> — §13.18.57.4 GR3 for VALUE; a SAME AS / TYPE subject can own almost none of these, since
    /// §13.16.3 SR12 and SR14 list the few clauses admissible in the same entry and no clause copied here is
    /// among them).
    ///
    /// <para>⛔ WHICH FIELDS TRAVEL IS NOT WRITTEN HERE. Both GR-1s state an EXCLUSION list, so the default is
    /// "carried", and a field-by-field hand list in an object initializer inverts that default silently: a
    /// clause absent from the list is simply lost, with no diagnostic and no place the loss is visible. That is
    /// how <see cref="DataItem.GroupUsage"/> — the one field that makes an item a bit or national group — went
    /// missing from BOTH copiers, so <c>01 R TYPE T</c> over a <c>GROUP-USAGE NATIONAL</c> template bound as an
    /// ordinary alphanumeric group with the wrong class, category and LENGTH (kb/Work PB522), and how
    /// SYNCHRONIZED, ALIGNED, ANY LENGTH and DYNAMIC LENGTH were lost with it. Every stored
    /// <see cref="DataItem"/> property therefore carries a <see cref="DescriptionCopyAttribute"/> saying what it
    /// is to this copy and why, and <c>DescriptionCopyCompletenessDriftTests</c> (Unit) fails when a property
    /// has no classification, when a <see cref="DescriptionCopyKind.Clause"/> field is not transferred here, or
    /// when a <see cref="DescriptionCopyKind.None"/> / <see cref="DescriptionCopyKind.MemberOnly"/> field is.
    /// Adding a field to <see cref="DataItem"/> is then a CHOICE, not an omission.</para></summary>
    /// <param name="scope">Which copy this is — the one axis on which the rules make the copies differ
    /// (<see cref="DescriptionCopyScope"/>). The ALIGNMENT clauses — SYNCHRONIZED (§13.18.55) and ALIGNED
    /// (§13.18.1) — stay behind for a TYPE SUBJECT alone: §13.18.57.4 GR1 is the only GR-1 that excludes
    /// "alignment" (its GR2d re-aligns that subject "as though it were a level 1 item"); §13.18.49.4 GR1 has no
    /// alignment exclusion, and a reproduced subordinate carries its own entry's clauses whole. The
    /// <see cref="DescriptionCopyKind.EntryOnly"/> clauses (VALUE, ANY LENGTH) stay behind for a COMPILER
    /// TEMPORARY alone — the enum member says why.</param>
    private static void CopyEntryDescription(DataItem from, DataItem to, DescriptionCopyScope scope)
    {
        bool entryCopy = scope is not DescriptionCopyScope.CompilerTemp;
        if (to.Pic is null && from.Pic is not null)
        {
            to.Pic = from.Pic;
            to.PicIsUsageSynthesized = from.PicIsUsageSynthesized;   // the profile's provenance travels with it (PB495)
            to.PicIsValueImplied = from.PicIsValueImplied;           // … written or SR9-implied (§13.18.57.4 GR3; PB1300)
        }
        // ⛔ The WRITTEN character-string travels on its OWN condition, not on Pic's. §13.16.3 SR8's recovery
        // clears the analyzed profile while keeping the spelling the later screens have to name, so an entry can
        // carry PictureText with a NULL Pic; guarding this on `from.Pic is not null` would drop the spelling from
        // every clone of such an entry and change a downstream diagnostic under an already-failed compile. A
        // receiver never owns one here anyway — §13.16.3 SR12/SR14 forbid a PICTURE clause in the same entry as
        // SAME AS or TYPE — so the ??= is the subject-wins discipline, not a second condition.
        to.PictureText ??= from.PictureText;
        // ⛔ GROUP-USAGE (§13.18.29) — the clause that makes the item a BIT or NATIONAL group, i.e. its class,
        // its category and the unit its LENGTH counts in (§8.5.2.5 item 3 / §8.5.2.10 item 3; §15.50.4 r1/r2).
        // It is in NEITHER GR-1 exclusion list, and §13.18.49.4 GR4 propagating a GROUP-USAGE from data-name-1's
        // SUPERORDINATE shows the clause is plainly meant to travel. Losing it did not merely mis-measure the
        // subject: §13.16.4 GR1/GR2 imply the clause for every SUBORDINATE group, so a false antecedent here
        // silently re-categorized a whole subtree (kb/Work PB522).
        if (to.GroupUsage is GroupUsage.None) to.GroupUsage = from.GroupUsage;
        if (to.Pending is PicPending.None) to.Pending = from.Pending;
        // The provenance of the VALUE travels with it (DataItem.ValueIsCopied): the §13.18.63.3 SR13/SR14
        // screen's subject is the entry that WROTE the VALUE clause, so a copied one must not re-report the
        // template's / target's already-screened entry once per reference site.
        // ⛔ BOTH VALUE CARRIERS, not just the Format-1 one (kb/Work PB505's sibling sweep). RawValue and
        // TableValues are the two spellings of ONE clause (§13.18.63.2 formats 1 and 2, mutually exclusive on the
        // entry), and copying only the first silently DROPPED a table VALUE across every TYPE / SAME AS reference:
        // `01 TT TYPEDEF. 05 X PIC X(2) OCCURS 3 VALUE "AB" FROM (1). 01 R TYPE TT.` composed a clone whose
        // occurrences were all VALUE-less. §13.18.57.4 GR1 / §13.18.49 GR1 copy the DESCRIPTION, and the VALUE
        // clause is in neither GR's exclusion list, in either of its formats.
        if (entryCopy)
        {
            if (to.RawValue is null && to.TableValues is null && (from.RawValue is not null || from.TableValues is not null))
                to.ValueIsCopied = true;
            if (to.RawValue is null && to.TableValues is null) to.TableValues = from.TableValues;
            to.RawValue ??= from.RawValue;
        }
        to.Justified |= from.Justified;                             // JUSTIFIED (§13.18.32)
        to.BlankWhenZero |= from.BlankWhenZero;                     // BLANK WHEN ZERO (§13.18.8)
        // ⛔ BOTH ALIGNMENT CLAUSES, not just SYNCHRONIZED: §13.18.57.4 GR1 excludes "alignment", and ALIGNED
        // (§13.18.1) is the other clause that word names — §13.18.1.3 SR1 admits it on a bit group item or an
        // elementary bit data item, i.e. exactly on a TYPEDEF member, where dropping it moved every following
        // bit item (§13.18.1.4 GR1's "first bit of the first available byte boundary", §8.5.1.6.3).
        if (scope is not DescriptionCopyScope.TypeSubject)
        {
            to.Synchronized |= from.Synchronized;                   // SYNCHRONIZED (§13.18.55)
            to.IsAligned |= from.IsAligned;                         // ALIGNED (§13.18.1)
            to.AlignedAsLevelOne |= from.AlignedAsLevelOne;         // a TYPE clause's GR2 d) (§13.18.57.4)
        }
        to.OwnUsage ??= from.OwnUsage;
        // The WITH NO SIGN phrase is PART OF the USAGE clause (§13.18.60.2 prints it inside the PACKED-DECIMAL
        // alternative), so it travels wherever that clause does. Its EFFECT rides on Pic.PackedNoSign, which
        // UsageInheritancePass has not yet applied when this copy runs (ExpandTypes is the first pass) — copying
        // the clause fact is what makes `01 TT TYPEDEF USAGE PACKED-DECIMAL WITH NO SIGN. 05 X PIC 9(4).` give
        // its subject the same 2-byte leaf the template describes (kb/Work PB570).
        to.OwnNoSign |= from.OwnNoSign;
        to.OwnSign ??= from.OwnSign;
        // ANY LENGTH (§13.18.2) and DYNAMIC LENGTH (§13.18.19) are in neither GR-1 exclusion list either, and
        // both decide the item's LENGTH: a SAME AS of a DYNAMIC LENGTH item that dropped the clause became a
        // FIXED one-character item. §13.18.2.3 SR2/SR3/SR4 and the DYNAMIC LENGTH shape rules are re-screened at
        // the copy's own site by the placement sweeps, which CLEAR the flag where the new site fails them.
        if (entryCopy) to.IsAnyLength |= from.IsAnyLength;   // EntryOnly: §13.18.2.3 SR2's linkage parameter shape
        if (entryCopy) to.Property ??= from.Property;          // EntryOnly: a temporary is never object data (§13.18.42.3 SR1)
        // BASED (§13.18.5) is in neither GR-1 exclusion list, so the subject of `01 X TYPE T` over
        // `01 T TYPEDEF BASED …` IS a based item (§13.18.58.4 GR3; kb/Work PB1300). §13.18.57.4 GR4 — "that BASED
        // clause applies and any BASED clause specified in the description of type-name-1 is ignored for this
        // entry" — decides only WHOSE clause it is when the subject writes one too, and both clauses mean the
        // same thing, so the |= is the whole of GR4. The composed entry's placement is screened at the
        // copy's site (ScreenComposedBased), where the subject's level and section are known.
        if (entryCopy) to.IsBased |= from.IsBased;           // EntryOnly: a temporary is never a template
        if (!to.IsDynamicLength && from.IsDynamicLength)
        {
            to.IsDynamicLength = true;
            to.DynMaxSize = from.DynMaxSize;    // §8.5.1.10.1's maximum size rides the clause (kb/Work PB463)
            to.DynStructure = from.DynStructure;   // … and so does its dynamic-length-structure-name (kb/Work PB829)
        }
        to.TypeName ??= from.TypeName;     // a data-name-1 declared with TYPE carries its type identity (§8.5.3)
        to.StrongType |= from.StrongType;
    }

    /// <summary>Deep-clone one template node under <paramref name="newParent"/> (generalizes the flat OO compiler-temp
    /// clone, <c>CreateCompilerTemp</c>): a FRESH <see cref="DataItem.Uid"/> (StructName/ProfileName ride on it, so a
    /// shared Uid would collide the clone's emitted type/profile with the template's), the immutable
    /// <see cref="DataItem.Pic"/> shared, the description fields copied, the <see cref="DataItem.CsName"/> re-uniquified
    /// in the NEW scope, and — unlike the template — REGISTERED (clones ARE referenceable). A nested TYPE reference in
    /// the template is expanded in place; so is a nested SAME AS (§13.18.49 — a subordinate of the copied
    /// description carrying SAME AS re-expands per clone, the TypeRefName pattern). <paramref name="levelDelta"/>
    /// renumbers the cloned subtree's level-numbers relative to the new subject (ISO §13.18.49.4 GR2b — SAME AS
    /// splices a level-1 description under an arbitrary-level subject, and the adjusted levels may exceed 49,
    /// GR2c; TYPE expansion passes 0 — its templates clone level-verbatim, byte-stable).
    ///
    /// <para>⛔ THE CLAUSES ARE NOT LISTED HERE. This initializer carries only what is NOT a data description
    /// clause — identity (a fresh Uid, a re-uniquified CsName), the renumbered level, and the
    /// <see cref="DescriptionCopyKind.MemberOnly"/> fields the two ENTRY-copy GR-1s exclude but §13.18.58.4 GR1
    /// reproduces on a subordinate (the name, OCCURS, REDEFINES, a nested TYPE / SAME AS reference, the
    /// declaration cursor). Every actual clause goes through <see cref="CopyEntryDescription"/>, the ONE copy,
    /// so a clause cannot be present in one copier and absent from the other — which is precisely how
    /// GROUP-USAGE, SYNCHRONIZED and ALIGNED were lost from this one (kb/Work PB522). Scope <c>Entry</c>:
    /// only §13.18.57.4 GR1's TYPE SUBJECT excludes alignment; a reproduced subordinate carries its own entry's
    /// clauses whole.</para></summary>
    private DataItem CloneItem(DataItem src, DataItem newParent, HashSet<string> expanding, int levelDelta = 0)
    {
        var clone = new DataItem
        {
            Level = src.Level + levelDelta,
            DeclaredAt = src.DeclaredAt,
            CobolName = src.CobolName,
            CsName = Unique(src.CsName, newParent.Children.Select(c => c.CsName)),
            Occurs = src.Occurs,
            // Clone the OccursSpec — never SHARE it: its Depending / CapacityRegister are RESOLVED per-clone by the
            // post-build OdoResolve / DynamicResolve, so a shared object would let two clones of the same group type
            // collide on those fields (D17 risk #1). Copy the declared fields; leave the resolved ones null.
            OccursSpec = src.OccursSpec is { } os ? CloneOccursSpec(os) : null,
        };
        // Every data description CLAUSE of the member's own entry — §13.18.58.4 GR1 ("the subordinate entries
        // are part of the type") / §13.18.49.4 GR2a ("the same names, DESCRIPTIONS, and hierarchy").
        CopyEntryDescription(src, clone, DescriptionCopyScope.Entry);
        clone.RedefinesTargetName = src.RedefinesTargetName;   // a member's REDEFINES is part of the type
        clone.TypeRefName = src.TypeRefName;                   // a nested TYPE reference re-expands per clone (below)
        clone.SameAsName = src.SameAsName;                     // a pending nested SAME AS likewise (below)
        clone.Uid = _uidCounter++;
        clone.Parent = newParent;
        clone.SameAsQualifiers.AddRange(src.SameAsQualifiers);
        // The member's INDEXED BY names are part of the type (§13.18.58.4 GR1). RegisterName re-declares them on
        // THIS clone with its own cells, so a type referenced twice yields two tables whose indexes are both named
        // IX — referenced as IX OF A / IX OF B (§8.4.2.2.3 SR6). The former COBOLNET1531 stage refused the second
        // reference only because the two shared one spelling-keyed cell; it is retired (kb/Work PB919).
        clone.IndexNames.AddRange(src.IndexNames);
        RegisterName(clone);
        foreach (var c88 in src.Own88s) CloneConditionOnto(clone, c88);   // §13.18.58.4 GR1 — the 88s are part of the type (D17 inc 3)
        foreach (var child in src.Children)
            clone.Children.Add(CloneItem(child, clone, expanding, levelDelta));
        if (clone.TypeRefName is not null) ExpandType(clone, expanding);   // a nested TYPE reference inside the template
        if (clone.SameAsName is not null) ExpandSameAs(clone, []);         // a nested SAME AS inside the copied description (§13.18.49)
        return clone;
    }

    /// <summary>Clone one level-88 condition-name from a TYPEDEF template item onto its <paramref name="target"/> clone
    /// (ISO §13.18.58.4 GR1 — the condition-names are part of the type): the clone's Parent is the target, its whole
    /// Format-3 state is copied by <see cref="Condition88.CopyOnto"/>, and — unlike the template's copy — it IS
    /// registered in the global by-name index (a clone's condition-names ARE referenceable). D17 inc 3.
    /// <para>⛔ THE FIELD LIST LIVES ON THE MODEL, NOT HERE (kb/Work PB555). This method used to copy the VALUE set
    /// and nothing else, so a clone silently lost the <c>IN alphabet-name-1</c> phrase — §14.7.8 rule 2's sequence
    /// fell back to the program default and every THRU range the clone tested could answer differently from the
    /// template's. One copier next to the state, with a drift test, is what stops the next added field from
    /// repeating it.</para></summary>
    private void CloneConditionOnto(DataItem target, Condition88 src)
    {
        var c = src.CopyOnto(target);
        target.Own88s.Add(c);
        if (!Conditions.TryGetValue(c.Name, out var list)) Conditions[c.Name] = list = [];
        list.Add(c);
    }

    /// <summary>Copy an <see cref="OccursSpec"/>'s DECLARED fields for a TYPEDEF clone (D17), leaving the post-build
    /// RESOLVED fields (<c>Depending</c>/<c>CapacityRegister</c>) null so OdoResolve/DynamicResolve re-bind them in
    /// the clone's OWN scope — a shared spec would let sibling clones of the same type collide on those.</summary>
    private static OccursSpec CloneOccursSpec(OccursSpec s)
    {
        var c = new OccursSpec
        {
            Min = s.Min, Max = s.Max, DependingName = s.DependingName, DependingQualifiers = s.DependingQualifiers,
            IsDynamic = s.IsDynamic,
            CapacityName = s.CapacityName, InitialCap = s.InitialCap, ExpectedMax = s.ExpectedMax,
            Initialized = s.Initialized,
        };
        c.Keys.AddRange(s.Keys);
        return c;
    }

    // ── SAME AS (ISO §13.18.49, COBOL-2002; P10 Step 16) ─────────────────────────────────────────────────────

    /// <summary>Expand one <c>SAME AS data-name-1</c> reference (ISO §13.18.49): resolve data-name-1 as an
    /// ordinary (optionally OF/IN-qualified) data-name, enforce the §13.18.49.3 syntax rules, then copy its
    /// data description onto <paramref name="item"/> — the subject "as though the data description identified
    /// by data-name-1 had been coded in place of the SAME AS clause" (GR1), via the ONE
    /// <see cref="CopyEntryDescription"/>/<see cref="CloneItem"/> machinery TYPE expansion uses
    /// (feedback_one_mechanism_per_job; SAME AS is structurally the TYPE expansion with a DATA-NAME source).
    /// Excluded from the copy per GR1: data-name-1's level-number, name, CONSTANT RECORD, EXTERNAL, GLOBAL,
    /// REDEFINES, and SELECT WHEN (not modeled). Subordinate levels renumber relative to the subject (GR2b/c —
    /// the <see cref="CloneItem"/> levelDelta). <paramref name="expanding"/> is the SAME-AS expansion chain —
    /// a target already on it is the SR3 cycle.</summary>
    private void ExpandSameAs(DataItem item, HashSet<DataItem> expanding)
    {
        using var _ = Edition.At(item);
        if (item.SameAsName is null) return;   // already expanded through a chain hop (idempotent — the TypeRefName pattern)
        string targetName = item.SameAsName;
        item.SameAsName = null;
        string subject = item.CobolName ?? item.CsName;
        expanding.Add(item);

        // §13.18.49.3 SR2: the entry shall not be immediately followed by a subordinate data description entry
        // or level-88 entry — the copied description IS the subject's whole shape.
        if (item.Children.Count > 0 || item.Own88s.Count > 0)
        {
            Edition.Error(DiagnosticCatalog.SameAsEntryRule, $"'{subject}': a data description entry that "
                + "specifies the SAME AS clause shall not be immediately followed by a subordinate data "
                + "description entry or level 88 entry (ISO §13.18.49.3 SR2)");
            return;
        }
        // §13.18.49.3 SR9: "A group item to which the subject of the entry is subordinate shall not contain a
        // GROUP-USAGE, SIGN, or USAGE clause" — the TYPE clause's §13.18.57.3 SR5 twin (1538), through the ONE
        // predicate both read (kb/Work PB889: both walks used to test SIGN and USAGE only, so
        // `01 OUTER GROUP-USAGE NATIONAL. 02 INNER SAME AS S.` compiled clean). Reported once; expansion
        // continues under the already-failed compile.
        if (SuperordinateWithRepresentationClause(item) is { } sp)
            Edition.Error(DiagnosticCatalog.SameAsEntryRule, $"'{subject}': a group item to which a SAME AS "
                + "entry is subordinate shall not contain a GROUP-USAGE, SIGN, or USAGE clause — "
                + $"'{sp.CobolName ?? "FILLER"}' does (ISO §13.18.49.3 SR9)");

        // Resolve data-name-1: an ordinary data-name reference (qualification narrows by ancestor names);
        // never subscripted — SR1 makes a table-subordinate target illegal anyway. TYPEDEF template members
        // are OFF ByName (§13.18.58.4 GR1), so a type declaration's insides are unreachable here by design.
        // ⛔ Through the ONE §8.4.2.2 resolver and the ONE ambiguity verdict (kb/Work PB978): the private
        // qualifier matcher this used knew nothing of the file-name qualifier (§8.4.2.2.2 Format 1's
        // file-report-qualifier), and its ambiguity sentence cited §8.4.3.2, a clause about something else.
        // §8.4.2.2.1 rule 5's implicit qualifiers apply: SAME AS is a data description entry clause.
        var candidates = EntryClauseCandidates(item, targetName, item.SameAsQualifiers, Model.Scope.Program);
        string writtenTarget = WrittenQualified(targetName, item.SameAsQualifiers);
        if (candidates.Count == 0)
        {
            Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': SAME AS '{writtenTarget}' "
                + "does not resolve to a data description entry (ISO §13.18.49.2 — data-name-1 shall reference a "
                + "data item; §13.18.49.3 SR7)");
            return;
        }
        if (UniqueOrReportAmbiguous(candidates, $"'{subject}': SAME AS", writtenTarget, out bool _) is not { } target)
            return;

        // §13.18.49.3 SR3 — cycles: data-name-1 (or its description, through a chain) shall not reference the
        // subject or any group the subject is subordinate to. The chain leg is the expanding-set test; the
        // containment leg is the subject-ancestor walk. (SR4 — a TYPE clause in data-name-1's description
        // referencing the subject's record — is caught by the same walks over the EXPANDED target, plus
        // §13.18.57.3 SR1 on the TYPE side.)
        if (ReferenceEquals(target, item) || expanding.Contains(target))
        {
            Edition.Error(DiagnosticCatalog.SameAsCycle, $"'{subject}': SAME AS '{targetName}' directly or "
                + "indirectly references the subject of the entry (ISO §13.18.49.3 SR3)");
            return;
        }
        // A chained SAME AS target expands FIRST, so GR1 copies the COMPLETE description; a pending TYPE
        // reference likewise (defensive — the SAME AS loop runs after the TYPE loop, so Roots targets are
        // already expanded; ExpandType is idempotent via the TypeRefName-null mark).
        if (target.SameAsName is not null) ExpandSameAs(target, expanding);
        if (target.TypeRefName is not null) ExpandType(target, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        for (var p = item.Parent; p is not null; p = p.Parent)
            if (ReferenceEquals(p, target))
            {
                Edition.Error(DiagnosticCatalog.SameAsCycle, $"'{subject}': SAME AS '{targetName}' references a "
                    + "group item to which this entry is subordinate (ISO §13.18.49.3 SR3)");
                return;
            }

        // §13.18.49.3 SR7: data-name-1 shall reference an elementary item or a level-1 group item (of the
        // file / working-storage / local-storage / linkage section; a level-66 RENAMES alias is neither).
        if (target.Level == 66 || !(target.IsElementary || target.Level == 1))
        {
            Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': SAME AS '{targetName}' shall "
                + "reference an elementary item or a level 1 group item (ISO §13.18.49.3 SR7)");
            return;
        }
        // §13.18.49.3 SR5: data-name-1's own entry shall not contain an OCCURS clause (subordinates may).
        if (target.Occurs is not null || target.OccursSpec is not null)
        {
            Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': the description of SAME AS "
                + $"target '{targetName}' shall not contain an OCCURS clause — only items subordinate to it may "
                + "(ISO §13.18.49.3 SR5)");
            return;
        }
        // §13.18.49.3 SR1: data-name-1 shall not be SUBJECT TO any OCCURS clause (no table ancestor — the
        // reference is a bare data-name, never subscripted).
        for (var p = target.Parent; p is not null; p = p.Parent)
            if (p.IsTable)
            {
                Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': SAME AS target "
                    + $"'{targetName}' shall not be subject to any OCCURS clause (ISO §13.18.49.3 SR1)");
                return;
            }
        // §13.18.49.3 SR10: data-name-1's description shall not contain a CONSTANT RECORD clause.
        if (target.IsConstantRecord)
        {
            Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': the description of SAME AS "
                + $"target '{targetName}' shall not contain a CONSTANT RECORD clause (ISO §13.18.49.3 SR10)");
            return;
        }
        // §13.18.49.3 SR8: a level-77 subject requires an elementary data-name-1 (a level-77 item is an
        // independent ELEMENTARY item — the TYPE-clause SR7 twin, 1536).
        if (item.Level == 77 && target.IsGroup)
        {
            Edition.Error(DiagnosticCatalog.SameAsEntryRule, $"'{subject}': a level-77 SAME AS subject requires "
                + "an elementary data-name-1 (ISO §13.18.49.3 SR8)");
            return;
        }
        // §13.18.49.3 SR6: in the FILE SECTION, data-name-1's description (subordinates included) shall not
        // contain a USAGE OBJECT REFERENCE item.
        if (IsFileSectionItem(item) && HasObjectReferenceLeaf(target))
        {
            Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': a SAME AS clause in the file "
                + $"section shall not reference '{targetName}', whose description contains a USAGE OBJECT "
                + "REFERENCE data item (ISO §13.18.49.3 SR6)");
            return;
        }

        // ── GR1/GR2: the copy. ──────────────────────────────────────────────────────────────────────────────
        // The entry description (PICTURE/USAGE/SIGN/VALUE/JUSTIFIED/BLANK WHEN ZERO/SYNCHRONIZED + the carried
        // TYPE identity; scope Entry — §13.18.49.4 GR1 does NOT exclude alignment, unlike §13.18.57.4 GR1).
        bool wroteBased = item.IsBased;   // §13.16.3 SR12 admits no BASED beside SAME AS; an error-recovery guard
        CopyEntryDescription(target, item, DescriptionCopyScope.Entry);
        if (!wroteBased && item.IsBased) ScreenComposedBased(item, $"SAME AS '{targetName}'");
        // GR3/GR5: a USAGE / SIGN clause of a group containing data-name-1 takes effect as though specified
        // for the SUBJECT (nearest enclosing clause, the §13.18.60 GR1 discipline; only an ELEMENTARY target
        // can have ancestors — SR7). The subject's chain cannot see data-name-1's ancestors, so the transform
        // the InheritUsage/InheritSign passes would have applied is applied here, on the copied Pic.
        // ⛔ EACH RULE NAMES THE GROUP KINDS IT TRANSFERS FROM, and the two sets differ (kb/Work PB1300): GR3 —
        // "If an alphanumeric group item or strongly-typed group item to which data-name-1 is subordinate
        // contains a USAGE clause"; GR5 — "an alphanumeric group item, national group item, or strongly-typed
        // group item … contains a SIGN clause". A group of any other kind carries no clause across, so the walk
        // passes over it to the next enclosing group. Unfiltered, `S SAME AS E` with E under `05 H USAGE
        // NATIONAL` inside `01 G GROUP-USAGE NATIONAL` took H's USAGE NATIONAL and was 6 bytes where GR1 makes
        // it E's own PIC 9(3) DISPLAY description, 3 bytes. Asked through the ONE group-kind classifier.
        if (item.OwnUsage is null)
            for (var p = target.Parent; p is not null; p = p.Parent)
                if (p.OwnUsage is { } au
                    && (ItemCategory.GroupKindsOf(p) & (GroupKinds.Alphanumeric | GroupKinds.StronglyTyped)) != 0)
                {
                    item.OwnUsage = au;
                    // ⛔ THE SAME transform §13.18.60.4 GR1 applies to a subordinate leaf — one method, so this
                    // third acquisition route cannot drift from the other two (before PB495 it carried its own
                    // copy of the `Binary or Packed or Comp5` hand-list and silently dropped every other usage).
                    ApplyEffectiveUsage(item, au);
                    // The clause travels WHOLE: its WITH NO SIGN phrase (§13.18.60.2) is not a separate clause,
                    // so GR3's "as though specified for the subject" carries it too. Recorded, not applied — the
                    // ONE adjudication site (ApplyEffectiveNoSign, from UsageInheritancePass) runs later in the
                    // pipeline and would otherwise report §13.18.40.3 SR31 twice on one entry (kb/Work PB570).
                    item.OwnNoSign |= p.OwnNoSign;
                    break;
                }
        if (item.OwnSign is null)
            for (var p = target.Parent; p is not null; p = p.Parent)
                if (p.OwnSign is { } asg
                    && (ItemCategory.GroupKindsOf(p)
                        & (GroupKinds.Alphanumeric | GroupKinds.National | GroupKinds.StronglyTyped)) != 0)
                {
                    item.OwnSign = asg;
                    // ⛔ THE SAME transform §13.18.52 applies to a subordinate leaf — one method, exactly as the
                    // USAGE route above, so this second acquisition route cannot drift from the inheritance one
                    // (it did: it kept a DISPLAY-only guard after PB646 widened the SR2 pair — see
                    // ApplyEffectiveSign). §13.18.49.4 GR5 names "an alphanumeric group item, national group
                    // item, or strongly-typed group item to which data-name-1 is subordinate".
                    ApplyEffectiveSign(item, asg);
                    break;
                }
        // GR2: a group data-name-1 → the subject becomes a group with the same subordinate names, descriptions,
        // and hierarchy; levels renumber relative to the subject (GR2b, may exceed 49 — GR2c). The ONE CloneItem.
        if (target.IsGroup)
        {
            int levelDelta = item.Level - target.Level;
            var typeExpanding = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in target.Children)
                item.Children.Add(CloneItem(child, item, typeExpanding, levelDelta));
        }
        foreach (var c88 in target.Own88s) CloneConditionOnto(item, c88);   // GR2a — data-name-1's own condition-names ride the copy

        // §13.18.22 GR3 carried through SAME AS: when data-name-1's external-ness came FROM ITS TYPE (not from
        // an explicit EXTERNAL clause — that one GR1 excludes), the copied description still names the external
        // type, so the subject is external under the same GR2 level-1 constraint.
        if (target.ExternalFromType)
        {
            if (item.Level != 1)
                Edition.Error(DiagnosticCatalog.ExternalTypeRule, $"'{subject}': a data description containing "
                    + "an external type declaration shall be at level-number 1 (ISO §13.18.22.4 GR2)");
            else
                item.ExternalFromType = true;
        }
        // A copied STRONG type identity re-checks placement (§13.18.57.3 SR6 — level 1 or subordinate to a
        // strongly-typed group; the ExpandType twin).
        if (item.StrongType && item.Level != 1)
        {
            bool underStrong = false;
            for (var p = item.Parent; p is not null; p = p.Parent)
                if (p.StrongType) { underStrong = true; break; }
            if (!underStrong)
                Edition.Error("COBOLNET1532", $"'{subject}' copies strongly-typed '{targetName}' (type "
                    + $"'{item.TypeName}'): a strongly-typed item shall be specified only at level 1 or "
                    + "subordinate to a strongly-typed group (ISO §13.18.57.3 SR6)");
        }
        expanding.Remove(item);
    }

    /// <summary>Whether an item belongs to a FILE SECTION record (its root is some FD/SD's record) — the
    /// §13.18.49.3 SR6 placement test.</summary>
    private bool IsFileSectionItem(DataItem item)
    {
        var root = item;
        while (root.Parent is { } p) root = p;
        return Files.Any(f => f.Records.Contains(root));
    }

    /// <summary>True when the item's description (subordinates included) contains a USAGE OBJECT REFERENCE
    /// data item (ISO §13.18.49.3 SR6 / §13.18.57.3 SR8).</summary>
    private static bool HasObjectReferenceLeaf(DataItem item) =>
        item.Pic?.Category is PicCategory.ObjectReference || item.Children.Any(HasObjectReferenceLeaf);

    /// <summary>The STRONG type-declaration use restrictions that need the RESOLVED REDEFINES/RENAMES graph
    /// (ISO §13.18.57.3): <b>SR4</b> — a strongly-typed item shall not be redefined in whole or in part; <b>SR3</b> —
    /// nor renamed in whole or in part. Both → COBOLNET1532. (SR6, the level-1/strong-parent placement rule, is
    /// checked at clone time in <see cref="ExpandType"/>.) An INTERNAL redefine — a REDEFINES that is part of the same
    /// strong subtree, cloned in from the type template — is legitimate and NOT flagged (its subject and target share
    /// a strong root); only an EXTERNAL redefinition of a strong item is prohibited.
    /// <para><b>The carve-out survives §13.18.57.3 SR4's letter — kb/Work PB183's companion derivation, ANSWERED.</b>
    /// SR4 reads "If type-name-1 is described with the STRONG phrase, the subject of the entry shall not be implicitly
    /// or explicitly redefined in whole or in part", and the question was whether that overturns the internal carve-out.
    /// It does not, because SR4's SUBJECT is the entry CARRYING the TYPE clause, and §13.18.57.3 SR2 — "A data
    /// description entry in which a TYPE clause is specified shall not be followed immediately by a subordinate data
    /// description entry or a level 88 entry" — makes a subordinate REDEFINES under a TYPE entry syntactically
    /// unwritable. The only REDEFINES that can reach a strong subtree from outside is <c>01 X REDEFINES S.</c>, which
    /// the loop below already rejects. A REDEFINES written INSIDE the typedef TEMPLATE has no TYPE-clause subject at
    /// all, so SR4 never reaches it. PB179's cross-root narrowing was correct and needs no change; a positive golden
    /// (<c>pb183_redefines_in_strong_typedef_ok</c>) guards it against a later "tightening".</para></summary>
    internal void CheckStrongTypeDeclarations()
    {
        // ⛔ SR4's IMPLICIT arm (kb/Work PB836): "the subject of the entry shall not be implicitly or explicitly
        // redefined in whole or in part". An FD/SD's level-1 records ARE implicit redefinitions of one area
        // (§13.18.33.4 GR3), and so are the records of a record-area SAME clause (§12.4.6.4.4 GR2), so a
        // strongly-typed item anywhere in such a record is implicitly redefined by every OTHER record sharing the
        // area. The sharing is SYMMETRIC — it does not matter which record the binder anchors the area on — so
        // both sides of each implicit pair are tested, and each strong item is reported once.
        var implicitlyRedefined = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        foreach (var item in AllItems())
            if (item.RedefinesImplicitly && item.RedefinesTarget is { } area)
                foreach (var (record, other) in new[] { (area, item), (item, area) })
                    if (FirstStrongItemIn(record) is { } strong && implicitlyRedefined.Add(strong))
                    {
                        using var _ = Edition.At(strong);
                        string by = item.RedefinesKind is RedefinitionKind.SameRecordArea
                            ? "a record-area SAME clause (ISO §12.4.6.4.4 GR2)"
                            : "the FD/SD's multiple level 1 entries (ISO §13.18.33.4 GR3)";
                        Edition.Error("COBOLNET1532", $"'{strong.CobolName ?? strong.CsName}' is a strongly-typed item "
                            + $"and shares its storage area with '{other.CobolName ?? other.CsName}' — an implicit redefinition by {by}: "
                            + "a strongly-typed item shall not be implicitly or explicitly redefined in whole or in "
                            + "part (ISO §13.18.57.3 SR4)");
                    }

        foreach (var item in AllItems())
            if (item.RedefinesKind is RedefinitionKind.Clause && item.RedefinesTarget is { } tgt
                && StrongTypeModel.IsStronglyTyped(tgt)
                && !ReferenceEquals(StrongTypeModel.StrongRoot(item), StrongTypeModel.StrongRoot(tgt)))
            {
                using var _ = Edition.At(item);
                Edition.Error("COBOLNET1532", $"'{item.CobolName ?? item.CsName}' REDEFINES strongly-typed item "
                    + $"'{tgt.CobolName ?? tgt.CsName}': a strongly-typed item shall not be redefined in whole or in "
                    + "part (ISO §13.18.57.3 SR4)");
            }

        foreach (var owner in Roots)
            foreach (var ren66 in owner.Renames66)
                if (ren66.Renames is { } ri
                    && ((ri.From is { } f && StrongTypeModel.IsStronglyTyped(f))
                        || (ri.Thru is { } t && StrongTypeModel.IsStronglyTyped(t))
                        || ri.SpanLeaves.Any(StrongTypeModel.IsStronglyTyped)))
                {
                    using var _ = Edition.At(ren66);
                    Edition.Error("COBOLNET1532", $"RENAMES '{ren66.CobolName ?? ren66.CsName}' renames a "
                        + "strongly-typed item in whole or in part — prohibited (ISO §13.18.57.3 SR3)");
                }
    }

    /// <summary>Bind a level-66 RENAMES entry (ISO §13.18.45): a re-grouping alias <c>RENAMES from [THRU thru]</c>
    /// over a contiguous sibling run of the owning record. It adds no storage (SR2/SR3) — it is attached to the
    /// owning record's <see cref="DataItem.Renames66"/> list (not <see cref="DataItem.Children"/>) and registered for
    /// reference resolution; the FROM/THRU operands are resolved by the post-build pass.
    /// <para>Returns the alias item, or null when the entry is too malformed to build one. The RETURN is what
    /// <c>BindEntries</c> hands a following level-88 entry as "the entry describing the item" (ISO §13.16.3
    /// SR24), so the alias reaches exclusion b) instead of being stepped over (kb/Work PB488).</para></summary>
    private DataItem? BindRenames(Core.DataDescriptionEntryContext entry)
    {
        var rc = entry.dataDescriptionBody().renamesClause();
        if (rc is null || entry.dataName()?.GetText() is not { } name || _lastRoot is null) return null;
        bool thru = rc.THRU() is not null || rc.THROUGH() is not null;
        // data-name-2 / data-name-3 through the ONE screened data-name-n capture: §13.18.45.3 SR7 — "Data-name-2 and
        // data-name-3 shall not be subscripted" — which the bare-word capture used to satisfy by dropping the
        // subscript in silence (kb/Work PB978's sweep).
        var (fromBase, fromQuals) = ClauseDataName(rc.dataReference(0), $"'{name}' RENAMES");
        var (thruBase, thruQuals) = thru && rc.dataReference().Length > 1
            ? ClauseDataName(rc.dataReference(1), $"'{name}' RENAMES … THRU")
            : (null, []);
        var item = new DataItem
        {
            Level = 66,
            DeclaredAt = Edition.Cursor,
            CobolName = name,
            CsName = DataItem.Sanitize(name),
            Renames = new RenamesInfo
            {
                // The base word AND its IN/OF qualifiers (`SUB-GRP-1 OF GRP` — NC252A RENAMES-TEST-2). The
                // qualifiers used to be dropped as "redundant inside the owning record", which they are only
                // when the base word is unique there; with two same-named items in the record they are what
                // §8.4.2.2.3 SR1 requires, and dropping them bound the first-declared (kb/Work PB978).
                FromName = fromBase,
                FromQualifiers = fromQuals,
                ThruName = thruBase,
                ThruQualifiers = thruQuals,
            },
        };
        item.Uid = _uidCounter++;
        item.Parent = _lastRoot;        // owning record — an alias sibling, NOT a storage child
        _lastRoot.Renames66.Add(item);
        DeclareUserWord(name, UserWordKind.DataName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        RegisterName(item);
        return item;
    }

    /// <summary>Bind a level-88 condition-name on its conditional variable <paramref name="parent"/>, capturing the
    /// VALUE set (singletons + THRU ranges) as raw operand text (decoded at emit time).</summary>
    private void BindCondition(Core.DataDescriptionEntryContext entry, DataItem parent, bool registerGlobal = true)
    {
        if (entry.dataName()?.GetText() is not { } name) return;
        var cond = new Condition88 { Name = name, Parent = parent };
        // §13.18.63.3 SR25 — a format 3 VALUE clause under a CONSTANT RECORD entry (DataBinder.ValuePlacement,
        // the ONE placement screen; kb/Work PB551).
        ScreenConditionValuePlacement(name, parent);

        if (entry.dataDescriptionBody().dataDescriptionClauses() is { } clauses)
            foreach (var clause in clauses.dataDescriptionClause())
                if (clause.valueClause() is { } value)
                {
                    // Format 3 prints `{ VALUE IS | VALUES ARE }`; Format 5 detaches the connective entirely
                    // (its [IS/ARE] bracket sits before INVALID/VALID), and it is refused BY NAME at bind
                    // (COBOLNET1708), so screening its head here would be a second diagnostic on one entry.
                    if (value.validateValidPhrase() is null)
                        CheckValueConnective(value, pairedConnective: true, $"condition-name '{name}'");
                    // `[ IN alphabet-name-1 ]`, IN optional (kb/Work PB983) — decided BEFORE the operand loop,
                    // because with IN omitted the parser has handed the word in AS the last operand.
                    var (alphaName, alphaOperand) = RangeAlphabetPhraseOf(value);
                    foreach (var vi in value.valueItem())
                    {
                        // Numeric operands normalize to dot-decimal form (DECIMAL-POINT IS COMMA, ISO §12.3.7 GR14a).
                        if (vi.valueClauseRange() is { } range)
                        {
                            // §13.18.63.3 SR29: THROUGH shall not be specified for a boolean conditional
                            // variable — "If the category of the subject of the entry is boolean, the THROUGH
                            // phrase shall not be specified" (0898). That is the ONLY category THROUGH is
                            // banned for.
                            // ⛔ THERE IS NO LONGER A NATIONAL STAGE HERE (kb/Work PB761, discharged in landing
                            // train 18). A COBOLNET0899 "recognized but not yet implemented (Phase 4a residue)"
                            // stood over a national THROUGH range on the citation §13.18.63 SR31 — but SR31 says
                            // "Alphabet-name-1 may be specified only when the literals specified in the THROUGH
                            // phrase are of class alphanumeric or national", which governs WHEN the
                            // IN alphabet-name-1 phrase may be written and never makes the range
                            // unimplementable. §14.7.8 rule 2 is the rule that actually governs the range —
                            // "When the range of values is defined by alphanumeric or national literals, the
                            // range of values depends on the collating sequence used for evaluation of the
                            // range" — and ConditionRenderer has rendered exactly that since PB678 put
                            // __COLLATE_NAT live. MEASURED with the stage removed, under
                            // ALPHABET REV-NAT FOR NATIONAL IS N"ABC" and its inverted control N"CBA": the
                            // elementary PIC N(3) subject and its GROUP-USAGE NATIONAL twin both answer Y and
                            // both flip to N under the inverted alphabet, so the national program collating
                            // sequence is genuinely consulted. The stage was refusing conforming source the
                            // compiler already implemented. (The IN alphabet-name-1 phrase itself is a separate
                            // construct and is not claimed here.)
                            // ⛔ EVERY guard in this method reads the ONE category reader, DataItem.OperandPic —
                            // an elementary item's own PICTURE, a bit / national GROUP's §13.18.29.4 GR1b/GR2b
                            // as-if PICTURE (1(m) / N(m)) — never raw `Pic`, which is NULL for every group.
                            // §8.8.4.2.1 makes the antecedent real, not hypothetical: "A national group item or a
                            // bit group item shall be treated as an elementary national data item or an
                            // elementary bit data item, respectively", and §13.18.63.3 SR10 (reached from Format 3
                            // by SR24) speaks by name of "the VALUE clause of a bit group item". Reading `Pic`
                            // dropped every one of these screens on a group subject, so an 8-bit literal on a
                            // 4-bit group compiled AND RAN and a national THROUGH range slipped through to codegen
                            // and was then weighed on the ALPHANUMERIC table (kb/Work PB575 + PB728 — the binder
                            // and codegen halves of ONE rule, landed together). An ORDINARY (alphanumeric) group
                            // has no as-if PICTURE either, so it keeps exactly the behaviour it had.
                            if (parent.OperandPic is { Category: PicCategory.Boolean })
                                Edition.Error("COBOLNET0898", $"condition-name '{name}': THROUGH may not be "
                                    + "specified when the conditional variable is boolean (ISO §13.18.63.3 SR29)");
                            // Fold ONCE per operand (a §8.8.3 concat folds here; RawValueOperandText) so the
                            // category check and the stored value see the same text without double diagnostics.
                            // NULL = the operand is not a literal position at all and has been reported
                            // (kb/Work PB732); the range binds nothing rather than storing a raw word.
                            string? rawLo = RawValueOperandText(range.valueClauseOperand(0), $"condition-name '{name}'");
                            string? rawHi = RawValueOperandText(range.valueClauseOperand(1), $"condition-name '{name}'");
                            if (rawLo is null || rawHi is null) continue;
                            // ⛔ NATIONAL IS NOT EXCLUDED FROM THE CATEGORY FUNNEL, and that is half of PB761's
                            // discharge rather than an afterthought: while the stage above refused every national
                            // range, no national range ever reached this screen, so dropping the stage alone
                            // would have opened an UNDER-REJECTION — `88 X VALUE "AAA" THRU "CCC"` on a
                            // PIC N(3) subject, which §13.18.63.3 SR5 requires to be national literals. Only
                            // BOOLEAN stays excluded, because SR29 has already refused the phrase outright above
                            // and a second diagnostic on the same entry would be noise.
                            if (parent.OperandPic is { Category: not PicCategory.Boolean } rp)
                            {
                                // §13.18.63 SR4/SR5/SR24→SR10: the VALUE literals' CATEGORY must match the
                                // conditional variable's — the SAME funnel the item-entry VALUE uses. Their SIZE
                                // sentences do not reach a condition-name subject, which is why the subject is
                                // ValueSubject.ForConditionName() and not the conditional variable's size
                                // (kb/Work PB598 — the derivation is on that factory).
                                var condSubject = ValueSubject.ForConditionName();
                                rawLo = ScreenValueLiteral(rp, rawLo, $"condition-name '{name}'", condSubject);
                                rawHi = ScreenValueLiteral(rp, rawHi, $"condition-name '{name}'", condSubject);
                            }
                            cond.Values.Add((rawLo, rawHi));
                        }
                        else
                            foreach (var op in vi.valueClauseOperand())
                            {
                                if (op == alphaOperand) continue;   // alphabet-name-1, not a literal-2
                                // §13.18.63 SR4/SR5/SR24→SR10 (both directions): an N"…"/B"…" literal seeds
                                // only its own category, and a national/boolean conditional variable takes only
                                // its own literal form — the ONE canonical checker (0898 band). A bit /
                                // national GROUP parent funnels here too, through its §13.18.29.4 GR1b/GR2b as-if
                                // PICTURE (kb/Work PB575 + PB728); an ORDINARY (alphanumeric) group has neither a
                                // PICTURE nor an as-if one and takes no category screen — that leg is §8.8.4.2.1's
                                // "treated as an elementary alphanumeric data item", which any alphanumeric
                                // literal already satisfies.
                                // Fold ONCE (a §8.8.3 concat folds in RawValueOperandText) so the category
                                // check and the stored value share one text without double diagnostics.
                                // NULL = a non-literal operand, already reported (kb/Work PB732).
                                if (RawValueOperandText(op, $"condition-name '{name}'") is not { } raw) continue;
                                if (parent.OperandPic is { } sp)
                                    raw = ScreenValueLiteral(sp, raw, $"condition-name '{name}'",
                                        ValueSubject.ForConditionName());
                                cond.Values.Add((raw, null));
                            }
                    }
                    // `[ IN alphabet-name-1 ]` — printed AFTER the repeated literal group and BEFORE the
                    // `[ WHEN SET TO FALSE … ]` line, so it is resolved here, between the two. ONE alphabet
                    // governs the whole VALUE set (the bracket stands outside the group's ellipsis), and it is
                    // §14.7.8 rule 2's collating sequence for every THRU range in it.
                    // ⛔ IT USED TO BE PARSED AND DROPPED — a SILENT WRONG ANSWER, measured: under
                    // `ALPHABET AL IS "ZYXW…A"`, `88 X VALUE "M" THRU "A" IN AL` answered NO for the value "C"
                    // that AL's ordering puts inside the range, because the range was weighed natively
                    // (kb/Work PB398). The resolution is the SAME one EVALUATE's range-expression takes, because
                    // §14.7.8 opens by governing both clauses in one sentence.
                    // ⛔ THE SCREENS RUN ONLY WHEN THE CLAUSE ACTUALLY WRITES A THROUGH PHRASE, because that is
                    // what SR31 is a rule ABOUT: "Alphabet-name-1 may be specified only when the literals
                    // specified in THE THROUGH PHRASE are of class alphanumeric or national." Over a value list
                    // with no range the phrase governs nothing — the singletons are compared by §8.8.4.5.3 GR2's
                    // ordinary relation rules under the PROGRAM collating sequence — so it is inert, not
                    // erroneous. Keying the screen on the conditional VARIABLE alone instead refused
                    // `88 CN-ORDER VALUE 1 IN AL1 WHEN SET TO FALSE 0`, conforming source this corpus already
                    // pins (tests/conformance/2002/pb695_value_false_optional_words), on a rule that says nothing
                    // about it. The class read is still the variable's, which §13.18.63.3 SR4/SR5 make the
                    // literals' own class.
                    if (alphaName is not null)
                    {
                        if (!cond.Values.Any(v => v.High is not null))
                        {
                            if (IsAlphabetName(alphaName)) GateRangeAlphabetPhrase($"condition-name '{name}'");
                            Edition.Error(DiagnosticCatalog.ValueAlphabetWithoutThrough, $"condition-name "
                                + $"'{name}': alphabet-name '{alphaName}' is written with no THROUGH phrase in "
                                + "the clause, so there are no \"literals specified in the THROUGH phrase\" for "
                                + "it to order (ISO §13.18.63.3 SR31)");
                        }
                        else if (TryResolveRangeAlphabet(alphaName,
                                CollatingSelection.ForComparison(parent.OperandPic?.Category, parent.OperandPic?.Category),
                                $"condition-name '{name}'"))
                            cond.Alphabet = alphaName;
                    }
                    // literal-4 LAST, because the phrase is written last (§13.18.63.2 format 3 prints
                    // `[ WHEN SET TO FALSE IS literal-4 ]` on the line after the operand list), so the
                    // diagnostics a malformed entry produces come out in source order. It is bound AFTER the
                    // alphabet because §13.18.63.3 SR27 b) weighs literal-4 against the ranges in the sequence
                    // §14.7.8 rule 2 gives them, which `cond.Alphabet` is what names.
                    BindFalsePhraseOperand(value, parent, cond);
                }

        parent.Own88s.Add(cond);   // the item owns its 88s (source of truth; lets CloneItem carry a TYPEDEF's 88s)
        if (registerGlobal)
        {
            DeclareUserWord(name, UserWordKind.ConditionName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB65, PB1083)
            if (!Conditions.TryGetValue(name, out var list)) Conditions[name] = list = [];
            list.Add(cond);
        }
    }

    /// <summary>THE <c>[ IN alphabet-name-1 ]</c> phrase of a condition-name VALUE clause (ISO §13.18.63.2 formats
    /// 3 and 5), as (the alphabet-name written, the operand node that spelled it) — or (null, null) when the clause
    /// has none. The operand is non-null only for the IN-OMITTED spelling, which the caller must then skip as a
    /// literal (kb/Work PB983).
    /// <para>⛔ <c>IN</c> IS AN OPTIONAL WORD. Neither format underlines it (PDF p546 / folio 516), so §5.2.3
    /// makes <c>88 MID VALUE "A" THRU "Z" AL</c> conforming. The grammar cannot see that spelling: §13.10.3 SR2
    /// lets a constant-name stand wherever a format writes a literal, so a bare trailing word is by POSITION one
    /// more literal-2, and the operand loop takes it. The SYMBOL decides it — ISO §8.3.2.2 ("a given
    /// user-defined word may be used as only one type of user-defined word"; its exceptions pair constant-names
    /// with data-names, never with alphabet-names), so a LAST operand that is a bare word naming a declared
    /// alphabet IS the phrase, and any other word keeps its literal-position reading (and its COBOLNET1639 when
    /// it names nothing). The EVALUATE range-expression prints the same phrase, and there the grammar CAN decide
    /// it by position (<c>valueRange</c>); <c>TrailingInPhraseDriftTests</c> pins the two together.</para>
    /// <para>A lone operand is never peeled: the clause needs a literal-2 before the phrase.</para>
    /// <para>⛔ AND THE IN SPELLING HAS THE SAME AMBIGUITY THE EVALUATE ARM HAS (kb/Work PB843's other arm): over a
    /// constant-name operand — <c>VALUE "M" THRU K IN AL</c> — the greedy qualification loop takes <c>IN AL</c> as
    /// a qualifier of K, and the clause's own <c>IN</c> is never parsed. A trailing <c>IN word</c> suffix naming a
    /// declared alphabet is the phrase by the same §8.3.2.2 argument; the operand is registered in
    /// <see cref="_alphabetPeeledOperands"/> so every literal-position reader sees K alone.</para></summary>
    private (string? Name, Core.ValueClauseOperandContext? Operand) RangeAlphabetPhraseOf(Core.ValueClauseContext value)
    {
        if (value.IN() is not null) return (value.cobolWord()?.GetText(), null);
        var items = value.valueItem();
        if (items.Length == 0) return (null, null);
        var ops = items[^1].valueClauseOperand();
        var last = items[^1].valueClauseRange() is { } range ? range.valueClauseOperand(1) : ops.LastOrDefault();
        if (last is null || WrittenValueOperandWord(last) is not { } dref) return (null, null);
        // The IN spelling, claimed as a qualifier.
        if (dref.dataReferenceSuffix() is { Length: > 0 } suffixes)
        {
            if (suffixes[^1].qualification() is { } q && q.IN() is not null && q.ChildCount == 2
                && q.cobolWord().GetText() is var qualWord && IsAlphabetName(qualWord))
            {
                _alphabetPeeledOperands[last] = ReferenceResolver.WithoutTrailingSuffix(dref);
                return (qualWord, null);
            }
            return (null, null);
        }
        // The IN-omitted spelling: a bare last operand, after at least one literal-2.
        if (items[^1].valueClauseRange() is not null || (items.Length == 1 && ops.Length == 1)) return (null, null);
        return dref.GetText() is var word && IsAlphabetName(word) ? (word, last) : (null, null);
    }

    /// <summary>Screen <c>[ WHEN SET TO FALSE IS literal-4 ]</c> — Format 3's SECOND literal position (ISO
    /// §13.18.63.2 format 3) — through THE SAME literal-position chokepoint every other VALUE operand takes
    /// (kb/Work PB732).
    /// <para>The grammar used to write this operand as <c>literal</c>, which rejected the two word spellings a
    /// literal position admits: <c>88 CN VALUE 1 WHEN SET TO FALSE IS K</c> with K a constant-name (§13.10.3 SR2
    /// — "constant-name-1 may be used anywhere that a format specifies a literal") and the
    /// symbolic-character-1 figurative (§8.3.3.6.2 format 7) both died COBOL0001 + COBOL0309. Widening the
    /// operand to <c>valueClauseOperand</c> without this screen would have re-opened PB732's sink on literal-4,
    /// so the widening and the screen are ONE change.</para>
    /// <para>The CATEGORY screen is the ALL-FORMATS one, and it reaches literal-4 by its own words: §13.18.63.3
    /// SR2/SR4/SR5 speak of "all literals in the VALUE clause" / "literals in the VALUE clause", not of
    /// literal-1 or literal-2, so literal-4 takes the conditional variable's category exactly as literal-2 does.
    /// The subject is <c>ValueSubject.ForConditionName()</c> for the same reason (kb/Work PB598): those rules'
    /// SIZE sentences name an elementary or group item as the VALUE bearer and do not reach a Format-3
    /// subject.</para>
    /// <para>⛔ AND THE SCREENED VALUE IS NOW STORED (kb/Work PB555). It used to be screened and dropped, on the
    /// reasoning that <c>SET condition-name TO FALSE</c> was unimplemented so "a field here would be a lookup
    /// nothing reads" — but the causality ran the other way: <see cref="Condition88.FalseValue"/> not existing is
    /// WHY §13.18.63.4 GR20 could not be implemented and WHY §13.18.63.3 SR27 had nothing to compare. Binding the
    /// operand is one change; SR27 below and the SET store in <c>SetEmitter</c> are its two consequences.</para></summary>
    private void BindFalsePhraseOperand(Core.ValueClauseContext value, DataItem parent, Condition88 cond)
    {
        if (value.valueClauseFalsePhrase()?.valueClauseOperand() is not { } falseOp) return;
        string where = $"condition-name '{cond.Name}' (WHEN SET TO FALSE)";
        // NULL = not a literal position at all, already reported at the ONE report (kb/Work PB732).
        if (RawValueOperandText(falseOp, where) is not { } raw) return;
        if (parent.OperandPic is { } fp)
            raw = ScreenValueLiteral(fp, raw, where, ValueSubject.ForConditionName());
        cond.FalseValue = raw;
        CheckFalseValueDistinct(cond, parent, where);
    }

    /// <summary>§13.18.63.3 SR27 — literal-4 shall name a value the condition-name is FALSE for: "<i>The value of
    /// literal-4 shall not be equal to the value of any occurrence of literal-2. When the THROUGH phrase is
    /// specified: a) when literal-2 is of a class other than alphanumeric or national, the value of literal-4
    /// shall not be equal to any value in the range of any occurrence of literal-2 through literal-3, inclusive.
    /// b) when literal-2 is of class alphanumeric or national, and the runtime collating sequence is known, the
    /// value of literal-4 shall not be equal to the value of any literal-2 or any value in the range of any
    /// occurrence of literal-2 through literal-3, inclusive.</i>"
    /// <para>The FIRST sentence is unconditional — it carries no class qualifier and no THROUGH precondition — so
    /// it is tested against every occurrence of literal-2, singleton or range bound, before the sub-rules are
    /// reached. a) and b) then add RANGE MEMBERSHIP, and differ only in which ordering decides it: a) is
    /// §14.7.8 rule 1's algebraic order, b) is rule 2's collating sequence. b)'s "<i>any literal-2</i>" clause is
    /// the first sentence restated and needs no second test.</para>
    /// <para>⛔ WHY b) CAN BE ENFORCED AT ALL. The rule is conditioned on the runtime collating sequence being
    /// KNOWN, and SR26's NOTE says when it is not: "<i>The runtime collating sequence is unknown when the
    /// collating sequence is defined by a locale or the collating sequence is otherwise determined at runtime</i>".
    /// This processor fixes every other sequence at compile time — the <c>IN alphabet-name-1</c> phrase's alphabet
    /// (§14.7.8 rule 2) or, absent it, the PROGRAM COLLATING SEQUENCE — so a LOCALE alphabet is the only arm that
    /// drops the screen, and it drops it rather than guessing. The ordering itself is the runtime's own
    /// <c>CobolCollation.Compare</c>, reached through <c>CollatingTable.Collation</c>: one §8.8.4.2.7 comparison,
    /// not a compile-time paraphrase of it.</para>
    /// <para>⚠ An operand whose VALUE is not known at compile time — a figurative constant, an <c>ALL</c> literal,
    /// a symbolic-character — is skipped rather than guessed at: SR27 speaks of "the value of literal-4", and a
    /// screen that invented one would reject conforming source. The figuratives ZERO/ZEROS/ZEROES over a NUMERIC
    /// subject are the exception, because §8.3.3.6.4 GR3 gives them the value zero exactly.</para></summary>
    private void CheckFalseValueDistinct(Condition88 cond, DataItem parent, string where)
    {
        if (cond.FalseValue is not { } raw || cond.Values.Count == 0) return;
        var ord = new ConditionValueOrder(this, cond, parent);
        var v4 = ConditionValueOf(parent, raw);

        foreach (var (lo, hi) in cond.Values)
        {
            var vlo = ConditionValueOf(parent, lo);
            // ⛔ THE SUB-RULES KEY ON LITERAL-2's VALUE, WHICH IS WHAT "the value of" MEANS ON THIS SUBJECT.
            // SR27 a) opens "when literal-2 is of a class other than alphanumeric or national" and b) "when
            // literal-2 is of class alphanumeric or national", per occurrence — so the arm is chosen from the
            // operand in hand, never from the subject's category. On a NUMERIC-EDITED subject the two arms
            // COINCIDE, because SR6/SR7 give BOTH operands a character value (see ConditionValueOf), and that
            // cross-class pair is the hole this screen used to fall through (kb/Work PB920).
            switch (CompareConditionValues(ord, v4, vlo))
            {
                case 0: Report(lo, null); continue;                       // sentence 1
                case null: continue;                                      // no value the compiler can weigh
            }
            if (hi is null) continue;
            // a) / b) — RANGE membership. b) fires only where the runtime collating sequence is KNOWN (SR26's
            // NOTE: a LOCALE sequence is not); a) is §14.7.8 rule 1's algebraic order and is always known. An
            // INVERTED range is EMPTY (§14.7.8 rule 2's last paragraph — it raises EC-RANGE-INVALID and
            // "execution proceeds as if the range of values were empty"), so nothing can be inside it and the
            // test is skipped rather than inverted. ⛔ SR26 is the rule that refuses the inversion ITSELF, and
            // it is NOT implemented (kb/Work PB552) — an owner determination is owed first, because SR26 b)'s
            // "runtime collating sequence is known" is implementor-defined by its own NOTE 5.
            if (vlo.Str is not null && !ord.Known) continue;
            var vhi = ConditionValueOf(parent, hi);
            if (CompareConditionValues(ord, vlo, vhi) <= 0
                && CompareConditionValues(ord, v4, vlo) >= 0
                && CompareConditionValues(ord, v4, vhi) <= 0) Report(lo, hi);
        }

        void Report(string lo, string? hi) => Edition.Error(DiagnosticCatalog.FalseValueNotDistinct,
            $"{where}: the FALSE-phrase literal {raw} is {(hi is null ? $"equal to the VALUE literal {lo}"
                : $"within the VALUE range {lo} THRU {hi}")}, so it is a value for which the condition-name is "
            + "TRUE — literal-4 shall name a FALSE value (ISO §13.18.63.3 SR27)");
    }

    /// <summary>⛔ THE ONE "<i>value of</i>" A FORMAT-3 VALUE-CLAUSE OPERAND, over its conditional variable
    /// (kb/Work PB920) — an algebraic value (<c>Num</c>) or a character value (<c>Str</c>), never both, and
    /// neither when the operand names no value this compiler can weigh. §13.18.63.3 SR27 is written in terms
    /// of it, and so is SR26 (kb/Work PB552, not yet implemented — see the COBOLNET2176/2177 family comment
    /// in DiagnosticCatalog), which is why it is ONE reader and not a decoder per screen.
    /// <para>⛔ ON A NUMERIC-EDITED SUBJECT BOTH SPELLINGS DENOTE THE SAME KIND OF VALUE, and missing that was a
    /// measured WRONG ANSWER: <c>01 X PIC ZZ9.99. 88 X-TEN VALUE " 10.00" WHEN SET TO FALSE IS 10.</c> compiled
    /// clean and <c>SET X-TEN TO FALSE</c> left the condition TRUE, because a screen that compared only same-kind
    /// pairs could not see that the two literals name one value. SR6 converts a numeric literal "<i>to their
    /// numeric-edited forms according to the rules for the MOVE statement</i>"; SR7 takes an alphanumeric or
    /// national literal as the edited form the programmer wrote. §8.8.4.2.1's NOTE states the consequence this
    /// screen must predict — "<i>All comparisons involving numeric-edited data items are alphanumeric or
    /// national comparisons, including when the associated VALUE clause is a numeric literal</i>" — and
    /// §8.8.4.5.3 GR2 makes the condition-name test one of those comparisons. So the edited image IS the value,
    /// and it is composed by the ONE SR6 composer the VALUE initializer and the membership test already share
    /// (<c>ValueInitializer.EditedImageOfNumericValue</c>; <c>ConditionValueRecipeDriftTests</c> names its
    /// readers). A null from it — a PICTURE format-2 (LOCALE) subject, whose image exists only at run time, or
    /// a figurative ZERO below 2023 — falls back to the literal's own value rather than guessing.</para>
    /// </summary>
    private (decimal? Num, string? Str) ConditionValueOf(DataItem parent, string raw)
    {
        if (parent.OperandPic is { Category: PicCategory.NumericEdited } npic
            && CobolNet.CodeGen.ValueInitializer.EditedImageOfNumericValue(
                   Edition.DialectLevel, DecimalPointIsComma, parent, npic, raw) is { } edited)
            return (null, edited);
        return (NumericLiteralValue(raw), StringValue(raw));
    }

    /// <summary>Order two operand VALUES: negative / zero / positive as the first is less than, equal to or
    /// greater than the second, and NULL when either names no value the compiler can weigh or the two are not
    /// comparable at all. A character pair is ordered by §14.7.8 rule 2's sequence
    /// (<see cref="ConditionValueOrder"/>), an algebraic pair by rule 1's algebraic order.
    /// <para>A MIXED pair reaching here means the entry already violates the category funnel (§13.18.63.3
    /// SR2/SR4/SR5/SR10, reported by <c>ValidateValueCategory</c>) on a subject whose two spellings have no common
    /// domain — so it answers "unknown" rather than inventing a conversion. The numeric-edited subject, where
    /// they DO have a common domain, is normalized in <see cref="ConditionValueOf"/> and never arrives mixed.
    /// </para></summary>
    private static int? CompareConditionValues(ConditionValueOrder ord,
        (decimal? Num, string? Str) a, (decimal? Num, string? Str) b)
    {
        if (a.Str is { } sa && b.Str is { } sb) return ord.Compare(sa, sb);
        if (a.Num is { } na && b.Num is { } nb) return na.CompareTo(nb);
        return null;
    }

    /// <summary>§14.7.8 rule 2's collating sequence for one condition-name's VALUE set — the <c>IN
    /// alphabet-name-1</c> phrase's alphabet, or the PROGRAM COLLATING SEQUENCE — resolved AT MOST ONCE and
    /// only when a character comparison actually asks for it, because building the carrier materializes a weight
    /// block and most VALUE sets never reach one. <see cref="Known"/> is SR26's NOTE ("<i>The runtime collating
    /// sequence is unknown when the collating sequence is defined by a locale or the collating sequence is
    /// otherwise determined at runtime</i>"), the precondition SR26 b) and SR27 b) share.</summary>
    private sealed class ConditionValueOrder(DataBinder owner, Condition88 cond, DataItem parent)
    {
        private CobolNet.Runtime.CobolCollation? _order;
        private bool _resolved, _known = true;

        /// <summary>§8.8.4.2.7 (alphanumeric) and §8.8.4.2.9 (national) both extend the shorter operand on
        /// the RIGHT with spaces; §8.8.4.2.8 extends a BOOLEAN one with zeros. THROUGH cannot reach a boolean
        /// subject (SR29), but SR27's sentence-1 equality test can, so the boolean pad is named rather than
        /// assumed away.</summary>
        private char Pad { get; } = parent.OperandPic?.Category is PicCategory.Boolean ? '0' : ' ';

        public bool Known { get { Resolve(); return _known; } }

        /// <summary>The comparison is asked THROUGH the sequence for EQUALITY too, and deliberately:
        /// §8.8.4.5.3 GR2 tests the conditional variable against its values by the ordinary relation rules,
        /// which §8.8.4.2.7 evaluates in the collating sequence in effect — so a literal-4 that COLLATES
        /// equal to a literal-2 is a value for which the condition-name is TRUE, which is exactly what SR27
        /// forbids. A null carrier is the native sequence, for which the padded ordinal compare IS the ordering.
        /// </summary>
        public int Compare(string a, string b)
        {
            Resolve();
            return _order is { } c
                ? CobolNet.Runtime.CobolString.Compare(a, b, c)
                : CobolNet.Runtime.CobolString.Compare(a, b, Pad);
        }

        private void Resolve()
        {
            if (_resolved) return;
            _resolved = true;
            (_order, _known) = owner.RangeOrdering(cond.Alphabet,
                parent.OperandPic?.Category is PicCategory.National);
        }
    }

    /// <summary>⛔ THE ONE SCREEN ON THE VALUE CLAUSE'S LEADING WORDS (kb/Work PB559) — §13.18.63.2 prints
    /// them, so the rule is about the FORMAT, and the format is a fact only the CALLER has: Format 1 and Format 3
    /// are the same token sequence and differ by the entry's level-number, Format 4 by the section. That is why
    /// this takes <paramref name="pairedConnective"/> rather than re-deriving the format, and why the grammar
    /// keeps the superset alternative — a parse rule cannot see a level-number.
    /// <para>Format 1 (data-item) prints "<c>VALUE IS literal-1</c>": VALUE underlined, IS not, and NEITHER
    /// VALUES NOR ARE appears. Formats 2, 3 and 4 print a two-line required choice, "<c>VALUE IS</c>" over
    /// "<c>VALUES ARE</c>", and §5.2.6.3 makes a brace choice exactly one of its alternatives — so VALUE pairs
    /// with IS and VALUES with ARE, and the cross-product spellings are printed by nothing. §13.18.63.3 SR17
    /// ("<i>The words VALUE and VALUES are equivalent</i>") is what admits VALUES at all, and it is a FORMAT 2
    /// rule that Format 3 reaches through SR24 and Format 4 through SR34; Format 1 applies neither.</para>
    /// <para>§13.18.63.3 SR39 states the identical pairing for Format 5, the one format that detaches the
    /// connective — which is the standard confirming, for the format where it had to be said in words, exactly
    /// what the other four say with a brace.</para></summary>
    /// <param name="pairedConnective">True for Formats 2, 3 and 4 (the <c>VALUE IS</c> / <c>VALUES ARE</c>
    /// choice); false for Format 1, which prints only <c>VALUE</c> and only <c>IS</c>.</param>
    private void CheckValueConnective(Core.ValueClauseContext value, bool pairedConnective, string where)
    {
        bool values = value.VALUES() is not null, are = value.ARE() is not null;
        string? written = !pairedConnective && values
                ? "VALUES" + (are ? " ARE" : value.IS() is not null ? " IS" : "")
            : !pairedConnective && are ? "VALUE ARE"
            : values && value.IS() is not null ? "VALUES IS"
            : !values && are ? "VALUE ARE"
            : null;
        if (written is null) return;
        Edition.Error(DiagnosticCatalog.ValueConnectiveNotPrinted, $"{where}: the VALUE clause is written "
            + $"'{written}', which no general format of the clause prints — "
            + (pairedConnective
                ? "the leading braces are a required choice between 'VALUE IS' and 'VALUES ARE', so VALUE pairs "
                  + "with IS and VALUES with ARE (ISO §13.18.63.2 formats 2/3/4, §5.2.6.3)"
                : "format 1 (data-item) prints 'VALUE IS literal-1', and §13.18.63.3 SR17's VALUE/VALUES "
                  + "equivalence is a format 2 rule that format 1 does not apply (ISO §13.18.63.2 format 1)"));
    }

    /// <summary>The compile-time CHARACTER value of a VALUE-clause operand, or null when the operand is not a
    /// literal whose characters are known here — a figurative constant, an <c>ALL</c> literal or a
    /// symbolic-character names a value that depends on width or on SPECIAL-NAMES, and §13.18.63.3 SR27 speaks of
    /// "<i>the value of</i>" its operands, so a screen that invented one would reject conforming source.</summary>
    private static string? StringValue(string raw) =>
        CobolLiteral.IsStringLiteral(raw) ? CobolLiteral.Decode(raw) : null;

    /// <summary>The compile-time VALUE of a numeric VALUE-clause operand (ISO §13.18.63.3 SR27's "<i>the value of
    /// literal-4</i>" / "<i>the value of … literal-2</i>"), or null when the operand names no value the compiler
    /// can weigh. The binder has already normalized a numeric operand to dot-decimal form (§12.3.7 GR14a), so the
    /// parse is invariant-culture; the figurative ZERO family is admitted because §8.3.3.6.4 GR3 gives it the
    /// value zero exactly, and every other figurative (SPACE, HIGH-VALUE, …) is a CHARACTER value that a numeric
    /// subject could not have taken in the first place (§13.18.63.3 SR2).</summary>
    private static decimal? NumericLiteralValue(string raw)
    {
        string w = raw.Trim();
        // THE one §8.3.3.6.2 operand classifier (kb/Work PB461) — the private strip here fired only before a
        // SPACE, and the parse tree glues the words, so `VALUE ALL ZEROS` arrived as "ALLZEROS" and this reader
        // answered "no value the compiler can weigh" for a constant whose value §8.3.3.6.4 GR4 states exactly.
        if (CobolNet.CodeGen.FigurativeConstants.Classify(w).Kind is { } k) return k == 'Z' ? 0m : null;
        return decimal.TryParse(w, System.Globalization.NumberStyles.AllowDecimalPoint
                | System.Globalization.NumberStyles.AllowLeadingSign,
            System.Globalization.CultureInfo.InvariantCulture, out decimal v) ? v : null;
    }

    /// <summary>The ordering a level-88 THROUGH range is evaluated in (ISO §14.7.8 rule 2) AS THE COMPILER KNOWS
    /// IT: the <c>IN alphabet-name-1</c> alphabet when the clause writes one, else the PROGRAM COLLATING SEQUENCE
    /// (§12.3.6), else the native order. <c>Known</c> is false exactly for §13.18.63.3 SR26 NOTE's unknowable arm
    /// — a LOCALE sequence, whose ordering is the locale current at each use (§12.3.7.4 GR7e) — and its consumers
    /// then decline to enforce a rule the standard conditions on knowing it.
    /// <para>A null <c>Order</c> with <c>Known</c> true is the NATIVE (identity) sequence: no carrier exists for
    /// it, and the caller's two-argument <c>CobolString.Compare</c> is exactly its ordering.</para></summary>
    private (CobolNet.Runtime.CobolCollation? Order, bool Known) RangeOrdering(string? alphabetName, bool national)
    {
        if (alphabetName is not null)
        {
            if (national && NationalAlphabets.TryGetValue(alphabetName, out var nd))
                return nd.Locale is not null ? (null, false) : (nd.Table?.Collation(true), true);
            if (!national && Alphabets.TryGetValue(alphabetName, out var ad))
                return ad.Locale is not null ? (null, false) : (ad.Table?.Collation(false), true);
            return (null, true);       // unresolved: TryResolveRangeAlphabet already reported it; native order
        }
        if (national)
            return NationalCollating is { } pn
                ? pn.Locale is not null ? (null, false) : (pn.Table?.Collation(true), true)
                : (null, true);
        return Collating is { } pa
            ? pa.Locale is not null ? (null, false) : (pa.Table?.Collation(false), true)
            : (null, true);
    }

    /// <summary>Make <paramref name="name"/> unique within a C# name scope, appending <c>_2</c>, <c>_3</c>, … on collision.</summary>
    private static string Unique(string name, IEnumerable<string> used)
    {
        var set = used as ICollection<string> ?? used.ToList();
        if (!set.Contains(name)) return name;
        for (int n = 2; ; n++)
        {
            string candidate = $"{name}_{n}";
            if (!set.Contains(candidate)) return candidate;
        }
    }

    /// <summary>Bind one data-description entry (skips level-66 RENAMES and level-88 condition names for now).</summary>
    /// <summary>A clause's source spelling with its words SEPARATED. <c>ctx.GetText()</c> concatenates the
    /// tokens ("FORMATCHARACTERDATA"), which is unreadable in a diagnostic whose whole job is to quote back
    /// what the user wrote — one join, at the three declined-clause sites that quote a multi-word clause
    /// (FORMAT, SELECT WHEN, RECORD DELIMITER).</summary>
    private static string Spelled(Antlr4.Runtime.Tree.IParseTree node) =>
        string.Join(' ', Enumerable.Range(0, node.ChildCount).Select(i => node.GetChild(i).GetText()));

    /// <summary>A construct's text AS WRITTEN — the source char-stream slice, spacing intact (the
    /// <c>IntrinsicBinder.ReparseArgs</c> precedent) — for a diagnostic that quotes back a DEEP node, where
    /// <see cref="Spelled"/>'s direct-children join still glues (<c>FUNCTION LENGTH(X)</c> has ONE child).
    /// Falls back to the glued spelling when the node carries no input stream (a synthesized token).</summary>
    private static string AsWritten(Antlr4.Runtime.ParserRuleContext node) =>
        node.Start?.InputStream is { } chars && node.Stop is { } stop
            ? chars.GetText(new Antlr4.Runtime.Misc.Interval(node.Start.StartIndex, stop.StopIndex))
            : Spelled(node);

    private DataItem? BindEntry(Core.DataDescriptionEntryContext entry, EntrySection section)
    {
        DataDescriptionCst e = entry;
        if (e.Level is not { } level) return null;
        if (level is 66 or 88) return null; // RENAMES / condition-names: later slice.

        string? cobolName = e.Name;
        bool isFiller = cobolName is null || cobolName.Equals("FILLER", StringComparison.OrdinalIgnoreCase);
        string csName = isFiller ? $"_filler{_fillerCounter++}" : DataItem.Sanitize(cobolName!);

        string? pictureText = null, usageText = null, rawValue = null, redefinesTargetName = null;
        var groupUsage = GroupUsage.None;   // GROUP-USAGE (§13.18.29; D20/PB79)
        List<EditingPhraseSpec>? editingSpecs = null;   // PICTURE EDITING phrases (§13.18.40.2), threaded to PictureAnalyzer
        LocaleEditSpec? pictureLocale = null;           // PICTURE format 2 — the LOCALE phrase (§13.18.40.2; PB64 T6)
        List<TableValueSpec>? tableValues = null;       // Format 2 (table) VALUE phrases (§13.18.63.2)
        bool valueClauseWritten = false;                 // §13.16.3 SR10 asks whether the clause was WRITTEN
        bool gluedMultiLiteral = false;                 // a Format-1 VALUE with >1 operand (no FROM) — the glued-list reject
        // USAGE OBJECT REFERENCE, as WRITTEN — the four independent axes §13.18.60.2 prints, kept raw here and
        // adjudicated into an ObjectRefDescriptor below (OoBindObjectRefDescriptor), where the class table and
        // the entry's section are both in hand. kb/Work PB389.
        Core.ObjectReferenceUsageContext? objectRefUsage = null;
        int? occurs = null;
        OccursSpec? occursSpec = null;
        var indexNames = new List<string>();
        SignSpec? ownSign = null;
        bool justified = false, blankWhenZero = false, synchronized = false;
        bool binaryUnsigned = false;   // USAGE BINARY-CHAR/... UNSIGNED (SIGNED is the default, ISO §13.18.60.4 GR12)
        bool noSign = false;           // USAGE PACKED-DECIMAL WITH NO SIGN (ISO §13.18.60.4 GR11 — no sign nibble; 2023)
        // The entry's USAGE clause node, kept for the float FORMAT phrases (ISO §13.18.60.2 general format, 2014):
        // their applicability depends on the RESOLVED usage, so the whole phrase adjudication runs in one block
        // after entryUsage exists. kb/Work PB174.
        Core.UsageClauseContext? usageCtx = null;
        // §13.18.60.3 SR21's subject is the SET of usage clauses in the entry, not the last one to win an
        // overwrite (kb/Work PB544). Split in two so the overwhelmingly common ONE-clause entry allocates
        // nothing: BindEntry runs per data item, thousands of times per compile.
        Core.UsageClauseContext? firstUsage = null;
        List<Core.UsageClauseContext>? extraUsages = null;   // non-null only once a SECOND clause is written
        bool isBased = false;          // BASED (ISO §13.18.5 — a storage template; Phase-4b increment 2)
        PropertyClauseSpec? property = null;   // PROPERTY (ISO §13.18.42; kb/Work PB1273)
        bool isAnyLength = false;      // ANY LENGTH (ISO §13.18.2 — a runtime-length LINKAGE formal; PHASE-09 Step 11)
        bool isDynamicLength = false;  // DYNAMIC LENGTH (ISO §8.5.1.10 / §13.18.19 — a variable-length min-0 string; P12 wave 2)
        Int128? dynLengthLimit = null; // integer-1 of the LIMIT phrase (§13.18.19.4 GR2); null = the phrase is absent
        string dynLengthLimitText = "";// integer-1 AS WRITTEN, for the COBOLNET2027 report
        string? dynLengthStructureName = null;   // the optional dynamic-length-structure-name-1 (§13.18.19.2)
        DynamicLengthStructure? dynStructure = null;   // … and the SPECIAL-NAMES declaration it resolves to (SR2)
        bool hasExternal = false;      // observed for the BASED×EXTERNAL SR (the clause itself binds later)
        Core.ExternalizedNamePhraseContext? externalAs = null;   // the EXTERNAL clause's `AS literal-1` (§13.18.22.2; kb/Work PB511)
        bool isTypedef = false, typedefStrong = false;   // TYPEDEF [STRONG] — a type declaration (ISO §13.18.58; D17)
        bool isConstantRecord = false; // CONSTANT RECORD (ISO §13.18.15 — a structured constant; P10 Step 15)
        string? typeRefName = null;    // TYPE IS type-name — the type this entry clones, expanded post-build (D17)
        string? sameAsName = null;     // SAME AS data-name-1 — the entry this one copies, expanded post-build (§13.18.49)
        IReadOnlyList<string> sameAsQuals = [];   // the SAME AS target's OF/IN qualifiers
        bool isAligned = false;        // ALIGNED (ISO §13.18.1 — bit alignment at the first bit of the first available byte)

        // The §13.16.2 Format-1 clause slots this entry WRITES (kb/Work PB487). ⛔ THE operand of every §13.16.3
        // permitted-set rule below — SR12, SR13, SR17, SR18 are all "…in the same data description entry…" rules
        // over the WHOLE clause list, and each used to be an `||` chain over whichever local decode flags the
        // author remembered. Read from the CST rather than reconstructed from those flags, because a syntax rule
        // asks what was SPECIFIED and several of the flags below are CLEARED by their own recovery paths.
        DataClauseKind written = e.WrittenClauses;

        // The dataDescriptionClauses presence guard folds into e.Clauses (empty when the body has no clause list).
            foreach (var clause in e.Clauses)
            {
                if (clause.PictureText is { } picText)
                {
                    pictureText = picText;
                    editingSpecs = BuildEditingSpecs(clause.Context.pictureClause(),   // PICTURE EDITING (§13.18.40.2)
                        $"data item '{cobolName ?? "FILLER"}'");
                    // PICTURE Format 2 (locale) — `LOCALE [IS locale-name-1] SIZE IS integer-1` (§13.18.40.2; LIVE
                    // since kb/Work PB64 T6): capture the locale (SR37 through the ONE undeclared-locale-name path,
                    // COBOLNET1664; absent ⇒ the current locale at each edit, §13.18.40.5 r11) and SIZE integer-1
                    // (§13.18.40.4 GR17 — the item's length); the analysis is AnalyzeLocaleEdited's (SR33–SR36 +
                    // Table 11 → COBOLNET1673).
                    if (clause.Context.pictureClause()?.pictureLocalePhrase() is { } lp)
                    {
                        var localeName = lp.cobolWord();   // locale-name-1 when written; LOCALE itself is the formatWord (kb/Work PB764)
                        var locale = LocaleRef.Current;
                        if (localeName is not null)
                        {
                            var sym = ResolveLocaleName(localeName.GetText(),
                                $"data item '{cobolName ?? "FILLER"}' PICTURE … LOCALE {localeName.GetText()}",
                                "ISO §13.18.40.3 SR37 — locale-name-1 shall be specified in the LOCALE clause in the SPECIAL-NAMES paragraph");
                            if (sym is not null) locale = new LocaleRef(sym);
                        }
                        // A ZERO integer-1 is ISO §5.5 1)'s, reported by the one integer-n screen
                        // (Validation/IntegerOperandPass, COBOLNET2386 — kb/Work PB859); only a value too large for
                        // any character count is this clause's own to report.
                        bool zero = lp.integerLiteral().GetText().AsSpan().Trim('0').IsEmpty;
                        int size = CobolNet.Validation.IntegerOperandRules.HostValue(lp.integerLiteral());
                        if (size == 0 && !zero)
                            Edition.Error(DiagnosticCatalog.PictureLocaleFormat2Violation,
                                $"data item '{cobolName ?? "FILLER"}': SIZE IS {lp.integerLiteral().GetText()} — "
                                + "integer-1 gives the item's character positions and shall be a nonzero unsigned "
                                + "integer (ISO §13.18.40.2 / §13.18.40.4 GR17)");
                        pictureLocale = new LocaleEditSpec(locale, Math.Max(1, size), "");
                    }
                }
                else if (clause.Context.basedClause() is not null)
                    // BASED (§13.18.5) validated below (§13.16.3 SR16 placement; the 0881 declaration band). The
                    // COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitBasedClause (14g.2,
                    // recognition-based — IsBased is cleared for a LINKAGE item, so a bound-arm gate would drop it).
                    isBased = true;
                else if (clause.Context.anyLengthClause() is not null)
                    // ANY LENGTH (§13.18.2) — SR1 + the §13.16.3 SR17 clause-exclusion validated below; the
                    // COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitAnyLengthClause
                    // (recognition-based — IsAnyLength is cleared on every SR violation, so a bound-arm gate
                    // would drop the 0900 on exactly those paths; the BASED pattern).
                    isAnyLength = true;
                else if (clause.Context.dynamicLengthClause() is { } dl)
                {
                    // DYNAMIC LENGTH (§8.5.1.10 / §13.18.19) — SR1, SR2, SR4 and the §13.16.3 SR18 clause-exclusion
                    // are validated below (the ANY LENGTH pattern). The COBOL-2014
                    // introduction gate is VersionConformancePass ParseArm.VisitDynamicLengthClause (recognition —
                    // IsDynamicLength is cleared on every SR violation, so a bound-arm gate would drop the 0900).
                    isDynamicLength = true;
                    dynLengthStructureName = dl.cobolWord()?.GetText();
                    // ⛔ integer-1 is read WIDE and bounded LATER, by CobolDynString.MaxSizeOf: §8.5.1.10.1 makes
                    // the item's maximum size "the smallest of" this phrase and the implementor maximum, so a
                    // literal past `int` is not "unparseable", it is simply larger than the maximum. Narrowing it
                    // HERE is what let such a LIMIT fall through to -1 — "no bound at all" — and take the same
                    // wrapping path as an item with no LIMIT phrase (kb/Work PB463). A literal too wide even for
                    // Int128 is larger still; Int128.MaxValue carries it to the same clamp.
                    if (dl.integerLiteral() is { } lim)
                    {
                        dynLengthLimitText = lim.GetText();
                        dynLengthLimit = Int128.TryParse(dynLengthLimitText, out Int128 lv) ? lv : Int128.MaxValue;
                    }
                }
                else if (clause.Context.externalClause() is { } extc)
                {
                    hasExternal = true;   // consumed by CallBindExternalAndGlobal; flagged here for the 0881 check
                    // §13.18.22.2's `[ AS literal-1 ]` (kb/Work PB511). CAPTURED here, SCREENED below where
                    // entryWhere exists — the message has to be able to name the subject.
                    externalAs = extc.externalizedNamePhrase();
                }
                else if (clause.Context.globalClause() is not null)
                    { /* §13.18.27 — binds post-build in CallBindExternalAndGlobal; the §13.16.3 co-clause rules
                         read it off `written`, so no decode flag is needed here (kb/Work PB487). */ }
                else if (clause.Context.propertyClause() is { } pcx)
                    // §13.18.42 (OO; kb/Work PB1273) — recorded ON THE ITEM, so every later rule asks the entry that
                    // carries the clause (the OO pass used to find its subject by FIRST NAME MATCH). §13.18.42.3 SR1
                    // is screened below, where the entry's name is in hand for the message.
                    property = new PropertyClauseSpec(
                        NoGet: pcx.NO() is not null && pcx.GET() is not null,
                        NoSet: pcx.NO() is not null && pcx.SET() is not null,
                        IsFinal: pcx.FINAL() is not null);
                else if (clause.Context.typedefClause() is { } td)
                    // §13.18.58; D17. The COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitTypedefClause
                    // (14g.2, recognition-based — the typedef item is discarded from ConformanceForest when it fails to
                    // register (unnamed/duplicate) or binds into method scope, so a bound-arm gate would drop it; DEVLOG 734).
                    { isTypedef = true; typedefStrong = td.STRONG() is not null; }
                else if (clause.TypeRefName is { } trn)
                    // TYPE IS type-name — cloned in ExpandTypes (D17). The COBOL-2002 introduction gate is
                    // VersionConformancePass ParseArm.VisitTypeClause (14g.2, recognition-based — TypeRefName is
                    // nulled by ExpandTypes during bind, so a bound-arm gate would drop it).
                    typeRefName = trn;
                else if (clause.SameAsTargetName is { } san)
                    // SAME AS data-name-1 (ISO §13.18.49; P10 Step 16) — the subject copies data-name-1's
                    // description via the ONE ExpandTypes/CloneItem machinery (ExpandSameAs, GR1/GR2). The
                    // COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitSameAsClause
                    // (recognition-based — SameAsName is nulled by ExpandSameAs during bind, the TypeRefName
                    // pattern).
                    { sameAsName = san; sameAsQuals = clause.SameAsQualifiers; }
                else if (clause.Context.justifiedClause() is not null)
                    justified = true;   // JUSTIFIED [RIGHT] (ISO §13.18.32 — right-justify alphanumeric receives)
                else if (clause.Context.blankWhenZeroClause() is not null)
                    blankWhenZero = true;   // BLANK [WHEN] ZERO (ISO §13.18.8 — a zero value stores all spaces)
                else if (clause.Context.syncClause() is not null)
                    synchronized = true;   // SYNCHRONIZED/SYNC (ISO §13.18.55) — no-op here; gated on a GROUP <2023 (step 10)
                else if (clause.Context.constantRecordClause() is not null)
                    // CONSTANT RECORD (§13.18.15) — a structured constant; the §13.16.3 SR3/SR6/SR13 same-entry
                    // shape checks run below (the IsBased discipline). The COBOL-2002 introduction gate is
                    // VersionConformancePass ParseArm.VisitConstantRecordClause (recognition-based).
                    isConstantRecord = true;
                // PROPERTY clause (§13.18.42, COBOL-2002 OO): superset-parsed at every edition; its OO SEMANTICS bind
                // independently in DataBinder.Oo.OoBindPropertyClauses (which reads the propertyClause node directly),
                // and its COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitPropertyClause (14g.2) —
                // so the storage-clause loop needs no branch for it.
                else if (clause.Context.selectWhenClause() is { } sw)
                    // SELECT WHEN clause (ISO §13.18.51) — Annex A.4.8 item 2), DECLINED (CONFORMANCE.md §5).
                    // Reported HERE, in BindEntry, because BindEntry is the ONE path every data description
                    // entry of every section reaches (BindEntries feeds it for file / working-storage /
                    // local-storage / linkage alike), so the refusal cannot be dodged by moving the record —
                    // and §13.18.51.3 SR1 admits the clause in all four of those sections. Never bound: an
                    // inert SELECT WHEN would silently select the wrong record description entry
                    // (§13.18.51.4 GR1/GR2).
                    Edition.Declined(DiagnosticCatalog.FormatSelectWhenUnclaimed,
                        $"the SELECT WHEN clause on '{cobolName ?? "FILLER"}' ({Spelled(sw)})");
                else if (clause.Context.alignedClause() is not null)
                    // ALIGNED (ISO §13.18.1; kb/Work PB487). The COBOL-2002 introduction gate is
                    // VersionConformancePass ParseArm.VisitAlignedClause; §13.18.1.3 SR1 (a bit group item or an
                    // elementary bit data item only) is checked below, where the usage and GROUP-USAGE of THIS
                    // entry are both decoded; §13.18.1.4 GR1/GR2 are honoured at the ONE bit-layout site,
                    // Binding/Model/BitLayout.cs.
                    isAligned = true;
                // ⛔ THE CLOSED §13.16.2 FORMAT-1 CLAUSE LIST has NO arm here (kb/Work PB487, generalized by
                // kb/Work PB829). `unrecognizedClause` is an ERROR PRODUCTION, not a clause, and it is refused —
                // by name, COBOLNET1941 — from `Validation/ClosedFormatPass.cs`, the ONE place every closed
                // general format's residue is reported. It moved out of this loop because BindEntry is NOT
                // reached for every written entry: the level-66 and level-88 paths return before it, so an
                // unrecognized word on `88 CN WIBBLE VALUE 1.` was dropped in silence here (MEASURED on the pre-change
                // build: only COBOLNET1747 fired, and WIBBLE vanished). A parse-tree walk
                // sees the written syntax whether or not anything binds it (the DeclinedFacilityPass argument).
                // The `DataClauseKind.Unrecognized` BIT is still read below: an entry whose only defect is an
                // unrecognized word draws COBOLNET1941 alone, and one that ALSO violates a §13.16.3 permitted-set
                // rule draws both.
                else if (clause.Context.groupUsageClause() is { } gu)
                    // GROUP-USAGE (ISO §13.18.29; D20/PB79). The COBOL-2002 introduction gate is VersionConformancePass
                    // ParseArm.VisitGroupUsageClause; SR1 (a group, not strongly typed, not variable-length) and the
                    // subordinate conformance (SR2/SR3) are adjudicated once the forest is complete (ResolveIndexItems);
                    // the no-explicit-USAGE half of SR2/SR3 is checked below, where both clauses are known.
                    groupUsage = gu.BIT() is not null ? GroupUsage.Bit : GroupUsage.National;
                else if (clause.Context.usageClause() is { } usage)
                {
                    // ⛔ EVERY usage clause of the entry is KEPT, not just the last one (kb/Work PB544). The
                    // §13.16.2 Format 1 general format prints `[ usage-clause ]` ONCE, so a repeated USAGE
                    // clause is non-conforming — and the rule that says so BY NAME for one of them is
                    // §13.18.60.3 SR21, "If MESSAGE-TAG is specified, no other usage clauses shall be
                    // specified in the data description entry", screened below. A single overwritten
                    // `usageText` could not express that rule at all: its subject is the SET of clauses.
                    if (firstUsage is null) firstUsage = usage;
                    else (extraUsages ??= []).Add(usage);
                    usageText = UsageKeyword(usage);
                    // SIGNED (default) / UNSIGNED on a fixed-width binary usage (ISO §13.18.60.4 GR12) — the
                    // binarySign sibling is a direct child of usageClause in BOTH the full (USAGE IS
                    // BINARY-CHAR SIGNED) and the bare (BINARY-CHAR SIGNED) alternatives.
                    binaryUnsigned = usage.binarySign()?.UNSIGNED() is not null;
                    noSign = usage.noSignPhrase() is not null;   // §13.18.60.4 GR11 — validated against usage/picture below
                    // The item's OWN float format phrases (ISO §13.18.60.2, 2014) are adjudicated in ONE block
                    // below, where entryUsage is resolved (their applicability is per-usage) — the clause node is
                    // captured here rather than four derived locals, so §13.18.60.2's phrase rules stay in one
                    // place. kb/Work PB174.
                    usageCtx = usage;
                    // USAGE OBJECT REFERENCE — the WHOLE §13.18.60.2 phrase (kind × FACTORY × ONLY × name),
                    // carried raw to OoBindObjectRefDescriptor below. FACTORY OF used to be loud-staged here
                    // (COBOLNET0899) and the item left UNTYPED, which produced a SECOND misleading diagnostic at
                    // every later statement mentioning it; it is implemented now (kb/Work PB389).
                    objectRefUsage = usage.usageKeyword()?.objectReferenceUsage();
                }
                else if (clause.RedefinesTargetName is { } redefTarget)
                    // Capture the target name only; resolution waits until the forest is built (the target is a
                    // prior sibling, but a chain A REDEFINES B REDEFINES C resolves in the post-build pass).
                    redefinesTargetName = redefTarget;
                else if (clause.Context.valueClause() is { } value)
                {
                    valueClauseWritten = true;
                    string valueWhere = $"data item '{cobolName ?? "FILLER"}'";
                    // ⛔ §13.18.63.3 SR33 — "Formats 3 and 5 may be specified only when the level-number of the
                    // subject of the entry is 88." This arm binds only non-88 entries (BindEntry returns early
                    // for 66 and 88), so a format-3/5-only phrase HERE is nonconforming source, and it must not
                    // reach the Format-1 raw-value channel: the parse glues a `valueItem` that IS a
                    // `valueClauseRange`, so `VALUE 1 THRU 5` arrived as the text `1THRU5` and failed the Roslyn
                    // compilation, while `VALUE "A" THRU "C"` stored that glued text truncated to the picture
                    // width. Reported and BOUND AS NOTHING — a syntax-rule violation is a compile-time reject,
                    // never a backend failure and never a stored value (kb/Work PB556).
                    if (ConditionOnlyPhraseOf(value) is { } only)
                        Edition.Error(DiagnosticCatalog.ValueFormatRequiresLevel88, $"{valueWhere}: {only} "
                            + "belongs to the VALUE clause's condition-name / content-validation formats, which "
                            + $"may be specified only on a level-88 entry; this entry is level {level} "
                            + "(ISO §13.18.63.3 SR33)");
                    else if (value.valueClauseTablePhrase() is { Length: > 0 } tphrases)
                    {
                        CheckValueConnective(value, pairedConnective: true, valueWhere);
                        tableValues = BuildTableValueSpecs(tphrases, valueWhere);   // Format 2 (table) — §13.18.63.2
                    }
                    else
                    {
                        // Format 1 (data-item), the ONLY format that prints neither VALUES nor ARE. The report
                        // section's Format-4 list has its own binder (DataBinder.Reports), so this arm is
                        // Format 1 outright.
                        CheckValueConnective(value, pairedConnective: false, valueWhere);
                        rawValue = ExtractValue(value, valueWhere);
                        // Format 1 takes EXACTLY ONE literal (§13.18.63.2); a bare multi-literal list (no FROM) is
                        // Format 2 or (report section) Format 4, never a Format-1 data item — ExtractValue GLUES it
                        // today (GetText over the collapsed valueItem). Flag it for a loud reject once entryWhere exists.
                        var vi0 = value.valueItem().FirstOrDefault();
                        if (vi0?.valueClauseRange() is null && (vi0?.valueClauseOperand().Length ?? 0) > 1)
                            gluedMultiLiteral = true;
                    }
                }
                else if (clause.Context.signClause() is { } sign)
                    ownSign = new SignSpec(sign.LEADING() is not null, sign.SEPARATE() is not null);
                else if (clause.Context.occursClause() is { } occ)
                {
                    // Allocate at the table's MAXIMUM occurrence count — the last fixed bound (integer-2 for a
                    // Format-2 `n TO m` table, the sole bound for a fixed table) — per ISO §8.5.1.8 (physical
                    // capacity fixed at compile time). Each bound is an integer literal or an integer
                    // constant-name (§13.10.3 SR2) — OccursBoundValue resolves both (DataBinder.Constants.cs).
                    // The min/DEPENDING/KEY surface is captured in the OccursSpec.
                    string occWhere = $"data item '{cobolName ?? "FILLER"}'";
                    if (occ.occursBound() is { Length: > 0 } bnds && OccursBoundValue(bnds[^1], occWhere) is { } n)
                        occurs = n;
                    occursSpec = OdoBindOccursSpec(occ, occWhere, occurs);
                    if (occ.INDEXED() is not null)
                        foreach (var idxName in clause.IndexNames)
                            indexNames.Add(idxName);
                }
            }

        string entryWhere = $"data item '{cobolName ?? "FILLER"}'";
        CheckMessageTagExclusivity(firstUsage, extraUsages, entryWhere);

        // ⛔ THE CLAUSE-PLACEMENT SCREEN'S ENTRY-LOCAL ROWS (DataBinder.ClausePlacement.cs; kb/Work PB518/PB519):
        // EXTERNAL / GLOBAL residence (§13.16.3 SR6, §13.18.22.3 SR1, §13.18.27.3 SR1 b)), the data-name format
        // (§13.16.3 SR7) and EXTERNAL×REDEFINES/BASED (§13.16.3 SR5). A refused clause is DROPPED here, so it never
        // reaches HasExternalClause / HasGlobalClause — the only inputs of CallBindExternalAndGlobal's re-basing
        // and global-name registration.
        DataClauseKind placementRefused = ScreenClausePlacement(written, level, isFiller, section, entryWhere);
        if ((placementRefused & DataClauseKind.External) != 0) { hasExternal = false; externalAs = null; }
        if ((placementRefused & DataClauseKind.Property) != 0) property = null;   // §13.16.3 SR21 — no accessor from a refused clause
        // §13.18.42.3 SR1 — "The PROPERTY clause may be specified only in the working-storage section of a factory
        // definition or an instance definition" (kb/Work PB1273). Not a table row: the rule's axis is the SOURCE
        // ELEMENT (object or factory, never a program, function or METHOD), which the placement table's section bits
        // cannot state. Until this arm the clause was silently INERT everywhere but object/factory storage — the OO
        // pass never looked anywhere else. The clause stays recorded (the compile has failed), so the elementary
        // rule still names a group subject too.
        if (property is not null
            && !(OoIsClassUnit && _bindingMethodScope is null && section is EntrySection.WorkingStorage))
            Edition.Error(DiagnosticCatalog.PropertyClauseRule, $"{entryWhere}: the PROPERTY clause is written in "
                + (!OoIsClassUnit ? "a source element that is not a class definition"
                    : _bindingMethodScope is not null ? "a method definition"
                    : $"the {SectionWords(section)} of a class definition")
                + "; the PROPERTY clause may be specified only in the working-storage section of a factory "
                + "definition or an instance definition (ISO §13.18.42.3 SR1)");
        bool hasGlobal = (written & ~placementRefused & DataClauseKind.Global) != 0;

        // Parse the usage keyword ONCE per entry — ParseUsage carries the W2 loud-guard gates (the 2002+
        // skeleton usages error, ISO §13.18.60), and a re-parse would duplicate their diagnostics.
        Usage entryUsage = PictureAnalyzer.ParseUsage(usageText, Edition, entryWhere);

        // THE GLUED-MULTI-LITERAL REJECT (ISO §13.18.63.2): a Format-1 (data-item) VALUE takes exactly one literal;
        // a list needs Format 2's FROM (subscript) phrase. The greedy operand loop silently glued a bare list into
        // one corrupt value for the life of the tree — reject it loud, binding nothing.
        if (gluedMultiLiteral)
        {
            Edition.Error("COBOLNET1585", $"{entryWhere}: a data-item VALUE clause (Format 1) takes exactly one "
                + "literal; a list of literals requires the Format 2 table form's FROM (subscript) phrase "
                + "(ISO §13.18.63.2)");
            rawValue = null;
        }

        // An integer constant-name may specify repetition in a PICTURE character-string (ISO §13.10.3 SR2 second
        // sentence → §13.18.40): expand `PIC X(K)` to `PIC X(5)` BEFORE Analyze reads the string. Guarded on the
        // unit actually defining constants, so a constant-free program's PICTURE pipeline is untouched.
        if (pictureText is not null && _constants.Count > 0)
            pictureText = ExpandPicConstants(pictureText, entryWhere);
        // The WRITTEN character-string, kept for the screens that cannot run until the forest is complete
        // (§13.18.60.4 GR1's inherited usage — DataItem.PictureText). Captured HERE, before the §13.16.3 SR8
        // recovery below clears `pictureText`.
        string? writtenPicture = pictureText;

        // A PICTURE-less USAGE INDEX entry is an ELEMENTARY index data item (ISO §13.18.60 — class index, no
        // PICTURE allowed), not a group: synthesize its profile so it emits as a long occurrence-number field.
        // A PICTURE-less SKELETON usage (BINARY-CHAR / POINTER / FLOAT-x / NATIONAL / BIT —
        // legally picture-less per §13.18.60) gets the RECOVERY shape: the compile has already failed, and a
        // Pic-null elementary item NREs the doomed emit on any MOVE receiver (the binary_usage crash,
        // DEVLOG 597) instead of surfacing the 0899/0900. Group headers shed it in ResolveIndexItems.
        // USAGE OBJECT REFERENCE (LIVE — the Phase-3 OO spine): a PICTURE-less elementary reference item
        // (§13.18.60.4; the IndexItem synthesis pattern). PICTURE is prohibited with it — reject loud, never
        // let Analyze classify an incoherent picture-with-reference shape (the W2 silent-misbind rule).
        // ── ⛔ THE §13.16.3 SR8 PICTURE-LESS SCREEN — ONE SET, ONE SITE. "The PICTURE clause shall not be
        // specified … for an item whose usage is binary-char, binary-short, binary-long, binary-double,
        // float-short, float-long, float-extended, index, message-tag, object reference, pointer,
        // function-pointer, or program-pointer": the usage ALONE fixes such an item's representation, so a
        // PICTURE beside it is a violation and Analyze must never classify the incoherent pair (the W2
        // silent-misbind rule). Report loud, then bind the item from its usage.
        //
        // The SET is <see cref="UsageFamilies.IsPictureless"/> — the SAME predicate UsageInheritancePass screens
        // an INHERITED usage against (§13.18.60.4 GR1 makes a group's clause the elementary item's clause), so
        // the two spellings of one item cannot diverge. It replaced FIVE hand-written or-chains here, and
        // USAGE INDEX was the arm they were missing: `05 A PIC 9(4) USAGE INDEX.` silently DROPPED the picture
        // and bound an 8-byte index item (kb/Work PB495). Each family keeps the diagnostic code it has always
        // carried, and the message names the family the programmer actually wrote.
        if (pictureText is not null && UsageFamilies.IsPictureless(entryUsage))
        {
            (string sr8Code, string sr8Text) = Sr8PicturelessVerdict(entryUsage);
            Edition.Error(sr8Code, $"{entryWhere}: {sr8Text}");
            pictureText = null;
        }
        // The §13.18.60.2 description, adjudicated ONCE: which alternative was written, whether the name
        // resolves to a class or an interface — in the REFERRING SOURCE ELEMENT's scope (§8.4.6.4), through
        // the one OoNameResolution funnel, kb/Work PB365 — whether FACTORY/ONLY are admissible on that
        // alternative, and §13.18.60.3 SR16's placement of ACTIVE-CLASS. Every consumer downstream ASKS this
        // descriptor.
        var objectRefDesc = entryUsage is Usage.ObjectReference
            ? OoBindObjectRefDescriptor(objectRefUsage, entryWhere, section)
            : ObjectRefDescriptor.Universal;

        // A VALUE clause is prohibited on EVERY usage whose subject admits none —
        // UsageFamilies.AdmitsNoValueLiteral (PicInfo.cs), THE ONE SET — §13.16.3 SR10's classes index,
        // message-tag, object and pointer, of which §13.18.63.3 SR9 restates four by usage. (Their PICTURE
        // prohibition is the §13.16.3 SR8 screen above, over the ONE picture-less set — the same shape, one file
        // over.) The usage an elementary item ACQUIRES from its group (§13.18.60.4 GR1) is screened against the
        // same set by UsageInheritanceElementary, with the same diagnostic (kb/Work PB515).
        //
        // ⛔ THIS WAS A FOUR-ARM RULE WITH TWO ARMS WRITTEN (kb/Work PB557), and the two that were written are
        // exactly the two that already had a diagnostic band. Which arm did I fix? BOTH MISSING ONES, and the
        // sibling one level out: `01 O USAGE OBJECT REFERENCE VALUE "X".` compiled, linked and RAN CLEAN with the
        // literal silently discarded, while `VALUE NULL` on an object reference or on a plain POINTER reached the
        // code generator and failed the whole compilation (CS0029, 'string' to CobolObject / ManagedPointer) — a
        // compiler crash where a syntax rule asks for a diagnostic. Writing the SET rather than a longer `or`
        // chain is what makes the next pointer-class usage screened on the day it is added.
        //
        // ⛔ THE `TO {function|program}-prototype-name-1` PHRASE IS ONE RULE OVER TWO CARRIERS (kb/Work PB817),
        // so it binds ONCE here: §13.18.60.4 GR25 ("If program-prototype-name-1 is specified, this data item is a
        // restricted program-pointer") and GR26 ("A function-pointer shall contain only the predefined address
        // NULL or the address of a function with the same signature as that identified by the specified
        // function-prototype-name-1") differ only in which namespace the name is resolved in. The SCOPE rules are
        // the same sentence twice as well — §8.4.6.6 for function-prototype-names ("either the user-function-name
        // of the containing function definition or a function-prototype-name declared in the REPOSITORY
        // paragraph") and §8.4.6.8 for program-prototype-names — so the SELF leg and the REPOSITORY leg are one
        // lookup parameterized by the carrier. The OPTIONALITY is the only real difference, and it is measured on
        // the PRINTED general format (folio 503): FUNCTION-POINTER's TO phrase carries NO brackets while
        // PROGRAM-POINTER's does, so every function-pointer is restricted and a bare one is nonconforming.
        bool isFunctionPointer = entryUsage is Usage.FunctionPointer;
        // §13.16.3 SR10 is a rule about the SUBJECT — "shall not be specified" — so it keys on the clause having been
        // WRITTEN, not on a value having been read: an operand the literal-position screen refused (`VALUE NULL`,
        // COBOLNET2576 — kb/Work PB1427) leaves no value, and the subject's own violation is reported as well.
        if (UsageFamilies.AdmitsNoValueLiteral(entryUsage) && valueClauseWritten)
        {
            Edition.Error(DiagnosticCatalog.ValueOnNonLiteralUsage, $"{entryWhere}: the VALUE clause shall not be "
                + $"specified with USAGE {UsageFamilies.UsageWord(entryUsage)} ({UsageFamilies.NoValueClauseRule(entryUsage)})");
            rawValue = null;
            tableValues = null;
        }

        // The §13.18.60.2 general format's THREE `TO` phrases — `POINTER [TO type-name-1]`,
        // `PROGRAM-POINTER [TO program-prototype-name-1]`, `FUNCTION-POINTER TO function-prototype-name-1` —
        // are READ AND SCREENED HERE, once, off the ONE captured usage-clause node. Splitting them is how the
        // pair below drifted: SR18 was enforced and its own next sentence SR19 was not, twenty lines apart.
        //
        // ⛔ OPTIONALITY IS MEASURED, NOT ASSUMED (printed folio 503, rendered at 300 dpi): POINTER's and
        // PROGRAM-POINTER's operands are BRACKETED and FUNCTION-POINTER's is NOT, so every function-pointer is
        // restricted to a prototype and a bare `USAGE FUNCTION-POINTER.` is nonconforming — which is also why
        // §13.18.60.4 GR26's signature invariant always has a prototype to name. kb/Work PB452/PB817.
        var usageKeywordCtx = usageCtx?.usageKeyword();
        var dpu = usageKeywordCtx?.dataPointerUsage();
        var ppu = usageKeywordCtx?.programPointerUsage();
        var fpu = usageKeywordCtx?.functionPointerUsage();
        // The OPERAND's presence decides, never the TO token's: TO is an optional word in all three phrases (not
        // underlined on folio 503, §5.2.3 — kb/Work PB848), so `USAGE POINTER T` restricts exactly as
        // `USAGE POINTER TO T` does.
        string? restrictedTypeName = dpu?.cobolWord()?.GetText();
        // ONE field for BOTH prototype carriers (kb/Work PB817: §14.9.39.3 SR20 and SR22 are one same-signature
        // rule over two carriers; the CATEGORY says which namespace resolves the name).
        string? restrictedPrototypeName =
            fpu?.cobolWord()?.GetText() ?? ppu?.cobolWord()?.GetText();

        // (1) FUNCTION-POINTER's TO phrase is REQUIRED — the general format prints it unbracketed.
        if (isFunctionPointer && restrictedPrototypeName is null)
            Edition.Error(DiagnosticCatalog.PrototypePointerRestriction, $"{entryWhere}: USAGE FUNCTION-POINTER "
                + "shall be followed by [TO] function-prototype-name — the operand is not bracketed in the general "
                + "format (ISO §13.18.60.2), and §13.18.60.4 GR26 takes every function-pointer's signature "
                + "restriction from it");

        // (2) §13.18.60.3 SR18 and SR19 — ONE sentence written twice, so ONE screen: "If {type-name-1 |
        // program-prototype-name-1} is specified, the TYPEDEF clause shall be specified for the subject of the
        // entry." A restricted data-pointer or a restricted program-pointer is therefore declared as a TYPE
        // DECLARATION and referenced by a TYPE clause; `01 P USAGE POINTER TO T.` and
        // `01 PP USAGE PROGRAM-POINTER TO PROTO.` are both nonconforming as written. The 0881 declaration band.
        // ⛔ THERE IS NO FUNCTION-POINTER TWIN — measured: §13.18.60.3 stops at SR21 and carries no
        // function-prototype-name sentence, so `01 FP USAGE FUNCTION-POINTER TO FP-PROTO.` at level 1 is
        // conforming exactly as SR14 allows. Enforcing a third arm here would reject legal source.
        // kb/Work PB153 (SR18) + PB609 (SR19, the sibling this screen had missing — the note's own fix shape:
        // "enforce SR19 at the entry (a named diagnostic, not the 0899 loud)").
        if (!isTypedef)
        {
            if (restrictedTypeName is not null)
            {
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: USAGE POINTER TO {restrictedTypeName} declares a "
                    + "RESTRICTED data-pointer, and the TYPEDEF clause shall be specified for the subject of such an "
                    + "entry (ISO §13.18.60.3 SR18) — declare the restricted pointer as a type declaration and "
                    + "reference it with a TYPE clause");
                restrictedTypeName = null;
            }
            if (restrictedPrototypeName is not null && !isFunctionPointer)
            {
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: USAGE PROGRAM-POINTER TO "
                    + $"{restrictedPrototypeName} declares a RESTRICTED program-pointer, and the TYPEDEF clause "
                    + "shall be specified for the subject of such an entry (ISO §13.18.60.3 SR19) — declare the "
                    + "restricted pointer as a type declaration and reference it with a TYPE clause");
                restrictedPrototypeName = null;
            }
        }

        // (3) The prototype name's SCOPE — §8.4.6.6 for a function-prototype-name ("either the user-function-name
        // of the containing function definition or a function-prototype-name declared in the REPOSITORY
        // paragraph") and §8.4.6.8 for a program-prototype-name, which is the same sentence over the other
        // namespace. A name in neither leg names no signature, so the GR25/GR26 restriction would be
        // unverifiable — and a restriction nothing can check is the a_dead_lookup_is_also_unverified shape.
        if (restrictedPrototypeName is not null
            && !(string.Equals(UnitSelfName, restrictedPrototypeName, StringComparison.OrdinalIgnoreCase)
                 && UnitIsFunction == isFunctionPointer)
            && !(isFunctionPointer
                 ? UserFunctionNames.Contains(restrictedPrototypeName)
                 : ProgramSpecifiers.ContainsKey(restrictedPrototypeName)))
        {
            Edition.Error(DiagnosticCatalog.PrototypePointerRestriction, $"{entryWhere}: USAGE "
                + $"{(isFunctionPointer ? "FUNCTION" : "PROGRAM")}-POINTER TO {restrictedPrototypeName} — "
                + $"'{restrictedPrototypeName}' is not a {(isFunctionPointer ? "function" : "program")}-prototype-name "
                + $"in this source element's scope (ISO {(isFunctionPointer ? "§8.4.6.6" : "§8.4.6.8")}: it shall be "
                + $"either the {(isFunctionPointer ? "user-function-name of the containing function definition" : "program-name of a containing program definition")} "
                + "or a name declared in the REPOSITORY paragraph)");
            restrictedPrototypeName = null;
        }

        // ⛔ THE USAGE CLAUSE'S OWN FLOAT FORMAT PHRASES (ISO §13.18.60.2 general format, a COBOL-2014 addition;
        // kb/Work PB174). ONE block owns all three of the general format's rules over them, because the grammar
        // parses `floatFormatPhrase*` after ANY usageKeyword (the established binarySign/noSignPhrase superset
        // posture) and the narrowing is the binder's:
        //   • REPEAT — §5.2.6.4, "any single alternative may be specified only once" (COBOLNET1718). The
        //     FLOAT-DECIMAL-16/-34 tail is a BRACKETED CHOICE-INDICATOR group over { encoding-phrase,
        //     endianness-phrase }: zero or more, each at most once, in ANY order. The FLOAT-BINARY-* tail is a
        //     plain single bracket, so at most one endianness-phrase there too.
        //   • ENDIANNESS APPLICABILITY — the figure prints the endianness-phrase only on the five STANDARD float
        //     usages (COBOLNET1716); §13.18.60.4 GR19c/d scope the OPTIONS-implied phrase the same way, and
        //     GR13/GR21 leave the implementor-defined float usages (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) to
        //     our documented big-endian pin (Annex A.1 item 48).
        //   • ENCODING APPLICABILITY — the figure prints the encoding-phrase only on FLOAT-DECIMAL-16/-34
        //     (COBOLNET1717); GR20a says the same in prose ("any standard decimal floating-point usage").
        // The RESULT — the item's own endianness — then feeds PicInfo.FloatItem below as the phrase that WINS over
        // the OPTIONS-supplied implied one, which is not a preference but §11.9.8.3 SR2/SR3's own wording: the
        // clause is implied "for … any data item described with a standard binary floating-point usage IN WHICH AN
        // ENDIANNESS-PHRASE IS NOT SPECIFIED". §11.9.9.3 SR1-SR6 say it for the decimal usages.
        // A phrase on FLOAT-BINARY-128 / FLOAT-DECIMAL-16 / -34 PARSES and is applicable — those usages are
        // documented non-support (COBOLNET1564, Annex A.3 items 17/19), so the reject a user sees names the
        // non-support, never a raw parse error at the wrong stage.
        FloatEndianness itemEndianness = FloatEndianness.Unspecified;
        FloatEncoding itemEncoding = FloatEncoding.Unspecified;
        if (usageCtx is not null)
        {
            foreach (var phrase in usageCtx.floatFormatPhrase())
            {
                if (phrase.endiannessPhrase() is { } ep)
                {
                    if (itemEndianness is not FloatEndianness.Unspecified)
                        Edition.Error(DiagnosticCatalog.UsageFloatFormatPhraseRepeated, $"{entryWhere}: the "
                            + "endianness-phrase is specified more than once on this USAGE clause "
                            + "(ISO §5.2.6.4 — any single alternative may be specified only once)");
                    itemEndianness = FloatFormatPhrase.Endianness(ep);
                }
                else if (phrase.encodingPhrase() is { } enc)
                {
                    if (itemEncoding is not FloatEncoding.Unspecified)
                        Edition.Error(DiagnosticCatalog.UsageFloatFormatPhraseRepeated, $"{entryWhere}: the "
                            + "encoding-phrase is specified more than once on this USAGE clause "
                            + "(ISO §5.2.6.4 — any single alternative may be specified only once)");
                    itemEncoding = FloatFormatPhrase.Encoding(enc);
                }
            }
            if (itemEndianness is not FloatEndianness.Unspecified && !UsageFamilies.IsStandardFloat(entryUsage))
            {
                Edition.Error(DiagnosticCatalog.UsageEndiannessPhraseScope, $"{entryWhere}: an endianness-phrase "
                    + "(HIGH-ORDER-LEFT / HIGH-ORDER-RIGHT) is written only with a standard floating-point usage — "
                    + "FLOAT-BINARY-32/-64/-128 or FLOAT-DECIMAL-16/-34 — not USAGE "
                    + $"{usageText ?? "DISPLAY"} (ISO §13.18.60.2 general format; §13.18.60.4 GR19c/d)");
                itemEndianness = FloatEndianness.Unspecified;
            }
            if (itemEncoding is not FloatEncoding.Unspecified
                && !UsageFamilies.IsStandardDecimalFloat(entryUsage))
            {
                Edition.Error(DiagnosticCatalog.UsageEncodingPhraseScope, $"{entryWhere}: an encoding-phrase "
                    + "(BINARY-ENCODING / DECIMAL-ENCODING) is written only with a standard DECIMAL "
                    + "floating-point usage — FLOAT-DECIMAL-16 or FLOAT-DECIMAL-34 — not USAGE "
                    + $"{usageText ?? "DISPLAY"} (ISO §13.18.60.2 general format; §13.18.60.4 GR20a)");
                itemEncoding = FloatEncoding.Unspecified;
            }
            // A SURVIVING encoding-phrase has no consumer, and that is a MEASURED state, not an oversight: the
            // only usages it is applicable to — FLOAT-DECIMAL-16/-34 — are documented non-support (COBOLNET1564,
            // Annex A.3 item 19: .NET has no IEEE decimal64/128 type and GR17/GR18 pin the formats). The phrase
            // is screened here so the reject a user sees names that non-support rather than a parse error, and the
            // value is deliberately NOT carried onto PicInfo: a field nothing reads is a lookup nothing has ever
            // contradicted. It threads through when the usages themselves land.
        }

        var pic = pictureText is not null
            ? PictureAnalyzer.Analyze(pictureText, entryUsage, Edition, entryWhere, ownSign, currencies: CurrencySigns,
                blankWhenZero: blankWhenZero, explicitUsage: usageText is not null, editing: editingSpecs,
                localeFormat2: pictureLocale, decimalPointIsComma: DecimalPointIsComma)
            : entryUsage is Usage.Index ? PicInfo.IndexItem
            : entryUsage is Usage.Pointer ? PicInfo.PointerItem(restrictedTypeName)
            : entryUsage is Usage.ProgramPointer ? PicInfo.ProgramPointerItem(restrictedPrototypeName)   // §13.18.60.4 GR24/GR25 (P10 Step 7; kb/Work PB817)
            // USAGE FUNCTION-POINTER (§13.18.60.4 GR26; kb/Work PB452/PB817) — the ProgramPointer twin. The
            // restriction operand is MANDATORY in the general format, so a null here is the already-diagnosed
            // shape error; the recovery profile keeps the item in its own category rather than letting the
            // errored compile misclassify it as alphanumeric (which is what the PICTURE-less fall-through did).
            : entryUsage is Usage.FunctionPointer ? PicInfo.FunctionPointerItem(restrictedPrototypeName ?? "")
            // USAGE MESSAGE-TAG — declined non-support, already refused BY NAME in ParseUsage (COBOLNET1943;
            // Annex A.3 item 4, docs/CONFORMANCE.md §4 item 1). The recovery shape exists for the same reason
            // the FLOAT-BINARY-128 / FLOAT-DECIMAL forms below have one: §13.16.3 SR8 exempts message-tag from
            // needing a PICTURE, so without it the errored compile would carry a null Pic into the emitter —
            // the unhandled NullReferenceException kb/Work PB487 measured. Never a bind: the compile has failed.
            // ⛔ IT CARRIES THE WRITTEN USAGE (kb/Work PB453): a bare Recovery() said Usage.Display / category
            // alphanumeric, so every later screen answered about a category the item does not have — a SET on
            // such an item drew §14.9.39.3 SR8's object-reference diagnostic and §8.8.1.1's "not a numeric
            // operand", neither of which is true of the program. The usage is a FACT the source states; the
            // category still is not an analysis, which is what IsRecovery goes on saying.
            : entryUsage is Usage.MessageTag ? PicInfo.MessageTagRecovery()
            : entryUsage is Usage.ObjectReference ? PicInfo.ObjectReferenceItem(objectRefDesc)
            : entryUsage is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
                ? PicInfo.BinaryItem(entryUsage, signed: !binaryUnsigned)
            // A PICTURE-less floating-point item (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED + the 2014
            // FLOAT-BINARY-*/FLOAT-DECIMAL-* family, §13.18.60.2, D16) — its value is a native float/double, never
            // scaled-integer (before this the chain fell to null → NRE). The processor-dependent non-support forms
            // (binary128/decimal, rejected COBOLNET1564) still synthesize a Pic so the errored compile does not NRE.
            // The item's effective endianness rides in, resolved by ONE null-coalesce over the two-level
            // derivation §11.9.8.3 SR2/SR3 legislates: the ITEM's own endianness-phrase (§13.18.60.2, captured
            // above) where it stated one, else the unit's OPTIONS FLOAT-BINARY model (§11.9.8 — Options is bound
            // at the top of BindDeclarations, before any entry), whose clause is implied only "for … any data
            // item … IN WHICH AN ENDIANNESS-PHRASE IS NOT SPECIFIED". FloatItem then applies GR19a/b ONCE, for
            // the standard binary float usages only (kb/Work PB164 wave 2 + PB174).
            : entryUsage is Usage.Float or Usage.Double or Usage.FloatShort or Usage.FloatLong or Usage.FloatExtended
                or Usage.FloatBinary32 or Usage.FloatBinary64 or Usage.FloatBinary128
                or Usage.FloatDecimal16 or Usage.FloatDecimal34
                ? PicInfo.FloatItem(entryUsage, itemEndianness is FloatEndianness.Unspecified
                    ? Options.FloatBinaryEndianness : itemEndianness)
            : null;   // incl. a PICTURE-less USAGE NATIONAL/BIT entry — Pending (below) carries its adjudication

        // A PICTURE-less USAGE NATIONAL/BIT entry is a GROUP header (legal — the usage sheds to subordinates,
        // §13.18.60.4 GR1) or an illegal picture-less elementary item (0881) — unknowable until the forest is
        // complete: ResolveIndexItems adjudicates via this EXPLICIT mark (P5.11c; formerly the reference-identity
        // sentinel PicInfos NationalUsagePending/BitUsagePending).
        var pending = pic is null && entryUsage is Usage.National ? PicPending.NationalUsage
            : pic is null && entryUsage is Usage.Bit ? PicPending.BitUsage
            : PicPending.None;
        // GROUP-USAGE (§13.18.29.3 SR2/SR3; D20/PB79): "USAGE BIT / NATIONAL is implied for the subject of the entry.
        // A USAGE clause shall not be explicitly specified for the subject" — the implied usage rides the SAME
        // Pending adjudication a group-level USAGE BIT / NATIONAL clause rides (§13.18.60.4 GR1: the usage sheds to
        // every subordinate leaf, checked and applied in ResolveIndexItems), so the two spellings share one walk.
        if (groupUsage is not GroupUsage.None)
        {
            if (usageText is not null)
                Edition.Error(DiagnosticCatalog.GroupUsageRule, $"{entryWhere}: a USAGE clause shall not be explicitly "
                    + $"specified for the subject of a GROUP-USAGE {(groupUsage is GroupUsage.Bit ? "BIT" : "NATIONAL")} "
                    + "entry — the usage is implied by the GROUP-USAGE clause (ISO §13.18.29.3 "
                    + $"{(groupUsage is GroupUsage.Bit ? "SR2" : "SR3")})");
            else if (pic is not null)
                Edition.Error(DiagnosticCatalog.GroupUsageRule, $"{entryWhere}: the GROUP-USAGE clause may be specified "
                    + "only if the subject of the entry is a group item — this entry has a PICTURE clause "
                    + "(ISO §13.18.29.3 SR1)");
            else
                pending = groupUsage is GroupUsage.Bit ? PicPending.BitUsage : PicPending.NationalUsage;
        }

        // USAGE … WITH NO SIGN — the GENERAL-FORMAT half, and only that half. §13.18.60.2 prints the phrase on
        // exactly one alternative, `PACKED-DECIMAL [ WITH NO SIGN ]` (rendered figure, kb/Work PB570), while the
        // grammar tolerates it after ANY usageKeyword (the established binarySign / floatFormatPhrase superset
        // posture), so the usage word it was written on is screened HERE — where the clause is, and where the word
        // is known whether or not the entry turns out to be a group. COBOLNET1565.
        //
        // ⛔ THE PHRASE'S EFFECT AND ITS §13.18.40.3 SR31 SCREEN ARE NOT HERE. Both are written against the item
        // the phrase APPLIES TO, and §13.18.60.4 GR1 makes that "each elementary item in the group" whenever the
        // clause is written at a group level — which entry bind cannot yet see (the subordinate entries are not
        // parsed, so `pic` is null on a group header). Adjudicated once, for BOTH spellings, in
        // UsageInheritancePass → ApplyEffectiveNoSign; the phrase travels there on DataItem.OwnNoSign. Before
        // PB570 the two arms below ran here on the entry's own `pic` and a group-level phrase fell through all of
        // them in silence: `01 GRP USAGE PACKED-DECIMAL WITH NO SIGN. 05 EG PIC 9(4).` kept the 3-byte SIGNED
        // layout, and `05 W1 PIC S9(4).` under the same header drew no diagnostic at all.
        // The 2023 introduction gate is VersionConformancePass (recognition-based, so it sees both spellings).
        if (noSign && entryUsage is not Usage.Packed)
            Edition.Error("COBOLNET1565", $"{entryWhere}: the WITH NO SIGN phrase (ISO §13.18.60.2; §13.18.60.4 GR11) "
                + $"applies only to USAGE PACKED-DECIMAL, not USAGE {usageText}");

        // Edition gating (the four-compilers rule): a fixed-point picture's digit positions are capped at 18 by
        // COBOL-85 and 31 by 2002+ (ISO §8.3.3.3.2 / §13.18.40) — reject, never silently mis-store.
        // §13.18.40.3 SR14: the 1–31 (18 pre-2002) cap is measured against DIGIT POSITIONS, not just the '9' count —
        // a numeric-edited Z(11)9(8) is 19 positions and Z(35) is 35 (Digits=0), both of which the old '9'-only
        // Digits check let slip past. DigitPositions == Digits for pure-numeric-without-P, so no regression. (CA33.)
        // §13.18.40.3 SR14 reaches category numeric and FIXED-POINT numeric-edited items; the floating-point form's
        // capacity is SR15's 1..36 significand digits, checked by the analyzer (kb/Work PB66 — DigitPositions is 0 there).
        if (pic is { Category: PicCategory.Numeric or PicCategory.NumericEdited, IsFloat: false, IsFloatEdited: false } && pic.DigitPositions > 0)
            Edition.CheckDigitCapacity(pic.DigitPositions, $"data item '{cobolName ?? "FILLER"}' (PICTURE {pictureText})");
        // (§13.18.52.3 SR1/SR2 — the SIGN clause's subject — is CheckSignClauses, post-forest: the float-edited
        // special case that used to stand here is its numeric-edited arm, kb/Work PB537.)
        // §13.16.3 SR19 — the SIGN clause shall not be specified with a format-2 (LOCALE) PICTURE: the sign
        // representation is the LOCALE's (§13.18.40.5 r13). The SCREEN description twin (§13.17.3 SR9) rides the
        // screen arm; a REPORT GROUP entry carries NO such rule (§13.15.3) and the pair is legal there (PB113).
        if (pic is { LocaleEdit: not null } && ownSign is not null)
            Edition.Error(DiagnosticCatalog.SignClauseWithLocalePicture, $"{entryWhere}: the SIGN clause shall not be "
                + "specified when the LOCALE phrase of the PICTURE clause is specified (ISO §13.16.3 SR19)");

        // THE VALUE-CLAUSE LITERAL SCREEN — one funnel, every format (see ScreenValueLiteral). The format-2
        // (table) literals ride the SAME call from ScreenTableValueLiterals, per occurrence (kb/Work PB208), and the
        // SUBJECT the SR4/SR5/SR10 sentences are written against travels with the literal (kb/Work PB206/PB598) —
        // here an ELEMENTARY item, whose size is null for a DYNAMIC LENGTH / ANY LENGTH subject (its PICTURE
        // indicates a class and no size).
        if (rawValue is { } rv && pic is not null)
            rawValue = ScreenValueLiteral(pic, rv, entryWhere,
                ValueSubject.ForElementary(pic, isDynamicLength, isAnyLength, implicitPicture: false));
        // An EXTERNAL type declaration (ISO §13.18.22 SR1 — EXTERNAL is legal on a level-1 type declaration;
        // the level-1 shape is §13.18.58.3 SR3, already enforced by RegisterTypeDecl's 1529). The declaration
        // itself has no storage (§13.18.58.4 GR2); the effect lands on its REFERENCES — §13.18.22 GR2 (a data
        // description containing the type shall be level-1) and GR3 (those records are themselves EXTERNAL) are
        // enforced in ExpandType, and the record rides the ordinary run-unit ExternalStore re-basing
        // (CallBindExternalAndGlobal). The former COBOLNET1534 stage is LIFTED (P10 Step 16).
        // (Its old §13.18.57.4 GR5 citation was wrong — GR5 is a Format-2 REPORT-GROUP rule.)

        // SAME AS same-entry composition (ISO §13.16.3 SR12): the SAME AS clause shall not share a data
        // description entry with any clause except CONSTANT RECORD, entry-name, EXTERNAL, GLOBAL, level-number,
        // and OCCURS. A violation reports and clears the reference so the item binds as ordinary storage under
        // an already-failed compile (the IsBased discipline).
        // ⛔ Over the WRITTEN clause set, not an `||` chain of decode flags (kb/Work PB487). The chain here could
        // not see ALIGNED, DYNAMIC LENGTH, GROUP-USAGE, PROPERTY, SELECT WHEN or the validation clauses, so
        // `01 A SAME AS Z ALIGNED.` compiled clean; the set makes the rule complete BY CONSTRUCTION and names
        // the offending clause instead of restating the permitted list.
        if (sameAsName is not null
            && (written & ~(DataClauseKind.SameAs | DataClauseKinds.SameAsCoPermitted)) is var sameAsBad
            && sameAsBad != DataClauseKind.None)
        {
            Edition.Error(DiagnosticCatalog.SameAsEntryRule, $"{entryWhere}: the SAME AS clause shall not be "
                + "specified in the same data description entry with any clauses except CONSTANT RECORD, "
                + $"entry-name, EXTERNAL, GLOBAL, level-number, and OCCURS (ISO §13.16.3 SR12); this entry also "
                + $"specifies {DataClauseKinds.Name(sameAsBad)}");
            sameAsName = null;
        }

        // CONSTANT RECORD same-entry shape checks (P10 Step 15; the IsBased discipline — a violation reports and
        // clears the flag so the item binds as ordinary storage under an already-failed compile). §13.16.3 SR6:
        // level-01 entries only; SR3: REDEFINES excluded; SR13: ANY LENGTH / BASED / BLANK WHEN ZERO /
        // SYNCHRONIZED / TYPEDEF excluded same-entry (the subordinate-entry half of SR13 checks in BindEntries,
        // where the parent chain exists); SR13 ¶2: with EXTERNAL, a strongly-typed TYPE clause is required.
        if (isConstantRecord)
        {
            string? crViolation =
                level != 1
                    ? "the CONSTANT RECORD clause may be specified only in a data description entry whose "
                      + "level-number is 1 (ISO §13.16.3 SR6)"
                : redefinesTargetName is not null
                    ? "REDEFINES shall not be specified in the same data description entry as the CONSTANT "
                      + "RECORD clause (ISO §13.16.3 SR3)"
                // ⛔ SR13 ¶1 over the WRITTEN clause set (kb/Work PB487): the former `||` chain listed five of the
                // rule's EIGHT excluded clauses — DYNAMIC LENGTH, select-when and the validation clauses were
                // missing, and no amount of care would have kept a hand list complete as the grammar grew.
                : (written & DataClauseKinds.ConstantRecordExcluded) is var crBad && crBad != DataClauseKind.None
                    ? "the ANY LENGTH, BASED, BLANK WHEN ZERO, DYNAMIC LENGTH, select-when, SYNCHRONIZED, and "
                      + "TYPEDEF clauses and validation-clauses shall not be specified in the same data "
                      + "description entry with the CONSTANT RECORD clause (ISO §13.16.3 SR13); this entry also "
                      + $"specifies {DataClauseKinds.Name(crBad)}"
                // VCR 16 (ISO §13.16.3 SR13 ¶2; Annex E.2 item 10): the "EXTERNAL CONSTANT RECORD requires a strong
                // TYPE" requirement is a COBOL-2023 addition — below 2023 the bare external constant record (no TYPE)
                // was the legacy accepted form (its content initializes per §13.18.15.4 GR1 and is not re-initialized
                // on run-unit re-entry, §11.9.10.4 GR7 / §14.6.2.3.3). Version-conditioned structural SR ⇒ read DialectLevel directly (the
                // CheckDigitCapacity/binder-reads-edition doctrine), NOT a ConstructRegistry introduction gate.
                : hasExternal && typeRefName is null && Edition.DialectLevel >= 2023
                    ? "a CONSTANT RECORD clause specified with the EXTERNAL clause requires a TYPE clause "
                      + "naming a strongly typed definition (ISO §13.16.3 SR13 ¶2; ≥2023)"
                : null;
            if (crViolation is not null)
            {
                Edition.Error(DiagnosticCatalog.ConstantRecordRule, $"{entryWhere}: {crViolation}");
                isConstantRecord = false;
            }
            // §13.18.40.3 SR32 (same-entry half): a format 2 PICTURE clause shall not be specified in a data item
            // described with the CONSTANT RECORD clause. Its OWN rule and code (the rule lives in §13.18.40.3,
            // not §13.16.3, so not ConstantRecordRule); the subordinate half checks in BindEntries, where the
            // parent chain exists.
            if (isConstantRecord && pic is { LocaleEdit: not null })
                Edition.Error(DiagnosticCatalog.PictureLocaleFormat2Violation, $"{entryWhere}: a format 2 PICTURE "
                    + "clause shall not be specified in a data item described with the CONSTANT RECORD clause "
                    + "(ISO §13.18.40.3 SR32)");
        }
        // TYPE same-entry composition (ISO §13.16.3 SR14) — SR12's twin, over the same WRITTEN clause set, with
        // the same recovery: report and clear the reference so the item binds as ordinary storage under an
        // already-failed compile (the IsBased discipline).
        // ⛔ THIS IS THE WARRANT FOR CopyEntryDescription's RECEIVER-WINS COPY (kb/Work PB513). That method's
        // `to.Pic ??= from.Pic` is documented as safe because "§13.16.3 SR12 and SR14 list the few clauses
        // admissible in the same entry and no clause copied here is among them" — true of SAME AS, whose SR12
        // guard is above, and FALSE of TYPE until now. With SR14 unenforced the subject's own illegal PICTURE /
        // USAGE / REDEFINES silently DISCARDED the type's declared description: `01 T IS TYPEDEF PIC X(3).
        // 01 A TYPE T PIC 9(5).` gave A a 5-digit numeric description, defeating §8.5.3's whole purpose. One
        // permitted-set constant per rule, so neither arm can drift from the other's warrant.
        // ⛔ PLACED AFTER the CONSTANT RECORD block, not beside SR12's guard: SR13 ¶2 asks whether the entry
        // specifies a TYPE clause, and it must see the one the PROGRAMMER wrote rather than the one this
        // recovery cleared — otherwise one violation reports as two.
        if (typeRefName is not null
            && (written & ~(DataClauseKind.Type | DataClauseKinds.TypeCoPermitted)) is var typeBad
            && typeBad != DataClauseKind.None)
        {
            Edition.Error(DiagnosticCatalog.TypeEntryRule, $"{entryWhere}: the TYPE clause shall not be "
                + "specified in the same data description entry with any clauses except BASED, CLASS, CONSTANT "
                + "RECORD, DEFAULT, DESTINATION, entry-name, EXTERNAL, GLOBAL, INVALID, level-number, OCCURS, "
                + "PRESENT WHEN, PROPERTY, TYPEDEF, VALIDATE-STATUS, VALUE, and VARYING (ISO §13.16.3 SR14); "
                + $"this entry also specifies {DataClauseKinds.Name(typeBad)}");
            typeRefName = null;
        }
        // The EXTERNAL clause's `AS literal-1` (ISO §13.18.22.2; kb/Work PB511) — the SEVENTH restatement of the
        // one externalized-name syntax rule, screened through the ONE shared screen (§13.18.22.3 SR3 reads
        // word-for-word like §11.10.3 SR1 and its four siblings). A violation clears the name rather than
        // failing the entry: §13.18.22.4 GR5's second sentence then externalizes the record under its own
        // data-name, so the program binds under an already-failed compile instead of cascading.
        string? externalizedAs = externalAs is null ? null
            : ExternalizedName.Screen(externalAs.literal(), Edition, DiagnosticCatalog.ExternalClauseAsLiteral,
                $"{entryWhere} IS EXTERNAL AS {externalAs.literal().GetText()}", "literal-1",
                "ISO §13.18.22.3 SR3", LiteralEnv, rejectZeroLength: true);
        var item = new DataItem
        {
            Level = level,
            CobolName = isFiller ? null : cobolName,
            CsName = csName,
            Pic = pic,
            // The profile above came from the USAGE, not from a PICTURE character-string — §13.18.60.4 GR1 gives
            // it to the elementary items under this entry rather than to the entry itself when the entry turns
            // out to be a group, which UsageInheritancePass decides once the forest is complete (PB495).
            PicIsUsageSynthesized = pictureText is null && pic is not null,
            PictureText = writtenPicture,
            Pending = pending,
            OwnSign = ownSign,
            OwnUsage = usageText is not null ? entryUsage : null,
            // The WITH NO SIGN phrase of that clause (§13.18.60.2) rides WITH it — §13.18.60.4 GR1 applies the
            // clause, phrase included, to each elementary item in the group (kb/Work PB570). Kept only when the
            // phrase was written on a usage the general format admits it on: the COBOLNET1565 arm above has
            // already refused the others, and a rejected phrase must not go on to change a width.
            OwnNoSign = noSign && entryUsage is Usage.Packed,
            RawValue = rawValue,
            TableValues = tableValues,
            Occurs = occurs,
            OccursSpec = occursSpec,
            RedefinesTargetName = redefinesTargetName,
            GroupUsage = pic is null ? groupUsage : GroupUsage.None,   // §13.18.29 (D20/PB79); never on an elementary item
            DeclaredAt = Edition.Cursor,   // the entry cursor (kb/Work PB82) — where post-build passes report
            Justified = justified,
            BlankWhenZero = blankWhenZero,
            Synchronized = synchronized,
            IsTypedef = isTypedef,
            TypedefStrong = typedefStrong,
            IsExternalTypedef = isTypedef && hasExternal,   // §13.18.22 SR1 / §13.18.58.3 SR3 (P10 Step 16)
            HasExternalClause = hasExternal,                // the ADMITTED clause — re-basing input + §13.18.22 SR5 pairing check
            HasGlobalClause = hasGlobal,                    // the ADMITTED clause — global-name registration input (PB518)
            ExternalizedAs = externalizedAs,                // §13.18.22.4 GR5's literal-1 (kb/Work PB511)
            IsConstantRecord = isConstantRecord,
            TypeRefName = typeRefName,
            SameAsName = sameAsName,
        };
        if (sameAsName is not null) item.SameAsQualifiers.AddRange(sameAsQuals);
        // §13.18.52.3 SR1/SR2 speak about the entry that WROTE the SIGN clause — screened post-forest by
        // CheckSignClauses (DataBinder.SignClause.cs), where group-ness and the inherited usage are known.
        if (ownSign is not null) _signClauseWritten.Add(item);
        // §13.16.3 SR10 for the ACQUIRED usage (the post-forest arm below) asks the same question the own-usage arm
        // asks — was a VALUE clause written on this entry — so it reads this set, never the value (kb/Work PB1427).
        if (valueClauseWritten) _valueClauseWritten.Add(item);
        // The Format 2 (table) VALUE's ALL-FORMATS literal screen (§13.18.63.3 SR2/SR3 + SR16's pull-in). The
        // clause's GEOMETRY (SR18–SR23) and its §13.18.63.4 GR12–GR16 resolution are the post-forest
        // ResolveTableValues pass — they read the entry's OCCURS ANCESTORS, which do not exist yet here.
        ScreenTableValueLiterals(item, entryWhere);

        // BASED declaration validation (the 0881 declaration-entry band; Phase-4b increment 2): §13.16 SR16 —
        // a BASED entry is a level-01/77 record-description entry (WS/LS/LINKAGE; the file-subsystem sweep is
        // a named residue); §13.18.5 SRs — REDEFINES and BASED are mutually exclusive (:17215) and a VALUE
        // clause cannot seed storage the item does not own. Violations clear the flag so the item binds as
        // ordinary storage under an already-failed compile (never a half-based state).
        if (isBased)
        {
            if (!BasedLevelAdmitted(level))
            {
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: the BASED clause may be specified only in a "
                    + "level-01 or level-77 entry (ISO §13.16.3 SR16 / §13.18.5)");
                isBased = false;
            }
            else if (redefinesTargetName is not null)
            {
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: BASED and REDEFINES may not be specified "
                    + "together (ISO §13.16.3 SR3)");
                isBased = false;
            }
            // BASED × EXTERNAL (§13.16.3 SR5) is the clause-placement table's NotWith row, screened above with
            // its REDEFINES sibling (kb/Work PB519); the refusal drops EXTERNAL, so `hasExternal` is false here
            // and the two bridge mechanisms can never both claim the one BackingCsName (the former CS0102).
            // A VALUE clause on a BASED entry is LEGAL (its data seeds ALLOCATE … INITIALIZED per §14.9.3
            // GR7's TO-VALUE leg); without INITIALIZED the allocated content is undefined (GR8), so the
            // space-filled cell is conformant — the clause simply has no stored field to seed here.
        }
        item.IsBased = isBased;
        item.Property = property;

        // ALIGNED (ISO §13.18.1; kb/Work PB487). Recorded here, ADJUDICATED in CheckAlignedClauses — §13.18.1.3
        // SR1's subject test asks whether this is "a bit group item or an elementary bit data item", and a group
        // becomes a bit group by INHERITANCE too (§13.16.4 GR1), which is not settled until UsageInheritancePass
        // has run. Exactly the placement argument CheckUsageDeclarations records for §13.18.60.3 SR14.
        item.IsAligned = isAligned;

        // ANY LENGTH declaration-shape validation (ISO §13.18.2; the COBOLNET1542 SR band — the 08xx declaration
        // band is exhausted, the 1521 precedent). Violations clear the flag so the item binds as ordinary storage
        // under an already-failed compile (the IsBased discipline). The SR2/SR3/SR4 PLACEMENT rules (linkage-only,
        // elementary, unit kind, formal/RETURNING reference) need unit-level facts and live in the post-bind
        // sweeps: AnyLengthValidateUnit (program/function/object paths) and OoBindMethodData (methods).
        if (isAnyLength)
        {
            // §13.18.2.3 SR1: a PICTURE clause shall be specified, and its character-string shall be ONE
            // instance of the picture symbol 'N', 'X', or '1' — a repetition factor of 1 in any spelling (`X(01)`,
            // a constant-name) is still one instance (§13.18.40.3 SR6). Checked on the WRITTEN character-string —
            // category+length cannot distinguish 'A' — by the ONE predicate DYNAMIC LENGTH SR1 also asks
            // (PictureAnalyzer.IsSingleInstance, kb/Work PB1210).
            if (!PictureAnalyzer.IsSingleInstance(pictureText, "NX1"))
            {
                Edition.Error("COBOLNET1542", $"{entryWhere}: the ANY LENGTH clause requires a PICTURE whose "
                    + "character-string is exactly one instance of the picture symbol 'N', 'X', or '1' "
                    + $"(ISO §13.18.2.3 SR1{(pictureText is null ? "; no PICTURE clause is specified" : $"; PICTURE {pictureText}")})");
                isAnyLength = false;
            }
            // §13.16.3 SR17: with ANY LENGTH the only other clauses permitted are level-number, entry-name,
            // PICTURE, USAGE, and VALUE — reject every other WRITTEN clause loud, never a half-shaped item.
            // ⛔ Over the written clause set (kb/Work PB487). The former `||` chain listed ten decode flags and
            // still could not see ALIGNED, CONSTANT RECORD, DYNAMIC LENGTH, GLOBAL, GROUP-USAGE, PROPERTY,
            // SAME AS, SELECT WHEN or the validation clauses — nine of the fourteen clauses SR17 excludes.
            else if ((written & ~(DataClauseKind.AnyLength | DataClauseKinds.LengthClauseCoPermitted))
                     is var alBad && alBad != DataClauseKind.None)
            {
                Edition.Error("COBOLNET1542", $"{entryWhere}: with the ANY LENGTH clause the only other clauses "
                    + "permitted are level-number, entry-name, PICTURE, USAGE, and VALUE (ISO §13.16.3 SR17); "
                    + $"this entry also specifies {DataClauseKinds.Name(alBad)}");
                isAnyLength = false;
            }
            // §13.18.2.3 SR2 (the level half — checkable right here): an elementary LEVEL 1 entry only
            // (a 77 item is not a level-1 entry; the elementary/section/unit halves are the sweeps' job).
            else if (level is not 1)
            {
                Edition.Error("COBOLNET1542", $"{entryWhere}: the ANY LENGTH clause may be specified only in an "
                    + "elementary level 1 entry in the linkage section (ISO §13.18.2.3 SR2)");
                isAnyLength = false;
            }
        }
        item.IsAnyLength = isAnyLength;

        // DYNAMIC LENGTH declaration-shape validation (ISO §8.5.1.10 / §13.18.19; COBOL-2014). Violations clear the
        // flag so the item binds as ordinary storage under an already-failed compile (the ANY LENGTH / IsBased
        // discipline). The COBOL-2014 introduction gate is VersionConformancePass ParseArm.VisitDynamicLengthClause.
        if (isDynamicLength)
        {
            // §13.18.19.3 SR1: a PICTURE clause shall be specified and its character-string shall be exactly ONE
            // instance of the picture symbol 'N' or 'X'. A count of 1 in ANY spelling is still one instance —
            // `X`, `X(1)`, `X(01)`, `N(001)` all denote one position — asked of the ONE predicate ANY LENGTH SR1
            // also asks (PictureAnalyzer.IsSingleInstance, kb/Work PB1210). Unlike ANY LENGTH (§13.18.2.3 SR1),
            // the boolean symbol '1' is NOT permitted, so a dynamic-length item is alphanumeric or national only ("The
            // picture symbol determines the class", §13.18.19.4 GR1).
            if (!PictureAnalyzer.IsSingleInstance(pictureText, "NX"))
            {
                Edition.Error("COBOLNET1561", $"{entryWhere}: the DYNAMIC LENGTH clause requires a PICTURE whose "
                    + "character-string is exactly one instance of the picture symbol 'N' or 'X' "
                    + $"(ISO §13.18.19.3 SR1{(pictureText is null ? "; no PICTURE clause is specified" : $"; PICTURE {pictureText}")})");
                isDynamicLength = false;
            }
            // §13.18.19.3 SR2: "If dynamic-length-structure-name-1 is specified, it shall correspond to a
            // dynamic-length-structure-name specified in the DYNAMIC LENGTH STRUCTURE clause in the SPECIAL-NAMES
            // paragraph" — this source element's, or a containing one's (§8.4.6.1; InheritConfiguration). The name
            // used to be refused outright as "not yet supported" (COBOLNET1562, retired): the clause it names had no
            // grammar at all (kb/Work PB829).
            else if (dynLengthStructureName is not null
                     && !DynamicLengthStructures.TryGetValue(dynLengthStructureName, out dynStructure))
            {
                Edition.Error(DiagnosticCatalog.DynamicLengthStructureReference, $"{entryWhere}: the DYNAMIC LENGTH "
                    + $"clause names '{dynLengthStructureName}', which is not a dynamic-length-structure-name "
                    + "declared by a DYNAMIC LENGTH STRUCTURE clause in the SPECIAL-NAMES paragraph (ISO §13.18.19.3 SR2)");
                isDynamicLength = false;
            }
            // §13.18.19.3 SR4: "If the LIMIT phrase and dynamic-structure-name-1 are both specified, integer-1 shall
            // not be greater than the maximum length associated with dynamic-length-structure-name-1" — the
            // structure's length-field capacity, bounded by the implementor maximum (DynamicLengthStructure.MaximumLength).
            else if (dynStructure is not null && dynLengthLimit is { } limit4 && limit4 > dynStructure.MaximumLength)
            {
                Edition.Error(DiagnosticCatalog.DynamicLengthStructureReference, $"{entryWhere}: the DYNAMIC LENGTH "
                    + $"clause's LIMIT phrase specifies {dynLengthLimitText} characters, more than the "
                    + $"{dynStructure.MaximumLength} associated with dynamic-length-structure-name '{dynStructure.Name}' "
                    + "(ISO §13.18.19.3 SR4)");
                isDynamicLength = false;
            }
            // §13.16.3 SR18: with DYNAMIC LENGTH the ONLY other clauses permitted are level-number, entry-name,
            // PICTURE, USAGE, and VALUE — reject every other WRITTEN clause loud, never a half-shaped item.
            // ⛔ Over the written clause set (kb/Work PB487), the SAME constant SR17 uses: the two rules state
            // identical permitted lists, and keeping them as two hand-maintained chains is exactly how they
            // drifted apart (SR18's had fifteen flags to SR17's ten, and both were still short of ALIGNED,
            // GROUP-USAGE, SELECT WHEN and the validation clauses). The written set also retires the `hasGlobal`
            // and `hasProperty` locals, which existed ONLY because GLOBAL and PROPERTY bind post-build and so
            // escaped an allowlist keyed on decode flags.
            else if ((written & ~(DataClauseKind.DynamicLength | DataClauseKinds.LengthClauseCoPermitted))
                     is var dlBad && dlBad != DataClauseKind.None)
            {
                Edition.Error("COBOLNET1563", $"{entryWhere}: with the DYNAMIC LENGTH clause the only other clauses "
                    + "permitted are level-number, entry-name, PICTURE, USAGE, and VALUE (ISO §13.16.3 SR18); "
                    + $"this entry also specifies {DataClauseKinds.Name(dlBad)}");
                isDynamicLength = false;
            }
        }
        item.IsDynamicLength = isDynamicLength;
        if (isDynamicLength)
        {
            // §8.5.1.10.1 — the item's MAXIMUM SIZE is "the smallest of" the LIMIT phrase value, the largest
            // integer storable in the PREFIXED usage, and the maximum permitted by the implementor. The PREFIXED
            // candidate is the length field of the dynamic-length-structure-name the clause names, when that
            // structure has a PREFIXED phrase (kb/Work PB829). The rule is written ONCE, in
            // CobolDynString.MaxSizeOf, so the binder, the emitters and the two runtime helpers all see the same
            // real bound — never a "no LIMIT" sentinel (kb/Work PB463).
            if (dynLengthLimit is { } limitAsked && limitAsked > CobolDynString.MaxLength)
                Edition.Warning(DiagnosticCatalog.DynLengthLimitAboveImplementorMaximum,
                    $"{entryWhere}: the DYNAMIC LENGTH clause's LIMIT phrase specifies {dynLengthLimitText} "
                    + $"characters, above this implementation's maximum of {CobolDynString.MaxLength}; the maximum "
                    + $"size of the item is therefore {CobolDynString.MaxLength} characters (ISO §8.5.1.10.1)");
            item.DynMaxSize = CobolDynString.MaxSizeOf(dynLengthLimit, dynStructure?.PrefixedMaximum);
            item.DynStructure = dynStructure;
        }

        // The INDEXED BY index-names ride the entry; each is DECLARED — with its own cell, in the scope being
        // bound — when the item is registered (RegisterIndexes, kb/Work PB919), never here: a TYPEDEF template's
        // entries are never registered, and each of its clones declares its own.
        foreach (var idxName in indexNames)
        {
            DeclareUserWord(idxName, UserWordKind.IndexName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB65, PB1083)
            item.IndexNames.Add(idxName);
        }
        return item;
    }

    /// <summary>ISO §13.18.60.3 syntax rule 21: <i>"If MESSAGE-TAG is specified, no other usage clauses shall
    /// be specified in the data description entry."</i>
    /// <para>⛔ RAISED BEFORE AND INDEPENDENTLY OF THE NON-SUPPORT DECLINE, which is the whole point (kb/Work
    /// PB544). USAGE MESSAGE-TAG is an Annex A.3 item-4 processor-dependent element this compiler declines by
    /// name (<see cref="DiagnosticCatalog.MessageTagUsageUnsupported"/>, COBOLNET1943, raised inside
    /// <c>PictureAnalyzer.ParseUsage</c>), and a declined usage still has SYNTAX RULES over it. Before this
    /// screen the rule had no subject at all: <c>usageText</c> was overwritten by each successive USAGE
    /// clause, so <c>01 M USAGE MESSAGE-TAG USAGE DISPLAY PIC X(3).</c> bound as a plain DISPLAY item and
    /// compiled with NO diagnostic — not even the decline — at every edition.</para>
    /// <para>The rule is one-directional and this screen is too: it fires when MESSAGE-TAG is among the
    /// entry's usage clauses AND there is more than one, whichever order they are written in, because SR21
    /// forbids "any other usage clauses" in the entry rather than any particular pairing.</para>
    /// <para>⚠ A repeated USAGE clause with NO message-tag arm (<c>USAGE DISPLAY USAGE BINARY</c>) is ALSO
    /// non-conforming — §13.16.2 Format 1 prints <c>[ usage-clause ]</c> once — and is still accepted here,
    /// silently, with the last clause winning. That is a WIDER rule about clause repetition in a closed
    /// general format, not §13.18.60.3 SR21, and it is registered as its own mechanism; this list is the
    /// structure it will be enforced from.</para>
    /// <para>⚠ NO EDITION GATE HERE, DELIBERATELY. MESSAGE-TAG is an Annex E.2 item-25 COBOL-2023 addition and
    /// below 2023 the word is an ordinary user-defined word, so the edition-correct diagnostic below 2023 is
    /// the introduction gate rather than this rule. That routing is BROKEN for the whole MESSAGE-TAG surface —
    /// COBOLNET1943 itself fires at 2002 and 2014, contradicting its own message text — and it is one
    /// mechanism with one fix, registered separately. Gating only THIS screen would re-open the silent
    /// acceptance below 2023 while leaving the sibling wrong, so the screen stays edition-blind until the
    /// gate lands, and its negative golden pins only the 2023 cell (feedback green_test_can_hold_a_gap_open:
    /// a golden that pinned 2002 would freeze the defect).</para></summary>
    private void CheckMessageTagExclusivity(
        Core.UsageClauseContext? firstUsage, List<Core.UsageClauseContext>? extraUsages, string entryWhere)
    {
        if (firstUsage is null || extraUsages is null) return;   // fewer than two clauses — nothing to exclude
        List<Core.UsageClauseContext> usageClauses = [firstUsage, .. extraUsages];
        var tag = usageClauses.FirstOrDefault(u => CobolWords.Is(UsageKeyword(u), "MESSAGE-TAG"));
        if (tag is null) return;
        string others = string.Join(", ", usageClauses.Where(u => !ReferenceEquals(u, tag))
                                                      .Select(u => "USAGE " + UsageKeyword(u)));
        using var _ = Edition.At(new DiagnosticCursor(tag.Start.Line, tag.Start.Column + 1));
        Edition.Error(DiagnosticCatalog.MessageTagUsageExclusive,
            $"{entryWhere}: the entry specifies USAGE MESSAGE-TAG and also {others} — \"If MESSAGE-TAG is "
            + "specified, no other usage clauses shall be specified in the data description entry\" "
            + "(ISO §13.18.60.3 SR21). §13.18.60.4 GR9 makes the class and category of a message-tag data "
            + "item message-tag, which no other usage clause can also be true of.");
    }

    /// <summary>Extract a usage clause's canonical keyword text by TOKEN inspection — never string-stripping.
    /// The full form (<c>USAGE [IS] usageKeyword [binarySign]</c>) carries the keyword in its
    /// <c>usageKeyword</c> child; a bare-keyword alternative (the USAGE word is optional, ISO §13.18.60 general
    /// format) carries the keyword TERMINAL as the clause's FIRST child with an optional <c>binarySign</c>
    /// sibling (SIGNED/UNSIGNED on the BINARY-CHAR/-SHORT/-LONG/-DOUBLE family). The historical
    /// <c>GetText()</c>-and-strip fallback GLUED the sign phrase into the keyword (bare <c>BINARY-CHAR
    /// SIGNED</c> → <c>"BINARY-CHARSIGNED"</c>, which then silently misbound to DISPLAY — the W2 loud-guard
    /// sweep), and even bare <c>DISPLAY</c> survived it only by accident. <c>USAGE OBJECT REFERENCE</c> (a rule,
    /// not one terminal) canonicalizes to <c>"OBJECT REFERENCE"</c> — its class-name operand is not part of the
    /// keyword.</summary>
    private static string UsageKeyword(Core.UsageClauseContext usage)
    {
        if (usage.usageKeyword() is { } kw)
            return kw.objectReferenceUsage() is not null ? "OBJECT REFERENCE"
                // The pointer-to-prototype usages are RULES with an optional TO tail (§13.18.60) — GetText()
                // would glue the prototype name into the keyword (the OBJECT REFERENCE precedent).
                : kw.programPointerUsage() is not null ? "PROGRAM-POINTER"
                : kw.functionPointerUsage() is not null ? "FUNCTION-POINTER"
                // USAGE POINTER [TO type-name] (§13.18.60.2) is a RULE for the same reason: the restricted form's
                // type-name would otherwise glue into the keyword ("POINTERT") and reach ParseUsage's
                // internal-error arm. kb/Work PB153.
                : kw.dataPointerUsage() is not null ? "POINTER"
                : kw.GetText();
        // Unreachable since `[USAGE IS] usageKeyword` became ONE alternative (kb/Work PB95): every spelling, bare
        // or prefixed, carries the usageKeyword node.
        return usage.GetChild(0).GetText();
    }

    /// <summary>⛔ THE ONE §13.18.63.3 SR33 PHRASE SET — the VALUE-clause phrases that exist ONLY in format 3
    /// (condition-name) or format 5 (content-validation), named for the diagnostic, or null when the clause
    /// carries none of them. A SET, not three hand-written conditions, because SR33 names FORMATS and the
    /// grammar admits every format through one rule (formats 3 and 5 share their literal / THROUGH list): a
    /// phrase added to that rule tomorrow is screened by adding one line here, which is the difference between
    /// this rule staying true and its next arm being invisible the way these three were.
    /// <para>The phrases, and where the standard puts each: the THROUGH phrase and <c>IN alphabet-name-1</c>
    /// appear in formats 3 and 5 and in no other (§13.18.63.2); <c>WHEN SET TO FALSE IS literal-4</c> is
    /// format 3's alone (§13.18.63.4 GR20 is its only general rule); <c>{IS INVALID | ARE VALID}</c> is format
    /// 5's alone.</para>
    /// <para>⚠ The VALID / INVALID arm cannot be observed today — <c>DeclinedFacilityPass</c> refuses every
    /// A.4.14 content-validation surface by name (COBOLNET1708) in a pass that runs BEFORE the binder, at any
    /// level. It is listed anyway: SR33 governs formats 3 AND 5, and writing only the reachable half is how a
    /// rule's other arm goes missing. It will be the one that answers on the day A.4.14 is claimed.</para></summary>
    private static string? ConditionOnlyPhraseOf(Core.ValueClauseContext value) =>
        value.valueItem().Any(vi => vi.valueClauseRange() is not null) ? "the THROUGH phrase"
        : value.IN() is not null ? "the IN alphabet-name phrase"
        : value.valueClauseFalsePhrase() is not null ? "the WHEN SET TO FALSE phrase"
        : value.validateValidPhrase() is not null ? "the VALID / INVALID phrase"
        : null;

    /// <summary>Extract the first VALUE operand's raw source text (literal or figurative constant). THRU ranges /
    /// 88-levels are later. The emitter (<c>FieldEmitter</c>) interprets the text — including figurative constants
    /// such as ZERO/SPACE — against the item's category and width. A numeric literal is normalized to the
    /// canonical dot-decimal form (DECIMAL-POINT IS COMMA, ISO §12.3.7 GR14a).</summary>
    private string? ExtractValue(Core.ValueClauseContext value, string where)
    {
        var item = value.valueItem().FirstOrDefault();
        if (item is null) return null;
        // ONE operand — the Format 1 / Format 4 literal-1 — goes through the ONE reader, which screens the
        // literal position (§13.18.63.2) and then folds: §8.8.3.3 GR3 a concatenation expression to its
        // equivalent single literal's RAW text (GetText would glue the operand tokens — `"AB" & "CD"` →
        // `"AB"&"CD"` — and the emitter's decode would mis-read the value), §13.10.3 SR2 / §13.10.4 GR1 a
        // constant-name to its literal, §12.3.7.4 GR11 a symbolic-character to its ALL literal. The fold
        // happens HERE, once, so the whole raw-text VALUE pipeline (ValidateValueCategory, FieldEmitter,
        // ValueInitializer) sees an ordinary literal. kb/Work PB732 replaced this method's five-way
        // "does the operand need folding" test — a second copy of the reader's own chain — with the call.
        var ops = item.valueClauseOperand();
        if (ops.Length == 1) return RawValueOperandText(ops[0], where);
        // NOT a Format-1 single literal: a bare multi-literal list (the caller's COBOLNET1585 glued-list
        // reject; the Format-4 report list) or a THRU range. Every operand is still a literal position, so
        // each is screened — a non-literal there was silent until kb/Work PB732 — and the item's raw text is
        // withheld when any operand failed, so no glued spelling reaches the emitter.
        bool ok = true;
        foreach (var op in ops.Concat(item.valueClauseRange()?.valueClauseOperand() ?? []))
            if (!IsLiteralValueOperand(op)) { ReportNonLiteralValueOperand(op, where); ok = false; }
        if (!ok) return null;
        return item.GetText() is { } raw ? NormalizeIfNumericLiteral(raw) : null;
    }

    /// <summary>EVERY operand of a FORMAT-4 (report-section) VALUE clause, in written order — ISO §13.18.63.2
    /// format 4 is <c>{ VALUE IS | VALUES ARE } { literal-1 } …</c>, so the clause takes one or MORE literals and
    /// §13.18.63.4 GR23 distributes them across the entry's repetitions. Each operand goes through the SAME ONE
    /// reader <see cref="ExtractValue"/>'s single-operand path uses (<see cref="RawValueOperandText"/> — the
    /// literal-position screen, the §8.8.3.3 GR3 concatenation fold, §13.10.3 SR2 constant-names, §12.3.7.4 GR11
    /// symbolic-characters, numeric normalization), so a report VALUE and a working-storage VALUE decode one
    /// literal identically.
    /// <para>⛔ IT DOES NOT GLUE (kb/Work PB506). <see cref="ExtractValue"/>'s multi-operand tail returns
    /// <c>item.GetText()</c> — the operand TOKENS run together — which is exactly what the Format-1 caller wants
    /// (it rejects that shape, COBOLNET1585) and exactly wrong for Format 4: `VALUE "XX" "YY"` reached the
    /// emitter as the raw text <c>"XX""YY"</c>, whose doubled quote the literal decoder then read as an escaped
    /// one, so a legal report entry printed <c>XX"YY</c>.</para>
    /// <para>NULL when the clause is not a bare literal list at all (a THRU range — §13.18.63.3 SR30 keeps the
    /// condition-name and content-validation formats out of the report section — or a Format-2 table phrase,
    /// which <see cref="Core.ValueClauseContext.valueClauseTablePhrase"/> carries) or when any operand failed the
    /// literal-position screen and has been reported: nothing is stored rather than a partial list.</para></summary>
    private List<string>? ExtractValueOperandList(Core.ValueClauseContext value, string where)
    {
        if (value.valueClauseTablePhrase() is { Length: > 0 }) return null;   // Format 2 — not a report-section clause
        var item = value.valueItem().FirstOrDefault();
        if (item is null || item.valueClauseRange() is not null) return null;
        var ops = item.valueClauseOperand();
        if (ops.Length == 0) return null;
        var raws = new List<string>(ops.Length);
        bool ok = true;
        foreach (var op in ops)
        {
            // Screen and fold EVERY operand (never short-circuit): a list with two bad operands draws two
            // diagnostics, the way the Format-1 multi-operand screen already does.
            if (RawValueOperandText(op, where) is { } raw) raws.Add(raw);
            else ok = false;
        }
        return ok ? raws : null;
    }

    /// <summary>Bind an FD CODE-SET clause (ISO §13.18.13; kb/Work PB110 — it was parsed as the '85 one-name form
    /// and read by nothing): both formats — <c>IS alphabet-name-1 [alphabet-name-2]</c> and the FOR ALPHANUMERIC /
    /// FOR NATIONAL phrases (one or both, any order, each at most once — §5.2.6.4). SR1/SR2: each name shall
    /// reference an alphabet defining a coded character set of its class (a LOCALE alphabet is COBOLNET1669 through
    /// the ONE resolver; a class mismatch or an undeclared name is COBOLNET1672). SR3: with record description
    /// entries and no SELECT WHEN, one class only, every elementary item of that usage, signed items SIGN SEPARATE.
    /// GR2/GR6: the on-medium coded character set, decided by <see cref="CodedCharacterSet.Medium"/> — the ONE
    /// classifier, so this binder names no set. <c>Identity</c> (NATIVE; UTF-16 on the D-N1 substrate — the
    /// whole-channel identity sets) converts as the identity and is CLAIMED, and nothing is emitted for it.
    /// <c>Translated</c> (an implementor code-name whose GR7 i correspondence with the native set is a
    /// single-byte code — EBCDIC, kb/Work PB793 — and the ISO/IEC 646 sets STANDARD-1/2 (GR7 c) and the ASCII
    /// code-name, whose identity correspondence covers only 128 characters, kb/Work PB1542) is CLAIMED and
    /// carried to the connector, which applies GR6 a/b at the medium boundary and refuses, at the WRITE or
    /// REWRITE, a record character outside the set. <c>NotProvided</c> (a literal-phrase
    /// alphabet's remapped ordinals; UTF-8 / UCS-4 as variable-width medium encodings) is the DOCUMENTED A.3
    /// item 27 non-support (COBOLNET1672 — "dependent upon a device capable of supporting the specified code";
    /// CONFORMANCE.md §2 row 27), never a silent identity.</summary>
    private void BindCodeSetClause(Core.CodeSetClauseContext cs, FileModel file, IReadOnlyList<DataItem> records)
    {
        using var _ = Edition.At(cs);
        var (alnumName, natName) = ChoiceIndicators.AlphabetPair(Edition, cs.codeSetForPhrase(),
            f => f.NATIONAL() is not null, f => f.cobolWord().GetText(), cs.cobolWord(),
            $"file '{file.CobolName}' CODE-SET", "13.18.13.2");
        var alnumSet = alnumName is null ? null : CodedCharacterSetOf(alnumName, $"CODE-SET … {alnumName}",
            "ISO §13.18.13.3 SR1 — alphabet-name-1 shall reference an alphabet that defines an alphanumeric coded character set");
        var natSet = natName is null ? null : CodedCharacterSetOf(natName, $"CODE-SET … {natName}",
            "ISO §13.18.13.3 SR2 — alphabet-name-2 shall reference an alphabet that defines a national coded character set");
        if (alnumName is not null && alnumSet is null || natName is not null && natSet is null) return;
        if (alnumSet is { National: true })
        {
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET … {alnumName}: alphabet-name-1 shall "
                + "reference an alphabet that defines an ALPHANUMERIC coded character set — this alphabet is defined "
                + "FOR NATIONAL (ISO §13.18.13.3 SR1)");
            return;
        }
        if (natSet is { National: false })
        {
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET … {natName}: alphabet-name-2 shall "
                + "reference an alphabet that defines a NATIONAL coded character set — this alphabet is alphanumeric "
                + "(ISO §13.18.13.3 SR2)");
            return;
        }
        // SR3 — record description entries and no SELECT WHEN (none exists in this grammar): one class only.
        if (records.Count > 0 && alnumName is not null && natName is not null)
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, "CODE-SET: if any record description entries are "
                + "associated with the file and no SELECT WHEN clauses are specified, either alphabet-name-1 or "
                + "alphabet-name-2 may be specified, but not both (ISO §13.18.13.3 SR3)");
        // GR2/GR6 — which conversion the set asks for. ONE question, asked of the SET (CodedCharacterSet.Medium)
        // rather than answered from a list of phrase names here: an identity correspondence over the whole
        // one-byte channel converts byte-for-byte and needs nothing; a single-byte code whose correspondence this
        // processor has (§12.3.7.4 GR7 c / GR7 i — including ISO/IEC 646's 128-character identity, which the
        // connector needs in order to REFUSE what the set lacks, kb/Work PB1542) is carried to the connector,
        // which replaces each character per GR6 a/b; a set whose
        // medium form is neither is the documented A.3 item 27 non-support — refused loudly, never a silent
        // identity (that would be a wrong answer for an EBCDIC-shaped alphabet).
        foreach (var (set, name) in new[] { (alnumSet, alnumName), (natSet, natName) })
        {
            if (set is null) continue;
            if (set.Medium is CodeSetMedium.NotProvided)
                Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET … {name}: the {set.Phrase} coded "
                    + "character set's on-medium representation differs from the native encoding, and this processor "
                    + "does not provide alternate device code sets (Annex A §A.3 item 27 — documented non-support, "
                    + "CONFORMANCE.md §2 row 27); NATIVE, STANDARD-1, STANDARD-2, UTF-16 and the ASCII code-name "
                    + "convert as the identity, and the EBCDIC code-name converts through its GR7 i correspondence");
            else if (set.Medium is CodeSetMedium.Translated)
                // GR1/GR2 — the alphabet the medium's data is represented in, from the successful OPEN onwards.
                file.CodeSetCorrespondence = set.MediumCorrespondence;
        }
        // SR3 a/b — the selected class's usage over every elementary record item; signed numeric SIGN SEPARATE.
        // ⛔ DEFERRED, not written here (kb/Work PB536): both sentences ask what an item IS DESCRIBED WITH, and
        // a group-level SIGN (§13.18.52.4 GR1) / USAGE (§13.18.60.4 GR1) clause reaches the leaf only in a later
        // pass, so a screen run from this FD walk refused the legal `05 G SIGN IS LEADING SEPARATE. 10 N PIC
        // S9(4).`. The CLASS the clause selected is all that travels; the screen is the declared pass
        // DataBinder.CheckCodeSetRecordItems, at PassPhase.SignResolved.
        if (alnumName is not null || natName is not null)
            file.CodeSetRecordClass = natName is not null;
    }

    /// <summary>WHICH general format a literal-position operand belongs to — the operand's name and the format's
    /// citation, which is all that differs between the positions sharing the one literal-position chokepoint
    /// (<see cref="RawValueOperandText"/>): the VALUE clause's literal-n, since kb/Work PB778 the PICTURE
    /// EDITING phrase's literal-1/-2/-3, and since kb/Work PB1072 the ALTERNATE RECORD KEY clause's SUPPRESS WHEN
    /// literal-1. The screen, the substitutions and the diagnostic codes are one.</summary>
    private sealed record LiteralPosition(string Operand, string Format)
    {
        public static readonly LiteralPosition Value = new("the VALUE operand",
            "every format of the VALUE clause writes literal-n (ISO §13.18.63.2)");
        public static readonly LiteralPosition Editing = new("the PICTURE EDITING operand",
            "the EDITING phrase writes literal-1, literal-2 and literal-3 (ISO §13.18.40.2 Format 1)");
        public static readonly LiteralPosition SuppressWhen = new("the SUPPRESS WHEN operand",
            "the ALTERNATE RECORD KEY clause writes SUPPRESS WHEN literal-1 (ISO §12.4.5.6.2)");
    }

    /// <summary>The RAW single-literal text of a VALUE operand — the data path's currency (decoded at emit
    /// time): a §8.8.3 concatenation expression folds to its equivalent literal's raw text (§8.8.3.3 GR3); a
    /// constant-name substitutes its literal's raw text (§13.10.3 SR2 / §13.10.4 GR1 — a VALUE operand is a
    /// literal position); any other operand keeps its source text, numeric literals normalized to dot-decimal
    /// (ISO §12.3.7 GR14a).
    /// <para>NULL when the operand is NOT a literal position at all (<see cref="IsLiteralValueOperand"/> said
    /// so and <see cref="ReportNonLiteralValueOperand"/> has reported it) — the caller stores no value, so the
    /// unreadable operand never reaches the emitter. kb/Work PB732: this method's last arm used to be an
    /// unconditional <c>op.GetText()</c>, which promoted an UNDEFINED WORD to an alphanumeric literal of its
    /// own spelling (a silent wrong answer on an alphanumeric subject) or handed the C# backend an identifier
    /// that does not exist (CS0103, exit 70, on a numeric one).</para></summary>
    private string? RawValueOperandText(Core.ValueClauseOperandContext op, string where,
        LiteralPosition? position = null)
    {
        // THE literal-position screen (§13.18.63.2 — every format writes literal-n), asked BEFORE any folding so
        // a rejected operand draws exactly one diagnostic and the ConcatFolder is never entered for it.
        if (!IsLiteralValueOperand(op)) { ReportNonLiteralValueOperand(op, where, position); return null; }
        return op.nonNumericLiteral()?.concatenationExpression() is { } ce
            ? ConcatFolder.Fold(ce, Edition, LiteralEnv).RawText
            // ALL literal-1 (§8.3.3.6.3 SR2 — kb/Work PB71, PB1627): `ALL` + literal-1 folded by the ONE Format 6
            // reader (a quoted literal, a concatenation expression, or a constant-name) and re-quoted, so the
            // raw-text ALL reader (CobolLiteral.AllLiteralRaw) sees ONE literal of the right class.
            : op.nonNumericLiteral()?.figurativeConstant() is { } allFig
              && ConcatFolder.FoldAllLiteral(allFig, LiteralEnv, Edition) is { } literal1
            ? "ALL" + literal1.RawText
            // [ALL] symbolic-character-1 (§8.3.3.6.2 Format 7; §12.3.7.4 GR11 — kb/Work PB110): the figurative's ONE
            // character as an ALL literal of its class — GR2's fill in a VALUE association, exactly like ALL "c".
            : op.nonNumericLiteral()?.figurativeConstant()?.cobolWord() is { } symAll && SymbolicRaw(symAll.GetText()) is { } rawAll
            ? rawAll
            : SymbolicValueRawText(op) is { } rawBare ? rawBare
            // A constant-name substitutes its literal AS WRITTEN and that literal then takes the path the written
            // literal takes (§13.10.4 GR1 "as if literal-1 … were written"; kb/Work PB1230): `VALUE K` where
            // `K CONSTANT AS 1,5` under DECIMAL-POINT IS COMMA normalizes exactly as `VALUE 1,5` does.
            : NormalizeIfNumericLiteral(ConstantValueRawText(op) ?? op.GetText());
    }

    /// <summary>THE literal-position predicate for a VALUE operand (kb/Work PB732), PURE — no folding, no
    /// diagnostics, so a caller may ask about an operand it will not read.
    /// <para>Every format of the VALUE clause writes <i>literal-n</i> in every operand position (ISO
    /// §13.18.63.2, verified against the printed general-format figures: Format 1 <c>literal-1</c>, Format 2
    /// <c>{literal-1}…</c>, Format 3 <c>literal-2 [THROUGH literal-3]</c>, Format 4 <c>{literal-1}…</c>,
    /// Format 5 <c>literal-5 [THROUGH literal-6]</c> — no format admits an identifier, a data-name or an
    /// expression). The spellings a literal position accepts are therefore exactly: a literal; a figurative
    /// constant (§8.3.3.6.3 SR1 — "A figurative constant may be used whenever 'literal' appears in a format");
    /// the ALL symbolic-character-1 figurative, whose word §8.3.3.6.3 SR4 requires to be a SYMBOLIC CHARACTERS
    /// name; a bare symbolic-character; and a constant-name (§13.10.3 SR2 — "constant-name-1 may be used
    /// anywhere that a format specifies a literal"). Anything else is a word — or a shape — that names no
    /// literal.</para>
    /// <para>The <c>valueClauseOperand : unaryExpression | nonNumericLiteral</c> grammar is a deliberate
    /// superset (a constant-name and a symbolic-character are spelled as ordinary words, and only the symbol
    /// tables tell them from a data-name), so this is a BIND rule, never a grammar screen.</para></summary>
    private bool IsLiteralValueOperand(Core.ValueClauseOperandContext op)
    {
        if (op.nonNumericLiteral() is { } nn)
            // ALL cobolWord is the ONE word-bearing figurative alternative: Format 7 (§8.3.3.6.2), whose word SR4
            // requires to be declared in SYMBOLIC CHARACTERS, or Format 6 over a constant-name literal-1 (§13.10.3
            // SR2 — kb/Work PB1627). Every other alternative — STRINGLIT / NATLIT / BOOLLIT / HEXLIT, a concatenation
            // expression, ALL literal-1, and the keyword figuratives ZERO / SPACE / HIGH-VALUE / LOW-VALUE / QUOTE —
            // is a literal by shape. The predefined NULL is NOT: it is an identifier (§8.4.3.1.2 Format 8), and
            // §8.4.3.10.3 SR1 names no VALUE clause among the places it may be written (kb/Work PB1427 / PB939 —
            // counted a literal here, `PIC X VALUE NULL` stored LOW-VALUE).
            return nn.predefinedNull() is null
                && (nn.figurativeConstant()?.cobolWord()?.GetText() is not { } symWord
                || SymbolicOf(symWord) is not null || FindConstant(symWord) is not null);
        // §8.3.3.3.2 rule 2 makes a written sign part of the numeric literal — ONE sign, as its leftmost
        // character — so `VALUE -9999` is literal-1, while `--5`, `+-5` and `- 5` are no literal at all (kb/Work
        // PB1445: this arm used to strip ANY number of signs, and `VALUE --5` reached the C# backend as a
        // decrement operator, CS1059). THE ONE §8.3.3.3.2 reading, SoleOperand.NumericLiteral, decides it. The
        // figurative ZERO is a literal only UNSIGNED: a sign belongs to a numeric literal, never to a figurative.
        var u = op.unaryExpression();
        if (CobolNet.Frontend.Expressions.SoleOperand.NumericLiteral(u) is not null
            || u?.addOp() is null && u?.primaryExpression()?.ZERO_ARITH() is not null) return true;
        // A word: the only names a literal position admits are a constant-name (§13.10.3 SR2) and a
        // symbolic-character (§8.3.3.6.2 Format 7). ⛔ The lookup takes the operand through
        // BareValueOperandWord — the SAME accessor the two substituting readers use — so the predicate and the
        // readers cannot diverge. That is load-bearing for the SIGNED shape: rule 2's sign belongs to a NUMERIC
        // LITERAL, so `VALUE -K` (K a constant-name) is a sign applied to a NAME and is no literal at all; a
        // sign-stripping lookup here would have called it a literal while ConstantValueRawText, which does not
        // strip, still fell through — re-opening the very sink PB732 closed, on `-K` instead of `K`.
        return BareValueOperandWord(op) is { } dref
            && (ConstantOf(dref) is not null || SymbolicOf(dref) is not null);
    }

    /// <summary>The BARE (unqualified, unsubscripted, UNSIGNED) word reference a VALUE operand reduces to, or
    /// null — the ONE accessor for "which word does this operand name", shared by
    /// <see cref="IsLiteralValueOperand"/>, <see cref="ReportNonLiteralValueOperand"/>,
    /// <see cref="SymbolicValueRawText"/> and <c>ConstantValueRawText</c> (kb/Work PB732; it was the same
    /// sole-child walk written out twice). Any operator or suffix on the way down makes the operand a
    /// non-constant, non-symbolic shape, which is exactly what a null says.
    /// <para>An operand whose trailing <c>IN alphabet-name-1</c> the parser took as a qualifier answers with the
    /// reference WITHOUT that suffix (<see cref="_alphabetPeeledOperands"/>, kb/Work PB983), so every reader sees
    /// the word the symbol decided.</para></summary>
    private Core.DataReferenceContext? BareValueOperandWord(Core.ValueClauseOperandContext op) =>
        _alphabetPeeledOperands.TryGetValue(op, out var peeled) ? peeled : WrittenValueOperandWord(op);

    /// <summary>The sole-child walk <see cref="BareValueOperandWord"/> answers with when no phrase was peeled.</summary>
    private static Core.DataReferenceContext? WrittenValueOperandWord(Core.ValueClauseOperandContext op)
    {
        Antlr4.Runtime.Tree.IParseTree? n = op.unaryExpression();
        while (n is not null and not Core.DataReferenceContext)
            n = n.ChildCount == 1 ? n.GetChild(0) : null;
        return n as Core.DataReferenceContext;
    }

    /// <summary>VALUE operands whose written reference ended in the <c>IN alphabet-name-1</c> phrase that the
    /// greedy qualification loop claimed (<c>VALUE "M" THRU K IN AL</c>, K a constant-name) → the reference with
    /// that suffix cut off (<see cref="ReferenceResolver.WithoutTrailingSuffix"/>). Filled only by
    /// <see cref="RangeAlphabetPhraseOf"/>.</summary>
    private readonly Dictionary<Core.ValueClauseOperandContext, Core.DataReferenceContext> _alphabetPeeledOperands = [];

    /// <summary>The VALUE operands this binder has already diagnosed — one report per source operand even when
    /// two callers read the same node (<see cref="ExtractValue"/> screens a THRU range or a glued literal list
    /// that <see cref="RawValueOperandText"/> may also visit). The <c>ReferenceResolver._diagnosed</c>
    /// shape.</summary>
    private readonly HashSet<Core.ValueClauseOperandContext> _valueOperandDiagnosed = [];

    /// <summary>The entries that WROTE a VALUE clause (data-item formats 1 and 2) — the subject of §13.16.3 SR10, "The
    /// VALUE clause shall not be specified for data items of class index, message-tag, object, or pointer". Kept apart
    /// from <see cref="DataItem.RawValue"/> because a refused operand (`VALUE NULL`, COBOLNET2576) stores no value
    /// while the clause is still specified, and the acquired-usage arm is asked after the forest is built.</summary>
    private readonly HashSet<DataItem> _valueClauseWritten = [];

    /// <summary>THE report for a VALUE operand that occupies a literal position and is not a literal (kb/Work
    /// PB732) — the data-division arm of the R30 "a word that names nothing" chokepoint, which
    /// <c>ReferenceResolver.Resolve</c> closes for the PROCEDURE DIVISION and which the VALUE clause never
    /// entered because it resolves its own operands.
    /// <list type="bullet">
    /// <item>A BARE word (including the <c>ALL symbolic-character-1</c> figurative) draws
    /// <c>COBOLNET1639</c>: the position admits a constant-name or a symbolic-character, and the word is
    /// neither, so "a statement shall contain a reference that uniquely identifies that resource" (ISO
    /// §8.4.2.1) is violated.</item>
    /// <item>Any other non-literal shape — a qualified / subscripted / reference-modified reference, a
    /// function-identifier, a parenthesized arithmetic expression — draws <c>COBOLNET1902</c>: nothing is
    /// undefined, the general format simply writes literal-n (ISO §13.18.63.2).</item>
    /// </list>
    /// §4.2.2 is the obligation to indicate both ("An implementation shall provide a warning mechanism …
    /// to indicate violations of the general formats and the explicit syntax rules of standard COBOL").</summary>
    private void ReportNonLiteralValueOperand(Core.ValueClauseOperandContext op, string where,
        LiteralPosition? position = null)
    {
        if (!_valueOperandDiagnosed.Add(op)) return;
        position ??= LiteralPosition.Value;
        using var _ = Edition.At(op);
        if (op.nonNumericLiteral()?.predefinedNull() is not null)
        {
            PredefinedNullRule.Report(Edition, $"{position.Operand} of {where}, where {position.Format},");
            return;
        }
        if (op.nonNumericLiteral()?.figurativeConstant()?.cobolWord() is { } symWord)
        {
            Edition.Error(DiagnosticCatalog.UndefinedReference, $"{where}: {position.Operand} "
                + $"'ALL {symWord.GetText()}' names no symbolic character and no constant-name — symbolic-character-1 "
                + "shall be specified in the SYMBOLIC CHARACTERS clause of the SPECIAL-NAMES paragraph (ISO §8.3.3.6.3 "
                + "SR4), and the only other word ALL may precede is a constant-name standing for literal-1 (§13.10.3 "
                + "SR2), so the operand identifies no resource (ISO §8.4.2.1).");
            return;
        }
        // Signs in front of a numeric literal that do not make ONE literal (`--5`, `+-5`, `- 5`, a sign at the end
        // of a line and its digits on the next): §8.3.3.3.2 rule 2 is the rule broken, so it is the rule named
        // (kb/Work PB1445) — the same code LiteralScreenPass reports for a separated sign in every other literal slot.
        if (op.unaryExpression() is { } signedShape
            && CobolNet.Frontend.Expressions.ArithmeticFormationRules.SignedLiteralViolation(signedShape) is { } why)
        {
            Edition.Error(DiagnosticCatalog.SignedLiteralSignNotAdjacent, $"{where}: {position.Operand} "
                + $"'{AsWritten(op)}' is not a literal — {why}: "
                + CobolNet.Frontend.Expressions.ArithmeticFormationRules.SignRuleQuote
                + $"; and {position.Format}, which admits no arithmetic expression.");
            return;
        }
        // The BARE-word arm only (the SAME accessor the predicate used): a SIGNED or suffixed operand is not a
        // word in a literal position at all — §8.3.3.3.2 rule 2's sign belongs to a numeric literal — so it
        // falls to the general-format verdict below, which quotes the operand as written.
        if (BareValueOperandWord(op) is { } dref && dref.dataReferenceSuffix().Length == 0
            && dref.cobolWord()?.GetText() is { } word)
        {
            // A POSITIVE hint only: ByName carries the data items bound SO FAR, so its silence is not evidence
            // that the name is undeclared (a later sibling is not registered yet) — the verdict above never
            // depends on it (feedback_verdict_evidence_invariant).
            string hint = ByName.ContainsKey(word)
                ? $" ('{word}' is declared as a data item; a data item is not a literal.)" : "";
            Edition.Error(DiagnosticCatalog.UndefinedReference, $"{where}: {position.Operand} '{word}' is not a "
                + $"literal — {position.Format}, and the only "
                + "words a literal position admits are a constant-name (ISO §13.10.3 SR2) and a "
                + "symbolic-character (ISO §8.3.3.6.2 Format 7). '" + word + "' is neither, so the operand "
                + "identifies no resource (ISO §8.4.2.1: \"a statement shall contain a reference that uniquely "
                + $"identifies that resource\"). Check the spelling, or define the constant.{hint}");
            return;
        }
        Edition.Error(DiagnosticCatalog.ValueOperandNotALiteral, $"{where}: {position.Operand} "
            + $"'{AsWritten(op)}' is not a literal — {position.Format}. A figurative constant (ISO §8.3.3.6.3 SR1), a constant-name (ISO §13.10.3 SR2) "
            + "and a symbolic-character (ISO §8.3.3.6.2 Format 7) stand where a literal stands, each written "
            + "as a BARE word; an arithmetic expression, a function-identifier and a qualified, subscripted or "
            + "reference-modified reference do not.");
    }

    /// <summary>The raw <c>ALL"c"</c> text a symbolic-character VALUE operand substitutes (bare-word form), or
    /// null when the operand names no symbolic character (kb/Work PB110; the ConstantValueRawText shape).</summary>
    private string? SymbolicValueRawText(Core.ValueClauseOperandContext op) =>
        BareValueOperandWord(op) is { } dref && SymbolicOf(dref) is not null ? SymbolicRaw(dref.GetText()) : null;

    /// <summary>The raw ALL-literal text of the symbolic character named <paramref name="word"/>, or null: the
    /// class-prefixed re-quoted one-character literal (embedded delimiters doubled per §8.3.3.2.3 r3).</summary>
    internal string? SymbolicRaw(string word) =>
        SymbolicOf(word) is not { } sym ? null
        : "ALL" + (sym.National ? "N" : "") + "\"" + sym.Value.Replace("\"", "\"\"") + "\"";

    /// <summary>Build the <see cref="EditingPhraseSpec"/> list for a PICTURE clause's EDITING phrases
    /// (ISO §13.18.40.2 Format 1) — character-1 + DECODED literal text, handed to <see cref="PictureAnalyzer"/>
    /// for SR8–SR12 validation and render-rule construction. Null when the clause carries no EDITING phrase.
    /// <para>⛔ character-1 is the BARE word the printed general format writes (kb/Work PB568). The quoted
    /// spelling the grammar used to REQUIRE still parses, so that it can be REFUSED BY NAME (COBOLNET2149)
    /// instead of as an ANTLR syntax error; which spelling was written also decides where literal-1 sits in the
    /// phrase's literal list, so the two are read in one place.</para></summary>
    private List<EditingPhraseSpec>? BuildEditingSpecs(Core.PictureClauseContext? pic, string where)
    {
        var phrases = pic?.editingPhrase();
        if (phrases is null || phrases.Length == 0) return null;
        var list = new List<EditingPhraseSpec>(phrases.Length);
        foreach (var ph in phrases)
        {
            string char1;
            if (ph.cobolWord() is { } word)
                char1 = word.GetText();
            else
            {
                var quoted = ph.literal();
                char1 = quoted is null ? "" : CobolLiteral.Decode(quoted.GetText());
                Edition.Error(DiagnosticCatalog.PictureEditingChar1NotALiteral, $"{where}: the PICTURE EDITING "
                    + $"phrase's character-1 is written as a quoted literal ({quoted?.GetText() ?? "\"\""}) "
                    + "— the general format writes it BARE, as a picture symbol: `EDITING character-1 { IS "
                    + "literal-1 | FOR … }` (ISO §13.18.40.2 Format 1; §13.18.40.3 SR8 types character-1 as a basic "
                    + $"letter, and SR9 types only literal-1/-2/-3 as literals). Write EDITING {char1.ToUpperInvariant()} instead");
            }
            if (ph.editingForPhrase() is { } forp)
            {
                // FOR (extended sign control): map the literals to NEGATIVE / POSITIVE by keyword position (either
                // order is legal — §13.18.40.2 choice indicators). The keyword that appears first owns literal[0].
                var flits = forp.editingLiteral();
                var neg = forp.NEGATIVE();
                var pos = forp.POSITIVE();
                bool negFirst = neg is not null && (pos is null || neg.Symbol.TokenIndex < pos.Symbol.TokenIndex);
                EditLiteral? negLit, posLit;
                if (negFirst)
                {
                    negLit = DecodeEditLiteral(flits.Length > 0 ? flits[0] : null, where);
                    posLit = pos is not null && flits.Length > 1 ? DecodeEditLiteral(flits[1], where) : null;
                }
                else
                {
                    posLit = DecodeEditLiteral(flits.Length > 0 ? flits[0] : null, where);
                    negLit = neg is not null && flits.Length > 1 ? DecodeEditLiteral(flits[1], where) : null;
                }
                list.Add(new EditingPhraseSpec(char1, Simple: null, Neg: negLit, Pos: posLit, IsForForm: true));
            }
            else
            {
                // IS (simple insertion): literal-1 is the phrase's editingLiteral — a node of its own, never
                // counted out of a shared literal list alongside a (refused) quoted character-1.
                list.Add(new EditingPhraseSpec(char1, Simple: DecodeEditLiteral(ph.editingLiteral(), where),
                    Neg: null, Pos: null, IsForForm: false));
            }
        }
        return list;
    }

    /// <summary>Decode a PICTURE EDITING literal operand — literal-1, literal-2 or literal-3 of ISO §13.18.40.2
    /// Format 1 — to its content AND the literal CLASS it was written in, which §13.18.40.3 SR9's first sentence
    /// is a rule about ("… shall be national literals. Otherwise … shall be alphanumeric literals"; kb/Work
    /// PB492). Null when the operand is absent or is no literal at all (already reported).
    /// <para>⛔ THE OPERAND IS A LITERAL POSITION (kb/Work PB778), so it goes through THE literal-position
    /// chokepoint the VALUE clause uses — <see cref="RawValueOperandText"/> — never a second copy of its screen or
    /// its substitutions: a constant-name (§13.10.3 SR2 admits it, §13.10.4 GR1 substitutes its literal), a
    /// symbolic-character and ALL literal-1 arrive as the literal they stand for, a concatenation expression as
    /// its folded literal (§8.8.3.3 GR3), and anything else is refused by name there. A KEYWORD figurative constant
    /// (§8.3.3.6.3 SR1) is ONE character in this context (§8.3.3.6.4 GR3b — the EDITING rules specify no length)
    /// whose class is the subject's (GR1), so it travels in both representations and the analyzer picks one. A
    /// numeric or boolean literal — or NULL, which has no character value — is carried as
    /// <see cref="EditLiteralClass.NotAlphanumericOrNational"/> for SR9 to refuse. Before PB778 the narrow
    /// <c>literal</c> rule refused a constant-name at PARSE time, and the figuratives and concatenations it did
    /// admit reached the edited item as their SOURCE TEXT (<c>EDITING T IS SPACE</c> inserted "SPACE").</para></summary>
    private EditLiteral? DecodeEditLiteral(Core.EditingLiteralContext? lit, string where)
    {
        if (lit?.valueClauseOperand() is not { } op) return null;
        if (op.nonNumericLiteral()?.figurativeConstant() is { } fig && fig.allLiteral() is null && fig.cobolWord() is null)
            return ConcatFolder.FigurativeChar(fig, PicCategory.Alphanumeric, LiteralEnv) is { } alnum
                ? new EditLiteral(alnum.ToString(), EditLiteralClass.Figurative,
                    ConcatFolder.FigurativeChar(fig, PicCategory.National, LiteralEnv)?.ToString())
                : new EditLiteral(fig.GetText(), EditLiteralClass.NotAlphanumericOrNational);
        if (RawValueOperandText(op, where, LiteralPosition.Editing) is not { } raw) return null;
        string written = CobolLiteral.AllLiteralRaw(raw) ?? raw;   // ALL literal-1 / symbolic-character: literal-1 (GR3c)
        return CobolLiteral.ClassOf(written) switch
        {
            LiteralClass.Alphanumeric => new EditLiteral(CobolLiteral.Decode(written), EditLiteralClass.Alphanumeric),
            LiteralClass.National => new EditLiteral(CobolLiteral.Decode(written), EditLiteralClass.National),
            _ => new EditLiteral(raw, EditLiteralClass.NotAlphanumericOrNational),
        };
    }

    /// <summary>Build the <see cref="TableValueSpec"/> list for a Format 2 (table) VALUE clause (ISO §13.18.63.2):
    /// each phrase's literal list (raw operand text, concat/constant folded — the Format-1 currency) plus its FROM
    /// (subscript-1 …) and optional TO (subscript-2 …) integer subscripts. The subscripts are split by the TO
    /// token's position (FROM's precede it, TO's follow).
    /// <para>NULL when any operand is not a literal position: it has been reported (kb/Work PB732 — the table
    /// arm was the same silent sink as Format 1) and the whole clause binds nothing, because dropping one
    /// literal from a phrase would shift its §13.18.63.3 SR19–SR22 subscript correspondence.</para></summary>
    private List<TableValueSpec>? BuildTableValueSpecs(Core.ValueClauseTablePhraseContext[] phrases, string where)
    {
        var list = new List<TableValueSpec>(phrases.Length);
        bool ok = true;
        for (int i = 0; i < phrases.Length; i++)
        {
            var ph = phrases[i];
            // Every phrase is screened even after one has failed, so a clause with two bad operands names both.
            var raws = ph.valueClauseOperand().Select(op => RawValueOperandText(op, where)).ToList();
            if (raws.Any(r => r is null)) { ok = false; continue; }
            var literals = raws.Select(r => r!).ToList();
            int toIdx = ph.TO()?.Symbol.TokenIndex ?? int.MaxValue;
            var from = new List<int>();
            var fromWritten = new List<string>();
            List<int>? to = ph.TO() is not null ? [] : null;
            List<string>? toWritten = ph.TO() is not null ? [] : null;
            foreach (var il in ph.signedIntegerLiteral())
            {
                // SATURATE rather than fall to 0 on an out-of-int literal, through THE ONE integer-literal reader
                // (kb/Work PB1579): the saturated value is out of range for any real table, so the §13.18.63.3
                // SR20/SR21 screen still finds it, and the screen quotes the subscript AS WRITTEN (FromWritten /
                // ToWritten) — never the saturated number, which the program does not contain.
                // ⛔ THE SATURATION FLOOR IS int.MinValue, NOT 0 (kb/Work PB553). §13.18.63.3 SR19 admits a
                // SIGNED integer numeric literal here (§5.5 2) a) → §8.3.3.3.2 2)), so a negative subscript is
                // now a bindable value that §8.4.2.3.4 GR2 ("The value of a subscript shall be a positive
                // integer") rejects downstream. A long.TryParse here saturated a 20-to-31-digit NEGATIVE subscript
                // to int.MaxValue, and the screen then quoted a positive number back.
                string written = SignedIntegerLiteral.Read(il);
                int v = CobolNet.Validation.IntegerOperandRules.TryHostValue(written, out int n) ? n : int.MaxValue;
                if (il.Start.TokenIndex < toIdx) { from.Add(v); fromWritten.Add(written); }
                else { to!.Add(v); toWritten!.Add(written); }
            }
            list.Add(new TableValueSpec(literals, from, to, i, fromWritten, toWritten));
        }
        return ok ? list : null;
    }

    /// <summary>The ALL-FORMATS literal screen a Format 2 (table) VALUE's occurrence-literals ride (ISO
    /// §13.18.63.3 SR2/SR3 and, through SR16, SR10–SR15) — ONE OCCURRENCE-LITERAL AT A TIME, through the SAME
    /// <see cref="ScreenValueLiteral"/> funnel the Format-1 <see cref="DataItem.RawValue"/> takes (kb/Work PB208).
    /// SR2 — "If the category of the subject of the entry is numeric, all literals in the VALUE clause shall be
    /// numeric and shall be permissible values within the range indicated by the PICTURE clause or the USAGE
    /// clause" — is an ALL FORMATS rule, and SR16 carries SRs 10–15 in as well, so a format-2 literal is governed
    /// exactly as the format-1 one. It was not: <see cref="BuildTableValueSpecs"/>' literals went straight to the
    /// emitter, so <c>05 B PIC 9(4) COMP OCCURS 2 VALUE "0012" FROM (1) TO (2).</c> compiled clean at strict 2023
    /// where its format-1 twin is COBOLNET1657. The text the funnel RETURNS is what gets stored — including the
    /// <c>--permissive</c> numeric rewrite of a class-mismatched literal, so the emitter's per-occurrence literal
    /// carries a screened value exactly as <c>item.RawValue</c> does.
    /// <para>⛔ THE LITERAL screen belongs HERE, at entry bind, beside the Format-1 call site: it is a property of
    /// the literal and the subject's own PICTURE, and the two formats answering on ONE timeline is what
    /// <c>ValueFormat2Tests.Format1AndFormat2_ScreenTheSameLiteralAlike</c> pins. The clause's GEOMETRY —
    /// §13.18.63.3 SR18–SR23 and the §13.18.63.4 GR12–GR16 resolution — is NOT here and cannot be: those rules are
    /// written against the entry's ANCESTORS ("or superordinate to that entry"), and <see cref="DataItem.Parent"/>
    /// is assigned by <c>BindEntries</c> only AFTER this returns. They are the post-forest
    /// <c>ResolveTableValues</c> pass (DataBinder.TableValue.cs; kb/Work PB505).</para></summary>
    private void ScreenTableValueLiterals(DataItem item, string where)
    {
        if (item.TableValues is not { Count: > 0 } specs) return;
        if (item.Pic is not { } subject) return;
        // The subject §13.18.63.3 SR4/SR5/SR10 measure each occurrence-literal against is the ELEMENT, an
        // ELEMENTARY item (kb/Work PB206) — described once, exactly as the format-1 call site describes it, so
        // the two formats cannot disagree about a size any more than they can about a category.
        var elementSubject = ValueSubject.ForElementary(subject, item.IsDynamicLength, item.IsAnyLength,
            item.PicIsValueImplied);
        var screened = new List<TableValueSpec>(specs.Count);
        bool rewritten = false;
        foreach (var s in specs)
        {
            var lits = new List<string>(s.Literals.Count);
            foreach (string lit in s.Literals)
            {
                string kept = ScreenValueLiteral(subject, lit,
                    $"{where}, Format 2 VALUE FROM ({string.Join(" ", s.From)})", elementSubject);
                rewritten |= !string.Equals(kept, lit, StringComparison.Ordinal);
                lits.Add(kept);
            }
            screened.Add(s with { Literals = lits });
        }
        if (rewritten) item.TableValues = screened;
    }

    /// <summary>ISO §13.16.3 SR8's verdict on a PICTURE written beside a PICTURE-LESS usage: the diagnostic
    /// code that usage family has always carried, and the message that names the family the programmer wrote.
    ///
    /// <para>⛔ ONE method for BOTH ways an item can acquire the usage — its own USAGE clause
    /// (<c>BindEntry</c>) and §13.18.60.4 GR1 group inheritance (<c>UsageInheritanceElementary</c>). The two
    /// call sites differ only in WHOSE clause they name, never in the verdict, which is what GR1 requires: the
    /// two spellings describe one item. Guard every call with <see cref="UsageFamilies.IsPictureless"/> — the
    /// fall-through arm is the FLOAT family, so a usage outside the set would be misreported as one.</para></summary>
    private static (string Code, string Text) Sr8PicturelessVerdict(Usage usage) => usage switch
    {
        Usage.ObjectReference => ("COBOLNET0812",
            "PICTURE may not be specified with USAGE OBJECT REFERENCE (ISO §13.16.3 SR8 — an "
            + "object-reference item is picture-less)"),
        Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble => ("COBOLNET0870",
            "PICTURE may not be specified with a fixed-width binary usage "
            + "(BINARY-CHAR/-SHORT/-LONG/-DOUBLE) — the item is picture-less (ISO §13.16.3 SR8)"),
        Usage.ProgramPointer or Usage.FunctionPointer => (DiagnosticCatalog.UsageClauseCompatibility.Code,
            "PICTURE may not be specified with USAGE PROGRAM-POINTER or FUNCTION-POINTER — the item "
            + "is picture-less (ISO §13.16.3 SR8)"),
        Usage.Pointer => (DiagnosticCatalog.UsageClauseCompatibility.Code,
            "PICTURE may not be specified with USAGE POINTER — a data-pointer item is picture-less "
            + "(ISO §13.16.3 SR8)"),
        Usage.Index => (DiagnosticCatalog.UsageClauseCompatibility.Code,
            "PICTURE may not be specified with USAGE INDEX — an index data item is picture-less, and "
            + "its content is an occurrence number rather than a pictured value (ISO §13.16.3 SR8; "
            + "§13.18.60.4 GR10)"),
        _ => ("COBOLNET1521",
            "PICTURE may not be specified with a floating-point usage "
            + "(COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED/FLOAT-BINARY-*/FLOAT-DECIMAL-*) — a "
            + "floating-point item is picture-less (ISO §13.16.3 SR8; §13.18.60.2)"),
    };

    /// <summary>The USAGE an entry hands DOWN to the elementary items under it — ISO §13.18.60.4 GR1, "If the
    /// USAGE clause is specified or implied at a group level, it applies only to each elementary item in the
    /// group … and not to the group itself".
    /// <para><see cref="Pictureless"/> is the representation entry bind synthesized for a picture-less usage
    /// (§13.16.3 SR8) — the profile that, by GR1, belongs to the leaves rather than to the group header that
    /// wrote the clause. <see cref="Pending"/> carries the deferred NATIONAL/BIT mark the same way, and
    /// <see cref="FromName"/> names the entry the clause was written on so §13.18.60.3 SR2's message can point
    /// at it.</para>
    /// <para><see cref="NoSign"/> is the <c>WITH NO SIGN</c> phrase of that same clause (§13.18.60.2 prints it
    /// INSIDE the PACKED-DECIMAL alternative, so it is not a clause of its own): it hands down with the clause
    /// it belongs to, and is adjudicated at each leaf by <see cref="ApplyEffectiveNoSign"/> — kb/Work PB570,
    /// where it was read from the entry's own <c>usageClause</c> and therefore never reached a leaf at all.</para></summary>
    private readonly record struct InheritedUsage(
        Usage? Effective, PicInfo? Pictureless, PicPending Pending, string? FromName, bool NoSign);

    /// <summary>
    /// ⛔ THE USAGE-INHERITANCE PASS (P5.11e, DESIGN-data-model §2.7) — ISO §13.18.60.4 GR1, run ONCE over the
    /// completed forest: <i>"If the USAGE clause is specified or implied at a group level, it applies only to
    /// each elementary item in the group. Unless the GROUP-USAGE clause is also specified or implied, the USAGE
    /// clause applies only to each elementary item in the group and not to the group itself."</i>
    ///
    /// <para><b>THE INVARIANT THIS PASS EXISTS TO HOLD.</b> GR1 does not say a group's usage <i>influences</i>
    /// its leaves; it says the clause <i>applies to</i> each of them. So an elementary item that acquired its
    /// usage from a group shall be THE SAME ITEM the written-clause spelling produces — same representation,
    /// same width, same syntax-rule verdicts, same diagnostics. <c>01 G USAGE u. 05 A ⟨pic⟩.</c> ≡
    /// <c>01 G. 05 A ⟨pic⟩ USAGE u.</c> That equivalence is a drift test (<c>UsageInheritanceTests</c>), it
    /// enumerates the <see cref="Usage"/> enum, and it is what makes the NEXT usage automatic.</para>
    ///
    /// <para><b>Why it is ONE walk over ONE derivation (kb/Work PB495).</b> The pass used to be two — a
    /// <c>ResolveIndexItems</c> that shed a hand-listed set of synthesized profiles from group headers, and an
    /// <c>InheritUsageClauses</c> that applied a DIFFERENT hand-listed set (<c>Binary or Packed or Comp5</c>) to
    /// leaves. Neither list was derived from anything, the two disagreed, and every usage in neither fell
    /// through in SILENCE: a picture-less leaf under a group-level BINARY-SHORT or FLOAT-LONG became a
    /// ZERO-LENGTH item (a MOVE into it then NRE'd the emitter), a group-level FLOAT-SHORT left its synthesized
    /// float profile ON the group so the record struct emitted a scalar <c>float</c> and every member reference
    /// became a raw Roslyn CS1061, and a PICTURE-bearing leaf under a group-level BINARY-LONG or INDEX kept its
    /// usage-display width with no diagnostic at all. There is now ONE derivation of an elementary item's
    /// effective usage — its own clause, else the nearest enclosing one — and three consumers of it: the group
    /// header's shed, the leaf's representation, and the screens (§13.18.60.3 SR2/SR3/SR5/SR12/SR20 and
    /// §13.16.3 SR8), each of which is the SAME code the written-clause path runs.</para>
    ///
    /// <para>Runs BEFORE <see cref="InheritSignClauses"/> — a non-DISPLAY item takes the BinaryMinus sign form
    /// regardless of any inherited SIGN clause (§13.18.52 applies only to usage-display items).</para>
    /// </summary>
    internal void UsageInheritancePass()
    {
        foreach (var root in Roots) UsageInheritanceWalk(root, default);
    }

    /// <summary>⛔ THE ONE "usage this entry carries in its OWN right" derivation: its written USAGE clause, or
    /// the one a GROUP-USAGE clause IMPLIES (§13.18.29.3 SR2/SR3 — "USAGE BIT / NATIONAL may be implicitly
    /// specified"), or null when the entry states neither. §13.18.60.4 GR1 is written over "specified OR IMPLIED",
    /// so the two spellings are one fact and this is where it is computed.
    /// <para>Read PUSH-style by <see cref="UsageInheritanceWalk"/>, which hands it down the forest, and PULL-style
    /// by <c>DataBinder.ImpliedPicture</c>'s §8.3.3.6.4 GR1 context question, which runs before that walk.</para></summary>
    private static Usage? DeclaredUsageOf(DataItem item) => item.OwnUsage ?? item.GroupUsage switch
    {
        GroupUsage.Bit => Usage.Bit,
        GroupUsage.National => Usage.National,
        _ => null,
    };

    /// <summary>One entry of the §13.18.60.4 GR1 walk: screen its own clause against the one it inherited
    /// (§13.18.60.3 SR2), decide what it hands DOWN, and dispatch to the group or the elementary arm.</summary>
    private void UsageInheritanceWalk(DataItem item, InheritedUsage inherited)
    {
        using var _ = Edition.At(item);

        // The usage this entry carries in its OWN right (see DeclaredUsageOf). GR1 is written over
        // "specified OR IMPLIED", so both spellings hand down.
        Usage? ownOrImplied = DeclaredUsageOf(item);

        // ── §13.18.60.3 SR2 ─────────────────────────────────────────────────────────────────────────────────
        // "If the USAGE clause is written in the data description entry for a group item, it may also be written
        // in the data description entry for any subordinate elementary item or group item, but the same usage
        // shall be specified in both entries." Compared against the NEAREST enclosing entry that wrote one, so a
        // chain G→H→A reports each adjacent contradiction once, at the entry whose clause has to change.
        // ⛔ "The same USAGE" is compared as the RESOLVED Usage member, not as the written word: ParseUsage has
        // already folded every spelling of one representation onto one member, which is exactly what SR6 asks
        // for ("COMP is an abbreviation for COMPUTATIONAL") and what this implementation's BINARY ≡ COMPUTATIONAL
        // identification (§13.18.60.4 GR4/GR6, both implementor-defined) means. `USAGE COMP` inside a
        // `USAGE BINARY` group is therefore the same usage in both entries and conforms.
        if (item.OwnUsage is { } ownWritten && inherited.Effective is { } fromGroup && ownWritten != fromGroup)
            Edition.Error(DiagnosticCatalog.UsageGroupContradiction, $"data item '{item.CobolName ?? "FILLER"}': "
                + $"USAGE {UsageFamilies.UsageWord(ownWritten)} contradicts the USAGE {UsageFamilies.UsageWord(fromGroup)} written for the group item "
                + $"'{inherited.FromName ?? "FILLER"}' to which it is subordinate — a USAGE clause may be written "
                + "in both entries, but the same usage shall be specified in both (ISO §13.18.60.3 SR2)");

        // What THIS entry hands down: its own (or implied) clause together with the representation entry bind
        // synthesized for it, else whatever it inherited, unchanged.
        // ⛔ The NO SIGN phrase travels with the clause it is part of, never on its own: an entry that writes a
        // USAGE clause REPLACES the inherited one whole (nearest-enclosing wins), so a PACKED-DECIMAL WITH NO SIGN
        // group containing a plain `05 H USAGE PACKED-DECIMAL.` group hands NO SIGN no further down — H's clause
        // is the one that applies to H's leaves (§13.18.60.4 GR1).
        var down = ownOrImplied is null && item.Pending is PicPending.None
            ? inherited
            : new InheritedUsage(ownOrImplied, item.PicIsUsageSynthesized ? item.Pic : null,
                                 item.Pending, item.CobolName, item.OwnNoSign);

        if (item.Children.Count > 0) UsageInheritanceGroup(item, down);
        else UsageInheritanceElementary(item, inherited);
    }

    /// <summary>The GROUP arm of the §13.18.60.4 GR1 walk: the GROUP-USAGE conformance rules (§13.18.29.3), the
    /// COBOL-2023 SYNCHRONIZED-on-a-group gate, and GR1's own "not to the group itself" — shed the profile entry
    /// bind synthesized here and hand it to the elementary items below.</summary>
    private void UsageInheritanceGroup(DataItem item, InheritedUsage down)
    {
        // GROUP-USAGE (ISO §13.18.29; D20/PB79) — SR1's forest-dependent halves, and SR2/SR3's "all subordinate
        // group items shall be explicitly or implicitly described with GROUP-USAGE BIT / NATIONAL": a
        // subordinate group inherits, one that declares the OTHER usage is a violation.
        if (item.GroupUsage is not GroupUsage.None)
        {
            string gu = item.GroupUsage is GroupUsage.Bit ? "BIT" : "NATIONAL";
            if (StrongTypeModel.IsStrongGroup(item))
                Edition.Error(DiagnosticCatalog.GroupUsageRule, $"data item '{item.CobolName ?? "FILLER"}': the "
                    + "GROUP-USAGE clause may be specified only if the subject of the entry is not strongly typed "
                    + "(ISO §13.18.29.3 SR1)");
            if (ReferenceResolver.HasVariableLengthSubordinate(item))
                Edition.Error(DiagnosticCatalog.GroupUsageRule, $"data item '{item.CobolName ?? "FILLER"}': the "
                    + "GROUP-USAGE clause may be specified only if the subject of the entry is not a "
                    + "variable-length group (ISO §13.18.29.3 SR1; §8.5.1.12)");
            foreach (var c in item.Children)
            {
                if (c.Children.Count == 0) continue;
                if (c.GroupUsage is GroupUsage.None) c.GroupUsage = item.GroupUsage;   // implied (SR2/SR3)
                else if (c.GroupUsage != item.GroupUsage)
                {
                    using var __c = Edition.At(c);
                    Edition.Error(DiagnosticCatalog.GroupUsageRule, $"data item '{c.CobolName ?? "FILLER"}': a group "
                        + $"subordinate to a GROUP-USAGE {gu} group shall itself be GROUP-USAGE {gu}, explicitly or "
                        + $"implicitly — not GROUP-USAGE {(c.GroupUsage is GroupUsage.Bit ? "BIT" : "NATIONAL")} "
                        + $"(ISO §13.18.29.3 {(item.GroupUsage is GroupUsage.Bit ? "SR2" : "SR3")})");
                }
            }
        }
        // SYNCHRONIZED on a GROUP item is a COBOL-2023 introduction (ISO §E.3.2 item 6 — "This clause may
        // now be specified for a group level data item"; §13.18.55 is the clause itself). It routes
        // through the CANONICAL funnel like every other introduction, so it is a hard error on BOTH
        // axes: §4.2.2's warning mechanism reports violations of the standard, and --permissive is the
        // migration mode for constructs an edition REMOVED — it has no meaning for one the targeted
        // edition has not yet acquired, which no pre-existing program can legally contain (CA14,
        // owner-approved option (a); this was the sole site routing an introduction through the
        // removed-severity seam, contradicting the compiler's own single-policy contract).
        if (item.Synchronized && Edition.DialectLevel < 2023)
            ConstructRegistry.Check(Edition.Edition, Edition.Sink, Constructs.SyncOnGroup2023,
                $"data item '{item.CobolName ?? "FILLER"}'");

        // ── §13.18.60.4 GR1, the "and not to the group itself" half ──────────────────────────────────────────
        // Entry bind could not yet tell a PICTURE-less elementary item from a group header — the subordinate
        // entries were not parsed — so it synthesized the usage's representation onto this entry optimistically.
        // The entry turned out to be a group, so that representation was never this entry's: shed it here, and
        // `down` (captured before this point) carries it to each elementary item below. Likewise the deferred
        // NATIONAL/BIT mark, whose SR5/SR12 adjudication is now the leaves' — one per leaf, at the leaf's own
        // position, through the same screen the written-clause spelling runs.
        if (item.PicIsUsageSynthesized)
        {
            item.Pic = null;
            item.PicIsUsageSynthesized = false;
        }
        item.Pending = PicPending.None;

        foreach (var c in item.Children) UsageInheritanceWalk(c, down);
    }

    /// <summary>The ELEMENTARY arm of the §13.18.60.4 GR1 walk — where the invariant is made true: an item that
    /// acquired its usage from a group binds as though it had written the clause itself.</summary>
    private void UsageInheritanceElementary(DataItem item, InheritedUsage fromGroup)
    {
        string name = item.CobolName ?? "FILLER";

        // A GROUP-USAGE entry with no subordinates is not a group at all (§13.18.29.3 SR1) — the implied
        // usage's own Pending mark is consumed here so the picture-less-usage arm below does not double-report.
        if (item.GroupUsage is not GroupUsage.None)
        {
            Edition.Error(DiagnosticCatalog.GroupUsageRule, $"data item '{name}': the GROUP-USAGE "
                + "clause may be specified only if the subject of the entry is a group item — this entry has no "
                + "subordinate entries (ISO §13.18.29.3 SR1)");
            item.GroupUsage = GroupUsage.None;
            item.Pending = PicPending.None;
            item.Pic = PicInfo.Recovery();
            return;
        }

        // THE ONE DERIVATION: the item's own USAGE clause, else the nearest enclosing group's (§13.18.60.4 GR1),
        // else §13.18.60.3 SR13b's implied DISPLAY. `acquired` is the GR1 half — the half that had no screens.
        bool acquired = item.OwnUsage is null && item.Pending is PicPending.None && fromGroup.Effective is not null;
        Usage effective = item.OwnUsage ?? (acquired ? fromGroup.Effective!.Value : Usage.Display);

        // §13.16.3 SR10 — "The VALUE clause shall not be specified for data items of class index, message-tag,
        // object, or pointer" — for the usage the item ACQUIRED. The class is the item's whichever way the usage
        // reached it, so `01 G USAGE INDEX. 05 A VALUE 1.` is the same violation as `05 A USAGE INDEX VALUE 1.`,
        // with the same diagnostic BindEntry reports for the written clause (kb/Work PB515: the acquired spelling
        // compiled clean and seeded the index item). The value is dropped after the report, as there.
        if (acquired && UsageFamilies.AdmitsNoValueLiteral(effective)
            && (_valueClauseWritten.Contains(item) || item.RawValue is not null || item.TableValues is not null))
        {
            Edition.Error(DiagnosticCatalog.ValueOnNonLiteralUsage, $"data item '{name}': the VALUE clause shall not "
                + $"be specified with USAGE {UsageFamilies.UsageWord(effective)}, inherited from its group "
                + $"'{fromGroup.FromName ?? "FILLER"}' ({UsageFamilies.NoValueClauseRule(effective)}; §13.18.60.4 GR1)");
            item.RawValue = null;
            item.TableValues = null;
        }

        if (item.Pic is null)
        {
            // ── A PICTURE-LESS elementary item ────────────────────────────────────────────────────────────
            // USAGE NATIONAL / BIT is not among §13.16.3 SR8's picture-less usages, and §13.18.60.3 SR12 / SR5
            // each require a picture character-string of a named kind — so an item with either usage and no
            // PICTURE is nonconforming WHICHEVER WAY it got the usage. The recovery shape keeps the doomed emit
            // crash-free (the DEVLOG-597 pattern).
            var pendingUsage = item.Pending is not PicPending.None ? item.Pending
                : acquired ? fromGroup.Pending : PicPending.None;
            if (pendingUsage is not PicPending.None)
            {
                bool inheritedMark = item.Pending is PicPending.None;
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"data item '{name}': an elementary item "
                    + $"with USAGE {(pendingUsage is PicPending.BitUsage ? "BIT" : "NATIONAL")}"
                    + (inheritedMark ? $" inherited from its group '{fromGroup.FromName ?? "FILLER"}'" : "")
                    + " requires a PICTURE clause (ISO §13.18.60.4 — not a picture-less usage"
                    + (inheritedMark ? "; §13.18.60.4 GR1" : "") + ")");
                item.Pic = PicInfo.Recovery();
                item.Pending = PicPending.None;
                return;
            }
            // §13.18.60.4 GR1 + §13.16.3 SR8: the group's clause IS this item's clause, and an item of a
            // picture-less usage takes its whole representation from that clause — so the profile entry bind
            // synthesized on the group header (its width, its SIGNED/UNSIGNED phrase, its declared object class,
            // its endianness) belongs HERE. The profile is immutable and shared by every leaf under the group,
            // which is what makes `01 G USAGE INDEX. 05 A. 05 B.` two index items rather than one.
            if (acquired && fromGroup.Pictureless is { } profile)
            {
                item.Pic = profile;
                item.PicIsUsageSynthesized = true;
            }
            // A picture-less item whose effective usage REQUIRES a picture is §13.16.3 SR8's second sentence,
            // screened once by the CheckPictureRequired closing guard — the same hole with or without a
            // group-level clause, so screening it here would write the rule down in the wrong place. Its SR9
            // exception has already been applied: SynthesizeImpliedPictures ran one pass earlier, so an entry
            // whose VALUE clause implies a PICTURE reaches this walk with one (DataBinder.ImpliedPicture.cs).
            return;
        }

        // ── A PICTURE-BEARING elementary item ────────────────────────────────────────────────────────────────
        // An entry that wrote its OWN usage clause was bound and screened by BindEntry, so only the acquired
        // half needs the representation work; the WITH NO SIGN phrase below is the ONE rule both spellings
        // still owe, and it runs for both (§13.18.60.4 GR1's equivalence, kb/Work PB570).
        if (acquired)
        {
            // §13.16.3 SR8: for the picture-less usages a PICTURE is prohibited, and the item's representation
            // comes from the usage. Identical verdict, identical recovery and identical diagnostic code to the
            // spelling that writes the clause on this entry (BindEntry's SR8 screen) — the ONE thing GR1 forbids
            // is that the two spellings differ.
            if (UsageFamilies.IsPictureless(effective))
            {
                (string sr8Code, string sr8Text) = Sr8PicturelessVerdict(effective);
                Edition.Error(sr8Code, $"data item '{name}': {sr8Text} — the usage is the one written for the group "
                    + $"item '{fromGroup.FromName ?? "FILLER"}' and applies to each elementary item in it "
                    + "(ISO §13.18.60.4 GR1)");
                item.Pic = fromGroup.Pictureless ?? PicInfo.Recovery();
                item.PicIsUsageSynthesized = fromGroup.Pictureless is not null;
                return;
            }

            ApplyEffectiveUsage(item, effective);
        }

        // ── The USAGE clause's WITH NO SIGN phrase (ISO §13.18.60.2), for BOTH spellings ─────────────────────
        // Its subject is "the subject of the entry" the phrase applies to (§13.18.40.3 SR31), and §13.18.60.4
        // GR1 makes that each elementary item in the group when the clause was written at a group level. Run
        // AFTER ApplyEffectiveUsage so the screened representation is the one the rule reads.
        if (item.OwnNoSign) ApplyEffectiveNoSign(item, fromGroupName: null);
        else if (acquired && fromGroup.NoSign) ApplyEffectiveNoSign(item, fromGroup.FromName ?? "FILLER");
    }

    /// <summary>Apply a USAGE clause's <c>WITH NO SIGN</c> phrase to an elementary item — ISO §13.18.40.3 SR31
    /// ("The symbol 'S' shall not be specified in character-string-1 when the NO SIGN phrase of the USAGE Clause
    /// is specified for the subject of the entry") and §13.18.60.4 GR11 ("If the WITH NO SIGN phrase is
    /// specified the representation of the data item in the storage of the computer reserves no storage for
    /// representing any sign value").
    /// <para>⛔ ONE site for every route by which the phrase reaches an item: the item's own USAGE clause, and
    /// §13.18.60.4 GR1 group inheritance (both from <see cref="UsageInheritanceElementary"/>), and the
    /// §13.18.49.4 GR3 SAME-AS ancestor transform, which records the phrase on <see cref="DataItem.OwnNoSign"/>
    /// and lets this pass adjudicate it. The phrase used to be adjudicated in <c>BindEntry</c> against the
    /// ENTRY'S OWN picture, where a group header has none — so the group spelling lost BOTH the SR31 screen and
    /// the GR11 width, silently, at every nesting depth (kb/Work PB570).</para>
    /// <para>The effective usage has already been screened against the picture by
    /// <see cref="ApplyEffectiveUsage"/>, so <c>Pic.Usage</c> is the representation the item really takes: a
    /// phrase that survived COBOLNET1565 on the written clause but whose PACKED-DECIMAL was downgraded by
    /// §13.18.60.3's USAGE × PICTURE screen changes nothing, exactly as the diagnosed usage requires. A RECOVERY
    /// profile is left alone — its PICTURE was already rejected, so SR31 would report an invented second defect
    /// on one entry.</para></summary>
    /// <param name="fromGroupName">The group entry whose clause supplied the phrase, for the §13.18.60.4 GR1
    /// provenance in the message, or <see langword="null"/> when this entry wrote it itself.</param>
    private void ApplyEffectiveNoSign(DataItem item, string? fromGroupName)
    {
        if (item.Pic is not { } pic || pic.IsRecovery || pic.Usage is not Usage.Packed) return;
        string gr1 = fromGroupName is null ? ""
            : $" — the phrase is the one written for the group item '{fromGroupName}', whose USAGE clause applies "
              + "to each elementary item in the group (ISO §13.18.60.4 GR1)";
        if (pic.Signed)
            Edition.Error("COBOLNET1566", $"data item '{item.CobolName ?? "FILLER"}': a PICTURE containing 'S' shall "
                + "not be specified with PACKED-DECIMAL WITH NO SIGN (ISO §13.18.40.3 SR31 — an unsigned "
                + $"representation){gr1}");
        else
            item.Pic = pic with { PackedNoSign = true };
    }

    /// <summary>Apply an elementary item's EFFECTIVE usage to its analyzed PICTURE — the §13.18.60.3
    /// USAGE × PICTURE screen (SR3/SR5/SR12/SR20, shared verbatim with the written-clause path) followed by the
    /// representation rewrite the screened usage calls for.
    /// <para>⛔ ONE site for every route by which an elementary item acquires a usage its own entry did not
    /// write: §13.18.60.4 GR1 group inheritance (<see cref="UsageInheritancePass"/>) and the §13.18.49.3 GR3
    /// SAME-AS ancestor transform. Both used to carry their own copy of a three-member
    /// <c>Binary or Packed or Comp5</c> hand-list over a <c>{ Numeric, !IsFloat, Display }</c> leaf shape, and
    /// each silently dropped every usage outside it (kb/Work PB495).</para>
    /// <para>A RECOVERY profile is left alone: its PICTURE was already rejected and the written-clause path
    /// never reached the screen either (<c>Analyze</c> returns before it), so screening it here would report a
    /// second, invented defect on one entry.</para></summary>
    private void ApplyEffectiveUsage(DataItem item, Usage effective)
    {
        if (item.Pic is not { } pic || pic.IsRecovery || pic.Usage == effective) return;
        var screened = PictureAnalyzer.ScreenUsageAgainstPicture(pic.Category, effective, explicitUsage: true,
            item.PictureText ?? "", Edition, $"data item '{item.CobolName ?? "FILLER"}'");
        if (screened != pic.Usage)
            item.Pic = pic with
            {
                Usage = screened,
                SignKind = PicInfo.SignKindFor(screened, pic.Signed, item.OwnSign),
            };
    }

    /// <summary>Apply an elementary item's EFFECTIVE SIGN clause to its analyzed PICTURE — the SIGN twin of
    /// <see cref="ApplyEffectiveUsage"/>, and the one <see cref="PicInfo.SignKindFor"/> recomputation.
    /// <para>⛔ ONE site for every route by which a signed numeric item acquires a SIGN clause its own entry did
    /// not write: §13.18.52.4 GR1 group inheritance (<see cref="InheritSignClauses"/>) and the §13.18.49.4 GR5
    /// SAME-AS ancestor transform (<c>ExpandSameAs</c>). Each carried its own copy of the guard, and the copies
    /// DID drift: when kb/Work PB646 widened the guard to the usage PAIR §13.18.52.3 SR2 names ("The usage of an
    /// elementary item for which the SIGN clause is specified shall be display or national") the SAME-AS copy
    /// was left at DISPLAY alone, so <c>01 TN USAGE NATIONAL SIGN IS LEADING SEPARATE. 05 TNE PIC S9(3).
    /// 01 SNX SAME AS TNE.</c> MEASURED <c>FUNCTION LENGTH</c> 3 / <c>BYTE-LENGTH</c> 6 and the over-punch image
    /// <c>04N</c> against the inheritance route's 4 / 8 / <c>-045</c> for the very same description — though
    /// GR5 names "an alphanumeric group item, national group item, or strongly-typed group item" explicitly.
    /// <see cref="PicInfo.SignKindFor"/> owns what each usage's sign representation IS; this owns WHEN an
    /// inherited clause reaches it.</para></summary>
    private static void ApplyEffectiveSign(DataItem item, SignSpec sign)
    {
        if (item.Pic is { Category: PicCategory.Numeric, Signed: true, Usage: Usage.Display or Usage.National } pic)
            item.Pic = pic with { SignKind = PicInfo.SignKindFor(pic.Usage, signed: true, sign) };
    }

    /// <summary>Apply group-level SIGN clauses to subordinate signed numeric DISPLAY/NATIONAL items (ISO
    /// §13.18.52 GR1–3): a SIGN on a group applies to every signed numeric item subordinate to it, the NEAREST
    /// enclosing clause takes precedence, and an item's OWN clause (already consumed by
    /// <see cref="PictureAnalyzer.Analyze"/> at entry bind) wins outright. Runs BEFORE the REDEFINES
    /// classification pass because a SEPARATE sign occupies its own character position (§13.18.52 GR6a) — it
    /// widens the item's image, which feeds the class-max width.</summary>
    internal void InheritSignClauses()
    {
        static void Walk(DataItem item, SignSpec? inherited)
        {
            SignSpec? effective = item.OwnSign ?? inherited;
            // GR1 applies a group's clause to "each numeric item subordinate to the group", with no usage
            // condition of its own; ApplyEffectiveSign holds the §13.18.52.3 SR2 usage pair, once.
            if (item.OwnSign is null && effective is not null) ApplyEffectiveSign(item, effective);
            foreach (var c in item.Children) Walk(c, effective);
        }
        foreach (var root in Roots) Walk(root, null);
    }

    // ── REDEFINES / RENAMES resolution + classification (post-build, ISO §13.18.44/45) ───────────────────────

    /// <summary>Resolve each item's REDEFINES target name to its <see cref="DataItem"/>, and each level-66 RENAMES
    /// FROM/THRU operand to its item. A REDEFINES target is an unqualified prior entry in the same scope (SR1/SR6); a
    /// RENAMES range names items within the owning record (SR3). Target resolution does not chase chains — the
    /// classification pass walks <see cref="DataItem.RedefinesTarget"/> transitively to the anchor (SR11).</summary>
    internal void ResolveRedefines()
    {
        static DataItem RootOf(DataItem d) { while (d.Parent is { } p) d = p; return d; }
        foreach (var item in AllItems())
            if (item.RedefinesTargetName is { } tname)
            {
                using var _ = Edition.At(item);
                // A subordinate (02+) redefiner scopes to its own siblings (correct in every scope). A top-level
                // 01/77 method redefiner must scope to the OWNING METHOD's own roots (§13.18.44.3 SR — the target
                // is a prior sibling in the same data description; a method may NOT redefine object/program data,
                // §11.7.4), NEVER the cross-scope global `Roots` pool (M2-OO-1h step 3).
                IReadOnlyList<DataItem> scope;
                if (item.Parent is { } par)
                    scope = par.Children;
                else if (OoRootOwner.TryGetValue(RootOf(item), out var mm))
                    // A top-level method redefiner scopes to its OWN section only (§13.18.44.3 SR — the target is a
                    // preceding item in the SAME data description; cross-section LOCAL↔LINKAGE aliasing is illegal,
                    // and their storage classes differ — review B). RootOf(item)
                    // == item here (Parent is null).
                    scope = mm.Binding!.LocalRoots.Contains(item) ? mm.Binding!.LocalRoots
                          : mm.Binding!.LinkageRoots;
                else
                    scope = Roots;
                // §13.18.44.3 SR4/SR7 + NOTE 1: data-name-2 is a PRECEDING entry at the same level, and when the name is
                // not unique "no ambiguity of reference exists because of the required placement" — the NEAREST preceding
                // same-named sibling. The former whole-scope FirstOrDefault admitted a LATER sibling (illegal source, kb/Work
                // PB93) and picked the FIRST of duplicates.
                item.SetRedefinition(scope.TakeWhile(s => !ReferenceEquals(s, item))
                    .LastOrDefault(s => string.Equals(s.CobolName, tname, StringComparison.OrdinalIgnoreCase)),
                    RedefinitionKind.Clause);
                // A method 01 REDEFINES whose target isn't in the method's own roots is a scope error (never a
                // silent cross-scope bind to an object/program item) — §13.18.44.3 SR.
                if (item.RedefinesTarget is null && item.Parent is null && OoRootOwner.ContainsKey(RootOf(item)))
                    // 1577, renumbered from a bare "COBOLNET1518" that collided with the locale-module
                    // non-support meaning (review V11 — the code comes from the catalog descriptor, never a literal).
                    Edition.Error(DiagnosticCatalog.MethodRedefinesScope,
                        $"REDEFINES target '{tname}' of method data item "
                        + $"'{item.CobolName ?? "FILLER"}' is not a preceding item in the same method scope "
                        + "(ISO §13.18.44.3 — a method item may not redefine object or program data)");
                else if (item.RedefinesTarget is null)
                    // kb/Work PB93: the PROGRAM-scope miss used to be silent — the item kept RedefinesTargetName (so
                    // BitLayout / ImageWidth / ByteWidth skipped it as an overlay) against a null RedefinesTarget (so
                    // the emitter gave it its own field): a storage shape no edition defines. ONE diagnostic; the
                    // item then binds as an ORDINARY entry (the name cleared below) so no consumer sees the half-state.
                    Edition.Error(DiagnosticCatalog.RedefinesTargetUnresolved,
                        $"'{item.CobolName ?? "FILLER"}' REDEFINES '{tname}': data-name-2 does not name a preceding "
                        + "entry in the same scope (ISO §13.18.44.3 SR4/SR7/SR10; §8.4.2.1)");
                if (item.RedefinesTarget is null)
                    item.RedefinesTargetName = null;   // no half-state: an unresolved redefiner is an ordinary entry
                // §13.18.44.3 SR7 (kb/Work PB93 sweep): data-name-2 shall be the entry that ORIGINALLY defined the area
                // — a redefiner naming a redefiner is a chain ISO forbids and the field's vendors accept: error strict,
                // warning under --permissive with the chain semantics (ClassifyRedefinesClasses chases the anchor).
                if (item.RedefinesTarget is { RedefinesTargetName: not null } viaRedefiner)
                    Edition.Removed(DiagnosticCatalog.RedefinesOfRedefinition.Code,
                        $"'{item.CobolName ?? "FILLER"}' REDEFINES '{viaRedefiner.CobolName ?? "FILLER"}', which is itself "
                        + "a redefinition — data-name-2 shall be the entry that originally defined the storage area "
                        + "(ISO §13.18.44.3 SR7)");
                // §13.18.44.3 SR16: data-name-2 (the redefined item) shall not be described with the ANY
                // LENGTH clause — a runtime-length item has no fixed storage area a redefiner could overlay.
                if (item.RedefinesTarget is { IsAnyLength: true })
                    Edition.Error("COBOLNET1542", $"'{item.CobolName ?? "FILLER"}' REDEFINES "
                        + $"'{tname}': the redefined item is described with the ANY LENGTH clause "
                        + "(ISO §13.18.44.3 SR16 — data-name-2 shall not be ANY LENGTH)");
                // §13.18.44.3 SR8 (the P5.8 spec find): the SUBJECT's storage area shall not be larger than
                // data-name-2's, unless data-name-2 is a level-1 item (without the EXTERNAL clause — that
                // residue is unmodeled; a level-1 FILE SECTION entry cannot carry REDEFINES at all, SR3, so
                // the exception is a WORKING-STORAGE/LOCAL/LINKAGE 01). Previously accepted SILENTLY — the
                // classifier took the class-max width, giving the overlay byte-position semantics NO edition
                // defines. The extent is the full OCCURS allocation (the "storage area required").
                // ⛔ MEASURED IN BYTES, WHICH IS WHAT "storage area" MEANS (kb/Work PB231). SR8 compares
                // STORAGE AREAS and §13.18.44.4 GR1 states the association in bits; character positions are the
                // same number for every leaf kind but NATIONAL, whose §13.18.60.4 GR8 size this implementation
                // pins at two bytes per position (D-N1). Read in positions, `01 G. 05 A PIC X(4).
                // 05 B REDEFINES A PIC N(4).` compared 4 against 4 and was ACCEPTED while B requires eight
                // bytes over a four-byte A — an under-rejection that then handed the class a backing half the
                // size of one of its own views.
                if (item.RedefinesTarget is { } tgt && tgt.Level != 1
                    && item.ByteWidth * (item.Occurs ?? 1) > tgt.ByteWidth * (tgt.Occurs ?? 1))
                    Edition.Error("COBOLNET1539", $"'{item.CobolName ?? "FILLER"}' REDEFINES "
                        + $"'{tgt.CobolName ?? "FILLER"}': the redefining storage area "
                        + $"({item.ByteWidth * (item.Occurs ?? 1)} bytes) is larger than the redefined "
                        + $"({tgt.ByteWidth * (tgt.Occurs ?? 1)}) — permitted only when the redefined item is "
                        + "level 1 (ISO §13.18.44.3 SR8)");
            }

        foreach (var root in Roots)
            foreach (var ren in root.Renames66)
            {
                var info = ren.Renames!;
                // SR4 confines both operands to THIS record, so the candidate set is the record's; it is COUNTED
                // (kb/Work PB978) — two same-named items there are §8.4.2.2.3 SR1's ambiguity, not the first found.
                // An operand the capture REFUSED (§13.18.45.3 SR7 / §8.4.2.2.2) was reported there — one fault, one verdict.
                if (_refusedClauseOperands.Contains(info.FromName)
                    || (info.ThruName is { } rt && _refusedClauseOperands.Contains(rt))) continue;
                using var __q = Edition.At(ren);
                string face = $"'{ren.CobolName ?? "FILLER"}' RENAMES";
                info.From = UniqueOrReportAmbiguous(SubtreeCandidates(root, info.FromName, info.FromQualifiers), face,
                    WrittenQualified(info.FromName, info.FromQualifiers), out bool fromAmbiguous);
                bool thruAmbiguous = false;
                info.Thru = info.ThruName is { } t
                    ? UniqueOrReportAmbiguous(SubtreeCandidates(root, t, info.ThruQualifiers), face,
                        WrittenQualified(t, info.ThruQualifiers), out thruAmbiguous)
                    : null;
                if (fromAmbiguous || thruAmbiguous) continue;
                if (info.From is null || (info.ThruName is not null && info.Thru is null))
                {
                    // kb/Work PB93: an operand naming nothing in the record was skipped silently — the alias then had
                    // no picture and no span, and every reference to it failed downstream with an unrelated message.
                    using var __r = Edition.At(ren);
                    string missing = info.From is null ? info.FromName : info.ThruName!;
                    Edition.Error(DiagnosticCatalog.RenamesOperandUnresolved,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName}{(info.ThruName is { } tn ? " THRU " + tn : "")}: "
                        + $"'{missing}' does not name an item of the record (ISO §13.18.45.3 SR4; §8.4.2.1)");
                    continue;
                }
                // §13.18.45.3 SR8 — WHAT THE RANGE MAY HOLD (kb/Work PB1284), asked before either form binds: a
                // refused range never reaches the no-THRU forwarding or the tiler, which would otherwise model the
                // alias as a fixed window over storage that has no fixed extent.
                if (RenamesRangeFault(root, info.From, info.Thru) is { } rangeFault)
                {
                    using var __s = Edition.At(ren);
                    Edition.Error(DiagnosticCatalog.RenamesRangeContent,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName}{(info.ThruName is { } tn8 ? " THRU " + tn8 : "")}: "
                        + $"{rangeFault}; none of the items within the range, including data-name-2 and data-name-3, "
                        + "shall be of class object, message-tag, or pointer, a variable-length data item, or an "
                        + "occurs-depending table (ISO §13.18.45.3 SR8)");
                    continue;
                }
                // The no-THRU alias inherits the renamed item's description (§13.18.45 GR1) — the resolver
                // forwards to the FROM item's place; no span, no synthetic alphanumeric picture.
                if (info.Thru is null) { ren.Pic = info.From.Pic; continue; }

                // The alias is the record's STORAGE WINDOW from data-name-2's first character to data-name-3's last
                // (§13.18.45.4 GR1/GR2 — a re-grouping of the record's characters; the alias reads/writes as one
                // elementary ALPHANUMERIC item of the window's width). kb/Work PB96: the former leaf-run walk listed
                // every leaf between FROM and THRU — a REDEFINES view included, as if it occupied its own characters —
                // so `RENAMES A THRU C` over A / B REDEFINES A / C was 6 characters, not 4. Offsets now come from the
                // ONE recursive storage function (a redefiner sits at its target's offset, §13.18.44), the parts are
                // the record's NON-redefining leaves that intersect the window, and a boundary inside a leaf (a FROM /
                // THRU that partially redefines it) is a partial part.
                int Width(DataItem d) => d.ByteWidth * (d.Occurs ?? 1);   // STORAGE bytes: a national position is two (D-N1; kb/Work PB1665)
                int Offset(DataItem d)
                {
                    if (d.RedefinesTarget is { } tgt) return Offset(tgt);          // an overlay starts where its target does
                    if (d.Parent is not { } par) return 0;                           // the record itself
                    int off = Offset(par);
                    foreach (var sib in par.Children)
                    {
                        if (ReferenceEquals(sib, d)) break;
                        if (sib.RedefinesTargetName is null) off += Width(sib);       // a redefining sibling adds no storage
                    }
                    return off;
                }
                int startOff = Offset(info.From);
                int endOff = Offset(info.Thru) + Width(info.Thru) - 1;
                if (endOff < startOff)
                {
                    using var __o = Edition.At(ren);
                    Edition.Error(DiagnosticCatalog.RenamesOperandUnresolved,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName} THRU {info.ThruName}: data-name-3 ends before "
                        + "data-name-2 begins — the THRU item shall follow the FROM item in the record (ISO §13.18.45.4 GR2)");
                    continue;
                }
                // §13.18.45.3 SR10 — "The area described by data-name-2 THROUGH data-name-3 shall define an integral
                // number of bytes" (kb/Work PB1284). Only a USAGE BIT item can make it fractional: §8.5.1.6.3 packs
                // same-level bit items into shared bytes, so `A PIC 1(3) USAGE BIT` THRU `B PIC 1(2) USAGE BIT` is a
                // 5-bit area the character offsets above cannot even see. Measured on THE ONE bit layout
                // (BitLayout), and only for a record that holds a bit leaf — without one every extent is whole
                // characters by construction.
                if (root.HasBitDescendant
                    && BitLayout.StartBitOf(root, info.From) is var fromBit and >= 0
                    && BitLayout.StartBitOf(root, info.Thru) is var thruBit and >= 0
                    && (thruBit + BitLayout.RunBits(info.Thru) - fromBit) is var areaBits
                    && areaBits % BitLayout.BitsPerCharacter != 0)
                {
                    using var __b = Edition.At(ren);
                    Edition.Error(DiagnosticCatalog.RenamesAreaNotWholeBytes,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName} THRU {info.ThruName}: the area is "
                        + $"{areaBits} bits, which is not a whole number of bytes; the area described by data-name-2 "
                        + "THROUGH data-name-3 shall define an integral number of bytes (ISO §13.18.45.3 SR10)");
                    continue;
                }
                // Every leaf of the record with its storage extent — REDEFINES views INCLUDED: a view reads the
                // storage it overlays exactly as the redefined entry does (NC252A's `RDF8-5 THRU RDF8-6` lies inside a
                // double redefinition of an OCCURS 36 table, and only those two views tile that window). Views are
                // never counted twice because the tiling below advances by what it covered.
                var leaves = new List<(DataItem Leaf, int Off, int W, int Occ)>();
                void Walk(DataItem n)
                {
                    if (n.IsElementary) { if (Width(n) > 0) leaves.Add((n, Offset(n), n.ByteWidth, n.Occurs ?? 1)); return; }
                    foreach (var c in n.Children) Walk(c);
                }
                Walk(root);
                // GREEDY TILING of [startOff, endOff]: at each position take the LONGEST whole leaf (or whole table
                // cell) that starts exactly there and fits inside the window; else the most specific (narrowest) leaf
                // cell containing the position, as a partial slice up to the window's end or the cell's end.
                int pos = startOff;
                bool stuck = false;
                while (pos <= endOff)
                {
                    (DataItem Leaf, int? Occ, int Start, int Len, int Cover)? best = null;
                    foreach (var (leaf, off, w, occ) in leaves)
                    {
                        int total = w * occ;
                        if (pos < off || pos > off + total - 1) continue;          // the leaf does not contain pos
                        // the whole leaf (every occurrence) starting exactly here and fitting the window
                        if (off == pos && off + total - 1 <= endOff)
                            Consider((leaf, null, 1, total, total));
                        int k = (pos - off) / w + 1;                                // the occurrence containing pos
                        int cellLo = off + (k - 1) * w, cellHi = cellLo + w - 1;
                        if (cellLo == pos && cellHi <= endOff)
                            Consider((leaf, occ == 1 ? null : k, 1, w, w));       // a whole cell (or the whole 1-occurrence leaf)
                        else
                        {
                            int to = Math.Min(endOff, cellHi);                     // a partial slice of the containing cell
                            Consider((leaf, k, pos - cellLo + 1, to - pos + 1, to - pos + 1));
                        }
                    }
                    void Consider((DataItem Leaf, int? Occ, int Start, int Len, int Cover) c)
                    {
                        // prefer: whole (non-partial) over partial; then the longest cover; then the narrowest leaf
                        bool cPartial = c.Occ is not null && (c.Start != 1 || c.Len != c.Leaf.ByteWidth);
                        bool bPartial = best is { } b0 && b0.Occ is not null && (b0.Start != 1 || b0.Len != b0.Leaf.ByteWidth);
                        if (best is null
                            || (bPartial && !cPartial)
                            || (bPartial == cPartial && c.Cover > best.Value.Cover)
                            || (bPartial == cPartial && c.Cover == best.Value.Cover && c.Leaf.ByteWidth < best.Value.Leaf.ByteWidth))
                            best = c;
                    }
                    if (best is not { } chosen || chosen.Cover <= 0) { stuck = true; break; }
                    // The tiling ran in STORAGE bytes; a part is kept in its leaf's own positions. A national leaf's boundary
                    // inside a position (an odd byte) is no whole character of that item - nothing can address it.
                    if (RenamesSpanPart.FromBytes(chosen.Leaf, chosen.Occ, chosen.Start, chosen.Len) is not { } spanPart)
                    { stuck = true; break; }
                    info.Span.Add(spanPart);
                    pos += chosen.Cover;
                }
                if (stuck || info.Span.Count == 0)
                {
                    using var __u = Edition.At(ren);
                    Edition.Error(DiagnosticCatalog.RenamesOperandUnresolved,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName} THRU {info.ThruName}: the record's leaves do "
                        + "not tile the alias's storage window (kb/Work PB96)");
                    info.Span.Clear();
                    continue;
                }
                ren.Pic = new PicInfo(PicCategory.Alphanumeric, Usage.Display,
                    Length: endOff - startOff + 1, Digits: 0, Scale: 0, Signed: false);
            }
    }

    /// <summary>The first §13.18.45.3 SR8 violation among the items WITHIN a RENAMES range, as the clause of the
    /// diagnostic naming the item and its shape, or null (kb/Work PB1284). SR8: "None of the items within the range,
    /// including data-name-2 and data-name-3, if specified, shall be of class object, message-tag, or pointer, a
    /// strongly-typed group item, an item subordinate to a strongly-typed group item, a variable-length data item, or
    /// an occurs-depending table."
    /// <para>THE RANGE is §13.18.45.4 GR2's: every entry from data-name-2 through the last entry of data-name-3's
    /// subtree, in declaration order — groups and elementary items alike, a REDEFINES view among them — or, without
    /// THROUGH, data-name-2 and its subordinates (GR1). It is enumerated over the DECLARATION tree, not over storage
    /// offsets, because the offending shapes are exactly the ones storage offsets cannot see: a dynamic-length item
    /// has zero fixed width, and a dynamic-capacity or occurs-depending table's extent is not its declared one.</para>
    /// <para>The strongly-typed arms are enforced with the rest of the strong-type rules they share a sentence
    /// with, <c>CheckStrongTypeDeclarations</c> (§13.18.57.3 SR3, COBOLNET1532); this screen owns the other three.
    /// The class arm reads §13.18.60.3 SR14's phrase reader, whose five phrases are exactly the usages of classes
    /// object (OBJECT REFERENCE), message-tag (MESSAGE-TAG) and pointer (POINTER, PROGRAM-POINTER,
    /// FUNCTION-POINTER).</para></summary>
    private static string? RenamesRangeFault(DataItem record, DataItem from, DataItem? thru)
    {
        var ordered = new List<DataItem>();
        void PreOrder(DataItem n) { ordered.Add(n); foreach (var c in n.Children) PreOrder(c); }
        PreOrder(record);
        int first = ordered.IndexOf(from);
        var last = thru ?? from;
        int lastAt = ordered.IndexOf(last);
        if (first < 0 || lastAt < 0) return null;   // not in this record — reported by the operand resolution
        int end = lastAt;
        while (end + 1 < ordered.Count && OdoModel.IsWithin(ordered[end + 1], last)) end++;   // last's subtree
        for (int i = first; i <= end; i++)
        {
            var d = ordered[i];
            string name = $"'{d.CobolName ?? "FILLER"}'";
            if (VariableLengthCompatibility.IsVariableLengthDataItem(d))
                return $"{name} is a variable-length data item — "
                    + (d.IsDynamicTable ? "a dynamic-capacity table" : "a dynamic-length elementary item")
                    + " (ISO §8.5.1.11.1)";
            if (d.OccursSpec is { DependingName: not null })
                return $"{name} is an occurs-depending table";
            if (!d.IsGroup
                && (ItemCategory.Sr14PhraseOf(d.Pic?.Usage) ?? ItemCategory.Sr14PhraseOf(d.OwnUsage)) is { } phrase)
                return $"{name} is described with USAGE {phrase} — of class " + phrase switch
                {
                    "OBJECT REFERENCE" => "object",
                    "MESSAGE-TAG" => "message-tag",
                    "POINTER" or "PROGRAM-POINTER" or "FUNCTION-POINTER" => "pointer",
                    _ => throw new System.Diagnostics.UnreachableException(
                        $"§13.18.60.3 SR14 phrase '{phrase}' has no §8.5.2 class in the RENAMES SR8 screen"),
                };
        }
        return null;
    }

    /// <summary>Group every redefining entry with the non-redefining anchor it ultimately overlays (SR7/SR11) into a
    /// <see cref="RedefinesClass"/>, mark the anchor canonical and every other member a view, then assign the class a
    /// tier (D &gt; C &gt; B &gt; A) and its class-max width, and propagate view-suppression to each view's
    /// subordinates (SR9 — no VALUE on a subordinate of a redefiner). (COBOLNET_DESIGN §4.2.)</summary>
    internal void ClassifyRedefinesClasses()
    {
        var byAnchor = new Dictionary<DataItem, RedefinesClass>();
        foreach (var item in AllItems())
        {
            if (item.RedefinesTarget is null) continue;
            using var _ = Edition.At(item);
            // ⛔ THE CLAUSE SCREENS SCREEN THE CLAUSE (kb/Work PB836). SR5, SR12, SR14 and SR17 below are rules about
            // a written REDEFINES clause — its subject and its data-name-2 operand. An FD/SD's later level-1 records
            // (§13.18.33.4 GR3) and a SAME RECORD AREA file's record (§12.4.6.4.4 GR2) share the area IMPLICITLY:
            // no clause, no operand, and none of these rules. Their own screens are the implicit-area arm in the
            // class loop below and §13.18.57.3 SR4's implicit arm in CheckStrongTypeDeclarations.
            if (item.RedefinesKind is RedefinitionKind.Clause)
            {
                // §13.18.44.3 SR12/SR14 (kb/Work PB179; the skeptic round moved this screen HERE, per WRITTEN
                // ENTRY and BEFORE dissolution — the dissolve loop below removes nested classes, so a per-class
                // screen let an inner entry's violation escape the diagnostic and fall to the outer class's
                // staged-loud arm: same verdict, wrong posture): SR12 bars the SUBJECT being of class
                // object/message-tag/pointer or a strongly-typed group; SR14 bars data-name-2 (the DIRECT
                // target as written) likewise, plus "subordinate to a strongly-typed group item". These are the
                // rules' LETTER — the drafting contrast with SR9's "nor any entry subordinate to it" shows they
                // name the entry-level items only; a NESTED pointer leaf is §13.18.60.3 SR14's territory — now
                // screened at its own declaration by CheckUsageDeclarations (kb/Work PB183, COBOLNET1724), with
                // ComputeTier's backstop arm kept as the recovery-path guard behind it.
                if (Sr12Sr14Violation(item, item.RedefinesTarget) is { } srv)
                    Edition.Error(DiagnosticCatalog.RedefinesPointerObject, srv);
                // §13.18.44.3 SR17, the SAME per-written-entry posture and for the same reason (kb/Work PB177 arm C):
                // "Neither data-name-2 nor the subject of the entry shall be a variable-length group or a
                // dynamic-length elementary item." A SYMMETRIC rule, so BOTH sides are tested — the two-arm
                // discipline applied to the screen itself. It must run HERE, before the dissolve loop, or a nested
                // entry's violation escapes into the outer class's staged-loud arm (the PB179 skeptic round's
                // finding, which is why SR12/SR14 sit here).
                // ⛔ AND IT CLOSES A SILENT MIS-MODEL, not just an under-rejection: StorageFormPass.Classify returns
                // DynamicString for an IsDynamicLength item BEFORE reaching its Tier-B view arm, so such a view got
                // its OWN disjoint native string — two storages for one shared area (§13.18.44.4 GR1 says one), with
                // no diagnostic. Rejecting the entry makes that path unreachable.
                foreach (var (side, sideItem) in new[]
                         { ("the subject of the REDEFINES entry", item), ("data-name-2 of a REDEFINES entry", item.RedefinesTarget) })
                    if (VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup(sideItem) is { } shape)
                        Edition.Error(DiagnosticCatalog.RedefinesVariableLength,
                            $"'{sideItem.CobolName ?? sideItem.CsName}' is {side} but is {shape}: neither "
                            + "data-name-2 nor the subject of the entry shall be a variable-length group or a "
                            + "dynamic-length elementary item (ISO §13.18.44.3 SR17)");
                // §13.18.44.3 SR5 SENTENCE 1 — the OBJECT side, per written entry like its two neighbours above.
                // "The data description entry for data-name-2 shall not contain an OCCURS clause." ANY format: the
                // fixed OCCURS of Format 1, Format 2's occurs-depending table, and Format 4's dynamic-capacity table
                // are all THE OCCURS CLAUSE (§13.18.38), so the predicate is `IsTable` — the union. ⛔ NEITHER half
                // alone works, and each is a live trap this repo has already sprung once: `Occurs is not null` is
                // the FIXED physical capacity and is NULL for a Format-4 table (the CONTROL SR3 arm's defect), while
                // `OccursSpec is not null` is NULL for a plain keyless fixed table, which OdoBindOccursSpec
                // deliberately leaves allocation-free (measured: with that spelling `05 T PIC X(3) OCCURS 4. 05 R
                // REDEFINES T PIC X(12).` still compiled clean — the very shape this screen exists for).
                // ⛔ SENTENCE 2 IS THE LIMIT OF THIS ARM: "However, data-name-2 may be subordinate to an item whose
                // data description entry contains an OCCURS clause." So the test is on data-name-2's OWN entry —
                // an ancestor walk here would reject legal source.
                // ⛔ AND THE DEPENDING SHAPE IS DELIBERATELY EXCLUDED, so one entry never draws two diagnostics from
                // one rule: SENTENCE 4 ("Neither the original definition nor the redefinition shall include an
                // occurs-depending table") is COBOLNET0855's, whose population is WIDER (an ODO table anywhere in
                // either definition, not merely on data-name-2's own entry) and which already rejects every entry
                // this arm would also match. One rule, four sentences, two disjoint screens.
                if (item.RedefinesTarget.IsTable && item.RedefinesTarget.OccursSpec?.DependingName is null)
                    Edition.Error(DiagnosticCatalog.RedefinesTargetOccurs,
                        $"'{item.CobolName ?? item.CsName}' REDEFINES '{item.RedefinesTarget.CobolName ?? item.RedefinesTarget.CsName}', "
                        + "whose data description entry contains an OCCURS clause: the data description entry for "
                        + "data-name-2 shall not contain an OCCURS clause (ISO §13.18.44.3 SR5) — data-name-2 may be "
                        + "SUBORDINATE to an item that has one, but shall not carry one itself");
            }
            DataItem anchor = item;
            // Chase data-name-2 to the non-redefining anchor. A CHAIN (X REDEFINES Y, Y REDEFINES Z) is
            // SR7-ILLEGAL (the skeptic round corrected this comment's SR11 miscitation — SR11 is the
            // nested-under-a-redefinition PERMISSION) and diagnosed at resolve; it reaches here only under
            // --permissive, where the anchor walk still yields one shared area.
            while (anchor.RedefinesTarget is { } t) anchor = t;
            if (!byAnchor.TryGetValue(anchor, out var cls))
            {
                cls = new RedefinesClass { Canonical = anchor };
                cls.Members.Add(anchor);
                anchor.Class = cls;
                byAnchor[anchor] = cls;
            }
            cls.Members.Add(item);
            item.Class = cls;
            item.IsCanonical = false;
        }

        // ONE shared area per overlay nest (ISO §13.18.44 — a REDEFINES nested under another class's member
        // shares THAT storage): a class whose ANCHOR lies inside another class's subtree — its ancestor chain
        // crosses a redefining member or another class's anchor (NC252A's `RDEF8 REDEFINES RDF8` under the view
        // `REDEF11 REDEFINES REDEF10`) — DISSOLVES into the outer class. The outer subtree walk below assigns
        // every nested item its window over the one backing (an inner redefiner starts at its target's
        // already-assigned offset), so a dissolved class needs no members of its own; keeping it would let its
        // later walk re-claim the subtree and emit a backing inside a suppressed view struct (CS0103).
        foreach (var (anchor, cls) in byAnchor.ToList())
            for (var a = anchor.Parent; a is not null; a = a.Parent)
                if (a.RedefinesTargetName is not null || (byAnchor.ContainsKey(a) && !ReferenceEquals(byAnchor[a], cls)))
                {
                    byAnchor.Remove(anchor);
                    break;
                }

        foreach (var cls in byAnchor.Values)
        {
            var tier = ComputeTier(cls, out string? reject);
            // A class joined by the file section's IMPLICIT redefinition (kb/Work PB836) is screened by the
            // implicit-area arm below, never by the REDEFINES-clause verdicts that follow — those name a clause
            // and a data-name-2 the source does not contain.
            bool implicitArea = cls.Members.Any(m => m.RedefinesImplicitly);
            // ⛔ CITATION REPAIRED **TWO-SIDEDLY**, AND THE SCREEN NARROWED TO THE SUBJECT SIDE (kb/Work PB177
            // arm C, then its follow-up). Three moves, and the middle one was itself wrong on one side:
            //  1. The arm used to cite "§13.18.44 SR5" for BOTH sides.
            //  2. The repair read SR5's FOURTH sentence ("Neither the original definition nor the redefinition
            //     shall include an occurs-depending table" — COBOLNET0855's rule) and concluded the whole rule
            //     was about something else, so it wrote "which NO syntax rule literally names". ⛔ THAT IS TRUE
            //     ONLY OF THE SUBJECT SIDE. SR5's FIRST sentence is "The data description entry for data-name-2
            //     shall not contain an OCCURS clause", and OCCURS DYNAMIC is Format 4 OF the OCCURS clause
            //     (§13.18.38) — so the OBJECT side is named outright, and is now COBOLNET1701 per written entry.
            //     Reading one sentence of a four-sentence rule and generalising is the same inheritance failure
            //     CLAUDE.md rule 1 names, one level down: the clause NUMBER was right and the SENTENCE was not.
            //  3. The dynamic-capacity SUBORDINATE case is §13.18.44.3 SR17's (a group with such a table
            //     subordinate IS a "variable-length group", §8.5.1.12.1) and is screened per WRITTEN ENTRY above
            //     as COBOLNET1698.
            // WHAT REMAINS HERE IS THE SUBJECT THAT IS ITSELF A DYNAMIC-CAPACITY TABLE, and for THAT side the
            // "no syntax rule names it" reading holds: §13.18.38.3 carries no REDEFINES restriction, and SR17
            // does not reach an ELEMENTARY dynamic table (§8.5.1.12.1's "variable-length group" is defined over
            // items SUBORDINATE to the group). So cite what actually decides it — §13.18.44.4 GR1 ("Storage
            // association for the subject of the entry starts at the first bit of the data item referenced by
            // data-name-2 and continues over an area sufficient to contain the number of bits required by the
            // data item referenced by the subject of the entry") against §8.5.1.9.1's dynamic-capacity model,
            // whose "physical and logical capacities may vary during execution": there is no fixed area to
            // associate. A WiseOwl COBOL storage-model rejection, honestly labelled, not a borrowed rule number.
            // (The message names the OFFENDING MEMBER, not the class canonical — it used to say "the
            // dynamic-capacity table in '<canonical>'" even when the canonical was the ordinary fixed item and
            // the dynamic table was the REDEFINING entry.)
            if (!implicitArea
                && cls.Members.FirstOrDefault(m => !ReferenceEquals(m, cls.Canonical) && m.IsDynamicTable) is { } dynSubject)
            {
                Edition.Error("COBOLNET1525", $"REDEFINES entry '{dynSubject.CobolName ?? dynSubject.CsName}' is "
                    + "itself a dynamic-capacity table: its capacity \"may vary during execution\" (ISO "
                    + "§8.5.1.9.1), so it has no fixed area for the storage association §13.18.44.4 GR1 requires; "
                    + "it shall not be the subject of a REDEFINES");
                tier = RedefinesTier.Rejected;
                reject ??= "REDEFINES subject is a dynamic-capacity table (§13.18.44.4 GR1 / §8.5.1.9.1, D9)";
            }
            // The §13.18.44.3 SR5 sentence-1 TIER verdict, same posture as SR12/SR14's and SR17's below (the
            // DIAGNOSTIC fired per written entry, before dissolution — no second diagnostic here). The tier
            // matters on its own: without it the class model is built as though the OCCURS-bearing object were
            // a legal single area, and the offsets/width walk below lays a view over one occurrence's worth of
            // storage. The predicate is the RULE's (`OccursSpec is not null`, every format) even though the
            // depending shape's DIAGNOSTIC belongs to COBOLNET0855 — a tier is not a message.
            foreach (var m in cls.Members)
                if (m.RedefinesKind is RedefinitionKind.Clause && m.RedefinesTarget is { IsTable: true } occTarget)
                {
                    tier = RedefinesTier.Rejected;
                    reject = $"REDEFINES over '{occTarget.CobolName ?? occTarget.CsName}', whose data "
                        + "description entry contains an OCCURS clause (ISO §13.18.44.3 SR5)";
                }
            // The SR12/SR14 TIER verdict for a retained class (the DIAGNOSTIC fired per written entry in the
            // grouping loop above, before dissolution — no second diagnostic here): the class rejects with
            // the spec-required reason, OVERRIDING ComputeTier's staged-loud one (its backstop arm also
            // matches an entry-level pointer).
            foreach (var m in cls.Members)
                if (!ReferenceEquals(m, cls.Canonical) && m.RedefinesKind is RedefinitionKind.Clause
                    && m.RedefinesTarget is { } srTarget && Sr12Sr14Violation(m, srTarget) is { } srReason)
                {
                    tier = RedefinesTier.Rejected;
                    reject = srReason;
                }
            // The §13.18.44.3 SR17 TIER verdict, same posture as SR12/SR14's above (the DIAGNOSTIC fired per
            // written entry, before dissolution — no second diagnostic here).
            // ⛔ THE TIER IS BELT-AND-BRACES, AND THE COMMENT HERE USED TO OVERCLAIM IT. It said "Rejecting the
            // CLASS makes that path structurally unreachable rather than merely unreached because a diagnostic
            // happened to stop the compile." MEASURED FALSE (RedefinesClassificationTests, once its assertion
            // was made non-inert): StorageFormPass.Classify orders its arms (1) IsDynamicTable, (1b)
            // IsDynamicLength → DynamicString, and only THEN (2) the REDEFINES-view arm — so arm 1b returns
            // before the tier is ever read, and a Rejected class's dynamic-length view still classifies as
            // DynamicString. A class DISSOLVED by the nested-anchor loop below bypasses the tier loop entirely,
            // which is the same hole from the other side. WHAT ACTUALLY PREVENTS THE DISJOINT STORAGE REACHING
            // A USER'S PROGRAM IS THE COBOLNET1698 DIAGNOSTIC, which is fatal. The tier verdict is a second,
            // independent verdict at the modelling layer and is worth keeping as one — every consumer that asks
            // the CLASS gets the right answer — but "structurally unreachable" was a claim about a path this
            // code does not close.
            if (!implicitArea)
                foreach (var m in cls.Members)
                    if (VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup(m) is { } vlShape)
                    {
                        tier = RedefinesTier.Rejected;
                        reject = $"REDEFINES entry side '{m.CobolName ?? m.CsName}' is {vlShape} "
                            + "(ISO §13.18.44.3 SR17)";
                    }
            // ⛔ THE IMPLICIT-AREA ARM (kb/Work PB836, narrowed by PB981). No syntax rule forbids a file's records
            // from being a dynamic-length item, a variable-length group or a pointer — §13.18.33.4 GR3 simply makes
            // them share one area — and since PB981 none of those reaches a class at all: they are OUT-OF-LINE
            // records (FileModel.IsOutOfLineRecord, determination D-FRA), which LinkImplicitRecordArea never links,
            // and they share the area at the transfer boundary (FileModel.OutOfLineRecords). What can still reach
            // here Rejected is a ComputeTier residue of a CHARACTER-window record (ByteWindowResidueOf), staged
            // LOUD rather than let through. The reason names the rule the program DID use, never §13.18.44.
            if (implicitArea)
            {
                if (tier is RedefinesTier.Rejected)
                {
                    var rule = cls.Members.Any(m => m.RedefinesKind is RedefinitionKind.SameRecordArea)
                        ? "a record-area SAME clause makes its files' records an implicit redefinition of one area "
                          + "(ISO §12.4.6.4.4 GR2)"
                        : "multiple level 1 entries of one FD or SD are implicit redefinitions of the same area "
                          + "(ISO §13.18.33.4 GR3)";
                    using var __ = Edition.At(cls.Members.FirstOrDefault(m => m.RedefinesImplicitly) ?? cls.Canonical);
                    Edition.Error(DiagnosticCatalog.ImplicitRecordAreaShape,
                        $"the record area of '{cls.Canonical.CobolName ?? cls.Canonical.CsName}' is shared — {rule} — "
                        + $"and {reject}: a shared record area of that shape is recognized but not yet implemented");
                }
            }
            // The width is a member table's FULL STORAGE extent (every occurrence) — §13.18.44.4 GR1: when the
            // subject "requires more bits than the data item referenced by data-name-2, the storage area
            // allocated … is the number of bits required by the data item referenced by the subject", i.e. the
            // maximum over the members. In BYTES, never character positions (kb/Work PB231): the two differ for
            // a NATIONAL member, whose §13.18.60.4 GR8 size is two bytes per position under D-N1, and a
            // character-position maximum would under-size the shared backing by half of it.
            // ONE verdict application (P5.11d).
            cls.Classify(tier, cls.Members.Max(m => m.ByteWidth * (m.Occurs ?? 1)), reject);
            // Each top-level member overlays the area from its start (a REDEFINES begins at the target's first
            // position, SR10); a subordinate accumulates its window offset within the member. Subordinates of any
            // member are themselves views (suppressed field, SR9).
            foreach (var member in cls.Members)
                AssignClassOffsets(member, 0, cls);
            // A Tier-B (string-canonical) numeric-DISPLAY view reads/writes its window through the character pipeline
            // (CobolNum.ParseDisplay / FormatDisplay) — the same image path used for whole-group numeric leaves.
            if (cls.Tier == RedefinesTier.StringCanonical)
                foreach (var leaf in cls.Members.SelectMany(LeavesOf))
                {
                    if (leaf.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                        MarkImageForced(leaf);   // the collected image fact (a Ptr-FORCED StringCanonical class deliberately never records — its display leaves stay TierBWindow)
                    // A national-form numeric leaf reaches the arm below (kb/Work PB646): its window over the
                    // one backing is Place.NationalWindow's transcoded CHARACTERS, not its bytes, but the
                    // image-forced verdict is the same one and this is the arm that already carried it.
                    else if (leaf.Pic is { HasImageByteForm: true, Usage: not Usage.Display })   // the ONE image predicate (kb/Work PB164 — COMP-5/BINARY-* windows included)
                        // A fixed-point BINARY/PACKED leaf of a Tier-B class is image-stored too: its window over
                        // the one string backing IS its BYTES — radix-2 two's complement or BCD, of exactly
                        // PicInfo.StorageWidth (ISO §13.18.60.4 GR4/GR11 implementor representation;
                        // COBOLNET_DESIGN §14.4, V59). Every accessor threads the leaf's OWN `_P_` profile
                        // through CobolNum.FormatImage/ParseImage, so no profile rewrite is needed or wanted:
                        // the sign lives in the bytes (two's complement / the sign nibble), and SignKind — a
                        // DISPLAY concern — stays the declared BinaryMinus, which is what DISPLAY of the leaf
                        // must still render. The former rewrite to a trailing-overpunch ZONED image existed only
                        // because the window used to be Digits characters wide.
                        MarkImageForced(leaf);
                }
        }
    }

    /// <summary>Assign each item in a redefines class its window offset within the class image and its class link; a
    /// top-level member starts at <paramref name="off"/> (0), a subordinate accumulates by preceding-sibling FULL
    /// extents (per-occurrence image width × OCCURS count — every occurrence is part of the layout). A subordinate
    /// that itself REDEFINES a prior sibling takes the TARGET's offset (redefinition begins at the redefined item's
    /// first position, ISO §13.18.44 GR1) and contributes NO width of its own. Every subordinate of a class member
    /// is itself a view (its stored field is suppressed — SR9).</summary>
    private static void AssignClassOffsets(DataItem item, int off, RedefinesClass cls) =>
        AssignClassBitOffsets(item, off * BitLayout.BitsPerCharacter, cls);

    /// <summary>The walk proper, carrying the offset in BITS — the unit §13.18.44.4 GR1 states the storage
    /// association in ("starts at the first BIT … an area sufficient to contain the number of BITS required").
    /// <para>⛔ <b>IT RUNS NO CURSOR OF ITS OWN.</b> Every child's place is the group's ABSOLUTE offset plus
    /// <see cref="BitLayout.ChildStarts"/> — THE one §8.5.1.6.3 walk, whose cursor is RELATIVE to the group (the
    /// extent of a member is <c>BitLayout.RunBits</c> inside it: a bit leaf's declared boolean positions, an
    /// elementary item's <c>ElementaryByteWidth × 8</c> — where the NATIONAL two-bytes-per-position lives, kb/Work
    /// PB231 — and a group's cursor extent, each times its OCCURS). This method used to keep its own cursor
    /// seeded with the ABSOLUTE bit offset and round THAT up to a byte before the first child, so the first member
    /// of a bit group sitting at bit 1 of a shared byte was placed at bit 8 (kb/Work PB1572 — a store through
    /// `05 P1 PIC 1 USAGE BIT` under `05 S3 GROUP-USAGE BIT` after `05 F3 PIC 1 USAGE BIT` was lost in a REDEFINES
    /// view): §8.5.1.6.3 puts "the start of a group item and … the first item within that group, when the first
    /// item is a bit data item, … at the same bit position in storage". A subtree with no <c>USAGE BIT</c> leaf
    /// needs no special case: every item there is a whole number of bytes, so the round-up is the identity.</para></summary>
    private static void AssignClassBitOffsets(DataItem item, int bitOff, RedefinesClass cls)
    {
        item.ClassBitOffset = bitOff;
        item.ClassOffset = bitOff / BitLayout.BitsPerCharacter;
        item.Class = cls;
        if (item.Children.Count == 0) return;
        var starts = BitLayout.ChildStarts(item);
        for (int i = 0; i < starts.Length; i++)
        {
            var c = item.Children[i];
            c.IsCanonical = false;
            // -1 is "a REDEFINES name with no sibling target"; an unresolved redefiner is demoted to an ordinary
            // entry (RedefinesTargetName = null, kb/Work PB93, COBOLNET1654), so no live child can carry it here.
            if (starts[i] < 0)
                throw new InvalidOperationException(
                    $"'{c.CobolName ?? c.CsName}' REDEFINES '{c.RedefinesTargetName}', which is not a sibling in its group");
            AssignClassBitOffsets(c, bitOff + starts[i], cls);
        }
    }

    /// <summary>Assign a redefines class its tier (COBOLNET_DESIGN §4.2; the Step D arm-1 dissolution made the
    /// cascade D &gt; B &gt; A — Tier C dissolved into B: every numeric leaf kind's byte form is pinned, so a
    /// mixed-USAGE pun is an ordinary byte-window class. D rejects the nested pointer/object backstop and the
    /// NATIONAL residue.</summary>
    // (ContainsDynamicTable was deleted with kb/Work PB177 arm C: its SUBORDINATE half is §13.18.44.3 SR17's,
    // now screened per written entry through ReferenceResolver.HasVariableLengthSubordinate — §8.5.1.12.1's own
    // definition of "variable-length group" — and its SELF half is the narrowed `m.IsDynamicTable` test at the
    // COBOLNET1525 arm. Two spellings of one walk collapsed into the predicate the standard actually defines.)

    /// <summary>The §13.18.44.3 SR12/SR14 class test (kb/Work PB179): object/pointer classes occupy no
    /// character positions and can neither overlay nor be overlaid. Message-tag has no bound model yet;
    /// a USAGE FUNCTION-POINTER entry's semantics are staged at declaration (the P13 band — its Pic stays
    /// null), so it never reaches the class machinery live.
    /// <para>⛔ THE ONE CLASS PREDICATE FOR BOTH SCREENS. §13.18.60.3 SR14's declaration-placement screen
    /// (<c>DataBinder.UsageDeclaration.cs</c>, kb/Work PB183) resolves its class question through this same
    /// method — see <c>Sr14PlacementClass</c>. The two rules spell the population differently (SR12/SR14 name
    /// the CLASSES "class object, message-tag, or pointer"; §13.18.60.3 SR14 names the five USAGE PHRASES that
    /// produce exactly those classes) and a second hand-written list would drift the moment MESSAGE-TAG or
    /// FUNCTION-POINTER gains a model. A unit pin asserts the identity. ⛔ CLASS INDEX IS NOT HERE and must
    /// never be added: §13.18.60.3 SR4 is the rule whose list includes INDEX, and it has its own predicate.
    /// </para>
    /// <para>⛔ AND IT IS NOW THE STORAGE PREDICATE TOO. <see cref="SlotWindow.CarriedBySlot"/> — "does this
    /// member ride the storage area's MANAGED SLOTS rather than its bytes?" (kb/Work PB231, the pointer third)
    /// — is the same population for the same reason: a class object/pointer item holds a managed reference and
    /// no bytes. It is the DEFINITION and this is the delegation, so the third spelling cannot drift from the
    /// first two either.</para></summary>
    private static bool PointerObjectClass(DataItem d) => SlotWindow.CarriedBySlot(d);

    /// <summary>⛔ THE ONE BYTE-WINDOW CARRIAGE GATE — the single answer to "may this LEAF ride a shared
    /// byte-window storage area?", returning null when it may and the residue clause naming why not when it
    /// may not. Both surfaces that ask the question ask it here: <see cref="ComputeTier"/> (a REDEFINES class
    /// over one string canonical, §13.18.44.4 GR1 — "Storage association for the subject of the entry starts
    /// at the first bit of the data item referenced by data-name-2 and continues over an area sufficient to
    /// contain the number of bits required") and <c>ForceStringCanonical</c> (the EXTERNAL run-unit cell, the
    /// ADDRESS-OF-taken per-instance cell, and the BASED pointer-deref bridge whose area ALLOCATE obtains,
    /// §14.9.3.4 GR3 — "the amount of storage to be allocated is the number of bytes required to hold an item
    /// as described by data-name-1").
    /// <para>⛔ IT IS ONE PREDICATE BECAUSE IT WAS TWO, AND THEY DRIFTED (kb/Work PB231). ComputeTier was a
    /// DENY-list and the cell forcer's local <c>CellCapable</c> an ALLOW-list, and PB203 opened only the
    /// REDEFINES one to USAGE BIT leaves — the ninth instance of this repo's two-arm dispatch shape. The
    /// consequence was rejects-legal-source: `01 R BASED. 05 B PIC 1(8) USAGE BIT.` drew COBOLNET1695 at bind
    /// while the byte-identical REDEFINES spelling compiled and ran, even though §13.18.5.3 SR1/SR2 restrict a
    /// BASED subject to exactly "shall not be of class object" and "shall not be a dynamic-length elementary
    /// item or a variable-length group" and §14.9.3.3 SR1 asks only for the BASED clause — no rule anywhere
    /// conditions BASED, EXTERNAL or ADDRESS OF on a subordinate's USAGE. Two smaller divergences went with it
    /// and are closed by the same collapse: a Pic-less leaf and a NATIONAL-usage numeric were denied by the
    /// cell and admitted by REDEFINES.</para>
    /// <para>The ALLOW-list direction is deliberate and must stay: a category added to the model with no
    /// pinned byte image has to be DENIED by default, never silently laid out as a zero-width alias.
    /// <c>ByteWindowResidueDriftTests</c> pins that both callers route through here.</para>
    /// <para>⛔ THE THIRD CALLER IS <see cref="GateFileRecordByteSurface"/> — the FD/SD record gate, folded in at
    /// kb/Work PB327 as PB231's paragraph here instructed. It used to screen the NATIONAL half only, with its own
    /// message and its own hand-written population; national now RIDES (the record codec lays a national position
    /// out as its two UTF-16BE bytes), and the pointer-class tightening the fold carries is a strict improvement
    /// on what a pointer leaf in a file record did before — the record was not image-capable, so every WRITE and
    /// READ of it was already the Tier-C runtime loud; the fold moves that to a bind diagnostic that names the
    /// leaf. Three callers, ONE predicate.</para></summary>
    internal static string? ByteWindowResidueOf(DataItem leaf)
    {
        // ⛔ A POINTER-CLASS LEAF RIDES — kb/Work PB231's pointer third, the LAST residue of this gate. Its
        // value is a managed reference and has no byte image, so it does not ride the BYTES: it rides the SAME
        // storage area's MANAGED SLOTS, keyed by the very byte offset its reserved bytes occupy
        // (Place.SlotWindow → StorageCell.SlotAt / CobolPtr.SlotRead). The area's byte quantity is unchanged —
        // §14.9.3.4 GR3's "the amount of storage to be allocated is the number of bytes required to hold an
        // item as described by data-name-1" is satisfied by the item's own 8-byte managed-carrier width — and
        // GR9's "data items of class object or class pointer in the allocated storage are initialized to null"
        // holds by construction, since an unwritten slot reads null.
        // ⚠ THE CARRIER IS A SECOND, DIFFERENT QUESTION, and it is NOT asked here: the slots live on a
        // StorageCell, which the three CELL surfaces have and a REDEFINES class's stored string backing does
        // not. ComputeTier asks it, once, and §13.18.44.3 SR12/SR14 plus §13.18.60.3 SR14 make its refusal
        // unreachable on conforming source (see Place.SlotWindow.CarriedBySlot for the three-rule derivation).
        // A USAGE FUNCTION-POINTER entry is staged at declaration (the P13 band) and never gains a PicInfo;
        // an elementary item with no bound representation has no image to window.
        if (leaf.Pic is not { } p)
            return $"a leaf with no bound representation ('{leaf.CobolName ?? "FILLER"}' — the staged "
                + "USAGE band; ISO §13.18.60)";
        // ⛔ NATIONAL RIDES, IN BOTH ITS SPELLINGS — the PB231 arm that discharged RESIDUE-11, and asked
        // through THE ONE national-window test rather than re-listing the population here. A national position
        // occupies TWO bytes of the byte-addressed area (§13.18.60.4 GR8 leaves the size to the implementor —
        // "National characters shall be represented in the storage of the computer as characters of a uniform
        // size equal to or a multiple of the size of characters in the computer's alphanumeric character set" —
        // and D-N1 pins two), and that is now the window's own geometry: the class walk advances by the
        // member's STORAGE extent (BitLayout.RunBits → WidthBits → ElementaryByteWidth, already 2n for
        // national), the class width is the members' maximum ByteWidth, and Place.NationalWindow transcodes the
        // pair through CobolBits.NatReadWindow/NatWriteWindow — the same UTF-16BE serialization CONVERT's
        // raw-storage channel and FUNCTION BYTE-LENGTH already answer with.
        // ⛔ ONE PREDICATE, because the gate and the geometry must not be able to disagree about WHICH leaves
        // are national. The two spellings are the CATEGORY (PIC N / USAGE NATIONAL) and the national-form
        // NUMERIC (PIC 9 USAGE NATIONAL, §13.18.60.3 SR12 — staged loud earlier at COBOLNET0899, so this arm is
        // the derived storage answer for the day that stage lifts rather than an observable one today).
        if (NationalWindow.PositionsOf(leaf) is not null) return null;
        // ⛔ THE POINTER FAMILY RIDES THE SLOTS — asked through THE ONE population test, never re-listed here,
        // for the same reason the national arm above is (kb/Work PB231): the gate and the geometry that carries
        // it must not be able to disagree about WHICH leaves are which.
        if (SlotWindow.CarriedBySlot(leaf)) return null;
        return p.Category switch
        {
            // One byte per character position (the documented item-209 serialization); an edited image is
            // its own characters.
            PicCategory.Alphanumeric or PicCategory.NumericEdited => null,
            // ⛔ BOOLEAN RIDES, IN BOTH ITS REPRESENTATIONS — the PB231 arm. A DISPLAY boolean is one
            // '0'/'1' character per position and byte==char holds (D-B1). A USAGE BIT run is a SUB-BYTE
            // packing, and that packing is already laid out and windowed: §13.18.60.4 GR5 ("The USAGE BIT
            // clause specifies that bits shall be used to represent a boolean data item"), §8.5.1.6.3's
            // cursor ("an elementary bit data item immediately following an elementary bit data item or bit
            // group item of the same level" takes the next bit position, everything else the first bit of
            // the first available byte) and §13.18.29.4 GR1c ("Data items contained within a bit group are
            // allocated in storage in accordance with the rules specified in 8.5.1.6.3") are walked by
            // BitLayout.ChildStarts — the SAME walk this predicate's two callers share — and the
            // resulting window is read/written by CobolBits.ReadWindow/WriteWindow over the class backing
            // (D19/PB43 for the layout, PB203 for the window). DataItem.ImageWidth is already the
            // ceil(bits/8) byte extent, so the area a cell allocates for such a record is GR3's byte
            // quantity by construction.
            PicCategory.Boolean => null,
            // Every NUMERIC usage carries a pinned byte form since the Step D dissolution (kb/Work PB164) —
            // THE ONE image predicate, never a hand-rolled usage union.
            PicCategory.Numeric when p.HasImageByteForm => null,
            _ => $"a leaf of category {p.Category} and usage {p.Usage} with no pinned byte image "
                + "(ISO §13.18.60.4 — no representation this model can lay out in a byte window)",
        };
    }

    /// <summary>ONE §13.18.44.3 SR12/SR14 verdict per WRITTEN entry — <paramref name="subject"/> is the item
    /// carrying the REDEFINES clause, <paramref name="target"/> its DIRECT data-name-2 (never the chained
    /// anchor). Returns the violation text (the diagnostic message AND the class RejectReason), or null.
    /// Each arm's message names the arm that actually fired (the skeptic round: a subordinate-to-strong
    /// violation was described as "is a strongly-typed group" — false of the item named).
    /// <para>⚠ The SR14 "subordinate to a strongly-typed group item" arm fires only when the subject sits
    /// OUTSIDE the target's strong subtree: a REDEFINES written INSIDE a STRONG typedef template (subject and
    /// target under one strong root) is the recorded deliberate carve-out `CheckStrongTypeDeclarations`
    /// documents ("An INTERNAL redefine … is legitimate and NOT flagged") — this screen must not silently
    /// overturn that determination; whether the §13.18.57.3 SR4 letter overturns IT is [[PB183]]'s companion
    /// derivation.</para></summary>

    /// <summary>The outermost strongly-typed item in <paramref name="record"/>'s subtree (the record itself when it
    /// is a TYPE subject naming a STRONG type), or null — the "in whole or in part" population of §13.18.57.3 SR4
    /// for a record that is implicitly redefined as a whole (kb/Work PB836).</summary>
    private static DataItem? FirstStrongItemIn(DataItem record)
    {
        if (record.StrongType) return record;
        foreach (var c in record.Children)
            if (FirstStrongItemIn(c) is { } s) return s;
        return null;
    }

    private static string? Sr12Sr14Violation(DataItem subject, DataItem target)
    {
        if (PointerObjectClass(subject))
            return $"'{subject.CobolName ?? subject.CsName}' is the subject of a REDEFINES entry but is of "
                + "class object/pointer (ISO §13.18.44.3 SR12)";
        if (StrongTypeModel.IsStrongGroup(subject))
            return $"'{subject.CobolName ?? subject.CsName}' is the subject of a REDEFINES entry but is a "
                + "strongly-typed group item (ISO §13.18.44.3 SR12)";
        if (PointerObjectClass(target))
            return $"'{target.CobolName ?? target.CsName}' is redefined (data-name-2) but is of class "
                + "object/pointer (ISO §13.18.44.3 SR14)";
        if (StrongTypeModel.IsStrongGroup(target))
            return $"'{target.CobolName ?? target.CsName}' is redefined (data-name-2) but is a strongly-typed "
                + "group item (ISO §13.18.44.3 SR14)";
        if (StrongTypeModel.StrongRoot(target) is { } tRoot
            && !ReferenceEquals(StrongTypeModel.StrongRoot(subject), tRoot))
            return $"'{target.CobolName ?? target.CsName}' is redefined (data-name-2) but is subordinate to "
                + "the strongly-typed group item "
                + $"'{tRoot.CobolName ?? tRoot.CsName}' (ISO §13.18.44.3 SR14)";
        return null;
    }

    private static RedefinesTier ComputeTier(RedefinesClass cls, out string? reject)
    {
        reject = null;
        var leaves = cls.Members.SelectMany(LeavesOf).ToList();

        // ⛔ THE TIER-C ARM IS GONE (the Step D arm-1 dissolution — DESIGN-data-model §2.3; kb/Work PB164's
        // LAST codegen half): every NUMERIC leaf kind carries a pinned byte form (NumericByteForm — waves
        // 1–2 + R40), the Tier-B geometry has been byte-form since V59 (§13.18.44.4 GR1 associates storage
        // at the bit over "the number of bits required"; GR2 grants every entry's name reference to that
        // storage), and the StoreAsImage loop's HasImageByteForm mark goes LIVE for float/COMP-5/BINARY-*/
        // INDEX leaves — each lane it feeds carries its own byte-level pin (the windowed-float read/store
        // arms, the unsigned ParseImage twins, the float seeds, the StorageFormPass canonical promotion).
        // A mixed class is simply Tier B: one string backing IS the shared storage area, every member an
        // (offset, StorageWidth) byte window.
        // Tier D backstop — a pointer/object-class LEAF nested inside a member (kb/Work PB179): NOT
        // §13.18.44.3 SR12/SR14's letter (those bar the ENTRY-level items — screened per written entry in
        // ClassifyRedefinesClasses). The skeptic round derived that §13.18.60.3 SR14 makes the nested
        // declaration itself NONCONFORMING (a pointer/object USAGE is legal only at level 1 or under a
        // STRONG type — and the strong case trips the entry-level screen via its enclosing group).
        // ⛔ THAT SCREEN NOW EXISTS — CheckUsageDeclarations (DataBinder.UsageDeclaration.cs, COBOLNET1724,
        // kb/Work PB183), which runs EARLIER in the pipeline and rejects the declaration itself, so on
        // conforming source this arm is unreachable-by-construction exactly as predicted. It is KEPT, not
        // deleted: it remains the loud guard for the recovery paths and for any future mode that admits a
        // nonconforming declaration, and without it such a leaf would reach the byte-window machinery as a
        // silent zero-width Tier-B alias. Never that.
        //
        // ⛔ Tier D IS NOW ONE CALL — ByteWindowResidueOf, the SHARED gate this screen and the cell forcer
        // (ForceStringCanonical) both route through. It used to be two hand-written lists, one per surface,
        // and they drifted: PB203 admitted USAGE BIT leaves HERE and nowhere else, so the byte-identical
        // BASED / EXTERNAL / ADDRESS-OF spelling of a conforming record still drew a bind-time reject
        // (kb/Work PB231). The residue clauses — the pointer/object backstop and the RESIDUE-11 national
        // layout — now live once, so the landing that discharges either one opens BOTH surfaces in a single
        // edit. ByteWindowResidueDriftTests pins the routing.
        // The class's area is named by the construct that shares it — a REDEFINES clause, or the file section's
        // implicit redefinition (kb/Work PB836), which has no REDEFINES to name.
        string where = cls.Members.Any(m => m.RedefinesImplicitly)
            ? $"in the shared record area of '{cls.Canonical.CobolName}'"
            : $"under REDEFINES of '{cls.Canonical.CobolName}'";
        if (leaves.Select(ByteWindowResidueOf).FirstOrDefault(r => r is not null) is { } residue)
        {
            reject = $"{residue} {where} — not yet implemented";
            return RedefinesTier.Rejected;
        }

        // ⛔ THE SECOND, DIFFERENT QUESTION — THE CARRIER (kb/Work PB231, the pointer third). ByteWindowResidueOf
        // answers "may this leaf ride a shared byte-window area?", and since PB231 a pointer-class leaf may: its
        // bytes are reserved and its VALUE rides the area's MANAGED SLOTS (Place.SlotWindow). Those slots live on
        // a StorageCell, which the three CELL surfaces have (RedefinesClass.IsCellBacked, set by the ONE forcer
        // ForceStringCanonical) and a REDEFINES class's plain stored string backing does NOT. That is a property
        // of the CARRIER, not of the leaf, so it is asked here rather than smuggled back into the shared gate —
        // which would re-create the very two-arm divergence PB231's collapse removed.
        // ⚠ UNREACHABLE THROUGH A REDEFINES CLAUSE on conforming source, by three rules that were re-derived
        // rather than inherited: §13.18.44.3 SR12 — "The REDEFINES clause shall not be specified for a data item
        // of class object, message-tag, or pointer or a strongly-typed group item"; SR14 — "Data-name-2 shall not
        // be of class object, message-tag, or pointer, a strongly-typed group item, or an item subordinate to a
        // strongly-typed group item"; and §13.18.60.3 SR14, which admits a pointer USAGE "only for an elementary
        // data item at level 1 or an elementary data item subordinate to a type declaration that includes the
        // STRONG phrase". The FILE SECTION's implicit area, which used to reach it (kb/Work PB836 — a level-1
        // `USAGE POINTER` record beside another record of the same FD, legal by that third rule and joined by
        // §13.18.33.4 GR3 with no REDEFINES clause for SR12/SR14 to bar), no longer does: since kb/Work PB981 a
        // pointer-class record is an OUT-OF-LINE record (FileModel.IsOutOfLineRecord, determination D-FRA) that
        // LinkImplicitRecordArea never links. The arm stays the loud guard for any path that would alias a
        // managed slot over a stored string backing — never a silent alias of reserved bytes.
        if (!cls.IsCellBacked && leaves.FirstOrDefault(SlotWindow.CarriedBySlot) is { } managed)
        {
            reject = $"'{managed.CobolName ?? "FILLER"}' is of a pointer class, whose value rides the storage "
                + "area's managed slots (ISO §14.9.3.4 GR9), and a shared area's stored backing has none "
                + $"— {where}";
            return RedefinesTier.Rejected;
        }
        // ⛔ THE SAME CARRIER QUESTION FOR A DYNAMIC-LENGTH LEAF (kb/Work PB1026): its content rides the cell's
        // dynamic slots (Place.DynSlotWindow), which a stored backing does not have. Unreachable through a REDEFINES
        // clause on conforming source — §13.18.44.3 SR17: "Neither data-name-2 nor the subject of the entry shall be
        // a variable-length group or a dynamic-length elementary item" — and a file's implicit area never links an
        // out-of-line record (D-FRA). Kept as the loud guard: never a zero-width alias of a dynamic item.
        if (!cls.IsCellBacked && leaves.FirstOrDefault(DynSlotWindow.CarriedBySlot) is { } dynLeaf)
        {
            reject = $"'{dynLeaf.CobolName ?? "FILLER"}' is a dynamic-length item, whose content rides a storage "
                + "cell's dynamic slots (ISO §8.5.1.10.3), and a shared area's stored backing has none "
                + $"— {where}";
            return RedefinesTier.Rejected;
        }

        // Tier A — every member is an elementary item sharing the canonical's CLR storage type AND its image width:
        // one stored field, the rest pass-throughs (a numeric view reinterprets the shared value via its own scale).
        // ⛔ AND THE SAME STORAGE UNIT (kb/Work PB203). CLR type + image width is not enough to prove ONE FIELD
        // SERVES BOTH: a `USAGE BIT` leaf's stored carrier is its PICTURE 1(n) boolean positions while its
        // ImageWidth is the ceil(n/8) BYTES it occupies (§13.18.60.4 GR5 — "bits shall be used"; D19/PB43), so
        // `01 A PIC X(1). 01 B REDEFINES A PIC 1(8) USAGE BIT.` matched on both counts (string, width 1) and B
        // aliased A's one-CHARACTER field — measured: `DISPLAY B` printed `A` where the eight boolean positions
        // of 0x41 are `01000001`. Two members share one field only when they agree on the UNIT (both bit or
        // neither) and, being bit, on the boolean-position COUNT — `PIC 1(5)` and `PIC 1(8)` share a byte
        // ceiling and NOT a carrier. A mismatched pair drops to Tier B, whose windows carry the unit explicitly.
        // ⛔ AND THE SAME UNIT FOR NATIONAL TOO (kb/Work PB231 — the PB203 defect re-run one category over, and
        // the reason this arm is written as an agreement per UNIT rather than a bit special case). A national
        // leaf's stored carrier is its PICTURE N(n) character positions while it OCCUPIES 2n bytes
        // (§13.18.60.4 GR8 / D-N1), so `01 A PIC X(4). 01 B REDEFINES A PIC N(4).` matched on CLR type (string)
        // and on ImageWidth (4) and B would have aliased A's four-CHARACTER field — reading alphanumeric
        // characters as national ones, the exact shape `01 A PIC X(1). 01 B REDEFINES A PIC 1(8) USAGE BIT.`
        // took. Two members share one field only when they agree on the UNIT (all three of bit, national and
        // byte are units here) and, sharing a non-byte unit, on the POSITION COUNT. Everything else drops to
        // Tier B, whose windows carry the unit explicitly. The extent test is ByteWidth for the same reason the
        // class width is.
        DataItem canon = cls.Canonical;
        static bool IsNatLeaf(DataItem d) => NationalWindow.PositionsOf(d) is not null;
        bool allAlias = canon.IsElementary && cls.Members.All(m =>
            m.IsElementary && m.ElementType == canon.ElementType && m.ByteWidth == canon.ByteWidth
            && BitLayout.IsBitLeaf(m) == BitLayout.IsBitLeaf(canon)
            && (!BitLayout.IsBitLeaf(m) || m.Pic!.Length == canon.Pic!.Length)
            && IsNatLeaf(m) == IsNatLeaf(canon)
            && (!IsNatLeaf(m) || NationalWindow.PositionsOf(m) == NationalWindow.PositionsOf(canon)));
        if (allAlias) return RedefinesTier.Alias;

        // Tier B — DISPLAY-homogeneous: one string canonical of class-max width, each view an (offset,width) accessor.
        return RedefinesTier.StringCanonical;
    }

    /// <summary>The byte-surface gate for FILE records — the THIRD caller of <see cref="ByteWindowResidueOf"/>
    /// (kb/Work PB327, folding in the instruction PB231 left on that predicate). An FD/SD record area is written
    /// to and read from a physical file as a run of BYTES (ISO §14.9.30.4 GR14/GR15 state the short/long-record
    /// rules over "the number of bytes in the record"), and the generated <c>AsImage()</c>/<c>FromImage()</c> pair
    /// IS that byte layout — so the question "can this leaf be laid out in a byte window?" is literally the one
    /// the REDEFINES classifier and the EXTERNAL/BASED cell forcer ask, and asking it in a second hand-written
    /// form is how the national half came to be screened here while bit and pointer were not.
    /// <para>⚠ AS OF PB231's POINTER THIRD (landed the same day, D-SLOT) THE PREDICATE REFUSES NOTHING, so this
    /// gate is currently VACUOUS — and it stays, for the reason the predicate's ALLOW-list direction exists: a
    /// category added to the model with no pinned representation must be refused BY NAME on every byte surface,
    /// this one included, rather than laid out as a zero-width alias. A pointer/object leaf in a FILE record is a
    /// separate question this gate deliberately does not answer: the RECORD image is a byte string with no
    /// managed-slot half, so such a record is not <c>IsImageCapable</c> and every WRITE/READ of it is the record
    /// codec's own Tier-C loud (<c>TierCIsland.Reason</c>) — unchanged by this fold.</para>
    /// <para>NATIONAL now RIDES: a national character position occupies two bytes, high-order first
    /// (§13.18.60.4 GR8 leaves the size to the implementor — D-N1 pins two), which the codec composes through
    /// <c>CobolBits.NatBytes</c> and decodes through <c>CobolBits.NatReadWindow</c>. What the fold refuses is what
    /// the predicate refuses everywhere else: a pointer/object-class leaf, and a leaf with no bound
    /// representation.</para></summary>
    internal void GateFileRecordByteSurface()
    {
        foreach (var f in Files)
            foreach (var rec in f.Records)
                foreach (var leaf in LeavesOf(rec))
                    if (ByteWindowResidueOf(leaf) is { } residue)
                        Edition.Error(DiagnosticCatalog.NationalData, $"a file record cannot be laid out — record "
                            + $"'{rec.CobolName}' has {residue} — recognized but not yet implemented");
    }

    /// <summary>Every item in the WORKING-STORAGE forest, in declaration (pre-order DFS) order.</summary>
    private IEnumerable<DataItem> AllItems()
    {
        static IEnumerable<DataItem> Walk(DataItem d)
        {
            yield return d;
            foreach (var c in d.Children)
                foreach (var x in Walk(c)) yield return x;
        }
        return Roots.SelectMany(Walk);
    }

    /// <summary>
    /// Every SOURCE-DECLARED data item this unit binds, for the post-bind <c>VersionConformancePass</c> data-attribute
    /// gates (Step 14g): the WS + FILE-record + OO-method forest (<see cref="Roots"/>), the LINKAGE forest
    /// (<see cref="LinkageRoots"/>, kept OFF <c>Roots</c>), and the TYPEDEF templates (<see cref="TypeDecls"/>, also OFF
    /// <c>Roots</c>) — pre-order DFS, declaration order. It EXCLUDES the products of post-bind expansion, which the binder
    /// never re-analyzed and so never gated: a <c>TYPE IS type-name</c> clone subtree (its items carry a non-null
    /// <see cref="DataItem.TypeAnchor"/>; the once-per-source gate fired on the TEMPLATE in <c>TypeDecls</c>) and the
    /// OO/UDF compiler temps (recorded in <see cref="CompilerTempClones"/>; both share the source item's <c>PicInfo</c> by
    /// reference — so they must NOT be dedup'd by <c>PicInfo</c> identity: a clone and its source share ONE <c>PicInfo</c>
    /// instance. (<c>PicInfo.PointerItem</c> stopped being a singleton at kb/Work PB153 — it is a factory now, since a
    /// restricted data-pointer carries its <c>TO type-name</c> — so the dedup hazard no longer needs that example to be
    /// real, but the clone-sharing one still is.) The result is exactly the set of items the binder's per-entry
    /// <c>PictureAnalyzer.ParseUsage</c>/<c>Analyze</c> gates fired for, once each.
    ///
    /// <para>⛔ This is the WRITTEN-ENTRY forest and its exclusion of the clones is part of its contract — do not
    /// widen it. A pass whose subject is a property of the COMPOSED entry rather than of the written clause list
    /// takes <see cref="CompositionForest"/> instead; that distinction is why the §13.18.63.3 SR13/SR14 screen
    /// missed the whole TYPE population on its first landing.</para>
    /// </summary>
    public IEnumerable<DataItem> ConformanceForest() =>
        DeclaredForest().Where(item => StrongTypeModel.TypeAnchor(item) is null);

    /// <summary>
    /// Every data item this unit binds AS COMPOSED — the same forest as <see cref="ConformanceForest"/> plus the
    /// TYPE / SAME AS clone subtrees it excludes, pre-order DFS in declaration order, still without the OO/UDF
    /// compiler temps.
    ///
    /// <para>⛔ TWO FORESTS, TWO KINDS OF SUBJECT — pick by what the rule is ABOUT, not by convenience.
    /// <see cref="ConformanceForest"/> answers "which entries did the programmer WRITE, as written": it is the
    /// once-per-source-entry set the per-entry data-attribute gates fire over, and excluding the clones is right
    /// there because the TEMPLATE carried the same clauses and was gated once. A rule whose subject is a property
    /// of the COMPOSED entry has no such counterpart on the template: `01 R TYPE T VALUE "ABCD".` composes a
    /// §13.18.63.3 SR14 violation out of a VALUE clause the reference site wrote and a usage the template wrote,
    /// and NEITHER entry carries it alone. Measured on the first landing of the SR13/SR14 screen: that program
    /// compiled clean while its byte-identical inline spelling was rejected — the whole TYPE population escaped.
    /// Use this forest for such a rule, and keep the item's OWN provenance (see <see cref="DataItem.ValueIsCopied"/>)
    /// to avoid re-reporting a clause the template already answered for.</para>
    /// </summary>
    public IEnumerable<DataItem> CompositionForest() => DeclaredForest();

    /// <summary>The shared spine of the two forests: <see cref="Roots"/> + <see cref="LinkageRoots"/> +
    /// <see cref="TypeDecls"/> templates, pre-order DFS in declaration order, pruning the OO/UDF compiler temps
    /// recorded in <see cref="CompilerTempClones"/>.</summary>
    private IEnumerable<DataItem> DeclaredForest()
    {
        var temps = CompilerTempClones.Count == 0 ? null
            : new HashSet<DataItem>(CompilerTempClones.Select(t => t.Temp));
        IEnumerable<DataItem> Walk(DataItem d)
        {
            // Prune a synthesized compiler temp's WHOLE subtree: a group temp (a UDF group-RETURNING result)
            // clones the callee's already-gated children — re-yielding them would double-fire the per-item
            // data-attribute gates in the CALLER's unit (the same exclusion the elementary temp always had).
            if (temps is not null && temps.Contains(d)) yield break;
            yield return d;
            foreach (var c in d.Children)
                foreach (var x in Walk(c)) yield return x;
        }
        return Roots.Concat(LinkageRoots).SelectMany(Walk).Concat(TypeDecls.Values.SelectMany(Walk));
    }

    /// <summary>The elementary leaves of an item (itself if elementary), in source order.</summary>
    private static IEnumerable<DataItem> LeavesOf(DataItem d)
    {
        if (d.IsElementary) { yield return d; yield break; }
        foreach (var c in d.Children)
            foreach (var l in LeavesOf(c)) yield return l;
    }

    /// <summary>⭐ THE ONE ISO §8.4.2.2 QUALIFICATION RESOLVER (kb/Work PB489). Every in-scope declaration of
    /// <paramref name="name"/> whose ancestor chain carries each written qualifier in order (inner → outer,
    /// §8.4.2.2.2 Format 1 — qualifiers need not name consecutive levels), with the OUTERMOST qualifier
    /// optionally naming the owning FILE instead: Format 1 is
    /// <c>data-name-1 [ data-qualifier ] … [ file-report-qualifier ]</c>, so the FD/SD is the highest permissible
    /// qualifier (SQ207M's <c>WRITE PRINT-REC IN PRINT-FILE</c>).
    /// <para>⛔ IT RETURNS THE CANDIDATE SET, NOT A WINNER, because §8.4.2.2.1 makes the number of survivors the
    /// whole answer: "uniqueness shall be established through qualification for each user-defined name explicitly
    /// referenced". One survivor resolves; zero or several is a resolution FAILURE the caller reports. A caller
    /// that took the first survivor would be substituting declaration order for the standard's uniqueness
    /// requirement — which is exactly what the file-clause lookups did (a bare <c>ByName[n][0]</c>) until PB489,
    /// so <c>LINAGE IS SZ LINES</c> with two SZs silently built the whole page model on the first-declared one.
    /// </para>
    /// <para>⛔ AND IT IS THE ONLY COPY. The binder used to carry a weaker private twin
    /// (<c>FindQualified</c>/<c>QualifiersMatch</c>) that matched the IN/OF chain but knew nothing about the
    /// file-name qualifier and never counted survivors, beside <c>ReferenceResolver.ResolveQualified</c>, which
    /// did both — one rule written down twice, with the procedure division reading the complete one and the data
    /// division the partial one (feedback_one_rule_one_place). <see cref="ReferenceResolver"/> now calls this.
    /// </para></summary>
    internal NameCandidates<DataItem> QualifiedCandidates(string name, IReadOnlyList<string> quals, Model.Scope scope)
    {
        List<DataItem> survivors = [];
        if (Symbols.TryResolve(name, scope, out var candidates))
            foreach (var cand in candidates)
                if (QualifierChainMatches(cand, quals) && !survivors.Contains(cand))
                    survivors.Add(cand);
        // §8.4.6.2.1 3) AFTER qualification (rule 1 applies "the normal rules for qualification" over the whole
        // set first): the survivors of the nearest declaring source element (kb/Work PB1047 / PB1243).
        return new NameCandidates<DataItem>(Symbols.NearestDeclaring(survivors, static i => i));
    }

    /// <summary>The §8.4.2.2 candidate set of <paramref name="name"/> WITHIN one record — every entry of
    /// <paramref name="root"/>'s subtree (itself included) so named whose ancestor chain carries the written
    /// qualifiers (<see cref="QualifierChainMatches"/>). For the operands confined to one record: RENAMES
    /// data-name-2 / data-name-3 (§13.18.45.3 SR4 — "in the same record") and a TYPEDEF clone's own OCCURS
    /// DEPENDING ON counter. The tree walk, not the name index, because a TYPEDEF template's members are off the
    /// name index (§13.18.58.4 GR1). Counted like every other candidate set (kb/Work PB978).</summary>
    internal NameCandidates<DataItem> SubtreeCandidates(DataItem root, string name, IReadOnlyList<string> quals)
    {
        List<DataItem> hits = [];
        void Walk(DataItem n)
        {
            if (string.Equals(n.CobolName, name, StringComparison.OrdinalIgnoreCase) && QualifierChainMatches(n, quals))
                hits.Add(n);
            foreach (var c in n.Children) Walk(c);
        }
        Walk(root);
        return new NameCandidates<DataItem>(hits);
    }

    /// <summary>The candidate set of a data-name referenced in a DATA DESCRIPTION ENTRY CLAUSE of
    /// <paramref name="subject"/> (OCCURS … DEPENDING ON, SAME AS), with ISO §8.4.2.2.1 rule 5's implicit
    /// qualification applied: "The name is a data-name referenced in a data description entry clause whose subject
    /// is subordinate to the same group item as that data-name. In this case, the names of any group items
    /// superordinate to both the data-name and the subject of the data description entry clause are used as
    /// implicit qualifiers for the reference, in addition to any explicit qualifiers needed to establish uniqueness
    /// within that group." So the set is the one under the INNERMOST group superordinate to the subject that holds
    /// any candidate — every candidate there shares exactly the same implicit qualifiers, so two of them are still
    /// ambiguous — and the scope-wide set only when no group of the subject's holds one. This is also the rule
    /// behind a TYPEDEF clone's own DEPENDING ON counter (a tree walk, so a clone's members off the name index are
    /// found). Counted like every candidate set (kb/Work PB978).</summary>
    internal NameCandidates<DataItem> EntryClauseCandidates(DataItem subject, string name, IReadOnlyList<string> quals,
                                                      Model.Scope scope)
    {
        for (DataItem? group = subject.Parent; group is not null; group = group.Parent)
            if (SubtreeCandidates(group, name, quals) is { Count: > 0 } within) return within;
        return QualifiedCandidates(name, quals, scope);
    }

    /// <summary>⛔ THE ONE AMBIGUITY VERDICT OF THE DATA DIVISION (kb/Work PB978). Exactly one survivor is the item.
    /// Several is ISO §8.4.2.2.3 SR1's failure — "For each non unique user-defined name that is explicitly
    /// referenced, uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity
    /// of reference" — reported HERE, at the caller's cursor, under the procedure division's own descriptor
    /// (COBOLNET1639) and with its disposition: an error, or under <c>--permissive</c> a warning and the
    /// first-declared survivor (ReferenceResolver.ResolveUnqualified, kb/Work R33). None returns null WITHOUT a
    /// report: an operand that names nothing breaks the CLAUSE's own rule, which the caller states
    /// (<paramref name="ambiguityReported"/> says which of the two a null is).</summary>
    internal T? UniqueOrReportAmbiguous<T>(NameCandidates<T> candidates, string clauseFace, string written,
                                           out bool ambiguityReported) where T : class
    {
        ambiguityReported = false;
        if (candidates.Count <= 1) return candidates.Single;
        ambiguityReported = true;
        // One verdict for every name class §8.4.2.2.3 SR1 covers (kb/Work PB919): only the noun differs.
        string what = typeof(T) == typeof(IndexDeclaration) ? "an index-name" : "a data item";
        string msg = $"{clauseFace} '{written}' does not uniquely identify {what} — {candidates.Count} "
            + "declarations match the written reference (ISO §8.4.2.2.3 SR1: \"For each non unique user-defined "
            + "name that is explicitly referenced, uniqueness shall be established through a sequence of qualifiers "
            + "that precludes any ambiguity of reference\")";
        if (!Edition.Permissive)
        {
            Edition.Error(DiagnosticCatalog.UndefinedReference, msg);
            return null;
        }
        Edition.Warning(DiagnosticCatalog.UndefinedReference, msg + "; --permissive resolves to the first declaration");
        return candidates.FirstDeclaredForPermissive;
    }

    /// <summary>The written form of a qualified data-name, for a diagnostic: <c>K OF G OF R</c>.</summary>
    internal static string WrittenQualified(string name, IReadOnlyList<string> quals) =>
        quals.Count == 0 ? name : name + " OF " + string.Join(" OF ", quals);

    /// <summary>True when every qualifier names strictly-superordinate context of <paramref name="cand"/>,
    /// consumed inner → outer with gaps allowed (ISO §8.4.2.2.3 SR4 — "Qualifiers shall be specified in the order
    /// of successively more inclusive levels in the hierarchy"); when the data ancestors are exhausted, the
    /// OUTERMOST remaining qualifier may instead name the file whose FD/SD owns the candidate's record
    /// (§8.4.2.2.2 Format 1's <i>file-report-qualifier</i>).</summary>
    internal bool QualifierChainMatches(DataItem cand, IReadOnlyList<string> qualifiers) =>
        QualifierChainMatchesFrom(cand.Parent, cand, qualifiers);

    /// <summary>The §8.4.2.2.2 Format 3 (qualified-index-name) twin of <see cref="QualifierChainMatches"/>:
    /// <c>index-name-1 [ data-qualifier ] … [ file-report-qualifier ]</c>, whose hierarchy §8.4.2.2.3 SR6 gives —
    /// "The qualification of an index-name may include the name of the table with which the index-name is
    /// associated, as well as any name by which that table may be qualified." So the chain starts AT the table,
    /// not above it; the matching itself is the same one walk (kb/Work PB919).</summary>
    internal bool IndexQualifierChainMatches(IndexDeclaration decl, IReadOnlyList<string> qualifiers) =>
        QualifierChainMatchesFrom(decl.Table, decl.Table, qualifiers);

    /// <summary>The §8.4.2.2.2 Format 2 (qualified-condition-name) twin of <see cref="QualifierChainMatches"/>:
    /// §8.4.2.2.3 SR4 — "For purposes of item qualification, the hierarchy of an item declared at level 88
    /// includes the conditional variable with which it is associated" — and SR5, "The qualification of a
    /// condition-name may include the conditional variable ... as well as by any name by which that conditional
    /// variable may be qualified". So the chain starts AT the conditional variable. ⛔ It used to be a PRIVATE
    /// fourth copy of this walk in <c>ConditionBinder</c> (kb/Work PB567), without the file-name arm, so a
    /// condition-name qualified through its record's FILE could never resolve.</summary>
    internal bool ConditionQualifierChainMatches(Condition88 cond, IReadOnlyList<string> qualifiers) =>
        QualifierChainMatchesFrom(cond.Parent, cond.Parent, qualifiers);

    /// <summary>The one qualifier walk: <paramref name="firstQualifiable"/> is the innermost entry a qualifier may
    /// name; <paramref name="member"/> locates the record for the file-name qualifier.</summary>
    private bool QualifierChainMatchesFrom(DataItem? firstQualifiable, DataItem member, IReadOnlyList<string> qualifiers)
    {
        if (qualifiers.Count == 0) return true;
        return QualifierWalk(NamesFrom(firstQualifiable), qualifiers, q =>
        {
            // Data ancestors exhausted: the OUTERMOST remaining qualifier may be the file name.
            if (!FilesByName.TryGetValue(q, out var file)) return false;
            DataItem root = member;
            while (root.Parent is { } p) root = p;
            return file.Records.Contains(root);
        });

        static IEnumerable<string?> NamesFrom(DataItem? anc)
        {
            for (; anc is not null; anc = anc.Parent) yield return anc.CobolName;
        }
    }

    /// <summary>⛔ THE ONE §8.4.2.2.3 SR4 QUALIFIER WALK — the written qualifiers are consumed INNER → OUTER against
    /// <paramref name="ancestorNames"/> (the names of the successively more inclusive levels above the item,
    /// innermost first; a level with no name is a null), with gaps allowed, and when the ancestors are exhausted the
    /// OUTERMOST remaining qualifier may instead be the container <paramref name="isOutermostContainer"/> accepts —
    /// §8.4.2.2.2 Format 1's <i>file-report-qualifier</i>: the owning FILE of a data record, the owning REPORT of a
    /// report-section sum counter (kb/Work PB1454). The hierarchy differs by item kind; the rule does not, so it is
    /// written once and each kind supplies its own ancestors and its own outermost container.</summary>
    internal static bool QualifierWalk(
        IEnumerable<string?> ancestorNames, IReadOnlyList<string> qualifiers, Func<string, bool> isOutermostContainer)
    {
        using var ancestors = ancestorNames.GetEnumerator();
        for (int qi = 0; qi < qualifiers.Count; qi++)
        {
            string q = qualifiers[qi];
            bool found = false;
            while (!found && ancestors.MoveNext())
                found = string.Equals(ancestors.Current, q, StringComparison.OrdinalIgnoreCase);
            if (found) continue;
            return qi == qualifiers.Count - 1 && isOutermostContainer(q);
        }
        return true;
    }
}
