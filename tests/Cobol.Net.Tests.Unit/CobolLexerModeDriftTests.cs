// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Pins WHICH PARENTHESIS TYPE each '(' gets, and what the lexer's LIST REGIONS do inside it, for the shapes where the
/// answer is load-bearing.
/// <para>The decision is frozen at lex time and cannot be repaired later: a '(' after a word that can name a data item
/// is a REFERENCE paren (<c>REF_LPAREN</c> … <c>REF_RPAREN</c>) whose content the parser reads as a subscript list, a
/// reference modifier or a keyword-omitted argument list; a '(' after <c>FUNCTION name</c> is the argument-list paren
/// (<c>FNARG_LPAREN</c>); anything else is a grouping <c>LPAREN</c>. Since kb/Work PB2113 (owner ruling D10) no paren
/// changes the lexer MODE: the tokens inside every one are the ordinary vocabulary, and the two list regions (argument
/// and reference) differ from a group only in keeping the §8.3.5 separator and the sign-adjacent literal.</para>
/// <para>⛔ WHY THIS TEST EXISTS. The PB8 fix-queue entry recorded the root cause of "reference-modifying a function
/// result is a parse error" as a LEXER defect, and called a lexer change "the riskiest category in this codebase". A
/// token dump showed that was wrong: in BOTH keyword-present shapes the ref-mod paren was never a reference paren, so
/// the whole fix was a grammar tail plus a binder rule. That conclusion is only as durable as the lexer behaviour it
/// rests on — hence this test rather than a sentence in a commit message. If someone widens <c>OnDefaultLParen</c>'s
/// reference trigger, the <c>refModPart</c> tail on <c>functionCall</c> sees a REF_LPAREN where its function's own
/// argument rules expect none, and ref-modified function results break. This fails first, and says why.</para>
/// </summary>
public sealed class CobolLexerModeDriftTests
{
    private static string[] TokenNames(string src)
    {
        var lexer = new CobolLexer(new AntlrInputStream(src));
        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        return [.. stream.GetTokens()
            .Where(t => t.Type != TokenConstants.EOF && t.Type != CobolLexer.WS)
            .Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t.Type) ?? t.Type.ToString())];
    }

    /// <summary>The paren that opens the group the (first) COLON sits in — the reference modifier's own paren.</summary>
    private static string OpenerOfColonGroup(string[] names)
    {
        int depth = 0;
        for (int i = Array.IndexOf(names, "COLON") - 1; i >= 0; i--)
        {
            if (names[i] is "RPAREN" or "FNARG_RPAREN" or "REF_RPAREN") depth++;
            else if (names[i] is "LPAREN" or "FNARG_LPAREN" or "REF_LPAREN" && depth-- == 0) return names[i];
        }
        return "";
    }

    /// <summary>A ref-mod written after a FUNCTION NAME or after a function's argument-list ')' is no reference paren,
    /// so it reaches <c>functionCall</c>'s <c>refModPart</c> tail (ISO §8.4.3.3.3 SR2 — the PB8 shapes).</summary>
    [Theory]
    // After a zero-argument function's NAME: the lexer's FUNCTION suppression makes the '(' the argument-list twin.
    [InlineData("MOVE FUNCTION CURRENT-DATE (1:4) TO X", "FNARG_LPAREN")]
    // After the argument list's ')': the previous token is not a data-name, so the reference trigger never fires.
    [InlineData("MOVE FUNCTION UPPER-CASE(\"abc\") (1:2) TO T", "LPAREN")]
    // The same tail in the KEYWORD-OMITTED form — the argument group is a reference paren, the ref-mod tail is not.
    [InlineData("MOVE UPPER-CASE(\"abc\") (2:3) TO T", "LPAREN")]
    public void RefModOnAFunctionResult_OpensNoReferenceParen(string src, string opener)
    {
        string[] names = TokenNames(src);
        Assert.Equal(opener, OpenerOfColonGroup(names));
    }

    /// <summary>The counterpart that must NOT drift the other way: a ref-mod on a DATA REFERENCE, and the
    /// keyword-omitted zero-argument function whose ref-mod shares that shape, open with a REFERENCE paren — the
    /// parser's <c>refModPart</c> accepts REF_LPAREN and tells it from a subscript list by the colon.</summary>
    [Theory]
    [InlineData("MOVE WS-A (2:3) TO T")]
    [InlineData("MOVE CURRENT-DATE (1:8) TO T")]
    public void RefModOnADataName_OpensAReferenceParen(string src)
    {
        string[] names = TokenNames(src);
        Assert.Equal("REF_LPAREN", OpenerOfColonGroup(names));
        Assert.Contains("REF_RPAREN", names);
    }

    /// <summary>The RESERVED intrinsic names (§8.9 ∩ §8.11) written WITHOUT the FUNCTION keyword (fix-queue
    /// PB9): each carries <c>subscriptTrigger=true</c>, so with no FUNCTION token before it the lexer types the '('
    /// REF_LPAREN. That is why the <c>functionCall</c> alternative for them takes a <c>subscriptPart</c> and binds
    /// through the ONE D2 argument path (IntrinsicBinder.ArgumentsOf), instead of the <c>functionArgList</c> the
    /// FUNCTION-keyword form uses. If the trigger were ever dropped for one of these words, that alternative would
    /// silently stop matching.</summary>
    [Theory]
    [InlineData("COMPUTE N = SIGN(V)")]
    [InlineData("COMPUTE N = SUM(1 2 3)")]
    [InlineData("COMPUTE N = LENGTH(A)")]
    public void ReservedIntrinsicName_KeywordOmitted_OpensAReferenceParen(string src)
    {
        string[] names = TokenNames(src);
        Assert.Contains("REF_LPAREN", names);
        Assert.Contains("REF_RPAREN", names);
    }

    /// <summary>ISO §8.3.5 1)/2) INSIDE a reference paren (kb/Work PB2113): the space and the comma-plus-space separate
    /// subscripts, so the lexer keeps the §8.3.5 2) separator as FNARG_SEPARATOR and retypes a sign that follows a
    /// separator and touches its digits as the literal's own sign (§8.3.3.3.2) — `T (I -1)` is two subscripts — while
    /// a space-surrounded sign stays the operator (§8.7.1) — `T (I - 1)` is one. Outside every list region nothing
    /// changes: the same `I -1` after COMPUTE is the subtraction it always was.</summary>
    [Theory]
    [InlineData("MOVE T (I -1) TO X", "SIGNED_INTEGERLIT")]
    [InlineData("MOVE T (I -1.5) TO X", "SIGNED_DECIMALLIT")]
    [InlineData("MOVE T (I - 1) TO X", "MINUS")]
    [InlineData("MOVE T (I+1) TO X", "PLUS")]   // I-1 would be one word (a hyphenated data-name)
    [InlineData("MOVE T (I, J) TO X", "FNARG_SEPARATOR")]
    [InlineData("MOVE T (I; J) TO X", "FNARG_SEPARATOR")]
    [InlineData("COMPUTE X = I -1", "MINUS")]
    [InlineData("COMPUTE X = (I -1)", "MINUS")]
    public void ReferenceParen_IsAListRegion(string src, string expected)
    {
        string[] names = TokenNames(src);
        Assert.Contains(expected, names);
        if (expected is "MINUS" or "PLUS") Assert.DoesNotContain(names, n => n.StartsWith("SIGNED_", StringComparison.Ordinal));
    }

    /// <summary>The DECIMAL-POINT IS COMMA fixed-point literal in a list region (kb/Work PB2506): §8.3.3.3.2 2) puts the
    /// sign inside the literal and §8.3.5 2) makes a comma a separator only when a space follows it, so `-1,5`, `+1,5`
    /// and `-,5` are each ONE SIGNED_DECIMALLIT token — never SIGNED_INTEGERLIT(-1) plus a contiguous `,5` that the
    /// parser assembles into a second argument. Outside every list region the sign is cut off as the operator and the
    /// unsigned literal assembles in numericLiteralCore as it always has (no SIGNED_* token).</summary>
    [Theory]
    [InlineData("COMPUTE N = FUNCTION MIN(-1,5 2)", "FNARG_LPAREN SIGNED_DECIMALLIT INTEGERLIT FNARG_RPAREN")]
    [InlineData("COMPUTE N = FUNCTION MAX(+1,5)", "FNARG_LPAREN SIGNED_DECIMALLIT FNARG_RPAREN")]
    [InlineData("COMPUTE N = FUNCTION MIN(-,5 1)", "FNARG_LPAREN SIGNED_DECIMALLIT INTEGERLIT FNARG_RPAREN")]
    [InlineData("COMPUTE N = FUNCTION MIN(3, -1,5)", "FNARG_LPAREN INTEGERLIT FNARG_SEPARATOR SIGNED_DECIMALLIT FNARG_RPAREN")]
    [InlineData("COMPUTE N = 2 -1,5", "INTEGERLIT MINUS INTEGERLIT COMMA INTEGERLIT")]
    public void SignedCommaDecimal_IsOneTokenInAListRegionOnly(string src, string expectedTail)
    {
        string[] names = TokenNames(src);
        string tail = string.Join(' ', names.SkipWhile(n => n is not ("FNARG_LPAREN" or "INTEGERLIT")));
        Assert.Equal(expectedTail, tail);
    }

    /// <summary>A '(' after a ')' is never a reference paren, inside a list region or out — `MAX(T (1) (A + B))` is
    /// two arguments, and `T (1) (2:3)` a subscript list then a reference modifier written with a plain LPAREN. The
    /// parser's subscriptPart takes only REF_LPAREN, so this is what keeps a grouping paren from becoming a second
    /// subscript list.</summary>
    [Theory]
    [InlineData("COMPUTE N = FUNCTION MAX(T (1) (A + B))")]
    [InlineData("MOVE T (1) (2:3) TO X")]
    public void ParenAfterAClosingParen_IsAGroup(string src)
    {
        string[] names = TokenNames(src);
        int close = Array.IndexOf(names, "REF_RPAREN");
        Assert.Equal("LPAREN", names[close + 1]);
    }

    // ── The REPORT SECTION region: a bare SUM is the SUM CLAUSE, never the function (kb/Work PB924) ─────────────

    private const string ReportSectionHead = "DATA DIVISION. REPORT SECTION. RD R1. 01 TYPE DETAIL. 05 COLUMN 1 PIC 9999 ";

    /// <summary>ISO §13.18.54.3 SR9: within a report description entry a SUM not preceded by FUNCTION is the SUM
    /// CLAUSE, whose addend (arithmetic-expression-1) may open with '(' — so that '(' is an arithmetic LPAREN,
    /// never a reference paren.</summary>
    [Theory]
    [InlineData("SUM (WS-K * WS-M).")]
    [InlineData("SUM OF (WS-K * WS-M).")]
    [InlineData("SUM (WS-K) WS-M.")]
    public void ReportSumClause_ParenthesisedAddend_LexesInDefaultMode(string entry)
    {
        string[] names = TokenNames(ReportSectionHead + entry);
        Assert.Contains("LPAREN", names);
        Assert.DoesNotContain("REF_LPAREN", names);
    }

    /// <summary>The OTHER arm of SR9 and of the region, which a region-blind "turn the trigger off" fix breaks:
    /// FUNCTION SUM( inside a report description entry is still the function's argument list (FNARG_LPAREN),
    /// and every §8.9 ∩ §8.11 word (LENGTH, RANDOM, SIGN, SUM) written keyword-omitted in the PROCEDURE DIVISION
    /// — after the REPORT SECTION has closed — still opens a reference paren for its arguments.</summary>
    [Theory]
    [InlineData(ReportSectionHead + "SOURCE FUNCTION SUM(1 2 3).", "FNARG_LPAREN")]
    [InlineData(ReportSectionHead + "SUM WS-K. PROCEDURE DIVISION. COMPUTE N = SUM(1 2 3).", "REF_LPAREN")]
    [InlineData(ReportSectionHead + "SUM WS-K. PROCEDURE DIVISION. COMPUTE N = SIGN(V).", "REF_LPAREN")]
    [InlineData(ReportSectionHead + "SUM WS-K. PROCEDURE DIVISION. COMPUTE N = LENGTH(A).", "REF_LPAREN")]
    [InlineData(ReportSectionHead + "SUM WS-K. PROCEDURE DIVISION. COMPUTE N = RANDOM(1).", "REF_LPAREN")]
    [InlineData(ReportSectionHead + "SUM WS-K. SCREEN SECTION. PROCEDURE DIVISION. COMPUTE N = SUM(1 2).", "REF_LPAREN")]
    public void ReportSumRegion_LeavesTheFunctionArmsIntact(string src, string paren)
    {
        string[] names = TokenNames(src);
        Assert.Contains(paren, names);
        if (paren == "FNARG_LPAREN") Assert.DoesNotContain("REF_LPAREN", names);
    }

    // ── The FUNCTION argument-list parenthesis, and the figurative ZERO it used to destroy (PB48) ────────────

    /// <summary>Token names AFTER <see cref="ZeroTokenRewriter"/> — the stream the PARSER actually sees. The
    /// plain <see cref="TokenNames"/> above cannot answer a ZERO-vs-ZERO_ARITH question at all, because the
    /// rewrite is a separate post-lex pass; asking it there is how a test can look right and prove nothing.</summary>
    private static string[] RewrittenTokenNames(string src)
    {
        var lexer = new CobolLexer(new AntlrInputStream(src));
        var stream = new CommonTokenStream(lexer);
        ZeroTokenRewriter.Rewrite(stream);
        stream.Fill();
        return [.. stream.GetTokens()
            .Where(t => t.Type != TokenConstants.EOF && t.Type != CobolLexer.WS)
            .Select(t => CobolParserCore.DefaultVocabulary.GetSymbolicName(t.Type) ?? t.Type.ToString())];
    }

    /// <summary>
    /// A '(' that immediately follows <c>FUNCTION &lt;name&gt;</c> is FNARG_LPAREN, and its ')' FNARG_RPAREN
    /// (ISO §8.4.3.2.3 SR6 — that paren is ALWAYS the argument list, never a grouping paren).
    /// </summary>
    /// <remarks>
    /// ⛔ THIS IS THE WHOLE OF PB48's MECHANISM. <c>ZeroTokenRewriter</c> converts a figurative ZERO adjacent to
    /// '(' or ')' into the arithmetic ZERO_ARITH, and an argument list is delimited by exactly those characters —
    /// so before the split, every bare ZERO argument reached the binder as class NUMERIC and
    /// <c>FUNCTION LOWER-CASE(ZERO)</c> was rejected as illegal. If someone collapses these back to
    /// LPAREN/RPAREN the rewriter silently resumes eating the figurative, the corpus golden fails on VALUES, and
    /// nothing here would have said why — hence a test at the token level, where the cause is.
    /// </remarks>
    [Theory]
    [InlineData("MOVE FUNCTION LOWER-CASE(ZERO) TO X")]
    [InlineData("COMPUTE N = FUNCTION MAX(ZERO 5)")]
    [InlineData("MOVE FUNCTION UPPER-CASE(\"abc\") TO X")]
    public void FunctionArgumentListParens_HaveTheirOwnTokenTypes(string src)
    {
        string[] names = RewrittenTokenNames(src);
        Assert.Contains("FNARG_LPAREN", names);
        Assert.Contains("FNARG_RPAREN", names);
    }

    /// <summary>A bare ZERO argument keeps its FIGURATIVE token, so the binder — which alone knows the
    /// function's §15.3 argument type — decides between §8.3.3.6.4 GR4's numeric and character readings.</summary>
    [Theory]
    [InlineData("MOVE FUNCTION LOWER-CASE(ZERO) TO X")]
    [InlineData("COMPUTE N = FUNCTION MAX(ZERO 5)")]      // numeric context, still figurative AT THE TOKEN
    [InlineData("COMPUTE N = FUNCTION MAX(5 ZERO)")]      // last argument: the ')' side of the same question
    public void BareZeroArgument_StaysFigurative(string src)
    {
        string[] names = RewrittenTokenNames(src);
        Assert.Contains("ZERO", names);
        Assert.DoesNotContain("ZERO_ARITH", names);
    }

    /// <summary>The converse, and the reason the paren TYPE rather than the rewriter is what changed: a
    /// GROUPING paren is still an ordinary LPAREN wherever it appears, including inside an argument list, so
    /// arithmetic ZERO keeps being rewritten exactly as before.</summary>
    [Theory]
    [InlineData("COMPUTE N = (ZERO)")]
    [InlineData("COMPUTE N = (ZERO + 3) * 2")]
    [InlineData("COMPUTE N = FUNCTION ABS((ZERO))")]
    [InlineData("COMPUTE N = FUNCTION ABS(ZERO + 1)")]
    [InlineData("COMPUTE N = FUNCTION MAX((ZERO + 2) 1)")]
    public void ArithmeticZero_IsStillRewritten(string src)
    {
        string[] names = RewrittenTokenNames(src);
        Assert.Contains("ZERO_ARITH", names);
        Assert.DoesNotContain("ZERO", names);
    }

    /// <summary>A reference modifier written directly after a ZERO-ARGUMENT function name is delimited by
    /// FNARG parens too — the lexer cannot know the arity, which is why <c>refModPart</c> accepts both flavours
    /// — so a ZERO in one of its positions has no arithmetic paren beside it. §8.4.3.3.3 SR4 makes both
    /// positions arithmetic expressions, and the rewriter's COLON arms are what keep that true.</summary>
    [Theory]
    [InlineData("MOVE FUNCTION CURRENT-DATE (ZERO:4) TO X")]
    [InlineData("MOVE FUNCTION CURRENT-DATE (2:ZERO) TO X")]
    public void RefModPositionAfterAFunctionName_RewritesZeroThroughTheColon(string src)
    {
        string[] names = RewrittenTokenNames(src);
        Assert.Contains("FNARG_LPAREN", names);
        Assert.Contains("ZERO_ARITH", names);
        Assert.DoesNotContain("ZERO", names);
    }

    private static string[] PrimedTokenNames(string src, int year)
    {
        var lexer = new CobolLexer(new AntlrInputStream(src));
        TokenRetypes.None.PrimeLexer(lexer, CobolNet.Editions.EditionInfo.Of(year));
        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        return [.. stream.GetTokens()
            .Where(t => t.Type != TokenConstants.EOF && t.Type != CobolLexer.WS)
            .Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t.Type) ?? t.Type.ToString())];
    }

    /// <summary>kb/Work PB1465 — the reference-paren trigger is narrowed PER COMPILE to the words the compile admits as
    /// user-defined words (ISO §8.3.2.1 GR1: "Reserved words shall not be used as user-defined words"). A '(' after
    /// a boolean operator §8.9 reserves (2002+) groups a boolean sub-expression, which §8.8.2 Table 4 permits; at
    /// COBOL-85, where B-OR is a user word, the same '(' is still a subscript. A data-name and an intrinsic-function
    /// name (a keyword-omitted call, §8.4.3.2.3 SR2 — SUM is reserved at every edition) keep their reference paren.</summary>
    [Theory]
    [InlineData("IF BZ B-OR (BW B-AND BW)", 2023, false)]
    [InlineData("IF B-NOT (BW B-XOR BZ)", 2002, false)]
    [InlineData("COMPUTE BR = BZ B-XOR (BW)", 2014, false)]
    [InlineData("IF BZ B-OR (BW B-AND BW)", 85, true)]
    [InlineData("IF BZ B-OR BT (2)", 2023, true)]
    [InlineData("COMPUTE X = SUM (1 2 3)", 2023, true)]
    public void ParenAfterAWord_OpensSubscriptOnlyWhereTheWordCanBeAName(string src, int year, bool subscript)
    {
        string[] names = PrimedTokenNames(src, year);
        Assert.Equal(subscript, names.Contains("REF_LPAREN"));
    }
}
