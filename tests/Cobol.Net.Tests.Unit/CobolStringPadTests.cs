// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The <see cref="CobolString"/> PAD-CHARACTER overloads (Phase 4a M2-DATA-3/4): a BOOLEAN receiver fills and
/// extends with boolean ZEROS — <c>Store</c>/<c>SpliceInto</c> right/left fill '0' (ISO §14.6.8.6 :24304–24308;
/// JUSTIFIED left fill §13.18.32 GR2 :19264–19273) and <c>Compare</c> right-extends the shorter operand with
/// '0' (§8.8.4.2.8 :9683–9689 — the boolean equality relation, by VALUE regardless of usage). A NATIONAL
/// receiver keeps the space pad — under D-N4's Latin-1 subset the national space IS ' ' (§14.6.8.5). The
/// default-<c>' '</c> paths must stay BYTE-IDENTICAL to the pre-Phase-4a behavior: every existing call site
/// compiles against the defaults, so the guard theories below pin today's outputs exactly.
/// </summary>
public sealed class CobolStringPadTests
{
    // ── Store(value, width, justifiedRight, pad) — the '0' pad (boolean receivers) ─────────────────────────

    /// <summary>Left-justified store fills the RIGHT with the pad char (§14.6.8.6 — boolean zeros).</summary>
    [Theory]
    [InlineData("11", 4, "1100")]
    [InlineData("", 4, "0000")]      // zero-length sender ⇒ all zeros (MOVE GR3 — behaves as ZERO)
    [InlineData(null, 4, "0000")]
    [InlineData("1010", 4, "1010")]  // exact fit — no pad consulted
    public void Store_PadZero_FillsRight(string? value, int width, string expected)
        => Assert.Equal(expected, CobolString.Store(value, width, pad: '0'));

    /// <summary>JUSTIFIED RIGHT fills the LEFT with the pad char (§13.18.32 GR2 — bit zeros).</summary>
    [Fact]
    public void Store_PadZero_JustifiedRight_FillsLeft()
        => Assert.Equal("0011", CobolString.Store("11", 4, justifiedRight: true, pad: '0'));

    /// <summary>Truncation ignores the pad char entirely: left-justified truncates on the RIGHT, justified
    /// truncates on the LEFT (§14.6.8.6 / §13.18.32 GR1).</summary>
    [Theory]
    [InlineData(false, "1100")]
    [InlineData(true, "0011")]
    public void Store_PadZero_TruncationUnchanged(bool justifiedRight, string expected)
        => Assert.Equal(expected, CobolString.Store("110011", 4, justifiedRight: justifiedRight, pad: '0'));

    /// <summary>The default-pad Store paths are byte-identical to the pre-pad behavior (the alphanumeric MOVE
    /// rules, ISO §14.9.25) — the guard for every existing call site.</summary>
    [Theory]
    [InlineData("HI", 5, false, "HI   ")]
    [InlineData("HI", 5, true, "   HI")]
    [InlineData("TOOLONG", 4, false, "TOOL")]
    [InlineData("TOOLONG", 4, true, "LONG")]
    [InlineData("", 3, false, "   ")]
    [InlineData(null, 3, false, "   ")]
    [InlineData("ABC", 0, false, "")]     // non-positive width ⇒ empty (unchanged)
    public void Store_DefaultPad_ByteIdentical(string? value, int width, bool justifiedRight, string expected)
        => Assert.Equal(expected, CobolString.Store(value, width, justifiedRight));

    // ── SpliceInto(dst, leftmost, length, slice, pad) — the ref-mod write fill (§8.4.3.3 / §14.9.24) ───────

    /// <summary>A ref-mod store into a BOOLEAN item fills the unreplaced tail of the targeted positions with
    /// '0' (the slice is left-justified in the window; §14.6.8.6 applied to the window).</summary>
    [Theory]
    [InlineData("1111", 2, 3, "0", "1000")]   // positions 2-4 ← "0" + '0' fill
    [InlineData("1111", 1, 2, "", "0011")]    // empty slice ⇒ the window zero-fills
    public void SpliceInto_PadZero_FillsWindow(string dst, int leftmost, int length, string slice, string expected)
        => Assert.Equal(expected, CobolString.SpliceInto(dst, leftmost, length, slice, pad: '0'));

    /// <summary>The default-pad SpliceInto paths are byte-identical to the pre-pad behavior (space fill;
    /// out-of-range starts leave the destination untouched).</summary>
    [Theory]
    [InlineData("ABCDEF", 2, 3, "x", "Ax  EF")]
    [InlineData("ABC", 0, 2, "zz", "ABC")]    // start before position 1 ⇒ unchanged
    [InlineData("ABC", 2, -1, "z", "Az ")]    // negative length ⇒ to the end, space-filled
    public void SpliceInto_DefaultPad_ByteIdentical(string dst, int leftmost, int length, string slice, string expected)
        => Assert.Equal(expected, CobolString.SpliceInto(dst, leftmost, length, slice));

    /// <summary>A FIGURATIVE seed (<c>repeat: true</c>) is repeated character by character over every position of
    /// the slice — ISO §8.3.3.6.4 GR2 against the reference-modified data item (§8.4.3.3.4 GR5), and the same
    /// answer <see cref="CobolString.FigToWidth"/> gives for a fixed receiver of the slice's length (kb/Work PB1655:
    /// `MOVE ALL "*" TO T(7:3)` over "---------" used to leave "------*  ").</summary>
    [Theory]
    [InlineData("---------", 7, 3, "*", "------***")]
    [InlineData("---------", 2, 5, "AB", "-ABABA---")]                             // truncated from the right
    [InlineData("---------", 4, CobolString.OmittedRefModLength, "xy", "---xyxyxy")]  // (leftmost:) — to the end
    [InlineData("---------", 1, 2, "LONGER", "LO-------")]                        // seed longer than the slice
    public void SpliceInto_Repeat_FillsTheSliceWithTheFigurativeSeed(string dst, int leftmost, int length, string seed, string expected)
    {
        Assert.Equal(expected, CobolString.SpliceInto(dst, leftmost, length, seed, repeat: true));
        int start = leftmost - 1, len = length == CobolString.OmittedRefModLength ? dst.Length - start : length;
        Assert.Equal(CobolString.FigToWidth(seed, len), expected.Substring(start, len));
    }

    // ── Compare(left, right, pad) — the boolean zero-extension (§8.8.4.2.8) ─────────────────────────────────

    /// <summary>Boolean comparison right-extends the SHORTER operand with boolean zeros — unequal lengths
    /// compare equal exactly when the longer's tail is all '0' (§8.8.4.2.8 :9683–9689).</summary>
    [Theory]
    [InlineData("11", "1100", 0)]
    [InlineData("11", "1101", -1)]
    [InlineData("111", "11", 1)]     // left "111" vs right-extended "110" — the tail '1' > '0'
    [InlineData("1", "1", 0)]
    [InlineData("", "000", 0)]       // zero-length extends to all zeros
    public void Compare_PadZero_ZeroExtends(string left, string right, int expectedSign)
        => Assert.Equal(expectedSign, Math.Sign(CobolString.Compare(left, right, pad: '0')));

    /// <summary>The default-pad Compare is byte-identical to the pre-pad behavior (space extension, ordinal —
    /// ISO §8.8.4.2.7 r2).</summary>
    [Theory]
    [InlineData("AB", "AB  ", 0)]
    [InlineData("AB", "AC", -1)]
    [InlineData("ABC ", "ABC", 0)]
    [InlineData("AB!", "AB", 1)]     // '!' (0x21) > ' ' (0x20) against the space extension
    public void Compare_DefaultPad_ByteIdentical(string left, string right, int expectedSign)
        => Assert.Equal(expectedSign, Math.Sign(CobolString.Compare(left, right)));
}
