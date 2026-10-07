// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// Post-lexer token rewriting pass that converts ZERO tokens to INTEGERLIT("0")
/// when they appear in arithmetic contexts. This avoids adding ZERO to the
/// grammar's primaryExpression rule, which causes exponential ANTLR prediction time.
///
/// Arithmetic context is detected by adjacency to arithmetic operators:
///   ZERO followed by  +, -, *, /, **   → rewrite to INTEGERLIT
///   ZERO preceded by  +, -, *, /, **   → rewrite to INTEGERLIT
///   ZERO followed by  )                → rewrite (closing a parenthesized expression)
///   ZERO preceded by  (                → rewrite (opening a parenthesized expression)
///
/// All other ZERO tokens are left unchanged — they remain figurative constants
/// for VALUE, MOVE, comparison, and other non-arithmetic contexts.
///
/// <para>
/// ⛔ THE PAREN ARMS MEAN <b>ARITHMETIC</b> PARENS, AND THAT ONLY BECAME TRUE WITH fix-queue PB48. A FUNCTION
/// argument list is delimited by <c>FNARG_LPAREN</c>/<c>FNARG_RPAREN</c>, retyped by the lexer from the paren
/// stack it already maintains (ISO §8.4.3.2.3 SR6 — that '(' is ALWAYS the argument list, never a grouping
/// paren). They are deliberately absent from the two sets below, so a bare <c>ZERO</c> argument keeps its
/// figurative identity all the way to the binder, where the function's own §15.3 argument type decides whether
/// it is the numeric value 0 or the character '0' (§8.3.3.6.4 GR4 — "depending on context"). Before that, this
/// pass answered the question itself, using adjacency, which cannot know the function: <c>LOWER-CASE(ZERO)</c>
/// arrived as class numeric and was rejected as illegal.
/// </para>
/// <para>
/// ⚠ Do NOT re-add a general LPAREN/RPAREN arm "for safety" — a grouping paren inside an argument list is still
/// a plain LPAREN, so <c>FUNCTION ABS((ZERO))</c> and <c>FUNCTION MAX((ZERO + 1) 2)</c> continue to rewrite
/// exactly as before. The two token types are what separates the two meanings; nothing here needs to.
/// </para>
/// <para>
/// The REFERENCE parens <c>REF_LPAREN</c>/<c>REF_RPAREN</c> (kb/Work PB2113) are absent for the same reason: the '('
/// after a name may open a keyword-omitted function's ARGUMENT list (§8.4.3.2.3 SR2), where <c>LOWER-CASE(ZERO)</c>
/// must keep the figurative exactly as the FUNCTION form does — and in a subscript, a sole ZERO keeps it too, while
/// <c>T(ZERO + 1)</c> is rewritten by its operator like any arithmetic operand.
/// </para>
/// </summary>
public static class ZeroTokenRewriter
{
    /// <summary>
    /// The set of token types that, when adjacent to ZERO, indicate arithmetic context.
    /// </summary>
    private static readonly HashSet<int> ArithmeticOperators = new()
    {
        CobolLexer.PLUS,
        CobolLexer.MINUS,
        CobolLexer.STAR,
        CobolLexer.SLASH,
        CobolLexer.POWER,
    };

    /// <summary>
    /// Token types that indicate ZERO is inside an expression when they precede it.
    /// Includes arithmetic operators and LPAREN (e.g., "(ZERO + 1)").
    /// </summary>
    private static readonly HashSet<int> PrecedingArithmeticContext = new()
    {
        CobolLexer.PLUS,
        CobolLexer.MINUS,
        CobolLexer.STAR,
        CobolLexer.SLASH,
        CobolLexer.POWER,
        // GROUPING-PAREN-ONLY — see the class remarks: excluding FNARG_LPAREN is the whole of fix-queue PB48, and
        // REF_LPAREN is excluded for the same reason (kb/Work PB2113).
        CobolLexer.LPAREN,
        // The reference-modification COLON (§8.4.3.3.3 SR4 — "leftmost-character-position and length shall be
        // arithmetic expressions"). Its ONE grammar use is refModSpec, so both neighbours are arithmetic by
        // construction; `FUNCTION CURRENT-DATE (2:ZERO)` needs it because the delimiting parens of a ref-mod
        // written directly after a function name are FNARG_*, which no longer count as arithmetic context.
        CobolLexer.COLON,
    };

    /// <summary>
    /// Token types that indicate ZERO is inside an expression when they follow it.
    /// Includes arithmetic operators and RPAREN (e.g., "(1 + ZERO)").
    /// </summary>
    private static readonly HashSet<int> FollowingArithmeticContext = new()
    {
        CobolLexer.PLUS,
        CobolLexer.MINUS,
        CobolLexer.STAR,
        CobolLexer.SLASH,
        CobolLexer.POWER,
        // GROUPING-PAREN-ONLY — FNARG_RPAREN and REF_RPAREN excluded deliberately (PB48, PB2113); see the remarks.
        CobolLexer.RPAREN,
        CobolLexer.COLON,   // the ref-mod COLON — see PrecedingArithmeticContext (`… (ZERO:2)`)
    };

    /// <summary>
    /// Scans all tokens in the stream and replaces ZERO tokens that appear in
    /// arithmetic contexts with ZERO_ARITH virtual tokens (text "0").
    /// Must be called after <see cref="CommonTokenStream.Fill"/> so all tokens
    /// are available, and before parsing begins.
    /// </summary>
    public static void Rewrite(CommonTokenStream tokenStream)
    {
        // Fill forces the lexer to tokenize the entire input so we can
        // inspect the full token list. This is idempotent if already filled.
        tokenStream.Fill();

        var tokens = tokenStream.GetTokens();
        if (tokens == null || tokens.Count == 0)
            return;

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            // All three §8.9 spellings of the figurative zero (§8.3.3.6.2 format 1) — they are distinct tokens since
            // kb/Work PB510, and interchangeable exactly here, where the word IS the figurative constant.
            if (token.Type is not (CobolLexer.ZERO or CobolLexer.ZEROS or CobolLexer.ZEROES))
                continue;

            // Look at the next non-hidden token to the right
            int nextType = GetAdjacentTokenType(tokens, i, forward: true);
            if (nextType != -1 && FollowingArithmeticContext.Contains(nextType))
            {
                ReplaceWithZeroArith(tokens, i);
                continue;
            }

            // Look at the previous non-hidden token to the left
            int prevType = GetAdjacentTokenType(tokens, i, forward: false);
            if (prevType != -1 && PrecedingArithmeticContext.Contains(prevType))
            {
                ReplaceWithZeroArith(tokens, i);
                continue;
            }
        }

        // Reset the stream position so the parser reads from the beginning.
        tokenStream.Seek(0);
    }

    /// <summary>
    /// Finds the nearest non-hidden token in the given direction and returns its type.
    /// Returns -1 if no such token exists (beginning/end of stream).
    /// </summary>
    private static int GetAdjacentTokenType(IList<IToken> tokens, int index, bool forward)
    {
        int step = forward ? 1 : -1;
        int pos = index + step;

        while (pos >= 0 && pos < tokens.Count)
        {
            var t = tokens[pos];
            if (t.Type == TokenConstants.EOF)
                return -1;
            if (t.Channel == Lexer.DefaultTokenChannel)
                return t.Type;
            pos += step;
        }

        return -1;
    }

    /// <summary>
    /// Replaces the token at the given index with a ZERO_ARITH virtual token
    /// that has text "0" but preserves the original token's position information.
    /// </summary>
    private static void ReplaceWithZeroArith(IList<IToken> tokens, int index)
    {
        var original = tokens[index];

        var synthetic = new CommonToken(original)
        {
            Type = CobolParserCore.ZERO_ARITH,
            Text = "0",
        };

        tokens[index] = synthetic;
    }
}
