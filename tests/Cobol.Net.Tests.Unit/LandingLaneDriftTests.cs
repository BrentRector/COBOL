// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE LOOP'S LANDING LANE LANDS, BESIDE THE UNIT, AND THE ENQUEUE UNIT DOES NOT (kb/Work PB3103;
/// DESIGN-orchestrator-loop.md section 4.9; MANDATORY-PRACTICES O11): <c>scripts/orchestrator/orchestrate.ps1</c>
/// starts <c>land_lane.py tick</c> as a BACKGROUND process (never awaited on the unit's event loop) as each unit starts
/// and on a period while it runs, and step 3 of <c>scripts/orchestrator/units/enqueue.md</c> reads
/// <c>land_lane.py status</c> and runs no <c>enqueue.py land</c> of its own.
/// </summary>
/// <remarks>
/// <para>Before the lane a branch reviewed CLEAN landed only when the loop's NEXT enqueue unit started, after its whole
/// wave ended: wave 1069's group D was stamped CLEAN at 12:34 PDT and E and F by 13:05, and all three idled while G and
/// H ran; then no implementer ran while the enqueue unit gated and waited on the merge queue. The owner chose the
/// supervisor lane (2026-10-12, "Supervisor lane (Recommended)"). Two drifts would bring the wait back without a red
/// anywhere else: a supervisor that no longer starts the lane (branches wait for an enqueue unit next_unit.py no longer
/// starts for them), and an enqueue.md step 3 that lands again (two landing paths racing for one branch). A third would
/// stall the unit: a tick awaited on the event loop, where its 30-second survey would block the stream read, the startup
/// watchdog and the STOP check (kb/Work PB2590's defect, for the checkpoint).</para>
/// <para>The lane's own decisions (which exit is retried, which waits for the enqueue unit, the halt, the stops) are
/// <c>land_lane.py --self-test</c>'s, which the self-test runner discovers and runs in every gate and in CI.</para>
/// </remarks>
public sealed class LandingLaneDriftTests
{
    /// <summary>A command that lands a branch: <c>enqueue.py land</c>, however it is spelled out.</summary>
    private static readonly Regex LandCommand = new(@"enqueue\.py\s+land\b", RegexOptions.Compiled);

    /// <summary>Step 3 of an enqueue unit prompt: from its <c>3.</c> line to the next top-level step.</summary>
    internal static string Step3(string unitPrompt)
    {
        Match m = Regex.Match(unitPrompt, @"^3\. .*?(?=^\d+[a-z]?\. )", RegexOptions.Multiline | RegexOptions.Singleline);
        return m.Success ? m.Value : "";
    }

    /// <summary>The body of a PowerShell function: from its <c>function Name</c> line to the next top-level function.</summary>
    internal static string FunctionBody(string script, string name)
    {
        Match m = Regex.Match(script, @"^function " + Regex.Escape(name) + @"\b.*?(?=^function |\z)",
            RegexOptions.Multiline | RegexOptions.Singleline);
        return m.Success ? m.Value : "";
    }

    [Fact]
    public void TheSupervisor_StartsTheLane_InTheBackground_BesideEveryUnit()
    {
        string script = File.ReadAllText(TestRepo.Scripts("orchestrator", "orchestrate.ps1"));
        string start = FunctionBody(script, "Start-LaneTick");
        Assert.True(start.Length > 0, "orchestrate.ps1 has no `function Start-LaneTick`: the landing lane is not started (kb/Work PB3103)");
        Assert.Contains("'land_lane.py'", start, StringComparison.Ordinal);
        Assert.Contains("'tick'", start, StringComparison.Ordinal);
        Assert.True(start.Contains("[System.Diagnostics.Process]::Start(", StringComparison.Ordinal)
                    && !start.Contains("WaitForExit", StringComparison.Ordinal),
            "Start-LaneTick must start the tick as a background process and never wait for it: the event loop reads the "
            + "unit's stream, ticks the startup watchdog and checks STOP (kb/Work PB2590)");

        string unit = FunctionBody(script, "Invoke-Unit");
        Assert.True(unit.Length > 0, "orchestrate.ps1 has no `function Invoke-Unit`; re-point this test");
        int calls = Regex.Matches(unit, @"^\s*Start-LaneTick\b", RegexOptions.Multiline).Count;
        Assert.True(calls >= 2, $"Invoke-Unit starts the lane {calls} time(s): it must tick as the unit starts and on "
            + "-LandLaneSeconds while it runs");
        Assert.Matches(@"\[int\]\$LandLaneSeconds\s*=\s*[1-9]\d*", script);
    }

    [Fact]
    public void EnqueueUnitStep3_ReadsTheLane_AndLandsNothingItself()
    {
        string step3 = Step3(File.ReadAllText(TestRepo.Scripts("orchestrator", "units", "enqueue.md")));
        Assert.True(step3.Length > 0, "scripts/orchestrator/units/enqueue.md has no step 3; re-point this test");
        Assert.Contains("land_lane.py status", step3, StringComparison.Ordinal);
        Assert.DoesNotMatch(LandCommand, step3);
        // A stop resolved with no new commit leaves the head unchanged and the lane never relaunches it there by itself:
        // step 3 must name the unit's way to land it again (review R1-1), or that branch waits forever.
        Assert.Contains("land_lane.py rearm", step3, StringComparison.Ordinal);
    }

    /// <summary>⭐ The step-3 reader's failure branch, fired: a planted prompt whose step 3 lands is caught, and the
    /// later steps (the clerk's own <c>enqueue.py land --lane clerk</c>, step 5) are outside it.</summary>
    [Fact]
    public void TheStep3Reader_CatchesAPlantedLanding_AndStopsAtStep4()
    {
        const string planted = "UNIT: enqueue.\n1. Load.\n2. Review.\n3. LAND. For each, `python scripts/orchestrator/"
            + "enqueue.py land <branch>`.\n4. FINISHERS.\n5. LEADS: the clerk lands with `enqueue.py land <b> --lane clerk`.\n";
        string step3 = Step3(planted);
        Assert.StartsWith("3. LAND.", step3, StringComparison.Ordinal);
        Assert.DoesNotContain("4. FINISHERS", step3, StringComparison.Ordinal);
        Assert.Matches(LandCommand, step3);
        Assert.DoesNotMatch(LandCommand, Step3(planted.Replace("enqueue.py land <branch>", "land_lane.py status",
            StringComparison.Ordinal)));
    }
}
