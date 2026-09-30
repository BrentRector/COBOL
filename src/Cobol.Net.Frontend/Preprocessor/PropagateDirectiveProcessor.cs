// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>One <c>&gt;&gt;PROPAGATE</c> directive toggle (ISO/IEC 1989:2023 §7.3.21), anchored to its 1-based line
/// in the FINAL preprocessed text — directly comparable to an ANTLR token's <c>Start.Line</c>. The binder's
/// <c>PropagateState</c> folds these at each source element's first line to decide whether AUTOMATIC PROPAGATION
/// of exception conditions is enabled for that function, method or program (§7.3.21.4 GR1/GR3).</summary>
/// <param name="Line">The directive's 1-based line in the final preprocessed text. A directive applies to the
/// runtime elements that FOLLOW it (§7.3.21.4 GR1: "functions, methods, and programs that follow in the compilation
/// group"), so the fold is strict <c>Line &lt; elementLine</c>.</param>
/// <param name="On">ON — written, or implied by the bare directive, since §7.3.21.2 leaves ON un-underlined — vs
/// OFF.</param>
public sealed record PropagateEvent(int Line, bool On);

/// <summary>
/// The WiseOwl COBOL <c>&gt;&gt;PROPAGATE</c> directive stage (ISO/IEC 1989:2023 §7.3.21): the directive controls
/// AUTOMATIC propagation of an unhandled exception condition to the activating runtime element (GR1/GR2 — as though
/// a <c>GOBACK RAISING LAST</c> were executed), scoped over the functions/methods/programs that follow in the
/// compilation group; the default is <c>PROPAGATE OFF</c> (GR4). This stage COLLECTS the ON/OFF toggles into a
/// <see cref="DirectiveTimeline{T}"/> (the PUSH/POP history of §7.3.20 / §7.3.22 replayed over them, like every other
/// line-scoped directive) and blanks each line, line-count preserving (the <c>&gt;&gt;TURN</c> H3 discipline). The
/// binder folds the timeline per source element (<c>PropagateState</c>) and the emitter applies §14.6.13.1.3 6) and
/// §14.6.13.1.5's EXIT/GOBACK item 3 where the element's fatal default is decided (kb/Work PB1119).
/// <para><b>§7.3.21.3 SR1 (the directive shall not be specified WITHIN a compilation unit) is judged by the ONE
/// placement pass</b> — the <c>propagate-directive-2002</c> row's <c>directivePlacement</c> data,
/// <c>DirectivePlacementPass</c> (COBOLNET2652, kb/Work PB1378) — not here: this pre-parse, line-based stage has no
/// compilation-unit-boundary model. For legal source the fold at an element's first line is also the state at its
/// containing compilation unit's first line, because no directive may intervene.</para>
/// <para>Its INTRODUCTION edition is PROVISIONAL COBOL-2002 (the roadmap decision-1 policy, as for TYPEDEF / the FLOAT
/// trio): §7.3.21 is live in the 2023 spec — Annex E lists no removal — and it belongs to the 2002-era EC /
/// compiler-directive facility (the same era as <c>&gt;&gt;TURN</c>, gated at 2002).</para>
/// </summary>
public static class PropagateDirectiveProcessor
{
    /// <summary>Process <paramref name="text"/>: collect the toggle events and blank the directive lines.
    /// ⛔ It takes NO diagnostic channel: the EDITION question was answered once at the directive-recognition point
    /// by the <c>propagate-directive-2002</c> registry row (kb/Work PB725) and the OPERAND question by that row's
    /// <c>directiveOperand</c> column (kb/Work PB794), so this stage needs neither a dialect nor a bag.
    /// <paramref name="stackOps"/> are the PUSH/POP directives of the same text, replayed over the toggles
    /// (§7.3.20 / §7.3.22; kb/Work PB941). Line-count preserving.</summary>
    public static (string Text, DirectiveTimeline<PropagateEvent> Events) Process(
        string text, IReadOnlyList<DirectiveStackOp>? stackOps = null)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, DirectiveTimeline<PropagateEvent>.Empty);
        var lines = text.Split('\n');
        var events = new DirectiveEventLog<PropagateEvent>();
        for (int i = 0; i < lines.Length; i++)
        {
            // The ONE compiler-directive line parse (kb/Work PB794) — which also removes the §7.3.3 SR3/SR4
            // inline comment, so `>>PROPAGATE ON *> on` is legal source.
            if (!CompilerDirectiveLine.TryParse(lines[i], Keyword, out string operand)) continue;

            // Neither the EDITION nor the OPERAND is decided here: the introduction gate fired at the ONE
            // directive-recognition point (CompilerDirectiveCatalog, from the propagate-directive-2002 row — kb/Work
            // PB725) and §7.3.21.2's { ON | OFF } is that row's directiveOperand column (COBOLNET1911, kb/Work PB794).
            // A malformed operand was reported there and changes no state here.
            if (CompilerDirectiveCatalog.TryOperandWord(Keyword, operand, out string word)
                && word is "" or "ON" or "OFF")
                events.Add(Constructs.PropagateDirective2002, i + 1, new PropagateEvent(i + 1, word != "OFF"));
            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        return (string.Join('\n', lines), events.ToTimeline(stackOps ?? []));
    }

    /// <summary>The compiler-directive word this stage owns (ISO §7.3.21; the <c>propagate-directive-2002</c>
    /// row's single <c>directiveWords</c> entry).</summary>
    private const string Keyword = "PROPAGATE";
}
