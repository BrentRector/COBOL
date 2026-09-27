// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding;
using CobolNet.Binding.Model;

namespace CobolNet.Compiler.Oo;

using Core = CobolParserCore;

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
    private readonly Dictionary<string, OoClassSymbol> _byName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>All classes in source order (the emitter's deterministic emission order).</summary>
    public IReadOnlyList<OoClassSymbol> Classes => _classes;
    private readonly List<OoClassSymbol> _classes = [];

    /// <summary>All interfaces in source order (§11.6 — emitted as C# interfaces BEFORE the classes, purely
    /// for readability; Roslyn needs no ordering). Interfaces and classes share ONE name namespace
    /// (<see cref="_ifaceByName"/> is checked against <see cref="_byName"/> at Build — a collision is 0840).</summary>
    public IReadOnlyList<OoInterfaceSymbol> Interfaces => _interfaces;
    private readonly List<OoInterfaceSymbol> _interfaces = [];
    private readonly Dictionary<string, OoInterfaceSymbol> _ifaceByName = new(StringComparer.OrdinalIgnoreCase);

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
        : string.Equals(name, OoStandardClasses.BaseName, StringComparison.OrdinalIgnoreCase) ? StandardBase
        : null;

    /// <summary>The class whose EXTERNALIZED name (its CLASS-ID's <c>AS literal-1</c>, or its name) is
    /// <paramref name="externalized"/> — what a REPOSITORY class-specifier's <c>AS literal-1</c> names (ISO §12.3.8.4
    /// GR2; kb/Work PB974). Compared as the group's other externalized names are (case-insensitive). The standard
    /// class BASE is externalized as its own name, and answers only when no definition of the group claims it —
    /// the same precedence as <see cref="Find"/>.</summary>
    public OoClassSymbol? FindByExternalizedName(string externalized) =>
        _classes.FirstOrDefault(c => string.Equals(c.ExternalizedName, externalized, StringComparison.OrdinalIgnoreCase))
        ?? (string.Equals(externalized, StandardBase.ExternalizedName, StringComparison.OrdinalIgnoreCase)
            ? StandardBase : null);

    /// <summary>This compilation group's symbol for the standard class BASE (ISO §16; <see cref="OoStandardClasses"/>).
    /// Never in <see cref="Classes"/>: it has no source, binds nothing and emits nothing.</summary>
    public OoClassSymbol StandardBase { get; } = OoStandardClasses.BuildBase();

    /// <summary>The interface twin of <see cref="FindByExternalizedName"/> (literal-2).</summary>
    public OoInterfaceSymbol? FindInterfaceByExternalizedName(string externalized) =>
        _interfaces.FirstOrDefault(i => string.Equals(i.ExternalizedName, externalized, StringComparison.OrdinalIgnoreCase));

    /// <summary>True when <paramref name="name"/> names a PARAMETERIZED class or interface definition of the group
    /// (kb/Work PB759). Such a definition is a skeleton (§9.3.12 / §9.3.13) and is deliberately NOT in this table
    /// — <see cref="OoExpansion"/> puts its EXPANSIONS here instead — so a lookup of its name misses; this is what
    /// lets <see cref="OoNameResolution.Resolve"/> say WHY (ISO §12.3.8.4 GR1: "If object-class-name-1 is a class
    /// described with the USING phrase, object-class-name-1 may be specified only in the REPOSITORY
    /// paragraph").</summary>
    public bool IsParameterized(string name) => _parameterized.Contains(name);
    private IReadOnlySet<string> _parameterized = EmptyNames;
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
        if (!_scopes.TryGetValue(key, out var scope)) _scopes[key] = scope = OoRepositoryScope.Build(key);
        return scope;
    }

    // Keyed by parse-tree node IDENTITY: a ParserRuleContext does not override Equals, so the default
    // comparer already IS reference equality.
    private readonly Dictionary<Antlr4.Runtime.RuleContext, OoRepositoryScope> _scopes = [];

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

    /// <summary>
    /// Build the table from the group's class definitions (pass-1: identity + roster only — no data or statement
    /// binding). Structural diagnostics raised here, each per its ISO rule: duplicate class name / emitted-type
    /// collision (COBOLNET0820), END CLASS name mismatch (§10.7 — 0820), unknown INHERITS base (§11.3.2 —
    /// COBOLNET0821, never silently a root class), duplicate method name within a class (COBOLNET0822 — the
    /// unique-name restriction, deep-dive D9: parametric polymorphism, ISO §9.3.5.3, is the OPTIONAL Annex
    /// A.4.10 item 3 whose support this implementation does not claim).
    /// INHERITS emission itself is a later port slice (3a) — a KNOWN base still 0899s until it lands, but the
    /// base link is recorded so method lookup walks the chain from day one.
    /// <para>⚠ The three §12063 citations that stood here and at the two COBOLNET0822 emit sites were NOT
    /// clause numbers — ISO/IEC 1989:2023 has no §12063; the number was a stray line anchor, recorded as
    /// finding 44 of <c>docs/rearchitecture/DESIGN-SPEC-RECONCILIATION.md</c>. Re-derived and
    /// <c>cite.py --check</c>ed: §9.3.5.3 is "Parametric polymorphism", its rule 7 is "Parametric polymorphism
    /// is an optional feature in this Working Draft International Standard", and Annex A.4.10 item 3 is
    /// "Parametric polymorphism (9.3.5.3)". The CLAUDE.md rule-1 inherited-citation failure, caught at the
    /// point the rows it covers were being witnessed.</para>
    /// </summary>
    public static OoClassTable Build(IReadOnlyList<Core.ClassDefinitionContext> classes, EditionContext edition,
        IReadOnlyList<Core.InterfaceDefinitionContext>? interfaces = null,
        IReadOnlySet<string>? parameterizedNames = null)
    {
        var table = new OoClassTable { _parameterized = parameterizedNames ?? EmptyNames };
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
            return ExternalizedName.Screen(phrase.literal(), edition,
                       DiagnosticCatalog.ExternalizedNameLiteral,
                       $"{kind} '{declared}' AS {phrase.literal().GetText()}", "literal-1", rule,
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
            string icsName = DataItem.Sanitize(iname).ToUpperInvariant();
            var isym = new OoInterfaceSymbol(iname, icsName, ictx)
            {
                ExternalizedName = Externalized(ictx.externalizedNamePhrase(), iname,
                    "INTERFACE-ID", "ISO §11.6.3 SR1"),
            };
            if (table._ifaceByName.ContainsKey(iname) || table._byName.ContainsKey(iname)
                || !usedCsNames.Add(icsName))
            {
                edition.Error("COBOLNET0840",
                    $"duplicate interface definition '{iname}' — interface and class names share one "
                    + "namespace and shall be unique in the compilation group (ISO §8.3.2.2/§11.6)");
                continue;
            }
            table._ifaceByName.Add(iname, isym);
            table._interfaces.Add(isym);
            if (!string.Equals(ictx.interfaceName(ictx.interfaceName().Length - 1).GetText(), iname,
                    StringComparison.OrdinalIgnoreCase))
                using (edition.At(ictx.interfaceName(ictx.interfaceName().Length - 1)))
                    edition.Error("COBOLNET0840",
                        $"END INTERFACE does not match INTERFACE-ID '{iname}' (ISO §10.7)");

            // PROTOTYPES (§10.6.2 SR4): a header + optional LINKAGE-only data division, NO procedure body,
            // NO OVERRIDE/FINAL attributes (§11.7 SR2/SR8 — the OVERRIDE/FINAL wave's forward obligation).
            foreach (var m in ictx.methodDefinition())
            {
                string pname = (m.methodName().Length > 0 ? m.methodName(0)?.GetText() : null)
                    ?? m.methodPropertySelector()?.propertyName()?.GetText() ?? "?";
                using var atPrototype = edition.At(m);
                var pd = m.procedureDivision();
                if (m.OVERRIDE() is not null || m.FINAL() is not null)
                    edition.Error("COBOLNET0840",
                        $"interface '{iname}', method '{pname}': OVERRIDE/FINAL may not appear in a method "
                        + "PROTOTYPE (ISO §11.7.3 SR2/SR8)");
                // §10.6.2 SR4 a)-f) — the ONE prototype-body screen every prototype kind shares (kb/Work PB894).
                // This arm used to carry its own two of the six restrictions, with the data division checked
                // against three of its five non-linkage sections.
                PrototypeUnitRules.ScreenBody($"interface '{iname}', method '{pname}'", m.optionsParagraph(),
                    m.environmentDivision(), m.dataDivision(), pd, edition);
                if (m.methodPropertySelector() is not null)
                {
                    edition.Error(DiagnosticCatalog.OoInterfacePropertyPrototype,
                        $"interface '{iname}': a GET/SET PROPERTY prototype is recognized but not yet "
                        + "implemented (the property-prototype leg — a later refinement)");
                    continue;
                }
                var proto = new OoMethodSymbol(
                    pname,
                    HasUsing: pd?.usingClause() is not null,
                    HasReturning: pd?.returningClause() is not null,
                    m)
                {
                    CsName = DataItem.Sanitize(pname).ToUpperInvariant(),
                    ExternalizedName = Externalized(m.externalizedNamePhrase(), pname,
                        "METHOD-ID", "ISO §11.7.3 SR1"),
                };
                if (!isym.TryAddPrototype(proto))
                    edition.Error("COBOLNET0840",
                        $"interface '{iname}': duplicate method prototype '{pname}' (v1 unique-name rule, D9)");
            }
        }
        // Interface INHERITS resolution + cycle check (§11.6.3 SR2/SR3/SR6).
        foreach (var isym in table._interfaces)
            foreach (var inhCtx in isym.Ctx.interfaceName().Skip(1).Take(isym.Ctx.interfaceName().Length - 2))
            {
                string inh = inhCtx.GetText();
                using var atInherits = edition.At(inhCtx);
                // §11.6.3 SR2: "Interface-name-2 shall be the name of an interface specified in the REPOSITORY
                // paragraph of this source element" — the class-INHERITS rule's twin, same funnel (PB365).
                if (OoNameResolution.Resolve(table, edition, isym.Ctx, inh, OoNameResolution.Want.Interface,
                        $"interface '{isym.Name}': INHERITS FROM", "COBOLNET0840",
                        "ISO §11.6.3 SR2").Interface is { } b)
                {
                    if (isym.Inherits.Contains(b))
                        edition.Error("COBOLNET0840",
                            $"interface '{isym.Name}': duplicate INHERITS FROM '{inh}' (ISO §11.6.3 SR6)");
                    else
                        isym.Inherits.Add(b);
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
                        edition.Error("COBOLNET0840",
                            $"interface '{isym.Name}': the INHERITS graph is cyclic (ISO §11.6.3 SR3)");
                        stack.Clear();
                        break;
                    }
                    if (seenI.Add(b)) stack.Push(b);
                }
            }
        }

        foreach (var ctx in classes)
        {
            var id = ctx.classIdParagraph();
            string name = id.className(0).GetText();
            using var atClass = edition.At(id.className(0));
            // COBOL-2002 introduction gate: VersionConformancePass ParseArm.VisitClassDefinition (rearch 14g.3,
            // recognition — fires per parse node, so a duplicate/colliding definition dropped below still names its edition).
            string csName = DataItem.Sanitize(name).ToUpperInvariant();   // MUST match PicInfo.ClrType's mapping
            var bases = id.className().Skip(1).Select(c => c.GetText()).ToList();
            var sym = new OoClassSymbol(name, csName, ctx)
            {
                // §11.3.3 SR1 alone omits the zero-length exclusion (verified on the printed page).
                ExternalizedName = Externalized(id.externalizedNamePhrase(), name,
                    "CLASS-ID", "ISO §11.3.3 SR1", rejectZeroLength: false),
                Bases = bases,
                BaseName = bases.Count >= 1 ? bases[0] : null,
                IsFinal = id.FINAL() is not null,
            };
            if (bases.Count > 1)
                // ISO §11.3.2 permits several INHERITS bases; WiseOwl COBOL v1 restricts to SINGLE inheritance and
                // rejects the rest LOUDLY (SSOT §18 #18; A.4.10 — multiple inheritance / parametric polymorphism
                // rejected). Silently compiling against only the first base was the R9 silent-miscompile.
                using (edition.At(id.className(2)))
                    edition.Error("COBOLNET0849",
                        $"class '{name}': INHERITS FROM {bases.Count} base classes ({string.Join(", ", bases)}) — "
                        + "WiseOwl COBOL v1 supports single inheritance only; multiple inheritance is rejected "
                        + "(ISO §11.3.2; SSOT §18 #18 / A.4.10)");
            usedCsNames.Add(csName + NamingConvention.FactorySuffix);   // belt-and-braces (a `__` name cannot collide with COBOL-derived names)
            if (table._ifaceByName.ContainsKey(name))
                edition.Error("COBOLNET0840",
                    $"'{name}' is defined as both a class and an interface — one name namespace "
                    + "(ISO §8.3.2.2)");
            if (!table._byName.TryAdd(name, sym) || !usedCsNames.Add(csName))
            {
                edition.Error("COBOLNET0820",
                    $"duplicate class definition '{name}' — a class-name shall be unique within the compilation "
                    + "group (ISO §8.3.2.2/§11.3; class names compare case-insensitively and map to one emitted "
                    + "type)");
                continue;
            }
            table._classes.Add(sym);

            if (ctx.endClassHeader().className().GetText() is { } endName
                && !string.Equals(endName, name, StringComparison.OrdinalIgnoreCase))
                using (edition.At(ctx.endClassHeader()))
                    edition.Error("COBOLNET0820",
                        $"END CLASS '{endName}' does not match CLASS-ID '{name}' (ISO §10.7 — the end marker names "
                        + "its class)");

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
                string mcs = sel is not null ? methodName : DataItem.Sanitize(methodName).ToUpperInvariant();
                // CS0542 guard: a C# member may not be named like its enclosing type — a METHOD-ID named like
                // its CLASS-ID is legal COBOL, so the SYMBOL renames. Derivation (kb/Work PB975): §8.3.2.2 bars
                // one word as two types of user-defined word only "Within a source element"; §10.4 makes a method
                // a source unit "directly contained within" its factory/instance definition, and §10.5 "A source
                // element is a source unit excluding any contained source units" — so the method-name and the
                // class-name sit in DIFFERENT source elements, which §8.4.6.1 lets "use identical user-defined
                // words ... independent of the use of these user-defined words by other source elements".
                if (mcs == csName) mcs += "_M";
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
                // §11.7.3 SR6/SR7 — the accessor SHAPES: GET = no USING + exactly one RETURNING; SET = exactly
                // one USING + no RETURNING (checked here on header presence; formal counts re-checked at
                // data-bind when they resolve).
                if (method.Accessor == 'G' && (method.HasUsing || !method.HasReturning))
                    edition.Error("COBOLNET0842",
                        $"class '{name}': METHOD-ID GET PROPERTY {method.PropertyName} shall have no USING "
                        + "and exactly one RETURNING (ISO §11.7.3 SR6)");
                if (method.Accessor == 'S' && (!method.HasUsing || method.HasReturning))
                    edition.Error("COBOLNET0842",
                        $"class '{name}': METHOD-ID SET PROPERTY {method.PropertyName} shall have exactly "
                        + "one USING and no RETURNING (ISO §11.7.3 SR7)");
                if (!sym.TryAddMethod(method))
                    edition.Error("COBOLNET0822",
                        $"class '{name}': duplicate method name '{methodName}' — method names shall be unique "
                        + "within a class in this implementation (OO deep-dive D9). Overloading by method "
                        + "resolution signature is PARAMETRIC POLYMORPHISM (ISO §9.3.5.3), an OPTIONAL element "
                        + "(Annex A.4.10 item 3; §9.3.5.3 rule 7) whose support WiseOwl COBOL does not claim");
                if (sel is null && m.methodName().Length > 1
                    && !string.Equals(m.methodName(1).GetText(), methodName, StringComparison.OrdinalIgnoreCase))
                    using (edition.At(m.methodName(1)))
                        edition.Error("COBOLNET0820",
                            $"class '{name}': END METHOD '{m.methodName(1).GetText()}' does not match METHOD-ID "
                            + $"'{methodName}' (ISO §10.7)");
            }

            // FACTORY methods (§11.4) — a SEPARATE roster/interface (§9.3.6: an instance method and a
            // factory method may share a name). A factory METHOD-ID named NEW is COBOLNET0836: the
            // predefined New (§16.2.1) is the generated ctor (D4) and overriding it is a v1 restriction.
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
                if (string.Equals(methodName, "NEW", StringComparison.OrdinalIgnoreCase))
                {
                    edition.Error("COBOLNET0836",
                        $"class '{name}': a factory method may not be named 'NEW' — the predefined New "
                        + "(ISO §16.2.1) is realized by the generated constructor (deep-dive D4); overriding "
                        + "New is a deferred v1 restriction");
                    continue;
                }
                string fcs = fsel is not null ? methodName : DataItem.Sanitize(methodName).ToUpperInvariant();
                if (fcs == csName + "__FACTORY") fcs += "_M";   // unreachable (no __ in COBOL names) — defensive
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
                    edition.Error("COBOLNET0822",
                        $"class '{name}': duplicate factory method name '{methodName}' — method names shall "
                        + "be unique within the factory definition in this implementation (OO deep-dive D9). "
                        + "Overloading by method resolution signature is PARAMETRIC POLYMORPHISM (ISO "
                        + "§9.3.5.3), an OPTIONAL element (Annex A.4.10 item 3) whose support WiseOwl COBOL does "
                        + "not claim — the factory arm carried NO citation at all before this");
                if (fsel is null && m.methodName().Length > 1
                    && !string.Equals(m.methodName(1).GetText(), methodName, StringComparison.OrdinalIgnoreCase))
                    using (edition.At(m.methodName(1)))
                        edition.Error("COBOLNET0820",
                            $"class '{name}': END METHOD '{m.methodName(1).GetText()}' does not match METHOD-ID "
                            + $"'{methodName}' (ISO §10.7)");
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
                    $"class '{sym.Name}': INHERITS FROM", "COBOLNET0821", "ISO §11.3.3 SR2").Class
                is { } baseSym)
            {
                sym.Base = baseSym;
                if (baseSym.IsFinal)
                    edition.Error("COBOLNET0839",
                        $"class '{sym.Name}': INHERITS FROM '{baseName}', which is declared FINAL — a FINAL "
                        + "class shall not be a superclass (ISO §11.3.3 SR5 / §11.3.4 GR3)");
            }
        }

        // An inheritance CYCLE would emit circular C# base declarations — a Roslyn CS error on user source
        // (the loud-failure violation). Reject at pass-1 and CUT the link so downstream chain walks stay finite.
        foreach (var sym in table._classes)
        {
            using var atClass = edition.At(sym.Ctx.classIdParagraph().className(0));
            var seen = new HashSet<OoClassSymbol> { sym };
            for (OoClassSymbol? b = sym.Base; b is not null; b = b.Base)
                if (!seen.Add(b))
                {
                    edition.Error("COBOLNET0820",
                        $"class '{sym.Name}': the INHERITS chain is cyclic through '{b.Name}' (ISO §11.3.2 — "
                        + "a class shall not inherit from itself directly or indirectly)");
                    sym.Base = null;
                    break;
                }
        }

        // OVERRIDE marking + the §11.7 SR3/SR4a/GR3 attribute rules (the OVERRIDE/FINAL wave, DEVLOG 605 —
        // the former by-name-inference leniency is RETIRED as the default): an EXPLICIT OVERRIDE marks the
        // override (0839 when the overridden method is FINAL — GR3); a name match WITHOUT the attribute is
        // the SR4a 0837 via EditionContext.Removed (error strict; warning + the pre-wave inference under
        // --permissive — the documented migration leniency), and the override is STILL marked so 0829
        // signature messages stay coherent; OVERRIDE with NO matching base method is the SR3 0838. Both
        // rosters (instance + factory) take the identical rules — per-interface, never cross-roster (D11).
        // The uppercase CsName convention still neutralizes trap #2; the override adopts the base slot's
        // CsName (C# requires the exact member name; the class-name collision corner stays 0820).
        foreach (var sym in table._classes)
        {
            MarkRoster(sym, sym.Methods, sym.Base is null ? null : (n => sym.Base!.FindMethod(n)), "");
            MarkRoster(sym, sym.FactoryMethods, sym.Base is null ? null : (n => sym.Base!.FindFactoryMethod(n)), "factory ");
        }

        // IMPLEMENTS capture (§11.8.2 — the OBJECT/FACTORY paragraph headers). §11.8.3 SR1 (OBJECT) and
        // §11.4.3 SR1 (FACTORY) are the SAME sentence — "Interface-name-1 shall be the name of an interface
        // specified in the REPOSITORY paragraph of the containing class definition" — and both now resolve
        // through the §8.4.6.4 funnel. Until kb/Work PB365 this comment said the REPOSITORY requirement was
        // "staged as a documented follow-up"; a staged rule is an accepted illegal program, so it is enforced.
        foreach (var sym in table._classes)
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
                            "COBOLNET0840", citation).Interface is { } isym)
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

        void MarkRoster(OoClassSymbol sym, IReadOnlyList<OoMethodSymbol> roster,
            Func<string, OoMethodSymbol?>? findInBase, string kind)
        {
            foreach (var m in roster)
            {
                using var atMethod = edition.At(m.Ctx);
                var baseM = findInBase?.Invoke(m.ExternalizedName);   // the roster key (PB303)
                if (baseM is null)
                {
                    if (m.HasOverride)
                        edition.Error("COBOLNET0838",
                            $"class '{sym.Name}': {kind}method '{m.Name}' specifies OVERRIDE but no "
                            + "superclass defines a method with that name"
                            + (sym.Base is null ? " (the class has no INHERITS clause)" : "")
                            + " (ISO §11.7.3 SR3)");
                    continue;
                }
                if (!m.HasOverride)
                    edition.Removed("COBOLNET0837",
                        $"class '{sym.Name}': {kind}method '{m.Name}' redefines a method inherited from "
                        + $"'{baseM.Owner.Name}' without the OVERRIDE attribute (ISO §11.7.3 SR4a — an "
                        + "inherited method may be redefined only with OVERRIDE; add OVERRIDE to the "
                        + "METHOD-ID paragraph)");
                if (baseM.IsFinal)
                    edition.Error("COBOLNET0839",
                        $"class '{sym.Name}': {kind}method '{m.Name}' overrides '{baseM.Owner.Name}'."
                        + $"'{baseM.Name}', which is declared FINAL — a FINAL method shall not be "
                        + "overridden (ISO §11.7.3 SR3 / §11.7.4 GR3)");
                m.OverrideOf = baseM;
                m.CsName = baseM.CsName;
                if (m.CsName == sym.CsName)
                    edition.Error("COBOLNET0820",
                        $"class '{sym.Name}': the inherited method '{m.Name}' collides with the class's "
                        + "own emitted type name (implementation restriction — rename the class or the "
                        + "method; §8.3.2.2 externalized-name mapping)");
            }
        }
    }
}
