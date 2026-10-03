// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Expressions;

/// <summary>
/// ⛔ THE ONE LIST OF THE BOOLEAN-OPERATOR TOKENS (ISO §8.8.2 rules 5–8; kb/Work PB1412), asked by every reader that
/// must tell a genuine boolean expression from an operand the grammar merely routed through the
/// <c>booleanExpression</c> rule: the parser's look-ahead predicates (<c>boolExprAhead</c>, <c>boolArgAhead</c>), the
/// condition binder's relation / simple-condition discriminator, and the introduction gate's two edition lists.
/// <para>The list used to be written four times. The binder's copy named the four 2002 tokens and omitted the four
/// COBOL-2023 shift tokens that the parser's predicates had learned when shifts landed, so a shift-ONLY boolean
/// expression — <c>IF A B-SHIFT-L 1 = B"1000"</c>, <c>IF F B-SHIFT-L 1</c>, <c>EVALUATE A B-SHIFT-L 1</c> — parsed as
/// a boolean expression and was then taken for a bare operand at bind and refused. Two arms of one dispatch, one
/// updated when shifts landed. <c>BooleanOperatorTokenDriftTests</c> reads the lexer's token vocabulary, so the next
/// <c>B-…</c> operator word fails a test until it is classified here.</para>
/// <para>Three tiers, because the three readers ask three different questions of the same tokens: an INFIX
/// operator needs a completed left operand to be an operator at all (below 2002 B-AND, B-OR and B-XOR are legal
/// user words, §8.9), the PREFIX operator B-NOT needs an operand start after it, and a SHIFT operator is never a
/// user word, so it is always the operator. <see cref="IsOperator"/> is their union.</para>
/// </summary>
public static class BooleanOperatorTokens
{
    /// <summary>A binary boolean operator between two operands: B-AND, B-OR, B-XOR (COBOL-2002, §8.8.2 rule 7) and the
    /// four shifts (COBOL-2023, rule 8), whose right operand is an integer rather than a boolean.</summary>
    public static bool IsInfix(int type) => IsBinary(type) || IsShift(type);

    /// <summary>B-AND, B-OR, B-XOR — the COBOL-2002 binary operators (§8.7.2).</summary>
    public static bool IsBinary(int type) => type is CobolLexer.B_AND or CobolLexer.B_OR or CobolLexer.B_XOR;

    /// <summary>B-NOT — the COBOL-2002 unary operator, written before its operand.</summary>
    public static bool IsPrefix(int type) => type == CobolLexer.B_NOT;

    /// <summary>B-SHIFT-L, B-SHIFT-R, B-SHIFT-LC, B-SHIFT-RC — the COBOL-2023 shift operators (§8.8.2 rule 8), a
    /// DISTINCT construct from the 2002 set so a program using only shifts at 2002 is told about the shift.</summary>
    public static bool IsShift(int type) =>
        type is CobolLexer.B_SHIFT_L or CobolLexer.B_SHIFT_R or CobolLexer.B_SHIFT_LC or CobolLexer.B_SHIFT_RC;

    /// <summary>The COBOL-2002 operator tokens (§8.7.2): the binary operators and B-NOT.</summary>
    public static bool IsBinaryOrNot(int type) => IsBinary(type) || IsPrefix(type);

    /// <summary>Any boolean-operator token.</summary>
    public static bool IsOperator(int type) => IsInfix(type) || IsPrefix(type);

    /// <summary>True when the subtree <paramref name="t"/> holds a terminal of which <paramref name="isOperator"/> is
    /// true — the whole-subtree scan every site-level reader makes.</summary>
    public static bool Contains(IParseTree t, Func<int, bool> isOperator)
    {
        if (t is ITerminalNode term) return isOperator(term.Symbol.Type);
        for (int i = 0; i < t.ChildCount; i++)
            if (Contains(t.GetChild(i), isOperator)) return true;
        return false;
    }

    /// <summary>True when the subtree holds any boolean-operator token — the discriminator between a genuine boolean
    /// expression and a bare operand parsed through the <c>booleanExpression</c> rule.</summary>
    public static bool ContainsOperator(IParseTree t) => Contains(t, IsOperator);
}
