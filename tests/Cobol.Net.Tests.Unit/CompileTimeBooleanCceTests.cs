// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Parsing;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The shared <see cref="CompileTimeExpressionEvaluator"/> BOOLEAN fold (ISO/IEC 1989:2023 §7.3.7 → §8.8.2), the
/// directive-operand dispatch (§7.3 with the §7.3.3 SR10 master constraint — no floating-point literal / figurative
/// constant / concatenation in a directive), and the constant-conditional-expression walk (§7.3.8). Driven through
/// the ANTLR directive-expression fragment parse (<see cref="DirectiveExpressionFragment"/>) — the SAME lexing/parse
/// path the frontend uses — with a stub name resolver + a collecting diagnostic sink.
/// </summary>
public sealed class CompileTimeBooleanCceTests
{
    private sealed class CollectingDiag : ICtDiagnostics
    {
        public readonly List<(CtDiagCode Code, string Message)> Reports = [];
        public void Report(CtDiagCode code, string message) => Reports.Add((code, message));
    }

    private static readonly CtOperandVocabulary Vocab =
        new("previously defined numeric compilation variables", "ISO §7.3.6.2 SR1b");

    private static CompileTimeExpressionEvaluator NewEval(CollectingDiag diag, Dictionary<string, CtValue>? names) =>
        new(edition: EditionInfo.Latest,
            resolveName: w => names is not null && names.TryGetValue(w, out var v) ? v : null,
            diag: diag, vocab: Vocab, decimalPointIsComma: false);

    private static (CtValue? Value, CollectingDiag Diag) EvalOperand(string text, Dictionary<string, CtValue>? names = null)
    {
        var frag = DirectiveExpressionFragment.ParseOperand(text);
        Assert.NotNull(frag);
        var diag = new CollectingDiag();
        return (NewEval(diag, names).EvaluateOperand(frag!.compileTimeOperand(), "test"), diag);
    }

    private static (bool? Result, CollectingDiag Diag) EvalCce(string text, Dictionary<string, CtValue>? names = null)
    {
        var frag = DirectiveExpressionFragment.ParseCce(text);
        Assert.NotNull(frag);
        var diag = new CollectingDiag();
        return (NewEval(diag, names).EvaluateCce(frag!.constantConditionalExpression(), "test"), diag);
    }

    private static string? Bits(CtValue? v) => v is { Category: CtCategory.Boolean, Bits: { } b } ? b.Bits : null;

    // ── boolean fold (§8.8.2) ─────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("B\"1100\" B-AND B\"0101\"", "0100")]   // Annex A Table A.2
    [InlineData("B\"1100\" B-OR B\"0101\"", "1101")]
    [InlineData("B\"1100\" B-XOR B\"0101\"", "1001")]
    [InlineData("B-NOT B\"1100\"", "0011")]
    [InlineData("B\"1\" B-AND B\"1100\"", "1000")]      // unequal length — shorter right-zero-extended (rule 9)
    [InlineData("B\"1\" B-OR B\"1100\"", "1100")]
    public void Boolean_BinaryAndUnary(string src, string expected)
    {
        var (v, diag) = EvalOperand(src);
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, Bits(v));
    }

    [Theory]
    [InlineData("B\"1100\" B-SHIFT-L 3", "0000")]       // logical left, zero-fill (Annex A Table A.2)
    [InlineData("B\"1100\" B-SHIFT-R 3", "0001")]
    [InlineData("B\"1100\" B-SHIFT-LC 3", "0110")]      // circular left
    [InlineData("B\"1100\" B-SHIFT-RC 3", "1001")]      // circular right
    [InlineData("B\"1100\" B-SHIFT-L 0", "1100")]       // count 0 is identity
    [InlineData("B\"1100\" B-SHIFT-L 9", "0000")]       // count ≥ length degenerates
    public void Boolean_Shift(string src, string expected)
    {
        var (v, diag) = EvalOperand(src);
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, Bits(v));
    }

    [Fact] // §8.8.2 rule 7b — a shift takes the precedence of the operator immediately before it (here B-OR), so
           // A B-OR B B-SHIFT-L 1 groups (A B-OR B) B-SHIFT-L 1, NOT A B-OR (B B-SHIFT-L 1). Via the shared resolver.
    public void Boolean_ContextInheritedShiftPrecedence()
    {
        var (v, diag) = EvalOperand("B\"1100\" B-OR B\"0000\" B-SHIFT-L 1");
        Assert.Empty(diag.Reports);
        Assert.Equal("1000", Bits(v));   // (1100 OR 0000)=1100, shift-L 1 = 1000
    }

    [Fact] // §8.8.2 rule 7b (adversarial-review C2) — a shift with a LOWER inherited precedence binds to its LEFT
           // operand ONLY; a higher-precedence FOLLOWING operator applies to the shift's RESULT, not into its operand.
    public void Boolean_ShiftLowerPrecedenceThenHigherOp()
    {
        // (0110 B-OR 0000)=0110; shift-L1 → 1100; 1100 B-AND 1000 → 1000.
        // NOT shiftL1((0110 B-OR 0000) B-AND 1000) = shiftL1(0000) = 0000.
        var (v, diag) = EvalOperand("B\"0110\" B-OR B\"0000\" B-SHIFT-L 1 B-AND B\"1000\"");
        Assert.Empty(diag.Reports);
        Assert.Equal("1000", Bits(v));
    }

    [Fact] // §8.8.2 rule 8 (adversarial-review C2) — an astronomically large shift count must not overflow the
           // (long) cast / hang. A logical shift by ≫ the length is all boolean zeros.
    public void Boolean_HugeShiftCount_NoCrash()
    {
        var (v, diag) = EvalOperand("B\"1100\" B-SHIFT-L 99999999999999999999999999");
        Assert.Empty(diag.Reports);
        Assert.Equal("0000", Bits(v));
    }

    [Fact] // §8.8.2 rule 5 — the shift count shall be an integer operand.
    public void Boolean_FractionalShiftCount_Rejected()
    {
        var (v, diag) = EvalOperand("B\"1100\" B-SHIFT-L 1.5");
        Assert.Null(v);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule && x.Message.Contains("integer operand"));
    }

    [Fact] // §8.8.2 rule 8 — a negative shift count is rejected. The sign is WRITTEN AGAINST the digits, so `-1` is
           // one integer literal (§8.3.3.3.2); `- 1` is a unary operator, the Table 4 formation error
           // BooleanExpressionFormationTests covers (kb/Work PB1413).
    public void Boolean_NegativeShiftCount_Rejected()
    {
        var (v, diag) = EvalOperand("B\"1100\" B-SHIFT-L -1");
        Assert.Null(v);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule && x.Message.Contains("negative"));
    }

    [Fact] // §7.3.3 SR10 / §7.3.7.2 SR1 — a figurative constant is not a compile-time boolean operand.
    public void Boolean_FigurativeOperand_Rejected()
    {
        var (v, diag) = EvalOperand("B\"1\" B-AND ZERO");
        Assert.Null(v);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule && x.Message.Contains("SR10"));
    }

    [Fact] // §7.3.7.2 SR1 — an undefined name is not a valid boolean operand (no literal to substitute).
    public void Boolean_UndefinedName_Rejected()
    {
        var (v, diag) = EvalOperand("FLAG B-AND B\"1\"");
        Assert.Null(v);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule);
    }

    [Fact] // A previously-defined boolean compilation variable substitutes its value (§7.3.7 substitution).
    public void Boolean_DefinedBooleanName_Substitutes()
    {
        var names = new Dictionary<string, CtValue> { ["FLAG"] = CtValue.Boolean(BitString.Of("1100")) };
        var (v, diag) = EvalOperand("FLAG B-AND B\"0101\"", names);
        Assert.Empty(diag.Reports);
        Assert.Equal("0100", Bits(v));
    }

    // ── §7.3.3 SR10 on non-boolean operands ───────────────────────────────────────────────────────────────────

    [Fact] // §7.3.3 SR10 — a floating-point literal shall not appear in a directive (unlike a CONSTANT data entry).
    public void Operand_SoleFloatLiteral_Rejected()
    {
        var (v, diag) = EvalOperand("1.5E3");
        Assert.Null(v);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule && x.Message.Contains("floating-point"));
    }

    [Fact] // §7.3.3 SR10 — a figurative constant (arithmetic context) is barred.
    public void Operand_FigurativeZero_Rejected()
    {
        var (v, diag) = EvalOperand("ZERO");
        Assert.Null(v);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule && x.Message.Contains("figurative"));
    }

    [Theory] // literal operands of each category, and the §7.3.11.4 GR5 single-literal (fraction kept, not truncated).
    [InlineData("42", CtCategory.Numeric)]
    [InlineData("0.25", CtCategory.Numeric)]
    [InlineData("2 + 3 * 4", CtCategory.Numeric)]
    [InlineData("\"type A\"", CtCategory.Alphanumeric)]
    [InlineData("B\"1010\"", CtCategory.Boolean)]
    public void Operand_LiteralCategories(string src, CtCategory expected)
    {
        var (v, diag) = EvalOperand(src);
        Assert.Empty(diag.Reports);
        Assert.NotNull(v);
        Assert.Equal(expected, v!.Category);
    }

    // ── constant-conditional-expression (§7.3.8) ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1 + 1 = 2", true)]                       // numeric relation by value
    [InlineData("7 / 2 = 3", true)]                       // GR3 integer truncation of the arithmetic subject
    [InlineData("\"ABC\" = \"ABC\"", true)]               // alphanumeric equal (§7.3.8.3 GR2)
    [InlineData("\"ABC\" = \"AB\"", false)]               // unequal length ⇒ not equal (GR2, NOT space-extended)
    [InlineData("\"abc\" = \"ABC\"", false)]              // case-sensitive binary compare
    [InlineData("B\"1\" = B\"1\"", true)]                 // boolean relation
    [InlineData("B\"1\" = B\"0\"", false)]
    [InlineData("1 = 1 AND 2 = 2", true)]                 // AND
    [InlineData("1 = 2 OR 3 = 3", true)]                  // OR
    [InlineData("NOT 1 = 2", true)]                       // NOT
    [InlineData("(1 = 1)", true)]                         // grouping
    [InlineData("1 IS NOT = 2", true)]                    // IS NOT relop
    [InlineData("B\"1\"", true)]                          // §8.8.4.3 bare simple boolean condition (length 1)
    [InlineData("B\"0\"", false)]
    public void Cce_TrueFalse(string src, bool expected)
    {
        var (r, diag) = EvalCce(src);
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, r);
    }

    [Fact] // §8.8.4.2.8 (adversarial-review C2) — a boolean cce relation RIGHT-zero-extends the shorter operand
           // (NOT length-sensitive; §7.3.8.3 GR2's unequal-length⇒unequal is for non-numeric-non-boolean operands).
    public void Cce_BooleanRelation_RightExtends()
    {
        Assert.True(EvalCce("B\"1\" = B\"10\"").Result);      // "1" → "10" == "10"
        Assert.False(EvalCce("B\"1\" = B\"11\"").Result);     // "1" → "10" != "11"
        Assert.False(EvalCce("B\"1\" <> B\"10\"").Result);    // symmetric
    }

    [Fact] // §7.3.8.4.4 defined-condition.
    public void Cce_DefinedCondition()
    {
        var names = new Dictionary<string, CtValue> { ["A"] = CtValue.Numeric(CtNumeric.FromDecimal(1m), "1") };
        Assert.True(EvalCce("A IS DEFINED", names).Result);
        Assert.False(EvalCce("A IS NOT DEFINED", names).Result);
        Assert.False(EvalCce("B IS DEFINED", names).Result);
        Assert.True(EvalCce("B IS NOT DEFINED", names).Result);
    }

    [Fact] // a compilation-variable name substitutes its value in a cce relation (SpecFixTests CC2/CC3 shape).
    public void Cce_NameSubstitution()
    {
        var names = new Dictionary<string, CtValue>
        {
            ["LVL"] = CtValue.Numeric(CtNumeric.FromDecimal(14m), "14"),
            ["SYS"] = CtValue.Alphanumeric("type A"),
        };
        Assert.True(EvalCce("LVL > 10 AND LVL < 20", names).Result);
        Assert.True(EvalCce("(LVL = 1 AND SYS = \"type A\") OR (LVL = 14 AND SYS = \"type A\")", names).Result);
        Assert.False(EvalCce("SYS = \"type Q\"", names).Result);
    }

    [Fact] // §7.3.8.2 SR1a.1 — the operands shall be of the same category.
    public void Cce_CategoryMismatch_Rejected()
    {
        var (r, diag) = EvalCce("1 = \"A\"");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule && x.Message.Contains("category"));
    }

    [Fact] // §7.3.8.2 SR1a.2 — a non-numeric relation admits only equal / not equal.
    public void Cce_NonNumericOrdering_Rejected()
    {
        var (r, diag) = EvalCce("\"ABC\" < \"ABD\"");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule && x.Message.Contains("EQUAL"));
    }

    [Fact] // §7.3.3 SR10 — a figurative constant is not a valid cce operand.
    public void Cce_FigurativeOperand_Rejected()
    {
        var (r, diag) = EvalCce("SPACE = \"A\"");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule && x.Message.Contains("SR10"));
    }

    [Fact] // §8.8.4.3 SR1 — a simple boolean condition shall be of length 1.
    public void Cce_BareBooleanLengthTwo_Rejected()
    {
        var (r, diag) = EvalCce("B\"11\"");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule && x.Message.Contains("length 1"));
    }

    [Fact] // §8.8.4.13 — a formation error is reported even in a branch a value short-circuit would skip.
    public void Cce_FormationErrorInShortCircuitedBranch_StillReported()
    {
        // The left OR operand is TRUE, so a value short-circuit need not evaluate the right; but the right's
        // category-mismatch formation error is still reported (and the whole cce becomes a formation error).
        var (r, diag) = EvalCce("1 = 1 OR 2 = \"A\"");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.DirectiveRule && x.Message.Contains("category"));
    }
}
