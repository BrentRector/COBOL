// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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

    /// <summary>The 0-based position of the indicator area (position 7).</summary>
    public const int IndicatorColumn = 6;

    /// <summary>The 0-based position where the program-text area begins (position 8).</summary>
    public const int SourceAreaStart = 7;

    /// <summary>Positions in the program-text area (positions 8–72).</summary>
    public const int SourceAreaWidth = 65;

    /// <summary>Margin R, the 0-based position just past the program-text area: our documented margin R is position 72
    /// (Annex A item 158 / docs/CONFORMANCE.md §7).</summary>
    public const int MarginR = SourceAreaStart + SourceAreaWidth;

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

    /// <summary>The text past margin R, which is ignored (Annex A item 158).</summary>
    public ReadOnlySpan<char> BeyondMarginR => _text.AsSpan(_marginRAt);
}
