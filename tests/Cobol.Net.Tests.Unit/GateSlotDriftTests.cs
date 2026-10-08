// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GATE CAP IS FIFO AND NEVER LEAKS A SLOT (kb/Work PB1720; DESIGN-test-build-ci.md §3.14.6):
/// <c>scripts/gate_slot.py --self-test</c> drives all five arms — FIFO order (a later waiter never overtakes a live
/// earlier ticket, and a re-gate queues last), a killed holder releases its slot, a dead waiter leaves the queue, an
/// orphaned tree is killed on Windows or keeps its slot until it exits on Linux, and the slots are shared across
/// worktrees — and every arm passes on the host running this test. ⛔ AND THE CAP IS ONE SHARED SETTING (kb/Work
/// PB2514): a raise written by <c>gate_slot.py set-cap</c> in one worktree reaches gates already queued in another at
/// once (no head-of-line block), an expired setting is the default again, malformed settings stop a gate, and
/// <c>status</c> shows every held slot, one above the cap in force included.
/// </summary>
/// <remarks>
/// <para>
/// The cap bounds how many implementer gates build and test at once, repository-wide, so the lander's gate is not
/// starved (train 48's lander leg took 30.6 min against 9.6 quiet). Each property is one arm of the self-test, and
/// each arm was seen to go RED on a planted defect when the tool landed: no ticket check (served out of order), a
/// ticket file counted live whether or not it was locked (the queue never drained), no Job object (the orphaned tree
/// survived its holder), the slot descriptor withheld from children on Linux (the orphans ran outside the cap), and
/// the per-worktree git dir in place of the common dir (two worktrees, two caps).
/// </para>
/// <para>
/// Running it here puts it in every Unit run: the implementer and lander gates on Windows and the CI unit jobs on
/// Linux, so each operating system's arm of the process-tree rule is proven where it runs. An arm deleted from the
/// self-test is red here by name — an arm never seen to run is not evidence.
/// </para>
/// </remarks>
public sealed class GateSlotDriftTests
{
    [Fact]
    public void SelfTest_DrivesAndPassesEveryArm()
    {
        string script = TestRepo.Scripts("gate_slot.py");
        Assert.True(File.Exists(script), $"the gate cap is missing: {script}");
        var r = PythonInstrument.Run(script, "--self-test");
        Assert.True(r.ExitCode == 0, $"`gate_slot.py --self-test` is RED:\n{r.Stdout}{r.Stderr}");
        Assert.Contains("ALL GREEN — 8 arms", r.Stdout, StringComparison.Ordinal);
        foreach (string arm in new[]
                 {
                     "PASS  FIFO order: a later waiter never overtakes a live earlier ticket, a re-gate queues last",
                     "PASS  a killed holder releases its slot",
                     "PASS  a dead waiter leaves the queue",
                     "PASS  an orphaned tree is killed (Windows) or keeps its slot until it exits (Linux)",
                     "PASS  the slots are shared across worktrees",
                     "PASS  one shared cap: a raise from another worktree reaches the queued gates at once, no head-of-line block",
                     "PASS  an expired setting is the default again; a malformed file stops the gate; bad values are refused",
                     "PASS  status shows every held slot, one above the cap in force included",
                 })
        {
            Assert.True(r.Stdout.Contains(arm, StringComparison.Ordinal),
                $"`gate_slot.py --self-test` no longer passes '{arm}'.\n{r.Stdout}{r.Stderr}");
        }
    }
}
