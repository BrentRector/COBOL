// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Expressions;

using Core = CobolParserCore;

/// <summary>⛔ THE ONE "is this arithmetic expression a single unparenthesized primary?" DESCENT, for BOTH
/// assemblies (kb/Work PB172, completed by PB224).
///
/// <para>The reduction is load-bearing in three different rules at once: §8.8.4.7.3 SR2's "a single data item …
/// and that name shall not be enclosed in parentheses", §8.8.1.1's sole-vs-compound operand boundary, and the
/// compile-time-directive operand of §7.3.11 / §13.10.3. Any operator, unary sign, or enclosing parenthesis
/// gives some node on the way down more than one child and stops the descent — which is exactly what all three
/// rules mean by "single".</para>
///
/// <para>⚠ IT REPLACED SIX COPIES OF ITSELF, and the last two were in DIFFERENT ASSEMBLIES, which is why the
/// first collapse (PB172, four copies inside <c>ConditionBinder</c>) could not finish the job: a compiler-side
/// helper is unreachable from <c>CompileTimeExpressionEvaluator</c>, which lives one layer down in
/// <c>Cobol.Net.Frontend</c>. The descent operates on PARSE TREES and on nothing else, so the frontend is where
/// it belongs and both layers can read it. The fifth copy was <c>IntrinsicBinder.SoleDataReference</c>, an
/// independently-written tier-by-tier list-pattern version that agreed with the others by coincidence rather
/// than by construction; the sixth was this file's former twin inside the compile-time evaluator.</para></summary>
public static class SoleOperand
{
    /// <summary>The sole unparenthesized <c>primaryExpression</c> of <paramref name="expr"/>, or
    /// <see langword="null"/> when the expression is compound, signed, or parenthesized (or absent).</summary>
    public static Core.PrimaryExpressionContext? Primary(Core.ArithmeticExpressionContext? expr)
    {
        IParseTree? n = expr;
        if (n is null) return null;
        while (n is not Core.PrimaryExpressionContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        return (Core.PrimaryExpressionContext)n;
    }

    /// <summary>The expression INSIDE the enclosing parentheses of <paramref name="expr"/> — through any number of
    /// them, <c>((X))</c> giving <c>X</c> — or <paramref name="expr"/> itself when it is not wholly enclosed. It is the
    /// one reading of a parenthesized operand for a caller whose rule says the parentheses change nothing:
    /// §8.8.4.2.1 — "The inclusion in parentheses of simple conditions does not change the simple condition truth
    /// value" — makes <c>(X1)</c>, <c>(S1-OFF)</c> and <c>(BW)</c> the simple conditions <c>X1</c>, <c>S1-OFF</c> and
    /// <c>BW</c> (kb/Work PB1464). <see cref="Primary"/> is deliberately the OPPOSITE reading (§8.8.4.7.3 SR2's "not
    /// enclosed in parentheses"), so the two are separate members and a caller names the rule it asks.</summary>
    public static Core.ArithmeticExpressionContext? Unparenthesized(Core.ArithmeticExpressionContext? expr)
    {
        // GROUPING-PAREN-ONLY: a primaryExpression parenthesis is always the arithmetic grouping one, never the
        // §8.4.3.2.3 SR6 argument-list twin, which belongs to functionCall (its tokens are FNARG_LPAREN/RPAREN).
        while (Primary(expr) is { } p && p.LPAREN() is not null && p.arithmeticExpression() is { } inner)
            expr = inner;
        return expr;
    }

    /// <summary>The sole <c>dataReference</c> primary of <paramref name="expr"/>, or null.</summary>
    public static Core.DataReferenceContext? DataRef(Core.ArithmeticExpressionContext? expr) =>
        Primary(expr)?.dataReference();

    /// <summary>⛔ THE ONE "does this operand CONSIST OF A SINGLE NUMERIC LITERAL?" TEST — the operand's literal
    /// text INCLUDING a leading sign, or <see langword="null"/> when the operand is anything else.
    ///
    /// <para><b>It is deliberately NOT <see cref="Primary"/>.</b> Every other member of this class answers
    /// §8.8.4.7.3 SR2's question ("a single data item … not enclosed in parentheses"), for which a leading sign
    /// makes the operand compound. THIS one answers a different question, and §8.3.3.3.2 rule 2 gives it the
    /// opposite answer: "A literal shall not contain more than one sign character. If a sign is used, it shall
    /// appear as the leftmost character of the literal" — so a sign written ADJACENT to the digits is INSIDE the
    /// literal, and <c>-5</c> consists of a single literal. (There is no signed data reference and no signed
    /// function-identifier, which is why only the LITERAL member of the family diverges.) Routing this question
    /// through <see cref="Primary"/> is what classified <c>EVALUATE -5</c> as an arithmetic expression —
    /// accepting non-conforming source on one side of Table 15 and crashing conforming source on the other
    /// (kb/Work PB400).</para>
    ///
    /// <para><b>Three clauses ask it, in three subsystems, in the same words.</b> §7.3.11.4 GR5 — "If the operand
    /// of the DEFINE directive consists of a single numeric literal, that operand is treated as a literal, not as
    /// an arithmetic-expression"; §13.10.3 SR1 for the CONSTANT clause; §14.9.13.4 GR1 — "If an operand of the
    /// EVALUATE statement consists of a single literal, that operand is treated as a literal, not as an
    /// expression". §8.8.4.2.1 asks it once more of a relation operand, where a sole numeric literal against an
    /// alphanumeric operand participates as its WRITTEN character form.</para>
    ///
    /// <para>The contiguity half is <see cref="ArithmeticFormationRules.SignIsPartOfLiteral"/>, so the spelling
    /// that makes <c>- -2</c> a permissible (unary, literal) pair in Table 3 and the spelling that makes
    /// <c>-5</c> a single literal here are ONE test: <c>-5</c> is a literal, <c>- 5</c> (separated) is a unary
    /// operator applied to one, and <c>- -5</c> is a unary operator applied to the literal <c>-5</c> — an
    /// expression under all three clauses, since it does not CONSIST OF a single literal.</para></summary>
    public static string? NumericLiteral(Core.ArithmeticExpressionContext? expr)
    {
        IParseTree? n = expr;
        if (n is null) return null;
        // The sole-child spine down to the unary tier — the same descent Primary makes, stopped one tier
        // higher so the sign is still visible.
        while (n is not Core.UnaryExpressionContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        return NumericLiteral((Core.UnaryExpressionContext)n);
    }

    /// <summary>The same question asked of a unary-tier operand directly — the VALUE clause's
    /// <c>valueClauseOperand : unaryExpression</c> slot, whose every format writes <i>literal-n</i> (§13.18.63.2), so
    /// its operand is a literal only when it IS one: unsigned, or ONE sign abutting a bare numeric literal
    /// (<c>-5</c>). <c>--5</c>, <c>+-5</c> and <c>- 5</c> answer null (kb/Work PB1445: the VALUE reader used to strip
    /// any number of signs, and <c>VALUE --5</c> reached the C# backend as a decrement operator).</summary>
    public static string? NumericLiteral(Core.UnaryExpressionContext? u)
    {
        if (u is null) return null;
        if (u.addOp() is not { } sign)
            return u.primaryExpression()?.numericLiteral()?.GetText();     // unsigned, or a lexer-fused SIGNED_*LIT
        var operand = u.unaryExpression();
        return ArithmeticFormationRules.SignIsPartOfLiteral(sign.Stop, operand)
            ? sign.GetText() + operand!.GetText()
            : null;
    }
}
