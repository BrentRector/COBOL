// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>The method-local NAME SCOPE of one METHOD's data (ISO §11.7 GR5 — a method-local name SHADOWS the
/// same name in object data; and it is INVISIBLE to sibling methods, the legacy trap-#6 cross-wiring guard made
/// structural): the per-method overlay <see cref="ReferenceResolver"/> and the condition-name lookup consult
/// FIRST while binding this method's statements. Built by <see cref="DataBinder.OoBindMethodData"/>, activated
/// per-pc by <c>StatementBinder.BindClassBody</c> through <see cref="DataBinder.ActiveMethodScope"/>.</summary>
public sealed class OoMethodDataScope
{
    public Dictionary<string, List<DataItem>> ByName { get; } = new(CobolNames.Comparer);
    public Dictionary<string, List<Condition88>> Conditions { get; } = new(CobolNames.Comparer);

    /// <summary>The method's own INDEX-NAME declarations (M2-OO-1h step 4; §11.7.4 GR5 — index-names are
    /// method-private: two methods each with <c>INDEXED BY IX</c> get DISTINCT cells, and a method IX shadows an
    /// object IX with its OWN cell — never one of the unit's <see cref="DataBinder.IndexNames"/>).</summary>
    public IndexNameRegistry IndexNames { get; } = new();
}

/// <summary>
/// The METHOD-data half of the data binder (OO deep-dive D6 — port slice 2): a method's LINKAGE SECTION
/// (→ typed C# parameters via capturable locals) and LOCAL-STORAGE SECTION (→ C# locals, re-initialized each
/// activation, §8.6.4). A method has NO WORKING-STORAGE at any edition: §13.5.3 SR1 allows it "only in a factory
/// definition or an instance definition, but not in a method definition", so one is refused (COBOLNET1519; kb/Work
/// PB1308, decision R62) and its entries bind as LOCAL-STORAGE for recovery only. Items bind into the CLASS's one
/// forest (so USAGE/SIGN inheritance, object-reference resolution, profiles, and struct types all apply unchanged)
/// but their NAMES move into the method's own scope.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The name scope of the method whose statements are CURRENTLY being bound (set per-pc by
    /// <c>StatementBinder.BindClassBody</c>; null while binding program/object-level code).</summary>
    public OoMethodDataScope? ActiveMethodScope { get; set; }

    /// <summary>True when this binder binds a CLASS unit (per-binder-instance configuration, set ONCE at
    /// construction by <c>Oo/OoDriver</c> — init-only since P9 Step 6, so a mid-bind mutation cannot compile): a root
    /// that is NOT method-scoped is then OBJECT data — §14.9.23.3 SR 10 bans it from crossing an INVOKE
    /// BY REFERENCE (a bare object-data argument is assumed BY CONTENT instead, §14.9.23.4 GR6a2).</summary>
    public bool OoIsClassUnit { get; init; }

    /// <summary>Every METHOD whose data this binder bound, in bind order — the roster whose procedure divisions
    /// the data-pointer pre-scan walks (a class unit's synthetic program carries no procedure division of its
    /// own; kb/Work PB956).</summary>
    internal IReadOnlyList<OoMethodSymbol> OoBoundMethods => _ooBoundMethods;
    private readonly List<OoMethodSymbol> _ooBoundMethods = [];

    /// <summary>Every method-scoped root of the class (union over all methods) — the SR 10 discriminator:
    /// an item whose 01/77 root is NOT in this set, in a class unit, is object data.</summary>
    public HashSet<DataItem> OoMethodScopedRoots { get; } = [];

    /// <summary>The COBOL name of the class this binder's entries are written in — set once by <c>Oo/OoDriver</c>
    /// for the instance and factory halves alike, null for a program/function unit. It is what
    /// <c>USAGE OBJECT REFERENCE ACTIVE-CLASS</c> resolves to: §13.18.60.4 GR22 e) makes the referenced object
    /// "of the same class as the object that was used to invoke the method in which this data description entry
    /// is specified", and §13.18.60.3 SR16 guarantees such a containing class exists wherever the phrase is
    /// legal. kb/Work PB389.</summary>
    public string? OoOwnerClassName { get; init; }

    /// <summary>
    /// ⛔ THE ONE ADJUDICATION OF A <c>USAGE OBJECT REFERENCE</c> PHRASE (ISO §13.18.60.2 general format) — the
    /// written syntax in, an <see cref="ObjectRefDescriptor"/> out. The format prints THREE stacked alternatives
    /// inside one bracket pair, over four independent axes:
    /// <code>
    /// OBJECT REFERENCE ⎡ interface-name-1                            ⎤
    ///                  ⎢ [ FACTORY OF ] ACTIVE-CLASS                 ⎥
    ///                  ⎣ [ FACTORY OF ] object-class-name-1 [ ONLY ] ⎦
    /// </code>
    /// <para>The grammar cannot tell interface-name-1 from object-class-name-1 (both are one <c>cobolWord</c>),
    /// so it parses the superset <c>[FACTORY OF] className [ONLY]</c> and THIS method makes the general-format
    /// rejection once the name resolves: the interface alternative carries neither FACTORY nor ONLY.</para>
    /// <para>Three verdicts live here and nowhere else — COBOLNET0813 (the name is neither a class nor an
    /// interface of the compilation group), COBOLNET1925 (FACTORY/ONLY on the interface-name alternative) and
    /// COBOLNET1924 (§13.18.60.3 SR16, ACTIVE-CLASS outside a factory definition, an instance definition, or the
    /// linkage or local-storage section of a method definition). kb/Work PB389.</para>
    /// </summary>
    internal ObjectRefDescriptor OoBindObjectRefDescriptor(
        Core.ObjectReferenceUsageContext? oru, string entryWhere, EntrySection section)
    {
        // No phrase at all — §13.18.60.4 GR22 b), the universal object reference. (Also the defensive answer for
        // an object-reference usage acquired without its own written clause: the description it inherits already
        // carries its own descriptor, so nothing reads this arm.)
        if (oru is null) return ObjectRefDescriptor.Universal;
        bool factory = oru.FACTORY() is not null;

        if (oru.ACTIVE_CLASS() is not null)
        {
            // §13.18.60.3 SR16: "The ACTIVE-CLASS phrase may be specified only in a factory definition, an
            // instance definition, or the linkage or local-storage section of a method definition." Two arms:
            // outside a class body entirely (a program, a function or a program/function prototype is not a method
            // definition), and inside a METHOD's data but in a section SR16 excludes (a method's WORKING-STORAGE is
            // the reachable case — a method has no other sections, §13.4.3 SR1 and friends being enforced by
            // OoDefinitionRules). A method PROTOTYPE of an interface definition IS a method definition (§10.6.1
            // NOTE), so its linkage section is admitted — with NO containing class (kb/Work PB1497): the interface
            // binder has no OoOwnerClassName, and the description is owner-less (ObjectRefDescriptor.ActiveClass).
            if (!OoIsClassUnit)
            {
                Edition.Error(DiagnosticCatalog.ObjectReferenceActiveClassPlacement, $"{entryWhere}: USAGE OBJECT "
                    + "REFERENCE ACTIVE-CLASS is written outside a class definition — the ACTIVE-CLASS phrase may "
                    + "be specified only in a factory definition, an instance definition, or the linkage or "
                    + "local-storage section of a method definition (ISO §13.18.60.3 SR16)");
                // No class body → nothing to bind to. Fall back to the UNIVERSAL description so the erroring
                // compile still emits a well-typed field (CobolObject?) instead of a dangling type name.
                return ObjectRefDescriptor.Universal;
            }
            if (_bindingMethodScope is not null
                ? section is not (EntrySection.LocalStorage or EntrySection.Linkage)
                : OoOwnerClassName is null)   // object/factory data has a class; an interface has no data outside its prototypes
            {
                Edition.Error(DiagnosticCatalog.ObjectReferenceActiveClassPlacement, $"{entryWhere}: USAGE OBJECT "
                    + "REFERENCE ACTIVE-CLASS is written "
                    + (_bindingMethodScope is not null ? "in a method definition's WORKING-STORAGE SECTION" : "outside a class definition")
                    + " — the phrase may be specified only in a factory definition, an instance definition, or the "
                    + "linkage or local-storage section of a method definition (ISO §13.18.60.3 SR16)");
                return ObjectRefDescriptor.Universal;
            }
            return ObjectRefDescriptor.ActiveClass(OoOwnerClassName, factory);
        }

        if (oru.className()?.GetText() is not { } name) return ObjectRefDescriptor.Universal;
        bool only = oru.ONLY() is not null;

        // A TYPED reference (spine part 2 — LIVE): the declared name must resolve — its emitted C# field type
        // IS the named class's or interface's emitted type (PicInfo.ClrType), so an unresolved name would
        // surface as a Roslyn CS0246 on user source (a loud-failure violation). §13.18.60.2: the operand is
        // an interface-name or an object-class-name, so the funnel is asked for `Either`.
        // ⛔ The set is the REFERRING SOURCE ELEMENT's (§8.4.6.4), NOT the compilation group's: this site
        // called `OoClasses.Find`/`FindInterface` directly and so accepted a class the source element may not
        // reference (kb/Work PB365 — the same widened set as the USE Format-4 arm; §13.18.60 states no
        // REPOSITORY rule of its own, so §8.4.6.4 IS the rule). Resolving the name HERE, through the one
        // funnel, is what makes that scope travel with the descriptor: `oru` is the reference site the
        // ancestor walk starts from.
        var resolved = Compiler.Oo.OoNameResolution.Resolve(OoClasses, Edition, oru, name,
            Compiler.Oo.OoNameResolution.Want.Either,
            $"{entryWhere}: USAGE OBJECT REFERENCE", "COBOLNET0813", "ISO §13.18.60.2/.4");
        bool isInterface = resolved.Interface is not null;
        if (!resolved.Ok)
            // The funnel has already reported WHICH of the two failures this is (defined in the group but out
            // of scope vs. defined nowhere). Keep the name so every later diagnostic about this item still
            // says what the programmer wrote.
            return ObjectRefDescriptor.ObjectClass(name, factory, only);
        if (isInterface)
        {
            // The general format's interface-name-1 alternative is the BARE name: FACTORY OF and ONLY belong to
            // the object-class-name-1 alternative alone, and GR22 c) states the interface reading with no
            // subordinate rules at all ("the object referenced by this data item shall implement interface-1").
            if (factory || only)
                Edition.Error(DiagnosticCatalog.ObjectReferenceInterfacePhrase, $"{entryWhere}: USAGE OBJECT "
                    + $"REFERENCE names the interface '{name}' with "
                    + (factory && only ? "the FACTORY OF and ONLY phrases" : factory ? "the FACTORY OF phrase" : "the ONLY phrase")
                    + " — those phrases belong to the object-class-name-1 alternative of the general format; the "
                    + "interface-name-1 alternative carries neither (ISO §13.18.60.2; §13.18.60.4 GR22 c))");
            return ObjectRefDescriptor.Interface(resolved.Interface!.Name);
        }
        // ⛔ The descriptor carries the RESOLVED definition's name, not the word written: a REPOSITORY specifier's
        // `AS literal-1` makes the local word (BOXY) name the class whose externalized name is the literal
        // (§12.3.8.4 GR2, kb/Work PB974), and every downstream reader of the descriptor looks the class up by
        // this name in the group's table.
        return ObjectRefDescriptor.ObjectClass(resolved.Class!.Name, factory, only);
    }

    /// <summary>The STATIC-field channel: every emitted root field name whose storage is ONE per-class copy —
    /// consumed by <c>RecordStructEmitter</c> to add the <c>static</c> modifier. ONE producer: a RECURSIVE-and-not-INITIAL
    /// unit's (incl. every FUNCTION's, §8.6.6) WORKING-STORAGE roots + Tier-B backings
    /// (<see cref="RouteStaticUnitStorage"/> — §13.5.4 GR1 static data, ONE last-used copy across all activations,
    /// §14.6.2.3.3). A method has no WORKING-STORAGE to put here (§13.5.3 SR1, kb/Work PB1308).
    /// (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlySet<string> StaticRootFields => _staticRootFields;
    private readonly HashSet<string> _staticRootFields = new(StringComparer.Ordinal);

    /// <summary>The BASED bridge address fields of a RECURSIVE unit's static WORKING-STORAGE (kb/Work PB154 —
    /// §14.6.2.3.2 action 5): the `__addr_X` field emits STATIC and `__ResetStatics` sets it back to NULL,
    /// where the old routing REJECTED legal `01 X BASED.` in a RECURSIVE program outright.</summary>
    public IReadOnlySet<string> StaticBasedBridgeAddrs => _staticBasedBridgeAddrs;
    private readonly HashSet<string> _staticBasedBridgeAddrs = new(StringComparer.Ordinal);

    /// <summary>The ADDRESS-OF cell fields of a RECURSIVE unit's static WORKING-STORAGE (kb/Work PB234): the
    /// record's <c>StorageCell</c> — its ONE storage, which <c>ManagedPointer.At</c> aliases — emits STATIC
    /// (§13.5.4 GR1: one copy shared by every activation) and <c>__ResetStatics</c> re-seeds it IN PLACE
    /// (§14.6.2.3.2 action 2), where the old routing REJECTED `ADDRESS OF` a static item outright
    /// (COBOLNET0899 — legal source refused).</summary>
    public IReadOnlySet<string> StaticAddressableCells => _staticAddressableCells;
    private readonly HashSet<string> _staticAddressableCells = new(StringComparer.Ordinal);

    /// <summary>Method root (01/77) → its owning method symbol (M2-OO-1h). The post-build passes (OdoResolve,
    /// ResolveRedefines) run AFTER <see cref="OoScopeSubtree"/> has moved method names out of the global maps, and
    /// <see cref="ActiveMethodScope"/> is null then — so they resolve a method item's data-name-1 / REDEFINES
    /// target through its OWNING method's scope (§11.7.4 GR5), keyed off the item's root, never the class globals.</summary>
    internal Dictionary<DataItem, OoMethodSymbol> OoRootOwner { get; } = new(ReferenceEqualityComparer.Instance);

    /// <summary>The STATIC index-cell channel — INDEXED BY cells emitted as class-level STATIC <c>long</c>
    /// fields because their table's storage is static: a RECURSIVE unit's WS table cells
    /// (<see cref="RouteStaticUnitStorage"/> — the cell rides its table's §13.5.4 GR1 static storage; these DO also
    /// appear in <see cref="IndexNames"/>, so the emitter's instance-cell loop consults this set for the modifier).
    /// A method table's cell is instead a per-activation method local, emitted in <c>OoEmitMethod</c>, and never
    /// appears here. (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlySet<string> StaticIndexCells => _staticIndexCells;
    private readonly HashSet<string> _staticIndexCells = new(StringComparer.Ordinal);

    /// <summary>⛔ THE ONE QUESTION "IS THIS CLASS MEMBER STATIC?" — true when <paramref name="csMember"/> is on any of
    /// the four static channels (<see cref="StaticRootFields"/>, <see cref="StaticBasedBridgeAddrs"/>,
    /// <see cref="StaticAddressableCells"/>, <see cref="StaticIndexCells"/>), i.e. it is ONE copy per class
    /// (§13.5.4 GR1) and C# reaches it through the CLASS name, never through an instance (CS0176). A contained
    /// program's reference to a container's member asks it, through <c>BoundUnit.AnchorOf</c>, so a container with
    /// static working-storage composes with its containees' GLOBAL bridges (kb/Work PB1133). A new static channel
    /// joins THIS predicate and nothing else.</summary>
    internal bool IsStaticMember(string csMember) =>
        _staticRootFields.Contains(csMember) || _staticBasedBridgeAddrs.Contains(csMember)
        || _staticAddressableCells.Contains(csMember) || _staticIndexCells.Contains(csMember);

    /// <summary>Every INDEX-NAME declaration under a root (its subtree's <c>INDEXED BY</c> declarations, each with
    /// its own cell — kb/Work PB919). The ONE subtree walk every cell router, bridge and emitter reads.</summary>
    internal static IEnumerable<IndexDeclaration> IndexDeclarationsUnder(DataItem root)
    {
        foreach (var d in root.Indexes) yield return d;
        foreach (var c in root.Children) foreach (var d in IndexDeclarationsUnder(c)) yield return d;
    }

    /// <summary>THE scope-aware name resolver of this binder's forest (P6 Step 7 — <see cref="Model.SymbolTable"/>
    /// collapses the former LookupData/LookupDataInScopeOf/TryGetVisibleIndexField/IndexFieldFor quadruple). One
    /// table per binder: COBOL name scopes are per-unit. A 7a wrapper over the live maps; builder-owned storage
    /// is P7.</summary>
    public Model.SymbolTable Symbols => _symbols ??= new Model.SymbolTable(this);
    private Model.SymbolTable? _symbols;

    /// <summary>The ACTIVE lookup scope — the method whose statements are currently being bound, else program
    /// scope (the scope <c>LookupData</c>/<c>TryGetVisibleIndexField</c>/<c>IndexFieldFor</c> implied).</summary>
    internal Model.Scope ActiveScope => new(ActiveMethodScope);

    /// <summary>The lookup scope that OWNS <paramref name="anchorRoot"/> (M2-OO-1h): the owning METHOD's scope
    /// when the root is method-scoped (§11.7.4 GR5), else program scope. The post-build passes (OdoResolve /
    /// ResolveRedefines) resolve through this — <see cref="ActiveMethodScope"/> is null there, which is exactly
    /// why <c>LookupDataInScopeOf</c> existed.</summary>
    internal Model.Scope ScopeOf(DataItem anchorRoot) =>
        OoRootOwner.TryGetValue(anchorRoot, out var m) ? new Model.Scope(m.DataScope) : Model.Scope.Program;

    // The former LookupData / LookupDataInScopeOf / TryGetVisibleIndexField / IndexFieldFor quadruple is DELETED
    // (P6 Step 7b) — every consumer resolves through Symbols.TryResolve/IndexCandidates with an
    // EXPLICIT Scope (ActiveScope at statement-bind sites; ScopeOf(anchorRoot) at post-build-pass sites).

    /// <summary>Set while binding a METHOD's data entries (M2-OO-1h step 4) so INDEXED BY index-names register into
    /// the method's own scope, never the unit's. Null at program/object scope.</summary>
    private OoMethodDataScope? _bindingMethodScope;

    /// <summary>True when <paramref name="item"/> is OBJECT data of a class unit (§14.9.23.3 SR 10): its 01/77
    /// root is not method-scoped. Always false outside a class unit.</summary>
    public bool OoIsObjectData(DataItem item)
    {
        if (!OoIsClassUnit) return false;
        return !OoMethodScopedRoots.Contains(item.Root);
    }

    /// <summary>
    /// Bind one METHOD's data sections into the class forest and its own name scope, and resolve the method's
    /// PROCEDURE DIVISION USING/RETURNING formals (positional, §14.9.23.4 GR3). Runs between
    /// <see cref="BindDeclarations"/> and <see cref="BindResolve"/> so the post-build passes cover method items.
    /// Part-2-of-slice-2 boundaries stage LOUD (never silently drop): REDEFINES / OCCURS INDEXED BY / OCCURS
    /// DEPENDING / EXTERNAL / GLOBAL / level-66 inside method data, and a FILE or REPORT section in a method.
    /// </summary>
    internal void OoBindMethodData(OoMethodSymbol m)
    {
        string where = $"method '{m.Name}'";
        m.Binding ??= new OoMethodBinding();   // the after-data-bind half attaches HERE (P9 R7 — phase-explicit)
        _ooBoundMethods.Add(m);
        // A method's environment division may not carry the configuration section (§12.3.3 SR2) or the
        // input-output section (§12.4.3 SR1) — a bare header is legal (§10.6.1, §12.2.1). One placement table
        // for every OO definition kind (kb/Work PB1076). A method references the class's files via §11.7.4 GR5.
        OoEnvironmentRules.Screen(OoDefinition.Method, where, m.Ctx.environmentDivision(), Edition);

        // §10.6.1's method skeleton prints [options-paragraph] (kb/Work PB135): fold it over the class
        // channel's model per §11.9.4 GR1; the roster swaps it in around this method's body bind.
        if (m.Ctx.optionsParagraph() is { } mo)
            m.MethodOptions = OptionsBinder.BindParagraph(mo, Edition, Options);

        var dd = m.Ctx.dataDivision();
        _bindingMethodScope = m.DataScope;   // M2-OO-1h step 4: route INDEXED BY index-names to the method scope
        if (dd is not null)
        {
            // The method's constant entries are declared before its sections bind, so one may be referenced before
            // the entry that defines it (kb/Work PB1231); BindRemainingConstants below binds what nothing needed.
            DeclareDataEntries(dd);
            // The sections a method definition may not carry — FILE, WORKING-STORAGE, REPORT, SCREEN (§13.4.3,
            // §13.5.3, §13.8.3, §13.9.3 SR1) — are judged once, at EVERY edition, by the one DATA-division placement
            // table (kb/Work PB1251, PB1308 decision R62). A method's own data division is LOCAL-STORAGE (§13.6.3) and
            // LINKAGE (§13.7.3), both bound below.
            OoDefinitionRules.Screen(OoDefinition.Method, where, dd, Edition);
            // §13.18.27.3 SR4: the GLOBAL clause is barred in a method definition — on a level-01 item of ANY
            // section a method may own (LOCAL-STORAGE / LINKAGE). Spec-FORBIDDEN (COBOLNET1520), not merely
            // unimplemented.
            void GateMethodGlobal(IEnumerable<Core.DataDescriptionEntryContext> entries)
            {
                foreach (var e in entries)
                    if (e.dataDescriptionBody()?.dataDescriptionClauses()?.dataDescriptionClause()
                            ?.Any(cl => cl.globalClause() is not null) == true)
                        Edition.Error("COBOLNET1520", $"{where}: a data item specifies the GLOBAL clause — GLOBAL "
                            + "shall not be specified in a factory, instance, or method definition (ISO §13.18.27.3 SR4)");
            }
            if (dd.workingStorageSection() is { } ws)
            {
                // §13.5.3 SR1 refused this section above (OoDefinitionRules, at EVERY edition — 2002, 2014 and 2023
                // alike, kb/Work PB1308 decision R62; never an edition gate, never relaxed by --permissive). The
                // entries are still bound — in their own (working-storage) section, so every section rule keeps
                // answering — into the method's local roots: recovery only (the compile has failed), so the method
                // body's references to these names do not draw a cascade of "not defined" errors that would bury the
                // real one. There is no static-field mapping of method data any more.
                m.Binding!.LocalRoots.AddRange(BindEntries(ws.dataDescriptionEntry(), _rootNames));
            }
            if (dd.localStorageSection() is { } ls)
            {
                GateMethodGlobal(ls.dataDescriptionEntry());
                m.Binding!.LocalRoots.AddRange(BindEntries(ls.dataDescriptionEntry(), _rootNames, EntrySection.LocalStorage));
            }
            if (dd.linkageSection() is { } lk)
            {
                var lkEntries = lk.linkageEntry().Select(e => e.dataDescriptionEntry())
                    .Where(e => e is not null).Select(e => e!).ToList();
                GateMethodGlobal(lkEntries);
                m.Binding!.LinkageRoots.AddRange(BindEntries(lkEntries, _rootNames, EntrySection.Linkage));
            }
            BindRemainingConstants();
        }
        _bindingMethodScope = null;

        foreach (var root in m.Binding!.LocalRoots.Concat(m.Binding!.LinkageRoots))
        {
            OoMethodScopedRoots.Add(root);
            OoRootOwner[root] = m;   // M2-OO-1h: the post-build passes resolve names through the owning method
            OoGateUnsupportedShapes(root, where);
            OoScopeSubtree(root, m.DataScope);
        }
        // M2-OO-1h step 4: a method table's index cell is a per-activation method local (emitted in OoEmitMethod).
        // LINKAGE + LOCAL-STORAGE roots are C# LOCALS of the emitted method (their struct types and numeric
        // profiles still emit at class level) — never instance fields.
        foreach (var root in m.Binding!.LocalRoots.Concat(m.Binding!.LinkageRoots))
            _callSuppressedRootFields.Add(root.CsName);

        // The PD header formals (§14.2.2 SR1 — level-01/77 LINKAGE entries; correspondence is positional).
        // The using-phrase modes (§14.2.1: { [BY REFERENCE] { [OPTIONAL] d }… | BY VALUE { d }… }…) are TRANSITIVE
        // across the parameters that follow until the other phrase appears, BY REFERENCE assumed before the first
        // (§14.2.3 GR4) — threaded flat over the grammar's one-parameter-per-node shape, exactly as the program arm
        // does (CallBindLinkage). A BY VALUE formal is recorded (OoFormal.ByValue): the activator hands it a detached
        // value and the method never copies it back (kb/Work PB1051). The OPTIONAL phrase CARRIES (kb/Work PB757): the
        // formal records it, and the method ABI pairs every formal with its omitted-presence parameter
        // (OoFormal.OmittedFlag) — the method arm of the one presence fact the program arm's null carrier realizes
        // (kb/Work PB133 wave C).
        var pd = m.Ctx.procedureDivision();
        int pos = 0;
        bool byValue = false;
        var header = new ProcedureHeaderScreen(Edition, where);   // §14.2.2 SR1/SR2/SR5/SR6 — the ONE header screen (kb/Work PB1145)
        foreach (var prm in pd?.usingClause()?.usingParameter() ?? [])
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
            string pname = dref.GetText();
            var item = m.Binding!.LinkageRoots.FirstOrDefault(r =>
                CobolNames.Same(r.CobolName, pname));
            if (item is null)
                Edition.Error("COBOLNET0888", $"{where}: PROCEDURE DIVISION USING parameter '{pname}' is not "
                    + "a level-01/77 LINKAGE SECTION item of the method (ISO §14.2.2 SR1)");
            else if (header.AdmitFormal(pname, item))
            {
                if (optional && byValue) header.OptionalNeedsByReference(pname);   // §14.2.1: OPTIONAL is a BY REFERENCE word
                if (byValue) header.ByValueClass(pname, item);                      // §14.2.2 SR2
                m.Binding!.Formals.Add(new OoFormal(item, pos, OoParamName(m, item, pos), optional, byValue));
            }
            pos++;
        }
        if (pd?.returningClause()?.dataReference() is { } rref)
        {
            m.Binding!.Returning = m.Binding!.LinkageRoots.FirstOrDefault(r =>
                CobolNames.Same(r.CobolName, rref.GetText()));
            if (m.Binding!.Returning is null)
                Edition.Error("COBOLNET0888", $"{where}: PROCEDURE DIVISION RETURNING item '{rref.GetText()}' "
                    + "is not a level-01/77 LINKAGE SECTION item of the method (ISO §14.2.2 SR1)");
            else
                header.CheckReturning(rref.GetText(), m.Binding!.Returning);   // SR5 / SR6 — SR6 was mis-cited "SR4" here
        }
        // §13.7.3 SR4: which of the method's linkage records its procedure division may reference — the ONE decision
        // the program/function arm takes at the same point (SR5 bars a FUNCTION's formals, so a method has none).
        SealLinkageRules(m.Binding!.LinkageRoots, m.Binding!.Formals.Select(f => f.Item).ToList(),
            m.Binding!.Returning, function: false);
        // The method's PD-header RAISING phrase (§14.2.1): the ONE partition every header arm calls
        // (RaisingPhrase — kb/Work PB815/PB814; D-EO8), so the method arm and the program arm cannot disagree.
        m.Raising.AddRange(RaisingPhrase.Partition(pd?.raisingClause(), OoClasses, Edition,
            $"{where}: METHOD-ID RAISING"));
        // A GROUP formal/RETURNING item crosses the boundary as its character image (§14.2.3 GR8) and so must be
        // whole-group-referenced (its numeric-DISPLAY leaves image-stored, untouched caller bytes round-tripping) —
        // registered post-bind by UsageCollectionPass (PHASE-05 Step 5), which receives these formals from the emitter.

        // ── The ANY LENGTH placement sweep, METHOD path (ISO §13.18.2.3 SR2/SR3; the CallBindLinkage sweep is
        // the program/function/object path). SR2: LINKAGE only, elementary, and the containing method shall not
        // be a PROPERTY method (an explicit METHOD-ID GET|SET PROPERTY — Accessor != '\0'; a PROPERTY-clause-
        // synthesized accessor clones object data, where the clause is already rejected). SR3: referenced in the
        // method's PD header as a BY REFERENCE formal (SR3a — a BY VALUE formal does not qualify) or the RETURNING
        // item (SR3b). Violations clear the flag (the IsBased discipline). ──
        foreach (var root in m.Binding!.LocalRoots)
            if (root.IsAnyLength)
            {
                Edition.Error("COBOLNET1542", $"{where}: data item '{root.CobolName ?? "FILLER"}': the ANY "
                    + "LENGTH clause may be specified only in the LINKAGE SECTION (ISO §13.18.2.3 SR2)");
                root.IsAnyLength = false;
            }
        foreach (var root in m.Binding!.LinkageRoots)
        {
            if (!root.IsAnyLength) continue;
            string rw = $"{where}: data item '{root.CobolName ?? "FILLER"}'";
            if (root.IsGroup)
                Edition.Error("COBOLNET1542", $"{rw}: the subject of an ANY LENGTH clause shall be ELEMENTARY "
                    + "— this entry has subordinate items (ISO §13.18.2.3 SR2)");
            else if (m.Accessor != '\0')
                Edition.Error("COBOLNET1542", $"{rw}: the ANY LENGTH clause may not be specified in a PROPERTY "
                    + "method (ISO §13.18.2.3 SR2 — a method that is not a property method)");
            else if (!m.Binding!.Formals.Any(f => ReferenceEquals(f.Item, root) && !f.ByValue)   // SR3 a): a BY VALUE formal does not qualify
                && !ReferenceEquals(m.Binding!.Returning, root))
                Edition.Error("COBOLNET1542", $"{rw}: the subject of an ANY LENGTH clause shall be referenced "
                    + "in the method's procedure division header as a BY REFERENCE formal parameter or as the "
                    + "RETURNING item (ISO §13.18.2.3 SR3)");
            else
                continue;   // conformant — keep the flag. A RETURNING item (SR3 b)) receives the INVOKE receiver's length
                            // through the method's trailing __retLen parameter (§13.18.2.4 GR1 b); OoEmitter.OoSignatureOf).
            root.IsAnyLength = false;
        }
    }

    /// <summary>The C# parameter name for a formal: the TAGGED family <see cref="NamingConvention.FormalParameterName"/>
    /// (<c>__formal_</c> + the sanitized COBOL name), uniquified within the method. The <c>__</c> head keeps it
    /// off the item's own <see cref="DataItem.CsName"/> (a user word never sanitizes to a leading <c>__</c>), which
    /// becomes the capturable LOCAL the body reads and writes (a C# local may not shadow a parameter — CS0136);
    /// the non-empty TAG keeps it off the emitter's fixed <c>__X</c> members — the untagged <c>"__" + word</c>
    /// shape turned a formal named N into <c>__N</c>, the paragraph-count constant (kb/Work PB973).</summary>
    private static string OoParamName(OoMethodSymbol m, DataItem item, int pos)
    {
        string name = NamingConvention.FormalParameterName(item.CobolName ?? $"P{pos}");
        while (m.Binding!.Formals.Any(f => f.ParamName == name)) name += "_";
        return name;
    }

    /// <summary>Move a method root subtree's NAME registrations (items + level-88 conditions) out of the class
    /// globals into the method's own scope (§11.7 GR5 shadowing / sibling invisibility).</summary>
    private void OoScopeSubtree(DataItem item, OoMethodDataScope scope)
    {
        if (item.CobolName is { } n)
        {
            if (ByName.TryGetValue(n, out var list))
            {
                list.Remove(item);
                if (list.Count == 0) ByName.Remove(n);
            }
            if (!scope.ByName.TryGetValue(n, out var slist)) scope.ByName[n] = slist = [];
            slist.Add(item);
        }
        // Level-88s under this item: transfer by conditional-variable identity.
        foreach (var (condName, conds) in Conditions.ToList())
        {
            List<Condition88>? moved = null;
            foreach (var c in conds)
                if (ReferenceEquals(c.Parent, item))
                    (moved ??= []).Add(c);
            if (moved is null) continue;
            foreach (var c in moved) conds.Remove(c);
            if (conds.Count == 0) Conditions.Remove(condName);
            if (!scope.Conditions.TryGetValue(condName, out var sc)) scope.Conditions[condName] = sc = [];
            sc.AddRange(moved);
        }
        foreach (var child in item.Children) OoScopeSubtree(child, scope);
        foreach (var ren in item.Renames66) OoScopeSubtree(ren, scope);
    }

    /// <summary>One RESOLVED object-property reference awaiting its statement-level desugar (deep-dive
    /// D-P2; ISO §8.4.3.9.4): the synthesized temp the statement bound over, the receiver (a Place for the
    /// instance form, null for the <c>prop OF Class-name</c> factory form), the accessor symbols found on
    /// the pinned-name roster (either may be null — SR3/SR4 checked against the CLASSIFIED polarity, not
    /// eagerly), and the source names for diagnostics. Registered by ReferenceResolver at resolution time,
    /// drained by StatementBinder.OoWrapPropertyOps after the carrying statement binds.</summary>
    internal sealed record OoPendingPropertyOp(
        DataItem Temp, Place? Receiver, string ClassCsName, bool Factory,
        OoMethodSymbol? Get, OoMethodSymbol? Set, string PropName, string ReceiverName);

    /// <summary>The unit's un-drained property-reference ops (statement-scoped: BindStatement marks the
    /// count on entry and drains only its own suffix, so a reference in an IF condition belongs to the IF,
    /// not to an arm statement that binds later).</summary>
    internal List<OoPendingPropertyOp> OoPendingPropertyOps { get; } = [];

    /// <summary>⛔ THE ONE statement-scoped pending PRE-OP list — every activation that must run BEFORE the
    /// carrying statement, in REGISTRATION order. Two clients register here:
    /// <list type="bullet">
    ///   <item>a user-defined function activation (<c>UdfBinder</c>, ISO §8.4.3.2.4 GR1 — the caller-side
    ///         <c>BoundCallProgram</c> into the result temp);</item>
    ///   <item>a D18 function-bearing subscript / ref-mod segment's §15.4 temporary store
    ///         (<c>StatementBinder.MaterializeSubscriptSegment</c>, fix-queue PB17).</item>
    /// </list>
    /// <para>⛔ ONE LIST, NOT TWO, AND THAT IS A CORRECTNESS REQUIREMENT rather than tidiness: the two kinds
    /// INTERLEAVE. A function subscript inside a user-function argument — <c>FUNCTION F(W-E(FUNCTION INTEGER(3)))</c>
    /// — registers the subscript temp while F's arguments bind and F's activation after, so only a single list in
    /// registration order puts the store before the activation that consumes it. Two parallel lists would have to
    /// re-derive that ordering, and any rule for merging them would be a second mechanism for one job.</para>
    /// <para>Both kinds are UNCONDITIONAL pre-ops with identical hoist rules, because §8.4.3.2.3 SR1 makes a
    /// function-identifier a non-receiving operand — so unlike <see cref="OoPendingPropertyOps"/> there is no
    /// store-polarity classification and there are no post-ops.</para>
    /// <para>It lives on <c>DataBinder</c> because that is the ONLY binder both clients can reach: the procedure
    /// dependency is one-way, <c>StatementBinder(DataBinder, ReferenceResolver)</c>, so ReferenceResolver's
    /// materialization hook cannot register on <c>UdfBinder</c>. <c>UdfBinder.Mark</c> is the mark for the
    /// mark-on-entry / drain-own-suffix protocol at the <c>BindStatement</c> chokepoint, and it now covers BOTH
    /// kinds — which is why a function subscript in a per-evaluation window rides
    /// <c>UdfAttachPerEvaluation</c> with no extra wiring.</para></summary>
    internal List<Bound.BoundStatement> PendingPreOps { get; } = [];

    /// <summary>How many OPERAND-EVALUATION steps (<see cref="Bound.BoundStatement.OperandEvaluation"/> — a
    /// function reference, an inline method invocation, an object-property accessor, a §15.4 function-bearing
    /// subscript's temporary store; kb/Work PB892, PB1432) the statement now binding has drained so far — the test
    /// <c>StatementBinder.BindStatement</c> uses to wrap that statement in its <c>BoundActivationSite</c>
    /// (kb/Work PB892). BindStatement saves it, zeroes it and restores it around each statement, so a nested
    /// statement's activations count toward the nested statement only (§14.9.33.4 GR2 a) 3.: "the lowest level
    /// statement, not the containing statement").</summary>
    internal int OperandEvaluations { get; set; }

    /// <summary>Synthesize the GR1/GR2/GR3 compiler temp for one property reference: a level-1 elementary
    /// item CLONED from the accessor's crossing description (<paramref name="model"/> = the GET RETURNING
    /// item or the SET formal — identical by the §13.18.42 clone rule / the 0842 §11.7.3 SR7 single-USING accessor
    /// check). One temp per REFERENCE (GR1 temp-1 / GR2 temp-2; GR3 reuses one — the caller decides).</summary>
    internal DataItem OoCreatePropertyTemp(DataItem model, string prop) =>
        CreateCompilerTemp(model, "__PROP-TEMP-", "__prop", prop);

    /// <summary>Synthesize the §8.4.3.4.4 GR1 b)/c) temporary of one INLINE METHOD INVOCATION (kb/Work
    /// PB428): "temp-identifier has the same description, class, and category as the RETURNING parameter in
    /// the specification of the method identified by literal-1", and it "is a temporary item that exists for
    /// the purpose of effecting the inline invocation in this way and for no other purpose". <paramref
    /// name="model"/> is that RETURNING item, so the delivery into the temp is an IDENTITY crossing —
    /// which is why the §14.8.3.3 conformance check the written-RETURNING form runs has nothing to say here.
    /// The SAME <see cref="CreateCompilerTemp"/> the property and user-function temps use: one temp
    /// constructor, three clients.</summary>
    internal DataItem OoCreateInvocationTemp(DataItem model, string method) =>
        CreateCompilerTemp(model, "__INV-TEMP-", "__inv", method);

    /// <summary>The (temp, model) clone pairs <see cref="CreateCompilerTemp"/> produced — consumed by the
    /// run-unit emitter's post-bind re-sync: <c>StoreAsImage</c> is still MUTABLE while procedure bodies
    /// bind (a ref-mod store or a figurative MOVE inside the MODEL's own unit flips it AFTER a temp cloned
    /// it — the M2-UDF-1 review's unit-order desync), so the frozen copy must be re-read once every
    /// procedure has bound.</summary>
    internal List<(DataItem Temp, DataItem Model)> CompilerTempClones { get; } = [];

    /// <summary>The ONE synthesized-compiler-temp constructor (property-reference temps, user-function result
    /// temps): a level-1 item cloned from <paramref name="model"/>'s description, appended to
    /// <see cref="Roots"/> so the FieldEmitter declares it like any other item. A GROUP model deep-clones its
    /// subtree (the §8.4.3.2.4 GR1 "description … is that specified by the description in the linkage
    /// section" for a group RETURNING item) via <see cref="CloneTempNode"/> — structurally like the TYPEDEF
    /// <c>CloneItem</c> but UNREGISTERED: a temp's subordinates are never referenceable (a function result is
    /// only ever a whole sending operand, §8.4.3.2.3 SR1), and registering the callee's LINKAGE member names
    /// in the CALLER's scope would collide/ambiguate legal caller names. The pair is recorded for the
    /// post-bind <c>StoreAsImage</c> re-sync (see <see cref="CompilerTempClones"/>); a group temp's
    /// numeric-DISPLAY leaves are promoted by the <c>UsageCollectionPass</c> whole-group collection instead
    /// (the temp is a <c>BoundCallProgram.Returning</c> whole-group operand).
    /// <para>⛔ THE DESCRIPTION IS NOT LISTED HERE (kb/Work PB888). §8.4.3.2.4 GR1 gives the temporary "the
    /// description, class, and category" of the RETURNING item — the whole entry, not a chosen subset — and
    /// this constructor used to spell five clauses of it (PICTURE, SIGN, JUSTIFIED, BLANK WHEN ZERO, plus USAGE
    /// on a subordinate), so GROUP-USAGE was lost and a <c>GROUP-USAGE NATIONAL</c> result measured 8 where
    /// the byte-identical working-storage item measures 4. Every clause now goes through
    /// <see cref="CopyEntryDescription"/> in its <see cref="DescriptionCopyScope.CompilerTemp"/> scope, and
    /// <c>DescriptionCopyCompletenessDriftTests</c> fails if a hand copy reappears.</para></summary>
    internal DataItem CreateCompilerTemp(DataItem model, string cobolPrefix, string csPrefix, string tag)
    {
        var t = new DataItem
        {
            Level = 1,
            CobolName = cobolPrefix + _uidCounter,
            CsName = csPrefix + _uidCounter + "_" + DataItem.Sanitize(tag).ToUpperInvariant(),
            IsCompilerTemp = true,   // kb/Work R26 — the positive is-a-declared-item discrimination
            // (P5.7: the clone-time StoreAsImage seed is gone — StorageFormPass's promoted-set re-sync derives
            //  the temp's storage from its model's PRE-whole-group facts, the fused pipeline's re-sync ordering.)
        };
        CopyEntryDescription(model, t, DescriptionCopyScope.CompilerTemp);
        t.Uid = _uidCounter++;
        if (model.IsGroup)
            foreach (var child in model.Children)
                t.Children.Add(CloneTempNode(child, t));
        _roots.Add(t);
        CompilerTempClones.Add((t, model));
        return t;
    }

    /// <summary>Deep-clone one description node under a compiler temp (see <see cref="CreateCompilerTemp"/>):
    /// fresh <see cref="DataItem.Uid"/> (StructName/ProfileName ride on it), the immutable
    /// <see cref="DataItem.Pic"/> shared, the <see cref="DataItem.CsName"/> uniquified among siblings — and,
    /// unlike the TYPEDEF <c>CloneItem</c>, NOT registered (no by-name entry, no 88s, no index-names: a temp's
    /// subordinates are unreachable by reference). The initializer carries only identity and the
    /// <see cref="DescriptionCopyKind.MemberOnly"/> fields a subordinate reproduces (level, name, OCCURS); every
    /// data description CLAUSE goes through <see cref="CopyEntryDescription"/>, the same ONE copy the TYPE /
    /// SAME AS clone uses, so the temp's subtree cannot lose a clause the model's has (kb/Work PB888 — it had
    /// lost GROUP-USAGE, SYNCHRONIZED and ALIGNED, the last two of which decide a group's slack bytes).</summary>
    private DataItem CloneTempNode(DataItem src, DataItem newParent)
    {
        var clone = new DataItem
        {
            Level = src.Level,
            DeclaredAt = src.DeclaredAt,
            CobolName = src.CobolName,
            CsName = Unique(src.CsName, newParent.Children.Select(c => c.CsName)),
            Occurs = src.Occurs,
            OccursSpec = src.OccursSpec is { } os ? CloneOccursSpec(os) : null,
        };
        CopyEntryDescription(src, clone, DescriptionCopyScope.CompilerTemp);
        clone.Uid = _uidCounter++;
        clone.Parent = newParent;
        foreach (var child in src.Children)
            clone.Children.Add(CloneTempNode(child, clone));
        return clone;
    }

    /// <summary>SYNTHESIZE the accessor method symbols (D-P1 — the PINNED §11.7.4 GR1a implementor naming
    /// <c>__GET_&lt;P&gt;</c>/<c>__SET_&lt;P&gt;</c>) for every OBJECT/FACTORY working-storage item that carries a
    /// PROPERTY clause (§13.18.42): GET returns the SUBJECT item's description; SET takes one formal of it. The
    /// emitter renders DIRECT field bodies — observably identical to the spec's implicit MOVE methods (§13.18.42.4
    /// GR1/GR2) because the descriptions are identical by construction. WITH NO GET/SET suppresses the accessor;
    /// explicit GET/SET PROPERTY methods (already on the roster) take precedence — a clause + an explicit accessor
    /// for the same property is the §11.7 SR5 duplicate (0842).
    /// <para>⛔ THE SUBJECT IS THE ITEM THAT CARRIES THE CLAUSE (<see cref="DataItem.Property"/>, kb/Work PB1273).
    /// This used to walk the PARSE entries and find each subject by FIRST NAME MATCH over the whole forest, so with
    /// two same-named items the accessor bound whichever came first in tree order — a wrong answer, and the one
    /// SR3 exists to prevent. The §13.18.42.3 screens asked here are the ones that need the finished object name
    /// space and ancestry: SR2 (subject to an OCCURS clause, by ancestry too), SR3's qualification half, SR4
    /// (superclass collision), SR5 (CONSTANT RECORD requires WITH NO SET) and SR6 (no ACTIVE-CLASS object
    /// reference). SR1 is <c>BindEntry</c>'s; SR3's elementary half and §13.16.3 SR21 are the clause-placement
    /// table's.</para></summary>
    internal void OoBindPropertyClauses(OoClassSymbol cls, bool factory)
    {
        string where = $"class '{cls.Name}'{(factory ? " (FACTORY)" : "")}";
        // The object (or factory) data — this binder's roots that are not a METHOD's (OoIsObjectData), in
        // working storage (SR1 has refused every other placement at BindEntry, where the clause stays recorded).
        var objectItems = Roots.Where(r => !OoMethodScopedRoots.Contains(r)).SelectMany(Flatten).ToList();
        foreach (var subject in objectItems)
        {
            if (subject.Property is not { } pc || subject.Section is not EntrySection.WorkingStorage) continue;
            // SR3's ELEMENTARY half is the placement table's ElementaryOnly row (CheckElementaryOnlyClauses), which
            // runs over the finished forest in BindResolve — after this pass — and reports it. A group defines no
            // accessor meanwhile.
            if (subject.Children.Count > 0) continue;
            using var _ = Edition.At(subject);
            if (subject.CobolName is not { } subjName)
            {
                Edition.Error("COBOLNET0842", $"{where}: a PROPERTY clause requires a named data item "
                    + "(ISO §13.18.42 — FILLER cannot be a property subject)");
                continue;
            }
            string? fault =
                RecordLayout.OccursSubjectOf(subject) is { } table
                    ? (ReferenceEquals(table, subject) ? "it is" : $"it is subordinate to '{table.CobolName ?? "FILLER"}', which is")
                      + " described with an OCCURS clause; the PROPERTY clause shall not be specified for data items "
                      + "subject to an OCCURS clause (ISO §13.18.42.3 SR2)"
                : objectItems.Count(i => CobolNames.Same(i.CobolName, subjName)) > 1
                    ? $"the name '{subjName}' is not unique in the {(factory ? "factory" : "object")} data, so a "
                      + "reference to it requires qualification; the PROPERTY clause may be specified only for an "
                      + "elementary item whose name does not require qualification for uniqueness of reference "
                      + "(ISO §13.18.42.3 SR3)"
                : IsConstantRecordItem(subject) && !pc.NoSet
                    ? "it is in a CONSTANT RECORD, which cannot be stored into, and the clause does not say WITH NO "
                      + "SET; if the PROPERTY clause is specified in a data item described with the CONSTANT RECORD "
                      + "clause, or in any data item subordinate to one, the SET phrase shall be specified "
                      + "(ISO §13.18.42.3 SR5)"
                : subject.Pic?.ObjectRef is { Kind: ObjectRefKind.ActiveClass }
                    ? "it is an object reference described with the ACTIVE-CLASS phrase; the PROPERTY clause shall not "
                      + "be specified for data items of usage object reference described with an ACTIVE-CLASS phrase "
                      + "(ISO §13.18.42.3 SR6)"
                : null;
            if (fault is not null)
            {
                Edition.Error(DiagnosticCatalog.PropertyClauseRule, $"{where}: property subject '{subjName}': {fault}");
                continue;
            }
            DeclareUserWord(subjName, UserWordKind.PropertyName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
            // Superclass property-name collision (§13.18.42.3 SR4): walk the base chain's accessor rosters.
            for (var b = cls.Base; b is not null; b = b.Base)
                if ((factory ? b.FactoryMethods : b.Methods).Any(bm =>
                        CobolNames.Same(bm.PropertyName, subjName)))
                    Edition.Error("COBOLNET0842", $"{where}: property '{subjName}' collides with a property "
                        + $"of superclass '{b.Name}' (ISO §13.18.42.3 SR4)");

            if (!pc.NoGet)
                AddAccessor('G', NamingConvention.GetAccessorName(subjName));
            if (!pc.NoSet)
                AddAccessor('S', NamingConvention.SetAccessorName(subjName));

            void AddAccessor(char kind, string csName)
            {
                var m = new OoMethodSymbol(csName,
                    HasUsing: kind == 'S', HasReturning: kind == 'G',
                    Ctx: null!)
                {
                    CsName = csName, Owner = cls, IsFactory = factory,
                    Accessor = kind, PropertyName = subjName, PropertySubject = subject,
                    IsFinal = pc.IsFinal,
                };
                m.Binding = new OoMethodBinding();   // synthesized accessors carry their signature immediately
                if (kind == 'G') m.Binding.Returning = subject; else m.Binding.Formals.Add(new OoFormal(subject, 0, "__V"));
                bool added = factory ? cls.TryAddFactoryMethod(m) : cls.TryAddMethod(m);
                if (!added)
                    Edition.Error("COBOLNET0842", $"{where}: duplicate accessor for property '{subjName}' — "
                        + "a data-name with the PROPERTY clause shall not also have an explicit GET/SET "
                        + "PROPERTY method (ISO §11.7.3 SR5), and only one PROPERTY clause per name");
            }
        }

        static IEnumerable<DataItem> Flatten(DataItem i)
        {
            yield return i;
            foreach (var c in i.Children)
                foreach (var d in Flatten(c))
                    yield return d;
        }
    }

    /// <summary>Stage the method-data shapes slice 2 does not carry yet — LOUD, naming the owning wave.</summary>
    private void OoGateUnsupportedShapes(DataItem item, string where)
    {
        // REDEFINES in method data is LIVE (M2-OO-1h step 3, DEVLOG 639): ResolveRedefines scopes a top-level
        // method redefiner's target to the owning method's own roots (§13.18.44.3 SR / §11.7.4); the Tier-B
        // string backing is routed static (method-WS) or method-local (LOCAL/LINKAGE) by OoRouteMethodRedefinesBackings.
        // OCCURS … INDEXED BY in method data is LIVE (M2-OO-1h step 4, DEVLOG 640): index-names register into the
        // method's own scope with a FRESH cell (§11.7.4 GR5 privacy — no cross-method sharing), resolved via
        // Symbols.IndexCandidates (kb/Work PB919) and emitted static (method-WS) or per-activation local (LOCAL/LINKAGE).
        // OCCURS DEPENDING ON in method data is LIVE (M2-OO-1h step 2, DEVLOG 638): OdoResolve resolves
        // data-name-1 through Symbols.TryResolve(…, ScopeOf(RootOf(item))) — the method's own scope first
        // (§11.7.4 GR5), then a visible object item — instead of the raw global ByName.
        // level-66 RENAMES in method data is LIVE (M2-OO-1h step 1, DEVLOG 637): ResolveRedefines resolves the
        // alias FROM/THRU structurally via SubtreeCandidates over the owning record (kb/Work PB978 — counted),
        // so it is correct regardless of OoScopeSubtree's name re-homing — no gate needed.
        foreach (var child in item.Children) OoGateUnsupportedShapes(child, where);
        // Level-66s live OFF the children (Renames66) — without this walk the gate above is dead code and a
        // 66 in method data slips through unstaged (the 3a/3b review's dead-gate finding).
        foreach (var ren in item.Renames66) OoGateUnsupportedShapes(ren, where);
    }

    /// <summary>Route a method-scoped Tier-B REDEFINES class's ONE string backing to its method LOCAL (M2-OO-1h
    /// step 3): a method LOCAL-STORAGE / LINKAGE canonical → a method LOCAL (suppressed from the class-level field
    /// loop via <see cref="CallSuppressedRootFields"/>, emitted in <c>OoEmitMethod</c>). A method owns no
    /// WORKING-STORAGE (§13.5.3 SR1 — COBOLNET1519), so there is no static arm. Runs after
    /// <c>ClassifyRedefinesClasses</c>. A subordinate (02) canonical rides its root's composed initializer and
    /// needs no routing (its backing is a member of the method-local root struct already).</summary>
    internal void OoRouteMethodRedefinesBackings()
    {
        foreach (var root in OoMethodScopedRoots)
            if (root.Class is { Tier: RedefinesTier.StringCanonical } cls && ReferenceEquals(cls.Canonical, root))
                _callSuppressedRootFields.Add(cls.BackingCsName);   // emitted as a method local
    }
}
