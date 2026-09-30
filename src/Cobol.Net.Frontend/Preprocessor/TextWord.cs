// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Preprocessor;

/// <summary>What a <see cref="TextWord"/> is — the §7.2.2.5 classification the text-manipulation stage (COPY and
/// REPLACE) needs to parse its statements and to compare text-words.</summary>
internal enum TextWordKind
{
    /// <summary>§7.2.2.5 3): "any other character or sequence of contiguous characters … bounded by separators" —
    /// COBOL words, numeric literals, picture character-strings, operators.</summary>
    CharacterString,

    /// <summary>§7.2.2.5 2): "an alphanumeric, boolean, or national literal including the opening and closing
    /// delimiters that bound the literal" — every §8.3.5 5) opening delimiter, prefixed (<c>X"</c>, <c>N"</c>,
    /// <c>NX"</c>, <c>B"</c>, <c>BX"</c>) or not.</summary>
    Literal,

    /// <summary>§7.2.2.5 1): a separator that is a text-word — the colon and the parentheses (separators "in any
    /// context except within alphanumeric or national literals") and the separator period (§8.3.5 3), a period
    /// followed by a space).</summary>
    Separator,

    /// <summary>§8.3.5 2): a comma or semicolon immediately followed by a space. A text-word, but one that matching
    /// treats as a space (§7.2.3.4 9) c) 1. / §7.2.4.4 8) c) 1.) and that a statement's syntax treats as the
    /// separator space it may stand for.</summary>
    SeparatorCommaOrSemicolon,

    /// <summary>§8.3.5 6): the pseudo-text delimiter <c>==</c>. Not a text-word (§7.2.2.5 1) excepts it); it bounds
    /// the pseudo-text operands of COPY REPLACING and REPLACE.</summary>
    PseudoTextDelimiter,

    /// <summary>A whole compiler directive line (§7.3.3: the <c>&gt;&gt;</c> indicator preceded only by spaces, as
    /// <see cref="CobolNet.Editions.CompilerDirectiveLine.TryParse(string, out CobolNet.Editions.CompilerDirectiveLine, bool)"/>
    /// recognizes it), from its indicator to the end of its line. Not a text-word: "Each occurrence of a compiler
    /// directive line is treated as a single space" for matching (§7.2.3.4 9) c) 5. / §7.2.4.4 8) c) 5.), and "A
    /// compiler directive line is not affected by the replacing action of a COPY statement or a REPLACE statement"
    /// (§7.3.4 1)) — so nothing inside it is ever a text-word to compare, a replacement target, or a COPY
    /// statement.</summary>
    DirectiveLine,
}

/// <summary>One text-word (ISO §7.2.2.5): its kind and its span <c>[Start, End)</c> in <see cref="Source"/>, the text
/// it was scanned from. The spelling is read from the span on demand (<see cref="Span"/> never allocates), so scanning
/// a whole compilation group for its COPY statements does not allocate a string per word.</summary>
internal readonly record struct TextWord(string Source, TextWordKind Kind, int Start, int End)
{
    /// <summary>The text-word's characters, without allocating.</summary>
    public ReadOnlySpan<char> Span => Source.AsSpan(Start, End - Start);

    /// <summary>The text-word's spelling as written.</summary>
    public string Value => Source[Start..End];

    /// <summary>A character-string equal to <paramref name="reservedWord"/>, case-insensitively — how the statement
    /// parsers recognise COPY, OF, IN, SUPPRESS, PRINTING, REPLACING, LEADING, TRAILING, BY and OFF.</summary>
    public bool IsWord(string reservedWord)
        => Kind == TextWordKind.CharacterString && Span.Equals(reservedWord, StringComparison.OrdinalIgnoreCase);

    /// <summary>The separator period that ends a COPY or REPLACE statement (§8.3.5 3)).</summary>
    public bool IsSeparatorPeriod => Kind == TextWordKind.Separator && Source[Start] == '.';

    /// <summary>Whether matching treats this element as a single space rather than a text-word to compare: a
    /// separator comma or semicolon (§7.2.3.4 9) c) 1. / §7.2.4.4 8) c) 1.) or a compiler directive line
    /// (c) 5.). A statement's syntax steps over the same elements where a separator space may stand.</summary>
    public bool IsSpaceForMatching => Kind is TextWordKind.SeparatorCommaOrSemicolon or TextWordKind.DirectiveLine;

    /// <summary>The two text-words match for COPY REPLACING (ISO §7.2.3.4 9) c)) and REPLACE (§7.2.4.4 8) c) — the
    /// same rules, one implementation):
    /// <list type="bullet">
    /// <item>3. "Except when used in the non-hexadecimal formats of alphanumeric and national literals, … each
    /// lowercase letter is equivalent to its corresponding uppercase letter" — so a character-string compares
    /// case-insensitively, and so do a literal's delimiter prefix and the content of its hexadecimal and boolean
    /// formats, while the content of <c>"…"</c> and <c>N"…"</c> compares case-SENSITIVELY;</item>
    /// <item>4. a. "The two representations of the quotation symbol match" — the quote character is not
    /// compared;</item>
    /// <item>4. b. "two contiguous occurrences of the character used as the quotation symbol in the opening
    /// delimiter are treated as a single occurrence" — contents compare un-doubled.</item>
    /// </list>
    /// The alphanumeric-to-national equivalence of rule 3 needs no code: both are UTF-16 here (the repertoire is
    /// one code set), so the same character is the same <see cref="char"/>. Rule 1 (comma and semicolon are a space)
    /// is the callers' — they drop <see cref="TextWordKind.SeparatorCommaOrSemicolon"/> words before comparing.</summary>
    public bool MatchesForReplacing(in TextWord other)
    {
        if ((Kind == TextWordKind.Literal) != (other.Kind == TextWordKind.Literal)) return false;
        if (Kind != TextWordKind.Literal)
            return Span.Equals(other.Span, StringComparison.OrdinalIgnoreCase);

        var a = TextWordScanner.DecomposeLiteral(Value);
        var b = TextWordScanner.DecomposeLiteral(other.Value);
        if (!string.Equals(a.Prefix, b.Prefix, StringComparison.OrdinalIgnoreCase)) return false;
        return string.Equals(a.Content, b.Content,
            a.IsNonHexadecimalAlphanumericOrNational ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>A literal text-word taken apart: its opening-delimiter prefix (<c>""</c>, <c>X</c>, <c>N</c>, <c>NX</c>,
/// <c>B</c>, <c>BX</c> — as written), its quotation symbol, and its content with doubled quotation symbols collapsed
/// (§7.2.3.4 9) c) 4. b.). <see cref="Terminated"/> is false when the line ended before the closing delimiter.</summary>
internal readonly record struct LiteralParts(string Prefix, char Quote, string Content, bool Terminated)
{
    /// <summary>The "non-hexadecimal formats of alphanumeric and national literals" (§7.2.3.4 9) c) 3.) —
    /// <c>"…"</c> and <c>N"…"</c> — whose content keeps its letter case in matching.</summary>
    public bool IsNonHexadecimalAlphanumericOrNational
        => Prefix.Length == 0 || Prefix.Equals("N", StringComparison.OrdinalIgnoreCase);

    /// <summary>An alphanumeric literal (§8.3.3.2): <c>"…"</c> or its hexadecimal format <c>X"…"</c>.</summary>
    public bool IsAlphanumeric => Prefix.Length == 0 || Prefix.Equals("X", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// THE text-word scanner of the text-manipulation stage (ISO §7.2.2.5 with the §8.3.5 separators): COPY and REPLACE
/// statements are parsed over its words, and COPY REPLACING / REPLACE compare its words — so what a text-word is
/// is decided here and nowhere else.
/// <list type="bullet">
/// <item>A space separates and is not a text-word; so is a comment (§7.2.3.4 9) c) 6. "Comments, if any, are
/// treated as a single space") — a floating comment indicator <c>*&gt;</c> outside a literal runs to the end of the
/// line, as the lexer reads it.</item>
/// <item>§7.2.2.5 1): the colon and the parentheses are separator text-words "in any context except within
/// alphanumeric or national literals"; a period, comma or semicolon is a separator only "when followed by a space"
/// (§8.3.5 2) / 3)) — so <c>Z,ZZ9</c> and <c>ZZ.ZZ</c> are ONE text-word each, and <c>1:5</c> is three.</item>
/// <item>§7.2.2.5 2): a literal is ONE text-word from its opening delimiter — including the §8.3.5 5) prefixes
/// <c>B" N" X" BX" NX"</c> (either quotation symbol, any letter case) — through its closing delimiter, a doubled
/// quotation symbol being content. An unterminated literal ends at its line's end, as the lexer ends it.</item>
/// <item>§7.2.3.4 9) c) 2.: "Each operand and operator of a concatenation expression is a separate text-word" —
/// <c>&amp;</c> (which no COBOL word contains) is always a text-word of its own.</item>
/// <item>§8.3.5 6): <c>==</c> is a pseudo-text delimiter wherever it begins outside a literal.</item>
/// <item>A compiler directive line left in the text (the stages after text manipulation read <c>&gt;&gt;TURN</c>,
/// <c>&gt;&gt;PROPAGATE</c>, <c>&gt;&gt;PAGE</c> …; library text is replaced before its own <c>&gt;&gt;DEFINE</c> /
/// <c>&gt;&gt;IF</c> lines are processed) is ONE <see cref="TextWordKind.DirectiveLine"/> element, never scanned
/// for text-words (§7.3.4 1); §7.2.3.4 9) c) 5.).</item>
/// </list>
/// A fixed-form debugging line reaches this stage as <see cref="ReferenceFormatProcessor.DebugLineCarrier"/> + its
/// text: the carrier is skipped and the text scanned, so its text-words take part in matching as if the <c>D</c>
/// were absent (the COBOL-85 rule the carrier exists for); an ordinary <c>*&gt;</c> comment, whatever it says, is a
/// space.
/// </summary>
internal static class TextWordScanner
{
    /// <summary>The characters that separate as the COBOL character space does (§8.3.5 1) "The COBOL character space is
    /// a separator") — the space and the line end, exactly the lexer's <c>WS</c> set (<c>CobolLexer.g4</c>:
    /// <c>[ \n]</c>), so a text-word ends where the lexer's word ends. No tab and no CR LF reaches this stage: the
    /// line-entry stage (<see cref="PhysicalLines"/>, DOC-A.1-157 / DOC-A.1-156) has expanded the one and taken the other
    /// to its LF. Never Unicode White_Space: U+00A0, U+2000–U+200A, U+3000 and the rest only LOOK like a space and are
    /// ordinary text-word characters (§7.2.2.5 3); kb/Work PB1543).</summary>
    public static readonly char[] SeparatorSpaces = [' ', '\n'];

    /// <summary>Whether <paramref name="c"/> is one of the <see cref="SeparatorSpaces"/>.</summary>
    public static bool IsSeparatorSpace(char c) => c is ' ' or '\n';

    /// <summary>Every text-word of <paramref name="text"/>, in order.</summary>
    public static List<TextWord> Scan(string text)
    {
        var words = new List<TextWord>();
        int pos = 0;
        while (TryNext(text, ref pos, out var word)) words.Add(word);
        return words;
    }

    /// <summary>The text-words of <paramref name="text"/> that take part in matching: every text-word but the
    /// separator comma and semicolon, which "is considered to be a single space" (§7.2.3.4 9) c) 1. /
    /// §7.2.4.4 8) c) 1.), and every compiler directive line, which "is treated as a single space" (c) 5.) — so,
    /// like the spaces around them, neither is a word to compare (<see cref="TextWord.IsSpaceForMatching"/>).</summary>
    public static List<TextWord> MatchWords(string text)
    {
        var words = Scan(text);
        words.RemoveAll(w => w.IsSpaceForMatching);
        return words;
    }

    /// <summary>The next text-word at or after <paramref name="pos"/> (which must not lie inside a literal or a
    /// comment), skipping spaces and comments; <paramref name="pos"/> advances past it. False at the end of the
    /// text.</summary>
    public static bool TryNext(string text, ref int pos, out TextWord word)
    {
        int n = text.Length;
        while (pos < n)
        {
            char c = text[pos];
            if (IsSeparatorSpace(c)) { pos++; continue; }

            if (IsDebugLineCarrierAt(text, pos))
            {
                pos += ReferenceFormatProcessor.DebugLineCarrier.Length;       // the debugging line's text is scanned
                continue;
            }
            if (CommentStartsAt(text, pos))
            {
                while (pos < n && text[pos] != '\n') pos++;                    // a comment is a single space
                continue;
            }

            int start = pos;
            TextWordKind kind;
            if (c == '>' && DirectiveLineEnd(text, pos) is var directiveEnd and >= 0)
            {
                kind = TextWordKind.DirectiveLine;
                pos = directiveEnd;
            }
            else if (c is '(' or ')' or ':' || (c == '.' && SeparatorFollows(text, pos)))
            {
                kind = TextWordKind.Separator;
                pos++;
            }
            else if (c is ',' or ';' && SeparatorFollows(text, pos))
            {
                kind = TextWordKind.SeparatorCommaOrSemicolon;
                pos++;
            }
            else if (IsPseudoTextDelimiter(text, pos))
            {
                kind = TextWordKind.PseudoTextDelimiter;
                pos += 2;
            }
            else if (c == '&')
            {
                kind = TextWordKind.CharacterString;
                pos++;
            }
            else if (LiteralPrefixLength(text, pos) is var prefix and >= 0)
            {
                kind = TextWordKind.Literal;
                pos = LiteralEnd(text, pos + prefix);
            }
            else
            {
                kind = TextWordKind.CharacterString;
                while (pos < n && !EndsCharacterString(text, pos)) pos++;
            }
            word = new TextWord(text, kind, start, pos);
            return true;
        }
        word = default;
        return false;
    }

    /// <summary>Whether a comment begins at <paramref name="pos"/> (outside a literal): the floating comment indicator
    /// <c>*&gt;</c> (§6.2.3.2), other than the fixed-form debugging-line carrier the §6.5 conversion writes in its
    /// place. The ONE reading of a comment indicator in text manipulation — <see cref="TryNext"/> skips what it
    /// starts, and <see cref="HoldsComment"/> finds what a replacing action produces.</summary>
    public static bool CommentStartsAt(string text, int pos)
        => text[pos] == '*' && pos + 1 < text.Length && text[pos + 1] == '>' && !IsDebugLineCarrierAt(text, pos);

    private static bool IsDebugLineCarrierAt(string text, int pos)
        => string.CompareOrdinal(text, pos, ReferenceFormatProcessor.DebugLineCarrier, 0,
            ReferenceFormatProcessor.DebugLineCarrier.Length) == 0;

    /// <summary>Whether <paramref name="text"/> holds a comment: a <see cref="CommentStartsAt">comment indicator</see>
    /// in the text between its text-words — never one inside a literal, which is a text-word of its own. Comments were
    /// removed from source and library text by the §6.5 logical conversion before text manipulation began, so in text
    /// a COPY or REPLACE replacing action produces a comment can only be one the replacement SPELLED — a partial-word
    /// result such as <c>*&gt;1</c> (kb/Work PB1356; §7.2.3.4 GR13 / §7.2.4.4 GR9).</summary>
    public static bool HoldsComment(string text)
    {
        int pos = 0;
        while (true)
        {
            int gapStart = pos;
            bool more = TryNext(text, ref pos, out var word);
            int gapEnd = more ? word.Start : text.Length;
            for (int i = gapStart; i < gapEnd; i++)
                if (CommentStartsAt(text, i)) return true;
            if (!more) return false;
        }
    }

    /// <summary>Take a literal text-word apart (see <see cref="LiteralParts"/>).</summary>
    public static LiteralParts DecomposeLiteral(string literal)
    {
        int q = literal.IndexOfAny(Quotes);
        char quote = literal[q];
        var content = new System.Text.StringBuilder(literal.Length);
        bool terminated = false;
        for (int i = q + 1; i < literal.Length; i++)
        {
            if (literal[i] != quote) { content.Append(literal[i]); continue; }
            if (i + 1 < literal.Length && literal[i + 1] == quote) { content.Append(quote); i++; continue; }
            terminated = true;
            break;
        }
        return new LiteralParts(literal[..q], quote, content.ToString(), terminated);
    }

    private static readonly char[] Quotes = ['"', '\''];

    /// <summary>A period, comma or semicolon is a separator only "when followed by a space" (§8.3.5 2) / 3)); the end
    /// of a line and of the text count as one (a space is assumed at the end of a source line — §7.2.4.4 NOTE 2), and
    /// so does a closing pseudo-text delimiter: §8.3.5 8) lets the separator space OPTIONALLY precede that separator,
    /// so <c>==STOP RUN.==</c> and <c>==STOP RUN. ==</c> must form the same text-words (⚠ determination, PB1350).</summary>
    private static bool SeparatorFollows(string text, int pos)
        => pos + 1 >= text.Length || IsSeparatorSpace(text[pos + 1]) || IsPseudoTextDelimiter(text, pos + 1);

    private static bool IsPseudoTextDelimiter(string text, int pos)
        => text[pos] == '=' && pos + 1 < text.Length && text[pos + 1] == '=';

    /// <summary>The end of the compiler directive line whose <c>&gt;&gt;</c> indicator is at <paramref name="pos"/>
    /// (the position of its line's line feed, or of the carriage return before it), or -1 when no directive line
    /// starts there. The text reaching this stage is logically free form (§7.2.1 Step 1), and "A compiler directive
    /// shall be preceded only by zero, one, or more space characters" (§7.3.3 SR2) — the ONE recognizer,
    /// <see cref="CobolNet.Editions.CompilerDirectiveLine"/>, decides the rest.</summary>
    private static int DirectiveLineEnd(string text, int pos)
    {
        if (pos + 1 >= text.Length || text[pos + 1] != '>') return -1;
        int lineStart = pos;
        while (lineStart > 0 && text[lineStart - 1] == ' ') lineStart--;
        if (lineStart > 0 && text[lineStart - 1] != '\n') return -1;

        int lineEnd = text.IndexOf('\n', pos);
        if (lineEnd < 0) lineEnd = text.Length;
        return CobolNet.Editions.CompilerDirectiveLine.TryParse(text[pos..lineEnd], out _) ? lineEnd : -1;
    }

    /// <summary>Where a character-string ends: at a space, at a separator (§7.2.2.5 1) / §8.3.5), at a literal's
    /// opening delimiter, at the concatenation operator, and at a comment indicator (the lexer's reading of
    /// <c>*&gt;</c>, which §6.2.3.2 2) requires to follow a space anyway).</summary>
    private static bool EndsCharacterString(string text, int pos)
    {
        char d = text[pos];
        return IsSeparatorSpace(d)
            || d is '(' or ')' or ':' or '"' or '\'' or '&'
            || (d is '.' or ',' or ';' && SeparatorFollows(text, pos))
            || IsPseudoTextDelimiter(text, pos)
            || (d == '*' && pos + 1 < text.Length && text[pos + 1] == '>');
    }

    /// <summary>The length of the literal opening-delimiter prefix at <paramref name="pos"/> — 0 for a bare
    /// quotation symbol, 1 for <c>B X N</c>, 2 for <c>BX NX</c> (§8.3.5 5), any letter case) — or -1 when no literal
    /// starts here.</summary>
    private static int LiteralPrefixLength(string text, int pos)
    {
        static bool QuoteAt(string t, int i) => i < t.Length && t[i] is '"' or '\'';
        if (QuoteAt(text, pos)) return 0;
        char p = char.ToUpperInvariant(text[pos]);
        if (p is 'B' or 'N' or 'X' && QuoteAt(text, pos + 1)) return 1;
        if (p is 'B' or 'N' && pos + 1 < text.Length && char.ToUpperInvariant(text[pos + 1]) == 'X' && QuoteAt(text, pos + 2))
            return 2;
        return -1;
    }

    /// <summary>The end of the literal whose opening quotation symbol is at <paramref name="quotePos"/>: just past
    /// the matching closing delimiter (a doubled quotation symbol is content), or the end of the line when the
    /// literal is unterminated there.</summary>
    private static int LiteralEnd(string text, int quotePos)
    {
        char quote = text[quotePos];
        int i = quotePos + 1;
        while (i < text.Length && text[i] != '\n')
        {
            if (text[i] == quote)
            {
                if (i + 1 < text.Length && text[i + 1] == quote) { i += 2; continue; }
                return i + 1;
            }
            i++;
        }
        return i;
    }
}

/// <summary>
/// The public face of text-word EQUALITY for the later stages: whether two source texts are the same ordered sequence
/// of §7.2.2.5 text-words under the ONE text-word comparison, <see cref="TextWord.MatchesForReplacing"/> (ISO
/// §7.2.3.4 9) c) — separators collapse to a space, COBOL words compare case-insensitively, the non-hexadecimal
/// alphanumeric and national literals case-sensitively, and the two quotation symbols match).
/// <para>Its asker is §13.10.3 SR9 (kb/Work PB1230): a duplicated constant-name's "specification of
/// arithmetic-expression-1, literal-1, data-name-1, data-name-2, or compilation-variable-name-1 shall be the same as
/// specified in the other constant-name" — a comparison of what was WRITTEN, which is the question this matcher
/// already answers for COPY REPLACING and REPLACE, so SR9 asks it here rather than keeping a second notion of "the
/// same text".</para>
/// </summary>
public static class TextWordSequence
{
    /// <summary>True when <paramref name="a"/> and <paramref name="b"/> are the same text-words in the same order.</summary>
    public static bool Matches(string a, string b)
    {
        var x = TextWordScanner.MatchWords(a);
        var y = TextWordScanner.MatchWords(b);
        if (x.Count != y.Count) return false;
        for (int i = 0; i < x.Count; i++)
            if (!x[i].MatchesForReplacing(y[i])) return false;
        return true;
    }
}
