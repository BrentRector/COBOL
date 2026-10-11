// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GATE CAP IS FIFO AND NEVER LEAKS A SLOT (kb/Work PB1720; DESIGN-test-build-ci.md §3.14.6):
/// <c>scripts/gate_slot.py --self-test</c> declares all ten arms — FIFO order (a later waiter never overtakes a live
/// earlier ticket, and a re-gate queues last), a killed holder releases its slot, a dead waiter leaves the queue, an
/// orphaned tree is killed on Windows or keeps its slot until it exits on Linux, the slots are shared across
/// worktrees, ⛔ ONE SHARED CAP (kb/Work PB2514: a raise written by <c>gate_slot.py set-cap</c> in one worktree reaches
/// gates already queued in another at once, an expired setting is the default again, malformed settings stop a gate,
/// <c>status</c> shows every held slot), ⛔ THE OWNER'S CODED DEFAULTS (2026-10-10, kb/Work PB2514, PB2515: with no
/// setting the cap is 3 and the implementer scope is <c>leg1</c>) and <c>gate_slot.py clear</c> returning to them —
/// and its ALL GREEN line counts exactly the arms it declares; the self-test runner runs it.
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
/// ⛔ THIS TEST DOES NOT RUN THE SELF-TEST (kb/Work PB2882). <c>scripts/self_tests.py</c> discovers it and runs it in
/// the lander's gate, the Linux gate and CI's Linux and Windows jobs, where any red arm makes its exit non-zero and a
/// hang is bounded by the runner's machine-wide budget place, not by a wall clock (<see cref="SelfTestDiscoveryDriftTests"/>
/// holds that every self-test is discovered and that every gate runs the runner). Until train 1055 this test ran it a
/// SECOND time inside the Unit leg under the test harness's fixed 120 s per-process budget: 9 s alone, 304-315 s in the
/// runner under the lander's whole-population gate, so the duplicate went RED as a harness non-observation in both of
/// that train's lander gates with every arm passing. A fixed time limit in a test is a defect; what is left here is the
/// part the runner cannot see — an arm deleted, renamed or left out of the count is red here by name.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class GateSlotDriftTests
{
    /// <summary>The arms, in the order the self-test runs them; each label is the first argument of an
    /// <c>arm(...)</c> call and the line the self-test prints as <c>PASS  &lt;label&gt;</c>.</summary>
    private static readonly string[] Arms =
    [
        "FIFO order: a later waiter never overtakes a live earlier ticket, a re-gate queues last",
        "a killed holder releases its slot",
        "a dead waiter leaves the queue",
        "an orphaned tree is killed (Windows) or keeps its slot until it exits (Linux)",
        "the slots are shared across worktrees",
        "one shared cap: a raise from another worktree reaches the queued gates at once, no head-of-line block",
        "an expired setting is the default again; a malformed file stops the gate; bad values are refused",
        "status shows every held slot, one above the cap in force included",
        "the coded defaults, with no setting: cap 3 (three gates at once, a fourth queues) and scope leg1",
        "clear returns one setting or both to the coded defaults, and repairs a malformed file",
    ];

    private static readonly Regex ArmCall = new(@"^\s*arm\(""(?<label>[^""]+)""", RegexOptions.Multiline);
    private static readonly Regex AllGreen = new(@"ALL GREEN — (?<n>\d+) arms");

    [Fact]
    public void SelfTest_DeclaresEveryArm_AndCountsThemAll()
    {
        string script = TestRepo.Scripts("gate_slot.py");
        Assert.True(File.Exists(script), $"the gate cap is missing: {script}");
        string source = File.ReadAllText(script);

        string[] declared = [.. ArmCall.Matches(source).Select(m => m.Groups["label"].Value)];
        Assert.True(Arms.SequenceEqual(declared, StringComparer.Ordinal),
            "`gate_slot.py --self-test` no longer declares exactly these arms, in this order.\n"
            + $"expected:\n  {string.Join("\n  ", Arms)}\ndeclared:\n  {string.Join("\n  ", declared)}");

        Match total = AllGreen.Match(source);
        Assert.True(total.Success, "`gate_slot.py --self-test` no longer prints its `ALL GREEN — <n> arms` line");
        Assert.Equal(declared.Length, int.Parse(total.Groups["n"].Value, System.Globalization.CultureInfo.InvariantCulture));
    }
}
