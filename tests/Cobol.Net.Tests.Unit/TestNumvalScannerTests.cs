// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §15.93/§15.94 TEST-NUMVAL/-C position-reporting scanners (P11 Step 6) — the RUNTIME scan alone, at the
/// shapes a golden would need a whole compilation unit for: the DECIMAL-POINT IS COMMA role swap (§15.67.3 r5
/// substitutes for NUMVAL; §15.68.3 r4d SWAPS both separator roles for NUMVAL-C), the SDIDI 34-digit cap
/// (§15.93.4 r1b sub-note 4), and the b)-vs-c)-leg discrimination for digit-free strings. Verdict values
/// hand-derived in PHASE-11-scout-notes.md (spec:validators).
/// <para>
/// ⛔ EVERY TEST HERE PASSES THE DECISION AS A LITERAL, so none of them measures the COMPILER arm that computes
/// it (<c>IntrinsicRenderer.CommaFlag</c> from <c>ctx.Data.DecimalPointIsComma</c>; <c>DigitCapFlag</c> from
/// <c>ArithmeticModes.NumvalDigitCap</c>). That half is measured END TO END by
/// <c>conformance:2002/pb256_test_numval_mode_flags</c> (both flags, both directions, three compilation units)
/// and <c>conformance:2014/pb256_test_numval_standard_decimal</c>, and
/// <c>IntrinsicModeFlagCoverageDriftTests</c> keeps that true for any function that gains such an argument
/// later. Until kb/Work PB256 neither existed and the second fact below was named
/// <c>DigitCap_FollowsArithmeticMode</c> — a green check over a name no arithmetic mode was in scope for.
/// </para>
/// </summary>
public sealed class TestNumvalScannerTests
{
    [Fact]
    public void CommaMode_SwapsSeparatorRoles()
    {
        // NUMVAL under DECIMAL-POINT IS COMMA (§15.67.3 r5): the comma IS the decimal separator...
        Assert.Equal(0, CobolIntrinsics.TestNumval("1,5", commaMode: true));
        // ...and the period is no longer a NUMVAL character at all.
        Assert.Equal(2, CobolIntrinsics.TestNumval("1.5", commaMode: true));
        // NUMVAL-C (§15.68.3 r4d): BOTH roles swap — '.' groups, ',' is the decimal point.
        Assert.Equal(0, CobolIntrinsics.TestNumvalC("$1.234,56", "$", commaMode: true));
        // A grouping separator after the decimal separator is not part of the format (§15.68.4 r2 scope):
        // in "$1,23.4" under comma mode the ',' is the decimal, so the '.' at position 6 is first in error.
        Assert.Equal(6, CobolIntrinsics.TestNumvalC("$1,23.4", "$", commaMode: true));
    }

    /// <summary>The SCAN's response to each cap value — NOT the mode selection, which is a compile-time arm this
    /// test cannot reach (see the class remark; the golden pair measures it).</summary>
    [Fact]
    public void DigitCapParameter_SelectsTheSubNoteVerdict()
    {
        string d40 = new('1', 40);
        Assert.Equal(32, CobolIntrinsics.TestNumval(d40));                 // r1b sub-note 2 — native 31-digit cap
        Assert.Equal(35, CobolIntrinsics.TestNumval(d40, digitCap: 34));   // r1b sub-note 4 — SDIDI 34-digit cap
        Assert.Equal(0, CobolIntrinsics.TestNumval(new string('1', 34), digitCap: 34));
    }

    [Fact]
    public void NoDigit_BLegAtOffendingChar_CLegAtEnd()
    {
        // A digit-free scan that BROKE on a real character is r1b at that character ('$-123': the sign
        // may not follow the currency — neither §15.68.3 format)...
        Assert.Equal(2, CobolIntrinsics.TestNumvalC("$-123", "$"));
        // ...while a valid-but-incomplete prefix that ran off the end is r1c LENGTH+1 (a bare currency).
        Assert.Equal(2, CobolIntrinsics.TestNumvalC("$", "$"));
        Assert.Equal(4, CobolIntrinsics.TestNumval(" +."));
    }

    /// <summary>kb/Work PB2631 - §15.68.3 r4a: the grouping separator is `digit [ , digit ] ...`, admitted only as
    /// ", digit". The character after a separator that is not a digit is the first one in error (§15.94.4 r1b); a text
    /// that ends right after it is valid-but-incomplete (r1c, LENGTH + 1). The value twin rides the same scan, so
    /// the non-conforming forms are rejected there too (no fabricated 1.5 / 12).</summary>
    [Theory]
    [InlineData("1,", false, 3)]
    [InlineData("1,,2", false, 3)]
    [InlineData("1,.5", false, 3)]
    [InlineData("1, 2", false, 3)]
    [InlineData("1,-", false, 3)]
    [InlineData("1,CR", false, 3)]
    [InlineData(",1", false, 1)]
    [InlineData("1,2,", false, 5)]
    [InlineData("1,2,3", false, 0)]
    [InlineData("1,2.5", false, 0)]
    [InlineData("1,5-", false, 0)]
    [InlineData("1,2.5,3", false, 6)]
    [InlineData("1.", true, 3)]
    [InlineData("1..2", true, 3)]
    [InlineData("1.,5", true, 3)]
    [InlineData("1.2.", true, 5)]
    [InlineData("1.2.3,5", true, 0)]
    [InlineData("1,5.2", true, 4)]
    public void GroupingSeparator_NeedsADigitAfterIt(string text, bool commaMode, long expected)
    {
        Assert.Equal(expected, CobolIntrinsics.TestNumvalC(text, "$", commaMode));
        // Never accepted by the value twin when the validator rejects (one scan, kb/Work PB60): checking off, the
        // §15.3 default 0 comes back; a conforming text keeps its value (always nonzero here).
        bool conforming = expected == 0;
        CobolDec value = CobolIntrinsics.NumvalCDec(text, "$", commaMode);
        Assert.Equal(conforming, CobolDec.Compare(value, new CobolDec(0, 0)) != 0);
    }

    /// <summary>kb/Work PB2631 - §15.68.3 r4 f defines ANYCASE by the LOWER-CASE fold, the SAME one FIND-STRING (§15.37.4
    /// r4) and SUBSTITUTE (§15.87.4 r5) use. The KELVIN SIGN (U+212A) lowercases to "k" but its uppercase is itself, so a
    /// lowercase-fold matcher accepts it for the currency "k" and an uppercase-fold one (OrdinalIgnoreCase, the old
    /// NUMVAL-C comparison) does not - the three families must agree on the same letters.</summary>
    [Fact]
    public void AnycaseCurrency_UsesTheLowerCaseFold_TheOtherAnycaseMatchersUse()
    {
        const string kelvin = "K";
        Assert.Equal(1L, CobolIntrinsics.FindString(kelvin, "k", last: false, skip: 0, anycase: true));
        Assert.Equal("Y", CobolIntrinsics.Substitute(kelvin, ["k"], ["Y"], [4]));
        Assert.Equal(0L, CobolIntrinsics.TestNumvalC(kelvin + "12", "k", anycase: true));
        Assert.Equal(1L, CobolIntrinsics.TestNumvalC(kelvin + "12", "k", anycase: false));
    }

    [Fact]
    public void AnycaseCurrency_FoldsPerR4f()
    {
        Assert.Equal(1, CobolIntrinsics.TestNumvalC("usd 12", "USD"));
        Assert.Equal(0, CobolIntrinsics.TestNumvalC("usd 12", "USD", anycase: true));
        // The value parser mirrors the fold (§15.94.1 — TEST verifies what NUMVAL-C would accept).
        Assert.Equal((Int128)12, CobolIntrinsics.NumvalCDec("usd 12", "USD", anycase: true).ToUnscaled(0, CobolRounding.Truncation));
    }
}
