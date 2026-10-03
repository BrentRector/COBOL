// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DYNAMIC LENGTH STRUCTURE IN A RECORD IMAGE (ISO §12.3.7.4 GR18 + GR19; docs/CONFORMANCE.md §3 determination
/// D-DL3; kb/Work PB1094). A dynamic-length item that names a structure occupies, in the record a file carries, its
/// data PREFIXED by a binary length field and FOLLOWED by a binary-zero delimiter — and every expected string below is
/// written out character by character from those two rules, never read back from the codec: GR18's table fixes the
/// length field's width (four characters for PREFIXED, two for SHORT PREFIXED) and this implementation's binary byte
/// order is most-significant-byte first (DOC-A.1-205); GR19 fixes the delimiter as one position of zero bits. Each
/// fact is pinned from both ends — what a WRITE puts on the medium (<see cref="CobolDynStructure.Frame"/> /
/// <see cref="CobolContiguousLayout.MediumImage"/>) and what a READ takes back from it, with and without the frame's
/// extent table (a file another program wrote has none).
/// </summary>
public sealed class CobolDynStructureTests
{
    private static readonly CobolDynStructure Delimited = new(0, true);
    private static readonly CobolDynStructure ShortPrefixed = new(2, false);
    private static readonly CobolDynStructure Prefixed = new(4, false);
    private static readonly CobolDynStructure Both = new(4, true);

    // ── GR18 / GR19: what the structure puts around the data ───────────────────────────────────────────────────

    [Fact]
    public void Delimited_FollowsTheDataWithOnePositionOfZeroBits()
    {
        Assert.Equal("AB\0", Delimited.Frame("AB"));      // GR19: "a delimiter shall directly follow the data"
        Assert.Equal("\0", Delimited.Frame(""));           // an empty item is its delimiter alone
        Assert.Equal(1, Delimited.Overhead);
    }

    [Fact]
    public void ShortPrefixed_PutsATwoCharacterBigEndianLengthFieldInFront()
    {
        Assert.Equal("\0\u0002AB", ShortPrefixed.Frame("AB"));        // GR18: SHORT PREFIXED — a 16-bit binary field
        Assert.Equal("\u0001\u0002" + new string('x', 258), ShortPrefixed.Frame(new string('x', 258)));  // 258 = 0x0102
        Assert.Equal(2, ShortPrefixed.Overhead);
    }

    [Fact]
    public void Prefixed_PutsAFourCharacterBigEndianLengthFieldInFront()
    {
        Assert.Equal("\0\0\0\u0003ABC", Prefixed.Frame("ABC"));       // GR18: PREFIXED — a 32-bit binary field
        Assert.Equal("\0\0\u0001\0" + new string('y', 256), Prefixed.Frame(new string('y', 256)));
        Assert.Equal(4, Prefixed.Overhead);
    }

    [Fact]
    public void PrefixedAndDelimited_AreBothApplied_LengthFieldFirstDelimiterLast()
    {
        Assert.Equal("\0\0\0\u0002AB\0", Both.Frame("AB"));
        Assert.Equal(5, Both.Overhead);
        Assert.Equal(Both.Code, CobolDynStructure.FromCode(Both.Code)!.Code);
    }

    [Fact]
    public void ANationalRecordsStructure_CountsPositions_AndItsDelimiterIsOneNationalCharacterWide()
    {
        // A national item that IS the record is its UTF-16BE byte pairs (D-N1): "AB" is 00 41 00 42, two POSITIONS.
        var delimited = new CobolDynStructure(0, true, unit: 2);
        var prefixed = new CobolDynStructure(2, false, unit: 2);
        Assert.Equal("\0A\0B\0\0", delimited.Frame("\0A\0B"));          // GR19: national delimiter = a national character of zero bits
        Assert.Equal("\0\u0002\0A\0B", prefixed.Frame("\0A\0B"));       // the length field counts positions, not bytes
        Assert.Equal(2, delimited.Overhead);
    }

    [Fact]
    public void NoStructure_CodeIsZero_AndAnEmptyStructureIsRefused()
    {
        Assert.Null(CobolDynStructure.FromCode(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CobolDynStructure(0, false));   // §12.3.7.4: one or more of PREFIXED / DELIMITED
        Assert.Throws<ArgumentOutOfRangeException>(() => new CobolDynStructure(3, true));    // GR18 names a 32-bit and a 16-bit field only
    }

    // ── In: the extent is known ────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("AB\0", "AB")]
    [InlineData("\0", "")]
    [InlineData("AB", "AB")]            // no delimiter in the extent: all data
    [InlineData("AB\0CD", "AB")]        // the delimiter ends it
    public void ContentOf_Delimited(string extent, string data) => Assert.Equal(data, Delimited.ContentOf(extent));

    [Theory]
    [InlineData("\0\u0002AB", "AB")]
    [InlineData("\0\0", "")]
    [InlineData("\0\u0009AB", "AB")]    // a length field larger than the extent is clamped to what the extent holds
    [InlineData("\0", "")]              // shorter than its own length field
    public void ContentOf_ShortPrefixed(string extent, string data) => Assert.Equal(data, ShortPrefixed.ContentOf(extent));

    [Fact]
    public void ContentOf_PrefixedAndDelimited_TrustsTheLengthField_SoTheDataMayHoldAZero()
    {
        Assert.Equal("A\0B", Both.ContentOf("\0\0\0\u0003A\0B\0"));
    }

    [Fact]
    public void ContentOf_Delimited_AnExtentEndingInItsDelimiter_IsExactlyTheDataBeforeIt()
    {
        // The frame's extent table is the length, so data holding a binary zero round-trips; only a reader with no
        // table (TakeAt, the fixed form's padded extent) stops at the first zero.
        Assert.Equal("A\0B", Delimited.ContentOf("A\0B\0"));
        var layout = new CobolContiguousLayout(0, [0], [1], [9L], [Delimited.Code]);
        var extents = layout.ExtentsOf(new CobolVarGroup("", ["A\0B"]));
        string medium = layout.MediumImage("A\0B", extents);
        Assert.Equal("A\0B\0", medium);
        Assert.Equal("A\0B", layout.Decompose(medium, layout.MediumExtents(extents)).Dyn(0));
        Assert.Equal("A", layout.Decompose(medium).Dyn(0));   // no table: the first zero is the delimiter
    }

    // ── In: nothing but the structure says where the item ends (a file another program wrote) ────────────────────

    [Fact]
    public void TakeAt_Delimited_ReadsToTheFirstZero_AndConsumesIt()
    {
        int used = Delimited.TakeAt("xxAB\0CD\0", 2, out string data);
        Assert.Equal("AB", data);
        Assert.Equal(3, used);
        Assert.Equal(2, Delimited.TakeAt("\0\0", 1, out string empty) + 1);
        Assert.Equal("", empty);
    }

    [Fact]
    public void TakeAt_Delimited_AMissingDelimiter_TakesTheRestOfTheRecord()
    {
        int used = Delimited.TakeAt("ABC", 1, out string data);
        Assert.Equal("BC", data);
        Assert.Equal(2, used);
    }

    [Fact]
    public void TakeAt_Prefixed_ReadsTheLengthField_AndAnOptionalDelimiter()
    {
        Assert.Equal(6, ShortPrefixed.TakeAt("\0\u0004ABCDtail", 0, out string a));
        Assert.Equal("ABCD", a);
        Assert.Equal(7, Both.TakeAt("\0\0\0\u0002AB\0tail", 0, out string b));      // 4 + 2 + the delimiter
        Assert.Equal("AB", b);
        Assert.Equal(6, Both.TakeAt("\0\0\0\u0002ABtail", 0, out string c));        // no delimiter present: not consumed
        Assert.Equal("AB", c);
    }

    [Fact]
    public void TakeAt_ARecordThatEndsEarly_GivesWhatIsLeft_NeverReadingPastIt()
    {
        Assert.Equal(1, ShortPrefixed.TakeAt("\0", 0, out string shortField));
        Assert.Equal("", shortField);
        Assert.Equal(5, ShortPrefixed.TakeAt("\0cABC", 0, out string clamped));    // field says 99, three remain
        Assert.Equal("ABC", clamped);
    }

    // ── The layout: ONE record, two forms ──────────────────────────────────────────────────────────────────────

    // A dynamic DELIMITED · KY X(3) · B dynamic (no structure) · C dynamic SHORT PREFIXED · Z X(1):
    // the fixed run is KY + Z = 4, A at 0, B at 3, C at 3.
    private static readonly CobolContiguousLayout Mixed =
        new(4, [0, 3, 3], [1, 1, 1], [5L, 5L, 5L], [Delimited.Code, 0, ShortPrefixed.Code]);

    private static CobolVarGroup Sent => new("KEYZ", ["AB", "CDE", "FG"]);

    [Fact]
    public void TheMediumImage_FramesEachStructuredMember_AndLeavesTheOthersBare()
    {
        var extents = Mixed.ExtentsOf(Sent);
        string contiguous = "AB" + "KEY" + "CDE" + "FG" + "Z";
        Assert.Equal("AB\0" + "KEY" + "CDE" + "\0\u0002FG" + "Z", Mixed.MediumImage(contiguous, extents));
    }

    [Fact]
    public void TheMediumExtents_DescribeTheMediumImage()
    {
        var extents = Mixed.ExtentsOf(Sent);
        var medium = Mixed.MediumExtents(extents);
        Assert.Equal(new[] { 3, 3, 4 }, medium.Lengths);     // data + delimiter, bare data, length field + data
        Assert.Equal(new[] { 0, 3, 3 }, medium.FixedAt);
        Assert.True(medium.Describes("AB\0KEYCDE\0\u0002FGZ".Length, 4, [0, 3, 3], [1, 1, 1]));
    }

    [Fact]
    public void ARecord_RoundTripsThroughItsMediumForm_WithTheFramesTable()
    {
        var extents = Mixed.ExtentsOf(Sent);
        string medium = Mixed.MediumImage("ABKEYCDEFGZ", extents);
        var back = Mixed.Decompose(medium, Mixed.MediumExtents(extents));
        Assert.Equal("KEYZ", back.Fixed);
        Assert.Equal(new[] { "AB", "CDE", "FG" }, new[] { back.Dyn(0), back.Dyn(1), back.Dyn(2) });
    }

    // A dynamic DELIMITED · KY X(3) · C dynamic SHORT PREFIXED · B dynamic (no structure) · Z X(1): the plain member is
    // LAST of the dynamic ones, the one place a record with no table can be taken apart (the take step gives a plain
    // member what is left; it cannot know where a plain member ends when a structured one follows it).
    private static readonly CobolContiguousLayout PlainLast =
        new(4, [0, 3, 3], [1, 1, 1], [5L, 5L, 5L], [Delimited.Code, ShortPrefixed.Code, 0]);

    [Fact]
    public void ARecord_RoundTripsWithNoTable_TheStructureSayingWhereEachStructuredMemberEnds()
    {
        // A file another program wrote carries no extent table. The structured members find their own ends; the
        // plain one takes what is left beyond the fixed material.
        string medium = "AB\0" + "KEY" + "\0\u0002FG" + "CDE" + "Z";
        var back = PlainLast.Decompose(medium);
        Assert.Equal("KEYZ", back.Fixed);
        Assert.Equal("AB", back.Dyn(0));
        Assert.Equal("FG", back.Dyn(1));
        Assert.Equal("CDE", back.Dyn(2));
    }

    [Fact]
    public void APlainMemberBeforeAStructuredOne_KeepsTheTakeStepsReading_WithNoTable()
    {
        // With no table the earlier PLAIN member takes the excess beyond the later members' minimum (their length
        // field and delimiter) — the determination D-FRA states for a record that carries no extent table.
        string medium = "AB\0" + "KEY" + "CDE" + "\0\u0002FG" + "Z";
        var back = Mixed.Decompose(medium);
        Assert.Equal("AB", back.Dyn(0));
        Assert.Equal("CDE\0\u0002", back.Dyn(1));
    }

    [Fact]
    public void AStructuredMembersTrailingSpace_IsData_NotPadding()
    {
        var layout = new CobolContiguousLayout(0, [0], [1], [9L], [ShortPrefixed.Code]);
        var back = layout.Decompose("\0\u0003AB ");
        Assert.Equal("AB ", back.Dyn(0));
    }

    [Fact]
    public void AKeyBehindAStructuredMember_IsFound_WhereTheMediumImageHoldsIt()
    {
        var extents = Mixed.ExtentsOf(Sent);
        var mediumExtents = Mixed.MediumExtents(extents);
        string medium = Mixed.MediumImage("ABKEYCDEFGZ", extents);
        // KY is at fixed-run offset 0, behind the first member (AB and its delimiter: 3 characters) ...
        Assert.Equal(3, Mixed.Position(medium, 0, mediumExtents));
        // ... Z follows all three members: fixed offset 3 (after KEY), plus every extent (3 + 3 + 4).
        Assert.Equal(3 + 3 + 3 + 4, Mixed.Position(medium, 3, mediumExtents));
        // With no table the same position comes from the structures and the take step (the plain member LAST).
        string last = "AB\0" + "KEY" + "\0\u0002FG" + "CDE" + "Z";
        Assert.Equal(3 + 3 + 4 + 3, PlainLast.Position(last, 3));
    }

    [Fact]
    public void TheFixedForm_PadsEachMemberToItsMaximumExtent_AndRoundTrips()
    {
        var extents = Mixed.ExtentsOf(Sent);
        var mediumExtents = Mixed.MediumExtents(extents);
        string medium = Mixed.MediumImage("ABKEYCDEFGZ", extents);
        string form = Mixed.ToFixedForm(medium, mediumExtents);
        // A: 5 data + delimiter = 6; B: 5 (bare); C: 2 + 5 = 7; fixed run KEY + Z.
        Assert.Equal(6 + 3 + 5 + 7 + 1, form.Length);
        Assert.Equal("AB\0   " + "KEY" + "CDE  " + "\0\u0002FG   " + "Z", form);
        var back = Mixed.Decompose(form, null, fixedForm: true);
        Assert.Equal(new[] { "AB", "CDE", "FG" }, new[] { back.Dyn(0), back.Dyn(1), back.Dyn(2) });
        Assert.Equal("KEYZ", back.Fixed);
    }

    [Fact]
    public void AnUnstructuredLayout_HasOneForm_TheMediumImageIsTheContiguousOne()
    {
        var plain = new CobolContiguousLayout(1, [1], [1], [5L]);
        var extents = plain.ExtentsOf(new CobolVarGroup("K", ["AB"]));
        const string image = "KAB";
        Assert.Same(image, plain.MediumImage(image, extents));
        Assert.Same(extents, plain.MediumExtents(extents));
        Assert.False(plain.HasStructure);
    }

    [Fact]
    public void TheExtentTable_ConvertsItself_ThroughItsOwnLayout()
    {
        var extents = Mixed.ExtentsOf(Sent);
        Assert.Equal("AB\0KEYCDE\0\u0002FGZ", extents.MediumImage("ABKEYCDEFGZ"));
        Assert.Equal(new[] { 3, 3, 4 }, extents.Medium().Lengths);
        var noLayout = new RecordExtents([0], [2]);
        Assert.Same(noLayout, noLayout.Medium());
        Assert.Equal("AB", noLayout.MediumImage("AB"));
    }

    [Fact]
    public void ATableThatDoesNotDescribeTheImage_IsACompilerDefect_NotARecord()
    {
        var wrong = new RecordExtents([0, 3, 3], [9, 9, 9], Mixed);
        Assert.Throws<InvalidOperationException>(() => Mixed.MediumImage("ABKEYCDEFGZ", wrong));
    }
}
