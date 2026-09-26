// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Binding;              // EditionContext, DiagnosticCursorAt
using CobolNet.Common;               // CobolLiteral — the ONE literal codec and its shape rules
using CobolNet.Editions.Diagnostics; // DiagnosticCatalog
using CobolNet.Frontend.Generated;   // CobolLexer token types

namespace CobolNet.Validation;

/// <summary>
/// THE ONE PLACE A LITERAL'S OWN SYNTAX RULES ARE ASKED (kb/Work PB1393) — the §8.3.3 rules that are a property of
/// the literal as WRITTEN, whatever position it is written in:
/// <list type="bullet">
/// <item>the LENGTH rule — §8.3.3.2.3 SR1 (alphanumeric, both formats), §8.3.3.4.3 SR1 (boolean, both formats),
/// §8.3.3.5.3 SR1 (national, both formats): at most 8,191 character positions of the literal's class
/// (<see cref="CobolLiteral.LengthViolation"/> → <c>COBOLNET0814</c>);</item>
/// <item>the hexadecimal GROUPING rule — §8.3.3.2.3 SR6 (<c>X"…"</c>, pairs) and §8.3.3.5.3 SR5 (<c>NX"…"</c>, groups
/// of four); a <c>BX"…"</c> literal has none (<see cref="CobolLiteral.HexGroupViolation"/> →
/// <c>COBOLNET1635</c>).</item>
/// </list>
/// </summary>
/// <remarks>
/// <para>⛔ WHY A TOKEN WALK AND NOT A CHECK IN EACH CONSUMER. Each rule used to live in whichever funnel its author
/// was fixing: the length cap in the binder's national and boolean PROCEDURE-operand arms (so an alphanumeric
/// literal, and every VALUE, level-88, CONSTANT and ALL literal of any class, went unchecked), and the grouping
/// rule in the version pass's <c>nonNumericLiteral</c> and <c>ALL</c> visitors (so a concatenation operand — a
/// <c>concatOperand</c>, not a <c>nonNumericLiteral</c> — and the keyword-omitted intrinsic argument, re-parsed
/// from text the version pass never walks, decoded a malformed <c>X"4"</c> to the empty string in silence). A
/// literal is ONE TOKEN whichever rule consumes it, so the check sits on the token: every literal the lexer
/// produced from the unit is screened here exactly once, and a new grammar position that admits a literal needs no
/// arm.</para>
/// <para>⚠ THE CAPTURED REGIONS ARE COVERED BY THEIR SUBSCRIPT-MODE TWINS. A parenthesized capture
/// (<c>subscriptOrRefMod</c>) is lexed in the SUBSCRIPT mode and re-parsed from its TEXT when the binder proves it is a
/// function argument list (<c>FunctionArgFragment</c>) or a subscript expression; those fragment trees are never
/// walked here, and do not need to be, because each of their literals was already a <c>SUB_*</c> literal token of
/// this tree. That argument holds only while every literal format has a SUBSCRIPT-mode twin — which is why
/// <c>SUB_HEXLIT</c> exists — and <c>LiteralScreenDriftTests</c> derives <see cref="LiteralTokenTypes"/> from the
/// lexer grammar (every token whose body is a literal fragment), so a new literal token cannot escape the screen.</para>
/// <para>Edition-invariant: the rules carry no edition qualifier in the text the repository holds, and the checks
/// they replace ran at every <c>--std</c>. It is a sibling of <see cref="ExpressionFormationPass"/> on the same
/// "orthogonal axis ⇒ separate pass" footing (<c>BinderDriver</c>), and — unlike a <see cref="CursorFollowingVisitor"/>
/// — a plain walk, so no visitor override that declines to descend can hide a literal from it.</para>
/// </remarks>
internal static class LiteralScreenPass
{
    /// <summary>The token types that ARE a literal of class alphanumeric, boolean or national: the four DEFAULT-mode
    /// literal tokens and their SUBSCRIPT-mode twins. Derived-and-pinned: <c>LiteralScreenDriftTests</c> reads the
    /// lexer grammar and requires this set to equal the tokens defined over a literal body fragment.</summary>
    internal static readonly IReadOnlySet<int> LiteralTokenTypes = new HashSet<int>
    {
        CobolLexer.STRINGLIT, CobolLexer.HEXLIT, CobolLexer.NATLIT, CobolLexer.BOOLLIT,
        CobolLexer.SUB_STRINGLIT, CobolLexer.SUB_HEXLIT, CobolLexer.SUB_NATLIT, CobolLexer.SUB_BOOLLIT,
    };

    /// <summary>Screen every literal token of <paramref name="tree"/>, reporting to <paramref name="edition"/> at the
    /// token's own position.</summary>
    public static void Run(IParseTree tree, EditionContext edition)
    {
        var pending = new Stack<IParseTree>();
        pending.Push(tree);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is ITerminalNode t)
            {
                if (LiteralTokenTypes.Contains(t.Symbol.Type)) Screen(t, edition);
                continue;
            }
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }
    }

    /// <summary>The two rules, in precedence order: a malformed hexadecimal literal has no value to measure, so the
    /// grouping violation is the one reported for it.</summary>
    private static void Screen(ITerminalNode t, EditionContext edition)
    {
        string raw = t.GetText();
        if (CobolLiteral.HexGroupViolation(raw) is { } grouping)
        {
            using var _ = edition.At(t.Symbol);
            edition.Error(DiagnosticCatalog.HexLiteralDigitGrouping, $"the literal {raw} {grouping}");
        }
        else if (CobolLiteral.LengthViolation(raw) is { } length)
        {
            using var _ = edition.At(t.Symbol);
            edition.Error(DiagnosticCatalog.LiteralTooLong, $"the literal {Abbreviated(raw)} {length}");
        }
    }

    /// <summary>An over-long literal is by definition too long to quote whole in a diagnostic: its first and last few
    /// characters identify it.</summary>
    private static string Abbreviated(string raw) => raw.Length <= 40 ? raw : raw[..24] + "…" + raw[^8..];
}
