// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE ONE DEFINITION OF A CHARACTER POSITION (kb/Work PB1966; docs/CONFORMANCE.md DOC-A.1-157; §6.1 1) c) "The
/// implementor shall specify the meaning of lines and character positions"). Reference format is described in terms of
/// character positions (§6.1 1) a)), and a position is one CHARACTER — one Unicode code point — never one UTF-16 code
/// unit: the decoded text is a .NET string, in which a supplementary-plane letter (an Annex B extended letter such as
/// an Adlam or Osage one, §8.1.3.2 GR4) is two <see cref="char"/>s but still one position, exactly as it is one
/// character of a word's length (§8.1.3.2 GR4 c), <c>CobolCharacterRepertoire.LengthInCharacters</c>). A surrogate that
/// is not half of a valid pair is a character of its own. Every site that turns a column into an index of a line, an
/// index into a column, or counts positions asks THIS type (<c>PhysicalLinesDriftTests</c>): a column read as a string
/// index puts margin R N positions early on a line holding N supplementary characters, so legal source whose text ends
/// at position 72 is cut short.
/// </summary>
internal static class CharacterPositions
{
    /// <summary>The number of character positions <paramref name="text"/> occupies.</summary>
    public static int Count(ReadOnlySpan<char> text)
    {
        if (!HasSurrogate(text)) return text.Length;
        int n = 0;
        for (int i = 0; i < text.Length; i += UnitsAt(text, i)) n++;
        return n;
    }

    /// <summary>The index in <paramref name="text"/> of the first UTF-16 unit of the character at 0-based
    /// <paramref name="position"/>, or <c>text.Length</c> when the text holds no character there.</summary>
    public static int IndexAt(ReadOnlySpan<char> text, int position)
    {
        if (!HasSurrogate(text)) return Math.Min(position, text.Length);
        int i = 0;
        for (int n = 0; n < position && i < text.Length; n++) i += UnitsAt(text, i);
        return i;
    }

    /// <summary>The 0-based character position of the character that begins at <paramref name="index"/> of
    /// <paramref name="text"/> — the number of characters before it.</summary>
    public static int PositionAt(ReadOnlySpan<char> text, int index) => Count(text[..index]);

    /// <summary>Whether any UTF-16 unit of <paramref name="text"/> is a surrogate — the only text in which a position and
    /// an index differ (a vectorized scan, so the BMP-only text of nearly every line pays one pass).</summary>
    private static bool HasSurrogate(ReadOnlySpan<char> text) => text.IndexOfAnyInRange('\uD800', '\uDFFF') >= 0;

    /// <summary>The UTF-16 units of the character at <paramref name="index"/>: two for a valid surrogate pair, else one.</summary>
    private static int UnitsAt(ReadOnlySpan<char> text, int index)
        => char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]) ? 2 : 1;
}
