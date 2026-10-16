// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Runtime;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>⛔ THE ONE READING OF A CONSTANT-NAME BEFORE ANY DATA DIVISION BINDS (kb/Work PB2588). An AS phrase's
/// literal (§8.3.2.2; <c>externalizedNamePhrase</c>) may be a constant-name (§13.10.3 SR2), and three of its readers run
/// before the constant entry it names is bound: the unit builder (PROGRAM-ID / FUNCTION-ID, <c>BinderDriver.MakeUnit</c>),
/// the class table (CLASS-ID, INTERFACE-ID, METHOD-ID, a prototype method, <c>OoClassTable.Build</c>) and the OO
/// repository scope (a REPOSITORY class- or interface-specifier, <c>OoRepositoryScope</c>). They read the constant
/// entry from the parse tree here; the readers inside the data bind ask <c>DataBinder.ReadConstant</c>, which binds the
/// same entry. Both decode through the same two functions (<see cref="DataBinder.LiteralTokenValue"/>,
/// <see cref="DataBinder.VariableStringValue"/>), so they agree on every constant they both can see.
/// <para>The SCOPE is the source element whose paragraph carries the word (§13.10.3 SR2 names the constant-name the
/// element declares): its own DATA DIVISION, and for a method definition the containing factory or object definition's
/// too — §11.7.4 GR5, "If a given user-defined word is defined in the data division of this method definition and in
/// the data division of the containing object definition, the use of that word in this method refers to the
/// declaration in this method", so the method's own entry is read first. No containing PROGRAM is read: no pre-bind
/// position stands in a contained program — §11.10.3 SR2 ("Literal-1 shall not be specified in a program that is
/// contained within another program"), §12.3.3 SR1 ("The configuration section shall not be specified in a program that
/// is contained within another program") — and a CLASS-ID, INTERFACE-ID or class REPOSITORY has no DATA DIVISION of its
/// own, so a word there names no constant. A data-name of the same spelling declared nearer is the item the word names,
/// never a constant.</para>
/// <para>It REPORTS NOTHING: a constant entry's own faults are the data bind's to report, once, at the entry
/// (<see cref="ConstantReadingKind.Refused"/>).</para></summary>
internal static class PreBindConstants
{
    /// <summary>The constant <paramref name="name"/> names at <paramref name="site"/> (a parse node inside the
    /// paragraph that carries the word).</summary>
    /// <param name="variables">The compilation group's &gt;&gt;DEFINE timeline, for a FROM constant (§13.10.2).</param>
    public static ConstantReading Read(RuleContext site, string name,
        DirectiveTimeline<CompilationVariableEvent> variables) => Read(site, name, variables, hops: 0);

    /// <summary>The longest chain of constant entries, each written AS the next one's name, that is read through; a
    /// longer chain is a circular dependence (ISO §13.10.3 SR5), which the data bind reports at the entry.</summary>
    private const int MaxHops = 64;

    private static ConstantReading Read(RuleContext site, string name,
        DirectiveTimeline<CompilationVariableEvent> variables, int hops)
    {
        for (RuleContext? c = site; c is not null; c = c.Parent)
        {
            if (DataDivisionOf(c) is { } dd)
            {
                foreach (var (entryName, body, line) in Entries(dd))
                {
                    if (!CobolNames.Same(entryName, name)) continue;
                    // A data-name declared in this element is the item the word names (§11.7.4 GR5's nearer
                    // declaration; §8.4.6) — never a constant of an enclosing element.
                    return body is null ? ConstantReading.NotAConstant : ValueOf(body, line, name, variables, hops);
                }
            }
            // §11.7.4 GR5: a method sees its factory / object definition's data; every other source element is the
            // end of the walk.
            if (c is Core.ProgramUnitContext or Core.NestedProgramContext or Core.ClassDefinitionContext
                    or Core.InterfaceDefinitionContext or Core.FactoryParagraphContext or Core.ObjectParagraphContext)
                break;
        }
        return ConstantReading.NotAConstant;
    }

    private static Core.DataDivisionContext? DataDivisionOf(RuleContext c) => c switch
    {
        Core.ProgramUnitContext p => p.dataDivision(),
        Core.NestedProgramContext n => n.dataDivision(),
        Core.MethodDefinitionContext m => m.dataDivision(),
        Core.FactoryParagraphContext f => f.dataDivision(),
        Core.ObjectParagraphContext o => o.dataDivision(),
        _ => null,
    };

    /// <summary>Every NAMED entry of <paramref name="dd"/> in source order: a constant entry with its body, or a data
    /// description entry with a null body.</summary>
    private static IEnumerable<(string Name, Core.ConstantEntryBodyContext? Body, int Line)> Entries(
        Core.DataDivisionContext dd)
    {
        var pending = new Stack<Antlr4.Runtime.Tree.IParseTree>();
        pending.Push(dd);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            switch (node)
            {
                case Core.DataDescriptionEntryContext e when e.dataName()?.GetText() is { } n:
                    yield return (n, e.dataDescriptionBody()?.constantEntryBody(), e.Start.Line);
                    break;
                case Core.ConstantEntryContext k when k.dataName()?.GetText() is { } n:
                    yield return (n, k.constantEntryBody(), k.Start.Line);
                    break;
            }
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }
    }

    /// <summary>The literal a constant entry's operand stands for (§13.10.4 GR1/GR2). A numeric form — an arithmetic
    /// expression (a single numeric literal included, §13.10.3 SR1) or a LENGTH / BYTE-LENGTH phrase (GR3–GR6) — is of
    /// class numeric; its value is no reader's business here, because every pre-bind position admits only the
    /// alphanumeric and national classes.</summary>
    private static ConstantReading ValueOf(Core.ConstantEntryBodyContext body, int line, string name,
        DirectiveTimeline<CompilationVariableEvent> variables, int hops)
    {
        string where = $"constant entry '{name}'";
        if (body.cobolWord() is { } variable)   // FROM compilation-variable-name-1, read as of the entry's line
        {
            if (variables.DefinitionAt(variable.GetText(), line) is not { Value: { } value })
                return ConstantReading.Refused;   // §13.10.3 SR8, reported at the entry
            return ConstantReading.Of(value.Category == CtCategory.Numeric
                ? new ConcatFolder.Folded(PicCategory.Numeric, value.Text)
                : DataBinder.VariableStringValue(value, where));
        }
        // An operand written as ONE constant-name is that constant's literal-1 (§13.10.3 SR2, §13.10.4 GR1), whatever its
        // class - the data bind's DataBinder.ConstantLiteralOperand reading, so the two agree (kb/Work PB780).
        if (SoleOperand.DataRef(body.constantValue()?.arithmeticExpression()) is { } word
            && word.dataReferenceSuffix().Length == 0 && word.cobolWord() is { } spelled)
        {
            if (hops >= MaxHops) return ConstantReading.Refused;
            var named = Read(body, spelled.GetText(), variables, hops + 1);
            if (named.Kind != ConstantReadingKind.NotAConstant) return named;
        }
        if (body.constantValue()?.nonNumericLiteral() is not { } nn)
            return ConstantReading.Of(new ConcatFolder.Folded(PicCategory.Numeric, body.constantValue()?.GetText() ?? ""));
        if (nn.figurativeConstant() is not null || nn.predefinedNull() is not null)
            return ConstantReading.Refused;   // §13.10.3 SR6 / SR7, reported at the entry
        if (nn.concatenationExpression() is { } ce)
        {
            // Folded into a sink of its own: a fault of the operand is the entry's to report (the data bind does).
            var sink = new EditionContext(2023);
            var folded = ConcatFolder.Fold(ce, sink, LiteralEnvironment.Unscoped);
            return sink.HasErrors ? ConstantReading.Refused : ConstantReading.Of(folded);
        }
        return ConstantReading.Of(DataBinder.LiteralTokenValue(nn, where));
    }
}
