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
public static class ReferenceFormatProcessor
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
    {
        var gates = diagnostics is null ? null : new EditionGates(dialectLevel, permissive, diagnostics, sourcePath);
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
            if (!IsFixedForm(sourceText)) return MappedText.Identity(sourceText, sourcePath);
            var (fl, fo) = ConvertFixedToFreeMapped(sourceText, gates, 0);
            return Mapped(fl, fo, sourcePath);
        }

        // Per-segment. The INITIAL segment (before the first directive) is auto-detected; each subsequent segment
        // is in the format its preceding directive declared (GR4 bootstrap: a leading directive makes the initial
        // segment empty, so its format governs from the next line). Emit one output line per source line, the
        // directive lines blanked — a fixed segment's continuation joins reduce its line count exactly as the
        // whole-file path already does.
        bool initialFixed = IsFixedForm(string.Join('\n', lines[..switches[0].Index]));
        var segments = WithPoppedFormats(switches, stackOps, initialFixed);
        var outLines = new List<string>();
        var outOrigins = new List<int>();   // the 1-based source line of each output line (kb/Work PB82)
        bool segFixed = initialFixed;
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
                    for (int k = segStart; k < segEnd; k++) { outLines.Add(lines[k].TrimEnd('\r')); outOrigins.Add(k + 1); }   // free: as-is
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

    /// <summary>The MAPPED fixed→free conversion (kb/Work PB82): the output lines (a fixed-form continuation JOINS
    /// physical lines) and, per output line, the 1-based physical source line where that output line STARTED — the
    /// first source line that wrote content into it (a joined continuation keeps its head line's number).</summary>
    private static (List<string> Lines, List<int> Origins) ConvertFixedToFreeMapped(string sourceText, EditionGates? gates, int lineOffset)
    {
        var origins = new List<int>();
        string converted = ConvertFixedToFree(sourceText, gates, lineOffset, origins);
        var outLines = SplitLines(converted).ToList();
        // origins has one entry per '\n' — SplitLines drops the artifact final empty piece, so the counts agree;
        // any residue is a defect in the tracking, never silently padded.
        if (origins.Count != outLines.Count)
            throw new InvalidOperationException(
                $"ConvertFixedToFree origin tracking: {origins.Count} origin(s) for {outLines.Count} output line(s) (kb/Work PB82)");
        return (outLines, origins);
    }

    /// <summary>Split a <see cref="ConvertFixedToFree"/> result into clean per-line strings for the per-segment
    /// assembly. Every emitted line ends with <c>AppendLine</c>, so the result ends with exactly ONE trailing
    /// newline (the artifact) — drop only that final empty element. A bare <c>TrimEnd('\n')</c> would instead delete
    /// a BLANK source line that terminates the segment, losing it and misaligning the following segment.</summary>
    private static IEnumerable<string> SplitLines(string s)
    {
        var parts = s.Replace("\r\n", "\n").Split('\n');
        return parts.Length > 0 && parts[^1].Length == 0 ? parts[..^1] : parts;
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
    /// The per-compilation edition gates of the fixed-form continuation mechanism (registry rows
    /// <c>col7-continuation-obsolete-2023</c> / <c>fixed-form-word-continuation-removed-2023</c>; the emit
    /// sites live HERE because only the column-aware pass can see the indicator — the metadata stays
    /// registry-canonical, and the strict/permissive decision is the ONE <see cref="EditionSeverityPolicy"/>
    /// (P2.9 — removed = error strict / warning permissive, obsolete = warning always; never a local
    /// <c>if(permissive)</c>). One diagnostic per file per gate.
    /// </summary>
    private sealed class EditionGates(int dialectLevel, bool permissive, DiagnosticBag diagnostics, string sourcePath)
    {
        private bool _col7Flagged, _wordFlagged;

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

    /// <summary>
    /// Convert fixed-form source to free-form by stripping columns 1-6 and 73+,
    /// handling indicator column (7), and joining continuation lines.
    /// Tracks literal/quote state across lines to correctly handle doubled-quotes
    /// ("") that straddle continuation boundaries (ISO §6.2.2).
    /// </summary>
    public static string ConvertFixedToFree(string sourceText) => ConvertFixedToFree(sourceText, gates: null);

    /// <summary>The MAPPED fixed→free conversion of a whole text originating in <paramref name="file"/> (kb/Work PB82).</summary>
    public static MappedText ConvertFixedToFreeMapped(string sourceText, string file)
    {
        var (l, o) = ConvertFixedToFreeMapped(sourceText, gates: null, 0);
        return Mapped(l, o, file);
    }

    /// <param name="lineOffset">The file-relative index (0-based) of this text's first line — nonzero when
    /// converting one SOURCE-FORMAT SEGMENT of a larger file, so the <see cref="EditionGates"/> continuation
    /// diagnostics report the file line, not the segment-relative one.</param>
    /// <param name="origins">When given (kb/Work PB82), receives the 1-based physical source line of each output line,
    /// one entry per newline written — the line that FIRST wrote content into that output line, so a continuation
    /// joined into its predecessor leaves the predecessor's origin standing.</param>
    private static string ConvertFixedToFree(string sourceText, EditionGates? gates, int lineOffset = 0, List<int>? origins = null)
    {
        var lines = sourceText.Split('\n');
        var result = new StringBuilder();
        int lineNo = lineOffset;
        // Origin tracking (kb/Work PB82): each newline appended to the builder completes one output line, whose
        // origin is the source line being processed — unless the line was REOPENED by a continuation join, which
        // strips the previous output line's newline (see the '-' arm) and hands its origin back, so the joined line
        // keeps its HEAD line's number. Newlines are counted incrementally from `scanned`; the join is the one
        // place the builder shrinks, and it is handled there, never inferred from the length.
        int nlSeen = 0, scanned = 0;
        int? reopenedOrigin = null;
        void SyncOrigins(int srcLine)
        {
            if (origins is null) return;
            for (int i = scanned; i < result.Length; i++)
                if (result[i] == '\n') { nlSeen++; origins.Add(reopenedOrigin ?? srcLine); reopenedOrigin = null; }
            scanned = result.Length;
        }

        // Literal state carried across lines for continuation decisions
        bool inLiteral = false;
        bool pendingQuote = false; // true when last char was first half of potential "" pair

        // True while inside an obsolete IDENTIFICATION comment-entry paragraph (AUTHOR/INSTALLATION/etc.):
        // its free-text body is commented out until the next Area-A header. See CommentEntryParagraphs.
        bool inCommentEntry = false;

        foreach (var rawLine in lines)
        {
            if (lineNo > lineOffset) SyncOrigins(lineNo);   // the previous source line's newly completed output lines
            lineNo++;
            var line = rawLine.TrimEnd('\r');

            if (line.Length < SourceAreaStart)
            {
                result.AppendLine();
                inLiteral = false;
                pendingQuote = false;
                continue;
            }

            char indicator = line[IndicatorColumn];
            string sourceArea = line[SourceAreaStart..];
            if (sourceArea.Length > SourceAreaWidth)
                sourceArea = sourceArea[..SourceAreaWidth];

            // Obsolete comment-entry handling: applies only to ordinary code lines (the '-' continuation
            // and the '*'/'/'/'D'/excluded-indicator lines fall through to the switch, which already
            // comments or joins them — a comment-entry body is plain space-indicator text).
            bool excludedIndicator =
                indicator is '*' or '/' or 'D' or 'd' or 'S' or 's' or 'Y' or 'y'
                          or 'P' or 'p' or 'J' or 'j' or 'H' or 'h' or 'E' or 'e' or 'U' or 'u' or '-';
            if (!excludedIndicator)
            {
                string? firstAreaAWord = FirstAreaAWord(sourceArea);
                if (firstAreaAWord is not null && CommentEntryParagraphs.Contains(firstAreaAWord))
                {
                    // Start (or continue, for back-to-back comment paragraphs) a comment-entry.
                    inCommentEntry = true;
                    result.AppendLine($"*> {sourceArea.TrimEnd()}");
                    inLiteral = false;
                    pendingQuote = false;
                    continue;
                }
                if (inCommentEntry)
                {
                    if (firstAreaAWord is not null)
                    {
                        // An Area-A header that is NOT a comment paragraph = the next real paragraph or
                        // division (ENVIRONMENT/DATA/PROCEDURE/section/program). End the comment-entry and
                        // let this line be processed normally below.
                        inCommentEntry = false;
                    }
                    else
                    {
                        // Area-B free text of the comment-entry: keep as commentary.
                        result.AppendLine($"*> {sourceArea.TrimEnd()}");
                        inLiteral = false;
                        pendingQuote = false;
                        continue;
                    }
                }
            }

            switch (indicator)
            {
                case '*' or '/':
                    result.AppendLine($"*> {sourceArea.TrimEnd()}");
                    inLiteral = false;
                    pendingQuote = false;
                    break;

                case 'D' or 'd' or 'S' or 's' or 'Y' or 'y':
                    result.AppendLine(DebugLineCarrier + sourceArea.TrimEnd());
                    inLiteral = false;
                    pendingQuote = false;
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
                    result.AppendLine($"*> {sourceArea.TrimEnd()}");
                    inLiteral = false;
                    pendingQuote = false;
                    break;

                case '-':
                    gates?.OnContinuation(lineNo);
                    // The WORD-continuation shape (removed 2023, VCR row 2): a NON-literal continuation whose
                    // splice joins two COBOL word characters (§6.3.5 — the continuation's first nonblank
                    // immediately follows the preceding line's last nonblank). Literal continuation and
                    // non-word splices (e.g. after a period or parenthesis) are NOT word continuation.
                    if (gates is not null && !inLiteral
                        && LastNonSpace(result) is { } prevCh && IsCobolWordChar(prevCh)
                        && sourceArea.TrimStart() is { Length: > 0 } contText && IsCobolWordChar(contText[0]))
                        gates.OnWordContinuation(lineNo);
                    // The join REOPENS the previous output line: strip its newline HERE (HandleContinuation then
                    // finds none), give its origin back so the completed joined line keeps the head line's number.
                    if (origins is not null)
                    {
                        while (result.Length > 0 && result[result.Length - 1] is '\n' or '\r')
                        {
                            if (result[result.Length - 1] == '\n' && nlSeen > 0)
                            {
                                nlSeen--;
                                reopenedOrigin = origins[^1];
                                origins.RemoveAt(origins.Count - 1);
                            }
                            result.Length--;
                        }
                        scanned = result.Length;
                    }
                    HandleContinuation(result, sourceArea,
                        ref inLiteral, ref pendingQuote);
                    break;

                default:
                    // Normal line: strip any inline *> comment (COBOL-2002) that lies outside a literal, then
                    // emit. Preserve trailing spaces when inside an unclosed literal.
                    inLiteral = false;
                    pendingQuote = false;
                    string code = StripInlineComment(sourceArea);
                    ScanLiteralState(code, ref inLiteral, ref pendingQuote);
                    result.AppendLine(inLiteral ? code : code.TrimEnd());
                    break;
            }
        }
        SyncOrigins(lineNo);   // the last source line's output lines

        return result.ToString();
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

    /// <summary>
    /// Truncate a fixed-format source area at a floating <c>*&gt;</c> inline comment (COBOL-2002, ISO §6.2.3)
    /// that lies outside any string literal. Everything from the <c>*&gt;</c> to end of line is commentary. The
    /// lexer would skip it regardless, but stripping it here keeps the cross-line literal-state scan from being
    /// confused by quotes/apostrophes inside the comment text (which would otherwise mis-join a following
    /// continuation line). A <c>*&gt;</c> embedded in a literal (e.g. <c>"A*&gt;B"</c>) is preserved.
    /// </summary>
    private static string StripInlineComment(string sourceArea)
    {
        bool inLiteral = false;
        char quote = '\0';
        for (int i = 0; i + 1 < sourceArea.Length; i++)
        {
            char c = sourceArea[i];
            if (inLiteral)
            {
                if (c == quote)
                {
                    // A doubled quote ("") is an embedded quote that stays in the literal; a lone quote closes it.
                    if (i + 1 < sourceArea.Length && sourceArea[i + 1] == quote) i++;
                    else inLiteral = false;
                }
            }
            else if (c is '"' or '\'')
            {
                inLiteral = true;
                quote = c;
            }
            else if (c == '*' && sourceArea[i + 1] == '>')
            {
                return sourceArea[..i];
            }
        }
        return sourceArea;
    }

    /// <summary>
    /// Scan a source area to determine the literal/quote state at end of line.
    /// Does NOT modify the content — only updates inLiteral and pendingQuote.
    /// </summary>
    private static void ScanLiteralState(string sourceArea,
        ref bool inLiteral, ref bool pendingQuote)
    {
        for (int i = 0; i < sourceArea.Length; i++)
        {
            char c = sourceArea[i];

            if (c is not ('"' or '\''))
            {
                if (pendingQuote)
                {
                    // Previous quote was NOT doubled — it closed the literal
                    inLiteral = false;
                    pendingQuote = false;
                }
                continue;
            }

            // c is a quote character
            if (!inLiteral)
            {
                inLiteral = true;
                pendingQuote = false;
            }
            else if (pendingQuote)
            {
                // Second half of a doubled-quote pair
                pendingQuote = false;
            }
            else
            {
                // First quote inside a literal — could be closing or first half of ""
                pendingQuote = true;
            }
        }
    }

    /// <summary>A COBOL word-forming character (§8.3.1 — letters, digits, hyphen; the underscore joined at
    /// 2002 and rides the same splice rule).</summary>
    private static bool IsCobolWordChar(char c) => char.IsLetterOrDigit(c) || c is '-' or '_';

    /// <summary>The last non-space, non-newline character accumulated so far (the splice's LEFT side), or
    /// null when the previous content is empty.</summary>
    private static char? LastNonSpace(StringBuilder sb)
    {
        for (int i = sb.Length - 1; i >= 0; i--)
            if (sb[i] is not (' ' or '\n' or '\r')) return sb[i];
        return null;
    }

    /// <summary>
    /// Handle a continuation line (column 7 = '-'). Joins to the previous line.
    /// When continuing a literal, decides whether the continuation's opening quote is:
    /// - a resume marker (strip it) — normal case
    /// - the second half of a "" pair (keep it) — when previous line ended mid-pair
    /// </summary>
    private static void HandleContinuation(StringBuilder result, string sourceArea,
        ref bool inLiteral, ref bool pendingQuote)
    {
        // Remove the last newline from result to join with previous line
        while (result.Length > 0 && result[result.Length - 1] is '\n' or '\r')
            result.Length--;

        if (!inLiteral)
        {
            // Not in a literal — strip trailing spaces from previous line and
            // append trimmed continuation content (§6.3.5: first non-space of
            // continuation immediately follows last non-space of preceding line)
            while (result.Length > 0 && result[result.Length - 1] == ' ')
                result.Length--;
            string trimmed = sourceArea.TrimStart();
            result.AppendLine(trimmed);
            ScanLiteralState(trimmed, ref inLiteral, ref pendingQuote);
            return;
        }

        // We're inside an unclosed literal from the previous line.
        // Find the first non-blank character.
        int firstNonBlank = 0;
        while (firstNonBlank < sourceArea.Length && sourceArea[firstNonBlank] == ' ')
            firstNonBlank++;

        if (firstNonBlank >= sourceArea.Length)
        {
            result.AppendLine();
            return;
        }

        char firstChar = sourceArea[firstNonBlank];

        if (firstChar is not ('"' or '\''))
        {
            // Not a quote — non-literal continuation
            result.Append(sourceArea[firstNonBlank..]);
            result.AppendLine();
            ScanLiteralState(sourceArea[firstNonBlank..], ref inLiteral, ref pendingQuote);
            return;
        }

        if (pendingQuote)
        {
            // Previous line ended with the first half of a "" pair.
            // This quote is the SECOND HALF — data, not a resume marker.
            // Keep this quote (append it).
            pendingQuote = false;

            // Check if the NEXT character is also a quote — if so, it's
            // the resume marker for the continuing literal and must be stripped.
            int contentStart = firstNonBlank + 1;
            if (contentStart < sourceArea.Length && sourceArea[contentStart] is '"' or '\'')
            {
                // Append the paired quote, then skip the resume marker
                result.Append(sourceArea[firstNonBlank]);
                result.Append(sourceArea[(contentStart + 1)..]);
                result.AppendLine();
                ScanLiteralState(sourceArea[(contentStart + 1)..], ref inLiteral, ref pendingQuote);
            }
            else
            {
                // No resume marker follows — just append everything
                result.Append(sourceArea[firstNonBlank..]);
                result.AppendLine();
                ScanLiteralState(sourceArea[(firstNonBlank + 1)..], ref inLiteral, ref pendingQuote);
            }
        }
        else
        {
            // Normal literal continuation: strip the resume marker quote.
            result.Append(sourceArea[(firstNonBlank + 1)..]);
            result.AppendLine();
            // Re-scan the appended content (after the stripped quote)
            ScanLiteralState(sourceArea[(firstNonBlank + 1)..], ref inLiteral, ref pendingQuote);
        }
    }
}
