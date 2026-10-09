// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE EXTENT TABLE of a variable-length record (docs/CONFORMANCE.md §3 determination D-FRA (v); kb/Work PB1053).
/// A record with dynamic-length members on BOTH sides of a fixed member — or side by side — is sent as its contiguous
/// image (ISO §8.5.1.11.2), and no split of the characters alone recovers it: <c>"AA" "KEY" "CCCC"</c> and
/// <c>"AAKEY" "CCC" "C"</c> are the same characters. The table travels beside the record, and these pin the four
/// things it owes: the decomposition is the exact inverse of the composer for every layout; a key the members
/// precede is found where the decomposition puts it; a table that does not describe the record received (another
/// layout under §8.5.1.12.2, or characters truncated on the way) is ignored in favour of the take step; and both
/// physical shapes of the framing carry it without it entering the record.
/// </summary>
public sealed class RecordExtentsTests
{
    // A dynamic (max 5) · KY X(3) · C dynamic (max 5) · FILLER X(2): fixed run 5, A at 0, C at 3.
    private static readonly CobolContiguousLayout BothSides = new(5, [0, 3], [1, 1], [5L, 5L]);

    private const string Written = "AA" + "KEY" + "CCCC" + "  ";

    [Fact]
    public void TheTakeStep_CannotInvert_MembersOnBothSides()
    {
        // The measured defect: without the table the earlier member takes the excess and KY reads C's characters.
        var v = BothSides.Decompose(Written);
        Assert.Equal("AAKEY", v.Dyn(0));
        Assert.Equal("CCC  ", v.Fixed);
    }

    [Fact]
    public void TheRecordsOwnTable_RestoresEveryMember()
    {
        var extents = BothSides.ExtentsOf(new CobolVarGroup("KEY  ", ["AA", "CCCC"]));
        var v = BothSides.Decompose(Written, extents);
        Assert.Equal("AA", v.Dyn(0));
        Assert.Equal("CCCC", v.Dyn(1));
        Assert.Equal("KEY  ", v.Fixed);
    }

    [Fact]
    public void AdjacentMembers_RoundTrip()
    {
        // K X(1) · A dynamic · B dynamic: both components at fixed offset 1 — the take step gives A everything.
        var layout = new CobolContiguousLayout(1, [1, 1], [1, 1], [10L, 10L]);
        var extents = new RecordExtents([1, 1], [2, 2]);
        var v = layout.Decompose("K" + "AB" + "CD", extents);
        Assert.Equal("AB", v.Dyn(0));
        Assert.Equal("CD", v.Dyn(1));
        Assert.Equal("ABCD", layout.Decompose("K" + "AB" + "CD").Dyn(0));
    }

    [Fact]
    public void AKey_IsFound_WhereTheDecompositionPutsIt()
    {
        var extents = new RecordExtents([0, 3], [2, 4]);
        int at = BothSides.Position(Written, 0, extents);
        Assert.Equal(2, at);
        Assert.Equal("KEY", Written.Substring(at, 3));
        Assert.NotEqual(2, BothSides.Position(Written, 0));   // the take step's answer, which the table corrects
    }

    [Fact]
    public void ATableForAnotherLayout_DoesNotDescribeTheRecord()
    {
        // §8.5.1.12.2: components correspond only at the same fixed-run positions.
        var other = new RecordExtents([0, 2], [2, 4]);
        Assert.False(other.Describes(Written.Length, 5, [0, 3], [1, 1]));
        Assert.Equal("AAKEY", BothSides.Decompose(Written, other).Dyn(0));   // the take step
    }

    [Fact]
    public void ATableForOtherCharacters_DoesNotDescribeTheRecord()
    {
        // A record truncated on its way is no longer the image the table describes.
        var extents = new RecordExtents([0, 3], [2, 4]);
        Assert.False(extents.Describes(Written.Length - 1, 5, [0, 3], [1, 1]));
        Assert.True(extents.Describes(Written.Length, 5, [0, 3], [1, 1]));
    }

    [Fact]
    public void AVoidTable_CorrespondsToNoLayout()
    {
        var voided = RecordFraming.VoidExtents(2);
        Assert.False(voided.Describes(Written.Length, 5, [0, 3], [1, 1]));
    }

    [Fact]
    public void TheStoreFraming_CarriesTheTable_OutsideTheRecord()
    {
        var extents = new RecordExtents([0, 3], [2, 4]);
        var attributes = new FixedFileAttributes(FixedFileAttributes.Relative, true, 5, 17, []);
        using var ms = RecordFraming.ComposeStore(attributes,
            [new StoredFrame(Written, extents), null, new StoredFrame("PLAIN", null)], generation: 0, releaseMint: 0);
        var back = RecordFraming.DecodeStore(ms.GetBuffer(), (int)ms.Length, out _);
        Assert.Equal(3, back.Count);
        Assert.Equal(Written, back[0]!.Value.Image);
        Assert.Equal([2, 4], back[0]!.Value.Extents!.Lengths);
        Assert.Equal([0, 3], back[0]!.Value.Extents!.FixedAt);
        Assert.Null(back[1]);
        Assert.Equal("PLAIN", back[2]!.Value.Image);
        Assert.Null(back[2]!.Value.Extents);
    }

    [Fact]
    public void TheStreamFraming_CarriesTheTable_AndTheFrameWalkSkipsIt()
    {
        var extents = new RecordExtents([0, 3], [2, 4]);
        var text = new StringWriter();
        RecordFraming.WriteFrameHead(text, Written.Length, extents);
        text.Write(Written);
        RecordFraming.WriteFrameHead(text, 5, null);
        text.Write("PLAIN");
        string medium = text.ToString();

        int len = RecordFraming.PrefixLength(medium.AsSpan(0, 4), out bool follows);
        Assert.True(follows);
        Assert.Equal(Written.Length, len);
        int count = RecordFraming.ExtentCount(medium.AsSpan(4, 4));
        Assert.Equal(2, count);
        var decoded = RecordFraming.ExtentTableFromChars(medium.AsSpan(8, 8 * count))!;
        Assert.Equal([2, 4], decoded.Lengths);
        Assert.Equal(Written, medium.Substring(8 + (8 * count), len));

        using var bytes = new MemoryStream(Encoding.Latin1.GetBytes(medium));
        var starts = RecordFraming.FrameStarts(bytes);
        Assert.Equal([0L, 8 + (8 * count) + len], starts);
    }
}
