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

    // ⛔ ONE SUSPENSION RULE, ONE OVERLOAD PER CARRIER (kb/Work PB1529). The interval is the value of an
    // arithmetic expression, and the emitter hands it over on the carrier it evaluated on: the scaled Int128, the
    // unsigned-wide UInt128, the SDIDI CobolDec or binary64 (the NumXCarrier family). Each overload takes the sign
    // (GR1 a-c) and the whole seconds (GR1's implicit COMPUTE without ROUNDED, m = 0) from the value IN ITS OWN
    // DOMAIN, exactly, and narrows through the carrier's saturating position reader (CobolNum.PositionOf). A value
    // past the long range therefore stays "greater than the maximum meaningful value" and is replaced by it, where
    // an emitter-side (long) cast, or the low-order-digits landing a store may take (CobolDec.ToUnscaled), wrapped a
    // huge interval into a short or a zero suspension. The value is evaluated ONCE (the former exact lane rendered
    // the expression twice, once for its sign and once for its seconds).

    /// <summary>
    /// CONTINUE AFTER n SECONDS (ISO §14.9.9.4 GR1) for a BINARY64 interval: suspend execution for
    /// <paramref name="seconds"/> seconds, then continue with the next statement. The implementor value of m (the
    /// fractional digit count of the temporary 9(n)V9(m) item) is 0 — fractional seconds truncate toward zero (the
    /// implicit COMPUTE without ROUNDED). GR1a — a value below zero is forced to 0; GR1b — when
    /// <paramref name="checkLessThanZero"/> (EC-CONTINUE-LESS-THAN-ZERO checking was enabled at the statement), the
    /// nonfatal EC-CONTINUE-LESS-THAN-ZERO is set to exist and REPORTED to the site by the return value, which runs
    /// the §14.6.13.1.4 USE-declarative selection (kb/Work PB138). A value above <see cref="MaxSeconds"/> suspends
    /// for the maximum.
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
        // truncation), so a negative FRACTIONAL interval in (-1, 0) must still set the exception.
        return Continue(seconds < 0.0, CobolNum.PositionOf(seconds), checkLessThanZero);
    }

    /// <summary>The scaled-<see cref="Int128"/> interval (a fixed-point item or expression: <paramref name="unscaled"/>
    /// × 10^-<paramref name="scale"/>). The sign is the unscaled value's, exactly, so a fraction in (-1, 0) still
    /// raises GR1b while truncating to 0 seconds, and a binary64 image that would round across an integer boundary
    /// is never consulted (kb/Work PB138).</summary>
    public static bool ContinueAfter(Int128 unscaled, int scale, bool checkLessThanZero) =>
        Continue(unscaled < 0, CobolNum.PositionOf(unscaled, scale), checkLessThanZero);

    /// <summary>The unsigned-wide interval (a 16-byte unsigned COMP-5 item's full [0, 2^128) range, kb/Work R10):
    /// never negative, and every value past the <see cref="long"/> range is above the maximum.</summary>
    public static bool ContinueAfter(UInt128 unscaled, int scale, bool checkLessThanZero) =>
        Continue(false, CobolNum.PositionOf(unscaled, scale), checkLessThanZero);

    /// <summary>The standard-decimal interval (§8.8.1.5 SDIDI: an expression the arithmetic planner lifted to
    /// <see cref="CobolDec"/>). The sign is the significand's and the whole seconds are the saturating truncation
    /// <see cref="CobolNum.PositionOf(CobolDec)"/> reads, so 10^40 suspends for the maximum and not for the zero its
    /// 38 low-order digits make.</summary>
    public static bool ContinueAfter(CobolDec seconds, bool checkLessThanZero) =>
        Continue(seconds.Sig < 0, CobolNum.PositionOf(seconds), checkLessThanZero);

    /// <summary>GR1 a-c and the suspension, once: a value below zero is set to 0 and raises the nonfatal
    /// EC-CONTINUE-LESS-THAN-ZERO when its checking is enabled (the return value reports it to the site); any
    /// other value suspends for its whole seconds, never above <see cref="MaxSeconds"/>.</summary>
    private static bool Continue(bool negative, long wholeSeconds, bool checkLessThanZero)
    {
        if (negative)
        {
            if (checkLessThanZero) { ExceptionState.Set("EC-CONTINUE-LESS-THAN-ZERO", fatal: false); return true; }
            return false;                                           // GR1a - value set to 0 → no suspension
        }
        if (wholeSeconds <= 0) return false;                        // GR1 - no suspension for a zero interval
        int ms = (int)System.Math.Min(wholeSeconds, MaxSeconds) * 1000;
        if (SuspensionObserver.Value is { } observe) { observe(ms); return false; }
        System.Threading.Thread.Sleep(ms);
        return false;
    }

    /// <summary>Test seam (kb/Work PB1590): when set, a suspension is REPORTED (its length in milliseconds) instead
    /// of performed, so a test asserts WHETHER and HOW LONG the run unit would suspend without timing a real sleep
    /// with a stopwatch — a wall-clock ceiling a loaded CI runner can breach. An <see cref="System.Threading.AsyncLocal{T}"/>,
    /// so only the calling test's own flow observes.</summary>
    internal static readonly System.Threading.AsyncLocal<System.Action<int>?> SuspensionObserver = new();
}
