// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// ⛔ THE ONE READING OF A LITERAL'S DELIMITERS AND OF THE COMMENT INDICATOR IN SOURCE TEXT (kb/Work PB2621). Every stage
/// that walks program text before the lexer — the §6.5 logical conversion (<c>LogicalLineBuilder.ScanProgramText</c>),
/// the text-manipulation text-word scanner (<c>TextWordScanner</c>) and the directive-line parse
/// (<see cref="CompilerDirectiveLine"/>) — has to know where a literal begins and ends, because a quotation symbol or a
/// <c>*&gt;</c> inside a literal is content, and a quotation symbol inside a comment opens nothing. That is one fact
/// about source text, so it is written here, beside <see cref="CobolSpace"/>, and each stage asks it:
/// <list type="bullet">
/// <item>ISO §8.3.5 5): "The opening delimiters and closing delimiters of literals are separators. Either an apostrophe
/// or a quotation mark may be used as the quotation symbol character in opening and closing delimiters." The opening
/// delimiters are a quotation symbol, "the two contiguous characters B", B', N", N', X", and X'" and "the three
/// contiguous characters BX", BX', NX", and NX'" (<see cref="TryOpeningDelimiterAt"/>); the closing delimiter is the
/// quotation symbol of the opening one (<see cref="ClosingDelimiterAt"/>).</item>
/// <item>§8.3.3.2.3 3): "Two contiguous quotation symbol characters matching the quotation symbol used in the opening
/// delimiter represent a single occurrence of that quotation symbol character in the content of the literal" — so a
/// doubled quotation symbol is content and never closes the literal (<see cref="ClosingDelimiterAt"/>,
/// <see cref="LiteralContent"/>).</item>
/// <item>§6.2.3.1: "The following COBOL character-strings are floating indicators" — <c>*&gt;</c>, the comment
/// indicator, which outside a literal begins a comment line or an inline comment (<see cref="CommentIndicatorAt"/>,
/// <see cref="InlineCommentAt"/>).</item>
/// </list>
/// <para>Before PB2621 this was written five times in three types, three of which each called itself THE ONE, and the
/// copies had begun to differ (only one refused a prefix letter glued to a preceding word). <c>LiteralScanDriftTests</c>
/// fails the build when a text stage spells the comment indicator, the quotation-symbol pair or a doubled-quotation
/// test outside this class.</para>
/// <para>Every member reads a SPAN and looks no further than its end: a caller that carries a literal across a line end
/// (the §6.5 conversion's <c>LiteralState</c>) owns that state and hands each line in turn, and a caller that bounds a
/// literal at its line's end (the text-word scanner) passes the line.</para>
/// </summary>
public static class CobolSourceText
{
    /// <summary>The floating comment indicator (§6.2.3.1).</summary>
    public const string CommentIndicator = "*>";

    /// <summary>Whether <paramref name="c"/> is a quotation symbol — a quotation mark or an apostrophe (§8.3.5 5),
    /// §8.3.3.1).</summary>
    public static bool IsQuotationSymbol(char c) => c is '"' or '\'';

    /// <summary>Whether the floating comment indicator <c>*&gt;</c> (§6.2.3.1) stands at <paramref name="pos"/>. The
    /// caller knows <paramref name="pos"/> is outside any literal.</summary>
    public static bool CommentIndicatorAt(ReadOnlySpan<char> text, int pos)
        => text[pos..].StartsWith(CommentIndicator, StringComparison.Ordinal);

    /// <summary>
    /// Whether a literal's opening delimiter (§8.3.5 5)) begins at <paramref name="pos"/>, which is outside any literal:
    /// a quotation symbol, or a <c>B</c>, <c>N</c> or <c>X</c> prefix (any letter case) — or <c>BX</c> / <c>NX</c> —
    /// immediately followed by one. A prefix letter that continues a COBOL word (a word character stands right before
    /// it) is that word's, as the lexer reads it: <c>ABX"41"</c> is the word <c>ABX</c> and the literal <c>"41"</c>
    /// (whose missing separator the §8.3.5 5) separator rule reports, "The opening delimiter shall be immediately
    /// preceded by a space, left parenthesis, or opening pseudo-text delimiter").
    /// </summary>
    public static bool TryOpeningDelimiterAt(ReadOnlySpan<char> text, int pos, out OpeningDelimiter delimiter)
    {
        delimiter = default;
        char c = text[pos];
        if (IsQuotationSymbol(c)) return true;
        char prefix = c switch { 'B' or 'b' => 'B', 'N' or 'n' => 'N', 'X' or 'x' => 'X', _ => '\0' };
        if (prefix == '\0' || (pos > 0 && CobolCharacterRepertoire.IsWordCharacter(text[pos - 1]))) return false;
        if (QuotationSymbolAt(text, pos + 1))
        {
            delimiter = new OpeningDelimiter(prefix, 1);
            return true;
        }
        if (prefix is 'B' or 'N' && pos + 1 < text.Length && text[pos + 1] is 'X' or 'x' && QuotationSymbolAt(text, pos + 2))
        {
            delimiter = new OpeningDelimiter(prefix, 2);
            return true;
        }
        return false;
    }

    private static bool QuotationSymbolAt(ReadOnlySpan<char> text, int pos) => pos < text.Length && IsQuotationSymbol(text[pos]);

    /// <summary>The index of the closing delimiter of a literal whose opening quotation symbol is
    /// <paramref name="quote"/>, searching from <paramref name="from"/> (the first content character): the first
    /// <paramref name="quote"/> that is not one of a doubled pair (§8.3.3.2.3 3)), or <c>text.Length</c> when the text
    /// ends inside the literal. A quotation symbol that is the LAST character of the text is returned as the closing
    /// delimiter: a caller whose literal may continue on the next line decides it from that line's first
    /// character.</summary>
    public static int ClosingDelimiterAt(ReadOnlySpan<char> text, int from, char quote)
    {
        int i = from;
        while (i < text.Length)
        {
            int k = text[i..].IndexOf(quote);
            if (k < 0) break;
            i += k;
            if (i + 1 < text.Length && text[i + 1] == quote) { i += 2; continue; }   // a doubled quotation symbol: content
            return i;
        }
        return text.Length;
    }

    /// <summary>The index just past the literal whose opening quotation symbol is at <paramref name="quotePos"/>: past
    /// its closing delimiter, or <c>text.Length</c> when the text ends inside it.</summary>
    public static int LiteralEnd(ReadOnlySpan<char> text, int quotePos)
    {
        int closing = ClosingDelimiterAt(text, quotePos + 1, text[quotePos]);
        return closing < text.Length ? closing + 1 : text.Length;
    }

    /// <summary>The content of the literal whose opening quotation symbol is at <paramref name="quotePos"/>, each doubled
    /// quotation symbol read as the one character it represents (§8.3.3.2.3 3)); <paramref name="closing"/> is the index
    /// of its closing delimiter, or <c>text.Length</c> when the text ends inside the literal.</summary>
    public static string LiteralContent(ReadOnlySpan<char> text, int quotePos, out int closing)
    {
        char quote = text[quotePos];
        closing = ClosingDelimiterAt(text, quotePos + 1, quote);
        var content = text[(quotePos + 1)..closing];
        // Every quotation symbol before the closing delimiter is one of a doubled pair, so the pairs are the only ones.
        return content.Contains(quote)
            ? content.ToString().Replace(new string(quote, 2), quote.ToString(), StringComparison.Ordinal)
            : content.ToString();
    }

    /// <summary>The index of the first comment indicator (§6.2.3.1) of <paramref name="text"/> that stands outside every
    /// literal — where its inline comment begins — or <c>text.Length</c> when it holds none. The text begins outside any
    /// literal and is one line, so a literal it leaves unterminated runs to its end.</summary>
    public static int InlineCommentAt(ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (IsQuotationSymbol(text[i])) i = LiteralEnd(text, i) - 1;
            else if (CommentIndicatorAt(text, i)) return i;
        }
        return text.Length;
    }
}

/// <summary>A literal's opening delimiter (§8.3.5 5)): its prefix letter, upper-cased (<c>B</c>, <c>N</c> or <c>X</c>;
/// NUL for a bare quotation symbol), and the length of its prefix — 0, 1, or 2 for <c>BX</c> and <c>NX</c>. The
/// quotation symbol stands at the delimiter's start plus <see cref="PrefixLength"/>.</summary>
public readonly record struct OpeningDelimiter(char Prefix, int PrefixLength)
{
    /// <summary>A national literal's delimiter, <c>N"</c> or <c>NX"</c> (§8.3.3.5).</summary>
    public bool IsNational => Prefix == 'N';
}
