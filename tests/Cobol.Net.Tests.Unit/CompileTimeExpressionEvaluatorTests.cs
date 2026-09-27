// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// The shared <see cref="CompileTimeExpressionEvaluator"/> arithmetic core (ISO/IEC 1989:2023 §7.3.6): the
/// §7.3.11.4 GR5 single-literal reclassification (a lone numeric literal keeps its fractional value), the §7.3.6.3
/// GR3 integer truncation of an expression's final result, numeric-name substitution (§7.3.6.2 SR1b), and the
/// SR1a/SR1c rejections — driven through the evaluator with stub name-resolution + a collecting diagnostic sink.
/// </summary>
public sealed class CompileTimeExpressionEvaluatorTests
{
    internal sealed class CollectingDiag : ICtDiagnostics
    {
        public readonly List<(CtDiagCode Code, string Message)> Reports = [];
        public void Report(CtDiagCode code, string message) => Reports.Add((code, message));
    }

    private static readonly CtOperandVocabulary Vocab =
        new("previously defined numeric compilation variables", "ISO §7.3.6.2 SR1b");

    /// <summary>Evaluate <paramref name="text"/> as an arithmetic operand at <paramref name="edition"/>;
    /// <paramref name="names"/> supplies any numeric constant-name values (as their literal texts).</summary>
    internal static (CompileTimeExpressionEvaluator.CtNumber? Result, CollectingDiag Diag) Eval(
        string text, Dictionary<string, string>? names = null, int edition = 2023)
    {
        var flag = new ErrorFlag();
        var lexer = new CobolLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(flag);
        var tokens = new CommonTokenStream(lexer);
        ZeroTokenRewriter.Rewrite(tokens);
        var parser = new Core(tokens) { Edition = EditionInfo.Of(edition) };
        parser.RemoveErrorListeners();
        parser.AddErrorListener(flag);
        Core.ArithmeticExpressionContext ctx = parser.arithmeticExpression();
        Assert.False(flag.HasError, $"parse error in arithmetic fragment '{text}'");
        Assert.Equal(TokenConstants.EOF, parser.CurrentToken.Type);

        var diag = new CollectingDiag();
        var ev = new CompileTimeExpressionEvaluator(
            edition: EditionInfo.Of(edition),
            resolveName: w => names is not null && names.TryGetValue(w, out var t) && CtNumeric.TryParseLiteral(t, out var v)
                ? CtValue.Numeric(v, t) : null,
            diag: diag, vocab: Vocab, decimalPointIsComma: false);
        return (ev.EvaluateArithmeticOperand(ctx, "test"), diag);
    }

    /// <summary>Assert a carrier value equals <paramref name="expected"/> BY VALUE (the carrier's spelling of a
    /// value — 2 vs 20E-1 — is not part of the contract).</summary>
    internal static void AssertValue(decimal expected, CobolDec actual) =>
        Assert.True(CobolDec.Compare(CtNumeric.FromDecimal(expected), actual) == 0,
            $"expected {expected}, got {CtNumeric.ToScientificText(actual)}");

    [Theory]
    // Expression forms: §8.8.1 precedence, then §7.3.6.3 GR3 integer truncation of the final result.
    [InlineData("2 + 3 * 4", "14", false)]
    [InlineData("(2 + 3) * 4 - 6 / 2", "17", false)]
    [InlineData("7 / 2", "3", false)]              // 3.5 → truncated to 3 (GR3)
    [InlineData("- (3 + 4)", "-7", false)]
    // §7.3.11.4 GR5 / §13.10.3 SR1 — a single numeric literal is a literal, NOT truncated.
    [InlineData("0.25", "0.25", true)]
    [InlineData("-5", "-5", true)]
    [InlineData("42", "42", true)]
    // kb/Work PB1230 — the literal is carried AS WRITTEN, its '+' included (§13.10.4 GR1 "as if literal-1 … were
    // written"): `+5` and `5` are one value but two literals, and a constant substitutes the one it was given.
    [InlineData("+5", "+5", true)]
    [InlineData("+0.25", "+0.25", true)]
    public void Evaluates_ArithmeticAndReclassification(string src, string expectedText, bool wasSingleLiteral)
    {
        var (r, diag) = Eval(src);
        Assert.Empty(diag.Reports);
        Assert.NotNull(r);
        Assert.Equal(expectedText, r!.Value.Literal);
        Assert.Equal(wasSingleLiteral, r.Value.WasSingleLiteral);
    }

    [Fact] // §7.3.11.4 GR5 — a SINGLE floating-point (E-form) literal is a valid literal constant (a literal keeps
           // its own class; only an arithmetic-EXPRESSION operand is bound to fixed-point by §7.3.6.2 SR1b).
    public void Accepts_SoleFloatingPointLiteral()
    {
        var (r, diag) = Eval("1.5E3");
        Assert.Empty(diag.Reports);
        Assert.NotNull(r);
        Assert.True(r!.Value.WasSingleLiteral);
        Assert.Equal("1.5E3", r.Value.Literal);
        AssertValue(1500m, r.Value.Value);
    }

    /// <summary>§8.8.1.2 Table 3, row "Unary + or −" × column "Unary + or −" = '—' (kb/Work PB158). This is the
    /// walker the note's two-arm framing MISSES: <c>EvaluateArithmeticOperand</c> reaches
    /// <c>SoleNumericLiteral</c>, which toggles the sign through a stacked unary chain and used to reclassify
    /// <c>- - 2</c> as the literal 2 — a value for an expression the standard does not admit. The screen runs
    /// BEFORE that probe, which is why the order in the method body is load-bearing.</summary>
    [Fact]
    public void Rejects_StackedUnarySigns_PinnedToSpec()
    {
        var (r, diag) = Eval("- - 2");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule
                                           && x.Message.Contains("Table 3", StringComparison.Ordinal));
    }

    /// <summary>The over-rejection guard, and the reason the rule could not be a grammar tier: §8.3.3.3.2 rule 2
    /// makes a numeric literal "a character-string" whose sign is "the leftmost character of the literal", so a
    /// sign written AGAINST the digits is part of the literal and <c>- -2</c> is Table 3's PERMISSIBLE
    /// (unary, literal) pair. Its token stream is identical to <c>- - 2</c>'s in the default lexer mode, so only
    /// the token POSITIONS separate them — a test that pinned the reject alone would pass for a screen that
    /// rejected legal source too.</summary>
    [Fact]
    public void Accepts_UnaryThenAdjacentSignedLiteral_PinnedToSpec()
    {
        var (r, diag) = Eval("- -2");
        Assert.Empty(diag.Reports);
        Assert.NotNull(r);
        AssertValue(2m, r!.Value.Value);
    }

    /// <summary>The other permissible neighbours of a unary sign, so the screen is pinned against firing on
    /// Table 3's 'P' cells: row "+ − * / **" × column "Unary" = P (a binary operator may be followed by a unary
    /// sign) and row "(" × column "Unary" = P.</summary>
    [Theory]
    [InlineData("5 - - 3", "8")]
    [InlineData("5 * - 3", "-15")]
    [InlineData("- (3 + 4)", "-7")]
    public void Accepts_PermissibleUnaryNeighbours_PinnedToSpec(string src, string expected)
    {
        var (r, diag) = Eval(src);
        Assert.Empty(diag.Reports);
        Assert.NotNull(r);
        Assert.Equal(expected, r!.Value.Literal);
    }

    /// <summary>§8.3.3.3.2 — a sole fixed-point literal past the edition's digit capacity (31) is rejected LOUDLY,
    /// never a silent null. It is a LITERAL (§7.3.11.4 GR5), so the limit that refuses it is the literal's own, not
    /// the §7.3.6.2 SR2 intermediate-result range an arithmetic mode imposes (kb/Work PB1592).</summary>
    [Fact]
    public void Rejects_OverCapacitySoleLiteral()
    {
        var (r, diag) = Eval("123456789012345678901234567890123456");   // 36 digit positions
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule
                                           && x.Message.Contains("§8.3.3.3.2", StringComparison.Ordinal)
                                           && x.Message.Contains("36 digit positions", StringComparison.Ordinal));
    }

    /// <summary>kb/Work PB1230 — <see cref="CompileTimeExpressionEvaluator.CtNumber.IsInteger"/> is the §13.10.3 SR2
    /// "constant-name-1 is an integer" answer the integer positions (OCCURS bound, PICTURE repetition, subscript) read:
    /// an integer literal whatever its sign, never a literal with a decimal point or an exponent, and every §7.3.6.3
    /// GR3 expression result. The value, not the written text, is what an integer position consumes.</summary>
    [Theory]
    [InlineData("+5", true, 5)]
    [InlineData("-5", true, -5)]
    [InlineData("7 / 2", true, 3)]
    [InlineData("5.0", false, 5)]
    [InlineData("1.5E2", false, 150)]
    public void IsInteger_FollowsTheLiteralsForm_NotItsValue(string src, bool isInteger, int value)
    {
        var (r, diag) = Eval(src);
        Assert.Empty(diag.Reports);
        Assert.NotNull(r);
        Assert.Equal(isInteger, r!.Value.IsInteger);
        AssertValue(value, r.Value.Value);
    }

    [Fact] // A previously-defined numeric constant-name substitutes its value (§7.3.6.2 SR1b / §13.10.3 SR2).
    public void Substitutes_NumericName()
    {
        var (r, diag) = Eval("K * 2 + 1", new() { ["K"] = "5" });
        Assert.Empty(diag.Reports);
        Assert.Equal("11", r!.Value.Literal);
    }

    [Fact] // §7.3.6.2 SR1c — division by zero is rejected.
    public void Rejects_DivisionByZero()
    {
        var (r, diag) = Eval("5 / 0");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule && x.Message.Contains("SR1c"));
    }

    [Fact] // §7.3.6.2 SR1a — the exponentiation operator is rejected.
    public void Rejects_Exponentiation()
    {
        var (r, diag) = Eval("2 ** 3");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule && x.Message.Contains("SR1a"));
    }

    [Fact] // §7.3.6.2 SR1b — an undefined / non-numeric name is not a valid operand.
    public void Rejects_UndefinedName()
    {
        var (r, diag) = Eval("UNDEF + 1");
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule && x.Message.Contains("SR1b"));
    }

    private sealed class ErrorFlag : BaseErrorListener, IAntlrErrorListener<int>
    {
        public bool HasError { get; private set; }
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;
        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;
    }
}
