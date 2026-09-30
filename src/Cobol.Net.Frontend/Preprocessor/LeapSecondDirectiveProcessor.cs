// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>One <c>&gt;&gt;LEAP-SECOND</c> directive toggle (ISO/IEC 1989:2023 §7.3.17), anchored to its 1-based line
/// in the FINAL preprocessed text — directly comparable to an ANTLR token's <c>Start.Line</c>. The binder's
/// <c>LeapSecondState</c> folds these at each compilation unit's first line (§7.3.4 GR5: a directive "applies to all
/// of the source text and library text that follows"), so the strict <c>Line &lt; unitLine</c> is the fold.</summary>
/// <param name="Line">The directive's 1-based line in the final preprocessed text.</param>
/// <param name="On">ON — written, or implied by the bare directive, since §7.3.17.2 leaves ON un-underlined — vs OFF.</param>
public sealed record LeapSecondEvent(int Line, bool On);

/// <summary>
/// The <c>&gt;&gt;LEAP-SECOND [ON | OFF]</c> compiler directive (ISO/IEC 1989:2023 §7.3.17) — the compilation fact every
/// §15.3 date/time consumer reads (kb/Work PB65, AR-15.79.3-4): with ON in effect a formatted-time argument's seconds
/// subfield may be 60 (§15.3.3.3 — "less than 61 when the LEAP-SECOND directive with the ON phrase is in effect") and
/// a standard numeric time form value is bounded at 86,401 (§7.3.17.4 GR4) instead of 86,400 (GR5). The directive used
/// to be CONSUMED and DISCARDED (<c>ConditionalCompilationProcessor</c>'s known-ignored set), so
/// <c>SECONDS-FROM-FORMATTED-TIME("hhmmss", "235960")</c> under <c>&gt;&gt;LEAP-SECOND ON</c> answered 0 (and killed the
/// run unit under EC-ARGUMENT-FUNCTION checking) where §15.79.4 requires 86,400.
/// <para>The reported side of the directive — whether a value greater than 59 is REPORTED in the seconds position
/// of ACCEPT … FROM TIME / CURRENT-DATE / FORMATTED-CURRENT-DATE / WHEN-COMPILED, and whether SECONDS-PAST-MIDNIGHT
/// may return ≥ 86,400 (GR2, GR4) — is implementor-defined and answered "never" (docs/CONFORMANCE.md A.1 items 111 and 112:
/// the .NET clock has no leap seconds), so ON changes only what the program may PRESENT as an argument.</para>
/// <para>This stage COLLECTS the ON/OFF toggles into a <see cref="DirectiveTimeline{T}"/> (the PUSH/POP history of
/// §7.3.20 / §7.3.22 replayed over them, like every other line-scoped directive) and blanks each line, line-count
/// preserving (the <c>&gt;&gt;TURN</c> H3 discipline). §7.3.4 GR5 — "A compiler directive applies to all of the source
/// text and library text that follows" — makes the state PER UNIT: <c>LeapSecondState</c> folds the timeline at each
/// unit's first line, so a directive BETWEEN two sibling units (outside both, and legal) governs the units after it
/// and not the ones before. §7.3.17.4 GR1 — absent, OFF is implied. The word ON is optional in the printed format
/// (only OFF is underlined — the figure note at §7.3.17.2), so a bare <c>&gt;&gt;LEAP-SECOND</c> selects ON.</para>
/// <para><b>§7.3.17.3 SR1 (the directive shall not be specified within a compilation unit) is judged by the ONE
/// placement pass</b> — the row's <c>directivePlacement</c> data, <c>DirectivePlacementPass</c> (kb/Work PB1378) —
/// not here: this stage holds no compilation-unit-boundary model. It used to latch "inside a unit" at the first
/// unit's first line and never clear it, which rejected the legal between-units directive and gave the whole group
/// one bool.</para>
/// </summary>
public static class LeapSecondDirectiveProcessor
{
    private const string Keyword = "LEAP-SECOND";

    /// <summary>Process <paramref name="text"/>: collect the toggle events and blank the directive lines.
    /// ⛔ It takes NO diagnostic channel: the EDITION question was answered once at the directive-recognition point
    /// (<c>CompilerDirectiveCatalog</c>, kb/Work PB725), the OPERAND question by the row's <c>directiveOperand</c>
    /// column (COBOLNET1911, kb/Work PB794) and the PLACEMENT question by <c>DirectivePlacementPass</c>.
    /// <paramref name="stackOps"/> are the PUSH/POP directives of the same text, replayed over the toggles
    /// (§7.3.20 / §7.3.22; kb/Work PB941). Line-count preserving.</summary>
    public static (string Text, DirectiveTimeline<LeapSecondEvent> Events) Process(
        string text, IReadOnlyList<DirectiveStackOp>? stackOps = null)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, DirectiveTimeline<LeapSecondEvent>.Empty);
        var lines = text.Split('\n');
        var events = new DirectiveEventLog<LeapSecondEvent>();
        for (int i = 0; i < lines.Length; i++)
        {
            // The ONE compiler-directive line parse (kb/Work PB794) — it removes the §7.3.3 SR3/SR4 inline
            // comment this stage's own slicing did not know about, so `>>LEAP-SECOND ON *> on` folds ON instead
            // of drawing a malformed-operand error.
            if (!CompilerDirectiveLine.TryParse(lines[i], Keyword, out string operand)) continue;
            if (CompilerDirectiveCatalog.TryOperandWord(Keyword, operand, out string word)
                && word is "" or "ON" or "OFF")
                events.Add(Constructs.LeapSecondDirective2002, i + 1, new LeapSecondEvent(i + 1, word != "OFF"));   // §7.3.17.2: ON is un-underlined, so a bare >>LEAP-SECOND selects it
            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        return (string.Join('\n', lines), events.ToTimeline(stackOps ?? []));
    }
}
