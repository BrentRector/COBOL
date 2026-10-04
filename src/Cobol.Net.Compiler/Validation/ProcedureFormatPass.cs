// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding;                // ConditionalStatements, Table12StatementNames
using CobolNet.Editions;               // IDiagnosticSink / EditionDiagnostic / EditionSeverity
using CobolNet.Editions.Diagnostics;   // DiagnosticCatalog
using CobolNet.Frontend.Generated;     // CobolParserCore

namespace CobolNet.Validation;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE PROCEDURE DIVISION'S GENERAL-FORMAT SCREENS THAT THE SUPERSET GRAMMAR LEAVES TO A NAMED REFUSAL — one
/// parse-tree pass, run pre-bind beside <see cref="IntegerOperandPass"/> for the same reason: each is a pure SYNTAX
/// rule over the raw tree, at every edition (the rules are the same in 1985, 2002, 2014 and 2023), and the grammar
/// admits the superset so the refusal can NAME the rule instead of answering a bare COBOL0001.
///
/// <list type="number">
/// <item><b>An imperative-statement operand holds only imperative statements</b> (kb/Work PB351; §14.5.1). Every
/// conditional phrase of every statement spells its operand <c>statementBlock</c> (= <c>statement+</c>), so an IF with
/// no END-IF written as a READ's AT END operand, a SEARCH WHEN operand or an EVALUATE WHEN operand compiled. §14.5.1:
/// "An imperative statement specifies an unconditional action to be taken by the runtime element or is a conditional
/// statement that is delimited by its explicit scope terminator", and "Any statement with a conditional phrase that is
/// not terminated by its explicit scope terminator is a conditional statement." The ONE operand that is not an
/// imperative-statement is IF's statement-1 / statement-2 (§14.9.19.3 SR1: "either one or more imperative statements or
/// a conditional statement optionally preceded by one or more imperative statements"), so a <c>statementBlock</c> is
/// screened unless its parent is the IF statement — a NEW phrase rule is screened automatically, which a list of
/// phrase rules could never be. The classification is <see cref="ConditionalStatements.IsConditional"/>.</item>
/// <item><b>Format 1 (with sections) and Format 2 (without sections) are distinct</b> (kb/Work PB1146; §14.2.1, §14.4.1).
/// The grammar parses <c>declarativePart* sentence* procedureUnit*</c>, the union of the two formats. Format 1 prints
/// nothing between the procedure division header (or END DECLARATIVES) and the first section header, prints ONE
/// bracketed DECLARATIVES portion, and §14.4.1 adds "If one paragraph is in a section, all paragraphs shall be in
/// sections" — so once a division has a section (a DECLARATIVES portion is made of sections), a sentence or a paragraph
/// outside every section is refused.</item>
/// </list>
/// </summary>
internal sealed class ProcedureFormatPass(IDiagnosticSink sink) : CursorFollowingVisitor(sink)
{
    /// <summary>Screen every procedure division in the group's raw parse tree.</summary>
    internal static void Run(Core.CompilationUnitContext tree, IDiagnosticSink sink) =>
        new ProcedureFormatPass(sink).VisitPositioned(tree);

    // No statement and no procedure division header lives in these divisions; a nested program's or a method's
    // procedure division is a sibling of them, never inside one.
    public override object? VisitIdentificationDivision(Core.IdentificationDivisionContext ctx) => null;
    public override object? VisitEnvironmentDivision(Core.EnvironmentDivisionContext ctx) => null;
    public override object? VisitDataDivision(Core.DataDivisionContext ctx) => null;

    // ── §14.5.1: an imperative-statement operand ───────────────────────────────────────────────────────────

    public override object? VisitStatementBlock(Core.StatementBlockContext ctx)
    {
        if (ctx.Parent is not Core.IfStatementContext)
            foreach (var s in ctx.statement())
                if (ConditionalStatements.IsConditional(s))
                    ReportConditionalOperand(ctx, s);
        return base.VisitChildren(ctx);
    }

    private void ReportConditionalOperand(Core.StatementBlockContext block, Core.StatementContext s)
    {
        string name = Table12StatementNames.NameOf(s);
        string owner = OwningStatementName(block);
        string phrase = PhraseWords(block);
        using var _ = Sink.At(s);
        Sink.Report(new EditionDiagnostic(DiagnosticCatalog.ConditionalStatementAsImperative.Code,
            EditionSeverity.Error, DiagnosticCatalog.ConditionalStatementAsImperative.Id,
            $"{owner} … {phrase}: this {name} statement is written without its explicit scope terminator END-{name}, "
            + "so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO "
            + "§14.5.1: \"Any statement with a conditional phrase that is not terminated by its explicit scope terminator "
            + "is a conditional statement\"; an imperative statement \"is a conditional statement that is delimited by its "
            + $"explicit scope terminator\". Write END-{name} to close it.",
            name, DiagnosticCatalog.ConditionalStatementAsImperative.IsoSection));
    }

    /// <summary>The Table 12 name of the statement whose phrase holds <paramref name="block"/>.</summary>
    private static string OwningStatementName(Core.StatementBlockContext block)
    {
        for (RuleContext? p = block.Parent; p is not null; p = p.Parent)
            if (p is Core.StatementContext st) return Table12StatementNames.NameOf(st);
        return "the statement";
    }

    /// <summary>The phrase's keywords as written — the terminal tokens immediately before the operand in its rule
    /// (<c>NOT ON SIZE ERROR</c>, <c>AT END</c>, <c>WHEN OTHER</c>), else the rule's first token (an EVALUATE or
    /// SEARCH WHEN operand follows a condition, an inline PERFORM's follows its head).</summary>
    private static string PhraseWords(Core.StatementBlockContext block)
    {
        var parent = (ParserRuleContext)block.Parent;
        int at = parent.children.IndexOf(block);
        var words = new List<string>();
        for (int i = at - 1; i >= 0 && parent.GetChild(i) is ITerminalNode t; i--) words.Insert(0, t.GetText());
        return (words.Count == 0 ? parent.Start.Text : string.Join(' ', words)).ToUpperInvariant();
    }

    // ── §14.2.1 / §14.4.1: Format 1 versus Format 2 ────────────────────────────────────────────────────────

    public override object? VisitProcedureDivision(Core.ProcedureDivisionContext ctx)
    {
        var declaratives = ctx.declarativePart();
        if (declaratives.Length > 1)
            ReportFormat(declaratives[1],
                "a second DECLARATIVES portion — §14.2.1 Format 1 prints ONE bracketed DECLARATIVES … END DECLARATIVES "
                + "portion, immediately after the procedure division header");

        var units = ctx.procedureUnit();
        bool hasSections = declaratives.Length > 0 || units.Any(u => u.sectionDefinition() is not null);
        if (hasSections)
        {
            string why = units.Any(u => u.sectionDefinition() is not null)
                ? "the procedure division has sections, so it is §14.2.1 Format 1 (with-sections)"
                : "the procedure division has DECLARATIVES, which only §14.2.1 Format 1 (with-sections) admits";
            var sentences = ctx.sentence();
            if (sentences.Length > 0)
                ReportFormat(sentences[0],
                    $"a sentence outside every section — {why}, which prints nothing between the procedure division "
                    + "header (or END DECLARATIVES) and the first section header");
            var paragraphs = units.Select(u => u.paragraphDefinition()).OfType<Core.ParagraphDefinitionContext>().ToList();
            if (paragraphs.Count > 0)
                ReportFormat(paragraphs[0],
                    $"paragraph {paragraphs[0].paragraphName().GetText().ToUpperInvariant()} is not in a section"
                    + (paragraphs.Count > 1 ? $" (nor are {paragraphs.Count - 1} more paragraphs after it)" : "")
                    + $" — {why}, and §14.4.1: \"If one paragraph is in a section, all paragraphs shall be in sections\"");
        }
        return base.VisitChildren(ctx);
    }

    private void ReportFormat(ParserRuleContext at, string detail)
    {
        using var _ = Sink.At(at);
        Sink.Report(new EditionDiagnostic(DiagnosticCatalog.ProcedureDivisionFormatMixed.Code, EditionSeverity.Error,
            DiagnosticCatalog.ProcedureDivisionFormatMixed.Id,
            $"PROCEDURE DIVISION: {detail}.", "PROCEDURE DIVISION",
            DiagnosticCatalog.ProcedureDivisionFormatMixed.IsoSection));
    }
}
