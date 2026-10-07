// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⛔ THE FORMAT-3 PERFORM DECISION, WRITTEN ONCE. An inline PERFORM is Format 3 (exception-checking) iff it carries
/// any WHEN phrase (ordinary / OTHER / COMMON), a FINALLY phrase, or a [WITH] LOCATION head (ISO §14.9.28.2
/// Format 3). Every reader asks THIS predicate — the binder's format dispatch, the COBOLNET0900 introduction gate
/// (<c>VersionConformancePass.VisitPerformStatement</c>) and the front end's §14.9.28.4 GR14 implicit PUSH ALL /
/// POP ALL placement (<see cref="Preprocessor.ExceptionPerformDirectiveScope"/>) — so the three cannot drift. It
/// lives in the front end because the last of them runs before binding: the implicit POP ALL must reach the
/// conditional-compilation driver's state, which only the front end holds (kb/Work PB1066).
/// </summary>
public static class PerformFormat
{
    /// <summary>True when <paramref name="p"/> is an exception-checking (Format-3) PERFORM.</summary>
    public static bool IsFormat3(CobolParserCore.PerformStatementContext p) =>
        p.performWhenPhrase().Length > 0 || p.performWhenOther() is not null
        || p.performWhenCommon() is not null || p.performFinally() is not null
        || p.performInlineHead()?.performLocationPhrase() is not null;

    // ── §14.9.28.3 SR8 — the two facts the rule asks of a PERFORM statement, written once (kb/Work PB434) ──────
    // "The UNTIL EXIT phrase shall not be specified in a PERFORM statement with or under a PERFORM statement with
    // the VARYING phrase or either the TEST BEFORE or TEST AFTER phrase." The binder asks both of the statement it
    // binds (the "with" arm) and the second of every PERFORM whose range it is inside (the "under" arm).

    /// <summary>Every loop-control phrase WRITTEN on <paramref name="p"/>, in whichever of the three tree shapes the
    /// grammar gives it: a direct child (the out-of-line <c>PERFORM proc TIMES|UNTIL|VARYING</c> alternatives), the
    /// THRU form's <c>performOptions</c>, or the inline head's list (a superset: more than one is COBOLNET2117).</summary>
    private static IEnumerable<Antlr4.Runtime.ParserRuleContext> LoopControlPhrases(CobolParserCore.PerformStatementContext p)
    {
        if (p.performUntil() is { } u) yield return u;
        if (p.performVarying() is { } v) yield return v;
        CobolParserCore.PerformOptionsContext[] options =
            p.performOptions() is { } o ? [o] : p.performInlineHead()?.performOptions() ?? [];
        foreach (var opt in options)
        {
            if (opt.performUntil() is { } ou) yield return ou;
            if (opt.performVarying() is { } ov) yield return ov;
        }
    }

    /// <summary>Does <paramref name="p"/> specify the UNTIL EXIT phrase — the until-phrase's EXIT alternative, or the
    /// superset EXIT the grammar admits after a varying-phrase's own UNTIL or an AFTER level's?</summary>
    public static bool SpecifiesUntilExit(CobolParserCore.PerformStatementContext p) =>
        LoopControlPhrases(p).Any(phrase => phrase switch
        {
            CobolParserCore.PerformUntilContext u => u.performUntilTarget().EXIT() is not null,
            CobolParserCore.PerformVaryingContext v => v.performUntilTarget().EXIT() is not null
                || v.performVaryingAfter().Any(a => a.performUntilTarget().EXIT() is not null),
            _ => false,
        });

    /// <summary>Is <paramref name="p"/> "a PERFORM statement with the VARYING phrase or either the TEST BEFORE or
    /// TEST AFTER phrase"? ⚠ The TEST phrase must be WRITTEN: §14.9.28.3 SR1 ("If neither the TEST BEFORE nor the
    /// TEST AFTER phrase is specified, the TEST BEFORE phrase is assumed") cannot make an assumed phrase count here,
    /// because SR1 governs the until-phrase that UNTIL EXIT itself is, so SR8 would then forbid every UNTIL EXIT.</summary>
    public static bool SpecifiesVaryingOrTest(CobolParserCore.PerformStatementContext p) =>
        LoopControlPhrases(p).Any(phrase => phrase is CobolParserCore.PerformVaryingContext
            || phrase is CobolParserCore.PerformUntilContext { } u && u.TEST() is not null);
}
