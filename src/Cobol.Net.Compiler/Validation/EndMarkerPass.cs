// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Binding;                // EditionContext, the At(ParserRuleContext) cursor
using CobolNet.Editions.Diagnostics;   // DiagnosticCatalog
using CobolNet.Frontend.Generated;     // CobolParserCore
using CobolNet.Runtime;                // CobolNames

namespace CobolNet.Validation;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE ONE SCREEN OF ISO §10.7.3 — every end marker of every source unit, asked once, over the raw parse tree
/// (kb/Work PB988). The general format (§10.7.2) prints nine alternatives; the syntax rules pair each with the ID
/// paragraph of the definition it ends:
/// <list type="bullet">
/// <item>SR1 — "An end marker shall be present in every source unit that contains, is contained in, or precedes
/// another source unit."</item>
/// <item>SR2 / SR7 / SR8 / SR9 — program-name-1, user-function-name-1, program-prototype-name-1 and
/// function-prototype-name-1 "shall be identical to" the name the PROGRAM-ID / FUNCTION-ID paragraph declared;
/// SR4 / SR6 the same of END CLASS and END INTERFACE.</item>
/// <item>SR3 — the END PROGRAM marker of a program whose PROGRAM-ID is stated inside another program "shall
/// precede" the container's.</item>
/// <item>SR5 — "Method-name-1 shall be identical to the method-name declared in the corresponding METHOD-ID
/// paragraph. If the PROPERTY phrase is specified in the METHOD-ID paragraph, method-name-1 shall be
/// omitted."</item>
/// </list>
/// <para>WHY ONE WALK, PRE-BIND. The grammar pairs every marker with the innermost open definition, so a wrong
/// name, a reversed nesting or a missing marker parses — §10.7.4 GR1 ("An end marker indicates the end of the
/// specified source unit") then means the program does not say what the parse assumed. Until PB988 the rule was
/// written in five places (END CLASS and END METHOD twice in <c>OoClassTable.Build</c>, END INTERFACE there and
/// again for parameterized skeletons in <c>OoExpansion</c>), the interface's method prototypes and every PROPERTY
/// method were asked by none of them, and nothing read a PROGRAM or FUNCTION marker's name at all. A pure syntax
/// rule over the tree needs nothing the binder produces, so it runs beside <see cref="LevelNumberPass"/>, before
/// binding — over EVERY definition the source states, parameterized skeletons included (an expansion is renamed
/// at both ends, so it is never asked again).</para>
/// <para>The marker's KEYWORD (END PROGRAM versus END FUNCTION) and the presence of the unconditionally printed
/// markers of the prototype and function formats are §10.6.1's, asked by <c>SourceUnitShape</c>; SR1 here is the
/// positional rule a program definition's bracketed marker answers to.</para>
/// </summary>
internal static class EndMarkerPass
{
    /// <summary>One PROGRAM-ID / FUNCTION-ID unit, whichever of the two grammar rules (<c>programUnit</c>, a unit of
    /// the compilation group; <c>nestedProgram</c>, a contained one) parsed it.</summary>
    private readonly record struct ProgramShape(
        Core.IdentificationDivisionContext Id, Core.NestedProgramContext[] Nested, Core.EndProgramHeaderContext? End);

    /// <summary>Screen every end marker in the group's raw parse tree.</summary>
    internal static void Run(Core.CompilationUnitContext tree, EditionContext edition)
    {
        var units = tree.compilationGroup()
            .SelectMany(g => g.children?.OfType<ParserRuleContext>() ?? [])
            .ToList();
        for (int i = 0; i < units.Count; i++)
            switch (units[i])
            {
                case Core.ProgramUnitContext p:
                    ScreenProgram(new(p.identificationDivision(), p.nestedProgram(), p.endProgramHeader()),
                        [], precedesAnother: i < units.Count - 1, edition);
                    break;
                case Core.ClassDefinitionContext c:
                    ScreenClass(c, edition);
                    break;
                case Core.InterfaceDefinitionContext f:
                    ScreenInterface(f, edition);
                    break;
            }
    }

    private static void ScreenProgram(ProgramShape unit, IReadOnlyList<string> containers, bool precedesAnother,
        EditionContext edition)
    {
        // A unit the parser could not recover an ID paragraph for has no declared name to compare; the syntax
        // error already reported is the diagnostic.
        var body = unit.Id?.identificationBody();
        var pid = body?.programIdParagraph();
        var fid = body?.functionIdParagraph();
        if ((pid?.programName() ?? fid?.programName())?.GetText() is not { } name) return;
        bool isPrototype = (pid?.prototypePhrase() ?? fid?.prototypePhrase()) is not null;
        string paragraph = pid is not null ? "PROGRAM-ID" : "FUNCTION-ID";

        // SR1 — only a program DEFINITION's format brackets its end marker (§10.6.1), so it is the only kind whose
        // marker this positional rule can require; the other three formats print it unconditionally and
        // SourceUnitShape asks that. A contained program always has one (`nestedProgram` requires it).
        if (unit.End is null && pid is not null && !isPrototype && (unit.Nested.Length > 0 || precedesAnother))
            Report(pid, "SR1",
                $"PROGRAM-ID '{name}' {(unit.Nested.Length > 0 ? "contains" : "precedes")} another source unit, so "
                + $"its end marker END PROGRAM {name} shall be present — an end marker shall be present in every "
                + "source unit that contains, is contained in, or precedes another source unit", edition);

        var inner = containers.Append(name).ToList();
        foreach (var n in unit.Nested)
            ScreenProgram(new(n.identificationDivision(), n.nestedProgram(), n.endProgramHeader()), inner,
                precedesAnother: false, edition);

        if (unit.End?.programName() is not { } endName || CobolNames.Same(endName.GetText(), name)) return;

        // The grammar pairs the marker with the innermost open unit. A name that is another unit of the SAME
        // nesting chain — a container still open, or a program this one contains — means the markers were written
        // in the wrong order (SR3); any other name is the plain name rule of the unit's kind.
        if (containers.Any(c => CobolNames.Same(c, endName.GetText()))
            || ContainedNames(unit.Nested).Any(c => CobolNames.Same(c, endName.GetText())))
            Report(endName, "SR3",
                $"END {(pid is not null ? "PROGRAM" : "FUNCTION")} {endName.GetText()} stands where the end marker "
                + $"of {paragraph} '{name}' belongs — the end markers of nested programs are out of order: an END "
                + "PROGRAM marker referencing a program whose PROGRAM-ID paragraph is stated inside another program "
                + "shall precede the END PROGRAM marker referencing the containing program", edition);
        else
        {
            var (rule, noun) = (pid is not null, isPrototype) switch
            {
                (true, false) => ("SR2", "program-name"),
                (true, true) => ("SR8", "program-prototype-name"),
                (false, false) => ("SR7", "user-function-name"),
                (false, true) => ("SR9", "function-prototype-name"),
            };
            Report(endName, rule,
                $"the end marker names '{endName.GetText()}', but the {noun} declared in the {paragraph} paragraph it "
                + $"ends is '{name}' — the name in the end marker shall be identical to it", edition);
        }
    }

    /// <summary>Every program-name declared inside <paramref name="nested"/>, at any depth.</summary>
    private static IEnumerable<string> ContainedNames(IEnumerable<Core.NestedProgramContext> nested) =>
        nested.SelectMany(n =>
        {
            var body = n.identificationDivision()?.identificationBody();
            var name = (body?.programIdParagraph()?.programName() ?? body?.functionIdParagraph()?.programName())?.GetText();
            var deeper = ContainedNames(n.nestedProgram());
            return name is null ? deeper : deeper.Prepend(name);
        });

    private static void ScreenClass(Core.ClassDefinitionContext c, EditionContext edition)
    {
        var declared = c.classIdParagraph()?.className(0);
        var endName = c.endClassHeader()?.className();
        if (declared is not null && endName is not null && !CobolNames.Same(endName.GetText(), declared.GetText()))
            Report(endName, "SR4",
                $"END CLASS {endName.GetText()} ends CLASS-ID '{declared.GetText()}' — object-class-name-1 shall be "
                + "identical to the object-class-name declared in the corresponding CLASS-ID paragraph", edition);
        foreach (var m in (c.factoryParagraph()?.methodDefinition() ?? []).Concat(c.objectParagraph()?.methodDefinition() ?? []))
            ScreenMethod(m, edition);
    }

    private static void ScreenInterface(Core.InterfaceDefinitionContext f, EditionContext edition)
    {
        // `interfaceName()` lists the INTERFACE-ID name, then the INHERITS names, then the END INTERFACE name.
        var names = f.interfaceName();
        if (names.Length > 1 && f.END() is not null && !CobolNames.Same(names[^1].GetText(), names[0].GetText()))
            Report(names[^1], "SR6",
                $"END INTERFACE {names[^1].GetText()} ends INTERFACE-ID '{names[0].GetText()}' — interface-name-1 shall "
                + "be identical to the interface-name declared in the corresponding INTERFACE-ID paragraph", edition);
        foreach (var m in f.methodDefinition())
            ScreenMethod(m, edition);
    }

    /// <summary>SR5, for a method of a class's factory or object definition and for an interface's method prototype
    /// alike — one rule, one arm. With a property selector, <c>methodName()</c> holds only the END METHOD name;
    /// without one, [0] is the METHOD-ID's and [1] (if written) the end marker's.</summary>
    private static void ScreenMethod(Core.MethodDefinitionContext m, EditionContext edition)
    {
        var names = m.methodName();
        if (m.methodPropertySelector() is { } sel)
        {
            if (names.Length > 0)
                Report(names[0], "SR5",
                    $"END METHOD {names[0].GetText()} names a {sel.GetChild(0).GetText().ToUpperInvariant()} PROPERTY "
                    + $"method (property '{sel.propertyName().GetText()}') — if the PROPERTY phrase is specified in "
                    + "the METHOD-ID paragraph, method-name-1 shall be omitted (write END METHOD.)", edition);
        }
        else if (names.Length > 1 && !CobolNames.Same(names[1].GetText(), names[0].GetText()))
            Report(names[1], "SR5",
                $"END METHOD {names[1].GetText()} ends METHOD-ID '{names[0].GetText()}' — method-name-1 shall be "
                + "identical to the method-name declared in the corresponding METHOD-ID paragraph", edition);
    }

    private static void Report(ParserRuleContext at, string rule, string message, EditionContext edition)
    {
        using var _ = edition.At(at);
        edition.Error(DiagnosticCatalog.EndMarkerRule, $"{message} (ISO §10.7.3 {rule})");
    }
}
