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
        var layout = new CobolContiguousLayout(1, [1, 1], [1, 1], [5L, 3L], Odo: new OdoTail(1, 3));
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

    // ── kb/Work PB244 shape (b), the CELL-BACKED half: the same record in a storage cell ───────────────────────────
    // Window H X(1) · D dynamic (max 5, ordinal 0 at window offset 1) · T X(1) OCCURS 1 TO 3 DEPENDING, held at its
    // maximum (3 positions) as the window's trailing storage: Ref "Hxyz", D "abc".

    private static readonly OdoTail Tail = new(1, 3);

    private static StorageCell Cell()
    {
        var cell = new StorageCell { Ref = "Hxyz" };
        cell.SetDynAt(0, "abc");
        return cell;
    }

    [Theory]
    [InlineData(3, "Habcxyz")]
    [InlineData(2, "Habcxy")]
    [InlineData(1, "Habcx")]
    [InlineData(0, "Habc")]      // below integer-1: the extent is what the count names (the caller clamps to the minimum)
    [InlineData(9, "Habcxyz")]   // above integer-2: clamped to the maximum
    public void ACellWindow_ComposesItsContiguousImage_AtTheOdoCount(int count, string expected) =>
        Assert.Equal(expected, Cell().ContiguousAt(0, 4, 0, [1], [0], Tail, count));

    [Fact]
    public void ACellWindowsCarrier_CutsItsFixedRun_AtTheOdoCount()
    {
        var carrier = Cell().VarGroupAt(0, 4, 0, [1], [0], Tail, 2);
        Assert.Equal("Hxy", carrier.Fixed);
        Assert.Equal(["abc"], carrier.Dynamic);
    }

    [Fact]
    public void ACellWindowsExtents_CarryTheTableAsTheLastComponent()
    {
        var extents = Cell().ContiguousExtentsAt(4, 0, [1], [5], [0], [0], Tail, 2);
        Assert.Equal([3, 2], extents.Lengths);   // D "abc", T "xy"
        Assert.Equal([1, 1], extents.FixedAt);
    }

    [Fact]
    public void ARecordStoredIntoACellWindow_IsDecomposedWithTheTableAsItsLastComponent()
    {
        // The record travels with its extent table (D-FRA (v)): D "abc" then the table's two occurrences.
        var cell = Cell();
        var extents = cell.ContiguousExtentsAt(4, 0, [1], [5], [0], [0], Tail, 2);
        var receiver = new StorageCell { Ref = "H   " };
        receiver.StoreContiguousAt(0, 4, 0, [1], [5], [0], [0], Tail, "Habcxy", extents);
        Assert.Equal("abc", receiver.DynAt(0));
        Assert.Equal("Hxy ", receiver.Ref);   // the table's third occurrence is the excess: space-filled
        // With no table to say where D ends, the take step (D-FRA) gives the EARLIER component, D, the whole excess
        // up to its maximum - the same answer a declared group's layout gives (OdoTailLayout test above).
        var taken = new StorageCell { Ref = "H   " };
        taken.StoreContiguousAt(0, 4, 0, [1], [5], [0], [0], Tail, "Habcxy");
        Assert.Equal("abcxy", taken.DynAt(0));
    }

    // ── kb/Work PB244, a TABLE OF VARIABLE-LENGTH ELEMENTS in a cell ───────────────────────────────────────────────
    // Window H X(1) · T X(1)+dynamic-DD OCCURS 1 TO 3 DEPENDING, at its maximum (3 positions): Ref "Hxyz", the
    // table's components (one per occurrence, ordinals 0..2, each BEFORE its occurrence's FX) "a", "bb", "ccc".

    private static readonly OdoTail ElementTail = new(1, 3, Comps: 1);

    private static StorageCell ElementCell()
    {
        var cell = new StorageCell { Ref = "Hxyz" };
        cell.SetDynAt(0, "a");
        cell.SetDynAt(1, "bb");
        cell.SetDynAt(2, "ccc");
        return cell;
    }

    [Theory]
    [InlineData(3, "Haxbbyccc" + "z")]
    [InlineData(2, "Haxbby")]
    [InlineData(1, "Hax")]
    [InlineData(0, "H")]
    [InlineData(9, "Haxbbyccc" + "z")]
    public void ACellWindowOfElementsWithComponents_DropsTheOccurrencesBeyondTheCount_WithTheirComponents(int count, string expected) =>
        Assert.Equal(expected, ElementCell().ContiguousAt(0, 4, 0, [1, 2, 3], [0, 0, 0], ElementTail, count));

    [Fact]
    public void ACellWindowOfElementsWithComponents_CarriesOnlyTheCurrentOccurrencesComponents()
    {
        var carrier = ElementCell().VarGroupAt(0, 4, 0, [1, 2, 3], [0, 0, 0], ElementTail, 2);
        Assert.Equal("Hxy", carrier.Fixed);
        Assert.Equal(["a", "bb"], carrier.Dynamic);
        Assert.Equal(1, ElementTail.CutAt(2));
        Assert.Equal(1, ElementTail.CutComponents(2));
        Assert.Equal(0, ElementTail.CutComponents(3));
    }

    [Fact]
    public void AReceivingCellWindow_UsesOnlyTheCurrentOccurrences_AndTheRestKeepTheirContent()
    {
        // ISO §13.18.38.4 GR8 a): with the DEPENDING item outside the group, "only that part of the table area that is
        // specified by the value of the data item ... at the start of the operation will be used" - occurrence 1 of 3.
        var carrier = new CobolVarGroup("HNEW", ["n1", "n2", "n3"]);
        var cell = ElementCell();
        cell.StoreVarGroupAt(0, 4, 0, [1, 2, 3], [5, 5, 5], [0, 0, 0], ElementTail, 1, carrier);
        Assert.Equal("HNyz", cell.Ref);   // H and occurrence 1 stored; occurrences 2 and 3 keep "y", "z"
        Assert.Equal(["n1", "bb", "ccc"], [cell.DynAt(0), cell.DynAt(1), cell.DynAt(2)]);
        // the maximum count (a depending item inside the group, a record read back, a boundary) stores them all
        var whole = ElementCell();
        whole.StoreVarGroupAt(0, 4, 0, [1, 2, 3], [5, 5, 5], [0, 0, 0], ElementTail, int.MaxValue, carrier);
        Assert.Equal("HNEW", whole.Ref);
        Assert.Equal(["n1", "n2", "n3"], [whole.DynAt(0), whole.DynAt(1), whole.DynAt(2)]);
    }

    [Fact]
    public void ADynamicTableOfElementsWithComponents_IsComposedWithTheElementsOwnShape()
    {
        // Window H X(1) · TD dynamic-capacity table whose element is DD dynamic + FX X(1): the table is component 0,
        // reserving its one-element extent (1) after H. Each element cell is a scope of its own, numbered from zero.
        var cell = new StorageCell { Ref = "H " };
        var table = cell.DynTableAt(0, 0, 1);
        var first = table.RefReceiving(1);
        first.Ref = "1";
        first.SetDynAt(0, "ab");
        var second = table.RefReceiving(2);
        second.Ref = "2";
        second.SetDynAt(0, "c");
        var shape = new CellGroupShape(1, [0], [0], [5]);
        Assert.Equal("Hab1c2", cell.ContiguousAt(0, 2, 0, [1], [1], default, 0, [shape]));
        // Without the shape the element is its fixed run only - the image the pre-PB244 composer gave, now the
        // fixed-element lane (an element with no components of its own).
        Assert.Equal("H12", cell.ContiguousAt(0, 2, 0, [1], [1], default, 0));
    }

    // ── kb/Work PB2497, a RECORD of an OCCURS DEPENDING table of VARIABLE-LENGTH ELEMENTS ────────────────────────────
    // Record K X(1) · TE OCCURS 1 TO 3 DEPENDING ON K, each element DD dynamic (max 5) then FX X(1). At the maximum the
    // fixed run is "K" + three FX (4 positions) and DD(i) sits before FX(i): fixed offsets 1, 2, 3. One occurrence is
    // one fixed position and one component.

    private static CobolContiguousLayout ElementRecord() =>
        new(4, [1, 2, 3], [1, 1, 1], [5L, 5L, 5L], Odo: new OdoTail(1, 3, Comps: 1));

    [Fact]
    public void ARecordOfVariableLengthElements_IsReadBackAtTheCountItsExtentTableStates()
    {
        var layout = ElementRecord();
        // the carrier AsVarImage(2) composes: the first two occurrences' fixed runs and components
        var sent = new CobolVarGroup("2xy", ["ab", "cde"]);
        var extents = layout.ExtentsOf(sent);
        Assert.Equal([2, 3], extents.Lengths);
        Assert.Equal([1, 2], extents.FixedAt);
        Assert.Equal([1, 2, 3], layout.ComponentOffsets);   // the components at the maximum, for compare and the boundary
        var back = layout.Decompose("2abxcdey", extents, count: 1);   // the record states 2: the count passed is ignored
        Assert.Equal("2xy", back.Fixed);
        Assert.Equal(["ab", "cde"], back.Dynamic);
        Assert.True(layout.StatesCount("2abxcdey", extents, fixedForm: false));
    }

    [Fact]
    public void ARecordOfVariableLengthElements_WithNoTable_IsDecomposedAtTheCountTheReadSupplies()
    {
        var layout = ElementRecord();
        Assert.False(layout.StatesCount("1abx", null, fixedForm: false));
        // at data-name-1's count (one occurrence) the take step is exact
        var one = layout.Decompose("1abx", null, count: 1);
        Assert.Equal("1x", one.Fixed);
        Assert.Equal(["ab"], one.Dynamic);
        // at the maximum (the default, §13.18.38.4 GR8 b) the same record leaves every component empty, the fixed run
        // taking the characters in turn: the first pass, which places a data-name-1 the record holds before the table
        var max = layout.Decompose("1abx");
        Assert.Equal("1abx", max.Fixed);
        Assert.Equal(["", "", ""], max.Dynamic);
        // a table describing other characters than the record's does not describe the record
        var stale = layout.ExtentsFrom([2, 3]);
        Assert.False(layout.StatesCount("1abx", stale, fixedForm: false));
    }

    [Fact]
    public void ARecordOfVariableLengthElements_TakesItsFixedForm_AtTheCountWritten_AndIsReadBackAtTheMaximum()
    {
        var layout = ElementRecord();
        var extents = layout.ExtentsOf(new CobolVarGroup("2xy", ["ab", "cde"]));
        // each DD padded to its maximum (5); the absent third occurrence is beyond the end, where the file fills spaces
        string form = layout.ToFixedForm("2abxcdey", extents);
        Assert.Equal("2ab   xcde  y", form);
        string record = form.PadRight(4 + 15);   // the file's fixed length: the record's maximum size
        Assert.True(layout.StatesCount(record, null, fixedForm: true));
        var back = layout.Decompose(record, null, fixedForm: true);
        Assert.Equal("2xy ", back.Fixed);
        Assert.Equal(["ab", "cde", ""], back.Dynamic);
    }

    [Theory]
    [InlineData(3, 3, 3)]
    [InlineData(1, 3, 1)]
    [InlineData(0, 3, 0)]
    [InlineData(4, 3, null)]   // more components than the maximum holds
    public void AnOdoTail_CountsTheOccurrencesOfARecord_ByItsComponents(int components, int total, int? expected) =>
        Assert.Equal(expected, new OdoTail(1, 3, Comps: 1).CountFrom(components, total));

    [Fact]
    public void AnOdoTailOfTwoComponentsAnOccurrence_RefusesAPartialOccurrence_AndAFixedElementTailCountsNothing()
    {
        Assert.Equal(1, new OdoTail(1, 3, Comps: 2).CountFrom(2, 6));
        Assert.Null(new OdoTail(1, 3, Comps: 2).CountFrom(3, 6));
        Assert.Null(new OdoTail(1, 3).CountFrom(1, 1));
    }

    [Fact]
    public void ACellWindowOfElementsWithComponents_IsWrittenAndReadBack_AtTheCountItsExtentTableStates()
    {
        // the ElementCell above at count 2: "H", (a, x), (bb, y)
        var extents = ElementCell().ContiguousExtentsAt(4, 0, [1, 2, 3], [5, 5, 5], [0, 0, 0], [0, 0, 0], ElementTail, 2);
        Assert.Equal([1, 2], extents.Lengths);
        Assert.Equal([1, 2], extents.FixedAt);
        var receiver = new StorageCell { Ref = "H   " };
        Assert.True(receiver.StoreContiguousAt(0, 4, 0, [1, 2, 3], [5, 5, 5], [0, 0, 0], [0, 0, 0], ElementTail,
            "Haxbby", extents, count: 1));
        Assert.Equal("Hxy ", receiver.Ref);   // the third occurrence is beyond the record: space-filled
        Assert.Equal(["a", "bb", ""], [receiver.DynAt(0), receiver.DynAt(1), receiver.DynAt(2)]);
        // no table: the count is the one the READ supplies, and the call says the record did not state it
        var taken = new StorageCell { Ref = "H   " };
        Assert.False(taken.StoreContiguousAt(0, 4, 0, [1, 2, 3], [5, 5, 5], [0, 0, 0], [0, 0, 0], ElementTail,
            "Hax", null, count: 1));
        Assert.Equal("Hx  ", taken.Ref);
        Assert.Equal("a", taken.DynAt(0));
    }

    [Fact]
    public void ConcatVarImages_TakesTheFirstCountOccurrencesCarriers_InOrder_AndClampsTheCount()
    {
        string[] table = ["a1", "b2", "c3"];
        CobolVarGroup Carrier(string e) => new(e[1..], [e[..1]]);
        var two = CobolTable.ConcatVarImages(table, 2, Carrier);
        Assert.Equal("12", two.Fixed);
        Assert.Equal(["a", "b"], two.Dynamic);
        Assert.Equal(["a", "b", "c"], CobolTable.ConcatVarImages(table, 9, Carrier).Dynamic);
        Assert.Empty(CobolTable.ConcatVarImages(table, 0, Carrier).Dynamic);
    }

    [Fact]
    public void Concat_JoinsEveryOccurrencesFixedRunAndComponents_InOrder()
    {
        var joined = CobolVarGroup.Concat([new CobolVarGroup("a1", ["ab"]), new CobolVarGroup("c2", ["c"])]);
        Assert.Equal("a1c2", joined.Fixed);
        Assert.Equal(["ab", "c"], joined.Dynamic);
    }
}
