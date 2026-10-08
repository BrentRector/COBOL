// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>⛔ ONE DERIVATION OF "WHICH SYMBOLS OF AN EDITED MASK ARE DIGIT POSITIONS" (kb/Work PB2639). The render
/// (<see cref="CobolEdit.Format"/>), the size-error bound (<see cref="CobolEdit.TryFormat"/>),
/// <see cref="CobolEdit.MaskCapacity"/>, <see cref="CobolEdit.MaskScale"/> and <see cref="CobolEdit.DeEdit"/> must
/// all answer it identically, because ISO §13.18.40.5 rule 6 makes the repetitions of a FLOATING extended editing
/// sign control symbol digit positions exactly as it does a floating currency symbol's. Three of the five dropped
/// the <see cref="CobolEdit.EditRule"/> array and counted such a symbol as an insertion character. These facts pin
/// the AGREEMENT over every edits-bearing picture: the bound accepts exactly what the render can show, the render
/// aligns at the scale the geometry reports, and the de-editing read recovers the value.</summary>
public sealed class CobolEditMaskFactsTests
{
    private static CobolEdit.EditRule[] Floating(char c, string neg, string pos) =>
        [new CobolEdit.EditRule(c, neg, pos, SimpleInsertion: false, Floating: true)];

    private static readonly CobolEdit.EditRule[] DebitL = Floating('L', "DEBIT ", "      ");
    private static readonly CobolEdit.EditRule[] DashL = Floating('L', "-", " ");

    /// <summary>The geometry counts a floating character-1's repetitions: the first is the symbol itself, so
    /// <c>LLLL9.99</c> holds 4 integer and 2 fraction digit positions (capacity 6), and a floating string that
    /// reaches PAST the decimal point (<c>LLL.LL</c>, §13.18.40.3 SR29 asks only for one symbol to the LEFT of
    /// it) has its fraction positions counted, so the scale is 2 and not 0.</summary>
    [Theory]
    [InlineData("LLLL9.99", 6, 2)]
    [InlineData("LLL.LL", 4, 2)]
    [InlineData("L99", 2, 0)]            // ONE L is a fixed insertion symbol, never a digit position
    [InlineData("LL9", 2, 0)]
    public void Geometry_CountsAFloatingCharacter1AsDigitPositions(string picture, int capacity, int scale)
    {
        var rules = picture.StartsWith("LLL.") ? DashL : DebitL;
        // `L99` carries a FIXED character-1: the bind decides Floating, so the rule says so.
        if (picture == "L99") rules = [new CobolEdit.EditRule('L', "DEBIT ", "      ", false, Floating: false)];
        Assert.Equal((capacity, scale), CobolEdit.MaskCapacity(picture, rules));
        Assert.Equal(scale, CobolEdit.MaskScale(picture, rules));
    }

    /// <summary>The same picture WITHOUT its rules is a mask of plain insertion letters: the rules are what make
    /// the symbol a digit position, which is why neither entry has a default for them.</summary>
    [Fact]
    public void Geometry_WithoutTheRules_CountsNoFloatingCharacter1()
    {
        Assert.Equal((3, 2), CobolEdit.MaskCapacity("LLLL9.99", null));
        Assert.Equal(0, CobolEdit.MaskScale("LLL.LL", null));
    }

    /// <summary>The size-error bound is the render's capacity: <c>COMPUTE C = 10</c> into <c>LLLL9.99</c> is
    /// inside its four integer digits (a false size error before PB2639: the bound counted three), and the first
    /// value past them, 10000, is the §14.7.5 case-3 size error.</summary>
    [Theory]
    [InlineData(1000, 2, true)]        // 10.00
    [InlineData(12345, 2, true)]       // 123.45 — Annex D.24's own value for this picture
    [InlineData(999999, 2, true)]      // 9999.99 — the last value that fits
    [InlineData(1000000, 2, false)]    // 10000.00 — five integer digits
    [InlineData(10, 0, true)]
    public void TryFormat_AcceptsExactlyWhatTheRenderCanShow(long unscaled, int scale, bool fits)
    {
        Assert.Equal(fits, CobolEdit.TryFormat(unscaled, scale, "LLLL9.99", out string image, edits: DebitL));
        Assert.Equal(fits, image.Length > 0);
    }

    /// <summary>The render aligns the sending value at the scale the geometry reports. <c>LLL.LL</c> with a
    /// width-1 literal: 1.5 is <c>01.50</c> with the sign on the second L, and 0.25 puts the sign immediately
    /// before the point (§13.18.40.5 rule 6 a). A scale of 0 used to render the digits of 1 at the fraction
    /// positions (<c>   .01</c>).</summary>
    [Theory]
    [InlineData(15, 1, "  1.50")]
    [InlineData(-15, 1, " -1.50")]
    [InlineData(25, 2, "   .25")]
    [InlineData(-25, 2, "  -.25")]
    [InlineData(9999, 2, " 99.99")]
    public void Format_AlignsAtTheGeometrysScale(long unscaled, int scale, string expected)
    {
        Assert.Equal(expected, CobolEdit.Format(unscaled, scale, "LLL.LL", edits: DashL));
        // The pin: formatting at the geometry's own scale is the identity of the alignment.
        int ms = CobolEdit.MaskScale("LLL.LL", DashL);
        Assert.Equal(expected, CobolEdit.Format(CobolNum.Rescale(unscaled, scale, ms, CobolRounding.Truncation), ms,
            "LLL.LL", edits: DashL));
    }

    /// <summary>De-editing reads each digit position at the mask's scale: the image of 1.50 is 1.50 again
    /// (it read back as 1.00 at scale 0), negative with the literal-2 sign.</summary>
    [Theory]
    [InlineData("  1.50", 150)]
    [InlineData(" -1.50", -150)]
    [InlineData("   .25", 25)]
    public void DeEdit_ReadsAtTheGeometrysScale(string image, long expected) =>
        Assert.Equal((Int128)expected, CobolEdit.DeEdit(image, "LLL.LL", edits: DashL));

    /// <summary>⛔ THE DRIFT PIN: the fixed-versus-floating classification of '+', '-' and the currency symbol is
    /// written in <c>CobolEdit.MaskFacts.cs</c> and nowhere else. Every consumer that re-derived it (Render,
    /// MaskCapacity, MaskScale, DeEdit) counted the symbols in its own loop, and a rule one of them did not learn
    /// (PB2639: the floating character-1) made the bound disagree with the image. A new consumer that counts
    /// again fails here.</summary>
    [Fact]
    public void TheFixedVersusFloatingClassification_IsWrittenOnlyInMaskFacts()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Runtime", "Values", "Numeric"), "*.cs"))
        {
            if (Path.GetFileName(file) == "CobolEdit.MaskFacts.cs") continue;
            foreach (string line in File.ReadAllLines(file))
            {
                string t = line.TrimStart();
                if (t.StartsWith("//")) continue;
                if (Regex.IsMatch(t, @"\b(plus|plusCount|Plus)\s*==\s*1\s*&&"))
                    offenders.Add($"{Path.GetFileName(file)}: {t}");
            }
        }
        Assert.Empty(offenders);
    }

    /// <summary>The magnitude of <see cref="Int128.MinValue"/> is representable in the formatter's capacity
    /// test, which used <c>Int128.Abs</c> and threw (kb/Work PB2639, the sibling sweep of the unsigned-magnitude
    /// sites): it is a size error against a 38-digit mask, never an exception.</summary>
    [Fact]
    public void TryFormat_OfInt128MinValue_IsASizeErrorNotAnException()
    {
        Assert.False(CobolEdit.TryFormat(Int128.MinValue, 0, new string('9', 38), out _, edits: null));
        Assert.False(CobolLocaleEdit.TryFormat(Int128.MinValue, 0, "+$ZZZZZZ9.99", "", 20, out _));
    }
}
