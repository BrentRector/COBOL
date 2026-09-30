// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The CONTINUE AFTER timed-pause facility (ISO/IEC 1989:2023 §14.9.9 — a COBOL-2023 addition).
/// </summary>
public static class CobolTiming
{
    /// <summary>The implementor-defined maximum meaningful interval (§14.9.9.4 GR1 — "the implementor shall
    /// specify the maximum meaningful value"): a value above it suspends for the maximum. One day is a generous,
    /// non-blocking-forever cap.</summary>
    public const long MaxSeconds = 86_400;

    /// <summary>
    /// CONTINUE AFTER n SECONDS (ISO §14.9.9.4 GR1): suspend execution for <paramref name="seconds"/> seconds,
    /// then continue with the next statement. The implementor value of m (the fractional digit count of the
    /// temporary 9(n)V9(m) item) is 0 — fractional seconds truncate toward zero (the implicit COMPUTE without
    /// ROUNDED), which the integer-typed caller already applied. GR1a — a value below zero is forced to 0; GR1b —
    /// when <paramref name="checkLessThanZero"/> (EC-CONTINUE-LESS-THAN-ZERO checking was enabled at the statement),
    /// the nonfatal EC-CONTINUE-LESS-THAN-ZERO is set to exist (observable via FUNCTION EXCEPTION-STATUS), then
    /// execution continues (§14.6.13.1.4). A value above <see cref="MaxSeconds"/> suspends for the maximum.
    /// (The §14.6.13.1.4 selection of a matching USE declarative for this nonfatal condition is a scheduled
    /// follow-on — the last-exception status this sets is the observable behavior.)
    /// </summary>
    public static bool ContinueAfter(double seconds, bool checkLessThanZero)
    {
        // kb/Work PB138: screen NON-FINITE before anything — `(long)double.NaN` saturates to 0, so a NaN
        // interval silently skipped the suspension where §14.6.13.2 item 3 makes a NaN/±Inf sending operand
        // EC-DATA-NOT-FINITE (the CA10 checked raise; unchecked, no suspension is the documented benign
        // outcome — sleeping forever on +Inf is the one thing no reading licenses).
        if (!double.IsFinite(seconds))
        {
            ExceptionState.FloatNotFiniteError($"CONTINUE AFTER interval is {seconds} (ISO §14.6.13.2 item 3)");
            return false;
        }
        // GR1a/GR1b operate on arithmetic-expression-1's EVALUATED value (the sign test precedes the m=0
        // truncation), so a negative FRACTIONAL interval in (-1, 0) must still set the exception — test the sign of
        // the full-precision value, not a pre-truncated integer. The raise is REPORTED to the site (kb/Work
        // PB138) so the emitted §14.6.13.1.4 nonfatal dispatch can run — the recorded status used to be the
        // whole story and the golden's own generated handler pc was dead code.
        if (seconds < 0.0)
        {
            if (checkLessThanZero) { ExceptionState.Set("EC-CONTINUE-LESS-THAN-ZERO", fatal: false); return true; }
            return false;                                           // GR1a - value set to 0 → no suspension
        }
        SleepTruncated(WholeSeconds(seconds));                      // m = 0: truncate toward zero (GR1, no ROUNDED)
        return false;
    }

    /// <summary>⛔ THE INTERVAL'S WHOLE SECONDS, CLAMPED BEFORE THE NARROWING (kb/Work PB1529). §14.9.9.4 GR1: a value
    /// "greater than this maximum meaningful value" is replaced by the maximum meaningful value
    /// (<see cref="MaxSeconds"/>), so the comparison has to come BEFORE any cast — an unchecked <c>(long)</c> of a
    /// double past the long range is unspecified (it wrapped to a negative or small count, so a huge interval
    /// suspended 0 s or a few seconds instead of the maximum). Every value at or above the maximum IS the maximum;
    /// only a smaller one is truncated toward zero.</summary>
    private static long WholeSeconds(double seconds) => seconds >= MaxSeconds ? MaxSeconds : (long)seconds;

    /// <summary>The EXACT-lane overload (kb/Work PB138): a fixed-point or standard-decimal interval's
    /// binary64 image can round UP across an integer boundary (0.999… with enough nines converts to exactly
    /// 1.0), so the emitter hands the sign-test value AND the exactly-truncated seconds separately — GR1's
    /// implicit COMPUTE without ROUNDED truncates in the value's own domain, never in binary64.</summary>
    public static bool ContinueAfterExact(double fullPrecisionForSign, long truncatedSeconds, bool checkLessThanZero)
    {
        if (fullPrecisionForSign < 0.0)
        {
            if (checkLessThanZero) { ExceptionState.Set("EC-CONTINUE-LESS-THAN-ZERO", fatal: false); return true; }
            return false;
        }
        SleepTruncated(truncatedSeconds);
        return false;
    }

    private static void SleepTruncated(long secs)
    {
        if (secs <= 0) return;                                      // GR1 - no suspension for a zero interval
        int ms = (int)System.Math.Min(secs, MaxSeconds) * 1000;
        if (SuspensionObserver.Value is { } observe) { observe(ms); return; }
        System.Threading.Thread.Sleep(ms);
    }

    /// <summary>Test seam (kb/Work PB1590): when set, a suspension is REPORTED (its length in milliseconds) instead
    /// of performed, so a test asserts WHETHER and HOW LONG the run unit would suspend without timing a real sleep
    /// with a stopwatch — a wall-clock ceiling a loaded CI runner can breach. An <see cref="System.Threading.AsyncLocal{T}"/>,
    /// so only the calling test's own flow observes.</summary>
    internal static readonly System.Threading.AsyncLocal<System.Action<int>?> SuspensionObserver = new();
}
