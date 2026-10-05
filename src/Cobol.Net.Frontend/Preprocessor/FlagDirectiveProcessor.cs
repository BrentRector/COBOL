// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The WiseOwl COBOL <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> migration-flagging directive stage (ISO §7.3.14 /
/// §7.3.15; greenfield-only — the legacy pipeline keeps consuming the words via
/// <see cref="ConditionalCompilationProcessor"/>'s <c>KnownIgnoredDirectives</c>): parses each surviving
/// <c>&gt;&gt;FLAG-nn { ALL | option… } { ON | OFF }</c> line of the FINAL preprocessed text into a
/// <see cref="FlagEvent"/> (via the ONE <see cref="FlagDirectiveLine"/> parser), reports a malformed operand, and
/// blanks the line — line-count preserving, so every later token line number is stable (the <c>&gt;&gt;TURN</c> H3
/// discipline). The events build the compile-time <see cref="Binding.FlagState"/> that
/// <see cref="Validation.FlagConformancePass"/> folds per source line to decide whether a construct is flagged.
/// Design SSOT: <c>docs/rearchitecture/DESIGN-flag-directives.md</c>.
/// </summary>
/// <remarks>The directive-WORD edition gate (>>FLAG-14 = a 2023 introduction; >>FLAG-02 = a 2014 introduction,
/// obsolete at 2023) is Increment 0b — added here through the ONE <see cref="Editions.ConstructRegistry"/> like
/// <see cref="RefModZeroLengthDirectiveProcessor"/>.</remarks>
public static class FlagDirectiveProcessor
{
    /// <summary>Process <paramref name="text"/>: edition-gate each directive word, collect the FLAG-02/FLAG-14
    /// toggle events, syntax-check each operand, and blank the directive lines. Line-count preserving.</summary>
    /// <para><paramref name="stackOps"/> are the PUSH/POP directives of the same text, replayed over the toggles —
    /// FLAG-02 and FLAG-14 are two directives, saved and restored independently (§7.3.20 / §7.3.22; kb/Work
    /// PB941).</para>
    public static (string Text, DirectiveTimeline<FlagEvent> Events) Process(
        string text, DiagnosticBag diagnostics, string sourcePath, SourceLineMap? lineMap = null,
        IReadOnlyList<DirectiveStackOp>? stackOps = null)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, DirectiveTimeline<FlagEvent>.Empty);
        var lines = text.Split('\n');
        var events = new DirectiveEventLog<FlagEvent>();
        for (int i = 0; i < lines.Length; i++)
        {
            // The ONE compiler-directive line parse (kb/Work PB794): the indicator's optional space (§7.3.3 SR5)
            // and the trailing inline comment (SR3/SR4) are its rules, not this stage's — `>>FLAG-14 ALL ON
            // *> why` used to be rejected as a malformed option list.
            if (!CompilerDirectiveLine.TryParse(lines[i], out var line)) continue;

            FlagDirective directive;
            if (line.Word == "FLAG-02") directive = FlagDirective.Flag02;
            else if (line.Word == "FLAG-14") directive = FlagDirective.Flag14;
            else continue;

            string operand = line.Operand;
            var loc = lineMap?.Locate(i + 1, sourcePath) ?? new SourceLocation(sourcePath, 0, i, 0);   // the SOURCE origin of resultant line i (kb/Work PB82)

            // The directive-WORD edition gate already fired at the ONE directive-recognition point
            // (CompilerDirectiveCatalog, from the flag-14-directive-2023 and flag-02-directive-2014 rows'
            // directiveWords — kb/Work PB725): >>FLAG-14 is a 2023 introduction (COBOLNET0900 below 2023);
            // >>FLAG-02 is a 2014 introduction that is OBSOLETE at 2023 (COBOLNET0900 below 2014, then the
            // COBOLNET0903 obsolete WARNING at 2023 — §7.3.14.1 NOTE / §4.2.13: obsolete elements are still
            // SUPPORTED and merely flagged, never rejected/removed). This stage collects the options.

            if (FlagDirectiveLine.TryParse(directive, operand, out var options, out bool on, out string? error))
                events.Add(directive == FlagDirective.Flag02 ? Constructs.Flag02Directive2014 : Constructs.Flag14Directive2023,
                    i + 1, new FlagEvent(i + 1, directive, on, options));
            else
                ReportMalformed(directive, error, diagnostics, loc);

            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        return (string.Join('\n', lines), events.ToTimeline(stackOps ?? []));
    }

    /// <summary>The SYNTAX-ONLY check of one <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> operand — what ISO §7.2.1
    /// asks of a directive line in an OMITTED conditional-compilation branch ("syntactically correct in the initial source
    /// text and library text"; kb/Work PB2003): the same parse and the same COBOLNET1622 the compiled directive gets,
    /// and nothing applied. <paramref name="word"/> is the directive word (<c>FLAG-02</c> / <c>FLAG-14</c>).</summary>
    internal static void CheckOperand(string word, string operand, DiagnosticBag diagnostics, SourceLocation loc)
    {
        var directive = CobolNames.Same(word, "FLAG-02") ? FlagDirective.Flag02 : FlagDirective.Flag14;
        if (!FlagDirectiveLine.TryParse(directive, operand, out _, out _, out string? error))
            ReportMalformed(directive, error, diagnostics, loc);
    }

    /// <summary>The ONE COBOLNET1622 sentence — the compiled directive and its omitted twin say the same thing.</summary>
    private static void ReportMalformed(FlagDirective directive, string? error, DiagnosticBag diagnostics, SourceLocation loc) =>
        diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.FlagDirectiveMalformed.Code,
            $">>{FlagDirectiveLine.DirectiveWord(directive)} is malformed: {error} "
            + $"(ISO §7.3.{(directive == FlagDirective.Flag02 ? "14" : "15")}.2)",
            loc, default);
}
