// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.CodeGen;

/// <summary>⛔ THE ONE EMIT OF A TABLE'S INITIAL CONTENT FROM ITS OCCURRENCE RUNS (kb/Work PB1722). Every lane that
/// writes the initial state of a table — the record-struct arrays (<see cref="ValueInitializer.FieldInit"/>), the
/// character-image and bit-carrier seeds (<see cref="GroupImageCodec"/>), the dynamic-capacity seed switch
/// (<see cref="ValueInitializer.DynTableNew"/>) and the group-VALUE slicer (<see cref="GroupValueSlicer"/>) — composes
/// ONE element per run period and emits the run, never one element per occurrence. The runs come from
/// <see cref="TableValueRuns.Of"/> (or, for the slicer, <see cref="RunsOfRepeats"/>), so the emitted text is
/// proportional to the table's DESCRIPTION, not to its element count: a `10000 x 10000` table used to compile to an
/// array literal of one hundred million element initializers.</summary>
internal static class OccurrenceRunEmit
{
    /// <summary>Every occurrence 1..<paramref name="n"/> in one uniform run — a table whose occurrences all
    /// initialize alike (§13.18.63.4 GR9).</summary>
    public static IReadOnlyList<OccurrenceRun> Uniform(int n) => n > 0 ? [new OccurrenceRun(1, n, 1)] : [];

    /// <summary>The runs of occurrences 1..<paramref name="n"/> whose content REPEATS, where
    /// <paramref name="same"/>(a, b) compares occurrences a and b — the slicer's runs, whose occurrences are windows
    /// of one compile-time area (<see cref="GroupArea"/>) and are compared directly rather than derived from a table
    /// VALUE. Each run is grown from its first occurrence under period 1 and under <paramref name="hint"/> (the
    /// period the area's repeated unit gives this element width, <see cref="GroupArea.PeriodFor"/>), and the longer
    /// one is kept, so a figurative fill is one run and an ALL literal one periodic run.</summary>
    public static IReadOnlyList<OccurrenceRun> RunsOfRepeats(int n, int hint, Func<int, int, bool> same)
    {
        var runs = new List<OccurrenceRun>();
        int start = 1;
        while (start <= n)
        {
            int bestPeriod = 1, bestLast = start;
            foreach (int p in hint > 1 ? new[] { 1, hint } : [1])
            {
                int o = start + p;
                while (o <= n && same(o - p, o)) o++;
                int last = Math.Min(o - 1, n);
                if (last > bestLast) (bestPeriod, bestLast) = (p, last);
            }
            runs.Add(new OccurrenceRun(start, bestLast, Math.Min(bestPeriod, bestLast - start + 1)));
            start = bestLast + 1;
        }
        return runs;
    }

    /// <summary>A FIXED table's initial elements as a run-time-built array: <c>CobolTable.Fill&lt;T&gt;(n, (int
    /// __oD) =&gt; …)</c>, the lambda selecting the run and, inside a periodic run, the element by its phase.
    /// <paramref name="depth"/> (the number of OCCURS levels above the table) names the parameter, so a nested
    /// table's lambda never shadows its enclosing one.</summary>
    public static string Array(string elementType, int n, IReadOnlyList<OccurrenceRun> runs, Func<int, string> elementAt,
        int depth)
    {
        string param = $"__o{depth}";
        var arms = Arms(runs, elementAt, param);
        string body = arms.Count switch
        {
            0 => "default!",
            1 => arms[0].Expr,
            _ => $"{param} switch {{ {string.Concat(arms.Take(arms.Count - 1).Select(a => $"<= {a.Last} => {a.Expr}, "))}_ => {arms[^1].Expr} }}",
        };
        return RuntimeApi.TableFill(elementType, n, param, body);
    }

    /// <summary>A DYNAMIC-capacity table's per-occurrence seed, <c>(int __i) =&gt; …</c> over occurrences
    /// 1..cap (the runs), and <paramref name="beyond"/> for every other occurrence number — the element as it
    /// stands with no table VALUE keyed to it. A run that composes exactly <paramref name="beyond"/> needs no arm.</summary>
    public static string Switch(IReadOnlyList<OccurrenceRun> runs, Func<int, string> elementAt, string beyond)
    {
        const string param = "__i";
        var arms = Arms(runs, elementAt, param).Where(a => !(a.Uniform && a.Expr == beyond)).ToList();
        return arms.Count == 0
            ? $"(int {param}) => {beyond}"
            : $"(int {param}) => {param} switch {{ {string.Concat(arms.Select(a => $">= {a.First} and <= {a.Last} => {a.Expr}, "))}_ => {beyond} }}";
    }

    /// <summary>A table's initial IMAGE (a string expression): each run's period of element images concatenated
    /// once and repeated (<c>CobolString.Repeat</c>), the remainder of a periodic run appended.</summary>
    public static string Image(IReadOnlyList<OccurrenceRun> runs, Func<int, string> elementAt)
    {
        var parts = new List<string>();
        foreach (var run in Merged(runs, elementAt))
        {
            int p = run.Texts.Count;
            string unit = p == 1 ? run.Texts[0] : "(" + string.Join(" + ", run.Texts) + ")";
            int reps = run.Length / p, rem = run.Length % p;
            if (reps > 0) parts.Add(reps == 1 ? unit : RuntimeApi.StrRepeat(unit, $"{reps}"));
            if (rem > 0) parts.Add(string.Join(" + ", run.Texts.Take(rem)));
        }
        return parts.Count switch
        {
            0 => "\"\"",
            1 => parts[0],
            _ => "(" + string.Join(" + ", parts) + ")",
        };
    }

    private readonly record struct Arm(int First, int Last, bool Uniform, string Expr);

    /// <summary>One expression per (merged) run: its one element, or — for a periodic run — a switch on the
    /// occurrence's phase within the run.</summary>
    private static List<Arm> Arms(IReadOnlyList<OccurrenceRun> runs, Func<int, string> elementAt, string param)
    {
        var arms = new List<Arm>();
        foreach (var run in Merged(runs, elementAt))
        {
            int p = run.Texts.Count;
            string expr = p == 1
                ? run.Texts[0]
                : $"(({param} - {run.First}) % {p}) switch {{ "
                  + string.Concat(run.Texts.Take(p - 1).Select((t, i) => $"{i} => {t}, ")) + $"_ => {run.Texts[^1]} }}";
            arms.Add(new Arm(run.First, run.Last, p == 1, expr));
        }
        return arms;
    }

    private sealed record ComposedRun(int First, int Last, IReadOnlyList<string> Texts)
    {
        public int Length => Last - First + 1;
    }

    /// <summary>Each run with its period's element texts composed ONCE, adjacent uniform runs of identical text
    /// merged (an occurrence range two phrases split for no visible difference is one run).</summary>
    private static List<ComposedRun> Merged(IReadOnlyList<OccurrenceRun> runs, Func<int, string> elementAt)
    {
        var merged = new List<ComposedRun>();
        foreach (var run in runs)
        {
            var texts = new string[run.Period];
            for (int i = 0; i < texts.Length; i++) texts[i] = elementAt(run.First + i);
            if (merged.Count > 0 && merged[^1] is { Texts.Count: 1 } prev && texts.Length == 1
                && prev.Last + 1 == run.First && string.Equals(prev.Texts[0], texts[0], StringComparison.Ordinal))
            {
                merged[^1] = prev with { Last = run.Last };
                continue;
            }
            merged.Add(new ComposedRun(run.First, run.Last, texts));
        }
        return merged;
    }
}
