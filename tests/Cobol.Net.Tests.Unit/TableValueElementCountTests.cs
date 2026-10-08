// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A Format 2 (table) VALUE phrase's ELEMENT COUNT and its §13.18.63.4 GR12 odometer walk must agree
/// (kb/Work PB1716). The count used to be absent, and a 64,000,000-element cap was the fill's only bound, so a
/// phrase that violated §13.18.63.3 SR23 ran to the cap before being rejected (about 30 s for a 26-line program).
/// The count is a mixed-radix distance and the walk (<see cref="TableValueOdometer.Step"/>) is a carry chain: two
/// statements of one order, so this class proves them equal over every tuple pair of a small table, and pins the
/// three ways a phrase names no run (a subscript out of range, TO before FROM, and unequal subscripts above a
/// dimension with no ceiling).
/// <para>Since kb/Work PB1722 the plan is its PHRASES (<see cref="TableValueOdometer.Resolve"/>), never an element
/// map, and the step walk is this class's ORACLE: the element-by-element fill is rebuilt here from it, and
/// <see cref="TableValuePlan.LiteralAt"/>, <see cref="TableValuePlan.RankForm"/> and <see cref="TableValueRuns.Of"/>
/// are each checked against it over every element of a 3 x 4 x 2 table.</para>
/// </summary>
public sealed class TableValueElementCountTests
{
    private static TableValueDim Fixed(int max) => new(Item(), max, Dynamic: false);

    private static TableValueDim Unbounded() => new(Item(), null, Dynamic: true);

    private static DataItem Item() => new() { Level = 5, CobolName = "T", CsName = "T" };

    private static Subscripts S(params int[] v) => new(v);

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

    /// <summary>The plan keys exactly the counted elements, GR13-cycling the literals, and a phrase SR23 rejects
    /// keys nothing (it still reaches the resolution when --permissive demotes the diagnostic).</summary>
    [Fact]
    public void Resolve_KeysExactlyTheCountedElements()
    {
        TableValueDim[] dims = [Fixed(2), Fixed(3)];
        var plan = Plan(dims, Spec(["A", "B", "C"], [1, 3], [2, 1]));
        Assert.Equal(2, plan.Phrases.Single().Count);
        Assert.Equal("A", plan.LiteralAt(S(1, 3)));
        Assert.Equal("B", plan.LiteralAt(S(2, 1)));
        Assert.Null(plan.LiteralAt(S(1, 2)));
        Assert.Null(plan.LiteralAt(S(2, 2)));

        TableValueDim[] inner = [Fixed(2), Unbounded()];
        Assert.Empty(TableValueOdometer.Resolve(inner, [Spec(["A"], [1, 1], [2, 3])]));
    }

    // ── kb/Work PB1722: the plan is its PHRASES, and every reader is checked against the element-by-element ──
    //    odometer fill it replaced (built here from Step, the GR12 carry chain, as the oracle).

    private static readonly TableValueDim[] Cube = [Fixed(3), Fixed(4), Fixed(2)];

    /// <summary>Phrase sets over <see cref="Cube"/>: whole-table, cyclic lists whose length does and does not
    /// divide a level's subtree size, FROM and TO landing inside an occurrence, and overlaps that GR15 settles.</summary>
    public static TheoryData<string> PhraseSets() => ["whole", "cyclic3", "partial", "overlap", "single"];

    private static TableValueSpec[] SpecsOf(string set) => set switch
    {
        "whole" => [Spec(["A"], [1, 1, 1], null)],
        "cyclic3" => [Spec(["A", "B", "C"], [1, 1, 1], null)],
        "partial" => [Spec(["A", "B"], [1, 2, 2], [3, 1, 1])],
        "overlap" => [Spec(["A", "B", "C"], [1, 1, 1], null), Spec(["X", "Y"], [2, 1, 2], [2, 4, 1]),
                      Spec(["Z"], [3, 3, 1], [3, 3, 2])],
        "single" => [Spec(["Q"], [2, 3, 1], [2, 3, 1])],
        _ => throw new ArgumentOutOfRangeException(nameof(set)),
    };

    private static TableValueSpec Spec(string[] literals, int[] from, int[]? to) =>
        new(literals, from, to, Ordinal: 0, [.. from.Select(n => n.ToString())], to?.Select(n => n.ToString()).ToArray());

    private static TableValuePlan Plan(IReadOnlyList<TableValueDim> dims, params TableValueSpec[] specs) =>
        new() { Dims = dims, Phrases = TableValueOdometer.Resolve(dims, [.. specs.Select((s, i) => s with { Ordinal = i })]) };

    /// <summary>The element-by-element GR12–GR15 fill, walked with <see cref="TableValueOdometer.Step"/>.</summary>
    private static Dictionary<Subscripts, string> OracleFill(IReadOnlyList<TableValueDim> dims, TableValueSpec[] specs)
    {
        var map = new Dictionary<Subscripts, string>();
        foreach (var spec in specs)
        {
            var to = spec.To is { } t ? S([.. t]) : TableValueOdometer.DefaultTo(dims)!.Value;
            long count = TableValueOdometer.ElementCount(dims, S([.. spec.From]), to)!.Value;
            var cur = spec.From.ToArray();
            for (long k = 0; k < count; k++)
            {
                map[S([.. cur])] = spec.Literals[(int)(k % spec.Literals.Count)];
                TableValueOdometer.Step(cur, dims);
            }
        }
        return map;
    }

    private static List<Subscripts> AllTuples(IReadOnlyList<TableValueDim> dims)
    {
        var all = new List<Subscripts>();
        var cur = Enumerable.Repeat(1, dims.Count).ToArray();
        do all.Add(S([.. cur])); while (TableValueOdometer.Step(cur, dims));
        return all;
    }

    /// <summary><see cref="TableValuePlan.LiteralAt"/> answers every element exactly as the odometer fill does
    /// (GR12's order, GR13's cycle, GR14's default TO, GR15's last-phrase-wins).</summary>
    [Theory, MemberData(nameof(PhraseSets))]
    public void LiteralAt_AgreesWithTheOdometerFill(string set)
    {
        var specs = SpecsOf(set);
        var plan = Plan(Cube, specs);
        var oracle = OracleFill(Cube, [.. specs.Select((s, i) => s with { Ordinal = i })]);
        foreach (var t in AllTuples(Cube))
            Assert.Equal(oracle.GetValueOrDefault(t), plan.LiteralAt(t));
    }

    /// <summary><see cref="TableValuePlan.RankForm"/> places every element: in a phrase's run exactly when its
    /// leading subscripts match and its rank lies in Lo..Hi, at literal (rank − Lo) mod k.</summary>
    [Theory, MemberData(nameof(PhraseSets))]
    public void RankForm_PlacesEveryElementOfEveryPhrase(string set)
    {
        var plan = Plan(Cube, SpecsOf(set));
        foreach (var p in plan.Phrases)
        {
            var (split, weights, lo, hi) = plan.RankForm(p);
            foreach (var t in AllTuples(Cube))
            {
                bool inRun = Subscripts.Compare(t, p.From) >= 0 && Subscripts.Compare(t, p.To) <= 0;
                bool prefix = Enumerable.Range(0, split).All(i => t[i] == p.From[i]);
                long rank = Enumerable.Range(split, Cube.Length - split).Sum(i => (t[i] - 1) * weights[i - split]);
                Assert.Equal(inRun, prefix && rank >= lo && rank <= hi);
                if (inRun)
                    Assert.Equal(p.Literals[(int)((TableValueOdometer.ElementCount(Cube, p.From, t)!.Value - 1) % p.Literals.Count)],
                        p.Literals[(int)((rank - lo) % p.Literals.Count)]);
            }
        }
    }

    /// <summary>⛔ <see cref="TableValueRuns.Of"/> is sound: at every level under every prefix the runs tile
    /// 1..n, and an occurrence's whole subtree initializes exactly as the one a period earlier does — for two
    /// plans in the subtree at once (the leaf T and a sibling leaf U with a different cycle).</summary>
    [Theory, MemberData(nameof(PhraseSets))]
    public void Runs_RepeatEveryOccurrencesSubtree(string set)
    {
        var g = Item(); var h = Item(); var t = Item(); var u = Item();
        g.AddMember(h); h.AddMember(t); h.AddMember(u);
        t.TableValuePlan = Plan(Cube, SpecsOf(set));
        u.TableValuePlan = Plan(Cube, Spec(["1", "2"], [1, 2, 1], [3, 2, 2]));
        g.ContainsTableValue = h.ContainsTableValue = t.ContainsTableValue = u.ContainsTableValue = true;
        DataItem[] levels = [g, h, t];

        // The subtree of an occurrence of T holds only T's elements; of G or H, both leaves'.
        string Signature(Subscripts prefix) => string.Join("|", AllTuples(Cube)
            .Where(x => Enumerable.Range(0, prefix.Count).All(i => x[i] == prefix[i]))
            .Select(x => prefix.Count == Cube.Length
                ? t.TableValuePlan.LiteralAt(x)
                : $"{t.TableValuePlan.LiteralAt(x)},{u.TableValuePlan.LiteralAt(x)}"));

        for (int level = 0; level < Cube.Length; level++)
            foreach (var prefix in AllTuples(Cube).Select(x => S([.. Enumerable.Range(0, level).Select(i => x[i])])).Distinct())
            {
                int n = Cube[level].Max!.Value;
                var runs = TableValueRuns.Of(levels[level], prefix, n);
                Assert.Equal(1, runs[0].First);
                Assert.Equal(n, runs[^1].Last);
                for (int r = 0; r < runs.Count; r++)
                {
                    if (r > 0) Assert.Equal(runs[r - 1].Last + 1, runs[r].First);
                    var run = runs[r];
                    Assert.InRange(run.Period, 1, run.Length);
                    for (int o = run.First; o <= run.Last; o++)
                        Assert.Equal(Signature(prefix.With(run.First + (o - run.First) % run.Period)), Signature(prefix.With(o)));
                }
            }
    }

    /// <summary>The runs stay O(phrases) at any size: the filed shape (`03 G OCCURS 10000. 05 T OCCURS 10000
    /// VALUE "A" FROM (1 1)`) is ONE uniform run at each level, and a three-literal cycle over a 10000-element
    /// level whose subtree is 10000 elements long repeats every three occurrences.</summary>
    [Fact]
    public void Runs_OfTheFiledTable_AreOneRunPerLevel()
    {
        TableValueDim[] dims = [Fixed(10_000), Fixed(10_000)];
        var g = Item(); var t = Item();
        g.AddMember(t);
        g.ContainsTableValue = t.ContainsTableValue = true;
        t.TableValuePlan = Plan(dims, Spec(["A"], [1, 1], null));
        Assert.Equal([new OccurrenceRun(1, 10_000, 1)], TableValueRuns.Of(g, default, 10_000));
        Assert.Equal([new OccurrenceRun(1, 10_000, 1)], TableValueRuns.Of(t, S(7), 10_000));

        t.TableValuePlan = Plan(dims, Spec(["A", "B", "C"], [1, 1], null));
        Assert.Equal([new OccurrenceRun(1, 10_000, 3)], TableValueRuns.Of(g, default, 10_000));
        Assert.Equal([new OccurrenceRun(1, 10_000, 3)], TableValueRuns.Of(t, S(7), 10_000));
    }
}
