// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// Post-lexer token rewriting pass that converts a figurative zero (ZERO, ZEROS or ZEROES) to the virtual
/// ZERO_ARITH token (text "0") when it appears in an arithmetic context. This avoids adding the figurative to the
/// grammar's primaryExpression rule, which causes exponential ANTLR prediction time.
///
/// Arithmetic context is detected by adjacency (<see cref="PrecedingArithmeticContext"/>,
/// <see cref="FollowingArithmeticContext"/>, <see cref="IsParenthesizedAlone"/>):
///   ZERO followed by  +, -, *, /, **   → rewrite to ZERO_ARITH
///   ZERO preceded by  +, -, *, /, **   → rewrite to ZERO_ARITH
///   ZERO between a grouping ( and )    → rewrite (<c>(ZERO)</c>, a parenthesized arithmetic expression)
///   ZERO beside the ref-mod :          → rewrite (a reference-modification position or length)
///
/// All other ZERO tokens are left unchanged — they remain figurative constants
/// for VALUE, MOVE, comparison, and other non-arithmetic contexts.
///
/// <para>
/// ⛔ ONE GROUPING PAREN IS NOT AN ARITHMETIC CONTEXT; A PAIR AROUND THE ZERO ALONE IS (kb/Work PB2734). A plain
/// <c>LPAREN</c>/<c>RPAREN</c> (GROUPING-PAREN-ONLY: the FNARG_ and REF_ twins below never enclose a condition)
/// also groups a CONDITION (§8.8.4.9: a complex condition's truth value is the same
/// "whether parenthesized or not"), and adjacency cannot tell that '(' from an arithmetic one. The paren arms used
/// to rewrite a ZERO beside EITHER paren, so <c>IF (WS-A = ZERO)</c> and <c>IF (ZERO = WS-A)</c> bound the numeric
/// literal 0 where the unparenthesized relation binds the figurative constant: over a <c>PIC X(3) VALUE "000"</c>
/// item the bare relation answered EQUAL and the parenthesized one NOT EQUAL, a silent wrong branch against
/// §8.3.3.6.4 GR4 (the zero format is the value 0 or the character '0' "depending on context") and GR2's NOTE 1
/// (the figurative is associated with the item it is "compared with"); and <c>IF (WS-N IS ZERO)</c>, whose ZERO is
/// the sign condition's keyword, did not parse at all. Inside an arithmetic paren the ZERO's other neighbour is an
/// arithmetic operator (which its own arm rewrites) or the matching ')': only <c>( ZERO )</c> leaves the parens
/// as the ZERO's whole context, and only there is the paren the deciding fact (§8.8.1.1: "an arithmetic expression
/// enclosed in parentheses"). A ZERO with a paren on one side and anything else on the other is a relation
/// operand, an abbreviated object or a sign-condition keyword inside a parenthesized condition, and keeps its
/// figurative identity.
/// </para>
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
    /// Token types that indicate ZERO is inside an expression when they precede it: the arithmetic operators and the
    /// ref-mod COLON. A grouping paren is NOT one by itself (kb/Work PB2734): see <see cref="IsParenthesizedAlone"/>.
    /// </summary>
    private static readonly HashSet<int> PrecedingArithmeticContext = new()
    {
        CobolLexer.PLUS,
        CobolLexer.MINUS,
        CobolLexer.STAR,
        CobolLexer.SLASH,
        CobolLexer.POWER,
        // The reference-modification COLON (§8.4.3.3.3 SR4 — "leftmost-character-position and length shall be
        // arithmetic expressions"). Its ONE grammar use is refModSpec, so both neighbours are arithmetic by
        // construction; `FUNCTION CURRENT-DATE (2:ZERO)` needs it because the delimiting parens of a ref-mod
        // written directly after a function name are FNARG_*, which no longer count as arithmetic context.
        CobolLexer.COLON,
    };

    /// <summary>
    /// Token types that indicate ZERO is inside an expression when they follow it: the arithmetic operators and the
    /// ref-mod COLON. A grouping paren is NOT one by itself (kb/Work PB2734): see <see cref="IsParenthesizedAlone"/>.
    /// </summary>
    private static readonly HashSet<int> FollowingArithmeticContext = new()
    {
        CobolLexer.PLUS,
        CobolLexer.MINUS,
        CobolLexer.STAR,
        CobolLexer.SLASH,
        CobolLexer.POWER,
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

            // The nearest non-hidden token on each side (-1 at either end of the stream, which no set contains).
            int nextType = GetAdjacentTokenType(tokens, i, forward: true);
            int prevType = GetAdjacentTokenType(tokens, i, forward: false);
            if (FollowingArithmeticContext.Contains(nextType) || PrecedingArithmeticContext.Contains(prevType)
                || IsParenthesizedAlone(prevType, nextType))
                ReplaceWithZeroArith(tokens, i);
        }

        // Reset the stream position so the parser reads from the beginning.
        tokenStream.Seek(0);
    }

    /// <summary>
    /// The ZERO is the whole content of a grouping paren pair, <c>( ZERO )</c>: an arithmetic expression enclosed in
    /// parentheses (§8.8.1.1), which no condition can be, so the pair is an arithmetic context by itself. One paren
    /// alone is not, because it may group a condition (kb/Work PB2734; the class remarks).
    /// GROUPING-PAREN-ONLY: the FNARG_ and REF_ twins are excluded deliberately (PB48, PB2113; the class remarks).
    /// </summary>
    private static bool IsParenthesizedAlone(int prevType, int nextType) =>
        prevType == CobolLexer.LPAREN && nextType == CobolLexer.RPAREN;

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
