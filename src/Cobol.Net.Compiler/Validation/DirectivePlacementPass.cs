// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Runtime;

namespace CobolNet.Validation;

/// <summary>
/// THE POSITION PASS for the placement rules of the §7.3 directives that need the PARSE TREE (kb/Work PB1005,
/// PB1065, PB1377, PB1378). Each rule is DATA on the directive's <c>constructs.json</c> row
/// (<see cref="ConstructDialectStatus.Placement"/>), so this pass is written once and a directive with one of these
/// restrictions is one row field:
/// <list type="bullet">
/// <item><see cref="DirectivePlacementRule.BetweenClauses"/> — ISO §7.3.14.3 SR1 (FLAG-02), §7.3.15.3 SR1 (FLAG-14)
/// and, for the ALL form, §7.3.22.3 SR3 / §7.3.20.3 SR3: "shall be specified only between clauses in divisions other
/// than the procedure division and only between statements in the procedure division" (and, for ALL, only in a
/// compilation unit). A WARNING (<c>COBOLNET2344</c>, §4.2.2 — the D20 disposition of the sibling SR4 bans): the
/// directive is still processed.</item>
/// <item><see cref="DirectivePlacementRule.OutsideCompilationUnits"/> — ISO §7.3.17.3 SR1 (LEAP-SECOND) and §7.3.21.3
/// SR1 (PROPAGATE): "shall not be specified within a compilation unit". An ERROR (<c>COBOLNET2652</c>): the
/// directive's effect is folded per unit at the unit's first line, which a directive inside the unit leaves
/// undefined. BETWEEN two sibling units, or before the first, or after the last END marker, is outside every unit
/// and LEGAL.</item>
/// </list>
/// A <c>&gt;&gt;PUSH</c> or <c>&gt;&gt;POP</c> that NAMES a directive inherits that directive's rule (§7.3.20.3 SR2,
/// §7.3.22.3 SR2: "shall not be specified where directive-name must not be specified") — the site carries the named
/// row, and the rule of the NAMED row is judged at the PUSH/POP's own position. (The text-level rule,
/// <see cref="DirectivePlacementRule.BeforeFirstCompilationUnit"/>, is judged where the text is —
/// <see cref="DirectiveSiteProcessor"/>.)
///
/// <para>A compiler directive is in no parse tree, so its position is the GAP between the last token before its
/// line and the first token after it — the final line frame, where a directive line and a token line are the same
/// number (<see cref="DirectiveSiteProcessor"/>). Two arms for the between-clauses rule:</para>
/// <list type="number">
/// <item>OUTSIDE a compilation unit — before the first unit's first token, or after a unit's END marker and before
/// the next unit. A program with no END PROGRAM header runs on to the end of the text, so a directive after
/// its last token is still inside it. (Only the ALL form says "in a compilation unit"; FLAG-02 / FLAG-14 say only
/// "between clauses … between statements", which a position outside every unit satisfies vacuously.)</item>
/// <item>INSIDE a clause or statement — the innermost clause (a <c>…Clause</c> rule) or <see
/// cref="CobolParserCore.StatementContext"/> spanning the gap, unless the gap is itself a boundary of a nested
/// clause or statement (the next token STARTS one, or the previous token ENDS one): <c>IF X = 1 &gt;&gt;PUSH ALL
/// DISPLAY …</c> is between statements; <c>PERFORM VARYING I FROM 1 &gt;&gt;PUSH ALL BY 1 …</c> is not.</item>
/// </list>
/// <para>The rule reads "between clauses", and an entry's name, a paragraph header or a division header is not a
/// clause; those gaps are not diagnosed — the reading that cannot reject a position the rule admits.</para>
/// </summary>
internal static class DirectivePlacementPass
{
    public static void Run(CobolParserCore.CompilationUnitContext tree, IReadOnlyList<DirectiveSite> sites,
        CobolNet.Binding.EditionContext sink)
    {
        List<(DirectiveSite Site, ConstructDialectStatus? Named, bool BetweenClauses, DirectivePlacement? Placement)>? judged = null;
        foreach (var s in sites)
        {
            // The row whose rule judges this site: the directive NAMED by a PUSH/POP, else the directive itself.
            var named = s.NamedRow is { } id ? ConstructRegistry.Find(id) : null;
            var subject = named ?? (s.AllForm ? null : CompilerDirectiveCatalog.Find(s.Word));
            var placement = subject?.Placement;
            bool between = s.AllForm || placement?.Rule == DirectivePlacementRule.BetweenClauses;
            bool outside = placement?.Rule == DirectivePlacementRule.OutsideCompilationUnits;
            if (between || outside) (judged ??= []).Add((s, named, between, placement));
        }
        if (judged is null) return;   // the common case: no tree walk at all

        var tokens = new List<ITerminalNode>();
        Collect(tree, tokens);
        var units = TopLevelUnits(tree);
        foreach (var (site, named, between, placement) in judged)
        {
            // The gap: the last token before the directive's line and the first token after it.
            int k = FirstAfter(tokens, site.Line);
            ITerminalNode? next = k < tokens.Count ? tokens[k] : null;
            ITerminalNode? prev = k > 0 ? tokens[k - 1] : null;
            bool insideUnit = InsideUnit(units, prev);
            using var _ = sink.At(site.Line, 0);
            if (!between)
            {
                // OutsideCompilationUnits (§7.3.17.3 SR1, §7.3.21.3 SR1).
                if (!insideUnit) continue;
                sink.Error(DiagnosticCatalog.DirectivePlacementViolation, Describe(site, named, placement!, "within a compilation unit"));
                continue;
            }
            string? where = !insideUnit ? "outside every compilation unit"
                : InnermostContainer(prev!, next) is { } c ? $"inside {DescribeContainer(c)}"
                : null;
            if (where is null) continue;
            sink.Warning(DiagnosticCatalog.DirectiveBetweenClausesPlacement,
                Describe(site, named, placement, where) + " The directive is still processed. Move it to a point "
                + "between two clauses or statements of the compilation unit.");
        }
    }

    /// <summary>The diagnostic's sentence: the directive, where it was found, and the rule it broke in the standard's
    /// own words — the named directive's for a PUSH/POP that names one (§7.3.20.3 SR2, §7.3.22.3 SR2), the ALL
    /// form's own otherwise.</summary>
    private static string Describe(DirectiveSite site, ConstructDialectStatus? named, DirectivePlacement? placement, string where)
    {
        if (site.AllForm)
        {
            var (rule, text) = CobolNames.Same(site.Word, "PUSH") ? PushRule : PopRule;
            return $"the >>{site.Word} ALL directive on line {site.Line} is written {where} — \"{text}\" (ISO {rule}).";
        }
        if (named is not null)
        {
            var (rule, _) = CobolNames.Same(site.Word, "PUSH") ? PushNameRule : PopNameRule;
            return $"the >>{site.Word} {named.DirectiveWords[0]} directive on line {site.Line} is written {where}, where "
                + $"{named.DirectiveWords[0]} must not be specified (ISO {rule}: \"the {site.Word} directive shall not be "
                + $"specified where directive-name must not be specified\") — \"{placement!.Text}\" (ISO {placement.Citation}).";
        }
        return $"the >>{site.Word} directive on line {site.Line} is written {where} — \"{placement!.Text}\" (ISO {placement.Citation}).";
    }

    /// <summary>The two ALL-form rules, each in its own words (the PUSH sentence is printed without the POP sentence's commas).</summary>
    private static readonly (string Rule, string Text) PushRule = ("§7.3.22.3 SR3",
        "If ALL is specified, the PUSH directive shall be specified only in a compilation unit between clauses in "
        + "divisions other than the procedure division and between statements in the procedure division");
    private static readonly (string Rule, string Text) PopRule = ("§7.3.20.3 SR3",
        "If ALL is specified, the POP directive shall be specified only in a compilation unit, between clauses in "
        + "divisions other than the procedure division, and between statements in the procedure division");

    /// <summary>The two directive-name rules (SR2 of each).</summary>
    private static readonly (string Rule, string Text) PushNameRule = ("§7.3.22.3 SR2",
        "If directive-name is specified, the PUSH directive shall not be specified where directive-name must not be specified");
    private static readonly (string Rule, string Text) PopNameRule = ("§7.3.20.3 SR2",
        "If directive-name is specified, the POP directive shall not be specified where directive-name must not be specified");

    /// <summary>Every token of the tree in source order, EOF excluded.</summary>
    private static void Collect(IParseTree node, List<ITerminalNode> into)
    {
        if (node is ITerminalNode t)
        {
            if (t.Symbol.Type != TokenConstants.EOF) into.Add(t);
            return;
        }
        for (int i = 0; i < node.ChildCount; i++) Collect(node.GetChild(i), into);
    }

    /// <summary>The index of the first token on a line after <paramref name="line"/> (tokens are line-ordered).</summary>
    private static int FirstAfter(List<ITerminalNode> tokens, int line)
    {
        int lo = 0, hi = tokens.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (tokens[mid].Symbol.Line <= line) lo = mid + 1; else hi = mid;
        }
        return lo;
    }

    /// <summary>Each top-level compilation unit's first and last token index, and whether it ends with an END
    /// marker (a class or interface always does; a program only with its END PROGRAM header).</summary>
    private static List<(int Start, int Stop, bool Closed)> TopLevelUnits(CobolParserCore.CompilationUnitContext tree)
    {
        var units = new List<(int, int, bool)>();
        foreach (var group in tree.compilationGroup())
            for (int i = 0; i < group.ChildCount; i++)
                if (group.GetChild(i) is ParserRuleContext u)
                    units.Add((u.Start.TokenIndex, u.Stop.TokenIndex,
                        u is not CobolParserCore.ProgramUnitContext p || p.endProgramHeader() is not null));
        return units;
    }

    private static bool InsideUnit(List<(int Start, int Stop, bool Closed)> units, ITerminalNode? prev)
    {
        if (prev is null) return false;   // before the first token of the group
        int p = prev.Symbol.TokenIndex;
        foreach (var (start, stop, closed) in units)
        {
            if (p < start) continue;
            if (p < stop) return true;                      // strictly inside the unit's token span
            if (p == stop) return !closed;   // after an END marker: outside; with none, the unit runs on
        }
        return false;
    }

    /// <summary>The innermost clause or statement that spans the gap between <paramref name="prev"/> and
    /// <paramref name="next"/>, when the gap is not a boundary of a nested one; null when the gap lies between
    /// clauses or statements.</summary>
    private static ParserRuleContext? InnermostContainer(ITerminalNode prev, ITerminalNode? next)
    {
        if (next is null) return null;   // after the last token: the end of the unit, not inside a construct
        int p = prev.Symbol.TokenIndex, n = next.Symbol.TokenIndex;
        // Below the lowest common ancestor, a clause or statement that STARTS at next (or ENDS at prev) makes the
        // gap a boundary between two such constructs.
        for (var a = next.Parent as ParserRuleContext; a is not null; a = a.Parent as ParserRuleContext)
        {
            if (a.Start.TokenIndex <= p) break;
            if (IsContainer(a) && a.Start.TokenIndex == n) return null;
        }
        ParserRuleContext? lca = null;
        for (var a = prev.Parent as ParserRuleContext; a is not null; a = a.Parent as ParserRuleContext)
        {
            if (a.Stop is not null && a.Stop.TokenIndex >= n) { lca = a; break; }
            if (IsContainer(a) && a.Stop?.TokenIndex == p) return null;
        }
        for (var a = lca; a is not null; a = a.Parent as ParserRuleContext)
            if (IsContainer(a)) return a;
        return null;
    }

    private static bool IsContainer(ParserRuleContext c) =>
        c is CobolParserCore.StatementContext
        || CobolParserCore.ruleNames[c.RuleIndex].EndsWith("Clause", StringComparison.Ordinal);

    private static string DescribeContainer(ParserRuleContext c) =>
        c is CobolParserCore.StatementContext s
            ? $"the {s.Start.Text.ToUpperInvariant()} statement starting on line {s.Start.Line}"
            : $"a clause ({CobolParserCore.ruleNames[c.RuleIndex]}) starting on line {c.Start.Line}";
}
