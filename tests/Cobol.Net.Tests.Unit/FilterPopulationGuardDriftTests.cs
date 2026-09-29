// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE INVARIANT: no filtered <c>dotnet test</c> invocation anywhere in this repository — CI leg, generator
/// script or gate — may treat exit 0 as evidence that the filtered tests RAN (kb/Work PB708, PB751, PB752).
/// </summary>
/// <remarks>
/// <para>
/// vstest answers a filter that matches nothing with a PASSING RUN OF ZERO TESTS: exit 0, no warning, no red.
/// So every filter is a CLAIM about which tests ran, and every claim needs its evidence
/// (<c>feedback_verdict_evidence_invariant</c> — a run must assert its population;
/// <c>feedback_measure_the_selectors_complement</c> — a selector is evidence about what it RETURNED, never
/// about what it dropped). The same hole has now been found FOUR times in three shapes: PB287 (four CI filters
/// keyed <c>Class.Method</c> that the A13 partition split had made unmatchable), PB708 (a dead term OR'd among
/// live ones inside the local gate's filter), PB751 (the two doc regenerators, each of whose ONE test is the
/// whole regeneration), PB752 (the two single-purpose CI legs, one of them the roadmap's fatal-challenge
/// criterion). Each was fixed at its own site; none of the fixes could stop the fifth site inheriting the hole.
/// </para>
/// <para>
/// This test is that stop (CLAUDE.md rule 5 — prefer the shape that makes the NEXT case automatic, and pair it
/// with a drift test so "automatic" stays true). A new script or CI leg that filters a test run is covered the
/// moment it is written, with no registration step to forget, because the scan is over the FILE TREE and not
/// over a list.
/// </para>
/// <para>
/// ⚠ TWO population assertions are admissible, and they are recognized by SHAPE rather than by a list of
/// blessed job names (CLAUDE.md rule 8 — no hand-maintained list where a structure belongs):
/// </para>
/// <list type="number">
/// <item><description>a call to <c>scripts/filter_population.py</c>, which asks vstest per TERM whether the term
/// names a real test — the ONE place that rule lives;</description></item>
/// <item><description>the SHARDED form: the leg lists its build's population with
/// <c>scripts/test_population.py list</c> and publishes it beside its trx, and the <c>conformance-population</c>
/// job runs <c>test_population.py check</c> over every shard's trx against that listing — the shards' UNION must be
/// the discovered population case by case (kb/Work PB1718). That is a STRONGER assertion than a per-term floor,
/// which is why the shard legs are not made to call the guard as well — a second, weaker guard beside a stronger
/// one is duplication, not defence in depth. Its validity depends entirely on that checking job still existing,
/// which is why <see cref="TheShardPopulationJob_StillChecksTheShardUnionAgainstTheListing"/> is a separate test
/// rather than a comment.</description></item>
/// </list>
/// <para>
/// ⚠ SCOPE — the EXECUTABLE surfaces: everything under <c>scripts/</c> and every workflow under
/// <c>.github/workflows/</c>. Docs are excluded deliberately: <c>DEVLOG.md</c> is the historical record and
/// quotes the gate commands as they were on the day, so scanning it would demand rewriting history to make a
/// test green. The guard itself is the one file exempt from its own rule — it is the mechanism, and its
/// docstring necessarily quotes <c>--filter</c>.
/// </para>
/// </remarks>
public sealed class FilterPopulationGuardDriftTests
{
    /// <summary>The guard — a site satisfies the invariant by naming it.</summary>
    private const string Guard = "filter_population.py";

    /// <summary>
    /// ⛔ EVERY CHECK BELOW READS CODE, NEVER COMMENTS. A banner that merely TALKS about <c>--list-tests</c> or
    /// about the guard is not a call to either, and a check that accepted prose could be satisfied by deleting
    /// the mechanism and keeping the sentence describing it. Measured, not theorised: the first draft of
    /// <see cref="TheShardPopulationJob_StillReDiscoversAndSumsTheShardCounts"/> stayed GREEN when the summing
    /// job's <c>--list-tests</c> discovery was deleted, because that job's own banner names it.
    /// <para>
    /// Only WHOLE-LINE comments are stripped, and <c>#</c> introduces one in all four languages in scope
    /// (PowerShell, bash, Python, YAML). A trailing <c>#</c> is left alone deliberately: it cannot be told from
    /// a <c>#</c> inside a string without parsing each language, and treating one as a comment would hide real
    /// code from the scan — the failure direction this whole file exists to prevent.
    /// </para>
    /// </summary>
    private static string CodeOnly(string text)
        => string.Join('\n', text.Split('\n').Where(l => !l.TrimStart().StartsWith('#')));

    /// <summary>A file/job only has to assert a population if it actually runs a FILTERED test invocation.</summary>
    private static bool RunsAFilteredTest(string text)
        => text.Contains("dotnet test", StringComparison.Ordinal) && text.Contains("--filter", StringComparison.Ordinal);

    /// <summary>
    /// The sharded population assertion, recognized by its moving parts rather than by name: the shard lists its
    /// population through the one tool and writes the trx that the checking job reads.
    /// </summary>
    private static bool AssertsPopulationByShardUnion(string text)
        => text.Contains(PopulationTool + " list", StringComparison.Ordinal)
        && text.Contains("trx;LogFileName=", StringComparison.Ordinal)
        && text.Contains("upload-artifact", StringComparison.Ordinal);

    /// <summary>The one population check every whole-assembly run shares (kb/Work PB1718).</summary>
    private const string PopulationTool = "scripts/test_population.py";

    /// <summary>
    /// Every script and every CI job that runs a filtered test asserts that the filter selected something.
    /// </summary>
    [Fact]
    public void EveryFilteredTestInvocation_InScriptsAndCi_AssertsItsPopulation()
    {
        var offenders = new List<string>();
        var sites = new List<string>();

        foreach (string file in Directory.EnumerateFiles(TestRepo.Scripts(), "*", SearchOption.AllDirectories))
        {
            string rel = "scripts/" + Path.GetRelativePath(TestRepo.Scripts(), file).Replace('\\', '/');
            // __pycache__ holds compiled BUILD OUTPUT, and the guard is exempt from its own rule. So is scripts/hooks/:
            // a Claude Code hook RECEIVES a command as data and decides whether to allow it — the PreToolUse guard
            // (forbidden_commands.py, kb/Work R49) is itself a filtered-test rule and quotes `dotnet test --filter`
            // shapes to recognize them — and never runs a test; the hooks that run anything are named in settings.json.
            if (rel.Contains("__pycache__", StringComparison.Ordinal) || rel.EndsWith(Guard, StringComparison.Ordinal)
                || rel.StartsWith("scripts/hooks/", StringComparison.Ordinal))
                continue;

            string code = CodeOnly(File.ReadAllText(file));
            if (!RunsAFilteredTest(code)) continue;
            sites.Add(rel);
            if (!code.Contains(Guard, StringComparison.Ordinal)) offenders.Add(rel);
        }

        // ⛔ THE SCAN MUST ASSERT ITS OWN POPULATION — the failure this whole test exists to prevent is a check
        // that looked at nothing and reported clean. The wave-local gate twins are the canonical filtered
        // invocations (plan §9); if the scan stops seeing them, the scan is broken, not the repo clean.
        Assert.Contains("scripts/build-local.ps1", sites);
        Assert.Contains("scripts/build-local.sh", sites);

        var jobSites = new List<string>();
        foreach (string workflow in Directory.EnumerateFiles(
                     TestRepo.At(".github", "workflows"), "*.yml", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(workflow);
            foreach ((string job, string body) in SplitJobs(File.ReadAllLines(workflow), name))
            {
                string code = CodeOnly(body);
                if (!RunsAFilteredTest(code)) continue;
                jobSites.Add($"{name}:{job}");
                if (!code.Contains(Guard, StringComparison.Ordinal) && !AssertsPopulationByShardUnion(code))
                    offenders.Add($"{name} job '{job}'");
            }
        }

        Assert.NotEmpty(jobSites);

        Assert.True(offenders.Count == 0,
            "A filtered `dotnet test` runs here with NOTHING asserting that the filter selected anything. vstest\n"
            + "answers an unmatched filter with a PASSING run of zero tests, so exit 0 proves nothing (kb/Work\n"
            + "PB708/PB751/PB752). Add the guard next to the run — it needs python and the built test project and\n"
            + "nothing else:\n"
            + "    python scripts/filter_population.py --filter \"<the same filter>\" --filtered <the test project>\n"
            + "(add --allow-build when the run itself is what builds), and fail the caller on any non-zero rc.\n"
            + "Unguarded sites:\n  " + string.Join("\n  ", offenders)
            + $"\n\nScanned: {sites.Count} script(s) + {jobSites.Count} CI job(s) that run a filtered test.");
    }

    /// <summary>
    /// The shard legs' population assertion IS the checking job; if it goes, their exemption goes with it.
    /// </summary>
    [Fact]
    public void TheShardPopulationJob_StillChecksTheShardUnionAgainstTheListing()
    {
        string workflow = TestRepo.At(".github", "workflows", "build-and-test.yml");
        var checking = SplitJobs(File.ReadAllLines(workflow), "build-and-test.yml")
            .Select(j => (j.Name, Code: CodeOnly(j.Body)))
            .Where(j => j.Code.Contains(PopulationTool + " check", StringComparison.Ordinal)
                     && j.Code.Contains("download-artifact", StringComparison.Ordinal))
            .ToList();

        Assert.True(checking.Count > 0,
            "No CI job checks the conformance shards' union against the discovered population. The sharded\n"
            + "legs are exempt from calling scripts/filter_population.py ONLY because that job asserts something\n"
            + "STRONGER on their behalf (kb/Work PB752, PB1718). With it gone, every shard filter is an unverified\n"
            + "claim again: either restore the checking job, or make each shard leg call the guard.");

        // The check reads BOTH halves — the shards' listings and their trx files — never a count of either.
        string code = checking[0].Code;
        Assert.Contains("--listing", code, StringComparison.Ordinal);
        Assert.Contains("--trx", code, StringComparison.Ordinal);
        Assert.DoesNotContain("grep -c", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// The workflow's jobs, split on the two-space keys under <c>jobs:</c> — every key inside a job is deeper,
    /// so the split is exact for this file's shape, and the caller asserts it found jobs at all.
    /// </summary>
    /// <remarks>
    /// ⛔ A comment block sitting immediately above a job key documents the job BELOW it, not the job above —
    /// this file's jobs are each introduced by a <c>── … ──</c> banner. A splitter that appended those lines to
    /// the preceding job attributed <c>conformance-population</c>'s banner (which names <c>--list-tests</c>) to
    /// <c>windows-conformance</c>, and the summing job was then "found" in the wrong place. Measured, not
    /// theorised: that is exactly how this test failed on its first run.
    /// </remarks>
    private static List<(string Name, string Body)> SplitJobs(string[] lines, string file)
    {
        int start = Array.FindIndex(lines, l => l.StartsWith("jobs:", StringComparison.Ordinal));
        Assert.True(start >= 0, $"{file} has no top-level `jobs:` key — the job splitter cannot have found "
            + "anything, and a scan that looked at nothing must never report clean.");

        var key = new Regex(@"^  (?<name>[A-Za-z0-9_.-]+):\s*$", RegexOptions.None, TimeSpan.FromSeconds(5));
        var jobs = new List<(string, string)>();
        string? name = null;
        var body = new StringBuilder();
        var pending = new StringBuilder();   // trailing comment/blank lines: they belong to the NEXT job
        for (int i = start + 1; i < lines.Length; i++)
        {
            Match m = key.Match(lines[i]);
            if (m.Success)
            {
                if (name is not null) jobs.Add((name, body.ToString()));
                name = m.Groups["name"].Value;
                body.Clear().Append(pending);
                pending.Clear();
                continue;
            }

            string trimmed = lines[i].TrimStart();
            if (trimmed.Length == 0 || trimmed[0] == '#')
            {
                pending.Append(lines[i]).Append('\n');
                continue;
            }

            body.Append(pending).Append(lines[i]).Append('\n');
            pending.Clear();
        }

        if (name is not null) jobs.Add((name, body.Append(pending).ToString()));
        Assert.NotEmpty(jobs);
        return jobs;
    }
}
