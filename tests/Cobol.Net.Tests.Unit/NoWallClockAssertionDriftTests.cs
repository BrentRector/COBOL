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
/// </summary>
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

    [Fact]
    public void ThePattern_CatchesTheShapesItForbids()
    {
        Assert.Matches(ElapsedComparison, "Assert.True(sw.Elapsed.TotalSeconds < 20, \"\");");
        Assert.Matches(ElapsedComparison, "Assert.True(sw.ElapsedMilliseconds < 500);");
        Assert.DoesNotMatch(ElapsedComparison, "output.WriteLine($\"zh built in {sw.ElapsedMilliseconds} ms\");");
    }
}
