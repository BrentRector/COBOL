// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Pins WHICH LEXER MODE each parenthesised group lands in, for the shapes where the answer is load-bearing.
/// <para>The lexer's own comment states the stake: "the SUBSCRIPT-mode decision at '(' is frozen at lex time and
/// cannot be repaired later." A '(' either pushes SUBSCRIPT mode — where the content is captured as an
/// undifferentiated <c>SUB_*</c> token stream a binder mini-parser interprets — or stays in DEFAULT mode, where
/// it parses through the real expression grammar. Nothing downstream can change that choice.</para>
/// <para>⛔ WHY THIS TEST EXISTS. The PB8 fix-queue entry recorded the root cause of "reference-modifying a
/// function result is a parse error" as a LEXER-MODE defect, and called a lexer change "the riskiest category in
/// this codebase". A token dump showed that was wrong: in BOTH keyword-present shapes the ref-mod paren is
/// already lexed in DEFAULT mode, so the whole fix was a grammar tail plus a binder rule and the lexer was never
/// touched. That conclusion is only as durable as the lexer behaviour it rests on — hence this test rather than
/// a sentence in a commit message. If someone widens <c>OnDefaultLParen</c>'s SUBSCRIPT trigger, the
/// <c>refModPart</c> tail on <c>functionCall</c> stops matching (it needs <c>COLON</c>, and SUBSCRIPT mode emits
/// <c>SUB_COLON</c>) and ref-modified function results break. This fails first, and says why.</para>
/// </summary>
public sealed class CobolLexerModeDriftTests
{
    private static string[] TokenNames(string src)
    {
        var lexer = new CobolLexer(new AntlrInputStream(src));
        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        return [.. stream.GetTokens()
            .Where(t => t.Type != TokenConstants.EOF && t.Type != CobolLexer.WS && t.Type != CobolLexer.SUB_WS)
            .Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t.Type) ?? t.Type.ToString())];
    }

    /// <summary>A ref-mod written after a FUNCTION NAME or after a function's argument-list ')' stays in DEFAULT
    /// mode, so it reaches the parsed <c>refModPart</c> (ISO §8.4.3.3.3 SR2 — the PB8 shapes).</summary>
    [Theory]
    // After a zero-argument function's NAME: the lexer's FUNCTION suppression keeps the '(' out of SUBSCRIPT mode.
    [InlineData("MOVE FUNCTION CURRENT-DATE (1:4) TO X")]
    // After the argument list's ')': the previous token is not a data-name, so the SUBSCRIPT trigger never fires.
    [InlineData("MOVE FUNCTION UPPER-CASE(\"abc\") (1:2) TO T")]
    // The same tail in the KEYWORD-OMITTED form — the argument group lexes SUBSCRIPT, the ref-mod tail does not.
    [InlineData("MOVE UPPER-CASE(\"abc\") (2:3) TO T")]
    public void RefModOnAFunctionResult_LexesInDefaultMode(string src)
    {
        string[] names = TokenNames(src);
        Assert.Contains("COLON", names);
        Assert.DoesNotContain("SUB_COLON", names);
    }

    /// <summary>The counterpart that must NOT drift the other way: a ref-mod on a DATA REFERENCE, and the
    /// keyword-omitted zero-argument function whose ref-mod shares that shape, are SUBSCRIPT-mode captures. The
    /// binder tells the two apart by <c>ReferenceResolver.HasDepth0Colon</c>, so the capture must stay.</summary>
    [Theory]
    [InlineData("MOVE WS-A (2:3) TO T")]
    [InlineData("MOVE CURRENT-DATE (1:8) TO T")]
    public void RefModOnADataName_LexesInSubscriptMode(string src)
    {
        string[] names = TokenNames(src);
        Assert.Contains("SUB_COLON", names);
        Assert.DoesNotContain("COLON", names);
    }

    /// <summary>The RESERVED intrinsic names (§8.9 ∩ §8.11) written WITHOUT the FUNCTION keyword (fix-queue
    /// PB9): each carries <c>subscriptTrigger=true</c>, so with no FUNCTION token before it the lexer pushes
    /// SUBSCRIPT mode at the '(' and the arguments arrive as <c>SUB_*</c> tokens. That is why the
    /// <c>functionCall</c> alternative for them takes a <c>subscriptPart</c> and re-parses through the ONE D2
    /// argument path, instead of the DEFAULT-mode <c>functionArgList</c> the FUNCTION-keyword form uses. If the
    /// trigger were ever dropped for one of these words, that alternative would silently stop matching.</summary>
    [Theory]
    [InlineData("COMPUTE N = SIGN(V)")]
    [InlineData("COMPUTE N = SUM(1 2 3)")]
    [InlineData("COMPUTE N = LENGTH(A)")]
    public void ReservedIntrinsicName_KeywordOmitted_CapturesArgumentsInSubscriptMode(string src)
    {
        string[] names = TokenNames(src);
        Assert.Contains("LPAREN", names);
        Assert.Contains(names, n => n.StartsWith("SUB_", StringComparison.Ordinal));
    }

    // ── The REPORT SECTION region: a bare SUM is the SUM CLAUSE, never the function (kb/Work PB924) ─────────────

    private const string ReportSectionHead = "DATA DIVISION. REPORT SECTION. RD R1. 01 TYPE DETAIL. 05 COLUMN 1 PIC 9999 ";

    /// <summary>ISO §13.18.54.3 SR9: within a report description entry a SUM not preceded by FUNCTION is the SUM
    /// CLAUSE, whose addend (arithmetic-expression-1) may open with '(' — so that '(' is an arithmetic LPAREN
    /// in DEFAULT mode and its content reaches the expression grammar, never a SUBSCRIPT capture.</summary>
    [Theory]
    [InlineData("SUM (WS-K * WS-M).")]
    [InlineData("SUM OF (WS-K * WS-M).")]
    [InlineData("SUM (WS-K) WS-M.")]
    public void ReportSumClause_ParenthesisedAddend_LexesInDefaultMode(string entry)
    {
        string[] names = TokenNames(ReportSectionHead + entry);
        Assert.Contains("LPAREN", names);
        Assert.DoesNotContain(names, n => n.StartsWith("SUB_", StringComparison.Ordinal));
    }

    /// <summary>The OTHER arm of SR9 and of the region, which a region-blind "turn the trigger off" fix breaks:
    /// FUNCTION SUM( inside a report description entry is still the function's argument list (FNARG_LPAREN),
    /// and every §8.9 ∩ §8.11 word (LENGTH, RANDOM, SIGN, SUM) written keyword-omitted in the PROCEDURE DIVISION
    /// — after the REPORT SECTION has closed — still captures its arguments in SUBSCRIPT mode.</summary>
    [Theory]
    [InlineData(ReportSectionHead + "SOURCE FUNCTION SUM(1 2 3).", "FNARG_LPAREN", null)]
    [InlineData(ReportSectionHead + "SUM WS-K. PROCEDURE DIVISION. COMPUTE N = SUM(1 2 3).", "LPAREN", "SUB_INTEGERLIT")]
    [InlineData(ReportSectionHead + "SUM WS-K. PROCEDURE DIVISION. COMPUTE N = SIGN(V).", "LPAREN", "SUB_")]
    [InlineData(ReportSectionHead + "SUM WS-K. PROCEDURE DIVISION. COMPUTE N = LENGTH(A).", "LPAREN", "SUB_")]
    [InlineData(ReportSectionHead + "SUM WS-K. PROCEDURE DIVISION. COMPUTE N = RANDOM(1).", "LPAREN", "SUB_")]
    [InlineData(ReportSectionHead + "SUM WS-K. SCREEN SECTION. PROCEDURE DIVISION. COMPUTE N = SUM(1 2).", "LPAREN", "SUB_")]
    public void ReportSumRegion_LeavesTheFunctionArmsIntact(string src, string paren, string? subPrefix)
    {
        string[] names = TokenNames(src);
        Assert.Contains(paren, names);
        if (subPrefix is null) Assert.DoesNotContain(names, n => n.StartsWith("SUB_", StringComparison.Ordinal));
        else Assert.Contains(names, n => n.StartsWith(subPrefix, StringComparison.Ordinal));
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
            .Where(t => t.Type != TokenConstants.EOF && t.Type != CobolLexer.WS && t.Type != CobolLexer.SUB_WS)
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
            .Where(t => t.Type != TokenConstants.EOF && t.Type != CobolLexer.WS && t.Type != CobolLexer.SUB_WS)
            .Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t.Type) ?? t.Type.ToString())];
    }

    /// <summary>kb/Work PB1465 — the SUBSCRIPT trigger is narrowed PER COMPILE to the words the compile admits as
    /// user-defined words (ISO §8.3.2.1 GR1: "Reserved words shall not be used as user-defined words"). A '(' after
    /// a boolean operator §8.9 reserves (2002+) groups a boolean sub-expression, which §8.8.2 Table 4 permits; at
    /// COBOL-85, where B-OR is a user word, the same '(' is still a subscript. A data-name and an intrinsic-function
    /// name (a keyword-omitted call, §8.4.3.2.3 SR2 — SUM is reserved at every edition) keep their capture.</summary>
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
        Assert.Equal(subscript, names.Any(n => n.StartsWith("SUB_", StringComparison.Ordinal)));
    }
}
