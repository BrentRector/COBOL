// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The <see cref="CobolDynString"/> receiving-store and SET-SIZE rules for a DYNAMIC LENGTH elementary item
/// (ISO §8.5.1.10.4 / §13.18.19, COBOL-2014; §14.9.39 Format 16, COBOL-2023): the sender REPLACES the content and
/// the new length is the sending length, TRUNCATED ON THE RIGHT at the item's MAXIMUM SIZE with NO padding (the
/// minimum length is zero, §13.18.19.4 GR1 — the difference from <see cref="CobolString.Store"/>, which space-pads
/// to a fixed width).
/// <para>⛔ EVERY entry point takes the §8.5.1.10.1 MAXIMUM SIZE, and <see cref="CobolDynString.MaxSizeOf"/> is the
/// one place that rule is written, so there is no "no LIMIT phrase" sentinel for a clamp to skip. It used to be
/// <c>-1</c>, and the clamp DID skip it (kb/Work PB463): a SET SIZE request of 2³²+k wrapped to k — a length nobody
/// asked for, no condition, exit 0 — and a request between <see cref="int.MaxValue"/> and 2³² narrowed NEGATIVE and
/// threw a raw <c>ArgumentOutOfRangeException</c> out of generated code. The two halves below are what make that
/// unreachable: the maximum is real for every item, and the clamp is applied to the value AS WRITTEN, before any
/// narrowing.</para>
/// </summary>
public sealed class CobolDynStringTests
{
    /// <summary>A sender shorter than the maximum size is stored as-is — its length becomes the sending length
    /// (no pad).</summary>
    [Theory]
    [InlineData("HI", 20, "HI")]
    [InlineData("HELLO", 20, "HELLO")]
    [InlineData("EXACTLY-TEN", 11, "EXACTLY-TEN")]   // exact fit at the maximum
    public void Store_ShorterThanMaximum_KeepsSendingLength(string value, int maxSize, string expected)
        => Assert.Equal(expected, CobolDynString.Store(value, maxSize));

    /// <summary>A sender longer than the maximum size is truncated ON THE RIGHT to it (§8.5.1.10.4 — "if the
    /// maximum length is reached, the value is truncated on the right as necessary").</summary>
    [Theory]
    [InlineData("ABCDEFGHIJ", 5, "ABCDE")]
    [InlineData("TRUNCATED-BEYOND-LIMIT", 9, "TRUNCATED")]
    public void Store_LongerThanMaximum_TruncatesRight(string value, int maxSize, string expected)
        => Assert.Equal(expected, CobolDynString.Store(value, maxSize));

    /// <summary>The minimum length is zero: an empty or null sender yields length 0 (§13.18.19.4 GR1).</summary>
    [Theory]
    [InlineData("", 20)]
    [InlineData(null, 20)]
    [InlineData("", 0)]
    public void Store_EmptyOrNull_YieldsLengthZero(string? value, int maxSize)
        => Assert.Equal("", CobolDynString.Store(value, maxSize));

    /// <summary>An item written with NO LIMIT phrase takes the implementor maximum (§13.18.19.4 GR2), which is a
    /// real bound and not "unbounded" — an ordinary sender is still stored in full under it.</summary>
    [Theory]
    [InlineData("A VERY LONG UNBOUNDED STRING VALUE")]
    [InlineData("SEED")]
    public void Store_NoLimitPhrase_StoresInFullUnderTheImplementorMaximum(string value)
        => Assert.Equal(value, CobolDynString.Store(value, CobolDynString.MaxSizeOf(null)));

    // ── §8.5.1.10.1 — THE maximum size, "the smallest of" ──────────────────────────────────────────────────────

    /// <summary>No LIMIT phrase ⇒ the implementor maximum (§13.18.19.4 GR2), never "no maximum".</summary>
    [Fact]
    public void MaxSizeOf_NoLimitPhrase_IsTheImplementorMaximum()
        => Assert.Equal(CobolDynString.MaxLength, CobolDynString.MaxSizeOf(null));

    /// <summary>A LIMIT phrase below the implementor maximum IS the maximum size — it is the smaller of the
    /// candidates §8.5.1.10.1 lists when the item names no PREFIXED structure.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(65_535)]
    public void MaxSizeOf_LimitBelowTheImplementorMaximum_IsTheLimit(int limit)
        => Assert.Equal(limit, CobolDynString.MaxSizeOf(limit));

    /// <summary>⛔ THE DEFECT'S COMPILE-SIDE HALF (kb/Work PB463). A LIMIT phrase AT or ABOVE the implementor
    /// maximum yields the implementor maximum — §8.5.1.10.1 takes the SMALLEST of the candidates. It used to be
    /// read by an <c>int.TryParse</c> that simply FAILED on such a literal and left the item at the <c>-1</c>
    /// "no bound at all" sentinel, so writing a LIMIT too large removed the bound instead of lowering it.</summary>
    [Theory]
    [InlineData(0x3FFF_FFDFL)]                 // exactly the implementor maximum
    [InlineData(0x3FFF_FFE0L)]                 // one past it
    [InlineData(2_147_483_647L)]               // int.MaxValue — the old parse still took this one
    [InlineData(4_294_967_306L)]               // past 2³² — the old parse dropped it
    public void MaxSizeOf_LimitAtOrAboveTheImplementorMaximum_IsTheImplementorMaximum(long limit)
        => Assert.Equal(CobolDynString.MaxLength, CobolDynString.MaxSizeOf(limit));

    /// <summary>Every maximum size is a real bound inside the carrier's own capacity, so a <c>(int)</c> narrowing
    /// of a clamped length is exact BY CONSTRUCTION rather than by inspection.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData(7L)]
    [InlineData(4_294_967_306L)]
    public void MaxSizeOf_IsAlwaysWithinTheCarriersCapacity(long? limit)
    {
        int max = CobolDynString.MaxSizeOf(limit is { } v ? (Int128)v : null);
        Assert.InRange(max, 0, CobolDynString.MaxLength);
    }

    /// <summary>§8.5.1.10.1's PREFIXED candidate (kb/Work PB829): the length field of the item's
    /// dynamic-length-structure-name bounds the maximum size — "the smallest of" the three — so a SHORT PREFIXED
    /// field (65535, §12.3.7.4 GR18) beats an absent or larger LIMIT, a smaller LIMIT beats the field, and a
    /// 32-bit field never raises the implementor maximum.</summary>
    [Theory]
    [InlineData(null, 65_535L, 65_535)]
    [InlineData(100_000L, 32_767L, 32_767)]
    [InlineData(10L, 65_535L, 10)]
    [InlineData(null, 4_294_967_295L, 0x3FFF_FFDF)]
    [InlineData(null, 2_147_483_647L, 0x3FFF_FFDF)]
    public void MaxSizeOf_PrefixedCandidate_IsTheSmallestOfTheThree(long? limit, long prefixed, int expected)
        => Assert.Equal(expected, CobolDynString.MaxSizeOf(limit is { } v ? (Int128)v : null, prefixed));

    // ── §14.9.39 Format 16 GR37–GR39 — SET [SIZE OF] ───────────────────────────────────────────────────────────

    /// <summary>GR37 — a value that does not evaluate to a nonnegative number sets the length to 0. The sign test
    /// precedes the toward-zero truncation, so a fractional negative in (−1,0) is caught.</summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(-0.5)]
    public void SetSize_NotNonnegative_YieldsLengthZero(double newLen)
        => Assert.Equal("", CobolDynString.SetSize("ABCDE", newLen, 20));

    /// <summary>kb/Work PB2647 — GR37 has TWO consequents ("the length of data-name-3 is set to 0 and an
    /// EC-STORAGE-NOT-AVAIL exception condition is set to exist"), and a NaN, which is not a number at all, must
    /// get both. It fails both range comparisons, so it used to fall to the <c>(int)</c> cast, which .NET saturates
    /// to 0: the right length and NO condition. −∞ is below zero (GR37), and +∞ is a number above every maximum
    /// size, GR38's clamp (with the same condition). The −1 row is the finite twin the fix must not disturb.</summary>
    [Theory]
    [InlineData(double.NaN, "")]
    [InlineData(double.NegativeInfinity, "")]
    [InlineData(-1.0, "")]
    [InlineData(double.PositiveInfinity, "ABC  ")]
    public void SetSize_NonFiniteOrNegative_TakesItsLeg_AndSetsEcStorageNotAvail(double newLen, string expected)
    {
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            ExceptionState.StorageNotAvailChecking = true;
            Assert.Equal(expected, CobolDynString.SetSize("ABC", newLen, 5));
            Assert.Equal("EC-STORAGE-NOT-AVAIL", ExceptionState.LastName);
            Assert.False(ExceptionState.LastFatal);   // Table 13: EC-STORAGE-NOT-AVAIL is nonfatal
        });
    }

    /// <summary>GR39 — the added positions are SPACES, and previously-truncated content is never restored.</summary>
    [Fact]
    public void SetSize_Grow_FillsTheAddedPositionsWithSpaces()
        => Assert.Equal("AB   ", CobolDynString.SetSize("AB", 5, 20));

    /// <summary>GR37 — a non-integer nonnegative value truncates toward zero; a shrink drops the trailing
    /// positions.</summary>
    [Fact]
    public void SetSize_Shrink_TruncatesTowardZeroAndDropsTrailingPositions()
        => Assert.Equal("ABC", CobolDynString.SetSize("ABCDE", 3.9, 20));

    /// <summary>⛔ THE DEFECT'S RUNTIME HALF, ARM 1 (kb/Work PB463) — a request past 2³² must take GR38's clamp,
    /// NOT wrap to its low 32 bits. 4294967306 = 2³² + 10 used to yield a TEN-character item, silently, with no
    /// condition; the clamp is now applied to the value AS WRITTEN, before any narrowing.</summary>
    [Fact]
    public void SetSize_RequestPastTwoToThe32_ClampsToTheMaximum_NeverWrapsToItsLowBits()
    {
        string got = CobolDynString.SetSize("ABCDE", 4_294_967_306d, 5);
        Assert.Equal("ABCDE", got);          // GR38 — clamped to the maximum size, 5
        Assert.Equal(5, got.Length);         // and emphatically NOT 10, the low 32 bits of the request
    }

    /// <summary>⛔ THE DEFECT'S RUNTIME HALF, ARM 2 (kb/Work PB463) — a request between <see cref="int.MaxValue"/>
    /// and 2³² used to narrow NEGATIVE and throw <c>ArgumentOutOfRangeException</c> out of generated code. GR38's
    /// clamp is the only outcome.</summary>
    [Fact]
    public void SetSize_RequestPastIntMaxValue_ClampsToTheMaximum_NeverThrows()
        => Assert.Equal("ABCDE", CobolDynString.SetSize("ABCDE", 3_000_000_000d, 5));

    /// <summary>The two arms above, composed with the maximum an item with NO LIMIT phrase actually gets: the
    /// clamp target is the implementor maximum, which is inside the carrier's capacity — so the narrowing that
    /// used to wrap cannot be reached from any COBOL source.</summary>
    [Theory]
    [InlineData(4_294_967_306d)]
    [InlineData(3_000_000_000d)]
    [InlineData(1e30)]
    public void SetSize_AtTheNoLimitMaximum_ClampTargetIsWithinTheCarriersCapacity(double newLen)
    {
        int max = CobolDynString.MaxSizeOf(null);
        Assert.True(newLen > max, "the probe value must exercise GR38's clamp leg");
        Assert.InRange(max, 0, CobolDynString.MaxLength);
    }

    /// <summary>GR38's THIRD leg — "If the amount of storage required to expand the size of data-name-3 is not
    /// available, the size of data-name-3 is not changed". The leg is REACHED by handing the helper a maximum the
    /// allocator refuses outright (above <c>System.String</c>'s own capacity); the observable is the item coming
    /// back UNCHANGED rather than an <see cref="OutOfMemoryException"/> escaping into generated code.</summary>
    [Fact]
    public void SetSize_StorageNotAvailable_LeavesTheSizeUnchanged()
        => Assert.Equal("AB", CobolDynString.SetSize("AB", 2_000_000_000d, int.MaxValue));

    /// <summary>ISO §14.6.13.2 rule 5 (kb/Work PB1118): content within THIS description's maximum agrees and is
    /// read unchanged; with EC-DATA-INCOMPATIBLE checking off (the default) over-long content is read whole — the
    /// condition does not exist (§14.6.13.1.1) — and is never truncated by the read, which is not a store. The
    /// checked raise is pinned end to end by conformance:2014/pb1118_dyn_length_agreement.</summary>
    [Theory]
    [InlineData("ABCD", 4, "ABCD")]
    [InlineData("", 0, "")]
    [InlineData(null, 3, "")]
    [InlineData("ABCDEFGH", 4, "ABCDEFGH")]
    public void Agree_ReadsTheContentUnchanged(string? content, int maxSize, string expected)
        => Assert.Equal(expected, CobolDynString.Agree(content, maxSize));
}
