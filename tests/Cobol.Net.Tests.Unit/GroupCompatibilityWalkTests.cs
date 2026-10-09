// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE §8.5.1.12 WALK MEASURES A PAIR, NOT EITHER GROUP ALONE (kb/Work PB2689).
/// <para>ISO §8.5.1.12.3: "Two corresponding tables match when the byte length of their elements is equal and their
/// elements are compatible", and "If one of the corresponding tables is not a dynamic-capacity table ... the
/// dynamic-capacity table is considered to be the same length as the corresponding table". So when two corresponding
/// tables' elements are groups, the element lengths compared are the ones the element walk measures for the pair — an
/// element holding <c>OCCURS DYNAMIC</c> opposite one holding <c>OCCURS 2</c> is 2 bytes there, although its
/// one-occurrence atom is 1. The walk compared the atoms' static <see cref="GroupAtom.ElementBytes"/> first and refused
/// the pair.</para>
/// <para>§8.5.1.12.2's latitude for unequal lengths covers a dynamic-capacity table "beyond the last character of the
/// shorter group" only. Once the shorter group's atoms were spent the walk stopped checking positions, so a
/// dynamic-capacity table that still started INSIDE the shorter group's last item was accepted with no corresponding
/// table.</para>
/// </summary>
public sealed class GroupCompatibilityWalkTests
{
    private static GroupAtom Fx(int n) => new(GroupAtomKind.Fixed, n, n);

    // 10 IA PIC X OCCURS DYNAMIC · 10 KA PIC X — the element of a dynamic table (one element: 1 + 1 bytes).
    private static readonly GroupAtom[] DynInnerElement = [new(GroupAtomKind.DynamicTable, 1, 1, 1, 1), Fx(1)];
    // 10 IB PIC X OCCURS 2 · 10 KB PIC X — 2 + 1 bytes.
    private static readonly GroupAtom[] FixedInnerElement = [new(GroupAtomKind.Table, 2, 2, 1, 1), Fx(1)];

    private static GroupAtom DynTableOf(GroupAtom[] element, int elementBytes) =>
        new(GroupAtomKind.DynamicTable, elementBytes, elementBytes, elementBytes, elementBytes, element);

    [Fact]
    public void ElementsHoldingADynamicTableOppositeAFixedOne_MatchAtTheLengthThePairGivesThem()
    {
        GroupAtom[] g1 = [DynTableOf(DynInnerElement, 2), Fx(2)];
        GroupAtom[] g2 = [DynTableOf(FixedInnerElement, 3), Fx(2)];
        var pairs = new List<(int, int)>();
        Assert.Null(GroupCompatibility.Walk(g1, g2, pairs));
        Assert.Null(GroupCompatibility.Walk(g2, g1));
        Assert.Equal([(0, 0)], pairs);
    }

    [Fact]
    public void ElementsWhoseMeasuredLengthsDiffer_DoNotMatch_AndTheReasonNamesTheMeasuredLengths()
    {
        // 10 IB PIC X OCCURS 2 · 10 KB PIC X(2): the pair measures 2 + 1 against 2 + 2.
        GroupAtom[] longer = [new(GroupAtomKind.Table, 2, 2, 1, 1), Fx(2)];
        var m = GroupCompatibility.Walk([DynTableOf(DynInnerElement, 2)], [DynTableOf(longer, 4)]);
        Assert.NotNull(m);
        Assert.Equal(GroupMismatchKind.ElementBytesDiffer, m.Kind);
        Assert.Equal((3L, 4L), (m.FirstLength, m.SecondLength));
    }

    [Fact]
    public void ADynamicTableStartingInsideTheShorterGroup_HasNoCorrespondingTable()
    {
        // 01 G1. 05 X1 PIC X(5).  against  01 G2. 05 Y2 PIC X(2). 05 T2 PIC X OCCURS DYNAMIC.
        GroupAtom[] g1 = [Fx(5)];
        GroupAtom[] g2 = [Fx(2), new(GroupAtomKind.DynamicTable, 1, 1, 1, 1)];
        var m = GroupCompatibility.Walk(g1, g2);
        Assert.NotNull(m);
        Assert.Equal((GroupMismatchKind.DynamicTableUnpaired, false, 1, 2L), (m.Kind, m.OnFirst, m.Atom, m.Position));
    }

    [Fact]
    public void ADynamicLengthItemStartingInsideTheShorterGroup_IsUnpaired_NotBeyondTheEnd()
    {
        GroupAtom[] g1 = [Fx(5)];
        GroupAtom[] g2 = [Fx(2), new(GroupAtomKind.DynamicLength, 0, 0)];
        Assert.Equal(GroupMismatchKind.DynamicLengthUnpaired, GroupCompatibility.Walk(g1, g2)!.Kind);
    }

    [Fact]
    public void ADynamicTableBeyondTheShorterGroupsLastCharacter_KeepsItsLatitude()
    {
        // §8.5.1.12.2: it "is treated as if it corresponds to a space-filled fixed-length table".
        GroupAtom[] g1 = [Fx(2)];
        GroupAtom[] g2 = [Fx(2), new(GroupAtomKind.DynamicTable, 1, 1, 1, 1)];
        Assert.Null(GroupCompatibility.Walk(g1, g2));
        Assert.Null(GroupCompatibility.Walk(g2, g1));
    }
}
