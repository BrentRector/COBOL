// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding;                 // EditionContext, the At(IToken) cursor
using CobolNet.Editions;                // ConstructRegistry, Constructs
using CobolNet.Frontend.Generated;      // CobolParserCore
using CobolNet.Frontend.Preprocessor;   // AreaPlacement, FixedFormAreas

namespace CobolNet.Validation;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE ONE SCREEN OF COBOL-85'S AREAS A AND B (kb/Work PB3147). ANSI X3.23-1985 split the program-text area of a
/// fixed-form line at margin B into area A (character positions 8–11) and area B (position 12 to margin R) and placed
/// every item by where it BEGINS ITS LINE:
/// <list type="bullet">
/// <item>an AREA-A ITEM — a division header, a section header, a paragraph header, a level indicator (FD, SD, RD),
/// level-number 01 or 77, DECLARATIVES, END DECLARATIVES, an end marker — begins its line in area A (row
/// <c>fixed-form-area-a-item-outside-area-a-2002</c>, COBOLNET3734);</item>
/// <item>every other text that begins a line — a sentence, a statement, an entry or clause continued on a later line,
/// a separator period — begins in area B (row <c>fixed-form-text-in-area-a-2002</c>, COBOLNET3735). A level-number
/// other than 01 and 77 may begin in either area: COBOL-85 lets entries be indented by level.</item>
/// </list>
/// ISO/IEC 1989:2002 removed the areas: the program-text area "begins in character position 8 and terminates with the
/// character position immediately to the left of margin R" (ISO §6.3.1) and an entry is continued "in the
/// program-text area of a subsequent line" (§6.3.5), so both rows are introductions of 2002 and the pass asks
/// nothing from COBOL-2002 on. Free form (§6.4.1) has no areas at any edition.
/// <para>THE CONTINUATION LINE IS THE REFERENCE FORMAT'S (kb/Work PB3168). X3.23-1985 also requires "Area A of a
/// continuation line [to] be blank" (the hyphen in the indicator area): the same text-in-area-A row, but the text of a
/// continued literal's tail is no token's start, so this pass cannot place it. The §6.5 logical conversion, which alone
/// sees the indicator, reports it (<c>ReferenceFormatDiagnostics.OnContinuationTextInAreaA</c>), and the map names such a
/// token <see cref="AreaPlacement.BeginsContinuationLineInAreaA"/> so this pass does not report the line a second time.</para>
/// <para>WHY ONE PASS OVER THE TOKENS. The rule has two arms, and each needs the other's knowledge: which tokens head
/// an area-A item is the GRAMMAR's (the parse tree), where a token begins its line is the REFERENCE FORMAT's
/// (<see cref="FixedFormAreas"/>, the record the §6.5 logical conversion keeps of where each fixed-form line's text
/// begins, read through the source map, so a token from library text or REPLACE text is placed where the user wrote
/// it). The pass collects the heads (<see cref="AreaItems"/>) and then asks every token once, so a token that is not
/// a head is screened by the second arm automatically — a new statement, clause or entry needs no change here. It
/// runs pre-bind, beside <see cref="LevelNumberPass"/>: a pure syntax rule over the raw tree whose diagnostics join
/// the edition gates' instead of pre-empting them.</para>
/// </summary>
internal static class AreaPlacementPass
{
    /// <summary>Screen every token of the group's raw parse tree against COBOL-85's areas.</summary>
    internal static void Run(Core.CompilationUnitContext tree, EditionContext edition)
    {
        if (edition.Areas is not { } areas
            || (ConstructRegistry.IsAvailable(edition.Edition, Constructs.FixedFormAreaAItemOutsideAreaA2002)
                && ConstructRegistry.IsAvailable(edition.Edition, Constructs.FixedFormTextInAreaA2002)))
            return;
        var items = new AreaItems();
        items.Visit(tree);
        var pending = new Stack<IParseTree>();
        pending.Push(tree);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is ITerminalNode terminal)
            {
                if (terminal is not IErrorNode && terminal.Symbol.Type != TokenConstants.EOF)
                    Screen(terminal.Symbol, areas.PlacementAt(terminal.Symbol.Line, terminal.Symbol.Column), items, edition);
                continue;
            }
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }
    }

    private static void Screen(IToken token, AreaPlacement placement, AreaItems items, EditionContext edition)
    {
        if (placement == AreaPlacement.None) return;
        using var _ = edition.At(token);
        if (items.Heads.TryGetValue(token.TokenIndex, out string? item))
        {
            // A head that begins a CONTINUATION line in area A is the reference format's to report (kb/Work PB3168).
            if (placement is not (AreaPlacement.BeginsLineInAreaA or AreaPlacement.BeginsContinuationLineInAreaA))
                ConstructRegistry.Check(edition.Edition, edition, Constructs.FixedFormAreaAItemOutsideAreaA2002,
                    $"{item} {(placement == AreaPlacement.WithinLine ? "follows other text of its line" : "begins in area B")}");
        }
        else if (placement == AreaPlacement.BeginsLineInAreaA && !items.EitherArea.Contains(token.TokenIndex))
            ConstructRegistry.Check(edition.Edition, edition, Constructs.FixedFormTextInAreaA2002,
                token.Type == Core.DOT ? "a separator period begins in area A" : $"'{token.Text}' begins in area A");
    }

    /// <summary>
    /// The tokens that head an AREA-A ITEM, each with the words that name it in a diagnostic, and the level-numbers
    /// that may begin in either area. A head is a context's first token where the grammar gives the item a rule of
    /// its own (the section headers, the paragraphs, the level indicators, DECLARATIVES, the level-number); the
    /// headers some rules write INLINE — the IDENTIFICATION / ENVIRONMENT / DATA / PROCEDURE DIVISION header, the
    /// METHOD-ID, INTERFACE-ID, FACTORY and OBJECT paragraph headers, the END PROGRAM / FUNCTION / CLASS / FACTORY /
    /// OBJECT / METHOD / INTERFACE markers and END DECLARATIVES — are found by their spelling among a rule's own
    /// children (<see cref="VisitChildren"/>), so a definition that writes one inline needs no rule of its own here.
    /// </summary>
    private sealed class AreaItems : CobolParserCoreBaseVisitor<object?>
    {
        /// <summary>Token index → the area-A item the token heads.</summary>
        public Dictionary<int, string> Heads { get; } = [];

        /// <summary>The level-numbers other than 01 and 77, which COBOL-85 lets begin in area A or area B.</summary>
        public HashSet<int> EitherArea { get; } = [];

        /// <summary>The paragraph-header keywords a rule writes inline, each followed by its separator period.</summary>
        private static readonly HashSet<int> InlineParagraphHeaders =
            [Core.PROGRAM_ID, Core.FUNCTION_ID, Core.CLASS_ID, Core.INTERFACE_ID, Core.METHOD_ID, Core.FACTORY, Core.OBJECT];

        /// <summary>The words an END marker ends (the METHOD marker's word is the <c>methodKeyword</c> rule).</summary>
        private static readonly HashSet<int> EndMarkerWords =
            [Core.PROGRAM, Core.FUNCTION, Core.CLASS, Core.FACTORY, Core.OBJECT, Core.INTERFACE];

        private void Head(IToken? token, string item)
        {
            if (token is not null) Heads.TryAdd(token.TokenIndex, item);
        }

        private object? Section(ParserRuleContext ctx)
        {
            Head(ctx.Start, $"the section header {ctx.Start.Text} SECTION");
            return VisitChildren(ctx);
        }

        private object? Paragraph(ParserRuleContext ctx)
        {
            Head(ctx.Start, $"the paragraph header {ctx.Start.Text}");
            return VisitChildren(ctx);
        }

        private object? LevelIndicator(ParserRuleContext ctx)
        {
            Head(ctx.Start, $"the level indicator {ctx.Start.Text}");
            return VisitChildren(ctx);
        }

        public override object? VisitChildren(IRuleNode node)
        {
            int n = node.ChildCount;
            for (int i = 0; i < n; i++)
            {
                if (node.GetChild(i) is not ITerminalNode { Symbol: var token }) continue;
                var next = i + 1 < n ? node.GetChild(i + 1) : null;
                var nextToken = (next as ITerminalNode)?.Symbol;
                if (token.Type == Core.DIVISION && i > 0 && node.GetChild(i - 1) is ITerminalNode { Symbol: var name })
                    Head(name, $"the division header {name.Text} DIVISION");
                else if (token.Type == Core.END && nextToken?.Type == Core.DECLARATIVES)
                    Head(token, "END DECLARATIVES");
                else if (token.Type == Core.END
                    && ((nextToken is not null && EndMarkerWords.Contains(nextToken.Type)) || next is Core.MethodKeywordContext))
                    Head(token, $"the end marker END {next!.GetText()}");
                else if (InlineParagraphHeaders.Contains(token.Type) && nextToken?.Type == Core.DOT)
                    Head(token, $"the paragraph header {token.Text}");
            }
            return base.VisitChildren(node);
        }

        // ── Section headers ──────────────────────────────────────────────────────────────────────────────────
        public override object? VisitConfigurationSection(Core.ConfigurationSectionContext ctx) => Section(ctx);
        public override object? VisitInputOutputSection(Core.InputOutputSectionContext ctx) => Section(ctx);
        public override object? VisitFileSection(Core.FileSectionContext ctx) => Section(ctx);
        public override object? VisitWorkingStorageSection(Core.WorkingStorageSectionContext ctx) => Section(ctx);
        public override object? VisitLocalStorageSection(Core.LocalStorageSectionContext ctx) => Section(ctx);
        public override object? VisitLinkageSection(Core.LinkageSectionContext ctx) => Section(ctx);
        public override object? VisitReportSection(Core.ReportSectionContext ctx) => Section(ctx);
        public override object? VisitScreenSection(Core.ScreenSectionContext ctx) => Section(ctx);
        public override object? VisitSectionDefinition(Core.SectionDefinitionContext ctx) => Section(ctx);
        public override object? VisitDeclarativeSection(Core.DeclarativeSectionContext ctx) => Section(ctx);

        // ── Paragraph headers ────────────────────────────────────────────────────────────────────────────────
        public override object? VisitIdentificationParagraph(Core.IdentificationParagraphContext ctx) => Paragraph(ctx);
        public override object? VisitOptionsParagraph(Core.OptionsParagraphContext ctx) => Paragraph(ctx);
        public override object? VisitConfigurationParagraph(Core.ConfigurationParagraphContext ctx) => Paragraph(ctx);
        public override object? VisitFileControlParagraph(Core.FileControlParagraphContext ctx) => Paragraph(ctx);
        public override object? VisitIoControlParagraph(Core.IoControlParagraphContext ctx) => Paragraph(ctx);
        public override object? VisitParagraphDefinition(Core.ParagraphDefinitionContext ctx) => Paragraph(ctx);
        public override object? VisitDeclarativeParagraph(Core.DeclarativeParagraphContext ctx) => Paragraph(ctx);

        // ── Level indicators, DECLARATIVES, level-numbers ────────────────────────────────────────────────────
        public override object? VisitFileDescriptionEntry(Core.FileDescriptionEntryContext ctx) => LevelIndicator(ctx);
        public override object? VisitSortMergeDescriptionEntry(Core.SortMergeDescriptionEntryContext ctx) => LevelIndicator(ctx);
        public override object? VisitReportDescriptionEntry(Core.ReportDescriptionEntryContext ctx) => LevelIndicator(ctx);

        public override object? VisitDeclarativePart(Core.DeclarativePartContext ctx)
        {
            Head(ctx.Start, "DECLARATIVES");
            return VisitChildren(ctx);
        }

        public override object? VisitLevelNumber(Core.LevelNumberContext ctx)
        {
            // A level-number is a word of one digit or two, so level 1 is spelled 1 or 01 and level 77 only 77.
            if (ctx.Start.Text is "1" or "01" or "77")
                Head(ctx.Start, $"level-number {ctx.Start.Text}");
            else
                EitherArea.Add(ctx.Start.TokenIndex);
            return VisitChildren(ctx);
        }
    }
}
