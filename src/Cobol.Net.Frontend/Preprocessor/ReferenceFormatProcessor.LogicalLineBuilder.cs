// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;

namespace CobolNet.Frontend.Preprocessor;

public static partial class ReferenceFormatProcessor
{
    /// <summary>
    /// THE RESULTANT COMPILATION GROUP of the ISO §6.5 logical conversion (kb/Work PB1804): ONE builder both reference
    /// formats feed, each supplying only its per-line classifier (<see cref="FixedFormConverter"/> reads the indicator
    /// area, <see cref="ConvertFreeLines"/> takes the whole line). It owns what §6.5 makes STATE of the in-order walk —
    /// the resultant line list with its origins, the LATEST LOGICAL LINE (the join target of §6.5 6) and 8)) and the
    /// literal state that line ends in — so a continuation rule is written once for both formats:
    /// <list type="bullet">
    /// <item>§6.5 2) a comment line or blank line is logically discarded: it keeps its slot as an EMPTY line, is never the
    /// join target and never touches the literal state ("Comment lines and blank lines may be interspersed among
    /// lines containing the parts of a literal", §6.3.5 / §6.4.2).</item>
    /// <item>§6.5 3) an inline comment is replaced by spaces, on every line, a continuation line included.</item>
    /// <item>§6.5 4) a line ending in a floating literal continuation indicator ends its program text before the
    /// indicator; §6.5 8) the next line that is not a comment or blank line is its continuation line, whose content
    /// after its initial quotation symbol is appended to the latest logical line. Free form has no other way to continue a
    /// literal.</item>
    /// <item>§6.5 6) a fixed-form continuation line (hyphen in column 7) appends to the latest logical line — for a
    /// continued literal from the character after the continuation's initial quotation symbol, otherwise from its first
    /// non-space character.</item>
    /// <item>§6.2.3.2 SR4-SR6 and §6.3.5 2) are checks of this state: one form of continuation per literal, no floating
    /// indicator on a fixed continuation line, the first nonblank character of a continuation line is the opening
    /// quotation symbol, a national literal only by the floating form, no multiple-character token split by a join.</item>
    /// </list>
    /// </summary>
    private sealed class LogicalLineBuilder(string file, ReferenceFormatDiagnostics? gates, bool fixedForm)
    {
        private readonly List<string> _lines = [];
        private readonly List<List<OriginSpan>> _origins = [];   // per resultant line, where its characters came from (PB1801)

        /// <summary>The index in <see cref="_lines"/> of the latest logical line — the §6.5 6) / 8) join target — or -1
        /// before the first one.</summary>
        private int _latest = -1;

        /// <summary>The literal state at the end of the latest logical line (carried across discarded lines).</summary>
        private LiteralState _literal;

        /// <summary>Whether the latest logical line is a COMPILER DIRECTIVE line — the §7.3.3 SR2 indicator, then a
        /// compiler-directive word the catalog knows (the SAME recognition every directive stage uses). §7.3.3 SR1: a
        /// directive is specified on ONE line, so such a line is never a join target (kb/Work PB1360).</summary>
        private bool _latestIsDirective;

        /// <summary>The resultant lines and, per line, where its characters came from (kb/Work PB82, PB1801): the physical
        /// line and column of each stretch — a continuation line's text joins its latest logical line, which keeps the
        /// line that began it as its name, and the joined text keeps its own line and column.</summary>
        public (List<string> Lines, List<LineOrigin> Origins) Result
        {
            get
            {
                var origins = new List<LineOrigin>(_origins.Count);
                for (int i = 0; i < _origins.Count; i++)
                {
                    // A stretch a later scan cut away (an inline comment, trailing spaces) maps no character.
                    var spans = _origins[i];
                    int kept = 1;
                    while (kept < spans.Count && spans[kept].ResultColumn < _lines[i].Length) kept++;
                    origins.Add(new LineOrigin([.. spans.GetRange(0, kept)]));
                }
                return (_lines, origins);
            }
        }

        /// <summary>A logically discarded line (§6.5 2)): its slot stays, empty, so later lines keep their numbers; the
        /// latest logical line and its literal state are untouched.</summary>
        public void Discard(TextSite site)
        {
            _lines.Add("");
            _origins.Add([site.Span(file, 0, 0)]);
        }

        /// <summary>A new logical line the classifier built itself (a comment-entry paragraph header) from the program text
        /// at <paramref name="site"/>: the §6.5 6) join target from now on, outside any literal.</summary>
        public void Emit(string text, TextSite site) => Emit(text, site.Spans(file, 0, 0, text.Length), LiteralState.Outside, isDirective: false);

        /// <summary>A fixed-form DEBUGGING line (indicator <c>D</c>), carried as <paramref name="carrier"/> (the
        /// <see cref="DebugLineCarrier"/> and the line's program text) for <c>DebuggingLineRewriter</c> to keep or hide
        /// per source unit (docs/CONFORMANCE.md D-DEBUG; kb/Work PB1705). Which of the two it is is decided after this
        /// walk, so the walk must read the line so that BOTH readings agree on every OTHER line:
        /// <list type="bullet">
        /// <item>a line of spaces is a blank line (§6.5 2)): discarded, never a join target;</item>
        /// <item>a line written while the latest logical line is inside a literal that needs its continuation (an open
        /// literal, or one a floating indicator continues) is passed over like a comment line — "Comment lines and blank
        /// lines may be interspersed among lines containing the parts of a literal" (§6.3.5 2)) — so the continuation line
        /// after it still continues the literal (kb/Work PB1914; GnuCOBOL reads the line as a comment unless
        /// <c>-fdebugging-line</c>). It stays in the output as a logical line of its own, after the line it interrupts, and
        /// is NOTED to the gates: once the unit's WITH DEBUGGING MODE is known, a line that is source there is a source
        /// line between the parts of a literal, which GnuCOBOL rejects and which is diagnosed (COBOLNET3473; kb/Work PB2490);</item>
        /// <item>any other debugging line is a new logical line and the join target from now on, so a continuation line
        /// after it continues the debugging line ("continuation of debugging lines is permitted").</item>
        /// </list></summary>
        public void EmitDebugging(string carrier, string programText, TextSite site)
        {
            // The line is read like any source line (§6.5 3) and 4): an inline comment is removed, a floating literal
            // continuation indicator ends it, and the literal state it ends in is tracked), so a continuation line after
            // it finds the literal it left open.
            var state = LiteralState.Outside;
            string kept = Scan(programText, 0, ref state, site, fixedContinuation: false);
            if (CobolSpace.IsBlank(kept))
            {
                Discard(site);
                return;
            }
            string text = carrier + (state.InLiteral ? kept : kept.TrimSpacesEnd());
            var spans = site.Spans(file, carrier.Length, 0, text.Length - carrier.Length);   // the carrier is written, not read
            if (_latest >= 0 && (_literal.AwaitsFloating || _literal.InLiteral && !_literal.PendingClose))
            {
                gates?.NoteDebuggingLineBetweenLiteralParts(file, site.Line);
                _lines.Add(text);
                _origins.Add(spans);
                return;
            }
            Emit(text, spans, state, isDirective: false);
        }

        /// <summary>One line of program text. <paramref name="text"/> is the program-text area — characters 8 through
        /// margin R of a fixed-form line, space-filled to margin R (DOC-A.1-157), or a whole free-form line — and
        /// <paramref name="site"/> where its first character stands in its physical line.
        /// <paramref name="fixedContinuation"/> is a fixed-form line whose indicator area holds the fixed continuation
        /// indicator; every other line is a source line, which is a continuation line all the same when the latest
        /// logical line ended in a floating literal continuation indicator (§6.5 8)).</summary>
        public void Add(string text, TextSite site, bool fixedContinuation)
        {
            if (fixedContinuation) gates?.OnContinuation(file, site.Line);
            int first = FirstNonSpace(text);
            if (first < 0)
            {
                if (!fixedContinuation) Discard(site);   // a blank line (§6.3.6): discarded; a blank continuation has no text to append
                return;
            }
            if (CobolSourceText.CommentIndicatorAt(text, first))
            {
                ReportComment(text, first, site);
                Discard(site);                           // a comment line (§6.2.3.1 1)): discarded, never a join
                return;
            }
            // X3.23-1985: "Area A of a continuation line must be blank" (kb/Work PB3168). A continuation line that continues
            // a directive is dropped below and diagnosed there.
            if (fixedContinuation && FixedFormLine.IsAreaAOffset(first) && !(_latest >= 0 && _latestIsDirective))
                gates?.OnContinuationTextInAreaA(file, site.Line, site.Col(first));
            if (_latest >= 0 && _latestIsDirective && (fixedContinuation || _literal.AwaitsFloating))
                DropDirectiveContinuation(site, site.Col(first));
            else if (_latest >= 0 && _literal.AwaitsFloating) ContinueFloating(text, first, site, fixedContinuation);
            else if (_latest >= 0 && fixedContinuation) ContinueFixed(text, first, site);
            else NewLine(text, site);
        }

        /// <summary>§7.3.3 SR1: a line that would continue a compiler directive line — a fixed-form hyphen in column 7, or
        /// the line after a floating literal continuation indicator that ended the directive — is diagnosed and NOT
        /// joined, so the directive keeps exactly what its own line says; the line takes its discarded slot, like a
        /// comment. A literal the directive left open is closed with it.</summary>
        private void DropDirectiveContinuation(TextSite site, int column)
        {
            gates?.OnDirectiveContinued(file, site.Line, column);
            _literal = LiteralState.Outside;
            Discard(site);
        }

        /// <summary>§6.5 5) / 7): the program text is copied to the resultant group as a NEW logical line — after §6.5 3)
        /// (an inline comment is removed) and §6.5 4) (a floating literal continuation indicator ends it). Trailing
        /// spaces are kept only while a literal is left open, where they are part of it (§6.3.5).</summary>
        private void NewLine(string text, TextSite site)
        {
            var state = LiteralState.Outside;
            string kept = Scan(text, 0, ref state, site, fixedContinuation: false);
            if (CobolSpace.IsBlank(kept))
            {
                Discard(site);
                return;
            }
            string line = state.InLiteral ? kept : kept.TrimSpacesEnd();
            Emit(line, site.Spans(file, 0, 0, line.Length), state, IsDirectiveLine(kept));
        }

        /// <summary>Whether program text is a compiler directive line (§7.3.3 SR2): the indicator preceded only by
        /// spaces, then a compiler-directive word.</summary>
        private static bool IsDirectiveLine(string text)
            => CompilerDirectiveLine.TryParse(text, out var directive) && CompilerDirectiveCatalog.IsDirective(directive.Word);

        /// <summary>§6.5 8): the continuation line of a literal continued with a floating indicator. Its first nonblank
        /// character shall be the quotation symbol of the opening delimiter (§6.2.3.2 SR6); the content after it is
        /// appended immediately to the right of the latest logical line, whose trailing spaces are literal content.</summary>
        private void ContinueFloating(string text, int first, TextSite site, bool fixedContinuation)
        {
            var state = _literal with { AwaitsFloating = false };
            // A hyphen in column 7 on the continuation line of a floating-continued literal is the SECOND form of
            // continuation of that literal (§6.2.3.2 SR4).
            if (fixedContinuation) gates?.OnTwoContinuationForms(file, site.Line, FixedFormLine.IndicatorColumn);
            int contentAt = AfterOpeningQuote(text, first, state.Quote, site);
            string content = text[contentAt..];
            if (!fixedForm && PartIsEmpty(content, state.Quote))
                gates?.OnEmptyLiteralPart(file, site.Line, site.Col(contentAt));
            Join(_lines[_latest], content, 0, state, site.At(contentAt), fixedContinuation);
        }

        /// <summary>§6.5 6): a fixed-form continuation line identified by the fixed continuation indicator.</summary>
        private void ContinueFixed(string area, int first, TextSite site)
        {
            string head = _lines[_latest];
            var state = _literal;
            char firstChar = area[first];
            // The continued line ended on its literal's quotation symbol at margin R and the continuation does not begin
            // with one: that quotation symbol CLOSED the literal, so this is the continuation of what follows it.
            if (state.PendingClose && firstChar != state.Quote) state = LiteralState.Outside;

            if (!state.InLiteral)
            {
                // §6.5 6) b): "the content of the program-text area, beginning with the first non-space character, is
                // appended immediately to the right of the last character in the latest logical line". The WORD-
                // continuation shape (removed 2023, VCR row 2) is a splice joining two COBOL word characters;
                // literal continuation and non-word splices (after a period or parenthesis) are not word continuation.
                if (gates is not null && LastNonSpace(head) is { } prev)
                {
                    if (IsCobolWordChar(prev) && IsCobolWordChar(firstChar)) gates.OnWordContinuation(file, site.Line);
                    // §6.2.3.2 SR3 / §6.3.5 2): "All characters composing any multiple-character separator or
                    // multiple-character indicator shall be specified on the same line" — the join would SPELL a
                    // token nobody wrote on one line.
                    foreach (var indicator in MultipleCharacterFloatingIndicators)
                        if (prev == indicator[0] && area.AsSpan(first).StartsWith(indicator.AsSpan(1), StringComparison.Ordinal))
                            gates.OnSplitFloatingIndicator(file, site.Line, indicator);
                    foreach (var token in MultipleCharacterSeparators)
                        if (prev == token[0] && area.AsSpan(first).StartsWith(token.AsSpan(1), StringComparison.Ordinal))
                            gates.OnSplitSeparator(file, site.Line, token);
                }
                Join(head.TrimSpacesEnd(), area[first..], 0, LiteralState.Outside, site.At(first), fixedContinuation: true);
                return;
            }

            // A continued literal. §6.3.5 2): "National literals may be continued only with a floating literal
            // continuation indicator"; §6.2.3.2 SR4: one form of continuation per literal.
            if (state.Opening.IsNational) gates?.OnNationalFixedContinuation(file, site.Line);
            if (state.Form == ContinuationForm.Floating) gates?.OnTwoContinuationForms(file, site.Line, FixedFormLine.IndicatorColumn);
            state = state with { Form = ContinuationForm.Fixed };

            if (state.PendingClose)
            {
                // The continued line ended on its literal's quotation symbol at margin R and the continuation's first
                // character is that quotation symbol: it is the SECOND HALF of a doubled quotation symbol (content), and a
                // quotation symbol right after it is the continuation's own opening one, which is stripped.
                // The stripped opening quotation symbol leaves a seam in the content: the text after it is at its own column.
                bool stripped = first + 1 < area.Length && area[first + 1] == state.Quote;
                string content = stripped ? area[first] + area[(first + 2)..] : area[first..];
                Join(head, content, 1, state with { PendingClose = false }, site.At(first), fixedContinuation: true,
                    seam: stripped ? (1, site.At(first + 2)) : null);
                return;
            }
            int contentAt = AfterOpeningQuote(area, first, state.Quote, site);
            Join(head, area[contentAt..], 0, state, site.At(contentAt), fixedContinuation: true);
        }

        /// <summary>The index in <paramref name="text"/> of the first character of a literal's continuation: the one after
        /// the continuation line's initial quotation symbol, which shall be <paramref name="quote"/> (§6.2.3.2 SR6; §6.3.5
        /// 2); §6.4.2). A different first character is diagnosed (COBOLNET2684) and read the way the standard's rule would
        /// have it were it correct: another quotation symbol is dropped like the right one, anything else is content.</summary>
        private int AfterOpeningQuote(string text, int first, char quote, TextSite site)
        {
            char c = text[first];
            if (c == quote) return first + 1;
            gates?.OnContinuationQuote(file, site.Line, site.Col(first), quote);
            return CobolSourceText.IsQuotationSymbol(c) ? first + 1 : first;
        }

        /// <summary>Append <paramref name="content"/> to <paramref name="head"/> (the latest logical line) after §6.5 3)
        /// and 4) have been applied to the appended text, scanned from <paramref name="scanFrom"/> with
        /// <paramref name="state"/> carried in. <paramref name="site"/> is where the content's first character stands —
        /// the appended text keeps its own physical line and column (kb/Work PB1801) — and <paramref name="seam"/>, when
        /// the content is not one run of its line, the index in it where the rest stands elsewhere.</summary>
        private void Join(string head, string content, int scanFrom, LiteralState state, TextSite site,
            bool fixedContinuation, (int At, TextSite Site)? seam = null)
        {
            string kept = Scan(content, scanFrom, ref state, site, fixedContinuation);
            string joined = head + kept;
            _lines[_latest] = state.InLiteral ? joined : joined.TrimSpacesEnd();
            // The stretches the head lost to a trim (or an earlier join that kept no character) map nothing now.
            var spans = _origins[_latest];
            for (int k = spans.Count - 1; k >= 0; k--)
                if (spans[k].ResultColumn >= head.Length && (k > 0 || head.Length == 0)) spans.RemoveAt(k);
            if (seam is { } cut && cut.At < kept.Length)
            {
                spans.AddRange(site.Spans(file, head.Length, 0, cut.At));
                spans.AddRange(cut.Site.Spans(file, head.Length + cut.At, 0, kept.Length - cut.At));
            }
            else spans.AddRange(site.Spans(file, head.Length, 0, kept.Length));
            _literal = state;
        }

        /// <summary>§6.5 3) and 4) over a piece of program text, scanned from <paramref name="scanFrom"/> with
        /// <paramref name="state"/> carried in and left as it is at the end of the piece: an inline comment is removed,
        /// and a floating literal continuation indicator ends the program text before it (the literal stays open for the
        /// next line, §6.5 8)). Returns the text that remains.</summary>
        private string Scan(string text, int scanFrom, ref LiteralState state, TextSite site, bool fixedContinuation)
        {
            var stop = ScanProgramText(text.AsSpan(scanFrom), ref state);
            int at = scanFrom + stop.At;
            switch (stop.Kind)
            {
                case ScanStopKind.Comment:
                    ReportComment(text, at, site);
                    return text[..at];
                case ScanStopKind.FloatingContinuation:
                    // The introduction gate is asked in FIXED form only, as the floating comment indicator's is (see
                    // ReportComment): free form is itself a 2002 introduction and this is its only continuation.
                    if (fixedForm) gates?.OnFloatingLiteralContinuation(file, site.Line, site.Col(at));
                    if (stop.CommentFollows) gates?.OnCommentAfterFloatingContinuation(file, site.Line, site.Col(at));
                    // Never on a fixed continuation line (§6.2.3.2 SR5). A literal continued BOTH ways (SR4) is found where
                    // the second form is written: by ContinueFloating (a hyphen in column 7 after a floating indicator) and
                    // by ContinueFixed (a hyphen after a literal a floating indicator continued before).
                    if (fixedContinuation) gates?.OnFloatingOnFixedContinuation(file, site.Line, site.Col(at));
                    // A literal opened in THIS text with no content before the indicator (a carried-in literal's part is
                    // ContinueFloating's PartIsEmpty).
                    if (!fixedForm && stop.LiteralStart >= 0 && scanFrom + stop.LiteralStart == at)
                        gates?.OnEmptyLiteralPart(file, site.Line, site.Col(at));
                    state = state with { AwaitsFloating = true, Form = ContinuationForm.Floating };
                    return text[..at];
                case ScanStopKind.Directive:
                    // §7.3.3 SR2: a compiler directive is preceded only by spaces. The directive and the rest of the line
                    // are not program text — they are what the author meant as a directive — so they are cut, and the
                    // parser does not add a second, nameless error for the stray indicator (kb/Work PB1690).
                    gates?.OnDirectiveAfterProgramText(file, site.Line, site.Col(at));
                    return text[..at];
                default:
                    return text;
            }
        }

        /// <summary>A comment indicator recognized at <paramref name="at"/> of <paramref name="text"/> (whose first
        /// character is at 0-based <paramref name="column"/> of the physical line): the introduction gate, and §6.2.3.2
        /// SR2 "The floating comment indicator of an inline comment shall be preceded by a separator space". The start of
        /// the program text is preceded by an implied space — §6.3.5 "If there is no fixed continuation indicator in a
        /// line, a space is implied before the first nonblank character in the line", and in free form §6.4.2 "The last
        /// nonblank character of each line is treated as if it were followed by a space".
        /// <para>The introduction gate is asked in FIXED form only. Fixed form is the one reference format a COBOL-85
        /// source can be written in, and there the fixed indicators <c>*</c> and <c>/</c> are its comments; free form is
        /// itself a COBOL-2002 introduction that WiseOwl COBOL reaches below 2002 only through its documented
        /// <c>--source-format free|auto</c> selection (docs/CONFORMANCE.md DOC-A.1-158; kb/Work PB1362), and a free-form
        /// source has no comment but the floating one, so gating it there would gate the extension's only comment rather
        /// than a construct.</para></summary>
        private void ReportComment(string text, int at, TextSite site)
        {
            if (gates is null) return;
            if (fixedForm) gates.OnFloatingComment(file, site.Line, site.Col(at));
            if (at > 0 && text[at - 1] != ' ') gates.OnUnseparatedFloatingComment(file, site.Line, site.Col(at));
        }

        /// <summary>A new logical line: the §6.5 6) / 8) join target from now on.</summary>
        private void Emit(string text, List<OriginSpan> spans, LiteralState state, bool isDirective)
        {
            _lines.Add(text);
            _origins.Add(spans);
            _latest = _lines.Count - 1;
            _literal = state;
            _latestIsDirective = isDirective;
        }
    }

    /// <summary>
    /// WHERE PROGRAM TEXT STANDS in its physical line (kb/Work PB1801): the text a classifier hands the
    /// <see cref="LogicalLineBuilder"/> — the program-text area of a fixed-form line, a whole free-form line — begins at
    /// index <see cref="Index"/> of the EXPANDED line of physical line <see cref="Line"/>, whose <see cref="Columns"/>
    /// (null when it held neither a tab nor a supplementary-plane character, so an index IS a position) lead back to the
    /// character positions the file holds. The one place an index of that text becomes a physical column: a diagnostic's
    /// (<see cref="Col"/>) and a resultant line's origin spans (<see cref="Spans"/>), so the two can never disagree, and
    /// the one column unit is the CHARACTER POSITION (<see cref="CharacterPositions"/>, kb/Work PB2983).
    /// </summary>
    private readonly record struct TextSite(int Line, int Index, ColumnMap? Columns)
    {
        /// <summary>The site of the text from its index <paramref name="i"/> on.</summary>
        public TextSite At(int i) => this with { Index = Index + i };

        /// <summary>The 0-based physical column of the character at index <paramref name="i"/> of the text — the column a
        /// diagnostic names: its character position in the line as the file holds it (a TAB counts one column, as in the
        /// user's editor, and a supplementary-plane character counts one, not two).</summary>
        public int Col(int i) => PhysicalLine.ColumnOf(Columns, Index + i);

        /// <summary>A one-span origin of text at <paramref name="i"/>, written at resultant column
        /// <paramref name="resultColumn"/>.</summary>
        public OriginSpan Span(string file, int resultColumn, int i)
            => new(resultColumn, new SourceOrigin(file, Line), Col(i));

        /// <summary>The origin spans of the <paramref name="length"/> characters of the text from <paramref name="i"/>
        /// on, written at resultant column <paramref name="resultColumn"/>: one span, or on a line that held a tab or a
        /// supplementary-plane character a new one wherever the physical columns stop running one for one (a tab's
        /// expansion all maps to the tab, and the second unit of a supplementary character to the character).</summary>
        public List<OriginSpan> Spans(string file, int resultColumn, int i, int length)
        {
            var spans = new List<OriginSpan> { Span(file, resultColumn, i) };
            if (Columns is null) return spans;
            for (int k = 1; k < length; k++)
                if (Col(i + k) != Col(i + k - 1) + 1)
                    spans.Add(Span(file, resultColumn + k, i + k));
            return spans;
        }
    }

    /// <summary>The multiple-character floating indicators whose characters are never literal content (§6.2.3.1): the
    /// comment indicator and the compiler directive indicator. §6.2.3.2 SR3 requires each on one line.</summary>
    private static readonly string[] MultipleCharacterFloatingIndicators =
        [CobolSourceText.CommentIndicator, CompilerDirectiveLine.Indicator];

    /// <summary>The multiple-character separator and the invocation operator (§8.3.5 6) the pseudo-text delimiter, §8.3.5
    /// 7) the invocation operator <c>::</c>) that §6.3.5 2) requires on one line.</summary>
    private static readonly string[] MultipleCharacterSeparators = ["==", "::"];

    /// <summary>The form of continuation a literal has been continued with so far (§6.2.3.2 SR4: "A given literal shall
    /// not be continued with more than one form of continuation").</summary>
    private enum ContinuationForm { None, Fixed, Floating }

    /// <summary>The literal state at a point of program text: outside any literal (<see cref="Quote"/> is NUL), or inside
    /// one whose opening delimiter was <see cref="Opening"/>, with quotation symbol <see cref="Quote"/> (§8.3.5 5); the
    /// opening delimiter says whether it is a national literal, which §6.3.5 2) lets only the floating form continue).
    /// <see cref="PendingClose"/> is set when the last character
    /// scanned was that quotation symbol, which either closes the literal or is the first half of a doubled quotation
    /// symbol — the next character decides, and in fixed form it may be on the continuation line.
    /// <see cref="AwaitsFloating"/> is set when the line ended in a floating literal continuation indicator, so the
    /// next line that is not a comment or blank line is the literal's continuation line (§6.5 8)).</summary>
    private readonly record struct LiteralState(char Quote, bool PendingClose, OpeningDelimiter Opening, ContinuationForm Form,
        bool AwaitsFloating)
    {
        public static LiteralState Outside => default;
        public bool InLiteral => Quote != '\0';
    }

    /// <summary>What ended a literal-aware scan of program text: its end, a floating comment indicator (§6.2.3.1), a
    /// floating literal continuation indicator (§6.2.3.1), or a compiler directive written after program text
    /// (§7.3.3 SR2).</summary>
    private enum ScanStopKind { End, Comment, FloatingContinuation, Directive }

    /// <summary>Where and why <see cref="ScanProgramText"/> stopped. <see cref="At"/> is the index of the comment
    /// indicator, or of the quotation symbol of the continuation indicator, in the scanned text.
    /// <see cref="CommentFollows"/>: a comment indicator follows the continuation indicator on the line.
    /// <see cref="LiteralStart"/>: the index of the first content character of the literal the indicator continues, or -1
    /// when that literal was opened before the scanned text.</summary>
    private readonly record struct ScanStop(ScanStopKind Kind, int At, bool CommentFollows = false, int LiteralStart = 0);

    /// <summary>
    /// The §6.5 conversion's literal-aware scan of a piece of program text (kb/Work PB1491): starting in
    /// <paramref name="state"/>, find the first floating comment indicator <c>*&gt;</c> that begins an inline comment
    /// or comment line (§6.2.3.1) outside any literal, the floating literal continuation indicator — the opening
    /// quotation symbol followed by a hyphen — that ends the line inside a literal (§6.2.3.1, §6.5 4)), or a compiler
    /// directive written after program text; leaving <paramref name="state"/> as it is at that point (or at the end of
    /// the text). Where a literal opens and closes, and what a comment indicator is, are
    /// <see cref="CobolSourceText"/>'s (kb/Work PB2621): a literal ends only at the quotation symbol that opened it, a
    /// doubled one being content, so a quotation symbol or <c>*&gt;</c> inside a literal is literal content, and a
    /// quotation symbol inside a comment never opens one. What this scan adds is the state a literal carries ACROSS a
    /// line end: a quotation symbol that ends the text either closed its literal or is the first half of a doubled one,
    /// and the next text's first character decides (<see cref="LiteralState.PendingClose"/>).
    /// </summary>
    private static ScanStop ScanProgramText(ReadOnlySpan<char> text, ref LiteralState state)
    {
        int literalStart = state.InLiteral ? -1 : 0;   // -1: the literal was opened before this text
        int i = 0;
        if (state.PendingClose && text.Length > 0)
        {
            if (text[0] == state.Quote)
            {
                state = state with { PendingClose = false };   // a doubled quotation symbol: content
                i = 1;
            }
            else state = LiteralState.Outside;                  // the quotation symbol closed the literal
        }
        while (i < text.Length)
        {
            if (state.InLiteral)
            {
                int closing = CobolSourceText.ClosingDelimiterAt(text, i, state.Quote);
                if (closing == text.Length) break;              // the literal runs on past this text
                if (ContinuationIndicatorEnds(text, closing, out bool commentFollows))
                    return new ScanStop(ScanStopKind.FloatingContinuation, closing, commentFollows, literalStart);
                if (closing == text.Length - 1)
                {
                    state = state with { PendingClose = true }; // the next text's first character decides
                    break;
                }
                state = LiteralState.Outside;
                i = closing + 1;
            }
            else if (CobolSourceText.TryOpeningDelimiterAt(text, i, out var opening))
            {
                int quote = i + opening.PrefixLength;
                state = new LiteralState(text[quote], false, opening, ContinuationForm.None, false);
                literalStart = quote + 1;
                i = quote + 1;
            }
            else if (CobolSourceText.CommentIndicatorAt(text, i))
                return new ScanStop(ScanStopKind.Comment, i);
            else if (text[i] == '>' && i + 1 < text.Length && text[i + 1] == '>' && DirectiveAfterText(text, i))
                return new ScanStop(ScanStopKind.Directive, i);
            else i++;
        }
        return new ScanStop(ScanStopKind.End, text.Length);
    }

    /// <summary>Whether the compiler directive indicator at <paramref name="indicator"/> follows program text on its line
    /// and heads a compiler directive: the §6.2.3.1 indicator "followed by a compiler-directive word - with or without an
    /// intervening space", the word being one the directive catalog knows (the SAME recognition every directive stage uses,
    /// <see cref="CompilerDirectiveLine"/>). A directive line proper — the indicator preceded only by spaces — is left
    /// alone: it is §7.3's, not this pass's.</summary>
    private static bool DirectiveAfterText(ReadOnlySpan<char> text, int indicator)
    {
        // Program text before the indicator — but a line that BEGINS with a directive is a directive line: what follows
        // its word is that directive's operand or comment-text (§7.3.19.3 SR1: PAGE's "may contain any character"), so a
        // `>>` inside it is text, not a second directive.
        var before = text[..indicator].TrimStart(' ');
        return !before.IsEmpty && !before.StartsWith(CompilerDirectiveLine.Indicator, StringComparison.Ordinal)
               && CompilerDirectiveLine.TryParse(text[indicator..].ToString(), out var directive)
               && CompilerDirectiveCatalog.IsDirective(directive.Word);
    }

    /// <summary>Whether the quotation symbol at <paramref name="quote"/> and the hyphen after it are the floating literal
    /// continuation indicator that ends the line (§6.4.2: "The continuation indicator may optionally be followed by one
    /// or more spaces" — and, only to be diagnosed, by an inline comment, §6.3.7.3). A hyphen followed by anything else
    /// is a closed literal and a minus sign, which the separator rule of §8.3.5 5) reports.</summary>
    private static bool ContinuationIndicatorEnds(ReadOnlySpan<char> text, int quote, out bool commentFollows)
    {
        commentFollows = false;
        if (quote + 1 >= text.Length || text[quote + 1] != '-') return false;
        var rest = text[(quote + 2)..].TrimStart(' ');
        if (rest.IsEmpty) return true;
        return commentFollows = rest.StartsWith(CobolSourceText.CommentIndicator, StringComparison.Ordinal);
    }

    /// <summary>Whether a continuation line's content (the text after its initial quotation symbol) holds no literal
    /// content: it ends the literal at once, or continues it with the indicator at once (§6.4.2 "At least one … of the
    /// literal content shall be specified on the continued line and on each continuation line").</summary>
    private static bool PartIsEmpty(string content, char quote)
    {
        if (content.Length == 0) return true;
        if (content[0] != quote) return false;
        return content.Length == 1 || content[1] != quote;
    }

    /// <summary>The index of the first non-space character of <paramref name="s"/>, or -1.</summary>
    private static int FirstNonSpace(string s)
    {
        for (int i = 0; i < s.Length; i++)
            if (s[i] != ' ') return i;
        return -1;
    }

    /// <summary>A COBOL word-forming character (§8.3.2.1 — basic letters, digits, hyphen, the underscore joined at
    /// 2002, and the extended letters): the ONE lexical word class the lexer's NAME_BODY shares (kb/Work PB1402 — this
    /// read char.IsLetterOrDigit, which a combining mark of a word is not).</summary>
    private static bool IsCobolWordChar(char c) => Editions.CobolCharacterRepertoire.IsWordCharacter(c);

    /// <summary>The last non-space character of a logical line (the splice's LEFT side), or null when it has none.</summary>
    private static char? LastNonSpace(string line)
    {
        for (int i = line.Length - 1; i >= 0; i--)
            if (line[i] != ' ') return line[i];
        return null;
    }
}
