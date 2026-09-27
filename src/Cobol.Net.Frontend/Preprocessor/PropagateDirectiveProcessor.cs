// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The WiseOwl COBOL <c>&gt;&gt;PROPAGATE</c> directive stage (ISO/IEC 1989:2023 §7.3.21): the directive controls
/// AUTOMATIC propagation of an unhandled exception condition to the activating runtime element (GR1/GR2 — as though
/// a <c>GOBACK RAISING LAST</c> were executed), scoped over the functions/methods/programs that follow in the
/// compilation group; the default is <c>PROPAGATE OFF</c> (GR4). <b>§7.3.21.3 SR1 (the directive shall not be
/// specified WITHIN a compilation unit) is NOT enforced</b> — this pre-parse, line-based stage has no
/// compilation-unit-boundary awareness, and blanking the line before parsing means no downstream stage can catch a
/// misplaced directive, so a <c>&gt;&gt;PROPAGATE</c> inside a unit is recognized and consumed. A documented
/// limitation (a placement-diagnostic follow-up), not a silent mis-compile of well-placed source.
/// <para>This stage RECOGNIZES the directive and EDITION-GATES it (the introduction gate). Its INTRODUCTION edition
/// is PROVISIONAL COBOL-2002 (the roadmap decision-1 policy, as for TYPEDEF / the FLOAT trio): §7.3.21 is live in
/// the 2023 spec — Annex E lists no removal — and it belongs to the 2002-era EC / compiler-directive facility (the
/// same era as <c>&gt;&gt;TURN</c>, gated at 2002), but the exact 2002-vs-2014 edge cannot be pinned from the 2023
/// text or the VCR; refine against the 1989:2002 standard when available. The RUNTIME semantics (actually driving
/// EC propagation) are the deferred PHASE-13 EC-remnant work — this stage does NOT implement them, so a
/// well-formed <c>&gt;&gt;PROPAGATE ON</c> is recognized-and-edition-gated but does not yet change EC behavior; it
/// is never a silent stray token nor a parse error. The line is blanked (line-count preserving, so every later
/// token line number is stable — the <c>&gt;&gt;TURN</c> H3 discipline).</para>
/// </summary>
public static class PropagateDirectiveProcessor
{
    /// <summary>Process <paramref name="text"/>: a <c>&gt;&gt;PROPAGATE</c> is recognized and blanked, its runtime
    /// semantics deferred to PHASE-13. Line-count preserving. ⛔ It takes NO diagnostic channel: the EDITION
    /// question was answered once at the directive-recognition point by the <c>propagate-directive-2002</c>
    /// registry row (kb/Work PB725) and the OPERAND question by that row's <c>directiveOperand</c> column
    /// (kb/Work PB794), so this stage needs neither a dialect nor a bag.</summary>
    public static string Process(string text)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return text;
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            // The ONE compiler-directive line parse (kb/Work PB794) — which also removes the §7.3.3 SR3/SR4
            // inline comment this stage's own slicing did not know about, so `>>PROPAGATE ON *> on` is legal
            // source again rather than a malformed-operand error.
            if (!CompilerDirectiveLine.TryParse(lines[i], Keyword, out _)) continue;

            // Neither the EDITION nor the OPERAND is decided here. The introduction gate fired at the ONE
            // directive-recognition point (CompilerDirectiveCatalog, from the propagate-directive-2002 row) —
            // this stage ran its own `if (dialectLevel < 2002)` with a BESPOKE COBOLNET0883 until kb/Work PB725
            // — and §7.3.21.2's { ON | OFF } is that row's directiveOperand column, checked by the same funnel
            // as COBOLNET1911 since kb/Work PB794, which retired the rest of COBOLNET0883. What is left for this
            // stage is the disposition of the line; the runtime propagation effect is PHASE-13.
            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        return string.Join('\n', lines);
    }

    /// <summary>The compiler-directive word this stage owns (ISO §7.3.21; the <c>propagate-directive-2002</c>
    /// row's single <c>directiveWords</c> entry).</summary>
    private const string Keyword = "PROPAGATE";
}
