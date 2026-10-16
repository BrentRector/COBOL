// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ WHICH CHARACTER IS THE DECIMAL POINT OF A PICTURE CHARACTER-STRING IS ASKED OF ONE FUNCTION (kb/Work PB3219).
/// ISO §13.18.40.3 SR13 makes the comma the decimal separator and the period the grouping separator under DECIMAL-POINT
/// IS COMMA, and says the rules written for one apply to the other; every PICTURE rule that names "the symbol
/// period" (SR12 b in both of its forms, SR17, SR20, the floating-string detector, Table 10's period row) therefore
/// takes its character from <c>PictureComposition.DecimalSeparator</c> / <c>GroupingSeparator</c>. The picture
/// analyzer once spelled the pair out at five sites and two of them (the FOR-form symbol screen and the
/// floating-point significand) spelled it without the swap. A literal <c>flag ? ',' : '.'</c> in the two picture files
/// is that regression; <see cref="PictureCategoryDriftTests"/> holds the property (SR13 as a closure over the
/// accepted set) that proves the rules agree.
/// </summary>
public sealed class PictureSeparatorDriftTests
{
    private static readonly Regex LiteralSeparatorTernary =
        new(@"\?\s*'[.,]'\s*:\s*'[.,]'", RegexOptions.Compiled);

    [Fact]
    public void ThePictureFiles_AskTheSeparatorPairOfOneFunction()
    {
        string binding = TestRepo.Src("Cobol.Net.Compiler", "Binding");
        var found = new List<string>();
        foreach (string name in new[] { "PictureAnalyzer.cs", "PictureComposition.cs" })
        {
            int n = 0;
            foreach (string line in File.ReadAllLines(Path.Combine(binding, name)))
                if (!line.TrimStart().StartsWith("//", StringComparison.Ordinal) && LiteralSeparatorTernary.IsMatch(line)) n++;
            // PictureComposition.cs holds the pair itself: DecimalSeparator and GroupingSeparator, one ternary each.
            int allowed = name == "PictureComposition.cs" ? 2 : 0;
            if (n != allowed) found.Add($"{name}: {n} literal separator ternar{(n == 1 ? "y" : "ies")}, expected {allowed}");
        }
        Assert.True(found.Count == 0,
            "A picture file spells the DECIMAL-POINT IS COMMA separator swap itself (" + string.Join("; ", found)
            + "). Ask PictureComposition.DecimalSeparator / GroupingSeparator: SR13's role swap lives in one place, and a "
            + "second spelling is how SR12 b's FOR-form screen and the floating-point significand lost it (kb/Work PB3219).");
    }
}
