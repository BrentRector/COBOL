// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;

namespace CobolNet.Compiler.Oo;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// ⛔ PARAMETERIZED CLASSES AND INTERFACES ARE EXPANDED, NOT GENERIC (kb/Work PB759; OO deep-dive D12).
///
/// <para><b>The rule.</b> ISO §9.3.12: "A parameterized class is a generic or skeleton class that has formal
/// parameters that will be replaced by one or more object-class-names or interface-names. When it is expanded by
/// substituting specific object-class-names or interface-names as actual parameters, a class is created that
/// functions as a non-parameterized class", and "An expansion of a parameterized class is treated in all respects
/// the same as if it were a class that is not a parameterized class". §12.3.8.4 GR5 says how: "The class
/// object-class-name-1 is created from the parameterized class object-class-name-2 by replacing each specification
/// of the formal parameter by the corresponding actual parameter" (GR8 is the interface twin; §9.3.13 its
/// concept).</para>
///
/// <para><b>Why a re-parse and not a C# generic.</b> The binder resolves method names, conformance (§9.3.8.2.4),
/// typed object-reference descriptions and INVOKE lookups STATICALLY, keyed on a class NAME; a formal parameter is
/// unconstrained, so none of those can be answered inside the definition, only inside an expansion. So each
/// expansion becomes an ORDINARY class definition: the parameterized definition's own tokens, re-parsed with each
/// formal replaced by its actual (GR5's own words) and the header renamed to the expansion's name. From
/// <see cref="OoClassTable.Build"/> onward nothing can tell an expansion from a written class — which is exactly
/// the "in all respects" of §9.3.12, and makes every later OO rule apply to expansions with no new code.</para>
///
/// <para><b>Why token substitution is the rule and not an approximation.</b> §11.3.4 GR6 (§11.6.4 GR4 for an
/// interface): "Parameter-name-1 may be specified within this class definition only where an object-class-name
/// or an interface-name is permitted." Every occurrence of a formal's word inside its definition therefore IS a
/// class-name or interface-name position, so replacing every user-defined-word occurrence is GR5's "each
/// specification of the formal parameter" — the substitution cannot hit a data-name, because a formal cannot be
/// spelled as one.</para>
///
/// <para><b>Identity.</b> §9.3.12: "two classes with the same externalized object-class-name that are created by
/// expanding the same parameterized class with the same actual parameters are the same class instance", and
/// expansions with different actuals "shall not have the same externalized object-class-name". The expansion
/// name is its identity: specifiers repeating one (name, definition, actuals) triple across source elements
/// create ONE class; a name reused with a different definition or actuals is COBOLNET2240.</para>
///
/// <para><b>The definition itself emits nothing.</b> It is a skeleton, and §12.3.8.4 GR1 ("If object-class-name-1
/// is a class described with the USING phrase, object-class-name-1 may be specified only in the REPOSITORY
/// paragraph") leaves it no other use. It is kept out of the class table and recorded as parameterized, so
/// <see cref="OoNameResolution.Resolve"/> can name GR1 when some other position writes it. It is still a class or
/// interface definition, so it BINDS as itself for its own rules — its data, environment and method headers, with each
/// formal resolving as a formal (kb/Work PB2051; <c>OoDriver.BindParameterized</c>).</para>
/// </summary>
internal static class OoExpansion
{
    /// <summary>The group's definitions after expansion: the written NON-parameterized definitions in source order,
    /// then one synthesized definition per distinct expansion (in first-specifier order), plus the names of the
    /// parameterized definitions (class and interface names share one namespace, §8.3.2.2) and the parameterized
    /// definitions themselves — kept out of the class table, but still class and interface definitions that every
    /// rule of their own text governs (kb/Work PB1505, PB2051): <c>OoClassTable.Build</c> screens their headers and the
    /// binder binds each as itself (<c>OoDriver.BindParameterized</c>), emitting nothing — and the names of the
    /// definitions that some expansion re-binds (<see cref="Result.ExpandedNames"/>): those are checked through their
    /// expansions, never also as themselves, because one fact is one diagnostic and a report naming the definition
    /// would sit beside each expansion's report of the same line (train 1053 review).</summary>
    public sealed record Result(
        IReadOnlyList<Core.ClassDefinitionContext> Classes,
        IReadOnlyList<Core.InterfaceDefinitionContext> Interfaces,
        IReadOnlySet<string> ParameterizedNames,
        IReadOnlyList<Core.ClassDefinitionContext> ParameterizedClasses,
        IReadOnlyList<Core.InterfaceDefinitionContext> ParameterizedInterfaces,
        IReadOnlySet<string> ExpandedNames);

    /// <summary>One parameterized definition: its context, its formals in USING order, and the kind each formal's
    /// REPOSITORY specifier declares it as (null when undeclared — already COBOLNET2239).</summary>
    private sealed record Skeleton(ParserRuleContext Ctx, string Name, bool IsInterface,
        IReadOnlyList<Core.OoParameterNameContext> Formals, IReadOnlyList<bool?> FormalIsInterface);

    /// <summary>One distinct expansion (§9.3.12 identity): the expansion name, its skeleton and the actuals.</summary>
    private sealed record Expansion(string Name, Skeleton Of, IReadOnlyList<string> Actuals);

    public static Result Expand(Core.CompilationUnitContext tree,
        IReadOnlyList<Core.ClassDefinitionContext> classes, IReadOnlyList<Core.InterfaceDefinitionContext> interfaces,
        EditionContext edition, CobolWordsMap words)
    {
        var skeletons = new Dictionary<string, Skeleton>(CobolNames.Comparer);
        var plainClasses = new List<Core.ClassDefinitionContext>();
        var plainInterfaces = new List<Core.InterfaceDefinitionContext>();
        foreach (var c in classes)
        {
            var id = c.classIdParagraph();
            if (id.ooParameterName().Length == 0) { plainClasses.Add(c); continue; }
            AddSkeleton(c, id.className(0).GetText(), false, id.ooParameterName(), c.environmentDivision(),
                "CLASS-ID", "ISO §11.3.3 SR8", "ISO §11.3.3 SR9");
        }
        foreach (var i in interfaces)
        {
            if (i.ooParameterName().Length == 0) { plainInterfaces.Add(i); continue; }
            var names = i.interfaceName();
            AddSkeleton(i, names[0].GetText(), true, i.ooParameterName(), i.environmentDivision(),
                "INTERFACE-ID", "ISO §11.6.3 SR4", "ISO §11.6.3 SR7");
        }
        var entries = new List<Core.RepositoryEntryContext>();
        CollectExpandsEntries(tree, entries);
        if (skeletons.Count == 0 && entries.Count == 0)
            return new Result(classes, interfaces, new HashSet<string>(), [], [], new HashSet<string>());   // the overwhelmingly common path
        // §12.3.8.3 SR3 — an EXPANDS phrase inside a parameterized definition is reported once, on the definition
        // (AddSkeleton), and is otherwise inert: it creates nothing.
        entries.RemoveAll(e => EnclosingSkeleton(e) is not null);

        // Every name an actual parameter may resolve to: the written plain definitions and every expansion NAME
        // (an actual may itself be an expansion, declared in the same paragraph — §12.3.8.3 SR4).
        var classNames = new HashSet<string>(plainClasses.Select(c => c.classIdParagraph().className(0).GetText()),
            CobolNames.Comparer);
        var ifaceNames = new HashSet<string>(plainInterfaces.Select(i => i.interfaceName(0).GetText()),
            CobolNames.Comparer);
        foreach (var sk in skeletons.Values)
            if (classNames.Contains(sk.Name) || ifaceNames.Contains(sk.Name))
            {
                using var _ = edition.At(sk.Ctx);
                edition.Error(sk.IsInterface ? "COBOLNET0840" : "COBOLNET0820",
                    $"duplicate {Kind(sk)} definition '{sk.Name}' — class and interface names share one namespace "
                    + "and shall be unique in the compilation group (ISO §8.3.2.2/§11.3/§11.6)");
            }
        foreach (var e in entries)
        {
            string en = DeclaredName(e)!;
            if (skeletons.TryGetValue(en, out var clash))
            {
                using var _ = edition.At(e);
                edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                    $"REPOSITORY {(e.INTERFACE() is not null ? "INTERFACE" : "CLASS")} '{en}' EXPANDS: '{en}' is the "
                    + $"name of the parameterized {Kind(clash)} definition itself, so no expansion can be created "
                    + "under it (ISO §12.3.8.4 GR1/GR5; §8.3.2.2)");
                continue;
            }
            (e.INTERFACE() is not null ? ifaceNames : classNames).Add(en);
        }

        var expansions = new Dictionary<string, Expansion>(CobolNames.Comparer);
        var order = new List<Expansion>();
        foreach (var e in entries)
            if (!skeletons.ContainsKey(DeclaredName(e)!) && Validate(e) is { } x)
            {
                if (!expansions.TryGetValue(x.Name, out var prior)) { expansions.Add(x.Name, x); order.Add(x); }
                else if (!ReferenceEquals(prior.Of, x.Of)
                         || !prior.Actuals.SequenceEqual(x.Actuals, CobolNames.Comparer))
                {
                    using var _ = edition.At(e);
                    edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                        $"REPOSITORY {Kind(x.Of)} '{x.Name}' EXPANDS {x.Of.Name} USING {string.Join(" ", x.Actuals)}: "
                        + $"the name '{x.Name}' is already given to the expansion of {prior.Of.Name} USING "
                        + $"{string.Join(" ", prior.Actuals)} — expansions {(ReferenceEquals(prior.Of, x.Of) ? "with different actual parameters" : "of different parameterized definitions")} "
                        + $"are not the same {Kind(x.Of)} instance and shall not have the same externalized name "
                        + (x.Of.IsInterface ? "(ISO §9.3.13)" : "(ISO §9.3.12)"));
                }
            }

        var outClasses = new List<Core.ClassDefinitionContext>(plainClasses);
        var outInterfaces = new List<Core.InterfaceDefinitionContext>(plainInterfaces);
        var expandedNames = new HashSet<string>(CobolNames.Comparer);
        foreach (var x in order)
        {
            var tokens = SubstitutedTokens(x);
            if (x.Of.IsInterface)
            {
                if (FragmentParse.ParseTokens(tokens, edition.Edition, words, p => p.interfaceDefinition()) is { } ictx)
                {
                    outInterfaces.Add(ictx);
                    expandedNames.Add(x.Of.Name);
                }
                else ReparseFailed(x);
            }
            else if (FragmentParse.ParseTokens(tokens, edition.Edition, words, p => p.classDefinition()) is { } cctx)
            {
                outClasses.Add(cctx);
                expandedNames.Add(x.Of.Name);
            }
            else ReparseFailed(x);
        }
        return new Result(outClasses, outInterfaces, new HashSet<string>(skeletons.Keys, CobolNames.Comparer),
            [.. classes.Where(c => c.classIdParagraph().ooParameterName().Length > 0)],
            [.. interfaces.Where(i => i.ooParameterName().Length > 0)], expandedNames);

        void AddSkeleton(ParserRuleContext ctx, string name, bool isInterface, Core.OoParameterNameContext[] formals,
            Core.EnvironmentDivisionContext? env, string header, string sr8, string sr9)
        {
            using var _ = edition.At(ctx);
            // The definition's OWN REPOSITORY paragraph (§11.3.3 SR8 / §11.6.3 SR4 say "of this class
            // definition" / "of this interface definition" — never a factory's or object's, which §12.3.3 SR3
            // forbids to carry one anyway).
            var repo = OoRepositoryScope.SpecifierEntries(env).ToList();
            var kinds = new List<bool?>();
            var seen = new HashSet<string>(CobolNames.Comparer);
            foreach (var f in formals)
            {
                string fn = f.GetText();
                using var __ = edition.At(f);
                if (!seen.Add(fn))
                    edition.Error(DiagnosticCatalog.ParameterizedDefinitionUsing,
                        $"{header} '{name}' USING: the parameter-name '{fn}' is written more than once — a given "
                        + $"parameter-name shall not appear more than once in a USING clause ({sr9})");
                bool? kind = repo.FirstOrDefault(r => CobolNames.Same(DeclaredName(r), fn)) is { } decl
                    ? decl.INTERFACE() is not null : null;
                if (kind is null)
                    edition.Error(DiagnosticCatalog.ParameterizedDefinitionUsing,
                        $"{header} '{name}' USING: the parameter-name '{fn}' is not declared by a class-specifier or "
                        + $"interface-specifier in the REPOSITORY paragraph of this {(isInterface ? "interface" : "class")} "
                        + $"definition ({sr8})");
                kinds.Add(kind);
            }
            // §12.3.8.3 SR12 / SR13 (kb/Work PB1744): a parameter-name is a user-defined word (§8.3.2.2) in the scope
            // of this definition's REPOSITORY paragraph, so one its FUNCTION … INTRINSIC specifier identifies is
            // refused — through the one declaration funnel, in a scope of its own: the binder that binds the definition
            // as itself (OoDriver.BindParameterized, kb/Work PB2051) declares the formal's class-specifier as an
            // object-class-name, and one word is only one type of user-defined word in a scope (§8.3.2.2).
            new DataBinder(edition) { CobolWords = words }
                .DeclareParameterNames(OoRepositoryScope.RepositoryEntries(env), formals);
            // §11.3.4 GR6 / §11.6.4 GR4: a parameter-name may be specified "only where an object-class-name or an
            // interface-name is permitted". Every occurrence is substituted (that is GR5), so a formal written in
            // a DECLARATION slot would silently rename a data item, paragraph, method or file in every expansion;
            // one written in a data REFERENCE is caught downstream (the actual names no data item). The
            // declaration slots are exactly the name rules below — none of them admits a class-name.
            var formalSet = new HashSet<string>(formals.Select(f => f.GetText()), CobolNames.Comparer);
            foreach (var w in DeclarationSlotWords(ctx).Where(w => formalSet.Contains(w.GetText())))
            {
                using var __ = edition.At(w);
                edition.Error(DiagnosticCatalog.ParameterizedDefinitionUsing,
                    $"{header} '{name}': the parameter-name '{w.GetText()}' is written as a {SlotKind(w)} — a "
                    + "parameter-name may be specified within the definition only where an object-class-name or an "
                    + $"interface-name is permitted ({(isInterface ? "ISO §11.6.4 GR4" : "ISO §11.3.4 GR6")})");
            }
            foreach (var r in repo.Where(r => r.expandsPhrase() is not null))
            {
                using var __ = edition.At(r);
                edition.Error(DiagnosticCatalog.ParameterizedDefinitionUsing,
                    $"{header} '{name}' is parameterized (it has a USING clause), so its REPOSITORY paragraph shall "
                    + $"not specify the EXPANDS phrase of '{DeclaredName(r)}' (ISO §12.3.8.3 SR3)");
            }
            // The skeleton's own END CLASS / END INTERFACE name (§10.7.3 SR4 / SR6) is asked with every other end
            // marker by Validation.EndMarkerPass, over the written tree, before any expansion renames it.
            if (!skeletons.TryAdd(name, new Skeleton(ctx, name, isInterface, formals, kinds)))
                edition.Error(isInterface ? "COBOLNET0840" : "COBOLNET0820",
                    $"duplicate {(isInterface ? "interface" : "class")} definition '{name}' — class and interface "
                    + "names share one namespace and shall be unique in the compilation group (ISO §8.3.2.2/§11.3/§11.6)");
        }

        Expansion? Validate(Core.RepositoryEntryContext e)
        {
            using var _ = edition.At(e);
            var ph = e.expandsPhrase();
            bool isInterface = e.INTERFACE() is not null;
            string kind = isInterface ? "INTERFACE" : "CLASS";
            string name = DeclaredName(e)!;
            string target = ph.expandsTarget().GetText();
            var actuals = ph.expandsActual().Select(a => a.GetText()).ToList();
            string where = $"REPOSITORY {kind} '{name}' EXPANDS {target}";

            // §12.3.8.3 SR4 / SR7: the parameterized name and every actual "shall be defined in the same
            // REPOSITORY paragraph where object-class-name-1 [interface-name-2] is defined".
            string sr = isInterface ? "ISO §12.3.8.3 SR7" : "ISO §12.3.8.3 SR4";
            var paragraph = ((Core.RepositoryParagraphContext)e.Parent).repositoryEntry();
            bool ok = true;
            foreach (string n in actuals.Prepend(target))
                if (!paragraph.Any(r => CobolNames.Same(DeclaredName(r), n)))
                {
                    edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                        $"{where}: '{n}' is not defined by a class-specifier or interface-specifier in the same "
                        + $"REPOSITORY paragraph ({sr})");
                    ok = false;
                }

            string gr = isInterface ? "ISO §12.3.8.4 GR8" : "ISO §12.3.8.4 GR5";
            if (!skeletons.TryGetValue(target, out var sk) || sk.IsInterface != isInterface)
            {
                bool isPlain = (isInterface ? ifaceNames : classNames).Contains(target);
                edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                    $"{where}: '{target}' is not a parameterized {(isInterface ? "interface" : "class")} of this "
                    + "compilation group — " + (sk is not null
                        ? $"it is a parameterized {(sk.IsInterface ? "interface" : "class")}, and "
                          + $"{(isInterface ? "an interface-specifier expands a parameterized interface" : "a class-specifier expands a parameterized class")}"
                        : isPlain ? $"it has no USING clause, so there is no formal parameter to replace"
                        : "no definition of that name is in this compilation group")
                    + $" ({gr})");
                return null;
            }
            if (actuals.Count != sk.Formals.Count)
            {
                edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                    $"{where}: {actuals.Count} actual parameter(s) are supplied but '{sk.Name}' declares "
                    + $"{sk.Formals.Count} in the USING clause of its {(isInterface ? "INTERFACE-ID" : "CLASS-ID")} "
                    + $"paragraph — the numbers shall be the same ({gr})");
                return null;
            }
            for (int i = 0; i < actuals.Count; i++)
            {
                string a = actuals[i];
                bool? actualIsInterface = skeletons.ContainsKey(a) ? null
                    : classNames.Contains(a) ? false : ifaceNames.Contains(a) ? true : null;
                if (actualIsInterface is null)
                {
                    edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                        $"{where}: the actual parameter '{a}' is " + (skeletons.ContainsKey(a)
                            ? "itself a parameterized definition, which is a skeleton and not a class or interface "
                              + "(ISO §9.3.12/§9.3.13); expand it in the REPOSITORY and pass the expansion"
                            : "not a class or interface of this compilation group")
                        + $" ({gr})");
                    ok = false;
                }
                else if (sk.FormalIsInterface[i] is { } fk && fk != actualIsInterface)
                {
                    edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                        $"{where}: the actual parameter '{a}' is {(actualIsInterface.Value ? "an interface" : "a class")} "
                        + $"but the formal '{sk.Formals[i].GetText()}' is declared by {(fk ? "an interface-specifier" : "a class-specifier")} "
                        + $"in '{sk.Name}' — replacing the formal by the actual ({gr}) would write "
                        + $"{(fk ? "an interface-specifier" : "a class-specifier")} naming {(actualIsInterface.Value ? "an interface" : "a class")}");
                    ok = false;
                }
            }
            return ok ? new Expansion(name, sk, actuals) : null;
        }

        void ReparseFailed(Expansion x)
        {
            using var _ = edition.At(x.Of.Ctx);
            edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                $"the expansion '{x.Name}' of {x.Of.Name} USING {string.Join(" ", x.Actuals)} does not parse once its "
                + "formal parameters are replaced (ISO §12.3.8.4 GR5/GR8) — an actual parameter's spelling is not "
                + "usable in every position its formal occupies");
        }

        Skeleton? EnclosingSkeleton(RuleContext e)
        {
            for (var c = e.Parent; c is not null; c = c.Parent)
                if (c is Core.ClassDefinitionContext or Core.InterfaceDefinitionContext)
                    return skeletons.Values.FirstOrDefault(s => ReferenceEquals(s.Ctx, c));
            return null;
        }
    }

    private static string Kind(Skeleton s) => s.IsInterface ? "interface" : "class";

    /// <summary>The name a CLASS / INTERFACE specifier DECLARES (object-class-name-1 / interface-name-2); null for
    /// every other specifier.</summary>
    private static string? DeclaredName(Core.RepositoryEntryContext r)
        => r.className()?.GetText() ?? r.interfaceName()?.GetText();

    /// <summary>Every word the definition writes in a DECLARATION slot — a data-name, condition-name,
    /// paragraph- or section-name, method-name, file-name, report-name, screen-name or property-name. None of
    /// those positions admits an object-class-name or interface-name, so a formal parameter found in one breaks
    /// §11.3.4 GR6 / §11.6.4 GR4.</summary>
    private static IEnumerable<Core.CobolWordContext> DeclarationSlotWords(ParserRuleContext root)
    {
        var stack = new Stack<IParseTree>([root]);
        while (stack.Count > 0)
        {
            var n = stack.Pop();
            if (n is Core.CobolWordContext w)
            {
                if (SlotOf(w) is not null) yield return w;
                continue;
            }
            for (int i = n.ChildCount - 1; i >= 0; i--)
                if (n.GetChild(i) is ParserRuleContext c) stack.Push(c);
        }
    }

    private static string SlotKind(Core.CobolWordContext w) => SlotOf(w) ?? "name";

    /// <summary>The declaration slot a word stands in (walking the short name-rule chain above it), or null.</summary>
    private static string? SlotOf(Core.CobolWordContext w)
    {
        for (var p = w.Parent; p is not null && p is not Core.StatementContext; p = p.Parent)
            switch (p)
            {
                case Core.DataNameContext: return "data-name";
                case Core.ConditionNameContext: return "condition-name";
                case Core.ParagraphNameContext: return "paragraph-name";
                case Core.SectionNameContext: return "section-name";
                case Core.MethodNameContext: return "method-name";
                case Core.FileNameContext: return "file-name";
                case Core.ReportNameContext: return "report-name";
                case Core.ScreenNameContext: return "screen-name";
                case Core.PropertyNameContext: return "property-name";
                case Core.DataReferenceContext or Core.ClassNameContext or Core.InterfaceNameContext
                    or Core.RepositoryEntryContext:
                    return null;
            }
        return null;
    }


    /// <summary>Every REPOSITORY entry in the group that carries an EXPANDS phrase. REPOSITORY paragraphs live in
    /// ENVIRONMENT DIVISIONs only, so the walk never descends a DATA or PROCEDURE DIVISION (the bulk of a
    /// tree).</summary>
    private static void CollectExpandsEntries(IParseTree node, List<Core.RepositoryEntryContext> into)
    {
        switch (node)
        {
            case Core.RepositoryEntryContext r:
                if (r.expandsPhrase() is not null) into.Add(r);
                return;
            case Core.DataDivisionContext or Core.ProcedureDivisionContext or ITerminalNode:
                return;
        }
        for (int i = 0; i < node.ChildCount; i++) CollectExpandsEntries(node.GetChild(i), into);
    }

    /// <summary>The expansion's token run: the skeleton's own on-channel tokens, copied (a fresh stream renumbers
    /// them, and the originals belong to the live tree), with — (a) the header's and the end marker's name
    /// replaced by the expansion's name; (b) the USING clause dropped, since the expansion is not parameterized
    /// ("a class is created that functions as a non-parameterized class", §9.3.12); (c) the header's
    /// <c>AS literal-1</c> dropped: it externalizes the SKELETON, and §9.3.12 forbids two expansions with
    /// different actuals to share an externalized name, so carrying it into every expansion would make each
    /// program with two expansions non-conforming by construction — the expansion's externalized name is its
    /// specifier's object-class-name-1 (§12.3.8.4 GR5 "a class object-class-name-1 is created"); and (d) every
    /// user-defined-word occurrence of a formal replaced by its actual (§12.3.8.4 GR5/GR8, sound by §11.3.4 GR6 /
    /// §11.6.4 GR4 — see the type summary).</summary>
    private static List<IToken> SubstitutedTokens(Expansion x)
    {
        var skel = x.Of;
        var map = new Dictionary<string, string>(CobolNames.Comparer);
        for (int i = 0; i < skel.Formals.Count; i++) map[skel.Formals[i].GetText()] = x.Actuals[i];

        var drop = new HashSet<IToken>();
        IToken headerName, endName;
        if (skel.Ctx is Core.ClassDefinitionContext c)
        {
            var id = c.classIdParagraph();
            headerName = id.className(0).Start;
            endName = c.endClassHeader().className().Start;
            DropTokens(id.USING(), id.ooParameterName(), id.externalizedNamePhrase());
        }
        else
        {
            var i = (Core.InterfaceDefinitionContext)skel.Ctx;
            headerName = i.interfaceName(0).Start;
            endName = i.interfaceName()[^1].Start;
            DropTokens(i.USING(), i.ooParameterName(), i.externalizedNamePhrase());
        }

        var result = new List<IToken>();
        Walk(skel.Ctx);
        return result;

        void DropTokens(ITerminalNode? usingKw, Core.OoParameterNameContext[] formals, ParserRuleContext? asPhrase)
        {
            if (usingKw is not null) drop.Add(usingKw.Symbol);
            foreach (var f in formals) AddRange(f);
            if (asPhrase is not null) AddRange(asPhrase);
        }

        void AddRange(ParserRuleContext r)
        {
            for (int i = 0; i < r.ChildCount; i++)
                switch (r.GetChild(i))
                {
                    case ITerminalNode t: drop.Add(t.Symbol); break;
                    case ParserRuleContext sub: AddRange(sub); break;
                }
        }

        void Walk(IParseTree n)
        {
            if (n is ITerminalNode t)
            {
                var tok = t.Symbol;
                if (tok.Type == TokenConstants.EOF || drop.Contains(tok)) return;
                string? text = null;
                if (ReferenceEquals(tok, headerName)
                    || (ReferenceEquals(tok, endName) && CobolNames.Same(tok.Text, skel.Name)))
                    text = x.Name;
                else if ((t.Parent is Core.CobolWordContext || tok.Type == CobolLexer.IDENTIFIER)
                         && map.TryGetValue(tok.Text, out var actual))
                    text = actual;
                var copy = new CommonToken(tok);
                if (text is not null)
                {
                    copy.Text = text;
                    // An actual is an ordinary user-defined word wherever its formal stood (§11.3.4 GR6).
                    copy.Type = CobolLexer.IDENTIFIER;
                }
                result.Add(copy);
                return;
            }
            for (int i = 0; i < n.ChildCount; i++) Walk(n.GetChild(i));
        }
    }
}
