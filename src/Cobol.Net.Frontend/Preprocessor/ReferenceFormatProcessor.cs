// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// THE ISO §6.5 logical conversion: reads fixed-form and free-form reference format and produces the logical
/// free-form text every later stage reads. Fixed-form: columns 1-6 sequence, 7 indicator, 8-72 program text, 73+
/// outside margin R. Free-form: no column restrictions. A compilation group starts in fixed form unless the
/// <see cref="InitialReferenceFormat"/> selection says otherwise (§7.3.24.3 2); kb/Work PB1362).
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
    internal const int IndicatorColumn = 6;

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
    /// the comment-entry as commentary in the column-aware preprocessor: discard the entry — the rest of the
    /// header line and its content up to the next Area-A header — and keep the paragraph HEADER, written
    /// <c>AUTHOR. .</c> (the paragraph's terminating period standing for the entry, which COBOL-85 ended at the next
    /// Area-A word), so the parser and the ONE removal gate (<c>VersionConformancePass</c>, COBOLNET0902 from 2002) see a
    /// fixed-form paragraph exactly as they see a free-form one (kb/Work PB1494, PB1758) — no embedded-period or
    /// terminating-period edge cases reach the grammar.
    /// </summary>
    private static readonly HashSet<string> CommentEntryParagraphs = new(StringComparer.OrdinalIgnoreCase)
    {
        "AUTHOR", "INSTALLATION", "DATE-WRITTEN", "DATE-COMPILED", "SECURITY", "REMARKS",
    };

    /// <summary>The compiler-directive word this stage owns (ISO §7.3.24; the <c>source-format-directive-2002</c>
    /// registry row's single <c>directiveWords</c> entry — FORMAT and IS are §5.2.3 optional words, not part of
    /// the word).</summary>
    internal const string SourceFormatWord = "SOURCE";

    /// <summary>
    /// Normalize a compilation group to logical free form, starting in <paramref name="initial"/> — fixed form, the
    /// standard default (§7.3.24.3 2)), unless selected otherwise. Each
    /// <c>&gt;&gt;SOURCE FORMAT [IS] {FIXED|FREE}</c> directive (ISO §7.3.24) switches the reference format of the
    /// text that FOLLOWS it, up to the next directive — the source is partitioned into homogeneous-format segments
    /// (§7.3.24.3 GR1) and each is converted in its own format. The directive line is discarded (§6.5 logical
    /// conversion, step 1) and left as a blank line so downstream source-line numbers stay aligned.
    /// </summary>
    public static string NormalizeToFreeForm(string sourceText, InitialReferenceFormat initial = InitialReferenceFormat.Fixed)
        => NormalizeToFreeForm(sourceText, gates: null, sourcePath: "<source>", initial);

    /// <summary>
    /// The edition-aware overload (W3 preprocessor threading, VCR rows 2/4/94 — DEVLOG 598): fixed-form
    /// continuation carries TWO per-edition obligations the column-blind path cannot see downstream:
    /// the col-7 hyphen indicator itself is OBSOLETE at 2023 (Annex F.2 item 4 → COBOLNET0903, once per
    /// compilation), and continuing a COBOL WORD across lines is REMOVED at 2023 (Annex E.2 item 1 bullet 2
    /// → COBOLNET0902, error strict / warning permissive — the pre-removal join semantics preserved).
    /// </summary>
    public static string NormalizeToFreeForm(string sourceText, ReferenceFormatDiagnostics? gates, string sourcePath,
        InitialReferenceFormat initial = InitialReferenceFormat.Fixed)
        => NormalizeToFreeFormMapped(sourceText, gates, sourcePath, initial).Text;

    /// <summary>The MAPPED normalizer (kb/Work PB82): the free-form text plus, per output line, the physical line of
    /// <paramref name="sourcePath"/> it came from — a fixed-form continuation JOINS lines, so the output line count is
    /// smaller than the source's and every later stage (COPY, the parser, the binder) would otherwise number lines the
    /// user cannot find. The string overload is this one's <c>.Text</c>.</summary>
    public static MappedText NormalizeToFreeFormMapped(string sourceText, ReferenceFormatDiagnostics? gates, string sourcePath,
        InitialReferenceFormat initial = InitialReferenceFormat.Fixed)
        => NormalizeToFreeFormMapped(sourceText, gates, sourcePath, initial.InitialFixed(), out _);

    /// <summary>The ONE §6.5 logical-conversion walker, for source text AND library text (§6.5 applies to both "in
    /// the order that lines of source text and library text are obtained"), also reporting the reference format it
    /// read each physical line in (<paramref name="formats"/>).</summary>
    /// <param name="gates">The compilation's reference-format diagnostics — the ONE instance the main source and every
    /// copybook share (kb/Work PB1640), or null for a conversion that reports nothing.</param>
    /// <param name="initialFixed">The format the text starts in: for a compilation group, its
    /// <see cref="InitialReferenceFormat"/> selection (fixed form by default, §7.3.24.3 2) — kb/Work PB1362); for
    /// library text, the format in effect for its COPY statement (§7.3.24.3 3) — kb/Work PB1067). Null DETECTS it
    /// (<see cref="IsFixedForm"/>) — the documented <see cref="InitialReferenceFormat.Auto"/> extension, and the library
    /// text of a COPY statement read in a detected-free text (<see cref="ReferenceFormatMap.LibraryTextDefaultAt"/>).</param>
    /// <param name="ccvsIndicators">Honor the NIST CCVS column-7 conventions (S/Y debugging lines, P/J/H/E/U excluded
    /// alternates, any other letter a primary-configuration line) — the <c>--nist</c> dialect only (kb/Work PB1494);
    /// otherwise a character that is not a fixed indicator (§6.2.2) is diagnosed.</param>
    /// <param name="implicitOps">The §14.9.28.4 GR14 implicit PUSH ALL / POP ALL written in THIS text, at its physical
    /// lines (<see cref="ImplicitFormatOps.For"/>) — the format state they save and restore is this walker's
    /// (kb/Work PB1066). Null or empty for every text without an exception-checking PERFORM that holds a format
    /// directive in a handler.</param>
    /// <param name="startsInIdentificationDivision">Whether the text starts in an IDENTIFICATION DIVISION — the only
    /// division with comment-entry paragraphs (kb/Work PB1494): true for a source text, which begins with one; for library
    /// text, whether its COPY statement stands in one (<see cref="DivisionCursor"/>).</param>
    public static MappedText NormalizeToFreeFormMapped(string sourceText, ReferenceFormatDiagnostics? gates, string sourcePath,
        bool? initialFixed, out ReferenceFormatMap formats, bool ccvsIndicators = false,
        IReadOnlyList<DirectiveStackOp>? implicitOps = null, bool startsInIdentificationDivision = true)
    {
        // THE line-entry stage (kb/Work PB1800): every line below is read from here — terminators, tabs and (under
        // --nist) archive markers are settled, so no consumer splits, trims or expands anything itself.
        var lines = PhysicalLines.Read(sourceText, ccvsIndicators).Lines;

        // The initial format: the one the caller selected (fixed form by default, §7.3.24.3 2)) or inherited from the
        // COPY statement (3)), or — only under the documented Auto extension — the one IsFixedForm detects in the text
        // before the first line that could be a >>SOURCE directive in either reading (kb/Work PB1362).
        bool firstFixed = initialFixed ?? IsFixedForm(lines[..FirstSourceDirectiveCandidate(lines)]);
        var (segments, directiveLines) = FormatSegments(lines, firstFixed, gates, sourcePath, implicitOps ?? []);
        var physicalText = new string[lines.Length];   // the map keeps what the line-entry stage settled (ReferenceFormatMap.PhysicalText)
        for (int i = 0; i < physicalText.Length; i++) physicalText[i] = lines[i].Text;

        // No format boundary → the whole text is one segment in its initial format.
        if (segments.Count == 0)
        {
            formats = ReferenceFormatMap.Create(firstFixed, detected: initialFixed is null, [], directiveLines, physicalText);
            var (wl, wo) = firstFixed
                ? new FixedFormConverter(gates, sourcePath, ccvsIndicators, startsInIdentificationDivision).Convert(lines)
                : ConvertFreeLines(lines, gates, sourcePath);
            return Mapped(wl, wo, sourcePath);
        }

        // Per-segment. The INITIAL segment (before the first boundary) is in the initial format; each subsequent
        // segment is in the format its boundary left in force (GR4 bootstrap: a leading directive makes the initial
        // segment empty, so its format governs from the next line). Emit one output line per source line, the
        // directive lines blanked — a fixed segment's continuation joins reduce its line count exactly as the
        // whole-file path already does.
        // A segment boundary at 0-based line i changes the format from the NEXT line: 1-based line i + 2.
        formats = ReferenceFormatMap.Create(firstFixed, detected: initialFixed is null,
            segments.Select(s => (s.Index + 2, s.Fixed)), directiveLines, physicalText);
        var outLines = new List<string>();
        var outOrigins = new List<int>();   // the 1-based source line of each output line (kb/Work PB82)
        bool segFixed = firstFixed;
        bool inIdentification = startsInIdentificationDivision;   // carried across the text's fixed segments
        int segStart = 0;
        for (int s = 0; s <= segments.Count; s++)
        {
            // A >>POP boundary KEEPS its line — DirectiveSiteProcessor consumes it after COPY — so the line closes
            // the segment it ends, in that segment's format; a >>SOURCE line is discarded here (§6.5 step 1).
            bool keepsLine = s < segments.Count && segments[s].KeepsLine;
            int segEnd = s < segments.Count ? segments[s].Index + (keepsLine ? 1 : 0) : lines.Length;   // exclusive
            if (segEnd > segStart)
            {
                var segment = lines[segStart..segEnd];
                List<string> sl;
                List<int> so;
                if (segFixed)
                {
                    var converter = new FixedFormConverter(gates, sourcePath, ccvsIndicators, inIdentification);
                    (sl, so) = converter.Convert(segment);
                    inIdentification = converter.InIdentificationDivision;
                }
                else
                    (sl, so) = ConvertFreeLines(segment, gates, sourcePath);   // free form: its comments removed, floating-continued literals joined (§6.5 2) / 3) / 4) / 8))
                outLines.AddRange(sl);
                outOrigins.AddRange(so);
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
    /// The reference-format segment boundaries of a text, found by reading it IN ORDER with the format in effect as
    /// state (§6.5: "the reference format mode is determined" by each SOURCE FORMAT directive line as the lines are
    /// obtained): a &gt;&gt;SOURCE line (discarded — §6.5 1)) resolved to the format it leaves in force (a malformed
    /// operand selects none and carries the current one on — kb/Work PB794), and every &gt;&gt;POP that RESTORES a
    /// different format than the one in force (it keeps its line; §7.3.20 / §7.3.22, kb/Work PB941). The format is
    /// carried by the ONE <see cref="DirectiveStateStack"/> — this stage's share of the directive state, as the
    /// conditional-compilation driver holds the compilation variables.
    /// <para>⛔ A line is a directive line only in the program-text area OF THE FORMAT IN EFFECT
    /// (<see cref="DirectiveText"/>, kb/Work PB1361): character positions 8–72 of a source line in fixed form — the
    /// sequence area may hold any character (§6.3.2) — and the whole line in free form. Recognizing it by a character
    /// class instead ("digits or spaces before <c>&gt;&gt;</c>") missed a fixed-form directive whose sequence area was
    /// not numeric, and took a directive preceded by program text, or written past margin R in free form, as a
    /// switch — the following text was then read in the wrong format.</para>
    /// <para>The §7.3 compiler-directive facility's introduction gate and operand check for the ONE directive that
    /// cannot be gated with its siblings are asked here: this stage CONSUMES the &gt;&gt;SOURCE FORMAT line (it must —
    /// the following segment's reference format depends on it), so the line never reaches the shared
    /// directive-recognition point in ConditionalCompilationProcessor. Same producer, same row
    /// (source-format-directive-2002 → COBOLNET0900; COBOLNET1911 for the operand), one stage earlier (kb/Work PB725,
    /// PB794). Both are silent when the conversion carries no DiagnosticBag.</para>
    /// <para>Recognition is by the directive WORD through the ONE compiler-directive line parse
    /// (<see cref="CompilerDirectiveLine"/>), never by the whole line's shape: the end-anchored regex that preceded it
    /// failed to match a legal <c>&gt;&gt;SOURCE FORMAT FIXED *&gt; switch</c>, so the line stayed in the text and the
    /// following segment was read in the WRONG reference format (kb/Work PB794).</para>
    /// </summary>
    private static (List<(int Index, bool Fixed, bool KeepsLine)> Segments, List<int> DirectiveLines) FormatSegments(
        ReadOnlySpan<PhysicalLine> lines, bool initialFixed, ReferenceFormatDiagnostics? gates, string file,
        IReadOnlyList<DirectiveStackOp> implicitOps)
    {
        bool current = initialFixed;
        var state = new DirectiveStateStack().Carry(Constructs.SourceFormatDirective2002,
            new DirectiveValueCarrier<bool>(() => current, saved => current = saved));
        var segments = new List<(int Index, bool Fixed, bool KeepsLine)>();
        var directiveLines = new List<int>();   // every written SOURCE FORMAT / PUSH / POP line (ReferenceFormatMap.DirectiveLines)
        // §14.9.28.4 GR14's implicit ops, as boundaries BETWEEN physical lines (see ImplicitBoundaries).
        var boundaries = ImplicitBoundaries(implicitOps);
        int nextBoundary = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            // The implicit PUSH ALL / POP ALL boundaries that fall before this line apply BEFORE it is read — through the
            // SAME stack the written >>PUSH / >>POP use, so they nest with them and restore whatever format is in force.
            // A POP that restores a DIFFERENT format closes the previous line's segment: the restored format governs this
            // line itself ("immediately preceding the END PERFORM phrase"), and the line before it is kept (it is not a
            // directive line, so nothing of it is discarded) — the shape of a written POP's boundary one line earlier.
            while (nextBoundary < boundaries.Count && boundaries[nextBoundary].BeforeLine <= i + 1)
            {
                bool before = current;
                state.Apply(new DirectiveStackOp(i + 1, boundaries[nextBoundary++].Kind, null));
                if (current != before) segments.Add((i - 1, current, true));
            }
            CompilerDirectiveLine d = default;
            bool isDirective = DirectiveText(lines[i].Text, current) is { } text && CompilerDirectiveLine.TryParse(text, out d);
            // §7.3.24.3 4): "A SOURCE FORMAT directive that is the first line of a compilation group or library text
            // may be in either fixed form or free form" — so `>>SOURCE FORMAT FREE` in column 1 of line 1 is a
            // directive even though the text starts in fixed form, where column 1 is the sequence area.
            if (!isDirective && i == 0 && DirectiveText(lines[0].Text, !current) is { } other
                && CompilerDirectiveLine.TryParse(other, out d) && d.Word == SourceFormatWord)
                isDirective = true;
            if (!isDirective) continue;
            if (DirectiveStackOp.TryParse(d, i + 1, out var op))
            {
                directiveLines.Add(i + 1);
                bool before = current;
                state.Apply(op);
                if (current != before) segments.Add((i, current, true));
            }
            else if (d.Word == SourceFormatWord)
            {
                directiveLines.Add(i + 1);
                gates?.OnSourceFormatDirective(file, i + 1, d.Operand);
                if (CompilerDirectiveCatalog.TryOperandWord(SourceFormatWord, d.Operand, out string w) && w is "FIXED" or "FREE")
                    current = w == "FIXED";
                segments.Add((i, current, false));
            }
        }
        return (segments, directiveLines);
    }

    /// <summary>⛔ ISO §14.9.28.4 GR14's implicit PUSH ALL / POP ALL (kb/Work PB1066) as BOUNDARIES between physical lines,
    /// in the order they apply (<paramref name="ops"/> are at this text's PHYSICAL lines and in token order, which nests
    /// each PERFORM's pair — <see cref="ImplicitFormatOps"/>). A reference format is read PER LINE, so an op is a position
    /// between two lines, and the two ops of a pair sit on opposite sides of the lines they bracket:
    /// <list type="bullet">
    /// <item>the PUSH — "assumed at the end of imperative-statement-1" — after the physical line P that ends it, so it
    /// takes effect before line P + 1;</item>
    /// <item>the POP — "immediately preceding the END PERFORM phrase" — before the physical line L that holds the phrase,
    /// so the restored format governs END-PERFORM's own line (§7.3.24.3 1: the format governs "the source text … following"
    /// the directive, and the phrase follows the POP).</item>
    /// </list>
    /// A pair whose POP boundary is not past its PUSH boundary (<c>L &lt;= P</c>: END-PERFORM on the line imperative-
    /// statement-1 ends on) brackets no line, so no directive can lie between — it is dropped whole, which keeps the
    /// remaining boundaries nested. Ties keep token order, so an inner PERFORM's POP precedes an outer one's.</summary>
    private static List<(int BeforeLine, DirectiveStackKind Kind)> ImplicitBoundaries(IReadOnlyList<DirectiveStackOp> ops)
    {
        var boundaries = new List<(int BeforeLine, DirectiveStackKind Kind)>();
        if (ops.Count == 0) return boundaries;
        var open = new Stack<int>();
        var pairs = new List<(int Push, int Pop)>();
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i].Kind == DirectiveStackKind.Push) { open.Push(i); continue; }
            if (open.Count == 0) continue;   // a POP with no PUSH of this text saves nothing to restore
            pairs.Add((open.Pop(), i));
        }
        var kept = new List<(int Order, int BeforeLine, DirectiveStackKind Kind)>();
        foreach (var (push, pop) in pairs)
        {
            int pushBefore = ops[push].Line + 1, popBefore = ops[pop].Line;
            if (popBefore <= pushBefore - 1) continue;   // END-PERFORM on or before the line that ends imperative-statement-1
            kept.Add((push, pushBefore, DirectiveStackKind.Push));
            kept.Add((pop, popBefore, DirectiveStackKind.Pop));
        }
        foreach (var k in kept.OrderBy(k => k.BeforeLine).ThenBy(k => k.Order)) boundaries.Add((k.BeforeLine, k.Kind));
        return boundaries;
    }

    /// <summary>The program-text area in which a compiler directive line is recognized, in the reference format in
    /// effect (§7.3.3 SR3: "When the reference format is fixed-form, a compiler directive shall be written in the
    /// program-text area"; SR2: "A compiler directive shall be preceded only by zero, one, or more space
    /// characters") — character positions 8 through margin R of a SOURCE line in fixed form (a comment, debugging or
    /// continuation line holds no directive), the whole line in free form — or null when the line holds none.</summary>
    private static string? DirectiveText(string line, bool fixedForm)
    {
        if (!fixedForm) return line;
        return line.Length > SourceAreaStart && line[IndicatorColumn] == ' ' ? ProgramTextArea(line) : null;
    }

    /// <summary>For the <see cref="InitialReferenceFormat.Auto"/> detector only: the index of the first line that is a
    /// &gt;&gt;SOURCE directive in EITHER reading (the format is not known yet — it is what is being detected), or
    /// the line count. The detector classifies the text before it.</summary>
    private static int FirstSourceDirectiveCandidate(ReadOnlySpan<PhysicalLine> lines)
    {
        for (int i = 0; i < lines.Length; i++)
            foreach (bool fixedForm in (ReadOnlySpan<bool>)[true, false])
                if (DirectiveText(lines[i].Text, fixedForm) is { } text
                    && CompilerDirectiveLine.TryParse(text, SourceFormatWord, out _))
                    return i;
        return lines.Length;
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

    /// <summary>
    /// The <see cref="InitialReferenceFormat.Auto"/> extension's detector — NEVER the default (kb/Work PB1362: it reads
    /// conforming fixed-form source with a non-numeric or blank sequence area and text past margin R as free form).
    /// Heuristic detection of fixed-form. Checks:
    /// - Lines are consistently >= 7 chars
    /// - Column 7 often contains space, *, or -
    /// - Columns 1-6 are often digits or spaces
    /// </summary>
    public static bool IsFixedForm(ReadOnlySpan<PhysicalLine> lines)
    {
        // NOTE: do NOT treat the presence of a *> floating comment (COBOL-2002, ISO §6.2.3) as proof of
        // free-form. *> is legal in BOTH fixed and free reference format; a file with a genuine fixed-format
        // column structure (numeric sequence area + consistent column-7 indicators) is fixed-format that merely
        // uses inline comments, and must still be column-normalized. Classification is therefore driven purely
        // by the structural heuristic below; the fixed-form converter strips any inline *> from the source area.
        int fixedIndicators = 0;
        int totalLines = 0;
        bool hasNumericSequence = false;
        bool hasFixedIndicatorGlyph = false;
        bool hasContentPastSourceArea = false;

        foreach (var physical in lines)
        {
            string line = physical.Text;
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
