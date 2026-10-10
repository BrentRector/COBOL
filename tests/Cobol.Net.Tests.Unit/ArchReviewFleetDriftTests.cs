// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE R2 REVIEW FLEET'S SUBSYSTEMS PARTITION THE CODE, AND ITS TOOLS CAN STILL FAIL (kb/Work PB2558–PB2561;
/// docs/rearchitecture/DESIGN-architecture-review.md §3 R2): <c>scripts/arch/r2_subsystems.py --check</c> finds every
/// reviewed file of the committed tree in exactly one subsystem (no hole, no overlap) and the design's subsystem block
/// equal to the table it is rendered from; and the fleet's instruments — <c>r2_subsystems.py</c>, <c>r2_inputs.py</c>,
/// <c>r2_collect.py</c>, <c>r2_cost.py</c> (kb/Work PB2707: a batch and a launch sized against the quota left),
/// <c>file_census_notes.py</c> (its R2 filing path) and the workflow's dry run <c>test_wf_r2_review.mjs</c> (which runs
/// the launch.js <c>r2_collect.py --launch</c> writes, as the operator launches it) — each pass a self-test that drives
/// every arm.
/// </summary>
/// <remarks>
/// The fleet's first brief named its subsystems in prose: one file sat in two of them, about 6,000 lines of the runtime
/// in none, and nothing could tell. A table is only as good as its coverage on the tree as it is TODAY, so a new folder no
/// subsystem owns is red here until the table owns it (CLAUDE.md rule 5: the next case automatic, and a drift test
/// keeping it so).
/// </remarks>
public sealed class ArchReviewFleetDriftTests
{
    [Fact]
    public void TheSubsystemTable_PartitionsTheCommittedTree_AndTheDesignBlockIsCurrent()
    {
        var r = Python("r2_subsystems.py", "--check");
        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== R2 SUBSYSTEMS: PASS", StringComparison.Ordinal),
            $"`r2_subsystems.py --check` failed — a file in no subsystem or in two, or a stale design block "
            + $"(run `--write-design`):\n{r.Stdout}{r.Stderr}");
    }

    [Theory]
    [InlineData("r2_subsystems.py", "=== R2 SUBSYSTEMS SELF-TEST: PASS ===")]
    [InlineData("r2_inputs.py", "=== R2 INPUTS SELF-TEST: PASS ===")]
    [InlineData("r2_collect.py", "=== R2 COLLECT SELF-TEST: PASS ===")]
    [InlineData("r2_cost.py", "=== R2 COST SELF-TEST: PASS ===")]
    [InlineData("file_census_notes.py", "self-test R2 OK")]
    public void EachFleetInstrument_PassesItsSelfTest(string script, string verdict)
    {
        var r = Python(script, "--self-test");
        Assert.True(r.ExitCode == 0 && r.Stdout.Contains(verdict, StringComparison.Ordinal),
            $"`{script} --self-test` failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
        Assert.DoesNotContain("FAIL  ", r.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void TheWorkflow_DryRunDrivesEveryArm()
    {
        string script = TestRepo.Scripts("arch", "test_wf_r2_review.mjs");
        Assert.True(File.Exists(script), $"the workflow's dry run is missing: {script}");
        var psi = new ProcessStartInfo("node") { WorkingDirectory = TestRepo.Root };
        psi.ArgumentList.Add(script);
        var r = ProcessObserver.ObserveOrThrow(psi);
        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== WF R2 REVIEW DRY RUN: PASS ===", StringComparison.Ordinal),
            $"the R2 workflow's dry run failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
        foreach (string arm in new[]
                 {
                     "the unread remainder gets exactly one finisher, for exactly those files",
                     "a pair that found nothing is examined, and only it",
                     "skeptics: chunks of four, three lenses each",
                     "after an agent reports a stop, no new agent starts",
                     // the w1034 claim refuter's C2: the launch state is the pair's, from disk
                     "resume after a stop: every file was read exactly once across all launches",
                     "resume after a stop: no finding is decided twice under a lens",
                     "resume after a stop: every finding — the finisher's included — is verified",
                     "a decided pair starts no agent on a relaunch",
                     "a silent finder whose files another finder saw read still has its findings verified",
                     // PB2707: the launch is the file r2_collect.py writes from disk; nothing is transcribed
                     "the launch is launch.js, which r2_collect.py --launch wrote from disk, run with no args",
                     "a relaunch with N decided pairs launches O(undecided) bytes and no agent for them",
                     "the template run with no plan (no args) is refused, naming launch.js",
                     "the old launch-args shape (a state, no plan) is refused",
                 })
        {
            Assert.True(r.Stdout.Contains("PASS  " + arm, StringComparison.Ordinal),
                $"the dry run no longer drives '{arm}' — an arm never seen to fire is not evidence.\n{r.Stdout}");
        }
    }

    private static ProcessObservation Python(string script, string arg)
    {
        string path = TestRepo.Scripts("arch", script);
        Assert.True(File.Exists(path), $"the R2 fleet instrument is missing: {path}");
        return PythonInstrument.Run(path, arg);
    }
}
