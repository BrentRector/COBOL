// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// THE parse of a compiler-directive LINE — the compiler-directive indicator, the compiler-directive word that
/// heads it, and the operand text that follows (kb/Work PB794).
///
/// <para>ISO/IEC 1989:2023 §7.3.2 gives ONE general format for every directive — <c>&gt;&gt;compiler-instruction</c>
/// — and §7.3.3 says what surrounds it: SR2, a directive is preceded only by spaces; SR5, the indicator is
/// "optionally followed by the COBOL character space … The compiler directive indicator shall be treated as
/// though it were followed by a space if no space is specified after the indicator"; SR3/SR4, a directive "may be
/// followed only by space characters and an optional inline comment". That is one fact about every directive
/// line, and before PB794 it was written SEVEN times — <c>ConditionalCompilationProcessor.SplitDirective</c> plus
/// a hand-rolled <c>trimmed[2..].TrimStart()</c> in each of the PROPAGATE, LEAP-SECOND, REF-MOD-ZERO-LENGTH,
/// COBOL-WORDS, TURN and FLAG stages, and an end-anchored regex in the reference-format normalizer. Six of the
/// seven copies knew nothing about the inline comment, so <c>&gt;&gt;PROPAGATE ON *> on</c> was REJECTED and
/// <c>&gt;&gt;SOURCE FORMAT FIXED *> switch</c> was not recognized at all — the following segment was then read
/// in the wrong reference format and the error surfaced on a line the user had not written wrong.</para>
///
/// <para>⛔ A trailing PERIOD is not tolerated. SR3/SR4 admit space characters and an inline comment after the
/// directive and nothing else; the reference-format regex used to allow <c>&gt;&gt;SOURCE FORMAT FREE.</c> while
/// every operand-checking stage already rejected the same spelling on its own directive. One rule, one place:
/// the period reaches <see cref="CompilerDirectiveCatalog.CheckOperand"/> as part of the operand and is
/// diagnosed there.</para>
/// </summary>
/// <param name="Word">The compiler-directive word, upper-cased (§7.3.3 SR6 / §8.12).</param>
/// <param name="Operand">The compiler-instruction's remainder — trimmed, with the §7.3.3 SR3/SR4 inline comment
/// removed. Empty when the directive word stands alone.</param>
public readonly record struct CompilerDirectiveLine(string Word, string Operand)
{
    /// <summary>The compiler-directive indicator (§6.2.3.1).</summary>
    public const string Indicator = ">>";

    /// <summary>The floating inline-comment indicator (§6.2.3.1 / §7.3.3 SR3, SR4).</summary>
    public const string InlineComment = "*>";

    /// <summary>
    /// Parse <paramref name="line"/> as a compiler-directive line. Returns false when the line is not one —
    /// which is the common case, so the cheap tests come first and nothing is allocated on the way out.
    /// </summary>
    /// <param name="line">The PROGRAM-TEXT AREA of one line — the whole line in free form, character positions 8 to
    /// margin R in fixed form (§7.3.3 SR3), which the reference-format stage extracts in the format in effect before
    /// asking (kb/Work PB1361) and every later stage already holds. Only spaces may precede the indicator (§7.3.3
    /// SR2). A trailing carriage return is tolerated (the text may still be CRLF).</param>
    /// <param name="directive">The word and its operand, on success.</param>
    public static bool TryParse(string line, out CompilerDirectiveLine directive)
    {
        directive = default;
        if (line.Length == 0) return false;
        ReadOnlySpan<char> s = line.AsSpan().TrimEnd('\r');

        int i = 0;
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t')) i++;
        if (i + 1 >= s.Length || s[i] != '>' || s[i + 1] != '>') return false;
        i += 2;
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t')) i++;   // SR5: the space after the indicator is optional

        int wordStart = i;
        while (i < s.Length && CobolCharacterRepertoire.IsWordCharacter(s[i])) i++;   // the ONE lexical word class (PB1402)
        if (i == wordStart) return false;                            // ">>" with no word heads no directive

        string word = s[wordStart..i].ToString().ToUpperInvariant();
        string operand = StripInlineComment(s[i..].ToString()).TrimSpaces();
        directive = new CompilerDirectiveLine(word, operand);
        return true;
    }

    /// <summary>
    /// Parse <paramref name="line"/> as a directive line headed by <paramref name="word"/>, yielding its operand.
    /// The per-stage form: a stage that owns one directive asks for its own word and gets the operand text the
    /// whole compiler agrees on.
    /// </summary>
    public static bool TryParse(string line, string word, out string operand)
    {
        operand = "";
        if (!TryParse(line, out var d)
            || !d.Word.Equals(word, StringComparison.OrdinalIgnoreCase)) return false;
        operand = d.Operand;
        return true;
    }

    /// <summary>
    /// Remove a §7.3.3 SR3/SR4 trailing inline comment. The scan honours character-strings, so the
    /// <c>*&gt;</c> inside <c>&gt;&gt;DISPLAY "a *&gt; b"</c> is data, not a comment (§8.3.3.1: a literal is
    /// delimited by a matched pair of quotation marks, and a doubled quotation mark within it is one character).
    /// </summary>
    public static string StripInlineComment(string text)
    {
        char quote = '\0';
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (quote != '\0')
            {
                if (c != quote) continue;
                if (i + 1 < text.Length && text[i + 1] == quote) i++;   // a doubled quotation mark stays inside
                else quote = '\0';
            }
            else if (c is '"' or '\'') quote = c;
            else if (c == '*' && i + 1 < text.Length && text[i + 1] == '>') return text[..i];
        }
        return text;
    }
}
