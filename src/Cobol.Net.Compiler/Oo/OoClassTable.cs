// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Preprocessor;

using CobolNet.Binding;
using CobolNet.Binding.Model;

namespace CobolNet.Compiler.Oo;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// The PASS-1 class symbol table of a compilation group (OO deep-dive D1; ISO §11.2/§11.3/§11.7): built from
/// every <c>classDefinition</c> parse context BEFORE any unit binds, so a driver program's INVOKE / typed
/// <c>USAGE OBJECT REFERENCE</c> resolves a class defined LATER in the source file (the cross-unit hard
/// problem — "a genuine two-pass over the compilation group, owned by the BINDER"). Pass-2 (statement binding)
/// consults it to choose each INVOKE's call form and validate the method roster; the emitters only render the
/// bound facts. Class and method names compare case-insensitively (§8.3.2.2).
/// </summary>
public sealed class OoClassTable
{
    private readonly Dictionary<string, OoClassSymbol> _byName = new(CobolNames.Comparer);

    /// <summary>All classes in source order (the emitter's deterministic emission order).</summary>
    public IReadOnlyList<OoClassSymbol> Classes => _classes;
    private readonly List<OoClassSymbol> _classes = [];

    /// <summary>All interfaces in source order (§11.6 — emitted as C# interfaces BEFORE the classes, purely
    /// for readability; Roslyn needs no ordering). Interfaces and classes share ONE name namespace
    /// (<see cref="_ifaceByName"/> is checked against <see cref="_byName"/> at Build — a collision is COBOLNET0820, §8.3.2.2).</summary>
    public IReadOnlyList<OoInterfaceSymbol> Interfaces => _interfaces;
    private readonly List<OoInterfaceSymbol> _interfaces = [];

    /// <summary>The PARAMETERIZED class and interface definitions (§9.3.12 / §9.3.13), in source order: kept OUT of
    /// <see cref="Classes"/> / <see cref="Interfaces"/> and out of every name lookup — each is a skeleton, its
    /// expansions are the classes (<see cref="OoExpansion"/>) — but each is still a class or interface definition, so
    /// the binder binds it as itself, for its own rules, and emits nothing from it (kb/Work PB2051; OO deep-dive D12).</summary>
    public IReadOnlyList<OoClassSymbol> ParameterizedClasses => _parameterizedClasses;
    private readonly List<OoClassSymbol> _parameterizedClasses = [];
    /// <summary>The interface twin of <see cref="ParameterizedClasses"/>.</summary>
    public IReadOnlyList<OoInterfaceSymbol> ParameterizedInterfaces => _parameterizedInterfaces;
    private readonly List<OoInterfaceSymbol> _parameterizedInterfaces = [];
    private readonly Dictionary<string, OoInterfaceSymbol> _ifaceByName = new(CobolNames.Comparer);

    /// <summary>The interface named <paramref name="name"/>, or null (case-insensitive, §8.3.2.2).</summary>
    public OoInterfaceSymbol? FindInterface(string name) => _ifaceByName.TryGetValue(name, out var i) ? i : null;
    /// <summary>The class named <paramref name="name"/>, or null (COBOL class names are case-insensitive). A class
    /// DEFINED in the compilation group wins; otherwise the name may be the standard class BASE (ISO §16.1), whose
    /// information the implementation's external repository always holds (§12.3.8.3 SR6 b) — the §12.3.8.4 GR6
    /// implementor-defined choice, documented at <see cref="OoStandardClasses"/>. This is the group-wide table, NOT
    /// the question "may this source element name it": that is <see cref="OoNameResolution"/>'s, and the standard
    /// class is as subject to it as any other (§8.4.6.4 — a REPOSITORY entry is still required).</summary>
    public OoClassSymbol? Find(string name) =>
        _byName.TryGetValue(name, out var c) ? c
        : CobolNames.Same(name, OoStandardClasses.BaseName) ? StandardBase
        : null;

    /// <summary>The class whose EXTERNALIZED name (its CLASS-ID's <c>AS literal-1</c>, or its name) is
    /// <paramref name="externalized"/> — what a REPOSITORY class-specifier's <c>AS literal-1</c> names (ISO §12.3.8.4
    /// GR2; kb/Work PB974). Compared as the group's other externalized names are (case-insensitive). The standard
    /// class BASE is externalized as its own name, and answers only when no definition of the group claims it —
    /// the same precedence as <see cref="Find"/>.</summary>
    public OoClassSymbol? FindByExternalizedName(string externalized) =>
        _classes.FirstOrDefault(c => CobolNames.Same(c.ExternalizedName, externalized))
        ?? (CobolNames.Same(externalized, StandardBase.ExternalizedName)
            ? StandardBase : null);

    /// <summary>This compilation group's symbol for the standard class BASE (ISO §16; <see cref="OoStandardClasses"/>).
    /// Never in <see cref="Classes"/>: it has no source, binds nothing and emits nothing.</summary>
    public OoClassSymbol StandardBase { get; } = OoStandardClasses.BuildBase();

    /// <summary>The interface twin of <see cref="FindByExternalizedName"/> (literal-2).</summary>
    public OoInterfaceSymbol? FindInterfaceByExternalizedName(string externalized) =>
        _interfaces.FirstOrDefault(i => CobolNames.Same(i.ExternalizedName, externalized));

    /// <summary>True when <paramref name="name"/> names a PARAMETERIZED class or interface definition of the group
    /// (kb/Work PB759). Such a definition is a skeleton (§9.3.12 / §9.3.13) and is deliberately NOT in this table
    /// — <see cref="OoExpansion"/> puts its EXPANSIONS here instead — so a lookup of its name misses; this is what
    /// lets <see cref="OoNameResolution.Resolve"/> say WHY (ISO §12.3.8.4 GR1: "If object-class-name-1 is a class
    /// described with the USING phrase, object-class-name-1 may be specified only in the REPOSITORY
    /// paragraph").</summary>
    public bool IsParameterized(string name) => _parameterized.Contains(name);
    private IReadOnlySet<string> _parameterized = EmptyNames;
    private int _dialectLevel;   // the edition's year: the spelling the repository AS literals are read in (kb/Work PB2840)
    private static readonly IReadOnlySet<string> EmptyNames = new HashSet<string>();

    /// <summary>The §8.4.6.4 name scope visible at <paramref name="site"/> — memoized per SOURCE ELEMENT so the
    /// ancestor walk runs once per program / class / interface / method definition rather than once per
    /// reference. ⛔ This table is the GROUP's set and is deliberately NOT the answer to "may this source
    /// element reference that name": go through <see cref="OoNameResolution"/>, which composes the two.</summary>
    public OoRepositoryScope RepositoryScopeFor(Antlr4.Runtime.RuleContext? site)
    {
        var key = SourceElementOf(site);
        if (key is null) return OoRepositoryScope.Empty;
        // Built from the SOURCE ELEMENT, not from `site`: the two walks are identical (nothing between a
        // reference and its source element carries a REPOSITORY paragraph or names a class), and starting at
        // the memo key makes that identity a property of the code rather than an argument about it.
        if (!_scopes.TryGetValue(key, out var scope))
            _scopes[key] = scope = OoRepositoryScope.Build(key, _variables, _dialectLevel);
        return scope;
    }

    /// <summary>The compilation group's &gt;&gt;DEFINE timeline — what a FROM constant-name written as an AS phrase's
    /// operand stands for before any DATA DIVISION binds (<see cref="PreBindConstants"/>; kb/Work PB2588).</summary>
    private DirectiveTimeline<CompilationVariableEvent> _variables = null!;

    // Keyed by parse-tree node IDENTITY: a ParserRuleContext does not override Equals, so the default
    // comparer already IS reference equality.
    private readonly Dictionary<Antlr4.Runtime.RuleContext, OoRepositoryScope> _scopes = [];

    /// <summary>The compilation's diagnostics, for the one rule this table reports at a reference
    /// (<see cref="RecordReferenceUse"/>). Null only for a table no <see cref="Build"/> made.</summary>
    private EditionContext? _edition;

    /// <summary>⛔ §8.3.2.2 at a REFERENCE (kb/Work PB3132): "Within a source element, a given user-defined word may be
    /// used as only one type of user-defined word". The object-class-names, interface-names and parameter-names a
    /// source element references are often declared by ANOTHER element: a method's by its CLASS (the CLASS-ID, the
    /// class's REPOSITORY, its USING clause), a contained program's, a factory definition's or an instance definition's
    /// by a CONTAINING element's REPOSITORY (§12.3.4 GR1), none of which is the referring element (§10.5: "A source element is a source unit
    /// excluding any contained source units"). So the referring element's census (<see cref="CensusOf"/>) learns each
    /// one here, from the one funnel every such reference resolves through (<see cref="OoNameResolution.Lookup"/>),
    /// whatever kind of element refers. A word the element declares itself as the same type is recorded twice, which
    /// is no conflict. A METHOD-ID spelled like its CLASS-ID, or a contained program's paragraph spelled like a class
    /// its container's REPOSITORY declares, is therefore legal until the element references that class-name, and then
    /// refused COBOLNET2692 at whichever of the two comes second, here or at the declaration funnel
    /// (<c>DataBinder.DeclareUserWord</c>). The names a CONFIGURATION SECTION declares reach a contained element the
    /// same way (§8.4.6.1), and the data binder records each reference to one here too
    /// (<c>DataBinder.RecordConfigurationNameUse</c>, kb/Work PB3153). False, recording nothing, when no census holds
    /// <paramref name="site"/> (a site in no source element, a table no <see cref="Build"/> made).</summary>
    internal bool RecordReferenceUse(Antlr4.Runtime.RuleContext? site, string name, UserWordKind kind)
    {
        if (_edition is null || CensusOf(site) is not { } elementWords) return false;
        if (elementWords.Record(name, kind) is not { } first) return true;
        using (_edition.At(site as Antlr4.Runtime.ParserRuleContext))
            _edition.Error(DiagnosticCatalog.UserWordTypeConflict, UserWordCensus.ConflictMessage(name, first, kind));
        return true;
    }

    /// <summary>THE §8.3.2.2 census of the source element <paramref name="site"/> lies in (§10.5), one per element for
    /// the whole compilation, so the binder that declares the element's words and every reference that uses one
    /// (<see cref="RecordReferenceUse"/>) record into the same object whatever order they run in. A method's is its
    /// symbol's (<see cref="OoMethodDataScope.Words"/>; kb/Work PB2921), found through <see cref="MethodAt"/>. Every
    /// other element's is kept here by its parse node: a program's (outermost or contained, the node its
    /// identification division hangs from: <c>BinderDriver.BindUnitData</c> passes that division), an interface's,
    /// a class definition's, and its factory and instance definitions', each its own: the class definition's
    /// environment division and CLASS-ID declare their words in the class's census alone, and the factory and
    /// instance binders inherit those names, never re-declare them (<c>OoDriver.BindClassData</c>; kb/Work PB3154).
    /// Null for a site in no source element, or in a method that has no symbol (one refused as a duplicate).</summary>
    internal UserWordCensus? CensusOf(Antlr4.Runtime.RuleContext? site)
    {
        var element = SourceElementOf(site);
        if (element is null) return null;
        if (element is Core.MethodDefinitionContext) return MethodAt(site)?.DataScope.Words;
        if (!_elementWords.TryGetValue(element, out var words))
            _elementWords[element] = words = new UserWordCensus();
        return words;
    }

    // Keyed by parse-tree node IDENTITY, like _scopes.
    private readonly Dictionary<Antlr4.Runtime.RuleContext, UserWordCensus> _elementWords = [];

    /// <summary>The method definition <paramref name="site"/> lies in, when its innermost source element is one: a
    /// class's instance or factory method, or an interface's method prototype, of this table's definitions or of a
    /// parameterized definition bound as itself. Memoized by the definition's parse node; a definition with no symbol
    /// (one refused as a duplicate) is looked up again and answers null.</summary>
    private OoMethodSymbol? MethodAt(Antlr4.Runtime.RuleContext? site)
    {
        if (SourceElementOf(site) is not Core.MethodDefinitionContext md) return null;
        if (_methodsByDefinition.TryGetValue(md, out var known)) return known;
        var found = _classes.Concat(_parameterizedClasses).SelectMany(c => c.Methods.Concat(c.FactoryMethods))
            .Concat(_interfaces.Concat(_parameterizedInterfaces).SelectMany(i => i.Prototypes))
            .FirstOrDefault(m => ReferenceEquals(m.Ctx, md));
        if (found is not null) _methodsByDefinition[md] = found;
        return found;
    }

    private readonly Dictionary<Core.MethodDefinitionContext, OoMethodSymbol> _methodsByDefinition = [];

    /// <summary>The innermost source element containing <paramref name="site"/> — the memo key, and the unit
    /// whose REPOSITORY (plus its containers') the scope is. A method definition is its own key even though it
    /// may not carry a REPOSITORY (§12.3.3 SR2): its scope is its class's, and keying on it costs one entry.</summary>
    private static Antlr4.Runtime.RuleContext? SourceElementOf(Antlr4.Runtime.RuleContext? site)
    {
        for (var c = site; c is not null; c = c.Parent)
            if (c is Core.ProgramUnitContext or Core.NestedProgramContext or Core.ClassDefinitionContext
                or Core.InterfaceDefinitionContext or Core.MethodDefinitionContext
                or Core.FactoryParagraphContext or Core.ObjectParagraphContext)
                return c;
        return null;
    }

    /// <summary>True when an item CROSSES the INVOKE boundary as a character string: groups (image crossing),
    /// image-stored numerics, alphanumeric / numeric-edited items. Native numerics and object references cross
    /// typed. The §14.8.2 strict-conformance bind rules guarantee both sides agree on the crossing form's
    /// WIDTH/description — which is what keeps the marshaling free of cross-class numeric profiles.
    /// (THE one definition — relocated from the emitter, P6 Step 5: the bind-phase harmonize below and the
    /// emitter's signature/marshaling renders both consult it.)</summary>
    public static bool StringCarried(DataItem item) =>
        item.IsGroup || item.StoreAsImage
        || item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean;   // string-stored (D-N1/D-B1) — char crossing

    /// <summary>⛔ THE LEAF-VECTOR CROSSING FORM (kb/Work PB1116): a strongly-typed group with NO boundary character
    /// image — §13.18.60.3 confines a subordinate object reference to a strong type declaration, so this is every
    /// strong group holding an object-reference (or class-pointer) leaf. It crosses an activation boundary as the
    /// <c>object?[]</c> of its record struct's physical fields (<c>AsLeaves</c> / <c>OfLeaves</c>, emitted by
    /// <c>RecordStructEmitter</c>): the two sides are of the SAME type (§14.8.2.2 / §14.8.3.2 "both shall be of the same
    /// type", decided before any carrier is chosen), so their fields correspond one to one, while their C# record
    /// structs are distinct (every TYPE clone has its own, and two equivalent type declarations in two source elements
    /// are two types) — a field-wise copy through a neutral vector is the one carrier that works between any two of
    /// them. An object reference is copied as a reference. A variable-length strong group keeps the §8.5.1.12 carrier.
    /// It is tested BEFORE <see cref="StringCarried"/>, which is true of every group.</summary>
    public static bool LeafCarried(DataItem item) =>
        item.IsGroup && !item.BoundaryImageCapable && !item.CurrentExtentImageCapable
        && !VariableLengthCompatibility.IsVariableLength(item) && StrongTypeModel.IsStrongGroup(item);

    /// <summary>The §11.8.4 GR2 closure: direct IMPLEMENTS + everything an implemented interface INHERITS +
    /// everything an inherited CLASS implements (transitively, cycle-safe).</summary>
    public IReadOnlyList<OoInterfaceSymbol> ImplementsClosure(OoClassSymbol cls, bool factory)
    {
        var result = new List<OoInterfaceSymbol>();
        var seen = new HashSet<OoInterfaceSymbol>();
        var seenCls = new HashSet<OoClassSymbol>();
        for (OoClassSymbol? c = cls; c is not null && seenCls.Add(c); c = c.Base)
            foreach (var direct in factory ? c.FactoryImplements : c.Implements)
                AddWithInherits(direct);
        return result;

        void AddWithInherits(OoInterfaceSymbol i)
        {
            if (!seen.Add(i)) return;
            result.Add(i);
            foreach (var b in i.Inherits) AddWithInherits(b);
        }
    }

    /// <summary>⛔ THE CLASS FACTS OF ONE OBJECT-REFERENCE DESCRIPTION, as ISO §14.9.39.3 SR10 / SR12 relate them
    /// (<see cref="ObjectReferenceOperand"/>; kb/Work PB2463) — the ONE derivation both the compile-time SET table
    /// (<see cref="OoConformance.ObjectRefAssignmentMismatch(OoClassTable, ObjectRefDescriptor, ObjectRefDescriptor, bool)"/>)
    /// and the run-time descriptions (<see cref="ActivationDescriptions"/>, <c>ActivationDescription.ObjectClasses</c> /
    /// <c>ObjectInterfaces</c>) read. An object-class-name description — and an ACTIVE-CLASS one, whose class is "the
    /// class containing the data item" (SR10 c), SR12 b) 2.) — has its class and every class it inherits from, and the
    /// §11.8.4 GR2 closure of what its objects (its factory object, FACTORY written) implement (SR10 b) / c)); an
    /// interface-name description has the interface and every interface it inherits from (SR10 a): "an interface that
    /// identifies int-1 or an interface inheriting from int-1"). A name this table does not resolve has neither.
    /// <para>A FORMAL description (<see cref="ObjectRefDescriptor.Formal"/>; kb/Work PB2937) names no class of the table
    /// — a parameter-name and a group class spelled alike are different words — so alone it is OPAQUE: its lineage is
    /// the parameter-name and nothing else, which relates it to exactly the descriptions naming the same parameter.
    /// A pair that relates a formal to something else is <see cref="SetOperands"/>'.</para></summary>
    public ObjectReferenceOperand SetOperand(ObjectRefDescriptor d)
    {
        IReadOnlyList<string>? classes = null, interfaces = null;
        if (d.Formal)
        {
            string[] self = [d.Name!];
            return new ObjectReferenceOperand(d.RuntimeKind, d.Name, d.Factory, d.Only,
                d.Kind is ObjectRefKind.Interface ? null : self, d.Kind is ObjectRefKind.Interface ? self : []);
        }
        if (d.Kind is ObjectRefKind.Interface && FindInterface(d.Name!) is { } i)
            interfaces = InterfaceLineage(i);
        else if (d.Kind is ObjectRefKind.ObjectClass or ObjectRefKind.ActiveClass && d.Name is { } n
                 && (Find(n) ?? ContainingSkeleton(d, n)) is { } c)
        {
            classes = ClassLineage(c);
            interfaces = ImplementsClosure(c, d.Factory).Select(x => x.Name).ToArray();
        }
        return new ObjectReferenceOperand(d.RuntimeKind, d.Name, d.Factory, d.Only, classes, interfaces);
    }

    /// <summary>⛔ THE SENDER AND RECEIVER OF ONE §14.9.39.3 PAIR, as the SET table relates them — <see cref="SetOperand"/>
    /// for each side, except that a FORMAL side (kb/Work PB2937) stands for the actual MOST FAVOURABLE to the pair. ISO
    /// §9.3.8.2.4 treats the definition "as if the actual parameter classes or interfaces were substituted for the
    /// parameters", so a definition bound as itself refuses a pair only when EVERY expansion would: a formal sender is
    /// taken to be the receiver's own class or interface (its class or a subclass of it, one implementing it), a formal
    /// receiver the sender's own class (or an interface the sender implements). What survives the substitution is what no
    /// actual can change — the alternative the formal's REPOSITORY specifier declares (an interface-typed sender into a
    /// class-typed receiver; any typed sender into ACTIVE-CLASS, SR14), the FACTORY and ONLY axes (SR12 a)1./a)3.), a
    /// universal sender into a typed receiver (SR10, SR12). Two references to the SAME parameter-name keep their opaque
    /// operands: they name one class, whatever it is.</summary>
    public (ObjectReferenceOperand Send, ObjectReferenceOperand Recv) SetOperands(ObjectRefDescriptor send,
        ObjectRefDescriptor recv)
    {
        var s = SetOperand(send);
        var r = SetOperand(recv);
        // An ACTIVE-CLASS sender in a definition whose superclass is a parameter-name: its class inherits from the actual,
        // which may be the receiver's class or one implementing the receiver's interface (§9.3.9) — most favourable too.
        if (send.IsActiveClass && send.Name is { } own && ContainingSkeleton(send, own)?.FormalBaseName is not null)
            s = s with { Classes = [.. s.Classes ?? [], .. r.Classes ?? []], Interfaces = [.. s.Interfaces ?? [], .. r.Interfaces ?? []] };
        if (send.Formal && recv.Formal && CobolNames.Same(send.Name, recv.Name)) return (s, r);
        if (send.Formal)
            return (s with { Name = r.Name ?? s.Name, Classes = r.Classes ?? s.Classes, Interfaces = r.Interfaces ?? s.Interfaces }, r);
        if (recv.Formal)
            return (s, r with
            {
                Name = s.Name ?? r.Name,
                Classes = s.Classes is [var c, ..] ? [c] : r.Classes,
                Interfaces = s.Interfaces is [var i, ..] ? [i] : r.Interfaces,
            });
        return (s, r);
    }

    /// <summary>The unexpanded parameterized class an ACTIVE-CLASS description written inside it names (its containing
    /// class, <see cref="ObjectRefDescriptor.ActiveClass"/>; kb/Work PB2937): it is in no name lookup (it creates no class),
    /// but its bodies bind as themselves and SELF there is described by it.</summary>
    private OoClassSymbol? ContainingSkeleton(ObjectRefDescriptor d, string name) =>
        d.IsActiveClass ? _parameterizedClasses.FirstOrDefault(c => CobolNames.Same(c.Name, name)) : null;

    /// <summary>The class and every class it inherits from, nearest first (cycle-safe; §11.3.3 SR4 refuses a cycle).</summary>
    private static string[] ClassLineage(OoClassSymbol cls)
    {
        var names = new List<string>();
        var seen = new HashSet<OoClassSymbol>();
        for (OoClassSymbol? c = cls; c is not null && seen.Add(c); c = c.Base)
            names.Add(c.Name);
        return [.. names];
    }

    /// <summary>The interface and every interface it inherits from, itself first (cycle-safe).</summary>
    private static string[] InterfaceLineage(OoInterfaceSymbol iface)
    {
        var names = new List<string>();
        var seen = new HashSet<OoInterfaceSymbol>();
        var stack = new Stack<OoInterfaceSymbol>([iface]);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (!seen.Add(cur)) continue;
            names.Add(cur.Name);
            foreach (var b in cur.Inherits) stack.Push(b);
        }
        return [.. names];
    }

    /// <summary>
    /// Build the table from the group's class definitions (pass-1: identity + roster only — no data or statement
    /// binding). Structural diagnostics raised here, each through its own <c>DiagnosticCatalog</c> descriptor (one
    /// rule, one code — kb/Work PB1784): a class or interface name declared twice (§8.4.6.4 / §8.3.2.2,
    /// <c>OoDefinitionNameNotUnique</c>); an INHERITS or IMPLEMENTS name that resolves to nothing in scope (§11.3.3 SR2
    /// <c>InheritedClassNotInScope</c> — never silently a root class — and §11.6.3 SR2 / §11.4.3 SR1 / §11.8.3 SR1
    /// <c>InterfaceNameNotInScope</c>); the INHERITS clauses' own rules, §11.3.3 SR3 / SR4 / SR5 / SR7 and §11.6.3 SR3
    /// / SR6 (<c>InheritsClauseRule</c>), over every class and interface definition, the parameterized ones
    /// (<paramref name="parameterizedClasses"/>, kept out of the table) included; OVERRIDE / FINAL in a prototype
    /// (§11.7.3 SR2 / SR8); and the two declined Annex A.4.10 items — multiple class inheritance (item 1) and a
    /// method-name defined twice in one definition (item 3: parametric polymorphism, ISO §9.3.5.3, whose support this
    /// implementation does not claim, deep-dive D9).
    /// <para>⚠ The three §12063 citations that stood here and at the two COBOLNET0822 emit sites were NOT
    /// clause numbers — ISO/IEC 1989:2023 has no §12063; the number was a stray line anchor, recorded as
    /// finding 44 of <c>docs/rearchitecture/DESIGN-SPEC-RECONCILIATION.md</c>. Re-derived and
    /// <c>cite.py --check</c>ed: §9.3.5.3 is "Parametric polymorphism", its rule 7 is "Parametric polymorphism
    /// is an optional feature in this Working Draft International Standard", and Annex A.4.10 item 3 is
    /// "Parametric polymorphism (9.3.5.3)". The CLAUDE.md rule-1 inherited-citation failure, caught at the
    /// point the rows it covers were being witnessed.</para>
    /// </summary>
    public static OoClassTable Build(IReadOnlyList<Core.ClassDefinitionContext> classes, EditionContext edition,
        IReadOnlyList<Core.InterfaceDefinitionContext>? interfaces,
        IReadOnlySet<string>? parameterizedNames,
        IReadOnlyList<Core.ClassDefinitionContext>? parameterizedClasses,
        IReadOnlyList<Core.InterfaceDefinitionContext>? parameterizedInterfaces,
        IReadOnlySet<string>? expandedNames,
        DirectiveTimeline<CompilationVariableEvent> variables)
    {
        var table = new OoClassTable
        {
            _parameterized = parameterizedNames ?? EmptyNames, _variables = variables,
            _dialectLevel = edition.Edition.Year, _edition = edition,
        };
        var usedCsNames = new HashSet<string>(StringComparer.Ordinal);

        // §11.3.3 SR1 / §11.6.3 SR1 / §11.7.3 SR1 — the AS phrase's literal, screened by the ONE screen
        // every AS site shares (kb/Work PB303). §11.3.3 SR1 is the outlier: it omits the zero-length
        // exclusion its four siblings carry, so CLASS-ID passes rejectZeroLength: false. Absent, or on a
        // rejected literal, the externalized name IS the declared word (§8.3.2.2 2)).
        string Externalized(Core.ExternalizedNamePhraseContext? phrase, string declared,
            string kind, string rule, bool rejectZeroLength = true)
        {
            if (phrase is null) return declared;
            using var _ = edition.At(phrase);
            // No DATA DIVISION is bound yet, so a constant-name operand (§13.10.3 SR2) is read off the parse tree
            // (kb/Work PB2588): a METHOD-ID's from its own and its object's or factory's entries (§11.7.4 GR5).
            return ExternalizedName.Screen(phrase, k => PreBindConstants.Read(phrase, k, variables), edition,
                       DiagnosticCatalog.ExternalizedNameLiteral,
                       $"{kind} '{declared}' AS {ExternalizedName.Written(phrase)}", "literal-1", rule,
                       LiteralEnvironment.Unscoped, rejectZeroLength)
                   ?? declared;
        }


        // ── INTERFACES first (§11.6) — classes' IMPLEMENTS then resolve regardless of source order ──
        foreach (var ictx in interfaces ?? [])
        {
            string iname = ictx.interfaceName(0).GetText();
            using var atInterface = edition.At(ictx.interfaceName(0));   // PB975 — every pass-1 report is positioned
            // COBOL-2002 introduction gate: VersionConformancePass ParseArm.VisitInterfaceDefinition (rearch 14g.3,
            // recognition — fires per parse node, so a duplicate/colliding definition dropped below still names its edition).
            var isym = NewInterfaceSymbol(ictx);
            string icsName = isym.CsName;
            if (table._ifaceByName.ContainsKey(iname) || table._byName.ContainsKey(iname)
                || !usedCsNames.Add(icsName))
            {
                edition.Error(DiagnosticCatalog.OoDefinitionNameNotUnique,
                    $"duplicate interface definition '{iname}' — interface definitions within a compilation group "
                    + "shall have unique interface-names (ISO §8.4.6.4)");
                continue;
            }
            table._ifaceByName.Add(iname, isym);
            table._interfaces.Add(isym);
            // The END INTERFACE and END METHOD names are §10.7.3 SR6 / SR5, asked by Validation.EndMarkerPass.
            AddPrototypes(isym);
        }
        // The PARAMETERIZED interface definitions (kb/Work PB2051): out of the table and every lookup, but interface
        // definitions all the same — their INTERFACE-ID and prototype rules are asked here, their formals bound later.
        // A definition some expansion re-binds is asked through that expansion only (one fact, one diagnostic:
        // OoExpansion.Result.ExpandedNames).
        foreach (var ictx in parameterizedInterfaces ?? [])
        {
            if (expandedNames?.Contains(ictx.interfaceName(0).GetText()) == true) continue;
            using var atInterface = edition.At(ictx.interfaceName(0));
            var isym = NewInterfaceSymbol(ictx);
            AddPrototypes(isym);
            table._parameterizedInterfaces.Add(isym);
        }

        // One interface definition's symbol (the table's and the parameterized ones); §11.6.3 SR1 screens the AS literal.
        OoInterfaceSymbol NewInterfaceSymbol(Core.InterfaceDefinitionContext ictx)
        {
            string iname = ictx.interfaceName(0).GetText();
            return new OoInterfaceSymbol(iname, DataItem.WordIdentifier(iname), ictx)
            {
                ExternalizedName = Externalized(ictx.externalizedNamePhrase(), iname,
                    "INTERFACE-ID", "ISO §11.6.3 SR1"),
            };
        }

        // PROTOTYPES (§10.6.2 SR4): a header + optional LINKAGE-only data division, NO procedure body,
        // NO OVERRIDE/FINAL attributes (§11.7 SR2/SR8 — the OVERRIDE/FINAL wave's forward obligation).
        void AddPrototypes(OoInterfaceSymbol isym)
        {
            var ictx = isym.Ctx;
            string iname = isym.Name;
            foreach (var m in ictx.methodDefinition())
            {
                string pname = (m.methodName().Length > 0 ? m.methodName(0)?.GetText() : null)
                    ?? m.methodPropertySelector()?.propertyName()?.GetText() ?? "?";
                using var atPrototype = edition.At(m);
                var pd = m.procedureDivision();
                if (m.OVERRIDE() is not null)
                    edition.Error(DiagnosticCatalog.MethodPrototypeOverrideOrFinal,
                        $"interface '{iname}', method '{pname}': the OVERRIDE phrase shall not be specified in a "
                        + "method prototype (ISO §11.7.3 SR2)");
                if (m.FINAL() is not null)
                    edition.Error(DiagnosticCatalog.MethodPrototypeOverrideOrFinal,
                        $"interface '{iname}', method '{pname}': the FINAL clause shall not be specified in a "
                        + "method prototype (ISO §11.7.3 SR8)");
                // §10.6.2 SR4 a)-f) — the ONE prototype-body screen every prototype kind shares (kb/Work PB894).
                // This arm used to carry its own two of the six restrictions, with the data division checked
                // against three of its five non-linkage sections.
                PrototypeUnitRules.ScreenBody($"interface '{iname}', method '{pname}'", m.optionsParagraph(),
                    m.environmentDivision(), m.dataDivision(), pd, edition);
                // A GET/SET PROPERTY prototype (kb/Work PB1449): §11.7.2's format is shared by method definitions and
                // prototypes, and §11.7.4 GR6/GR7 make a METHOD-ID with the GET / SET phrase a get / set property
                // method — so the prototype joins the roster under the SAME pinned accessor name a class's accessor
                // has (§11.7.4 GR1 a)), and IMPLEMENTS conformance pairs the two by that roster key.
                var psel = m.methodPropertySelector();
                string protoName = psel is null ? pname
                    : psel.GET() is not null ? NamingConvention.GetAccessorName(pname)
                    : NamingConvention.SetAccessorName(pname);
                var proto = new OoMethodSymbol(
                    protoName,
                    HasUsing: pd?.usingClause() is not null,
                    HasReturning: pd?.returningClause() is not null,
                    m)
                {
                    CsName = psel is null ? DataItem.WordIdentifier(pname) : protoName,
                    Accessor = psel is null ? '\0' : psel.GET() is not null ? 'G' : 'S',
                    PropertyName = psel is null ? null : pname,
                    // §11.7.2 prints [AS literal-1] on the method-name-1 arm only (the class arm's rule).
                    ExternalizedName = Externalized(m.externalizedNamePhrase(), protoName,
                        "METHOD-ID", "ISO §11.7.3 SR1"),
                };
                if (!isym.TryAddPrototype(proto))
                    edition.Declined(DiagnosticCatalog.ParametricPolymorphismDeclined,
                        $"interface '{iname}': a second method prototype named '{pname}'");
            }
        }
        // Interface INHERITS resolution + cycle check (§11.6.3 SR2/SR3/SR6). The INHERITS clause of a PARAMETERIZED
        // interface is asked too (kb/Work PB2051): nothing resolves to a skeleton, so it cannot close a cycle, and a
        // base written as one of its parameter-names resolves as a formal, which each expansion's own clause answers.
        foreach (var isym in table._interfaces.Concat(table._parameterizedInterfaces))
        {
            // §11.6.3 SR6: "A given interface-name shall not appear more than once in an INHERITS clause" — the rule is
            // about the WRITTEN NAME (two spellings are one name under the Annex C fold, §8.1.3.2 GR3 b)/GR4 b)), not the interface it resolves to:
            // two different REPOSITORY names for one externalized interface (§12.3.8.3 — INTERFACE R6X AS "E" and
            // INTERFACE R6Y AS "E") are two interface-names, which no sentence of the standard forbids, so both are
            // legal and name ONE base (docs/CONFORMANCE.md D-INH1; kb/Work PB1502, following the PB946 determination).
            var writtenNames = new HashSet<string>(CobolNames.Comparer);
            foreach (var inhCtx in isym.Ctx.interfaceName().Skip(1).Take(isym.Ctx.interfaceName().Length - 2))
            {
                string inh = inhCtx.GetText();
                using var atInherits = edition.At(inhCtx);
                // SR6 is asked of the written list before any name resolves, as the class twin's SR7 is
                // (ScreenInheritsNames): a repeated name that resolves to nothing is the repetition, reported once.
                if (!writtenNames.Add(inh))
                {
                    edition.Error(DiagnosticCatalog.InheritsClauseRule,
                        $"interface '{isym.Name}': INHERITS FROM names '{inh}' more than once; a given interface-name "
                        + "shall not appear more than once in an INHERITS clause (ISO §11.6.3 SR6)");
                    continue;
                }
                // §11.6.3 SR2: "Interface-name-2 shall be the name of an interface specified in the REPOSITORY
                // paragraph of this source element" — the class-INHERITS rule's twin, same funnel (PB365).
                if (OoNameResolution.Resolve(table, edition, isym.Ctx, inh, OoNameResolution.Want.Interface,
                        $"interface '{isym.Name}': INHERITS FROM", DiagnosticCatalog.InterfaceNameNotInScope.Code,
                        "ISO §11.6.3 SR2").Interface is { } b
                    && !isym.Inherits.Contains(b))
                    isym.Inherits.Add(b);   // an alias of a base already inherited adds nothing (one C# base)
            }
        }
        foreach (var isym in table._interfaces)
        {
            using var atInterface = edition.At(isym.Ctx.interfaceName(0));
            var seenI = new HashSet<OoInterfaceSymbol>();
            var stack = new Stack<OoInterfaceSymbol>([isym]);
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                foreach (var b in cur.Inherits)
                {
                    if (ReferenceEquals(b, isym))
                    {
                        edition.Error(DiagnosticCatalog.InheritsClauseRule,
                            $"interface '{isym.Name}': the INHERITS graph is cyclic; interface-name-2 shall not "
                            + "inherit directly or indirectly from interface-name-1 (ISO §11.6.3 SR3)");
                        stack.Clear();
                        break;
                    }
                    if (seenI.Add(b)) stack.Push(b);
                }
            }
        }

        // §11.7.3 SR4 b): "if this method definition is contained in an interface definition, no inherited method
        // prototype shall have the same method resolution signature as the method prototype declared by this method
        // definition" — the interface twin of the class arm's SR4 a) (the OVERRIDE marking below). Since SR2 forbids
        // OVERRIDE in a prototype there is no legal way to redeclare an inherited prototype, so ANY inherited
        // prototype of the same roster key (the v1 method resolution signature is the method-name, kb/Work PB1519) is a
        // violation. Asked after the INHERITS graph resolved (it needs the closure) and before any formal binds (the
        // key needs none); a cyclic graph is reported above and the closure walk terminates over it.
        foreach (var isym in table._interfaces.Concat(table._parameterizedInterfaces))
            foreach (var own in isym.Prototypes)
                foreach (var inherited in isym.InheritedClosure())
                    if (inherited.FindOwnPrototype(own.ExternalizedName) is not null)
                    {
                        using var atPrototype = edition.At(own.Ctx);
                        edition.Error(DiagnosticCatalog.InterfacePrototypeRedeclaresInherited,
                            $"interface '{isym.Name}', method '{own.Name}': the method prototype has the same method "
                            + $"resolution signature as one inherited from interface '{inherited.Name}' (ISO §11.7.3 SR4 b); "
                            + "OVERRIDE, the only way to redeclare an inherited method, is forbidden in a prototype by SR2)");
                        break;
                    }

        foreach (var ctx in classes)
        {
            var id = ctx.classIdParagraph();
            string name = id.className(0).GetText();
            using var atClass = edition.At(id.className(0));
            // COBOL-2002 introduction gate: VersionConformancePass ParseArm.VisitClassDefinition (rearch 14g.3,
            // recognition — fires per parse node, so a duplicate/colliding definition dropped below still names its edition).
            var sym = NewClassSymbol(ctx);
            string csName = sym.CsName;
            ScreenInheritsNames(id, name, edition);
            // A class named like an interface is ONE report, not two: the interface already holds the emitted name, so
            // the duplicate-class arm below would also have fired on the same definition.
            if (table._ifaceByName.ContainsKey(name))
            {
                edition.Error(DiagnosticCatalog.OoDefinitionNameNotUnique,
                    $"'{name}' is defined as both a class and an interface — two kinds of entity cannot share one "
                    + "externalized name (ISO §8.3.2.2)");
                continue;
            }
            if (!table._byName.TryAdd(name, sym) || !usedCsNames.Add(csName))
            {
                edition.Error(DiagnosticCatalog.OoDefinitionNameNotUnique,
                    $"duplicate class definition '{name}' — object class definitions within a compilation group shall "
                    + "have unique object-class-names (ISO §8.4.6.4; class names compare case-insensitively and map to "
                    + "one emitted type)");
                continue;
            }
            // The factory class's name is a NamingConvention prefix family no class-name spells (kb/Work PB2921), so it
            // is free whenever the class's own name was: a failure here is a defect of the naming convention.
            if (!usedCsNames.Add(sym.FactoryCsName))
                throw new InvalidOperationException(
                    $"factory class name '{sym.FactoryCsName}' of class '{name}' is already taken (kb/Work PB2921)");
            table._classes.Add(sym);
            // The END CLASS and END METHOD names are §10.7.3 SR4 / SR5, asked by Validation.EndMarkerPass.
            AddMethods(sym);
        }

        // One class definition's symbol: its names, its CLASS-ID attributes (§11.3.3 SR1 screens the AS literal), its
        // INHERITS operands as written. Shared by the table's classes and the parameterized definitions (kb/Work PB2051),
        // so a CLASS-ID rule asked here is asked of every class definition the source holds.
        OoClassSymbol NewClassSymbol(Core.ClassDefinitionContext ctx)
        {
            var id = ctx.classIdParagraph();
            string name = id.className(0).GetText();
            string csName = DataItem.WordIdentifier(name);   // MUST match PicInfo.ClrType's mapping
            var bases = id.className().Skip(1).Select(c => c.GetText()).ToList();
            return new OoClassSymbol(name, csName, ctx)
            {
                // §11.3.3 SR1 alone omits the zero-length exclusion (verified on the printed page).
                ExternalizedName = Externalized(id.externalizedNamePhrase(), name,
                    "CLASS-ID", "ISO §11.3.3 SR1", rejectZeroLength: false),
                Bases = bases,
                BaseName = bases.Count >= 1 ? bases[0] : null,
                IsFinal = id.FINAL() is not null,
            };
        }

        // The instance and factory method rosters of one class definition (the table's and the parameterized ones).
        void AddMethods(OoClassSymbol sym)
        {
            var ctx = sym.Ctx;
            string name = sym.Name, csName = sym.CsName;
            foreach (var m in ctx.objectParagraph()?.methodDefinition() ?? [])
            {
                using var atMethod = edition.At(m);
                var sel = m.methodPropertySelector();
                string methodName = sel is not null
                    ? (sel.GET() is not null
                        ? NamingConvention.GetAccessorName(sel.propertyName().GetText())
                        : NamingConvention.SetAccessorName(sel.propertyName().GetText()))
                    : m.methodName(0).GetText();
                var pd = m.procedureDivision();
                string mcs = sel is not null ? methodName : DataItem.WordIdentifier(methodName);
                // CS0542 guard: a C# member may not be named like its enclosing type — a METHOD-ID named like
                // its CLASS-ID is legal COBOL, so the SYMBOL renames. Derivation (kb/Work PB975): §8.3.2.2 bars
                // one word as two types of user-defined word only "Within a source element"; §10.4 makes a method
                // a source unit "directly contained within" its factory/instance definition, and §10.5 "A source
                // element is a source unit excluding any contained source units" — so the method-name and the
                // class-name sit in DIFFERENT source elements, which §8.4.6.1 lets "use identical user-defined
                // words ... independent of the use of these user-defined words by other source elements".
                if (mcs == csName) mcs = NamingConvention.ClassNamedMethodName(mcs);
                var method = new OoMethodSymbol(
                    methodName,
                    HasUsing: pd?.usingClause() is not null,
                    HasReturning: pd?.returningClause() is not null,
                    m)
                { CsName = mcs, Owner = sym, HasOverride = m.OVERRIDE() is not null, IsFinal = m.FINAL() is not null,
                  Accessor = sel is null ? '\0' : sel.GET() is not null ? 'G' : 'S',
                  PropertyName = sel?.propertyName().GetText(),
                  // §11.7.2 prints [AS literal-1] on the method-name-1 arm only — a GET/SET PROPERTY
                  // method's name is implementor-defined (§11.7.4 GR1 a), so `sel` never has one.
                  ExternalizedName = Externalized(m.externalizedNamePhrase(), methodName,
                      "METHOD-ID", "ISO §11.7.3 SR1") };
                // §11.7.3 SR6/SR7 (the accessor's header shape) are asked of the BOUND header, by
                // DataBinder.OoBindMethodData — the parameter count and the ACTIVE-CLASS descriptor (kb/Work PB1503).
                if (!sym.TryAddMethod(method))
                    edition.Declined(DiagnosticCatalog.ParametricPolymorphismDeclined,
                        $"class '{name}': a second method named '{methodName}' (OO deep-dive D9)");
            }

            // FACTORY methods (§11.4) — a SEPARATE roster/interface (§9.3.6: an instance method and a
            // factory method may share a name). A factory METHOD-ID named NEW is an ordinary method (kb/Work PB1582):
            // New belongs to the standard class BASE's factory interface (§16.2), so outside BASE's hierarchy NEW is
            // just a method-name, and inside it `METHOD-ID. NEW OVERRIDE.` overrides BASE's New (§11.7.3 SR3 — §16.2
            // does not declare New FINAL) while `METHOD-ID. NEW.` is the SR4 a) redefinition (ResolveOverrides).
            foreach (var m in ctx.factoryParagraph()?.methodDefinition() ?? [])
            {
                using var atMethod = edition.At(m);
                var fsel = m.methodPropertySelector();
                string methodName = fsel is not null
                    ? (fsel.GET() is not null
                        ? NamingConvention.GetAccessorName(fsel.propertyName().GetText())
                        : NamingConvention.SetAccessorName(fsel.propertyName().GetText()))
                    : m.methodName(0).GetText();
                var pd = m.procedureDivision();
                string fcs = fsel is not null ? methodName : DataItem.WordIdentifier(methodName);
                // No CS0542 guard: the enclosing type is the factory class, a NamingConvention prefix family no
                // method-name or accessor name spells (kb/Work PB2921).
                var method = new OoMethodSymbol(
                    methodName,
                    HasUsing: pd?.usingClause() is not null,
                    HasReturning: pd?.returningClause() is not null,
                    m)
                { CsName = fcs, Owner = sym, IsFactory = true,
                  HasOverride = m.OVERRIDE() is not null, IsFinal = m.FINAL() is not null,
                  Accessor = fsel is null ? '\0' : fsel.GET() is not null ? 'G' : 'S',
                  PropertyName = fsel?.propertyName().GetText(),
                  ExternalizedName = Externalized(m.externalizedNamePhrase(), methodName,
                      "METHOD-ID", "ISO §11.7.3 SR1") };
                if (!sym.TryAddFactoryMethod(method))
                    edition.Declined(DiagnosticCatalog.ParametricPolymorphismDeclined,
                        $"class '{name}': a second factory method named '{methodName}' (OO deep-dive D9)");
            }
        }

        // Resolve base links AFTER all classes are registered (a base may be defined later in the file).
        foreach (var sym in table._classes)
        {
            if (sym.BaseName is not { } baseName) continue;
            using var atBase = edition.At(sym.Ctx.classIdParagraph().className(1));
            // §11.3.3 SR2: "Object-class-name-2 shall be the name of a class specified in the REPOSITORY
            // paragraph of this source element" — an INSTANCE of §8.4.6.4, so it resolves through the ONE
            // funnel and NOT through this table's group-wide Find (kb/Work PB365).
            if (OoNameResolution.Resolve(table, edition, sym.Ctx, baseName, OoNameResolution.Want.Class,
                    $"class '{sym.Name}': INHERITS FROM", DiagnosticCatalog.InheritedClassNotInScope.Code,
                    "ISO §11.3.3 SR2").Class
                is { } baseSym)
            {
                sym.Base = baseSym;
                if (baseSym.IsFinal)
                    edition.Error(DiagnosticCatalog.InheritsClauseRule,
                        $"class '{sym.Name}': INHERITS FROM '{baseName}', which is declared FINAL; object-class-name-2 "
                        + "shall not be the name of a class defined with the FINAL clause (ISO §11.3.3 SR5 / §11.3.4 GR3)");
            }
        }

        // §11.3.3 SR3 / SR4 over the RESOLVED links (an AS-literal alias of the class's own name is still itself), and
        // a cycle would emit circular C# base declarations — so the link is CUT and downstream chain walks stay finite.
        // Every class ON a cycle breaks SR4 (each one's object-class-name-2 inherits from it), and one that merely
        // inherits INTO a cycle breaks nothing: so the cycle members are found first, over the uncut links, and only
        // then reported and cut — cutting while walking left the later members of a cycle unreported and reported the
        // class that led into it.
        var onCycle = new List<(OoClassSymbol Sym, List<string> Chain)>();
        foreach (var sym in table._classes)
        {
            var seen = new HashSet<OoClassSymbol> { sym };
            var chain = new List<string>();
            for (OoClassSymbol? b = sym.Base; b is not null; b = b.Base)
            {
                chain.Add(b.Name);
                if (ReferenceEquals(b, sym)) { onCycle.Add((sym, chain)); break; }
                if (!seen.Add(b)) break;   // a cycle that does not pass through sym: its own members report it
            }
        }
        foreach (var (sym, chain) in onCycle)
        {
            using var atClass = edition.At(sym.Ctx.classIdParagraph().className(1));   // the INHERITS name (a linked class has one)
            edition.Error(DiagnosticCatalog.InheritsClauseRule, chain.Count == 1
                ? $"class '{sym.Name}': INHERITS FROM '{sym.BaseName}' names the class itself; object-class-name-2 "
                  + "shall not be the name of the class declared by this class definition (ISO §11.3.3 SR3)"
                : $"class '{sym.Name}': INHERITS FROM '{sym.BaseName}', which inherits from '{sym.Name}' (the chain "
                  + $"{string.Join(" -> ", chain)}); object-class-name-2 shall not inherit from object-class-name-1 "
                  + "directly or indirectly (ISO §11.3.3 SR4)");
        }
        foreach (var (sym, _) in onCycle) sym.Base = null;

        // The PARAMETERIZED class definitions (kb/Work PB1505). OoExpansion keeps each out of the table — it is a
        // skeleton, and its expansions are the classes — but it is a class definition all the same, and §11.3.3's rules
        // on object-class-name-2 are syntax rules of its CLASS-ID paragraph. SR7 and the declined multiple inheritance
        // are the shared ScreenInheritsNames; SR3 compares the written name (the skeleton's own name resolves to no
        // class); SR2 and SR5 resolve the base through the one §8.4.6.4 funnel. A base written as one of the
        // definition's own parameter-names (§11.3.4 GR6 permits it wherever an object-class-name is) names the actual
        // of each expansion, where the expansion's own CLASS-ID answers these rules.
        foreach (var skeleton in parameterizedClasses ?? [])
        {
            var id = skeleton.classIdParagraph();
            string name = id.className(0).GetText();
            // The definition binds as itself (PB2051) only when nothing expands it; an expanded one is asked through
            // its expansions, which re-bind every line of it (one fact, one diagnostic: OoExpansion.Result.ExpandedNames).
            OoClassSymbol? sym = null;
            if (expandedNames?.Contains(name) != true)
                using (edition.At(id.className(0)))
                {
                    sym = NewClassSymbol(skeleton);
                    AddMethods(sym);
                    table._parameterizedClasses.Add(sym);
                }
            ScreenInheritsNames(id, name, edition);
            if (id.className().Length < 2) continue;
            var baseCtx = id.className(1);
            string baseName = baseCtx.GetText();
            if (id.ooParameterName().Any(p => CobolNames.Same(p.GetText(), baseName)))
            {
                // The base is a formal: the method search SUPER starts (§8.4.3.8.4 GR3) and SELF continues through is
                // the actual's, known only in an expansion (kb/Work PB2937).
                if (sym is not null) sym.FormalBaseName = baseName;
                continue;
            }
            using var atBase = edition.At(baseCtx);
            if (CobolNames.Same(baseName, name))
            {
                edition.Error(DiagnosticCatalog.InheritsClauseRule,
                    $"parameterized class '{name}': INHERITS FROM '{baseName}' names the class itself; object-class-name-2 "
                    + "shall not be the name of the class declared by this class definition (ISO §11.3.3 SR3)");
                continue;
            }
            var resolvedBase = OoNameResolution.Resolve(table, edition, skeleton, baseName, OoNameResolution.Want.Class,
                $"parameterized class '{name}': INHERITS FROM", DiagnosticCatalog.InheritedClassNotInScope.Code,
                "ISO §11.3.3 SR2").Class;
            // A written class is the skeleton's superclass exactly as it is an ordinary class's (§9.3.9): the search
            // SUPER starts and the methods SELF inherits are its, whether or not anything expands the definition (kb/Work
            // PB2937 — the skeleton's bodies bind as themselves). A FINAL one is refused and not inherited, as below.
            if (sym is not null && resolvedBase is { IsFinal: false }) sym.Base = resolvedBase;
            if (resolvedBase is { IsFinal: true })
                edition.Error(DiagnosticCatalog.InheritsClauseRule,
                    $"parameterized class '{name}': INHERITS FROM '{baseName}', which is declared FINAL; object-class-name-2 "
                    + "shall not be the name of a class defined with the FINAL clause (ISO §11.3.3 SR5 / §11.3.4 GR3)");
        }

        // OVERRIDE / FINAL resolution is NOT done here: a roster is complete only once its class's data has bound,
        // because the accessors a PROPERTY clause defines (§13.18.42.4 GR1/GR2) are a fact of the BOUND data — see
        // ResolveOverrides, which BinderDriver runs after every class's data binds (kb/Work PB1274).

        // IMPLEMENTS capture (§11.8.2 — the OBJECT/FACTORY paragraph headers). §11.8.3 SR1 (OBJECT) and
        // §11.4.3 SR1 (FACTORY) are the SAME sentence — "Interface-name-1 shall be the name of an interface
        // specified in the REPOSITORY paragraph of the containing class definition" — and both now resolve
        // through the §8.4.6.4 funnel. Until kb/Work PB365 this comment said the REPOSITORY requirement was
        // "staged as a documented follow-up"; a staged rule is an accepted illegal program, so it is enforced. A
        // PARAMETERIZED class's IMPLEMENTS clauses are asked too (kb/Work PB2051); a parameter-name there resolves as a
        // formal, and conformance to whatever it names is each expansion's (§9.3.8.2.4).
        foreach (var sym in table._classes.Concat(table._parameterizedClasses))
        {
            CaptureImplements(sym.Ctx.objectParagraph()?.implementsClause(), sym.Implements,
                "OBJECT", "ISO §11.8.3 SR1");
            CaptureImplements(sym.Ctx.factoryParagraph()?.implementsClause(), sym.FactoryImplements,
                "FACTORY", "ISO §11.4.3 SR1");

            void CaptureImplements(Core.ImplementsClauseContext? impl, List<OoInterfaceSymbol> into,
                string where, string citation)
            {
                foreach (var iref in impl?.interfaceName() ?? [])
                {
                    using var atImplements = edition.At(iref);
                    // The site is the CLASS definition (§11.8.3 SR1 says "of the containing class
                    // definition"), which is also where the OBJECT/FACTORY paragraph's own scope comes from:
                    // §12.3.3 SR3 forbids a REPOSITORY inside either paragraph.
                    if (OoNameResolution.Resolve(table, edition, sym.Ctx, iref.GetText(),
                            OoNameResolution.Want.Interface, $"class '{sym.Name}' ({where}): IMPLEMENTS",
                            DiagnosticCatalog.InterfaceNameNotInScope.Code, citation).Interface is { } isym)
                    {
                        // A REPEATED interface-name is legal and changes nothing (kb/Work PB946): §11.8.2/§11.4.2
                        // print `IMPLEMENTS { interface-name-1 } …`, §11.8.3/§11.4.3 have exactly two syntax rules
                        // (REPOSITORY membership; conformance) and neither forbids a repeat — where the standard
                        // DOES forbid one it says so, §11.3.3 SR7 and §11.6.3 SR6 for the two INHERITS clauses. The
                        // implemented set is what §11.8.4 GR2 consumes, so the second occurrence is absorbed.
                        if (!into.Contains(isym))
                            into.Add(isym);
                    }
                }
            }
        }
        return table;
    }

    /// <summary>The rules on the WRITTEN object-class-name-2 list of a CLASS-ID's INHERITS clause, asked of every class
    /// definition (a parameterized one included): ISO §11.3.3 SR7 — "A given class name shall not appear more than once
    /// in an INHERITS clause" (kb/Work PB1020) — first, then the declined multiple inheritance (Annex A.4.10 item 1,
    /// <c>MultipleInheritanceDeclined</c>) over the DISTINCT names, so <c>INHERITS FROM B B</c> reports the rule it breaks and not the
    /// restriction. The names compare as written words (§8.3.2.2 — case-insensitively), the reading the interface twin
    /// (§11.6.3 SR6, kb/Work PB1502) takes.</summary>
    private static void ScreenInheritsNames(Core.ClassIdParagraphContext id, string name, EditionContext edition)
    {
        var distinct = new List<string>();
        foreach (var written in id.className().Skip(1))
        {
            string w = written.GetText();
            if (distinct.Any(d => CobolNames.Same(d, w)))
            {
                using var _ = edition.At(written);
                edition.Error(DiagnosticCatalog.InheritsClauseRule,
                    $"class '{name}': INHERITS FROM names '{w}' more than once; a given class name shall not appear more "
                    + "than once in an INHERITS clause (ISO §11.3.3 SR7)");
                continue;
            }
            distinct.Add(w);
        }
        if (distinct.Count > 1)
            // §11.3.2 permits several INHERITS bases; WiseOwl COBOL restricts to SINGLE inheritance and refuses the rest
            // by name through the ONE declined-element seam (SSOT §18 #18; Annex A.4.10 item 1 — multiple inheritance,
            // not claimed). Silently compiling against only the first base was the R9 silent-miscompile.
            using (edition.At(id.className(2)))
                edition.Declined(DiagnosticCatalog.MultipleInheritanceDeclined,
                    $"class '{name}': INHERITS FROM {distinct.Count} base classes ({string.Join(", ", distinct)})");
    }

    /// <summary>
    /// OVERRIDE marking and the method-attribute rules, over rosters that are COMPLETE — run by <c>BinderDriver</c>
    /// once every class's data has bound (and so once every PROPERTY clause has defined its accessors), before any
    /// signature is compared. A PROPERTY clause "causes a method to be defined for the containing object" (ISO
    /// §13.18.42.4 GR1/GR2), and §8.4.3.9.1 calls it "a method implicitly generated for a data item described with the
    /// PROPERTY clause" — so its accessors are superclass methods like any other: an explicit `GET PROPERTY N OVERRIDE`
    /// overrides one, and a FINAL one (§13.18.42.4 GR3) refuses it. Which entries carry the clause is a fact of the
    /// BOUND data, not of the entry's own text (a SAME AS entry is "as though the data description identified by
    /// data-name-1 had been coded in place", §13.18.49.4 GR1), so the roster is complete only after data binding.
    /// Until kb/Work PB1274 this ran inside <see cref="Build"/>, before any clause accessor existed: the OVERRIDE of
    /// one was refused (0838), a FINAL one could be redefined, and §13.18.42.3 SR4 depended on SOURCE ORDER.
    /// <para>The rules, both rosters (instance + factory) alike — per-interface, never cross-roster (D11): an EXPLICIT
    /// OVERRIDE marks the override (0839 when the overridden method is FINAL — §11.7.3 SR3 "The method in the
    /// superclass shall not be defined with the FINAL clause"); a name match WITHOUT the attribute is the §11.7.3
    /// SR4 a) 0837 via EditionContext.Removed (error strict; warning + the pre-wave inference under --permissive — the
    /// documented migration leniency), and the override is STILL marked so 0829 signature messages stay coherent;
    /// OVERRIDE with NO matching base method is the SR3 0838. The override adopts the base slot's CsName (C# requires
    /// the exact member name; the class-name collision corner is COBOLNET4213, an implementation restriction). A PROPERTY clause's own accessors carry
    /// no OVERRIDE (its implicit METHOD-ID has none), so a clause accessor that meets a superclass method of its name
    /// is §13.18.42.3 SR4 — asked here too, by property-name, since SR4 bars the name whichever accessors either side
    /// defines.</para>
    /// </summary>
    public void ResolveOverrides(EditionContext edition)
    {
        foreach (var sym in _classes)
        {
            MarkRoster(sym, sym.Methods, sym.Base is null ? null : (n => sym.Base!.FindMethod(n)), "", edition);
            MarkRoster(sym, sym.FactoryMethods, sym.Base is null ? null : (n => sym.Base!.FindFactoryMethod(n)),
                "factory ", edition);
            ScreenInheritedPropertyNames(sym, factory: false, edition);
            ScreenInheritedPropertyNames(sym, factory: true, edition);
        }
    }

    /// <summary>ISO §13.18.42.3 SR4: "The data-name for the subject of the entry shall not be the same as a
    /// property-name defined in a superclass" — and its NOTE names both ways a superclass defines one (GET/SET PROPERTY
    /// methods, or a PROPERTY clause), which is exactly what a roster's accessors record. Asked of each PROPERTY-clause
    /// subject once, over the base chain's matching roster (instance or factory).</summary>
    private static void ScreenInheritedPropertyNames(OoClassSymbol sym, bool factory, EditionContext edition)
    {
        var subjects = new HashSet<DataItem>();
        foreach (var accessor in factory ? sym.FactoryMethods : sym.Methods)
        {
            if (accessor.PropertySubject is not { } subject || !subjects.Add(subject)) continue;
            for (var b = sym.Base; b is not null; b = b.Base)
                if ((factory ? b.FactoryMethods : b.Methods).Any(bm => CobolNames.Same(bm.PropertyName, accessor.PropertyName)))
                {
                    using var _ = edition.At(subject);
                    edition.Error(DiagnosticCatalog.PropertyClauseRule,
                        $"class '{sym.Name}'{(factory ? " (FACTORY)" : "")}: property subject '{accessor.PropertyName}': "
                        + $"superclass '{b.Name}' already defines a property of that name; the data-name for the subject "
                        + "of the entry shall not be the same as a property-name defined in a superclass "
                        + "(ISO §13.18.42.3 SR4)");
                    break;
                }
        }
    }

    private static void MarkRoster(OoClassSymbol sym, IReadOnlyList<OoMethodSymbol> roster,
        Func<string, OoMethodSymbol?>? findInBase, string kind, EditionContext edition)
    {
        foreach (var m in roster)
        {
            // A PROPERTY clause's accessor (no METHOD-ID of its own) is never an override: §13.18.42.3 SR4 above
            // refuses the only way one can meet a superclass method of its name.
            if (m.PropertySubject is not null) continue;
            using var atMethod = edition.At(m.Ctx);
            var baseM = findInBase?.Invoke(m.ExternalizedName);   // the roster key (PB303)
            if (baseM is null)
            {
                if (m.HasOverride)
                    edition.Error(DiagnosticCatalog.OverrideWithoutInheritedMethod,
                        $"class '{sym.Name}': {kind}method '{m.SourceName}' specifies OVERRIDE but no "
                        + "superclass defines a method with that name"
                        + (sym.Base is null ? " (the class has no INHERITS clause)" : "")
                        + " (ISO §11.7.3 SR3)");
                continue;
            }
            if (!m.HasOverride)
                edition.Removed(DiagnosticCatalog.RedefinitionWithoutOverride.Code,
                    $"class '{sym.Name}': {kind}method '{m.SourceName}' redefines a method inherited from "
                    + $"'{baseM.Owner.Name}' without the OVERRIDE attribute (ISO §11.7.3 SR4a — an "
                    + "inherited method may be redefined only with OVERRIDE; add OVERRIDE to the "
                    + "METHOD-ID paragraph)");
            if (baseM.IsFinal)
                edition.Error(DiagnosticCatalog.FinalMethodOverridden,
                    $"class '{sym.Name}': {kind}method '{m.SourceName}' overrides '{baseM.Owner.Name}'."
                    + $"'{baseM.SourceName}', which is declared FINAL — a FINAL method shall not be "
                    + "overridden (ISO §11.7.3 SR3 / §11.7.4 GR3)");
            m.OverrideOf = baseM;
            m.CsName = baseM.CsName;
            if (m.CsName == sym.CsName)
                edition.Error(DiagnosticCatalog.OverrideNamedLikeItsClass,
                    $"class '{sym.Name}': the method '{m.SourceName}' overrides the inherited method of the class's own "
                    + "name, whose emitted member name the generated class cannot declare (implementation restriction — "
                    + "rename the class or the method)");
        }
    }
}
