// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding;              // EditionContext, DiagnosticCursorAt
using CobolNet.Common;               // CobolLiteral — the ONE literal codec and its shape rules
using CobolNet.Editions.Diagnostics; // DiagnosticCatalog
using CobolNet.Frontend.Expressions; // ArithmeticFormationRules — the ONE §8.3.3.3.2 rule-2 contiguity test
using CobolNet.Frontend.Generated;   // CobolLexer token types
using CobolNet.Frontend.Parsing;     // WrittenSource — a node's text as written, spacing intact

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
/// <item>the SIGN rule — §8.3.3.3.2 2), a numeric literal's sign is its leftmost character — over the two grammar
/// rules that parse a sign as a separate token (<c>signedNumericLiteral</c>, <c>signedIntegerLiteral</c>): a sign
/// separated from its digits is refused (<c>COBOLNET2155</c>; kb/Work PB1445). Not a token rule, but a rule of the
/// literal as written, so it rides the same walk and every future signed slot is screened with no arm.</item>
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
            if (SignedSlot(node) is ({ } sign, { } first)) ScreenSign(node, sign, first, edition);
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }
    }

    /// <summary>The sign token and the literal's first token of a SIGNED LITERAL SLOT — the two grammar rules that
    /// admit a sign as a separate token in front of a numeric literal: <c>signedNumericLiteral : (PLUS | MINUS)?
    /// numericLiteralCore</c> (every procedure-division <c>literal</c> slot — MOVE, ADD, DISPLAY …) and
    /// <c>signedIntegerLiteral : (PLUS | MINUS)? INTEGERLIT</c> (the slots the standard signs, kb/Work PB553). Both are
    /// superset parses: the DEFAULT-mode lexer emits MINUS INTEGERLIT for <c>-5</c>, <c>- 5</c> and a sign at the end
    /// of a line alike. (null, null) when the node is neither rule or carries no sign.</summary>
    private static (IToken? Sign, IToken? First) SignedSlot(IParseTree node) => node switch
    {
        CobolParserCore.SignedNumericLiteralContext s when (s.PLUS() ?? s.MINUS()) is { } sign =>
            (sign.Symbol, s.numericLiteralCore().Start),
        CobolParserCore.SignedIntegerLiteralContext s when (s.PLUS() ?? s.MINUS()) is { } sign =>
            (sign.Symbol, s.INTEGERLIT().Symbol),
        _ => (null, null),
    };

    /// <summary>§8.3.3.3.2 rule 2 over a signed literal slot (kb/Work PB1445, generalizing PB553's one screened slot):
    /// the sign is the literal's leftmost character only when it ABUTS the digits — the ONE contiguity test,
    /// <see cref="ArithmeticFormationRules.SignAbuts"/>. A separated sign makes two things of one: <c>MOVE - 5 TO A</c>
    /// is refused, where <c>-5</c> is the literal. (<c>IF A = - 5</c> never reaches here: a relation operand is an
    /// arithmetic expression, whose unary-operator alternative the parser takes first.)</summary>
    private static void ScreenSign(IParseTree node, IToken sign, IToken first, EditionContext edition)
    {
        if (ArithmeticFormationRules.SignAbuts(sign, first)) return;
        using var _ = edition.At(sign);
        string written = WrittenSource.Of((ParserRuleContext)node);
        edition.Error(DiagnosticCatalog.SignedLiteralSignNotAdjacent, $"the numeric literal `{written}` has its sign "
            + $"separated from its digits — {ArithmeticFormationRules.SignRuleQuote}, and a space or line break is a "
            + $"separator (ISO §8.3.5). Write `{sign.Text}{first.Text}` if the sign belongs to the literal.");
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
