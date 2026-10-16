// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using System.Text;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE ONE READER OF A FIXED-FORM LINE'S COLUMNS (kb/Work PB1966; docs/CONFORMANCE.md DOC-A.1-157). Positions 1–6 are
/// the sequence number area, position 7 the indicator area and positions 8–72 the program-text area; margin R follows
/// position 72 and anything beyond it is ignored (Annex A item 158). Every one of those is a CHARACTER POSITION
/// (<see cref="CharacterPositions"/>), so this type turns each into an index of the line once and no other site spells
/// <c>line[6]</c>, <c>line[7..72]</c> or <c>line.Length &gt; 72</c>: such a read puts margin R N positions early on a line
/// holding N supplementary-plane characters.
/// </summary>
internal readonly struct FixedFormLine
{
    /// <summary>Positions in the sequence number area (positions 1–6).</summary>
    public const int SequenceAreaLength = 6;

    /// <summary>The 0-based position of the indicator area (position 7; ISO §6.3.1: "The indicator area is the 7th
    /// character position of a line"). It is also THE PHYSICAL COLUMN of every indicator character a diagnostic can name
    /// (kb/Work PB2982): a tab of the sequence area (positions 1–6) or of position 7 fills through position 8
    /// (DOC-A.1-157), so an indicator that is not a space is never preceded by a tab, and positions are characters
    /// (<see cref="CharacterPositions"/>), so a supplementary-plane character in the sequence area does not move it
    /// either.</summary>
    public const int IndicatorColumn = 6;

    /// <summary>The 0-based position where the program-text area begins (position 8).</summary>
    public const int SourceAreaStart = 7;

    /// <summary>Positions in the program-text area (positions 8–72).</summary>
    public const int SourceAreaWidth = 65;

    /// <summary>Margin R, the 0-based position just past the program-text area: our documented margin R is position 72
    /// (Annex A item 158 / docs/CONFORMANCE.md §7).</summary>
    public const int MarginR = SourceAreaStart + SourceAreaWidth;

    /// <summary>Positions in AREA A (positions 8–11), the first positions of the program-text area. ANSI X3.23-1985 split
    /// the program-text area at margin B into area A and area B (positions 12 to margin R); ISO/IEC 1989:2002 removed the
    /// split — the 2023 program-text area "begins in character position 8 and terminates with the character position
    /// immediately to the left of margin R" (ISO §6.3.1) and has no area — so the areas matter only to a COBOL-85
    /// reading: the comment-entry paragraph's end and the area-placement rules (kb/Work PB1494, PB3147).</summary>
    public const int AreaAWidth = 4;

    /// <summary>The indicator-area character of a fixed continuation line (ISO §6.2.2: a hyphen). The ONE spelling of it:
    /// the §6.5 line classifier and the reference-format detector both read it from here.</summary>
    public const char ContinuationIndicator = '-';

    /// <summary>Whether the character at 0-based <paramref name="programTextOffset"/> of the program-text area (position
    /// 8 is offset 0) is in area A — the ONE definition of where area A ends (<see cref="AreaAWidth"/>).</summary>
    public static bool IsAreaAOffset(int programTextOffset) => programTextOffset is >= 0 and < AreaAWidth;

    private readonly string _text;
    private readonly int _indicatorAt;      // the index of the indicator area, or _text.Length when the line ends before it
    private readonly int _programTextAt;    // the index of the first character of the program-text area
    private readonly int _marginRAt;        // the index just past the program-text area

    public FixedFormLine(string text)
    {
        _text = text;
        _indicatorAt = CharacterPositions.IndexAt(text, IndicatorColumn);
        _programTextAt = _indicatorAt >= text.Length ? text.Length : CharacterPositions.IndexAt(text.AsSpan(_indicatorAt), 1) + _indicatorAt;
        _marginRAt = CharacterPositions.IndexAt(text.AsSpan(_programTextAt), SourceAreaWidth) + _programTextAt;
    }

    /// <summary>The sequence number area: positions 1–6, or as many as the line has.</summary>
    public ReadOnlySpan<char> SequenceArea => _text.AsSpan(0, _indicatorAt);

    /// <summary>Whether the line reaches the indicator area (it has at least seven positions).</summary>
    public bool HasIndicator => _indicatorAt < _text.Length;

    /// <summary>The character in the indicator area — a space when the line ends before it.</summary>
    public Rune Indicator
    {
        get
        {
            if (!HasIndicator) return new Rune(' ');
            return Rune.TryGetRuneAt(_text, _indicatorAt, out var rune) ? rune : Rune.ReplacementChar;
        }
    }

    /// <summary>Whether the line holds any position of the program-text area (it has at least eight positions) — the line
    /// a compiler directive can stand on (§7.3.3 SR3).</summary>
    public bool HasProgramText => _programTextAt < _text.Length;

    /// <summary>The program-text area: positions 8 through margin R, a shorter line read as if space-filled to margin R
    /// (DOC-A.1-157), so it always holds exactly <see cref="SourceAreaWidth"/> positions.</summary>
    public string ProgramText
    {
        get
        {
            var area = _text.AsSpan(_programTextAt, _marginRAt - _programTextAt);
            int missing = SourceAreaWidth - CharacterPositions.Count(area);
            return missing == 0 ? area.ToString() : string.Concat(area, new string(' ', missing));
        }
    }

    /// <summary>The index in the line of the first character of <see cref="ProgramText"/> — where a resultant column of
    /// the program text maps back to (kb/Work PB1801). A line too short to reach the area has its program text wholly
    /// space-filled, read as if the line went on to position 8.</summary>
    public int ProgramTextIndex => HasProgramText ? _programTextAt : _programTextAt + SourceAreaStart - CharacterPositions.Count(_text);

    /// <summary>Where the line's program text BEGINS: the 0-based offset, in <see cref="ProgramText"/>, of the first
    /// character of the program-text area that is not a space, or -1 when the area holds only spaces. The characters
    /// before it are spaces, so the offset is also the character position past position 8 (<see cref="CharacterPositions"/>).</summary>
    public int ProgramTextStart =>
        _text.AsSpan(_programTextAt, _marginRAt - _programTextAt).IndexOfAnyExcept(CobolSpace.Separators);

    /// <summary>Whether the line's program text begins in area A (<see cref="AreaAWidth"/>): its first character that is
    /// not a space stands in positions 8–11. False for a line whose program-text area holds only spaces.</summary>
    public bool BeginsInAreaA => IsAreaAOffset(ProgramTextStart);

    /// <summary>The text past margin R, which is ignored (Annex A item 158).</summary>
    public ReadOnlySpan<char> BeyondMarginR => _text.AsSpan(_marginRAt);
}
