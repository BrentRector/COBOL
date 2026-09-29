// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A Format 2 (table) VALUE phrase's ELEMENT COUNT and its §13.18.63.4 GR12 odometer walk must agree
/// (kb/Work PB1716). <see cref="TableValueOdometer.Resolve"/> fills exactly
/// <see cref="TableValueOdometer.ElementCount"/> elements by stepping <see cref="TableValueOdometer.Step"/>; the
/// count used to be absent, and a 64,000,000-element cap was the fill's only bound, so a phrase that violated
/// §13.18.63.3 SR23 ran to the cap before being rejected (about 30 s for a 26-line program). The count is a
/// mixed-radix distance and the walk is a carry chain: two statements of one order, so this class proves them
/// equal over every tuple pair of a small table, and pins the three ways a phrase names no run (a subscript out of
/// range, TO before FROM, and unequal subscripts above a dimension with no ceiling).
/// </summary>
public sealed class TableValueElementCountTests
{
    private static TableValueDim Fixed(int max) => new(Item(), max, Dynamic: false);

    private static TableValueDim Unbounded() => new(Item(), null, Dynamic: true);

    private static DataItem Item() => new() { Level = 5, CobolName = "T", CsName = "T" };

    private static Subscripts S(params int[] v) => new(v);

    private static TableValueSpec Spec(string[] literals, int[] from, int[] to) =>
        new(literals, from, to, Ordinal: 0,
            [.. from.Select(n => n.ToString())], [.. to.Select(n => n.ToString())]);

    /// <summary>Every (FROM, TO) pair of a 2×3×2 table: the count equals the number of odometer steps from FROM
    /// to TO, plus one — and is null exactly when TO precedes FROM.</summary>
    [Fact]
    public void Count_AgreesWithTheOdometerWalk_OverEveryPair()
    {
        TableValueDim[] dims = [Fixed(2), Fixed(3), Fixed(2)];
        var tuples = new List<int[]>();
        var cur = new[] { 1, 1, 1 };
        do tuples.Add([.. cur]); while (TableValueOdometer.Step(cur, dims));
        Assert.Equal(12, tuples.Count);

        for (int i = 0; i < tuples.Count; i++)
            for (int j = 0; j < tuples.Count; j++)
            {
                long? count = TableValueOdometer.ElementCount(dims, S(tuples[i]), S(tuples[j]));
                if (j < i) Assert.Null(count);
                else Assert.Equal(j - i + 1, count);
            }
    }

    /// <summary>§13.18.63.3 SR20/SR21: a subscript outside 1..the dimension's ceiling names no element.</summary>
    [Fact]
    public void OutOfRangeSubscript_NamesNoRun()
    {
        TableValueDim[] dims = [Fixed(2), Fixed(3)];
        Assert.Null(TableValueOdometer.ElementCount(dims, S(0, 1), S(1, 1)));
        Assert.Null(TableValueOdometer.ElementCount(dims, S(1, 1), S(1, 4)));
        Assert.Null(TableValueOdometer.ElementCount(dims, S(1, 1), S(3, 1)));
        Assert.Null(TableValueOdometer.ElementCount(dims, S(1, 1), S(1)));   // wrong tuple length
    }

    /// <summary>§13.18.63.3 SR23 — "the values of subscript-1 and subscript-2 corresponding to all levels higher
    /// than that of the OCCURS clause, if applicable, shall be equal": an unbounded OUTER dimension may be spanned
    /// (nothing is above it), an unbounded INNER one may not be carried out of, and with two nested unbounded
    /// dimensions the rule binds the inner one too — the case the COBOLNET1946 screen used to miss.</summary>
    [Fact]
    public void UnboundedDimension_CountsOnlyWhereSr23Holds()
    {
        TableValueDim[] outer = [Unbounded(), Fixed(3)];
        Assert.Equal(4, TableValueOdometer.ElementCount(outer, S(1, 2), S(2, 2)));
        Assert.Equal(3 * 999_999 + 1, TableValueOdometer.ElementCount(outer, S(1, 1), S(1_000_000, 1)));

        TableValueDim[] inner = [Fixed(2), Unbounded()];
        Assert.Equal(3, TableValueOdometer.ElementCount(inner, S(1, 1), S(1, 3)));
        Assert.Null(TableValueOdometer.ElementCount(inner, S(1, 1), S(2, 3)));

        TableValueDim[] nested = [Unbounded(), Unbounded()];
        Assert.Equal(3, TableValueOdometer.ElementCount(nested, S(2, 1), S(2, 3)));
        Assert.Null(TableValueOdometer.ElementCount(nested, S(1, 1), S(2, 1)));
        Assert.Equal(1, TableValueOdometer.UnboundedBelow(nested, 0));
        Assert.Null(TableValueOdometer.UnboundedBelow(nested, 1));
    }

    /// <summary>A count past <see cref="long.MaxValue"/> saturates there instead of wrapping to a small or
    /// negative number that would silently truncate the fill.</summary>
    [Fact]
    public void HugeCount_Saturates()
    {
        TableValueDim[] dims = [Fixed(int.MaxValue), Fixed(int.MaxValue), Fixed(int.MaxValue)];
        Assert.Equal(long.MaxValue,
            TableValueOdometer.ElementCount(dims, S(1, 1, 1), S(int.MaxValue, int.MaxValue, int.MaxValue)));
    }

    /// <summary>The fill keys exactly the counted elements, GR13-cycling the literals, and a phrase SR23 rejects
    /// keys nothing (it still reaches the fill when --permissive demotes the diagnostic).</summary>
    [Fact]
    public void Resolve_FillsExactlyTheCountedElements()
    {
        TableValueDim[] dims = [Fixed(2), Fixed(3)];
        var map = TableValueOdometer.Resolve(dims,
            [Spec(["A", "B", "C"], [1, 3], [2, 1])]);
        Assert.Equal(2, map.Count);
        Assert.Equal("A", map[S(1, 3)]);
        Assert.Equal("B", map[S(2, 1)]);

        TableValueDim[] inner = [Fixed(2), Unbounded()];
        Assert.Empty(TableValueOdometer.Resolve(inner,
            [Spec(["A"], [1, 1], [2, 3])]));
    }
}
