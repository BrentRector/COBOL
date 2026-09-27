// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// Detects and converts fixed-form COBOL reference format to free-form.
/// Fixed-form: columns 1-6 sequence, 7 indicator, 8-72 source, 73+ comment.
/// Free-form: no column restrictions.
/// </summary>
public static partial class ReferenceFormatProcessor
{
    /// <summary>How a fixed-form debugging line (indicator <c>D</c>) reaches the text-manipulation stage: as a comment
    /// to the lexer, but one whose text COPY REPLACING and REPLACE still match over (the COPY rule of COBOL-85 —
    /// text-words in a debugging line participate in matching as if the <c>D</c> were absent;
    /// <see cref="TextWordScanner"/> skips only this carrier). The carrier holds U+FDD0, a Unicode NONCHARACTER —
    /// reserved for process-internal use and never in interchanged text — so no comment a programmer writes can be
    /// mistaken for a debugging line (kb/Work PB1350: the printable <c>*&gt; DEBUG:</c> carrier made a genuine
    /// <c>*&gt; DEBUG: …</c> comment take part in matching).</summary>
    public static readonly string DebugLineCarrier = "*>" + (char)0xFDD0 + "DEBUG ";

    /// <summary>Length of the sequence number area (columns 1-6).</summary>
    private const int SequenceAreaLength = 6;

    /// <summary>Column index of the indicator area (column 7, zero-based index 6).</summary>
    private const int IndicatorColumn = 6;

    /// <summary>Column index where the source area begins (column 8, zero-based index 7).</summary>
    private const int SourceAreaStart = 7;

    /// <summary>Maximum width of the source area (columns 8-72 = 65 characters).</summary>
    private const int SourceAreaWidth = 65;

    /// <summary>Minimum percentage of lines that must match fixed-form pattern for detection.</summary>
    private const int FixedFormThresholdPercent = 60;

    /// <summary>Width of Area A (columns 8-11). A division/section/paragraph header begins here;
    /// continuation/free text of a comment-entry is indented into Area B (column 12+).</summary>
    private const int AreaAWidth = 4;

    /// <summary>
    /// The obsolete IDENTIFICATION DIVISION "comment-entry" paragraphs (ISO 1989:1985 §III; obsolete,
    /// removed in COBOL-2002). Their content is free-form commentary — any characters, including embedded
    /// periods, reserved words, numbers and quoted strings, spanning one or more lines until the next
    /// Area-A header. That free text cannot be reliably bounded by a token grammar (e.g. the FCTC address
    /// in RW101A's INSTALLATION contains "...AUTOMATED DATA AND..." and "5203 LEESBURG PIKE"), so we treat
    /// the whole paragraph as commentary in the column-aware preprocessor: comment out the header and its
    /// content up to the next Area-A header. The paragraphs are optional (identificationParagraph*), so the
    /// parser simply never sees them — no grammar change, no embedded-period/terminating-period edge cases.
    /// </summary>
    private static readonly HashSet<string> CommentEntryParagraphs = new(StringComparer.OrdinalIgnoreCase)
    {
        "AUTHOR", "INSTALLATION", "DATE-WRITTEN", "DATE-COMPILED", "SECURITY", "REMARKS",
    };

    /// <summary>
    /// Remove NIST CCVS archive-control lines ('*HEADER,…' and '*END-OF,…') that delimit
    /// members inside newcob.val. They begin in column 1 (the sequence area), so reference-format
    /// normalization would otherwise read column 7 as an indicator and emit the rest of the line
    /// (e.g. ',SM102A') as stray code. These markers are never valid COBOL, so blank them out of
    /// the raw text before any other processing. LINE-COUNT PRESERVING (kb/Work PB82): a marker becomes an
    /// empty line, so the source-line map the normalizer builds next still names the physical lines of the
    /// file on disk (dropping the header made every reported line one short).
    /// </summary>
    public static string StripNistArchiveMarkers(string sourceText)
    {
        var lines = sourceText.Split('\n');
        var kept = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            string trimmed = line.TrimStart();
            kept.Add(trimmed.StartsWith("*HEADER,", StringComparison.Ordinal) ||
                     trimmed.StartsWith("*END-OF,", StringComparison.Ordinal) ? "" : line);
        }
        return string.Join('\n', kept);
    }

    /// <summary>The compiler-directive word this stage owns (ISO §7.3.24; the <c>source-format-directive-2002</c>
    /// registry row's single <c>directiveWords</c> entry — FORMAT and IS are §5.2.3 optional words, not part of
    /// the word).</summary>
    private const string SourceFormatWord = "SOURCE";

    /// <summary>
    /// Auto-detect whether source is fixed-form or free-form, and normalize to free-form. Each
    /// <c>&gt;&gt;SOURCE FORMAT [IS] {FIXED|FREE}</c> directive (ISO §7.3.24) switches the reference format of the
    /// text that FOLLOWS it, up to the next directive — the source is partitioned into homogeneous-format segments
    /// (§7.3.24.3 GR1) and each is converted in its own format. The directive line is discarded (§6.5 logical
    /// conversion, step 1) and left as a blank line so downstream source-line numbers stay aligned.
    /// </summary>
    public static string NormalizeToFreeForm(string sourceText)
        => NormalizeToFreeForm(sourceText, dialectLevel: 85, permissive: false, diagnostics: null, sourcePath: "<source>");

    /// <summary>
    /// The edition-aware overload (W3 preprocessor threading, VCR rows 2/4/94 — DEVLOG 598): fixed-form
    /// continuation carries TWO per-edition obligations the column-blind path cannot see downstream:
    /// the col-7 hyphen indicator itself is OBSOLETE at 2023 (Annex F.2 item 4 → COBOLNET0903, once per
    /// compilation), and continuing a COBOL WORD across lines is REMOVED at 2023 (Annex E.2 item 1 bullet 2
    /// → COBOLNET0902, error strict / warning permissive — the pre-removal join semantics preserved).
    /// </summary>
    public static string NormalizeToFreeForm(
        string sourceText, int dialectLevel, bool permissive, DiagnosticBag? diagnostics, string sourcePath)
        => NormalizeToFreeFormMapped(sourceText, dialectLevel, permissive, diagnostics, sourcePath).Text;

    /// <summary>The MAPPED normalizer (kb/Work PB82): the free-form text plus, per output line, the physical line of
    /// <paramref name="sourcePath"/> it came from — a fixed-form continuation JOINS lines, so the output line count is
    /// smaller than the source's and every later stage (COPY, the parser, the binder) would otherwise number lines the
    /// user cannot find. The string overload is this one's <c>.Text</c>.</summary>
    public static MappedText NormalizeToFreeFormMapped(
        string sourceText, int dialectLevel, bool permissive, DiagnosticBag? diagnostics, string sourcePath)
        => NormalizeToFreeFormMapped(sourceText, dialectLevel, permissive, diagnostics, sourcePath, initialFixed: null, out _);

    /// <summary>The ONE §6.5 logical-conversion walker, for source text AND library text (§6.5 applies to both "in
    /// the order that lines of source text and library text are obtained"), also reporting the reference format it
    /// read each physical line in (<paramref name="formats"/>).</summary>
    /// <param name="initialFixed">The format the text starts in: for library text, the format in effect for its COPY
    /// statement (§7.3.24.3 3) — kb/Work PB1067); null for a compilation group, whose initial format is detected
    /// (the documented extension over §7.3.24.3 2)'s fixed-form default, DEVLOG 931).</param>
    public static MappedText NormalizeToFreeFormMapped(string sourceText, int dialectLevel, bool permissive,
        DiagnosticBag? diagnostics, string sourcePath, bool? initialFixed, out ReferenceFormatMap formats)
    {
        var gates = diagnostics is null ? null : new ReferenceFormatDiagnostics(dialectLevel, permissive, diagnostics, sourcePath);
        var lines = sourceText.Split('\n');

        // Locate the >>SOURCE FORMAT switches: line index → the declared format (fixed?). Each switch partitions
        // the source into a homogeneous-format SEGMENT (§7.3.24.3 GR1); the directive line is discarded (§6.5
        // step 1) and the new format governs from the NEXT line. A continued character-string cannot cross a
        // switch (§7.3.3 SR8c), so each segment's continuation/literal state is self-contained.
        // A MALFORMED directive (an operand that is neither FIXED nor FREE) is still a directive line: it is
        // recognized by its WORD, diagnosed, and CONSUMED — `Fixed` is null and the format in effect is carried
        // on unchanged (kb/Work PB794). Leaving it in the text is what produced `COBOL0001: unexpected '>'`
        // before PB725 and, once PB725 taught the driver to swallow the word, silence.
        var switches = new List<(int Index, bool? Fixed)>();
        var stackOps = new List<(int Index, DirectiveStackOp Op)>();   // §7.3.20 / §7.3.22 — kb/Work PB941
        for (int i = 0; i < lines.Length; i++)
            if (TryMatchStackOp(lines[i], i + 1, out var stackOp)) stackOps.Add((i, stackOp));
            else if (TryMatchDirective(lines[i], out string operand))
            {
                // The §7.3 compiler-directive facility's introduction gate for the ONE directive that cannot be
                // gated with its siblings: this stage CONSUMES the >>SOURCE FORMAT line (it must — the following
                // segment's reference format depends on it), so the line never reaches the shared
                // directive-recognition point in ConditionalCompilationProcessor. Same producer, same row
                // (source-format-directive-2002 → COBOLNET0900), just an earlier stage (kb/Work PB725) — and, at
                // PB794, the same arrangement for the OPERAND: one row, one COBOLNET1911 producer, one stage
                // earlier. Both gates are silent when this overload carries no DiagnosticBag.
                gates?.OnSourceFormatDirective(i + 1, operand);
                switches.Add((i, CompilerDirectiveCatalog.TryOperandWord(SourceFormatWord, operand, out string w)
                    && w is "FIXED" or "FREE" ? w == "FIXED" : null));
            }

        // No directive → the whole file is one segment in the implementor-default format. Our default is
        // structural AUTO-DETECTION (a documented extension over the standard's fixed-form GR2 default; it is what
        // classifies the NIST fixed corpus and free-form real-world source without a directive — DEVLOG 931).
        if (switches.Count == 0)
        {
            bool wholeFixed = initialFixed ?? IsFixedForm(sourceText);
            formats = ReferenceFormatMap.Create(wholeFixed, detected: initialFixed is null, []);
            if (!wholeFixed) return ConvertFreeFormMapped(sourceText, gates, sourcePath);
            var (fl, fo) = ConvertFixedToFreeMapped(sourceText, gates, 0);
            return Mapped(fl, fo, sourcePath);
        }

        // Per-segment. The INITIAL segment (before the first directive) is in the given initial format, or
        // auto-detected; each subsequent segment is in the format its preceding directive declared (GR4 bootstrap: a
        // leading directive makes the initial segment empty, so its format governs from the next line). Emit one
        // output line per source line, the directive lines blanked — a fixed segment's continuation joins reduce its
        // line count exactly as the whole-file path already does.
        bool firstFixed = initialFixed ?? IsFixedForm(string.Join('\n', lines[..switches[0].Index]));
        var segments = WithPoppedFormats(switches, stackOps, firstFixed);
        // A segment boundary at 0-based line i changes the format from the NEXT line: 1-based line i + 2.
        formats = ReferenceFormatMap.Create(firstFixed, detected: initialFixed is null,
            segments.Select(s => (s.Index + 2, s.Fixed)));
        var outLines = new List<string>();
        var outOrigins = new List<int>();   // the 1-based source line of each output line (kb/Work PB82)
        bool segFixed = firstFixed;
        int segStart = 0;
        for (int s = 0; s <= segments.Count; s++)
        {
            // A >>POP boundary KEEPS its line — DirectiveSiteProcessor consumes it after COPY — so the line closes
            // the segment it ends, in that segment's format; a >>SOURCE line is discarded here (§6.5 step 1).
            bool keepsLine = s < segments.Count && segments[s].KeepsLine;
            int segEnd = s < segments.Count ? segments[s].Index + (keepsLine ? 1 : 0) : lines.Length;   // exclusive
            if (segEnd > segStart)
            {
                if (segFixed)
                {
                    var (sl, so) = ConvertFixedToFreeMapped(string.Join('\n', lines[segStart..segEnd]), gates, segStart);
                    outLines.AddRange(sl);
                    outOrigins.AddRange(so);
                }
                else
                    for (int k = segStart; k < segEnd; k++)   // free: line for line, its comment removed (§6.5 2) / 3))
                    {
                        outLines.Add(ConvertFreeLine(lines[k].TrimEnd('\r'), k + 1, gates));
                        outOrigins.Add(k + 1);
                    }
            }
            if (s < segments.Count)
            {
                if (!keepsLine)
                {
                    outLines.Add("");                 // the discarded directive line → a blank line (slot preserved)
                    outOrigins.Add(segments[s].Index + 1);
                }
                segFixed = segments[s].Fixed;
                segStart = segments[s].Index + 1;
            }
        }
        return Mapped(outLines, outOrigins, sourcePath);
    }

    /// <summary>
    /// The segment boundaries once PUSH/POP are applied to the reference format (§7.3.20 / §7.3.22; kb/Work
    /// PB941): the &gt;&gt;SOURCE switches, each resolved to the format it leaves in force (a malformed operand
    /// selects none and carries the current one on — kb/Work PB794), merged in line order with every &gt;&gt;POP
    /// that RESTORES a different format than the one in force. The format is carried by the ONE
    /// <see cref="DirectiveStateStack"/> — this stage's share of the directive state, as the conditional-
    /// compilation driver holds the compilation variables. A PUSH/POP written before the first &gt;&gt;SOURCE can
    /// only save and restore <paramref name="initialFixed"/>, so the auto-detected initial segment is unaffected.
    /// </summary>
    private static List<(int Index, bool Fixed, bool KeepsLine)> WithPoppedFormats(
        List<(int Index, bool? Fixed)> switches, List<(int Index, DirectiveStackOp Op)> stackOps, bool initialFixed)
    {
        bool current = initialFixed;
        var state = new DirectiveStateStack().Carry(Constructs.SourceFormatDirective2002,
            new DirectiveValueCarrier<bool>(() => current, saved => current = saved));
        var segments = new List<(int Index, bool Fixed, bool KeepsLine)>(switches.Count);
        int o = 0;
        foreach (var (index, fixedForm) in switches)
        {
            for (; o < stackOps.Count && stackOps[o].Index < index; o++) ApplyStackOp(stackOps[o].Index, stackOps[o].Op);
            current = fixedForm ?? current;
            segments.Add((index, current, false));
        }
        for (; o < stackOps.Count; o++) ApplyStackOp(stackOps[o].Index, stackOps[o].Op);
        return segments;

        void ApplyStackOp(int index, DirectiveStackOp op)
        {
            bool before = current;
            state.Apply(op);
            if (current != before) segments.Add((index, current, true));
        }
    }

    /// <summary>Match a <c>&gt;&gt;PUSH</c> / <c>&gt;&gt;POP</c> line in the raw (pre-normalization) text — the same
    /// margin-R cut and fixed-form sequence-area allowance as <see cref="TryMatchDirective"/>.</summary>
    private static bool TryMatchStackOp(string rawLine, int line, out DirectiveStackOp op)
    {
        string l = rawLine.TrimEnd('\r');
        return DirectiveStackOp.TryParse(l.Length > MarginR ? l[..MarginR] : l, line, out op, allowSequenceArea: true);
    }

    /// <summary>Assemble output lines and their source lines into a <see cref="MappedText"/> of <paramref name="file"/>.</summary>
    private static MappedText Mapped(List<string> outLines, List<int> outOrigins, string file)
    {
        var origins = new SourceOrigin[outLines.Count == 0 ? 1 : outLines.Count];
        for (int i = 0; i < origins.Length; i++) origins[i] = new SourceOrigin(file, i < outOrigins.Count ? outOrigins[i] : 1);
        return new MappedText(string.Join('\n', outLines), origins);
    }

    /// <summary>Our documented margin R (Annex A item 158 / CONFORMANCE.md §7): the program-text area is columns
    /// 8–72, so column position <see cref="SourceAreaStart"/>+<see cref="SourceAreaWidth"/> = 72.</summary>
    private const int MarginR = SourceAreaStart + SourceAreaWidth;

    /// <summary>Match a <c>&gt;&gt;SOURCE</c> directive line by its WORD — through the ONE compiler-directive line
    /// parse (<see cref="CompilerDirectiveLine"/>), which knows the optional space after the indicator (§7.3.3
    /// SR5) and removes a trailing inline comment (SR3/SR4). Text past margin R is ignored first (§6.3 — columns
    /// 73+ are outside the program-text area; in fixed form they hold the card-image sequence tag the corpus
    /// uses), and the fixed-form sequence area is allowed before the indicator because this stage runs BEFORE
    /// normalization.
    ///
    /// <para>⛔ Recognition is by the directive WORD, never by the whole line's shape: the end-anchored regex this
    /// replaced (<c>>>\s*SOURCE\s+(?:FORMAT\s+)?(?:IS\s+)?(FREE|FIXED)…</c>) failed to match a legal
    /// <c>&gt;&gt;SOURCE FORMAT FIXED *&gt; switch</c>, so the line stayed in the text, the following segment was
    /// read in the WRONG reference format, and the error surfaced on a line the user had not written wrong
    /// (kb/Work PB794). A word-keyed match cannot fail that way — a malformed operand is now diagnosed, not
    /// unseen.</para></summary>
    private static bool TryMatchDirective(string rawLine, out string operand)
    {
        string l = rawLine.TrimEnd('\r');
        return CompilerDirectiveLine.TryParse(
            l.Length > MarginR ? l[..MarginR] : l, SourceFormatWord, out operand, allowSequenceArea: true);
    }

    /// <summary>
    /// The per-compilation diagnostics of reference format — the ones only the §6.5 logical conversion can raise,
    /// because only it sees the indicator area and the line boundaries: the edition gates of the fixed-form
    /// continuation mechanism (registry rows <c>col7-continuation-obsolete-2023</c> /
    /// <c>fixed-form-word-continuation-removed-2023</c>), of the floating comment indicator
    /// (<c>floating-comment-indicator-2002</c>) and of the <c>&gt;&gt;SOURCE</c> directive, and the §6.2.3.2 syntax
    /// rules of the floating comment indicator. The edition metadata stays registry-canonical, and the
    /// strict/permissive decision is the ONE <see cref="EditionSeverityPolicy"/> (P2.9 — removed = error strict /
    /// warning permissive, obsolete = warning always; never a local <c>if(permissive)</c>). One diagnostic per file
    /// per edition gate.
    /// </summary>
    private sealed class ReferenceFormatDiagnostics(int dialectLevel, bool permissive, DiagnosticBag diagnostics, string sourcePath)
    {
        private bool _col7Flagged, _wordFlagged, _floatingCommentFlagged;

        /// <summary>A floating comment indicator in program text (§6.2.3.1) — a COBOL-2002 introduction, so COBOL-85
        /// source has none (row <c>floating-comment-indicator-2002</c>). Gated once per compilation, at its first use.</summary>
        public void OnFloatingComment(int line, int column)
        {
            if (_floatingCommentFlagged) return;
            _floatingCommentFlagged = true;
            var row = ConstructRegistry.Find(Constructs.FloatingCommentIndicator2002)!;
            ConstructRegistry.Check(EditionInfo.Of(dialectLevel, permissive),
                new BagSink(diagnostics, new SourceOrigin(sourcePath, line).ToLocation(column)), row.Id, row.Display);
        }

        /// <summary>§6.2.3.2 SR2: "The floating comment indicator of an inline comment shall be preceded by a
        /// separator space" — COBOLNET2495. The text from the indicator on is still taken as the comment.</summary>
        public void OnUnseparatedFloatingComment(int line, int column)
            => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.FloatingCommentNotSeparated.Code,
                "the floating comment indicator *> shall be preceded by a separator space (ISO §6.2.3.2 SR2); write a "
                + "space before it", new SourceOrigin(sourcePath, line).ToLocation(column), default);

        /// <summary>§6.2.3.2 SR3: "All the characters forming a multiple-character floating indicator shall be
        /// specified on the same line" — COBOLNET2496, for a continuation line that completes one begun at the end of
        /// the latest logical line.</summary>
        public void OnSplitFloatingIndicator(int line, string indicator)
            => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.FloatingIndicatorSplit.Code,
                $"the floating indicator {indicator} is split across a continued line and its continuation line; all the "
                + "characters of a multiple-character floating indicator shall be on the same line (ISO §6.2.3.2 SR3)",
                new SourceOrigin(sourcePath, line).ToLocation(IndicatorColumn), default);

        /// <summary>Any col-7 '-' continuation — OBSOLETE at 2023 (Annex F.2 item 4; VCR row 94).</summary>
        public void OnContinuation(int line)
        {
            if (dialectLevel < 2023 || _col7Flagged) return;
            _col7Flagged = true;
            var severity = EditionSeverityPolicy.For(ConstructAvailability.Obsolete, EditionInfo.Of(dialectLevel, permissive));
            Emit(severity, "COBOLNET0903",
                "the fixed continuation indicator (hyphen in column 7) is obsolete as of COBOL-2023 "
                + "(Annex F.2 item 4; use the floating continuation indicator) — first use at line " + line,
                new SourceOrigin(sourcePath, line).ToLocation(IndicatorColumn));
        }

        /// <summary>A continuation that SPLICES a COBOL word across lines — REMOVED at 2023
        /// (Annex E.2 item 1 bullet 2; VCR row 2). Pre-removal join semantics are preserved either way.</summary>
        public void OnWordContinuation(int line)
        {
            if (dialectLevel < 2023 || _wordFlagged) return;
            _wordFlagged = true;
            const string msg = "continuation of a COBOL word in fixed-form reference format was removed in "
                + "COBOL-2023 (Annex E.2 item 1 bullet 2) — first use at line ";
            var loc = new SourceOrigin(sourcePath, line).ToLocation(IndicatorColumn);
            var severity = EditionSeverityPolicy.For(ConstructAvailability.Removed, EditionInfo.Of(dialectLevel, permissive));
            Emit(severity, "COBOLNET0902", msg + line, loc);
        }

        /// <summary>
        /// The <c>&gt;&gt;SOURCE FORMAT</c> directive's edition gate (ISO §7.3.24; registry row
        /// <c>source-format-directive-2002</c>) — the §7.3 compiler-directive facility is a COBOL-2002
        /// introduction, so a <c>&gt;&gt;</c> line cannot occur in a conforming COBOL-85 source. It emits HERE
        /// rather than at the shared directive-recognition point because this stage consumes the line before the
        /// conditional-compilation driver can see it: same ONE producer, one stage earlier (kb/Work PB725).
        /// Every occurrence is reported — a format switch is not a once-per-file fact like the continuation gates.
        /// <para>The OPERAND is checked here for the same reason and from the same row (kb/Work PB794): §7.3.24.2
        /// admits FIXED or FREE and nothing else, and this is the only stage that sees the line. One producer
        /// (COBOLNET1911) for every directive whose operand is a closed word set.</para>
        /// </summary>
        public void OnSourceFormatDirective(int line, string operand)
        {
            var edition = EditionInfo.Of(dialectLevel, permissive);
            var sink = new BagSink(diagnostics, new SourceOrigin(sourcePath, line).ToLocation());
            CompilerDirectiveCatalog.CheckRow(Constructs.SourceFormatDirective2002, edition, sink);
            CompilerDirectiveCatalog.CheckOperand(SourceFormatWord, operand, edition, sink);
        }

        /// <summary>Emit through the <see cref="DiagnosticBag"/> at the ONE-policy-decided severity (P2.9).</summary>
        private void Emit(EditionSeverity severity, string code, string message, Common.SourceLocation loc)
        {
            if (severity == EditionSeverity.Error)
                diagnostics.ReportError(code, message, loc, default);
            else
                diagnostics.ReportWarning(code, message, loc, default);
        }
    }

    /// <summary>
    /// Heuristic detection of fixed-form. Checks:
    /// - Lines are consistently >= 7 chars
    /// - Column 7 often contains space, *, or -
    /// - Columns 1-6 are often digits or spaces
    /// </summary>
    public static bool IsFixedForm(string sourceText)
    {
        // NOTE: do NOT treat the presence of a *> floating comment (COBOL-2002, ISO §6.2.3) as proof of
        // free-form. *> is legal in BOTH fixed and free reference format; a file with a genuine fixed-format
        // column structure (numeric sequence area + consistent column-7 indicators) is fixed-format that merely
        // uses inline comments, and must still be column-normalized. Classification is therefore driven purely
        // by the structural heuristic below; ConvertFixedToFree strips any inline *> from the source area.
        var lines = sourceText.Split('\n');
        int fixedIndicators = 0;
        int totalLines = 0;
        bool hasNumericSequence = false;
        bool hasFixedIndicatorGlyph = false;
        bool hasContentPastSourceArea = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;
            totalLines++;

            // ⚠ A DETECTION HEURISTIC, not a spec rule — do not restate it as one.
            // ISO §6.3.1: "The rightmost character position of the program-text area is a fixed position
            // defined by the IMPLEMENTOR" (margin R). The standard does NOT mandate column 72, and characters
            // beyond margin R are not an error — they are simply outside the program-text area (§6.3.4;
            // comment-text likewise runs only "up to margin R"). ISO 2023 has no "identification area"; that
            // was a COBOL-85 card-image convention.
            // What this flag captures: OUR margin R is column 72 (Annex A item 158 — a REQUIRED documented
            // item, recorded in docs/CONFORMANCE.md §7). Source with real text past column 72 is therefore
            // source we would TRUNCATE if we treated it as fixed, so — absent a numeric sequence area proving
            // card-image origin — it is far likelier to be free-form. NIST/CCVS fills 73-80 with its member tag
            // ("IX2164.2") and IS fixed-form, which is why a numeric sequence area overrides this signal.
            if (line.Length > SourceAreaStart + SourceAreaWidth
                && line[(SourceAreaStart + SourceAreaWidth)..].Trim().Length > 0)
                hasContentPastSourceArea = true;

            if (line.Length > IndicatorColumn)
            {
                char indicator = line[IndicatorColumn];
                if (indicator is ' ' or '*' or '/' or 'D' or 'd' or '-')
                {
                    bool seqOk = true;
                    for (int i = 0; i < SequenceAreaLength && i < line.Length; i++)
                    {
                        if (!char.IsDigit(line[i]) && line[i] != ' ')
                        {
                            seqOk = false;
                            break;
                        }
                    }
                    if (seqOk)
                    {
                        fixedIndicators++;
                        for (int i = 0; i < SequenceAreaLength && i < line.Length; i++)
                        {
                            if (char.IsDigit(line[i]))
                            {
                                hasNumericSequence = true;
                                break;
                            }
                        }
                        // A NON-SPACE indicator in column 7 is independent positive evidence of fixed format.
                        // The sequence area is OPTIONAL (ISO §6.2.1 — it "may be used to label a source line"),
                        // so a great deal of real-world COBOL leaves columns 1-6 blank and is still fixed-form.
                        // Requiring a numeric sequence area misclassified every such file as FREE-form, where a
                        // '*' in column 7 is no longer a comment indicator but a stray token — so an ordinary
                        // comment line became a syntax error. NIST/CCVS never exposed this because it always
                        // fills the sequence area; the GnuCOBOL corpus did, immediately (DEVLOG 931).
                        if (indicator is not ' ') hasFixedIndicatorGlyph = true;
                    }
                }
            }
        }

        // Either signal suffices: a numeric sequence area, OR a real column-7 indicator glyph. Both are
        // gated behind the same structural ratio (columns 1-6 digits-or-blank and a valid indicator on most
        // lines), which is what keeps genuinely free-form source — whose code starts at column 1, so columns
        // 1-6 hold letters and seqOk fails — from being dragged into the fixed branch.
        // A numeric sequence area is decisive on its own (the NIST/CCVS shape).
        // Failing that, a real column-7 indicator glyph means fixed-form ONLY IF nothing runs past column 72:
        // free-form source is not column-bounded, so text in 73+ is proof the file is NOT fixed. Without this
        // veto the relaxed rule dragged 168 free-form corpus programs into the fixed branch, where truncation
        // at column 72 silently cut their code (30 conformance failures — DEVLOG 931).
        bool fixedShape = hasNumericSequence || (hasFixedIndicatorGlyph && !hasContentPastSourceArea);
        return totalLines > 0 && fixedShape &&
               fixedIndicators * 100 / totalLines > FixedFormThresholdPercent;
    }
}
