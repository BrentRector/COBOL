// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE LANDING PIPELINE IS JUDGED BY THE SAME INSTRUMENT THAT FOUND ITS PROBLEM, AND THAT INSTRUMENT IS RECORDED RIGHT
/// (kb/Work PB3028; DESIGN-test-build-ci.md §3.17.5): <c>scripts/orchestrator/landing_measure.py</c> keeps one record
/// per pull request (DONE → review clean → enqueued → on main, its bounces and their causes, the merge-group CI wall
/// and the local gate's seconds), completes it from GitHub, and sets the after column beside the retired train records.
/// It replaced <c>train_measure.py</c>, which is deleted: no tracked file outside the narrative paths may name it, and
/// <c>enqueue.py queue</c> must keep writing the record.
/// </summary>
/// <remarks>
/// The script's own <c>--self-test</c> runs in every gate and in CI through <c>scripts/self_tests.py</c>
/// (<see cref="SelfTestDiscoveryDriftTests"/> holds the discovery), so this class does not run it a second time under
/// the harness's fixed per-process budget; it holds the arms by name, so an arm deleted from the self-test (which would
/// still exit 0) is a red here. The summary decides whether the redesign paid, so a miscounted figure is a wrong
/// decision: each figure's arm is named below. This test measures nothing and times nothing.
/// </remarks>
public sealed class LandingMeasureDriftTests
{
    [Fact]
    public void TheLandingMeasurement_DeclaresEveryArm()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "landing_measure.py"), new[]
        {
            "a record is a patch: later calls fill fields, the first enqueue time stays, and every stamp must follow the last",
            "an inconsistent record is refused: stamps out of order, an unknown cause, causes with no bounces, a bad number",
            "done is each carried change's earliest DONE report by its HEAD commit's time, never a report file's time: a "
                + "finisher keeps its predecessor's, a train's changes come from its Report lines, nothing after the queue or "
                + "already on main counts",
            "local gate seconds are the sum of every verdict.json in the worktree",
            "complete reads the merge time, counts only failed merge-group runs as bounces and takes the CI wall of the "
                + "successful run",
            "summary sets the after column beside the before column, names unattributed bounces and says UNMEASURED for a "
                + "figure it never saw",
            "the headline is the merge queue's rate from two landings and the retired trains' rate before that, each "
                + "saying what its minutes measure",
            "freeze waits for a landing, refuses to rewrite the frozen copy, and the evidence figures are in the evidence file",
        });
    }

    /// <summary>The retired writer is gone and nothing calls it: a second writer of the old file would split the
    /// instrument. The narrative paths (docs, the work register, the history, tests) may name it.</summary>
    [Fact]
    public void TheRetiredTrainWriter_IsDeleted_AndNothingNamesIt()
    {
        Assert.False(File.Exists(TestRepo.Scripts("orchestrator", "train_measure.py")),
            "scripts/orchestrator/train_measure.py was replaced by landing_measure.py (kb/Work PB3028): delete it, never keep a second writer");
        string[] narrative = ["docs/", "kb/", "adjudication/", "specs/", "tests/", "DEVLOG/"];
        const string TheReplacement = "scripts/orchestrator/landing_measure.py";   // names the script it replaced, once, in its docstring
        IReadOnlyList<string> tracked = TrackedTree.Files();
        Assert.True(tracked.Count >= 1000, $"`git ls-files` reported only {tracked.Count} paths — the sweep would scan nothing");
        var offenders = tracked.Where(p => p != TheReplacement && !narrative.Any(n => p.StartsWith(n, StringComparison.Ordinal)))
                               .Where(p => File.Exists(Path.Combine(TestRepo.Root, p)))
                               .Where(p => File.ReadAllText(Path.Combine(TestRepo.Root, p)).Contains("train_measure", StringComparison.Ordinal))
                               .ToList();
        Assert.True(offenders.Count == 0,
            "these files still name the retired `train_measure.py` (kb/Work PB3028): point them at `landing_measure.py`:\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>The queue path records the landing itself, so no brief has to remember to.</summary>
    [Fact]
    public void TheQueueStep_WritesTheLandingRecord()
    {
        string source = File.ReadAllText(TestRepo.Scripts("orchestrator", "enqueue.py"));
        Assert.Contains("measure=lambda branch, pr: measure_landing(cwd, branch, pr, a.changes)", source);
        Assert.Contains("landing_measure.record_enqueued(", source);
    }
}
