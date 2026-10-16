// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>Where a text stands with respect to the AREAS of a fixed-form line (kb/Work PB3147). ANSI X3.23-1985 split
/// the program-text area into area A (positions 8–11, <see cref="FixedFormLine.AreaAWidth"/>) and area B (positions 12 to
/// margin R) and placed every item by where it BEGINS ITS LINE; ISO/IEC 1989:2002 removed the split (ISO §6.3.1), so only
/// a COBOL-85 reading asks.</summary>
public enum AreaPlacement
{
    /// <summary>The text has no area: its line was read in free form (§6.4.1), or no source line holds it.</summary>
    None,

    /// <summary>The text is the first text of its fixed-form line, and it begins in area A.</summary>
    BeginsLineInAreaA,

    /// <summary>The text is the first text of its fixed-form line, and it begins in area B.</summary>
    BeginsLineInAreaB,

    /// <summary>The text is the first text of a fixed-form CONTINUATION line (a hyphen in the indicator area), and it
    /// begins in area A. X3.23-1985: "Area A of a continuation line must be blank" - a rule the §6.5 logical conversion
    /// screens itself (kb/Work PB3168), because it alone sees the indicator and a continued literal's tail is no token's
    /// start; the area pass leaves such a token to it.</summary>
    BeginsContinuationLineInAreaA,

    /// <summary>The text follows other text of its fixed-form line.</summary>
    WithinLine,
}

/// <summary>
/// THE AREA OF A RESULTANT POSITION (kb/Work PB3147): the one composition of the compilation's resultant-line map with
/// the reference-format map of every text it read. A token of the parsed text is located through
/// <see cref="SourceLineMap.Locate"/> — the physical file, line and column the user wrote it at, through COPY, REPLACE
/// and the fixed-form logical conversion alike — and that file's <see cref="ReferenceFormatMap.PlacementAt"/> says
/// whether the token begins its fixed-form line in area A or area B. Built by the front end for the parse it returns
/// (<see cref="Frontend.Areas"/>); the COBOL-85 area-placement rules are its consumer.
/// </summary>
public sealed class FixedFormAreas(SourceLineMap lineMap, IReadOnlyDictionary<string, ReferenceFormatMap> formats)
{
    /// <summary>No text has an area — the map of a compilation that read no fixed-form text.</summary>
    public static FixedFormAreas None { get; } = new(SourceLineMap.Identity("<source>", 0), new Dictionary<string, ReferenceFormatMap>());

    /// <summary>Where the text at 0-based resultant <paramref name="column"/> of 1-based resultant
    /// <paramref name="line"/> (an ANTLR token's position) stands with respect to the areas of its physical line.</summary>
    public AreaPlacement PlacementAt(int line, int column)
    {
        var at = lineMap.Locate(line, column, fallbackFile: "");
        return formats.TryGetValue(at.FileName, out var map) ? map.PlacementAt(at.Line + 1, at.Column) : AreaPlacement.None;
    }
}
