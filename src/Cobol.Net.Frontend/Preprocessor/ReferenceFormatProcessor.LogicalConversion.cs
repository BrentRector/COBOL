// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;

namespace CobolNet.Frontend.Preprocessor;

public static partial class ReferenceFormatProcessor
{
    /// <summary>
    /// The fixed-form CLASSIFIER of the ISO §6.5 logical conversion of one fixed-form text — a whole file, one SOURCE
    /// FORMAT segment, or a copybook (kb/Work PB1491). It reads the indicator area and the program-text area of each
    /// physical line and hands the program text to the <see cref="LogicalLineBuilder"/>, which owns the resultant lines,
    /// the latest logical line and the literal state (kb/Work PB1804):
    /// <list type="bullet">
    /// <item>§6.5 2) "If the line is a comment line or a blank line, that line is logically discarded" — a comment line is
    /// a fixed <c>*</c> or <c>/</c> indicator or a line whose first character-string is the floating comment indicator;
    /// the builder keeps its slot as an EMPTY line.</item>
    /// <item>The program-text area is ALWAYS character positions 8–72: a physical line shorter than margin R is read as if
    /// space-filled to it (docs/CONFORMANCE.md DOC-A.1-157, §6.1 1) c) "The implementor shall specify the meaning of lines
    /// and character positions"), so a literal continued by the fixed continuation indicator carries every position up
    /// to margin R — §6.3.5: "any spaces at the end of the fixed-form continued line are part of the literal".</item>
    /// </list>
    /// </summary>
    private sealed class FixedFormConverter(ReferenceFormatDiagnostics? gates, string file, bool ccvsIndicators,
        bool startsInIdentificationDivision)
    {
        private readonly LogicalLineBuilder _builder = new(file, gates, fixedForm: true);

        /// <summary>True while inside an obsolete IDENTIFICATION comment-entry paragraph (AUTHOR/INSTALLATION/etc.):
        /// its free-text body is commentary until the next Area-A header. See <see cref="CommentEntryParagraphs"/>.</summary>
        private bool _inCommentEntry;

        /// <summary>True while the text is in an IDENTIFICATION DIVISION — the only place a comment-entry paragraph
        /// exists (kb/Work PB1494: a PROCEDURE DIVISION paragraph named REMARKS was dropped with every line after it).
        /// A source text starts in one (a program's IDENTIFICATION DIVISION header is where it begins), library text in
        /// the division its COPY statement stands in (<see cref="DivisionCursor"/>), an IDENTIFICATION / ID DIVISION
        /// header or a <c>*-ID</c> paragraph re-enters it, and any other division header leaves it.</summary>
        private bool _inIdentificationDivision = startsInIdentificationDivision;

        /// <summary>Whether the text read so far ends in an IDENTIFICATION DIVISION — the state the next SOURCE FORMAT
        /// segment of the same text starts in.</summary>
        public bool InIdentificationDivision => _inIdentificationDivision;

        /// <summary>Convert the lines of one text — a whole file, one SOURCE FORMAT segment of it, or a copybook —
        /// returning the resultant lines and, per resultant line, the 1-based physical source line it came from (kb/Work
        /// PB82). Each <see cref="PhysicalLine.Number"/> is the file's own, so a segment reports file lines, never
        /// segment-relative ones.</summary>
        public (List<string> Lines, List<int> Origins) Convert(ReadOnlySpan<PhysicalLine> physicalLines)
        {
            foreach (var line in physicalLines)
                ConvertLine(line.Text, line.Number);
            return _builder.Result;
        }

        /// <summary>The kind of line an indicator-area character marks (§6.3.3 "The indicator area identifies the type
        /// of a source line in accordance with the indicators specified in 6.2.2"). The NIST CCVS conventions — letters
        /// §6.2.2 does not list — are a dialect, honored only under <c>--nist</c> (kb/Work PB1494).</summary>
        private enum LineKind { Source, Comment, Continuation, Debugging, CcvsExcluded, NotAnIndicator }

        private LineKind KindOf(char indicator) => indicator switch
        {
            ' ' => LineKind.Source,
            '*' or '/' => LineKind.Comment,
            '-' => LineKind.Continuation,
            'D' or 'd' => LineKind.Debugging,                   // COBOL-85's debugging line
            _ when !ccvsIndicators => LineKind.NotAnIndicator,
            'S' or 's' or 'Y' or 'y' or 'P' or 'p' or 'J' or 'j' or 'H' or 'h' or 'E' or 'e' or 'U' or 'u' => LineKind.CcvsExcluded,
            _ => LineKind.Source,                               // CCVS: a primary-configuration line
        };

        private void ConvertLine(string line, int lineNo)
        {
            char indicator = line.Length > IndicatorColumn ? line[IndicatorColumn] : ' ';
            string area = ProgramTextArea(line);
            var kind = KindOf(indicator);
            switch (CommentEntryOf(kind, area))
            {
                case CommentEntryText.Header:
                    // The paragraph HEADER stays program text — the parser and the one removal gate
                    // (VersionConformancePass, COBOLNET0902 from 2002) must see the paragraph in fixed form as they do in
                    // free form (kb/Work PB1494, PB1758) — and only the comment-entry is discarded. The entry's end is
                    // implied by the next Area-A word (COBOL-85), so the logical conversion writes the paragraph's
                    // terminating period explicitly: `AUTHOR. .` is the paragraph `AUTHOR DOT DOT` with no content.
                    _builder.Emit(CommentEntryHeader(area), lineNo);
                    return;
                case CommentEntryText.Body:
                    _builder.Discard(lineNo);
                    return;
            }

            switch (kind)
            {
                case LineKind.Comment:   // a comment line (§6.2.2 fixed comment indicators; §6.5 2))
                    _builder.Discard(lineNo);
                    break;

                case LineKind.Debugging:
                    // §6.2.2 lists no debugging indicator at COBOL-2023: the facility is obsolete at 2002 and removed at
                    // 2014 (kb/Work R61, row debugging-line-removed-2014). Whether the line is SOURCE or COMMENT is the
                    // program's SOURCE-COMPUTER ... WITH DEBUGGING MODE clause's to say, and the clause is further down the
                    // text than any stage that has read this line, so the line is CARRIED: DebuggingLineRewriter decides
                    // per source unit after lexing (kb/Work PB1705, owner decision R56). Its tokens stay matchable by COPY
                    // REPLACING / REPLACE as if the D were absent (TextWordScanner skips the carrier).
                    gates?.OnDebuggingLine(file, lineNo);
                    _builder.Emit(DebugLineCarrier + area.TrimEnd(' '), lineNo);
                    break;

                // 'S' and 'Y' (the CCVS debug-suite letters, §6.2.2 lists neither) are excluded the same way: they are the
                // --nist dialect, not the D indicator, so the WITH DEBUGGING MODE clause that keeps a D line (PB1705) never
                // keeps them — they were comment lines before the D line's carrier became a rewritable one, and stay so.
                //
                // CCVS optional/alternate source lines: the file-I/O suites tag auxiliary or
                // alternate-configuration lines in the indicator column — 'P' (an optional scratch
                // file such as the INDEXED RAW-DATA member, assigned to an X-card the program's
                // header does not declare) and 'J' (an alternate ASSIGN target beside the primary
                // space-indicator one). The primary configuration is the space-indicator lines, so
                // these alternates are excluded for a standard run (treated as comment lines).
                //
                // 'H' (CLOSE … REEL) and 'E' (CLOSE … UNIT) tag the multi-reel/multi-volume tape
                // feature (with REELUNIT counters) — an optional feature CobolSharp does not support;
                // the CCVS pairs each such block with a replacement line ('I' for REEL, 'F' for UNIT,
                // e.g. MOVE "CLOSE REEL DELETED" TO RE-MARK) that becomes the controlling IF's body
                // once the 'H'/'E' lines (which carry the period) are deleted. Excluding 'H'/'E' and
                // keeping the replacement (a normal line) yields the intended no-multi-volume program
                // — and stops CLOSE … REEL/UNIT from prematurely closing the file mid write-loop.
                // (Only 'H'/'E' are excluded; 'F' is also used as ordinary code in the IC suite.)
                //
                // 'T'/'U' are a matched ALTERNATE PAIR that completes an intentionally-incomplete record
                // layout (IX207A/IX208A): the base (space-indicator) FD record omits the key/alternate-key
                // filler bytes, and exactly ONE of the T or U variant lines supplies them — base+T and
                // base+U each total the declared RECORD length, but keeping BOTH overflows it and shifts the
                // key offsets away from the fixed-width FILE-RECORD-INFO work area the records are written
                // through. The test's own working-storage key images use the 'T' form, so 'T' is the active
                // configuration (kept as ordinary code) and 'U' is the excluded alternate.
                case LineKind.CcvsExcluded:
                    _builder.Discard(lineNo);
                    break;

                case LineKind.Continuation:
                    _builder.Add(area, lineNo, SourceAreaStart, fixedContinuation: true);
                    break;

                case LineKind.NotAnIndicator:
                    gates?.OnInvalidIndicator(file, lineNo, indicator);   // COBOLNET2616, then read as a source line
                    _builder.Add(area, lineNo, SourceAreaStart, fixedContinuation: false);
                    break;

                default:
                    _builder.Add(area, lineNo, SourceAreaStart, fixedContinuation: false);
                    break;
            }
        }

        /// <summary>What a line is to an obsolete IDENTIFICATION comment-entry paragraph (AUTHOR / INSTALLATION /
        /// DATE-WRITTEN / DATE-COMPILED / SECURITY / REMARKS): not part of one, the header line that opens it, or
        /// the Area-B body of its comment-entry.</summary>
        private enum CommentEntryText { None, Header, Body }

        /// <summary>Classify a line against the comment-entry paragraph in progress. A comment, debugging or
        /// excluded-alternate line is processed by its own arm; a continuation line inside the paragraph continues
        /// its commentary.</summary>
        private CommentEntryText CommentEntryOf(LineKind kind, string area)
        {
            if (kind == LineKind.Continuation) return _inCommentEntry ? CommentEntryText.Body : CommentEntryText.None;
            if (kind is not (LineKind.Source or LineKind.NotAnIndicator)) return CommentEntryText.None;
            string? firstAreaAWord = FirstAreaAWord(area);
            if (firstAreaAWord is not null && DivisionHeader(area, firstAreaAWord) is { } division)
            {
                _inIdentificationDivision = division is "IDENTIFICATION" or "ID";
                _inCommentEntry = false;
                return CommentEntryText.None;
            }
            if (firstAreaAWord is not null && firstAreaAWord.EndsWith("-ID", StringComparison.OrdinalIgnoreCase))
                _inIdentificationDivision = true;           // PROGRAM-ID / CLASS-ID / ... (the header is optional)
            if (_inIdentificationDivision && firstAreaAWord is not null && CommentEntryParagraphs.Contains(firstAreaAWord))
            {
                _inCommentEntry = true;                     // start (or continue, back-to-back) a comment-entry
                return CommentEntryText.Header;
            }
            if (_inCommentEntry && firstAreaAWord is not null)
                _inCommentEntry = false;                    // the next Area-A header ends it
            return _inCommentEntry ? CommentEntryText.Body : CommentEntryText.None;
        }

        /// <summary>The logical-conversion text of a comment-entry paragraph's header line: the paragraph word at its own
        /// position, its period, and the explicit terminating period that stands for the discarded comment-entry.</summary>
        private static string CommentEntryHeader(string area)
        {
            int start = FirstNonSpace(area);
            int end = start;
            while (end < area.Length && area[end] is not (' ' or '.')) end++;
            return area[..end] + ". .";
        }
    }

    /// <summary>The free-form CLASSIFIER of the ISO §6.5 logical conversion — a whole text or one SOURCE FORMAT segment
    /// of it. A free-form line is "copied to the resultant compilation group" (§6.5 7)) once its comment is removed
    /// (§6.5 2) and 3)); the only line that is not copied as a new logical line is the continuation line of a literal
    /// continued with a floating literal continuation indicator (§6.5 8), kb/Work PB1359), which the
    /// <see cref="LogicalLineBuilder"/> joins to its latest logical line. A discarded or joined line keeps its slot as
    /// an empty line only when discarded; each resultant line's origin is the physical line it was read from.</summary>
    private static (List<string> Lines, List<int> Origins) ConvertFreeLines(ReadOnlySpan<PhysicalLine> physicalLines,
        ReferenceFormatDiagnostics? gates, string file)
    {
        var builder = new LogicalLineBuilder(file, gates, fixedForm: false);
        foreach (var line in physicalLines)
        {
            if (line.Text.Length > FreeFormMaxPositions) gates?.OnFreeFormLineTooLong(file, line.Number, line.Text.Length);
            builder.Add(line.Text, line.Number, column: 0, fixedContinuation: false);
        }
        return builder.Result;
    }

    /// <summary>The most character positions a free-form line may have (§6.1 3) a): "ranging from a minimum of 0 to a
    /// maximum of 255") — positions as DOC-A.1-157 counts them, so on the expanded line. Asked in the free-form arm
    /// because a fixed-form line may run past margin R and only its program-text area counts.</summary>
    internal const int FreeFormMaxPositions = 255;

    /// <summary>The program-text area of a fixed-form line — character positions 8 through 72 (margin R, Annex A
    /// item 158), a shorter line read as if space-filled to margin R (DOC-A.1-157).</summary>
    private static string ProgramTextArea(string line)
        => line.Length >= MarginR ? line[SourceAreaStart..MarginR]
            : line.Length > SourceAreaStart ? line[SourceAreaStart..].PadRight(SourceAreaWidth)
            : new string(' ', SourceAreaWidth);

    /// <summary>
    /// If the source area begins with a word in Area A (its first non-space character falls within
    /// columns 8-11, i.e. the first <see cref="AreaAWidth"/> characters), return that word (the run of
    /// non-space, non-period characters), else null. Used to detect division/section/paragraph headers,
    /// which begin in Area A, versus Area-B continuation/free text, which is indented to column 12+.
    /// </summary>
    private static string? FirstAreaAWord(string sourceArea)
    {
        int start = 0;
        while (start < sourceArea.Length && sourceArea[start] == ' ') start++;
        if (start >= sourceArea.Length || start >= AreaAWidth)
            return null; // blank line, or text begins in Area B (not a header)
        int end = start;
        while (end < sourceArea.Length && sourceArea[end] is not (' ' or '.')) end++;
        return sourceArea[start..end];
    }

    /// <summary>The division a division header names — <paramref name="firstWord"/> when the Area-A text is
    /// <c>&lt;word&gt; DIVISION</c> — or null.</summary>
    private static string? DivisionHeader(string sourceArea, string firstWord)
    {
        int at = sourceArea.IndexOf(firstWord, StringComparison.Ordinal) + firstWord.Length;
        ReadOnlySpan<char> rest = sourceArea.AsSpan(at).TrimStart(' ');
        return rest.StartsWith("DIVISION", StringComparison.OrdinalIgnoreCase)
               && (rest.Length == 8 || rest[8] is ' ' or '.')
            ? firstWord.ToUpperInvariant() : null;
    }
}
