// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>The edited-mask (digits, scale) geometry — ISO §13.18.40.3's P scaling against EVERY kind of
/// digit position, not only the literal 9/Z/* (kb/Work PB155: `PIC $$$$PP` has no 9/Z/* at all, so its
/// rightmost P run classified as LEADING and MaskScale answered +2 where the value is a multiple of 10^2,
/// scale −2 — mis-shaping both the §14.7.7 composite of operands and the §13.18.63.3 VALUE window).
/// <see cref="CobolEdit.MaskScale"/>/<see cref="CobolEdit.MaskCapacity"/> are the ONE canonical geometry
/// (DataBinder.StoredShapeOf consumes them), so these facts are its drift pin.</summary>
public class CobolEditMaskGeometryTests
{
    [Theory]
    // trailing P anchored on 9/Z/* (the NC124A shape) — a multiple of 10^P, scale −P
    [InlineData("ZZZPP", -2)]
    [InlineData("99PP", -2)]
    // trailing P anchored on a FLOATING string's digit positions (PB155 — no 9/Z/* at all)
    [InlineData("$$$$PP", -2)]
    [InlineData("++++PP", -2)]
    // trailing P with a trailing FIXED sign after it — the fixed insertion is NOT a digit position and
    // must not un-trail the P run
    [InlineData("99PPCR", -2)]
    // leading P — every digit position fractional: scale = P + digit positions (floating members count)
    [InlineData("PPP99", 5)]
    [InlineData("PPP$$$$", 6)]
    // the point forms
    [InlineData("ZZ9.99", 2)]
    [InlineData("Z9.9(29)", 29)]
    [InlineData("9(5)", 0)]
    public void MaskScale_ShapesThePDecimalGeometry(string picture, int expected) =>
        Assert.Equal(expected, CobolEdit.MaskScale(Expand(picture), null));

    [Theory]
    // Capacity = digit positions (9/Z/* + floating members less the one symbol position); P holds none.
    [InlineData("$$$$PP", 3)]
    [InlineData("PPP$$$$", 3)]
    [InlineData("ZZZPP", 3)]
    [InlineData("ZZ9.99", 5)]
    public void MaskCapacity_CountsDigitPositionsOnly(string picture, int expectedCapacity) =>
        Assert.Equal(expectedCapacity, CobolEdit.MaskCapacity(Expand(picture), null).Capacity);

    private static string Expand(string picture)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < picture.Length; i++)
        {
            if (i + 1 < picture.Length && picture[i + 1] == '(')
            {
                int close = picture.IndexOf(')', i);
                int n = int.Parse(picture[(i + 2)..close]);
                sb.Append(picture[i], n);
                i = close;
            }
            else sb.Append(picture[i]);
        }
        return sb.ToString();
    }
}
