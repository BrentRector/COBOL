// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The per-COMPILATION diagnostics of reference format — the ones only the §6.5 logical conversion can raise, because
/// only it sees the indicator area and the line boundaries: the edition gates of the fixed-form continuation mechanism
/// (registry rows <c>col7-continuation-obsolete-2023</c> / <c>fixed-form-word-continuation-removed-2023</c>), of the
/// floating comment indicator (<c>floating-comment-indicator-2002</c>), of the floating literal continuation indicator
/// (<c>floating-literal-continuation-2002</c>), of the debugging line (<c>debugging-line-removed-2014</c>) and of the
/// <c>&gt;&gt;SOURCE</c> directive, and the syntax rules of §6.2.3.2 and §6.3.5 on literal continuation. The edition
/// metadata stays registry-canonical, and the strict/permissive decision is the ONE <see cref="EditionSeverityPolicy"/>
/// (P2.9 — removed = error strict / warning permissive, obsolete = warning always; never a local
/// <c>if(permissive)</c>).
/// <para>ONE instance serves the whole compilation — the main source AND every copybook, which is library text read
/// by the same §6.5 walker (kb/Work PB1640) — so a once-per-compilation gate fires once, not once per file. The FILE
/// is therefore an argument of each report, never state of the instance.</para>
/// </summary>
public sealed class ReferenceFormatDiagnostics(int dialectLevel, bool permissive, DiagnosticBag diagnostics)
{
    private bool _col7Flagged, _wordFlagged, _floatingCommentFlagged, _floatingContinuationFlagged, _debuggingLineFlagged;

    private EditionInfo Edition => EditionInfo.Of(dialectLevel, permissive);

    private static Common.SourceLocation At(string file, int line, int column = 0) =>
        new SourceOrigin(file, line).ToLocation(column);

    /// <summary>A floating comment indicator in program text (§6.2.3.1) — a COBOL-2002 introduction, so COBOL-85
    /// source has none (row <c>floating-comment-indicator-2002</c>). Gated once per compilation, at its first use.</summary>
    public void OnFloatingComment(string file, int line, int column)
        => GateOnce(ref _floatingCommentFlagged, Constructs.FloatingCommentIndicator2002, file, line, column);

    /// <summary>A floating literal continuation indicator (§6.2.3.1, §6.5 4)) — introduced with the floating
    /// indicators at COBOL-2002 (row <c>floating-literal-continuation-2002</c>). Gated once per compilation, at its
    /// first use, in either reference format.</summary>
    public void OnFloatingLiteralContinuation(string file, int line, int column)
        => GateOnce(ref _floatingContinuationFlagged, Constructs.FloatingLiteralContinuation2002, file, line, column);

    /// <summary>A fixed-form debugging line (indicator <c>D</c>) — §6.2.2 lists no such indicator at COBOL-2023; the
    /// facility is obsolete at 2002 and removed at 2014 (owner decision kb/Work R61; row
    /// <c>debugging-line-removed-2014</c>). Gated once per compilation, at its first use.</summary>
    public void OnDebuggingLine(string file, int line)
        => GateOnce(ref _debuggingLineFlagged, Constructs.DebuggingLineRemoved2014, file, line, ReferenceFormatProcessor.IndicatorColumn);

    private void GateOnce(ref bool flagged, string constructId, string file, int line, int column)
    {
        if (flagged) return;
        flagged = true;
        var row = ConstructRegistry.Find(constructId)!;
        ConstructRegistry.Check(Edition, new BagSink(diagnostics, At(file, line, column)), row.Id, row.Display);
    }

    /// <summary>§6.2.3.2 SR2: "The floating comment indicator of an inline comment shall be preceded by a
    /// separator space" — COBOLNET2495. The text from the indicator on is still taken as the comment.</summary>
    public void OnUnseparatedFloatingComment(string file, int line, int column)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.FloatingCommentNotSeparated.Code,
            "the floating comment indicator *> shall be preceded by a separator space (ISO §6.2.3.2 SR2); write a "
            + "space before it", At(file, line, column), default);

    /// <summary>§6.2.3.2 SR3: "All the characters forming a multiple-character floating indicator shall be
    /// specified on the same line" — COBOLNET2496, for a continuation line that completes one begun at the end of
    /// the latest logical line.</summary>
    public void OnSplitFloatingIndicator(string file, int line, string indicator)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.FloatingIndicatorSplit.Code,
            $"the floating indicator {indicator} is split across a continued line and its continuation line; all the "
            + "characters of a multiple-character floating indicator shall be on the same line (ISO §6.2.3.2 SR3)",
            At(file, line, ReferenceFormatProcessor.IndicatorColumn), default);

    /// <summary>§6.3.5 2) — COBOLNET2688: a continuation line completes a multiple-character separator or invocation
    /// operator (<c>==</c>, <c>::</c>) begun at the end of the latest logical line.</summary>
    public void OnSplitSeparator(string file, int line, string token)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.MultipleCharacterTokenSplit.Code,
            $"{token} is split across a continued line and its continuation line; all the characters of a "
            + "multiple-character separator or operator shall be on the same line (ISO §6.3.5 2))",
            At(file, line, ReferenceFormatProcessor.IndicatorColumn), default);

    /// <summary>§6.2.3.2 SR6 / §6.3.5 2) / §6.4.2 — COBOLNET2684: the first nonblank character of a literal's
    /// continuation line is not the quotation symbol of its opening delimiter.</summary>
    public void OnContinuationQuote(string file, int line, int column, char quote)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.LiteralContinuationQuote.Code,
            $"the first nonblank character of a continuation line of a literal shall be {QuoteName(quote)}, the quotation "
            + "symbol used in the literal's opening delimiter (ISO §6.2.3.2 SR6)", At(file, line, column), default);

    /// <summary>§6.3.5 2) — COBOLNET2685: a national literal continued by the fixed continuation indicator.</summary>
    public void OnNationalFixedContinuation(string file, int line)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.NationalLiteralFixedContinuation.Code,
            "a national literal may be continued only with a floating literal continuation indicator, not the fixed "
            + "continuation indicator (ISO §6.3.5 2))", At(file, line, ReferenceFormatProcessor.IndicatorColumn), default);

    /// <summary>§6.2.3.2 SR5 — COBOLNET2686: a floating literal continuation indicator on a line that holds the fixed
    /// continuation indicator.</summary>
    public void OnFloatingOnFixedContinuation(string file, int line, int column)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.FloatingContinuationOnFixedContinuation.Code,
            "a floating literal continuation indicator shall not be specified on a line that contains a fixed literal "
            + "continuation indicator (ISO §6.2.3.2 SR5)", At(file, line, column), default);

    /// <summary>§6.2.3.2 SR4 — COBOLNET2687: a literal continued with both forms of continuation.</summary>
    public void OnTwoContinuationForms(string file, int line, int column)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.LiteralContinuedTwoForms.Code,
            "a literal shall not be continued with more than one form of continuation: the fixed indicator and the "
            + "floating indicator are both used on it (ISO §6.2.3.2 SR4)", At(file, line, column), default);

    /// <summary>§6.3.7.3 / §6.4.4.3 — COBOLNET2689: an inline comment on a line that holds a floating literal
    /// continuation indicator.</summary>
    public void OnCommentAfterFloatingContinuation(string file, int line, int column)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.FloatingContinuationComment.Code,
            "an inline comment shall not be written on a line that contains a floating literal continuation indicator "
            + "(ISO §6.3.7.3, §6.4.4.3)", At(file, line, column), default);

    /// <summary>§6.4.2 — COBOLNET2690: the continued line or a continuation line of a literal holds no literal
    /// content.</summary>
    public void OnEmptyLiteralPart(string file, int line, int column)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.LiteralContinuationPartEmpty.Code,
            "at least one character of the literal content shall be specified on the continued line and on each "
            + "continuation line (ISO §6.4.2)", At(file, line, column), default);

    /// <summary>§7.3.3 SR2 — COBOLNET2691: a compiler directive written after program text on its line.</summary>
    public void OnDirectiveAfterProgramText(string file, int line, int column)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.DirectiveAfterProgramText.Code,
            "a compiler directive shall be preceded only by zero, one, or more space characters; write it on a line of "
            + "its own (ISO §7.3.3 SR2)", At(file, line, column), default);

    /// <summary>§6.1 3) a) — COBOLNET2653: a free-form line of more than 255 character positions (kb/Work PB1496).
    /// Every such line is reported, at the first position past the limit; it is then read in full.</summary>
    public void OnFreeFormLineTooLong(string file, int line, int positions)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.FreeFormLineTooLong.Code,
            $"this free-form line has {positions} character positions; ISO §6.1 3) a) allows at most "
            + $"{ReferenceFormatProcessor.FreeFormMaxPositions} (a tab counts the positions it advances over, DOC-A.1-157)",
            At(file, line, ReferenceFormatProcessor.FreeFormMaxPositions), default);

    /// <summary>§6.3.3 / §6.2.2 — COBOLNET2616: the indicator area holds a character that is not a fixed indicator
    /// (kb/Work PB1494). Every such line is reported; it is then read as a source line.</summary>
    public void OnInvalidIndicator(string file, int line, char indicator)
        => diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.FixedIndicatorInvalid.Code,
            $"the indicator area (column 7) holds '{indicator}', which is not a fixed indicator: ISO §6.2.2 lists "
            + "*, / (comment line), - (continuation line) and space (source line); a NIST CCVS program's column-7 "
            + "conventions are honored under --nist", At(file, line, ReferenceFormatProcessor.IndicatorColumn), default);

    /// <summary>Any col-7 '-' continuation — OBSOLETE at 2023 (Annex F.2 item 4; VCR row 94).</summary>
    public void OnContinuation(string file, int line)
    {
        if (dialectLevel < 2023 || _col7Flagged) return;
        _col7Flagged = true;
        var severity = EditionSeverityPolicy.For(ConstructAvailability.Obsolete, Edition);
        Emit(severity, EditionCodes.ObsoleteFlag,
            "the fixed continuation indicator (hyphen in column 7) is obsolete as of COBOL-2023 "
            + "(Annex F.2 item 4; use the floating continuation indicator) — first use at line " + line,
            At(file, line, ReferenceFormatProcessor.IndicatorColumn));
    }

    /// <summary>A continuation that SPLICES a COBOL word across lines — REMOVED at 2023
    /// (Annex E.2 item 1 bullet 2; VCR row 2). Pre-removal join semantics are preserved either way.</summary>
    public void OnWordContinuation(string file, int line)
    {
        if (dialectLevel < 2023 || _wordFlagged) return;
        _wordFlagged = true;
        const string msg = "continuation of a COBOL word in fixed-form reference format was removed in "
            + "COBOL-2023 (Annex E.2 item 1 bullet 2) — first use at line ";
        var severity = EditionSeverityPolicy.For(ConstructAvailability.Removed, Edition);
        Emit(severity, EditionCodes.RemovedConstruct, msg + line, At(file, line, ReferenceFormatProcessor.IndicatorColumn));
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
    public void OnSourceFormatDirective(string file, int line, string operand)
    {
        var sink = new BagSink(diagnostics, At(file, line));
        CompilerDirectiveCatalog.CheckRow(Constructs.SourceFormatDirective2002, Edition, sink);
        CompilerDirectiveCatalog.CheckOperand(ReferenceFormatProcessor.SourceFormatWord, operand, Edition, sink);
    }

    private static string QuoteName(char quote) => quote == '"' ? "a quotation mark (\")" : "an apostrophe (')";

    /// <summary>Emit through the <see cref="DiagnosticBag"/> at the ONE-policy-decided severity (P2.9).</summary>
    private void Emit(EditionSeverity severity, string code, string message, Common.SourceLocation loc)
    {
        if (severity == EditionSeverity.Error)
            diagnostics.ReportError(code, message, loc, default);
        else
            diagnostics.ReportWarning(code, message, loc, default);
    }
}
