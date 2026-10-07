// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding;              // EditionContext, DiagnosticCursorAt
using CobolNet.Common;               // CobolLiteral — the ONE literal codec and its shape rules
using CobolNet.Editions.Diagnostics; // DiagnosticCatalog
using CobolNet.Frontend.Expressions; // ArithmeticFormationRules — the ONE §8.3.3.3.2 rule-2 contiguity test
using CobolNet.Frontend.Generated;   // CobolParserCore contexts of the signed literal slots
using CobolNet.Frontend.Parsing;     // LiteralTokens — the ONE literal token set; WrittenSource — a node's text as written

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
/// <c>COBOLNET1635</c>);</item>
/// <item>the content REPERTOIRE rule of the prefixed formats — §8.3.3.2.3 SR5 (<c>X"…"</c>) and §8.3.3.5.3 SR4
/// (<c>NX"…"</c>) hexadecimal digits, §8.3.3.4.3 SR3 (<c>BX"…"</c>) a hexadecimal digit, §8.3.3.4.3 SR2
/// (<c>B"…"</c>) '0' or '1' (<see cref="CobolLiteral.RepertoireViolation"/> → <c>COBOLNET2630</c>). The lexer delimits
/// a prefixed literal whatever its content (kb/Work PB1441), so this screen is the only place the rule is asked.</item>
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
/// <para>⚠ A SUBSCRIPT LIST AND A KEYWORD-OMITTED ARGUMENT LIST ARE PART OF THIS TREE. Since kb/Work PB2113 the tokens
/// between a reference's parentheses are the ordinary literal tokens, parsed in place, so they are screened here like
/// any other. A subscript the binder materializes is re-parsed from its TEXT (<c>SubscriptExpressionFragment</c>);
/// that fragment tree is never walked here, and does not need to be, because each of its literals is already a token
/// of this tree. <c>LiteralScreenDriftTests</c> derives <see cref="LiteralTokens.Types"/> from the lexer grammar
/// (every token whose body is a literal fragment), so a new literal token cannot escape the screen.</para>
/// <para>Edition-invariant: the rules carry no edition qualifier in the text the repository holds, and the checks
/// they replace ran at every <c>--std</c>. It is a sibling of <see cref="ExpressionFormationPass"/> on the same
/// "orthogonal axis ⇒ separate pass" footing (<c>BinderDriver</c>), and — unlike a <see cref="CursorFollowingVisitor"/>
/// — a plain walk, so no visitor override that declines to descend can hide a literal from it.</para>
/// </remarks>
internal static class LiteralScreenPass
{
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
                if (LiteralTokens.Types.Contains(t.Symbol.Type)) Screen(t, edition);
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

    /// <summary>The first of the three rules the literal violates, in <see cref="CobolLiteral.SyntaxViolation"/>'s one
    /// order, reported at the token under the rule's own descriptor.</summary>
    private static void Screen(ITerminalNode t, EditionContext edition)
    {
        string raw = t.GetText();
        if (CobolLiteral.SyntaxViolation(raw) is not { } v) return;
        using var _ = edition.At(t.Symbol);
        edition.Error(v.Rule switch
        {
            LiteralRule.Repertoire => DiagnosticCatalog.LiteralContentRepertoire,
            LiteralRule.HexGrouping => DiagnosticCatalog.HexLiteralDigitGrouping,
            _ => DiagnosticCatalog.LiteralTooLong,
        }, $"the literal {CobolLiteral.Abbreviated(raw)} {v.Message}");
    }
}
