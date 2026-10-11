// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1590: no test may assert on a stopwatch reading. A fixed wall-clock ceiling measures the host, not the
/// code — hosted CI runners are shared and loaded, and `DeepNestingTests` went red TWICE on a 1-second compile that
/// took 26 s there. Assert the property instead: a work count (<c>PhysicalModel.ListBuilds</c>), an observed
/// suspension (<c>CobolTiming.SuspensionObserver</c>), a growth ratio, or completion. Reporting an elapsed time with
/// <c>output.WriteLine</c> is fine; comparing it is not. ⛔ A GROWTH RATIO of two stopwatch readings is NOT a safe
/// shape either (kb/Work PB1600): the two readings are taken at different moments, load lands on one and not the
/// other, and <c>PictureCompositionTests</c>' 3 000-vs-30 000-symbol ratio read 36x against a bound of 30 on untouched
/// code — it now counts the Table-10 walk's role tests (<c>PictureComposition.WalkObserver</c>) instead.
/// ⛔ NOR MAY A TEST SET A LITERAL TIME LIMIT (kb/Work PB3022, the owner's rule of 2026-09-25): a bounded
/// <c>WaitForExit(n)</c>, a regex match timeout, a <c>CancelAfter</c>/<c>Wait</c>/<c>Join</c>/<c>Delay</c> of a literal,
/// a <c>timeoutMs:</c> literal or a literal budget handed to the process observer. Each one measured the host: a fixed
/// limit was the top recurring load red of the gate. A regex needs no timeout when it runs
/// <c>RegexOptions.NonBacktracking</c> (linear time); a child process is bounded by the ONE harness budget
/// (<c>ProcessObserver.TimeoutMs</c>, whose run past it is a named non-observation, never a value), or by its own exit.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class NoWallClockAssertionDriftTests
{
    // An elapsed-time reading on one side of a < / <= / > / >= comparison.
    private static readonly Regex ElapsedComparison = new(
        @"(Elapsed(Milliseconds|Ticks)?|Elapsed\.Total\w+|TotalMilliseconds|TotalSeconds)\s*[<>]=?|[<>]=?\s*\w*\.Elapsed",
        RegexOptions.Compiled);

    [Fact]
    public void NoTestComparesAWallClockReading()
    {
        var root = TestRepo.Root;
        var offenders = Directory.EnumerateFiles(TestRepo.Tests(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.EndsWith(nameof(NoWallClockAssertionDriftTests) + ".cs"))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, i, line)))
            .Where(x => ElapsedComparison.IsMatch(x.line))
            .Select(x => $"{Path.GetRelativePath(root, x.f)}:{x.i + 1}: {x.line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "tests compare a wall-clock reading (kb/Work PB1590 — assert a work count, an observed effect or completion):\n"
            + string.Join("\n", offenders));
    }

    // A literal time limit, in each shape a test has used one.
    private static readonly Regex LiteralLimit = new(
        @"WaitForExit\(\s*\d|CancelAfter\(\s*(?:\d|TimeSpan\.From)|RegexOptions[^;]*,\s*TimeSpan\.From"
        + @"|\btimeout(?:Ms)?\s*:\s*\d|ProcessObserver\.Observe\([^;]*,\s*\d[\d_]*\s*\)"
        + @"|new CancellationTokenSource\(\s*(?:\d|TimeSpan\.From)|\.Wait\(\s*(?:\d|TimeSpan\.From)"
        + @"|WaitOne\(\s*(?:\d|TimeSpan\.From)|\.Join\(\s*(?:\d|TimeSpan\.From)|Task\.Delay\(\s*(?:\d|TimeSpan\.From)",
        RegexOptions.NonBacktracking);

    // The harness's one budget, and the test of the observer itself (a never-ending child times out by construction).
    private static readonly string[] LimitExempt =
        ["ProcessObservation.cs", "ProcessObservationDriftTests.cs", nameof(NoWallClockAssertionDriftTests) + ".cs"];

    [Fact]
    public void NoTestSetsALiteralTimeLimit()
    {
        var offenders = Directory.EnumerateFiles(TestRepo.Tests(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !LimitExempt.Contains(Path.GetFileName(f)))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, i, line)))
            .Where(x => LiteralLimit.IsMatch(x.line))
            .Select(x => $"{Path.GetRelativePath(TestRepo.Root, x.f)}:{x.i + 1}: {x.line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "tests set a literal time limit (kb/Work PB3022 — a deterministic witness, RegexOptions.NonBacktracking, or "
            + "the harness's one budget ProcessObserver.TimeoutMs):\n" + string.Join("\n", offenders));
        Assert.Matches(LiteralLimit, "Assert.True(p.WaitForExit(180_000), \"timed out\");");
        Assert.Matches(LiteralLimit, "new Regex(\"x\", RegexOptions.None, TimeSpan.FromSeconds(5));");
        Assert.Matches(LiteralLimit, "ProcessObserver.Observe(psi, null, 30_000)");
        Assert.DoesNotMatch(LiteralLimit, "ProcessObserver.Observe(psi, null, ProcessObserver.TimeoutMs)");
        Assert.DoesNotMatch(LiteralLimit, "p.WaitForExit();");
    }

    [Fact]
    public void ThePattern_CatchesTheShapesItForbids()
    {
        Assert.Matches(ElapsedComparison, "Assert.True(sw.Elapsed.TotalSeconds < 20, \"\");");
        Assert.Matches(ElapsedComparison, "Assert.True(sw.ElapsedMilliseconds < 500);");
        Assert.DoesNotMatch(ElapsedComparison, "output.WriteLine($\"zh built in {sw.ElapsedMilliseconds} ms\");");
    }
}
