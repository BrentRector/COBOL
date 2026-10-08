// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The ONE power-of-ten source for the runtime (DESIGN-runtime-library §2.3): the type-shaped views over
/// tables computed once at type initialization. Replaces the six identical multiply-loop copies that recomputed
/// these compile-time constants on every numeric store/rescale/format (<c>CobolNum.Pow10/Pow10Wide</c>,
/// <c>CobolDec.Pow10</c>, <c>CobolDate.Pow10</c>, <c>CobolIntrinsics.Pow10D/Pow10I</c>, <c>CobolFloat.Pow10</c>).
/// The tables are built by the same cumulative ×10 recurrence the deleted loops used, so every value in range is
/// bit-identical to what the loops produced. ⛔ AN OUT-OF-TABLE EXPONENT IS A LOUD FAILURE, NEVER A VALUE
/// (kb/Work PB2633, PB648): the fallback loop the tables replaced silently WRAPPED the Int128 past 10^38 and
/// answered 1 for a negative exponent, so a caller that forgot a bound — <c>FromDoubleBounded</c> at a
/// trailing-P receiver's scale of -2 asked for 10^39 — computed with a plausible wrong power. A caller that can
/// legitimately reach an out-of-table exponent handles that case itself, where the scale is known.
/// </summary>
internal static class Pow10
{
    private static readonly long[] L = BuildLong();      // 10^0 .. 10^18 — the long-safe range
    private static readonly Int128[] W = BuildWide();    // 10^0 .. 10^38 — the wide-intermediate range (SSOT §18 #4)
    private static readonly Int128[] F = BuildFive();    // 5^0 .. 5^54 — 10^n's ODD COFACTOR (kb/Work PB623)

    /// <summary>10^<paramref name="n"/> as a <see cref="long"/> (n in 0..18 — within long's range).</summary>
    public static long AsLong(int n) => L[n];

    /// <summary>10^<paramref name="n"/> as an <see cref="Int128"/> (n in 0..38 — the wide intermediate range,
    /// COBOLNET_DESIGN §18 #4; an out-of-range n throws — 10^39 does not fit the carrier and a negative power is
    /// not an integer).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is outside 0..38.</exception>
    public static Int128 AsWide(int n)
    {
        if ((uint)n >= (uint)W.Length) throw OutOfTable(nameof(AsWide), n, W.Length - 1);
        return W[n];
    }

    /// <summary>5^<paramref name="n"/> as an <see cref="Int128"/> (n in 0..54 — 5^54 &lt; 2^126, the widest power
    /// of five the carrier holds; an out-of-table n throws, as <see cref="AsWide"/> does). This is 10^n's ODD COFACTOR
    /// (10^n = 5^n·2^n), which is what an EXACT binary64 expansion needs: a double is ±m·2^e, so
    /// m·10^n = m·5^n·2^(e+n) turns a decimal rescaling into ONE integer multiply and ONE shift with no rounding
    /// anywhere (<c>CobolFloat.TryExactScaled</c>, kb/Work PB623).
    /// <para>⛔ THERE IS DELIBERATELY NO <c>double</c> VIEW HERE ANY MORE. The 10^n-as-double table this replaced
    /// existed only to scale a binary64 before landing it in a fixed-point receiver, and that multiply is exactly
    /// the defect PB623 removed: past 2^53 the PRODUCT is rounded, so the landing answered with a value the sender
    /// never held. (The table was doubly unfit for it — built by a cumulative ×10 recurrence, its entries past
    /// 10^22 are not even the correctly-rounded powers of ten.) A caller that wants 10^n as a double is either
    /// converting a scaled value — <c>CobolFloat.ScaledToDouble</c>, whose own exact-power table is bounded at
    /// 10^22 for that reason — or reintroducing PB623.</para></summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is outside 0..54.</exception>
    public static Int128 FiveAsWide(int n)
    {
        if ((uint)n >= (uint)F.Length) throw OutOfTable(nameof(FiveAsWide), n, F.Length - 1);
        return F[n];
    }

    private static ArgumentOutOfRangeException OutOfTable(string method, int n, int max) =>
        new(nameof(n), n, $"Pow10.{method}: the exponent must be in 0..{max}; a wider power does not fit the Int128 carrier "
            + "and a negative one is not an integer, so the caller handles that case where the scale is known");

    private static long[] BuildLong()
    {
        var t = new long[19];
        long r = 1;
        for (int i = 0; i < t.Length; i++) { t[i] = r; if (i < t.Length - 1) r *= 10; }
        return t;
    }

    private static Int128[] BuildWide()
    {
        var t = new Int128[39];
        Int128 r = 1;
        for (int i = 0; i < t.Length; i++) { t[i] = r; if (i < t.Length - 1) r *= 10; }
        return t;
    }

    private static Int128[] BuildFive()
    {
        var t = new Int128[55];
        Int128 r = 1;
        for (int i = 0; i < t.Length; i++) { t[i] = r; if (i < t.Length - 1) r *= 5; }
        return t;
    }
}
