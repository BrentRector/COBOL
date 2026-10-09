// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE FIXED FORM OF A VARIABLE-LENGTH RECORD in a file of FIXED-length records (docs/CONFORMANCE.md §3
/// determination D-FRA (vi); kb/Work PB1562). ISO §13.18.43.4 GR6 makes a Format 1 file's records one size and §9.1.6
/// makes the record type and size fixed attributes every program shares, so such a file can neither be framed nor
/// carry an extent table: each member sits at the position it has at its MAXIMUM size, space padded
/// (<see cref="CobolContiguousLayout.ToFixedForm"/>), and a READ takes it back at that width and drops the padding
/// (<see cref="CobolContiguousLayout.Decompose"/>). These pin the five things the form
/// owes: the shape every record shares; the exact round trip for every member that does not end in a space; the
/// ONE documented loss (a trailing space is padding); a table that does not describe the record leaves it alone; and
/// the table read from a frame carries no layout, so it can never be asked for the form.
/// </summary>
public sealed class FixedFormRecordTests
{
    // A dynamic (max 9) · KY X(3) · C dynamic (max 8): the PB1562 record — fixed run 3, A at 0, C at 3, 20 bytes at maximum.
    private static readonly CobolContiguousLayout Pb1562 = new(3, [0, 3], [1, 1], [9L, 8L]);

    private static (string Image, RecordExtents Extents) Sent(string a, string key, string c) =>
        (a + key + c, Pb1562.ExtentsOf(new CobolVarGroup(key, [a, c])));

    [Theory]
    [InlineData("AAA", "KEY", "CCCC", "AAA      KEYCCCC    ")]
    [InlineData("B", "K2K", "DDDDDD", "B        K2KDDDDDD  ")]
    [InlineData("", "K3K", "", "         K3K        ")]
    [InlineData("123456789", "MAX", "ABCDEFGH", "123456789MAXABCDEFGH")]
    public void EveryRecord_HasTheOneShape_EveryMemberAtItsMaximumWidth(string a, string key, string c, string expected)
    {
        var (image, extents) = Sent(a, key, c);
        string form = Pb1562.ToFixedForm(image, extents);
        Assert.Equal(expected, form);
        Assert.Equal(20, form.Length);   // fixed run 3 + 9 + 8: integer-1 of RECORD CONTAINS 20
    }

    [Theory]
    [InlineData("AAA", "KEY", "CCCC")]
    [InlineData("B", "K2K", "DDDDDD")]
    [InlineData("", "K3K", "")]
    [InlineData("123456789", "MAX", "ABCDEFGH")]
    public void TheWritingProgram_ReadsEveryMemberBack_AtTheLengthItWrote(string a, string key, string c)
    {
        var (image, extents) = Sent(a, key, c);
        var read = Pb1562.Decompose(Pb1562.ToFixedForm(image, extents), extents: null, fixedForm: true);
        Assert.Equal(a, read.Dyn(0));
        Assert.Equal(c, read.Dyn(1));
        Assert.Equal(key, read.Fixed);
    }

    [Fact]
    public void ATrailingSpace_IsPadding_NeverData()
    {
        // The one loss: a fixed-size field cannot say "AB" from "AB ". Both write the same bytes and read back as "AB".
        var (i1, e1) = Sent("AB ", "KEY", "CD  ");
        var (i2, e2) = Sent("AB", "KEY", "CD");
        Assert.Equal(Pb1562.ToFixedForm(i1, e1), Pb1562.ToFixedForm(i2, e2));
        var read = Pb1562.Decompose(Pb1562.ToFixedForm(i1, e1), null, fixedForm: true);
        Assert.Equal("AB", read.Dyn(0));
        Assert.Equal("CD", read.Dyn(1));
    }

    [Fact]
    public void AnInnerSpace_IsData_OnlyTheTrailingPaddingIsDropped()
    {
        var (image, extents) = Sent("A B", "KEY", "C D");
        var read = Pb1562.Decompose(Pb1562.ToFixedForm(image, extents), null, fixedForm: true);
        Assert.Equal("A B", read.Dyn(0));
        Assert.Equal("C D", read.Dyn(1));
    }

    [Fact]
    public void WithoutTheFlag_TheTakeStepKeepsEveryCharacter()
    {
        // fixedForm is the READING FILE's fact (a Format 1 file); a record of any other file keeps its spaces.
        var read = Pb1562.Decompose("AAA      KEYCCCC    ", extents: null, fixedForm: false);
        Assert.Equal("AAA      ", read.Dyn(0));
        Assert.Equal("CCCC    ", read.Dyn(1));
    }

    [Fact]
    public void ATableThatDoesNotDescribeTheImage_LeavesItAlone()
    {
        var (image, extents) = Sent("AAA", "KEY", "CCCC");
        Assert.Same(image, Pb1562.ToFixedForm(image, null));
        string longer = image + "X";   // truncated or padded on its way: not the image the table was built over
        Assert.Equal(longer, Pb1562.ToFixedForm(longer, extents));
        var other = new CobolContiguousLayout(3, [0, 2], [1, 1], [9L, 8L]);
        string untouched = Pb1562.ToFixedForm(image, other.ExtentsOf(new CobolVarGroup("KEY", ["AAA", "CCCC"])));
        Assert.Equal(image, untouched);   // another layout's table does not describe THIS record (§8.5.1.12.2)
    }

    [Fact]
    public void ATableReadFromAFrame_HasNoLayout_SoItCanNeverBeAskedForTheForm()
    {
        var fromFrame = new RecordExtents([0, 3], [3, 4]);
        Assert.Null(fromFrame.Layout);
        Assert.Same(Pb1562, Pb1562.ExtentsOf(new CobolVarGroup("KEY", ["AAA", "CCCC"])).Layout);
    }
}
