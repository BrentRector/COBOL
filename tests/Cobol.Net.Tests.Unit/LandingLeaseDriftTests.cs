// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE LANDER ON MAIN AT A TIME, MECHANICALLY (kb/Work PB2537): <c>scripts/orchestrator/landing_lease.py --self-test</c>
/// plants two landers and shows the second WAITS (and gates only once the first releases) instead of gating against a
/// main the first is about to move; takes over an expired lease and one whose worktree is gone; lets only the holder
/// renew or release; lets a resuming lander re-take its own lease; admits exactly one of six parallel acquires;
/// answers <c>push-main.sh</c>'s <c>check</c> with own, free or held elsewhere; stops every verb with exit 2 on a
/// file that is not a lease (never 0, 1 or 4, which push-main acts on); and shows <c>next_unit.py</c> starts no
/// <c>land</c> unit while a lease is live.
/// </summary>
/// <remarks>
/// On 2026-10-07 the R1 lander lost the race to main three times (trains 1034 and 1034b landed inside its ~50-minute
/// rebase-gate-CI window), re-gated each time, and ended SPLIT with every gate and CI green and nothing landed: push-main
/// serialized only the final push. Every lander now takes the lease before its final rebase and gates, and push-main
/// refuses a landing while another worktree holds it. A self-test reduced to its happy path still exits 0, so the arms
/// are asserted by name. The waiting arm is event-driven (the waiter's own line, then the holder's release); its only
/// clock is a hang guard, never a speed bound.
/// </remarks>
public sealed class LandingLeaseDriftTests
{
    [Fact]
    public void TheLandingLease_ProvesEveryArm()
    {
        string script = TestRepo.Scripts("orchestrator", "landing_lease.py");
        Assert.True(File.Exists(script), $"the landing lease script is missing: {script}");
        var r = PythonInstrument.Run(script, "--self-test");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("landing_lease SELF-TEST: PASS", StringComparison.Ordinal),
            $"landing_lease.py --self-test failed (exit {r.ExitCode}).\n{r.Stdout}\n{r.Stderr}");
        foreach (string arm in new[]
                 {
                     "one lander holds the lease: a second acquire is refused, at once or when its wait runs out",
                     "a second lander waits instead of gating, and gates once the first releases",
                     "an expired lease is taken over, and so is one whose worktree is gone",
                     "only the holder renews or releases; a lost lease says so",
                     "the holder's own worktree re-takes its lease, by any spelling of its path, and its expiry moves",
                     "parallel acquires: exactly one wins",
                     "check answers push-main: own, free or held elsewhere",
                     "a file that is not a lease stops every verb loudly",
                     "the loop starts no land unit while a lease is live",
                 })
        {
            Assert.True(r.Stdout.Contains("PASS  " + arm, StringComparison.Ordinal),
                $"`landing_lease.py --self-test` no longer passes '{arm}'.\n{r.Stdout}{r.Stderr}");
        }
    }
}
