// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;

namespace CobolNet.Frontend.Preprocessor;

public static partial class ReferenceFormatProcessor
{
    /// <summary>Convert fixed-form source to free-form by the ISO §6.5 logical conversion (see
    /// <see cref="FixedFormConverter"/>): one resultant line per physical line, each ending in <c>\n</c>, except a
    /// continuation line, which is appended to the latest logical line.</summary>
    public static string ConvertFixedToFree(string sourceText)
        => string.Concat(new FixedFormConverter(gates: null, lineOffset: 0).Convert(sourceText).Lines.Select(l => l + "\n"));

    /// <summary>The MAPPED fixed→free conversion of a whole text originating in <paramref name="file"/> (kb/Work PB82).</summary>
    public static MappedText ConvertFixedToFreeMapped(string sourceText, string file)
    {
        var (l, o) = ConvertFixedToFreeMapped(sourceText, gates: null, 0);
        return Mapped(l, o, file);
    }

    /// <summary>The MAPPED fixed→free conversion (kb/Work PB82): the resultant lines and, per resultant line, the
    /// 1-based physical source line it came from — a continuation line joins its latest logical line, which keeps
    /// the number of the line that began it.</summary>
    /// <param name="lineOffset">The file-relative index (0-based) of this text's first line — nonzero when
    /// converting one SOURCE-FORMAT SEGMENT of a larger file, so the <see cref="ReferenceFormatDiagnostics"/> continuation
    /// diagnostics and the origins report the file line, not the segment-relative one.</param>
    private static (List<string> Lines, List<int> Origins) ConvertFixedToFreeMapped(string sourceText, ReferenceFormatDiagnostics? gates, int lineOffset)
        => new FixedFormConverter(gates, lineOffset).Convert(sourceText);

    /// <summary>
    /// THE ISO §6.5 LOGICAL CONVERSION of one fixed-form text — a whole file, one SOURCE FORMAT segment, or a
    /// copybook (kb/Work PB1491). The resultant compilation group is built as a list of lines, one per physical
    /// line so every later stage keeps the physical numbering (kb/Work PB82), and the continuation join is made
    /// against the LATEST LOGICAL LINE, which is not necessarily the last line written:
    /// <list type="bullet">
    /// <item>§6.5 2) "If the line is a comment line or a blank line, that line is logically discarded." A comment
    /// line (fixed <c>*</c> or <c>/</c> indicator, or a line whose first character-string is the floating comment
    /// indicator) and a blank line (§6.3.6) keep their slot as an EMPTY line, but they are never the join target and
    /// never touch the state of a literal left open by a continued line — §6.3.5: "Comment lines and blank lines may
    /// be interspersed among lines containing the parts of a literal", and "the next line that is not a comment line
    /// or a blank line is the continuation line". Because the comment is gone, no later stage (the lexer's picture
    /// mode, the text-manipulation scanners) ever sees comment-text.</item>
    /// <item>§6.5 3) "If the line contains an inline comment, the inline comment is replaced by spaces and processing
    /// of that line continues" — on EVERY line, a continuation line included, so a continuation appended after it
    /// is never swallowed by the comment (<see cref="ScanProgramText"/> finds it with the literal state carried in).</item>
    /// <item>§6.5 6) a) / b): a continuation line's program-text area is "appended immediately to the right of the
    /// last character in the latest logical line of the resultant compilation group" — for a continued literal from
    /// the character after the continuation's initial quotation symbol, otherwise from its first non-space
    /// character (the logical line's trailing spaces are not program text then, which is what lets a word be
    /// continued).</item>
    /// <item>The program-text area is ALWAYS character positions 8–72: a physical line shorter than margin R is read
    /// as if space-filled to it (docs/CONFORMANCE.md DOC-A.1-157, §6.1 1) c) "The implementor shall specify the
    /// meaning of lines and character positions"), so a literal continued by the fixed continuation indicator
    /// carries every position up to margin R — §6.3.5: "any spaces at the end of the fixed-form continued line are
    /// part of the literal".</item>
    /// </list>
    /// </summary>
    private sealed class FixedFormConverter(ReferenceFormatDiagnostics? gates, int lineOffset)
    {
        private readonly List<string> _lines = [];
        private readonly List<int> _origins = [];

        /// <summary>The index in <see cref="_lines"/> of the latest logical line — the §6.5 6) join target — or -1
        /// before the first one.</summary>
        private int _latest = -1;

        /// <summary>The literal state at the end of the latest logical line (carried across discarded lines).</summary>
        private LiteralState _literal;

        /// <summary>True while inside an obsolete IDENTIFICATION comment-entry paragraph (AUTHOR/INSTALLATION/etc.):
        /// its free-text body is commentary until the next Area-A header. See <see cref="CommentEntryParagraphs"/>.</summary>
        private bool _inCommentEntry;

        public (List<string> Lines, List<int> Origins) Convert(string sourceText)
        {
            int lineNo = lineOffset;
            foreach (var rawLine in sourceText.Split('\n'))
                ConvertLine(rawLine.TrimEnd('\r'), ++lineNo);
            return (_lines, _origins);
        }

        private void ConvertLine(string line, int lineNo)
        {
            char indicator = line.Length > IndicatorColumn ? line[IndicatorColumn] : ' ';
            string area = ProgramTextArea(line);
            if (IsCommentEntryText(indicator, area))
            {
                Discard(lineNo);
                return;
            }

            switch (indicator)
            {
                case '*' or '/':   // a comment line (§6.2.2 fixed comment indicators; §6.5 2))
                    Discard(lineNo);
                    break;

                case 'D' or 'd' or 'S' or 's' or 'Y' or 'y':
                    Emit(DebugLineCarrier + area.TrimEnd(), lineNo, LiteralState.Outside);
                    break;

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
                case 'P' or 'p' or 'J' or 'j' or 'H' or 'h' or 'E' or 'e' or 'U' or 'u':
                    Discard(lineNo);
                    break;

                case '-':
                    Continue(area, lineNo);
                    break;

                default:
                    Source(area, lineNo);
                    break;
            }
        }

        /// <summary>A line with a source indicator that is not a continuation line: §6.5 5) copies its program-text
        /// area to the resultant compilation group as a NEW logical line — after §6.5 2) (a blank line, or a comment
        /// line whose first character-string is the floating comment indicator, §6.2.3.1 1), is discarded) and
        /// §6.5 3) (an inline comment is removed). Trailing spaces are kept only while a literal is left open,
        /// where they are part of it (§6.3.5).</summary>
        private void Source(string area, int lineNo)
        {
            var state = LiteralState.Outside;
            int comment = ScanProgramText(area, ref state);
            if (comment >= 0) FloatingComment(gates, area, comment, lineNo, SourceAreaStart, fixedForm: true);
            string text = comment >= 0 ? area[..comment] : area;
            if (string.IsNullOrWhiteSpace(text))
            {
                Discard(lineNo);
                return;
            }
            Emit(state.InLiteral ? text : text.TrimEnd(), lineNo, state);
        }

        /// <summary>A fixed-form continuation line (§6.5 6)): its program text is appended to the LATEST LOGICAL line
        /// and the line occupies no resultant line of its own.</summary>
        private void Continue(string area, int lineNo)
        {
            gates?.OnContinuation(lineNo);
            int first = FirstNonSpace(area);
            if (first < 0) return;                          // no program text to append
            if (area.AsSpan(first).StartsWith("*>", StringComparison.Ordinal))
            {
                gates?.OnFloatingComment(lineNo, SourceAreaStart + first);
                Discard(lineNo);                            // a comment line (§6.2.3.1 1)): discarded, not a join
                return;
            }
            if (_latest < 0)
            {
                Source(area, lineNo);                       // nothing precedes it to continue
                return;
            }

            string head = _lines[_latest];
            var state = _literal;
            string content;
            int scanFrom = 0;
            if (!state.InLiteral)
            {
                // §6.5 6) b): "the content of the program-text area, beginning with the first non-space character, is
                // appended immediately to the right of the last character in the latest logical line". The WORD-
                // continuation shape (removed 2023, VCR row 2) is a splice joining two COBOL word characters;
                // literal continuation and non-word splices (after a period or parenthesis) are not word continuation.
                if (gates is not null && LastNonSpace(head) is { } prev)
                {
                    if (IsCobolWordChar(prev) && IsCobolWordChar(area[first]))
                        gates.OnWordContinuation(lineNo);
                    // §6.2.3.2 SR3 / §6.3.5: "All characters composing any multiple-character separator or
                    // multiple-character indicator shall be specified on the same line" — the join would SPELL a
                    // floating indicator nobody wrote on one line.
                    foreach (var indicator in MultipleCharacterFloatingIndicators)
                        if (prev == indicator[0] && area.AsSpan(first).StartsWith(indicator.AsSpan(1), StringComparison.Ordinal))
                            gates.OnSplitFloatingIndicator(lineNo, indicator);
                }
                head = head.TrimEnd();
                content = area[first..];
            }
            else if (area[first] is not ('"' or '\''))
            {
                content = area[first..];                    // no quotation symbol to strip (kb/Work PB1492 validates)
            }
            else if (state.PendingClose)
            {
                // The continued line ended on its literal's quotation symbol at margin R: the continuation's first
                // quotation symbol is the SECOND HALF of a doubled quotation symbol ("" — content), and a quotation
                // symbol right after it is the continuation's own opening one, which is stripped.
                content = first + 1 < area.Length && area[first + 1] is '"' or '\''
                    ? area[first] + area[(first + 2)..]
                    : area[first..];
                state = state with { PendingClose = false };
                scanFrom = 1;
            }
            else
            {
                content = area[(first + 1)..];              // §6.5 6) a): after the initial quotation symbol
            }

            int comment = ScanProgramText(content.AsSpan(scanFrom), ref state);   // §6.5 3), literal state carried in
            if (comment >= 0)
            {
                comment += scanFrom;
                FloatingComment(gates, content, comment, lineNo, SourceAreaStart + area.Length - content.Length, fixedForm: true);
                content = content[..comment];
            }
            string joined = head + content;
            _lines[_latest] = state.InLiteral ? joined : joined.TrimEnd();
            _literal = state;
        }

        /// <summary>Whether this line belongs to an obsolete IDENTIFICATION comment-entry paragraph (the header line
        /// itself, or its Area-B body) — commentary, discarded like a comment line. A comment, debugging or
        /// excluded-alternate line is processed by its own arm; a continuation line inside the paragraph continues
        /// its commentary.</summary>
        private bool IsCommentEntryText(char indicator, string area)
        {
            if (indicator == '-') return _inCommentEntry;   // a continuation inside the paragraph continues its text
            if (indicator is '*' or '/' or 'D' or 'd' or 'S' or 's' or 'Y' or 'y'
                    or 'P' or 'p' or 'J' or 'j' or 'H' or 'h' or 'E' or 'e' or 'U' or 'u')
                return false;
            string? firstAreaAWord = FirstAreaAWord(area);
            if (firstAreaAWord is not null && CommentEntryParagraphs.Contains(firstAreaAWord))
                return _inCommentEntry = true;              // start (or continue, back-to-back) a comment-entry
            if (_inCommentEntry && firstAreaAWord is not null)
                _inCommentEntry = false;                    // the next Area-A header ends it
            return _inCommentEntry;
        }

        /// <summary>A new logical line: the §6.5 6) join target from now on.</summary>
        private void Emit(string text, int lineNo, LiteralState state)
        {
            _lines.Add(text);
            _origins.Add(lineNo);
            _latest = _lines.Count - 1;
            _literal = state;
        }

        /// <summary>A logically discarded line (§6.5 2)): its slot stays, empty, so later lines keep their numbers;
        /// the latest logical line and its literal state are untouched.</summary>
        private void Discard(int lineNo)
        {
            _lines.Add("");
            _origins.Add(lineNo);
        }
    }

    /// <summary>The §6.5 logical conversion of a whole free-form text: line for line (a free-form line is "copied to the
    /// resultant compilation group", §6.5 7)), after its comment is removed by <see cref="ConvertFreeLine"/>. The
    /// line count never changes, so the identity line map still holds.</summary>
    private static MappedText ConvertFreeFormMapped(string sourceText, ReferenceFormatDiagnostics? diagnostics, string sourcePath)
    {
        var lines = sourceText.Split('\n');
        bool changed = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string converted = ConvertFreeLine(lines[i], i + 1, diagnostics);
            if (ReferenceEquals(converted, lines[i])) continue;
            lines[i] = converted;
            changed = true;
        }
        return MappedText.Identity(changed ? string.Join('\n', lines) : sourceText, sourcePath);
    }

    /// <summary>One free-form line through §6.5 2) and 3): a comment line (the floating comment indicator as its first
    /// character-string, §6.4.4.2) becomes an empty line, and an inline comment (§6.4.4.3) is removed — so, as in
    /// fixed form, no later stage sees comment-text. Returns <paramref name="line"/> itself when it has no comment.</summary>
    private static string ConvertFreeLine(string line, int lineNo, ReferenceFormatDiagnostics? diagnostics)
    {
        var state = LiteralState.Outside;
        int comment = ScanProgramText(line, ref state);
        if (comment < 0) return line;
        FloatingComment(diagnostics, line, comment, lineNo, column: 0, fixedForm: false);
        return line[..comment].TrimEnd();
    }

    /// <summary>The multiple-character floating indicators whose characters are never literal content (§6.2.3.1): the
    /// comment indicator and the compiler directive indicator. §6.2.3.2 SR3 requires each on one line.</summary>
    private static readonly string[] MultipleCharacterFloatingIndicators = ["*>", ">>"];

    /// <summary>A floating comment indicator recognized at <paramref name="at"/> of <paramref name="text"/> (whose
    /// first character is at 0-based <paramref name="column"/> of the physical line): the introduction gate, and
    /// §6.2.3.2 SR2 "The floating comment indicator of an inline comment shall be preceded by a separator space".
    /// The start of the program text is preceded by an implied space — §6.3.5 "If there is no fixed continuation
    /// indicator in a line, a space is implied before the first nonblank character in the line", and in free form
    /// §6.4.2 "The last nonblank character of each line is treated as if it were followed by a space".
    /// <para>The introduction gate is asked in FIXED form only. Fixed form is the one reference format a COBOL-85
    /// source can be written in, and there the fixed indicators <c>*</c> and <c>/</c> are its comments; free form is
    /// itself a COBOL-2002 introduction that WiseOwl COBOL reaches below 2002 only through its documented
    /// auto-detection extension (docs/CONFORMANCE.md DOC-A.1-158), and a free-form source has no comment but the
    /// floating one, so gating it there would gate the extension's only comment rather than a construct.</para></summary>
    private static void FloatingComment(ReferenceFormatDiagnostics? diagnostics, string text, int at, int lineNo, int column,
        bool fixedForm)
    {
        if (diagnostics is null) return;
        if (fixedForm) diagnostics.OnFloatingComment(lineNo, column + at);
        if (at > 0 && text[at - 1] is not (' ' or '\t'))
            diagnostics.OnUnseparatedFloatingComment(lineNo, column + at);
    }

    /// <summary>The program-text area of a fixed-form line — character positions 8 through 72 (margin R, Annex A
    /// item 158), a shorter line read as if space-filled to margin R (DOC-A.1-157).</summary>
    private static string ProgramTextArea(string line)
        => line.Length >= MarginR ? line[SourceAreaStart..MarginR]
            : line.Length > SourceAreaStart ? line[SourceAreaStart..].PadRight(SourceAreaWidth)
            : new string(' ', SourceAreaWidth);

    /// <summary>The literal state at a point of program text: outside any literal (<see cref="Quote"/> is NUL), or
    /// inside one whose opening delimiter was <see cref="Quote"/>. <see cref="PendingClose"/> is set when the last
    /// character scanned was that quotation symbol, which either closes the literal or is the first half of a doubled
    /// quotation symbol — the next character decides, and in fixed form it may be on the continuation line.</summary>
    private readonly record struct LiteralState(char Quote, bool PendingClose)
    {
        public static LiteralState Outside => default;
        public bool InLiteral => Quote != '\0';
    }

    /// <summary>
    /// THE ONE literal-aware scan of a piece of program text (kb/Work PB1491): starting in <paramref name="state"/>,
    /// return the index of the floating comment indicator <c>*&gt;</c> that begins an inline comment or comment line
    /// (§6.2.3.1) outside any literal — or -1 — leaving <paramref name="state"/> as it is at that index (or at the end
    /// of the text). A literal ends only at the quotation symbol that opened it, a doubled one being content, so a
    /// quotation symbol or <c>*&gt;</c> inside a literal is literal content, and a quotation symbol inside a comment
    /// never opens one.
    /// </summary>
    private static int ScanProgramText(ReadOnlySpan<char> text, ref LiteralState state)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (state.PendingClose)
            {
                if (c == state.Quote)
                {
                    state = state with { PendingClose = false };   // a doubled quotation symbol: content
                    continue;
                }
                state = LiteralState.Outside;                       // the quotation symbol closed the literal
            }
            if (state.InLiteral)
            {
                if (c == state.Quote) state = state with { PendingClose = true };
            }
            else if (c is '"' or '\'')
                state = new LiteralState(c, false);
            else if (c == '*' && i + 1 < text.Length && text[i + 1] == '>')
                return i;
        }
        return -1;
    }

    /// <summary>The index of the first non-space character of <paramref name="s"/>, or -1.</summary>
    private static int FirstNonSpace(string s)
    {
        for (int i = 0; i < s.Length; i++)
            if (s[i] != ' ') return i;
        return -1;
    }

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

    /// <summary>A COBOL word-forming character (§8.3.1 — letters, digits, hyphen; the underscore joined at
    /// 2002 and rides the same splice rule).</summary>
    private static bool IsCobolWordChar(char c) => char.IsLetterOrDigit(c) || c is '-' or '_';

    /// <summary>The last non-space character of a logical line (the splice's LEFT side), or null when it has none.</summary>
    private static char? LastNonSpace(string line)
    {
        for (int i = line.Length - 1; i >= 0; i--)
            if (line[i] != ' ') return line[i];
        return null;
    }
}
