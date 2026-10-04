// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The D-FRA split rule (docs/CONFORMANCE.md §3; kb/Work PB981) — <see cref="CobolVarGroup.FromContiguous"/>, the
/// ONE decomposition of a variable-length record read back from a file. A WRITE sends the record "as though it
/// were in fact contiguous with its neighbors" (ISO §8.5.1.11.2), so the read record carries no marker of where a
/// dynamic member ends; each component takes as many whole units as the record holds beyond the fixed material
/// still to come, up to its maximum. These pin the rule's three stated properties: an exact inverse of the
/// composer for ONE variable-length member wherever it sits, the EARLIER component taking the excess when there
/// are several, and a short record leaving every component empty.
/// </summary>
public sealed class CobolVarGroupContiguousTests
{
    // Record layout A X(3) · D dynamic (max 20) · B X(2): fixed run 5, D at fixed offset 3.
    private static CobolVarGroup Split(string record) =>
        CobolVarGroup.FromContiguous(record, 5, [3], [1], [20L]);

    [Fact]
    public void OneDynamicMember_InTheMiddle_IsTheExactInverseOfTheComposer()
    {
        var v = Split("ABC" + "HELLO" + "XY");
        Assert.Equal("ABCXY", v.Fixed);
        Assert.Equal("HELLO", v.Dyn(0));
    }

    [Fact]
    public void ARecordShorterThanTheFixedRun_LeavesTheComponentEmpty_AndTheFixedRunShort()
    {
        var v = Split("SHOR");
        Assert.Equal("SHOR", v.Fixed);
        Assert.Equal("", v.Dyn(0));
        Assert.True(v.HasDyn(0));
    }

    [Fact]
    public void AComponentStopsAtItsMaximum_AndTheFixedMaterialFollowsIt()
    {
        var v = CobolVarGroup.FromContiguous("ABC" + "0123456789" + "XY", 5, [3], [1], [4L]);
        Assert.Equal("0123", v.Dyn(0));
        Assert.Equal("ABC45", v.Fixed);   // the fixed material after D is read from D's end, left to right
    }

    [Fact]
    public void SeveralComponents_TheEarlierTakesTheExcess()
    {
        // K X(1) · D1 dynamic · D2 dynamic: fixed run 1, both at fixed offset 1.
        var v = CobolVarGroup.FromContiguous("K" + "AB" + "CD", 1, [1, 1], [1, 1], [10L, 10L]);
        Assert.Equal("K", v.Fixed);
        Assert.Equal("ABCD", v.Dyn(0));
        Assert.Equal("", v.Dyn(1));
    }

    [Fact]
    public void ATableComponent_TakesWholeElementsOnly()
    {
        // K X(1) · T dynamic table of 3-character elements (max 5) · Z X(1): fixed run 2, T at fixed offset 1.
        var v = CobolVarGroup.FromContiguous("K" + "AAABBBC" + "Z", 2, [1], [3], [5L]);
        Assert.Equal("AAABBB", v.Dyn(0));
        Assert.Equal("KC", v.Fixed);   // the partial element is not taken; the fixed run reads on from there
    }

    // ── CobolContiguousLayout.Position — a key a variable-length member precedes (kb/Work PB1025, D-KWV) ─────

    // Record layout NM dynamic (max 10) · KY X(2) · FL X(20): fixed run 22, NM at fixed offset 0, KY at 0.
    private static readonly CobolContiguousLayout KeyAfterDynamic = new(22, [0], [1], [10L]);

    [Theory]
    [InlineData("", "20")]
    [InlineData("A", "30")]
    [InlineData("CCCCCCCCCC", "10")]
    public void AKeyAfterADynamicMember_IsFoundWhereTheDecompositionPutsIt(string nm, string ky)
    {
        string record = nm + ky + new string('.', 20);
        int at = KeyAfterDynamic.Position(record, 0);
        Assert.Equal(nm.Length, at);
        Assert.Equal(ky, record.Substring(at, 2));
        // the SAME take step: the decomposition's fixed run starts with the key the position located
        Assert.StartsWith(ky, KeyAfterDynamic.Decompose(record).Fixed);
    }

    [Fact]
    public void AKeyBeforeEveryDynamicMember_KeepsItsFixedOffset()
    {
        // KY X(2) · NM dynamic (max 10) · FL X(3): fixed run 5, NM at fixed offset 2 — it FOLLOWS the key.
        var layout = new CobolContiguousLayout(5, [2], [1], [10L]);
        Assert.Equal(0, layout.Position("KY" + "HELLO" + "FFF", 0));
    }

    [Fact]
    public void EveryPrecedingMember_IsCharged_WhenSeveralPrecedeTheKey()
    {
        // A dynamic (max 4) · B X(1) · C dynamic (max 3) · KY X(2): fixed run 3, A at 0, C at 1, KY at 1.
        var layout = new CobolContiguousLayout(3, [0, 1], [1, 1], [4L, 3L]);
        string record = "AAAA" + "1" + "CCC" + "20";
        Assert.Equal(8, layout.Position(record, 1));
        Assert.Equal("20", record.Substring(layout.Position(record, 1), 2));
    }

    // ── kb/Work PB244 shape (b): the OCCURS DEPENDING table as the record's trailing component ─────────────────
    // Record H X(1) · D dynamic (max 5) · T X(1) OCCURS 1 TO 3 DEPENDING: the activation-boundary carrier holds T in
    // its fixed run (fixed "H" + T at its current count), the record layout makes T the LAST component.

    [Fact]
    public void SplitTail_MovesTheTrailingFixedMaterial_IntoOneMoreComponent()
    {
        var carrier = new CobolVarGroup("Hxy", ["abc"]);
        var split = carrier.SplitTail(1);
        Assert.Equal("H", split.Fixed);
        Assert.Equal(["abc", "xy"], split.Dynamic);
        var rejoined = split.JoinTail(1);
        Assert.Equal(carrier.Fixed, rejoined.Fixed);
        Assert.Equal(carrier.Dynamic, rejoined.Dynamic);
    }

    [Fact]
    public void ATailCarrierShorterThanTheTailStart_IsPaddedNotFaulted()
    {
        var split = new CobolVarGroup("", []).SplitTail(2);
        Assert.Equal("  ", split.Fixed);
        Assert.Equal([""], split.Dynamic);
    }

    [Fact]
    public void OdoTailLayout_DecomposesARecordByTheTablesCurrentCount_AndRoundTripsItsExtents()
    {
        // fixed run 1 ("H"); D at fixed offset 1 (unit 1, max 5); T at fixed offset 1 (unit 1, max 3, the tail).
        var layout = new CobolContiguousLayout(1, [1, 1], [1, 1], [5L, 3L], OdoTail: true);
        var sent = new CobolVarGroup("Hxy", ["abc"]);                // the carrier AsVarImage(2) composes
        var extents = layout.ExtentsOf(sent);
        Assert.Equal([3, 2], extents.Lengths);                        // D "abc", T "xy"
        Assert.Equal([1], layout.ComponentOffsets);                  // the carrier has ONE component: D
        var back = layout.Decompose("Habcxy", extents);
        Assert.Equal("Hxy", back.Fixed);
        Assert.Equal(["abc"], back.Dynamic);
        // the take step, with no table: D (the EARLIER component) takes the whole excess up to its maximum
        var taken = layout.Decompose("Habcde");
        Assert.Equal("abcde", taken.Dyn(0));
        Assert.Equal("H", taken.Fixed);
    }

    [Fact]
    public void Concat_JoinsEveryOccurrencesFixedRunAndComponents_InOrder()
    {
        var joined = CobolVarGroup.Concat([new CobolVarGroup("a1", ["ab"]), new CobolVarGroup("c2", ["c"])]);
        Assert.Equal("a1c2", joined.Fixed);
        Assert.Equal(["ab", "c"], joined.Dynamic);
    }
}
