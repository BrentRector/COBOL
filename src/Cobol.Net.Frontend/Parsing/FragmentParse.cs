// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⭐ THE ONE FRAGMENT RE-PARSE. Several places isolate a run of SOURCE TEXT and re-parse it through an isolated
/// fragment entry rule rather than growing a second expression compiler: the D18 subscript / reference-modifier
/// segment (<see cref="SubscriptExpressionFragment"/>) and the compile-time directive expression
/// (<see cref="DirectiveExpressionFragment"/>). (The D2 keyword-omitted argument list was the third until kb/Work
/// PB2113 made it a parse node of the main tree.) Each needs the SAME five steps — lex the text, prime the lexer
/// for its region, normalize the token stream, parse at an edition, and return null on any syntax error — and
/// each had its own copy.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ THE COPIES DISAGREED, AND BOTH DISAGREEMENTS WERE DEFECTS (fix-queue PB50 + PB54). Exactly one of the
/// three applied <see cref="ZeroTokenRewriter"/>, which is what turns a figurative <c>ZERO</c> adjacent to an
/// arithmetic operator into the <c>ZERO_ARITH</c> token <c>arithmeticExpression</c> can match (§8.8.1.1 admits
/// "the figurative constant ZERO" as an arithmetic operand). The two that did not:
/// </para>
/// <list type="bullet">
/// <item><b>Subscript / ref-mod</b> — <c>E(ZERO + 1)</c> could not parse, so the D18 route returned null and the
/// reference ABORTED AT RUN TIME on legal source (PB50). The queue entry blamed a missing arm in
/// <c>RenderSegment</c>'s token switch; PB42 had already widened that switch's <c>default:</c> to route
/// everything unrenderable here, so the arm was not the cause — this omission was.</item>
/// <item><b>Keyword-omitted arguments</b> — <c>MIN(ZERO + 5, 2)</c> returned <b>0</b> while
/// <c>FUNCTION MIN(ZERO + 5, 2)</c> returned 2, because the un-rewritten <c>ZERO</c> ended one argument and
/// <c>+ 5</c> began another. A SILENT WRONG ANSWER, and §8.4.3.2 SR2 makes the two spellings the same reference
/// (PB54).</item>
/// </list>
/// <para>
/// ⚠ <paramref name="rewriteZero"/> IS AN EXPLICIT ARGUMENT, NOT A DEFAULT, so a new fragment has to answer the
/// question rather than inherit an answer by omission — which is exactly how the two defects above arose. It is
/// safe for an argument-list fragment despite §8.4.3.2.3 SR6: the rewriter keys on adjacency to an arithmetic
/// operator or a PLAIN paren, and a fragment's text is the content BETWEEN its delimiters, so a bare <c>ZERO</c>
/// argument has no adjacent operator and keeps the figurative identity §8.3.3.6.4 GR4 requires the BINDER to
/// resolve by the function's §15.3 argument type (the PB48 rule).
/// </para>
/// </remarks>
public static class FragmentParse
{
    /// <summary>Re-parse <paramref name="text"/> through <paramref name="rule"/>, or null on any syntax error
    /// from either recognizer — a partial parse is never returned, so every caller keeps its own loud posture.</summary>
    /// <param name="prime">The lexer-region prime for this fragment (<c>PrimeDirectiveExpr</c>, or null for the
    /// DEFAULT mode, where no list region is open and a sign before digits is the arithmetic operator).</param>
    /// <param name="rewriteZero">Apply <see cref="ZeroTokenRewriter"/> to the token stream — required wherever
    /// the fragment's grammar can contain an ARITHMETIC EXPRESSION, since §8.8.1.1 admits the figurative ZERO as
    /// an operand and only the rewrite makes it matchable.</param>
    /// <param name="retypes">The post-lex token decisions of the tree this text came from
    /// (<c>CompilationUnitContext.TokenRetypes</c>) — so the fragment reads every word exactly as the tree does: a
    /// §8.9 word the program declared where the edition frees it (kb/Work PB655) and a <c>&gt;&gt;COBOL-WORDS</c>
    /// synonym or de-reserved word (ISO §7.3.10.4) are the same tokens here as there.</param>
    public static T? Parse<T>(string text, EditionInfo edition, TokenRetypes retypes, System.Action<CobolLexer>? prime,
        bool rewriteZero, System.Func<CobolParserCore, T> rule) where T : class
    {
        var flag = new SyntaxErrorFlag();
        var lexer = new CobolLexer(new AntlrInputStream(text));
        retypes.PrimeLexer(lexer, edition);
        prime?.Invoke(lexer);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(flag);
        var tokens = new CommonTokenStream(lexer);
        // A debugging line that is SOURCE had its carrier blanked before the main lex (kb/Work PB1913), so a carrier left in
        // this slice of that text is a COMMENT line: hide it, exactly as the main lexer's region skip did (kb/Work PB1705).
        DebuggingLineRewriter.HideAll(tokens);
        if (rewriteZero) ZeroTokenRewriter.Rewrite(tokens);
        retypes.Rewrite(tokens);
        var parser = new CobolParserCore(tokens) { Edition = edition, CobolWords = retypes.CobolWords };
        parser.RemoveErrorListeners();
        parser.AddErrorListener(flag);
        var tree = rule(parser);
        return flag.HasError ? null : tree;
    }

    /// <summary>Re-parse an already-LEXED token run through <paramref name="rule"/>, or null on any syntax error —
    /// the entry for a fragment whose SOURCE is a parse-tree slice rather than text: the §12.3.8.4 GR5/GR8
    /// EXPANSION of a parameterized class or interface (kb/Work PB759), which is the definition's own tokens with
    /// each formal parameter-name replaced by its actual. The tokens were lexed, zero-rewritten and
    /// <c>&gt;&gt;COBOL-WORDS</c>-retyped by the frontend's one pass, so none of those steps repeats here; the
    /// parser still takes <paramref name="words"/> because its TEXT predicates (<c>Word</c>) consult the map.
    /// Each token keeps its original line/column, so a diagnostic raised while binding the result points at the
    /// definition's own source line.</summary>
    public static T? ParseTokens<T>(IList<IToken> tokens, EditionInfo edition, CobolWordsMap words,
        System.Func<CobolParserCore, T> rule) where T : class
    {
        var flag = new SyntaxErrorFlag();
        var parser = new CobolParserCore(new CommonTokenStream(new ListTokenSource(tokens)))
        {
            Edition = edition,
            CobolWords = words,
        };
        parser.RemoveErrorListeners();
        parser.AddErrorListener(flag);
        var tree = rule(parser);
        return flag.HasError ? null : tree;
    }

    /// <summary>Error-presence flag for BOTH recognizers — the parser's token errors and the lexer's character
    /// errors. It was copied verbatim into all three fragment parsers; this is the one copy.</summary>
    private sealed class SyntaxErrorFlag : BaseErrorListener, IAntlrErrorListener<int>
    {
        public bool HasError { get; private set; }

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;
    }
}
