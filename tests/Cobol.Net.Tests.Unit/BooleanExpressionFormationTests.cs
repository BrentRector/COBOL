// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Parsing;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO/IEC 1989:2023 §8.8.2 boolean-expression FORMATION and PRECEDENCE, driven through the compile-time lane
/// (§7.3.7.2 SR1 "formed in accordance with 8.8.2"; §7.3.7.3 GR1 "the order of precedence … shown in 8.8.2") —
/// the directive fragment parse, the shared <see cref="BooleanExpressionResolver"/> and the shared
/// <see cref="ArithmeticFormationRules"/> the runtime lane also uses (kb/Work PB1370, PB1413). The runtime twins
/// are pinned by <c>tests/conformance/2023/boolean_expression_formation</c> and the PB1413 negatives. Every
/// expected value is derived from §8.8.2 in the test's own comment.
/// </summary>
public sealed class BooleanExpressionFormationTests
{
    private sealed class CollectingDiag : ICtDiagnostics
    {
        public readonly List<(CtDiagCode Code, string Message)> Reports = [];
        public void Report(CtDiagCode code, string message) => Reports.Add((code, message));
    }

    private static (CtValue? Value, CollectingDiag Diag) EvalOperand(string text, Dictionary<string, CtValue>? names = null)
    {
        var frag = DirectiveExpressionFragment.ParseOperand(text);
        Assert.NotNull(frag);
        var diag = new CollectingDiag();
        var eval = new CompileTimeExpressionEvaluator(
            edition: EditionInfo.Latest,
            resolveName: w => names is not null && names.TryGetValue(w, out var v) ? v : null,
            diag: diag,
            vocab: new CtOperandVocabulary("previously defined numeric compilation variables", "ISO §7.3.6.2 SR1b"),
            decimalPointIsComma: false);
        return (eval.EvaluateOperand(frag!.compileTimeOperand(), "test"), diag);
    }

    private static bool? EvalCce(string text)
    {
        var frag = DirectiveExpressionFragment.ParseCce(text);
        Assert.NotNull(frag);
        var diag = new CollectingDiag();
        var eval = new CompileTimeExpressionEvaluator(EditionInfo.Latest, _ => null, diag,
            new CtOperandVocabulary("previously defined numeric compilation variables", "ISO §7.3.6.2 SR1b"), false);
        bool? result = eval.EvaluateCce(frag!.constantConditionalExpression(), "test");
        Assert.Empty(diag.Reports);
        return result;
    }

    private static string? Bits(CtValue? v) => v is { Category: CtCategory.Boolean, Bits: { } b } ? b.Bits : null;

    // ── "a boolean expression enclosed in parentheses" (§8.8.2) — no operator to find ──────────────────────────

    [Theory]
    [InlineData("(B\"101\")", "101")]
    [InlineData("((B\"1100\"))", "1100")]
    public void ParenthesizedBooleanLiteral_IsABooleanOperand(string src, string expected)
    {
        // Table 4 permits ('(', literal) and (literal, ')'); rule 1 lets a boolean expression begin with '('. The
        // value of a parenthesized literal is the literal (rule 7a). It was COBOLNET1619 "malformed".
        Assert.NotNull(DirectiveExpressionFragment.ParseOperand(src));
        var (v, diag) = EvalOperand(src);
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, Bits(v));
    }

    [Theory]
    [InlineData("(B\"1\") = B\"1\"", true)]
    [InlineData("B\"1\" = (B\"1\")", true)]
    [InlineData("(B\"1\") = B\"0\"", false)]
    public void ParenthesizedBooleanLiteral_InACceRelation(string src, bool expected) =>
        Assert.Equal(expected, EvalCce(src));

    // ── rule 7b: a shift takes the precedence of THE PRECEDING OPERATION — B-NOT included ────────────────────────

    [Theory]
    // 1010 B-AND ((B-NOT 0011) B-SHIFT-R 1) = 1010 B-AND 0110 = 0010; B-AND precedence for the shift gives 0100.
    [InlineData("B\"1010\" B-AND B-NOT B\"0011\" B-SHIFT-R 1", "0010")]
    // 1010 B-OR (((B-NOT 0011) B-SHIFT-L 1) B-AND 1111) = 1010 B-OR 1000 = 1010; B-OR precedence gives 1100.
    [InlineData("B\"1010\" B-OR B-NOT B\"0011\" B-SHIFT-L 1 B-AND B\"1111\"", "1010")]
    // A following shift inherits the same (negation) precedence: 0001 B-OR (((B-NOT 0011) << 1) >> 2)
    // = 0001 B-OR (1000 >> 2 = 0010) = 0011; B-OR precedence for the shifts gives (1101 << 1) >> 2 = 0010.
    [InlineData("B\"0001\" B-OR B-NOT B\"0011\" B-SHIFT-L 1 B-SHIFT-R 2", "0011")]
    // The B-NOT FIRST, a binary operation LATER: the shift's preceding operation is the B-AND —
    // ((B-NOT 1100) B-AND 0111) << 1 = (0011 B-AND 0111 = 0011) << 1 = 0110.
    [InlineData("B-NOT B\"1100\" B-AND B\"0111\" B-SHIFT-L 1", "0110")]
    public void Shift_AfterBNot_TakesNegationPrecedence(string src, string expected)
    {
        var (v, diag) = EvalOperand(src);
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, Bits(v));
    }

    [Fact] // rule 7b's whole ladder B-NOT > B-AND > B-XOR > B-OR (GR-7.3.7.3-1):
           // 0011 B-OR (0101 B-XOR (0110 B-AND 0011)) = 0011 B-OR (0101 B-XOR 0010) = 0011 B-OR 0111 = 0111;
           // left-to-right would give 0001.
    public void PrecedenceLadder_NotAndXorOr()
    {
        var (v, diag) = EvalOperand("B-NOT B\"1100\" B-OR B\"0101\" B-XOR B\"0110\" B-AND B\"0011\"");
        Assert.Empty(diag.Reports);
        Assert.Equal("0111", Bits(v));
    }

    // ── rule 5 / Table 4: the shift count is ONE integer operand ───────────────────────────────────────────────

    [Theory]
    [InlineData("B\"1100\" B-SHIFT-L 1 + 1")]   // an arithmetic operator after the count (rule 2's ending)
    [InlineData("B\"1100\" B-SHIFT-L 2 * 1")]
    [InlineData("B\"1100\" B-SHIFT-L (1)")]     // the invalid (shift, '(') pair
    [InlineData("B\"1100\" B-SHIFT-L - 1")]     // a separated sign is a unary operator, no Table 4 symbol
    public void ShiftCount_NotASingleIdentifierOrLiteral_IsATable4Violation(string src)
    {
        var (v, diag) = EvalOperand(src);
        Assert.Null(v);
        Assert.Contains(diag.Reports, r => r.Code == CtDiagCode.ArithmeticRule && r.Message.Contains("Table 4"));
    }

    [Theory]
    [InlineData("B\"1100\" B-SHIFT-L 1.0")]   // integral VALUE, but not an integer literal (§8.3.3.3.2)
    [InlineData("B\"1100\" B-SHIFT-L 1.5")]
    public void ShiftCount_LiteralWithADecimalPoint_IsNotAnIntegerOperand(string src)
    {
        var (v, diag) = EvalOperand(src);
        Assert.Null(v);
        Assert.Contains(diag.Reports, r => r.Code == CtDiagCode.DirectiveRule && r.Message.Contains("integer literal"));
    }

    [Fact] // §7.3.11.4 GR1 — a numeric compilation variable stands where its literal may: K = 2 → 1100 RC 2 = 0011.
           // A variable holding 1.5 is refused, not truncated to 1.
    public void ShiftCount_CompilationVariable()
    {
        var names = new Dictionary<string, CtValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["K"] = CtValue.Numeric(new CobolNet.Runtime.CobolDec(2, 0), "2"),
            ["KF"] = CtValue.Numeric(new CobolNet.Runtime.CobolDec(15, -1), "1.5"),
        };
        var (v, diag) = EvalOperand("B\"1100\" B-SHIFT-RC K", names);
        Assert.Empty(diag.Reports);
        Assert.Equal("0011", Bits(v));

        var (bad, badDiag) = EvalOperand("B\"1100\" B-SHIFT-L KF", names);
        Assert.Null(bad);
        Assert.Contains(badDiag.Reports, r => r.Message.Contains("integer literal"));
    }

    [Fact] // a sign written against the digits is part of the integer literal (§8.3.3.3.2): 1100 B-SHIFT-LC +1 = 1001.
    public void ShiftCount_SignedIntegerLiteral()
    {
        var (v, diag) = EvalOperand("B\"1100\" B-SHIFT-LC +1");
        Assert.Empty(diag.Reports);
        Assert.Equal("1001", Bits(v));
    }
}
