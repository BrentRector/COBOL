// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>One <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> directive toggle (ISO/IEC 1989:2023 §7.3.23), anchored to its
/// 1-based line in the FINAL preprocessed text — directly comparable to an ANTLR token's <c>Start.Line</c>. The
/// compile-time <see cref="Binding.RefModZeroLengthState"/> folds these to decide whether a reference modification
/// on a given line ALLOWS a zero-length result (§8.4.3.3.4 item 5c).</summary>
/// <param name="Line">The directive's 1-based line in the final preprocessed text. A directive applies to the
/// text that FOLLOWS it (the <c>&gt;&gt;TURN</c> GR5 discipline); a directive occupies its own whole line after
/// free-form normalization, so the fold is strict <c>Line &lt; siteLine</c>.</param>
/// <param name="On">ON (zero-length allowed) vs OFF (the §7.3.23.3 GR1 default — a zero-length ref-mod raises
/// EC-BOUND-REF-MOD).</param>
public sealed record RefModZeroLengthEvent(int Line, bool On);

/// <summary>
/// The WiseOwl COBOL <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> directive stage (ISO §7.3.23; greenfield-only — the legacy
/// pipeline keeps consuming the directive via <see cref="ConditionalCompilationProcessor"/>'s
/// <c>KnownIgnoredDirectives</c>): parses each surviving <c>&gt;&gt;REF-MOD-ZERO-LENGTH {ON | OFF}</c> line of the
/// FINAL preprocessed text into a <see cref="RefModZeroLengthEvent"/>, edition-gates the directive (the
/// introduction gate — §7.3.23 is a COBOL-2023 addition, Annex E.3.3 item 23 — routed through the ONE
/// <see cref="ConstructRegistry"/> so the message is single-sourced and the construct enters the version matrix),
/// and blanks the line (line-count preserving, so every later token line number is stable — the <c>&gt;&gt;TURN</c>
/// H3 discipline).
/// </summary>
public static class RefModZeroLengthDirectiveProcessor
{
    private const string Keyword = "REF-MOD-ZERO-LENGTH";   // 19 characters

    /// <summary>Process <paramref name="text"/>: collect the toggle events and blank the directive lines.
    /// ⛔ It takes NO diagnostic channel. The introduction gate (COBOLNET0900, kb/Work PB725) and the OPERAND
    /// check (COBOLNET1911, kb/Work PB794) both fire at the ONE directive-recognition point, from the same
    /// registry row, so this stage has nothing left to report — the same removal PB725 made to four stages'
    /// dialect parameters. Line-count preserving.</summary>
    /// <para><paramref name="stackOps"/> are the PUSH/POP directives of the same text, replayed over the toggles
    /// (§7.3.20 / §7.3.22; kb/Work PB941).</para>
    public static (string Text, DirectiveTimeline<RefModZeroLengthEvent> Events) Process(
        string text, IReadOnlyList<DirectiveStackOp>? stackOps = null)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, DirectiveTimeline<RefModZeroLengthEvent>.Empty);
        var lines = text.Split('\n');
        var events = new DirectiveEventLog<RefModZeroLengthEvent>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (!CompilerDirectiveLine.TryParse(lines[i], Keyword, out string operand)) continue;

            // The introduction gate (§7.3.23 is a COBOL-2023 addition) already fired at the ONE
            // directive-recognition point — CompilerDirectiveCatalog, from the ref-mod-zero-length-2023 row's
            // directiveWords (kb/Work PB725) — and so did the OPERAND check, from the same row's
            // directiveOperand column (kb/Work PB794: §7.3.23.2's { ON | OFF }, with ON un-underlined so a bare
            // >>REF-MOD-ZERO-LENGTH selects it). This stage collects the toggles; it re-decides neither, and it
            // no longer re-implements the >> / keyword / operand slicing that missed a §7.3.3 SR3 inline comment.
            if (CompilerDirectiveCatalog.TryOperandWord(Keyword, operand, out string word)
                && word is "" or "ON" or "OFF")
                events.Add(Constructs.RefModZeroLength2023, i + 1, new RefModZeroLengthEvent(i + 1, word != "OFF"));
            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        return (string.Join('\n', lines), events.ToTimeline(stackOps ?? []));
    }
}
