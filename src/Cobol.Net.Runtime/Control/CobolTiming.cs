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
    /// for the maximum. A NON-FINITE interval takes the rule of its kind (kb/Work PB2841): -Infinity is a value
    /// below zero, +Infinity a value above the maximum, and a NaN, which is no number, a zero interval.
    /// </summary>
    public static bool ContinueAfter(double seconds, bool checkLessThanZero)
    {
        // ⛔ NO EC-DATA-NOT-FINITE SCREEN HERE (kb/Work PB2841, which corrects PB138's). §14.6.13.2 item 3 raises
        // EC-DATA-NOT-FINITE for a STANDARD floating-point SENDING OPERAND only, and that raise is made where such an
        // operand is READ (CobolFloat.Sending, wrapped by the emitter), so a NaN or an infinity that reaches this
        // method came from a NON-standard usage (FLOAT-LONG, COMP-2) or from a standard one with its checking off:
        // the standard names no condition for it, and every value takes the rule GR1 gives a value of its kind
        // (PB2647's determination: an infinity is a number in ISO/IEC 60559, a NaN is not):
        //   -Infinity  a value less than zero → GR1 a-c (the sign test below, the saturating reader gives long.MinValue);
        //   +Infinity  a number greater than the maximum meaningful value → the maximum (MaxSeconds), like 1E30;
        //   NaN        no number, so no seconds: CobolNum.PositionOf reads it as 0 (the disposition CobolFloat.ToScaled
        //              gives a NaN landing in a fixed-point receiver), which is a zero interval — no suspension and
        //              no condition (`seconds < 0.0` is false for a NaN).
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
