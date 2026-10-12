// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ GITHUB'S MERGE QUEUE IS THE LANDING, AND NOTHING OPENS A PULL REQUEST BEFORE THE QUEUE IS ACTIVE (kb/Work PB3024;
/// DESIGN-test-build-ci.md §3.17.6 and DESIGN-test-build-ci.md §3.17.8): <c>.github/workflows/build-and-test.yml</c> runs the matrix on the
/// <c>merge_group</c> event and only the cheap jobs on <c>pull_request</c>; its already-verified short-circuit (and
/// <c>push-main.sh</c>'s skip while it lives) counts only a run that could have run the matrix, never a pull request's;
/// no tracked file but <c>scripts/orchestrator/enqueue.py</c> opens or queues a pull request, and enqueue.py asks
/// <c>ruleset.py check</c> for an ACTIVE queue before it checks, pushes or opens anything.
/// </summary>
/// <remarks>
/// <para>The operator's review (2026-10-11 13:39, mailbox 20261011-133923): a pull request runs only CI's cheap jobs, so
/// its <c>ci-gate</c> passes on them; opened before the owner applies the ruleset there is no merge group, and
/// <c>gh pr merge --auto</c> would merge it with no whole-population run. The guard is closed by construction: one
/// script holds the pull-request step and its guard comes first, and this test is red on a brief, unit, script or
/// workflow that opens or queues a pull request any other way. A pull request's green <c>ci-gate</c> is attached to
/// its head commit, the very sha a landing pushes, so a short-circuit that read the check run would skip the matrix
/// for an untested commit; the workflow and push-main.sh read the workflow RUNS and exclude <c>pull_request</c>.</para>
/// <para>The live ruleset is compared with the committed one by <c>ruleset.py check</c>, whose consumers are the
/// landing itself (enqueue.py refuses on anything but ACTIVE) and the session probe's <c>gate :</c> line — not an
/// xUnit arm: this suite has no dynamic skip, and an offline comparison must read SKIPPED, never green. The arms that
/// retire <c>push-main.sh</c>, <c>landing_lease.py</c> and the <c>ci/**</c> trigger land with the first change merged
/// THROUGH the queue, which deletes them (kb/Work PB3024).</para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class LandingPipelineDriftTests
{
    /// <summary>A step that opens or queues a pull request: the gh CLI's text form, or a Python argument list.</summary>
    private static readonly Regex PullRequestStep = new(
        @"\bgh\s+pr\s+(?:create|merge)\b|[""']pr[""']\s*,\s*[""'](?:create|merge)[""']", RegexOptions.Compiled);

    /// <summary>Tracked paths that DESCRIBE the landing rather than perform it: design docs, the work register, the
    /// history, frozen evidence and adjudication records, the spec, and the test sources (this file names the patterns).</summary>
    private static readonly string[] Narrative = ["docs/", "kb/", "adjudication/", "specs/", "tests/", "DEVLOG/"];

    private const string TheGuardedStep = "scripts/orchestrator/enqueue.py";

    /// <summary>The tracked files, outside <see cref="Narrative"/>, that hold a pull-request step.</summary>
    internal static IReadOnlyList<string> FilesWithAPullRequestStep(string root, IEnumerable<string> tracked) =>
        [.. tracked.Where(p => !Narrative.Any(n => p.StartsWith(n, StringComparison.Ordinal)))
                   .Where(p => File.Exists(Path.Combine(root, p)))
                   .Where(p => PullRequestStep.IsMatch(File.ReadAllText(Path.Combine(root, p))))];

    [Fact]
    public void NoFileButEnqueue_OpensOrQueuesAPullRequest()
    {
        IReadOnlyList<string> tracked = TrackedTree.Files();
        Assert.True(tracked.Count >= 1000, $"`git ls-files` reported only {tracked.Count} paths — the sweep would scan nothing");
        Assert.Contains(TheGuardedStep, tracked);
        var offenders = FilesWithAPullRequestStep(TestRepo.Root, tracked).Where(p => p != TheGuardedStep).ToList();
        Assert.True(offenders.Count == 0,
            "these files open or queue a pull request outside scripts/orchestrator/enqueue.py, whose guard refuses "
            + "unless `ruleset.py check` reports the merge queue ACTIVE (kb/Work PB3024): route the step through "
            + $"`enqueue.py land`:\n  {string.Join("\n  ", offenders)}");
    }

    /// <summary>⭐ The sweep's failure branch, fired: a planted brief with a hand PR step is found, by the same function.</summary>
    [Fact]
    public void TheSweep_FindsAPlantedHandStep()
    {
        string dir = Path.Combine(Path.GetTempPath(), "landing-pipeline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "brief"));
        try
        {
            File.WriteAllText(Path.Combine(dir, "brief", "lander.md"), "7. `gh pr create --title T` then `gh pr merge --auto`.");
            File.WriteAllText(Path.Combine(dir, "brief", "tool.py"), "gh([\"pr\", \"merge\", n, \"--auto\"])");
            File.WriteAllText(Path.Combine(dir, "brief", "fine.md"), "land with enqueue.py queue");
            Directory.CreateDirectory(Path.Combine(dir, "docs"));
            File.WriteAllText(Path.Combine(dir, "docs", "design.md"), "the lander runs `gh pr create` (narrative)");
            var found = FilesWithAPullRequestStep(dir, ["brief/lander.md", "brief/tool.py", "brief/fine.md", "docs/design.md"]);
            Assert.Equal(new[] { "brief/lander.md", "brief/tool.py" }, found);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>enqueue.py's queue guard comes before every other step: the ACTIVE test precedes the landing checks,
    /// the push and both pull-request calls in its <c>queue</c> function.</summary>
    [Fact]
    public void TheQueueGuard_PrecedesEveryLandingStep()
    {
        string source = File.ReadAllText(TestRepo.Scripts("orchestrator", "enqueue.py"));
        int queueDef = source.IndexOf("\ndef queue(", StringComparison.Ordinal);
        Assert.True(queueDef > 0, "enqueue.py has no `def queue(` — the landing step moved; re-point this test");
        string body = source[queueDef..source.IndexOf("\n# ── self-test", queueDef, StringComparison.Ordinal)];
        int guard = body.IndexOf("if st != ruleset.ACTIVE:", StringComparison.Ordinal);
        Assert.True(guard > 0, "enqueue.py's queue() no longer tests the merge queue for ACTIVE");
        foreach (string step in new[] { "checks(sha, base)", "\"push\"", "\"pr\", \"create\"", "\"pr\", \"merge\"" })
        {
            int at = body.IndexOf(step, StringComparison.Ordinal);
            Assert.True(at > guard, $"in enqueue.py's queue(), `{step}` is {(at < 0 ? "missing" : "BEFORE the queue guard")}");
        }
    }

    /// <summary>enqueue.py's <c>land</c> asks for an ACTIVE queue before it merges main in, gates or stamps, and has no
    /// other path onto main: the ruleset binds administrators, so a gate spent while the queue is off could land nothing,
    /// and the former push-main.sh arm gated for half an hour with no landing lease held (kb/Work PB3023 review R1-1).</summary>
    [Fact]
    public void TheLandGuard_PrecedesTheMergeTheGateAndTheStamp_AndThereIsNoOtherPath()
    {
        string source = File.ReadAllText(TestRepo.Scripts("orchestrator", "enqueue.py"));
        int landDef = source.IndexOf("\ndef land(", StringComparison.Ordinal);
        Assert.True(landDef > 0, "enqueue.py has no `def land(` — the landing step moved; re-point this test");
        string body = source[landDef..source.IndexOf("\ndef ", landDef + 1, StringComparison.Ordinal)];
        int guard = body.IndexOf("if st != ruleset.ACTIVE:", StringComparison.Ordinal);
        Assert.True(guard > 0, "enqueue.py's land() no longer tests the merge queue for ACTIVE");
        foreach (string step in new[] { "merge_main(wt", "gates(wt)", "(stamp or _stamp)", "queue(wt" })
        {
            int at = body.IndexOf(step, StringComparison.Ordinal);
            Assert.True(at > guard, $"in enqueue.py's land(), `{step}` is {(at < 0 ? "missing" : "BEFORE the queue guard")}");
        }
        Assert.DoesNotContain("push-main", body);
    }

    [Fact]
    public void TheWorkflow_RunsTheMatrixOnTheMergeGroup_AndTheCheapJobsOnAPullRequest()
    {
        string wf = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml")).ReplaceLineEndings("\n");
        string triggers = wf[..wf.IndexOf("\nconcurrency:", StringComparison.Ordinal)];
        Assert.Matches(new Regex(@"^  merge_group:\s*$", RegexOptions.Multiline), triggers);
        Assert.Matches(new Regex(@"^  pull_request:\s*$", RegexOptions.Multiline), triggers);
        Assert.Matches(new Regex(@"if \[ ""\$EVENT"" = ""pull_request"" \]; then\n(?:\s*#[^\n]*\n)*\s*code=false\n"), wf);
        Assert.DoesNotContain("a pull_request / dispatch run is always the full matrix", wf);
        Assert.DoesNotContain("check-runs?check_name=ci-gate", wf);
        Assert.Contains(".event != \"pull_request\" and .conclusion == \"success\"", wf);
    }

    [Fact]
    public void PushMainsSkip_CountsOnlyARunThatCouldHaveRunTheMatrix()
    {
        string script = TestRepo.Scripts("push-main.sh");
        if (!File.Exists(script))
        {
            // Deleted by the first change landed through the queue (kb/Work PB3024): its landing branches go with it.
            string wf = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml"));
            Assert.DoesNotContain("'ci/**'", wf);
            return;
        }
        string text = File.ReadAllText(script);
        Assert.Contains("if verified_by_a_full_run \"$SHA\"; then", text);
        Assert.Contains(".event != \"pull_request\" and .conclusion == \"success\"", text);
        Assert.Contains("scripts/orchestrator/enqueue.py checks", text);
    }

    [Fact]
    public void TheQueueAndTheRuleset_DeclareEveryArm()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "enqueue.py"), new[]
        {
            "the queue not ACTIVE (ABSENT, DRIFT or SKIPPED) refuses with exit 8 before anything is checked, pushed or opened",
            "a landing check's refusal stops the queue with its own exit code and opens nothing",
            "an ACTIVE queue pushes the branch, opens one pull request and queues it with --auto",
            "a re-run reuses the branch's open pull request, moves its head and queues it again",
            "GitHub's refusal of the pull request list, the pull request or the queue is exit 9, never 0",
            "HEAD beside origin/main is refused (main brought in first), and HEAD on main is refused before any check",
            "HEAD at origin/main has nothing to land; an empty title or body is a usage error",
            "checks runs the three landing scripts on the range in order and maps each refusal to its own code: "
                + "DEVLOG 6, oracle 7, landing check 3, unreadable 2",
            // kb/Work PB3023: the whole landing step, one script
            "land: the migrating landing (the whole history added as files) titles its pull request from its ONE new "
                + "entry, the landing rule's own (devlog.landing_entries)",
            "land: a dirty worktree is refused (exit 2) before anything is fetched, merged or gated",
            "land: a conflict bringing main in is exit 4: the merge is aborted, the branch unchanged, the report told "
                + "which files, and nothing gated",
            "land: main is MERGED in (the branch's commits kept), and a host's red gate is exit 5 with the host named "
                + "in the report, before any stamp",
            // review R1-1: main takes only queued pull requests, so nothing is merged or gated while the queue is off
            "land: the queue not ACTIVE (ABSENT, DRIFT or SKIPPED) is exit 8 before anything is merged, gated, "
                + "stamped or pushed: main takes only queued pull requests (kb/Work PB3023 review R1-1)",
            "land: an ACTIVE queue opens one pull request titled from the DEVLOG entry, whose body carries the entry "
                + "and the commits, and queues it",
            "land: the queued pull request is measured once, in the branch's worktree (kb/Work PB3028)",
            "land: a re-run on the commit already gated GREEN does not gate again, a new commit does; a landing "
                + "check's refusal keeps its own code",
            // review R1-2: the queue merges other pull requests while a branch gates
            "land: main moving during the gate is merged in again WITHOUT a second gate and the branch is queued on "
                + "the new main; a re-run on that head (a clean merge after the gated one) is not gated again",
            "land: a conflict bringing the moved main in after the gate is exit 4: the merge is aborted, the branch "
                + "unchanged, the report told, and nothing pushed or queued",
            // MANDATORY-PRACTICES I10, L9 (kb/Work PB3027): only a tip its CLEAN review stamp covers lands
            "review: a tip that moved after its CLEAN review only by main's commits, a clean merge of main and a "
                + "DEVLOG-only commit is still reviewed",
            "review: a commit after the CLEAN review that is not mechanical (a finisher's fix, a hand-resolved "
                + "merge) is exit 11 before anything is merged or gated, the report told why",
            "review: a FINDINGS stamp or no report is unreviewed; only a lane I10 does not review, named with "
                + "--lane, lands without a stamp",
        });
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "ruleset.py"), new[]
        {
            "a live ruleset equal to the committed one, the queue present and no classic protection, is ACTIVE",
            "extra fields GitHub adds to the live ruleset, and its own rule order, are not drift",
            "no ruleset on main is ABSENT, never ACTIVE",
            "a live ruleset that differs (a parameter, a missing rule, enforcement off) is DRIFT with each difference named",
            "the classic protection still on main beside the ruleset is DRIFT",
            "no merge queue reported by GitHub, or a repository setting the queue needs turned off, is DRIFT",
            "no gh, no network or a server error is SKIPPED, never ACTIVE",
            "apply refuses without the owner's go, and with it sets the repository, creates or updates the ruleset by "
                + "name and deletes the classic protection",
            "the committed ruleset requires a pull request, the merge queue with a merge commit and ci-gate, with no bypass",
            // The pair guard (kb/Work PB3024, the first apply's HTTP 422): GitHub accepts three (title, message) pairs.
            "the committed repository settings name a merge-commit pair GitHub accepts",
            "apply refuses MERGE_MESSAGE+PR_BODY (GitHub's invalid_merge_commit_setting_combo, the first apply's 422) "
                + "before any call reaches GitHub",
        });
    }
}
