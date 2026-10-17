// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// TEST-NUMVAL-F's §15.95.4 position legs (kb/Work PB121). Two defects pinned here: (1) the r1b/r1c
/// misdispatch — a scan that BROKE on a real character with no significand digit yet returned LENGTH+1
/// (leg c) where leg b requires that character's own position ("Otherwise, if one or more characters are in
/// error, the position of the first character in error"; leg c's NOTE lists only no-character-in-error
/// shapes — zero-length, all-spaces, valid-but-incomplete); (2) the absent r1b sub-note 6 capacity leg —
/// under standard-decimal arithmetic a conforming argument whose magnitude exceeds the SDIDI's range
/// (±9.999…E+6144, §8.8.1.5.2 NOTE 2) must return the position of the FIRST DIGIT of the exponent, and
/// NvfScan checked only the significand digit cap and the exponent digit count, so "1E+9999" reported 0.
/// Native arithmetic has NO capacity leg — r1b.6 names only the standard modes — and underflow is not
/// "exceeds the capacity" (the value twin's subnormal re-round disposes of it).
/// </summary>
public sealed class TestNumvalFPositionTests
{
    [Theory]
    // r1b with no significand digit scanned — the broke-on position, NOT length+1 (the PB121 misdispatch).
    [InlineData("ABC", 1)]
    [InlineData("$1.5", 1)]
    [InlineData("--1", 2)]      // the second sign is the character in error (one leading sign in the format)
    [InlineData("+.A", 3)]
    [InlineData("E+1", 1)]      // an exponent with no significand: 'E' itself is in error
    // r1c — ran off the end with valid-but-incomplete content (the spec NOTE's own examples).
    [InlineData("", 1)]
    [InlineData("   ", 4)]
    [InlineData(" +.", 4)]
    [InlineData("1E", 3)]       // dangling E — incomplete, length+1
    [InlineData("1E+", 4)]      // E-sign with no digit — incomplete, length+1
    // r1b after digits — unchanged behavior, guarded here.
    [InlineData("1.5X", 4)]
    [InlineData("1E2", 3)]      // §15.69.3: the sign is REQUIRED once E is written
    [InlineData("0 1E+2", 3)]   // r1b.1 — the spec's own embedded-space example
    [InlineData("1 2E+3", 3)]   // the same, one position along
    // ⛔ THE EXPONENT-SPACE DETERMINATION (kb/Work PB256; docs/CONFORMANCE.md §3). §15.69.3 r1's figure draws n
    // as ONE contiguous 1..4-digit run with [space-string] only before and after it, and §15.95.4 r1 b) 1 is
    // unqualified as to WHICH digit run ("a string of numeric characters") where sub-notes 2/3/4 all say "of
    // the significand" — so an interior space ends n and the next non-space is the first character in error.
    // Under the rejected reading (§15.69.3 r5's "ignored" as ELIDE-AND-JOIN) the first case would be 0 and the
    // second 9, so these two are the discriminating pair. conformance:cases/15.95.4-Returned-value-rule/pb256_test_numval_f_spaces is the
    // end-to-end witness.
    [InlineData("1E+1 234", 6)]
    [InlineData("1E+1 2345", 6)]
    [InlineData("1E+12345", 8)]  // a CONTIGUOUS fifth exponent digit IS r1 b) 5's shape — its own position
    // Conforming.
    [InlineData("1.5E+3", 0)]
    [InlineData("1E+ 1234", 0)]  // the figure's [space-string] between the exponent sign and n
    [InlineData("1E+1234 ", 0)]  // the figure's trailing [space-string] after n
    [InlineData(" -  .5  ", 0)]
    public void Native_PositionLegs(string text, long expected) =>
        Assert.Equal(expected, CobolIntrinsics.TestNumvalF(text));

    [Theory]
    // r1b.6 under standard-decimal (digitCap 34): magnitude past the SDIDI's +6144 adjusted exponent →
    // the exponent's FIRST digit. Hand-derived: "1E+9999" msd exp 9999 → first exp digit at 4;
    // "123E+6143" msd exp 2+6143 = 6145 → digit '6' at 6; "1E+ 9999" spaces after the sign are legal (r5).
    [InlineData("1E+9999", 4)]
    [InlineData("123E+6143", 6)]
    [InlineData("1E+ 9999", 5)]
    [InlineData("-9.9E+6144", 0)]   // msd exp 6144 — exactly representable, conforms
    [InlineData("1E+6144", 0)]
    [InlineData("0E+9999", 0)]      // zero has no magnitude to exceed anything
    [InlineData("1E-9999", 0)]      // underflow is NOT "exceeds the capacity" (r1b.6 is overflow only)
    [InlineData(".001E+6148", 7)]   // msd exp -3+6148 = 6145 → overflow; the exponent's first digit is at 7
    public void StandardDecimal_CapacityLeg(string text, long expected) =>
        Assert.Equal(expected, CobolIntrinsics.TestNumvalF(text, digitCap: 34));

    [Fact]
    public void Native_HasNoCapacityLeg() =>
        // r1b.6 names only standard-decimal and standard-binary; native (cap 31) reports 0 — conforming —
        // and the native value path's own saturation/approximation disposes of the magnitude.
        Assert.Equal(0L, CobolIntrinsics.TestNumvalF("1E+9999"));
}
