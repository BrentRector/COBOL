// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE LOCAL LINUX GATE RUNS WHAT CI'S LINUX JOBS RUN (kb/Work PB1732, PB1955): every test project a Linux job of
/// <c>.github/workflows/build-and-test.yml</c> runs with <c>dotnet test</c>, and every repository script it runs with
/// <c>bash</c>, is a default leg of <c>scripts/linux-gate.sh</c>.
/// </summary>
/// <remarks>
/// Train 71b's first push went red in CI's Linux unit job on a Windows path literal that was green on Windows, and
/// the ~30-minute CI round trip found what a local WSL run finds in minutes. The WSL gate is only a substitute for
/// CI's Linux jobs while it runs the same projects. A Linux job that gains a test project the gate does not know
/// would be exactly the silent gap the gate exists to close, so that addition is red here until the gate learns it.
/// </remarks>
public sealed class LinuxGateDriftTests
{
    [Fact]
    public void EveryTestProjectCisLinuxJobsRun_IsALegOfTheLinuxGate()
    {
        string workflow = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml"));
        string gate = File.ReadAllText(TestRepo.Scripts("linux-gate.sh"));

        var linuxProjects = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string job in Regex.Split(workflow, @"^  (?=[A-Za-z0-9_-]+:\s*$)", RegexOptions.Multiline))
        {
            if (!Regex.IsMatch(job, @"^\s+runs-on:\s*ubuntu", RegexOptions.Multiline))
            {
                continue;
            }

            foreach (Match m in Regex.Matches(job, @"dotnet test (tests/[^\s]+\.csproj)"))
            {
                linuxProjects.Add(m.Groups[1].Value);
            }
        }

        var gateProjects = Regex.Matches(gate, @"proj=""(tests/[^""]+\.csproj)""").Select(m => m.Groups[1].Value).ToHashSet();

        Assert.NotEmpty(linuxProjects);
        var missing = linuxProjects.Where(p => !gateProjects.Contains(p)).ToList();
        Assert.True(missing.Count == 0,
            "CI's Linux jobs run test projects scripts/linux-gate.sh has no leg for: " + string.Join(", ", missing)
            + ". Add a leg, or the local Linux gate no longer stands in for CI's Linux jobs (kb/Work PB1732).");
    }

    /// <summary>
    /// ⛔ AND EVERY REPOSITORY SCRIPT A LINUX JOB RUNS AS A TEST STEP IS A LEG TOO (kb/Work PB1955, PB1957 row 40).
    /// </summary>
    /// <remarks>
    /// The fact above reads only <c>dotnet test</c> lines, and CI's <c>guard</c> job runs none: its whole test is
    /// <c>run: bash scripts/guard-fast.sh</c> (the NIST suite through the <c>cobol</c> CLI, the manifest audit, the
    /// guard's own self-tests). So that job was the one CI Linux job no local gate ran, and train 1013's
    /// guard red on PB322's TERMINATES rows reached CI behind a green Windows gate and a green Linux gate. A step that
    /// runs a script under <c>scripts/</c> with <c>bash</c> is held here to a <c>linux-gate.sh</c> leg that runs the
    /// SAME script, so the local gate and the CI job cannot measure different populations.
    /// </remarks>
    [Fact]
    public void EveryScriptCisLinuxJobsRun_IsALegOfTheLinuxGate()
    {
        string workflow = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml"));
        string gate = File.ReadAllText(TestRepo.Scripts("linux-gate.sh"));

        var linuxScripts = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string job in Regex.Split(workflow, @"^  (?=[A-Za-z0-9_-]+:\s*$)", RegexOptions.Multiline))
        {
            if (!Regex.IsMatch(job, @"^\s+runs-on:\s*ubuntu", RegexOptions.Multiline))
            {
                continue;
            }

            foreach (Match m in Regex.Matches(job, @"^\s+run:\s*bash (scripts/[^\s]+\.sh)\s*$", RegexOptions.Multiline))
            {
                linuxScripts.Add(m.Groups[1].Value);
            }
        }

        Assert.Contains("scripts/guard-fast.sh", linuxScripts);
        var missing = linuxScripts.Where(s => !Regex.IsMatch(gate, @"^[^#\n]*\bbash " + Regex.Escape(s) + @"\b", RegexOptions.Multiline)).ToList();
        Assert.True(missing.Count == 0,
            "CI's Linux jobs run scripts scripts/linux-gate.sh never runs: " + string.Join(", ", missing)
            + ". Add a leg that runs the same script, or the local Linux gate no longer stands in for CI's Linux jobs "
            + "(kb/Work PB1955).");

        // The guard leg is in the DEFAULT leg list: a leg only `--legs guard` reaches is a leg no gate runs.
        Match defaults = Regex.Match(gate, @"^legs=""([^""]+)""", RegexOptions.Multiline);
        Assert.True(defaults.Success, "scripts/linux-gate.sh no longer declares its default legs as legs=\"…\"");
        Assert.Contains("guard", defaults.Groups[1].Value.Split(','));
    }

    /// <summary>
    /// ⛔ THE LINUX GATE READS THE TREE WITH THE TREE'S OWN GIT, AND NO READ OF IT WRITES IT (kb/Work PB2880): every
    /// git call of <c>scripts/linux-gate.sh</c> that names the tree goes through its one <c>wgit</c>, whose arms all
    /// pass <c>--no-optional-locks</c>, and a tree on a Windows drive is read by Windows git (<c>git.exe</c>).
    /// </summary>
    /// <remarks>
    /// Linux git reading a Windows tree through <c>/mnt</c> found the index's stat data foreign, re-hashed every
    /// tracked file across the 9P boundary (34 s at half of one core: the gate's first serial valley, in every run),
    /// and then wrote the refreshed index back into the real worktree, which Windows git rewrote on its next command.
    /// A git call that bypasses <c>wgit</c>, or an arm without the flag, brings either cost back unseen: the gate's
    /// verdict would still be GREEN.
    /// </remarks>
    [Fact]
    public void TheLinuxGateReadsTheTree_WithItsOwnGit_AndNeverRefreshesItsIndex()
    {
        string[] code = File.ReadAllLines(TestRepo.Scripts("linux-gate.sh"))
            .Where(l => !l.TrimStart().StartsWith('#')).ToArray();
        string[] arms = code.Where(l => Regex.IsMatch(l, @"^\s*wgit\(\)\s*\{")).ToArray();

        Assert.True(arms.Length == 2, "scripts/linux-gate.sh should define wgit once per tree kind (a Windows drive, "
            + "a Linux filesystem); it defines it " + arms.Length + " time(s)");
        Assert.All(arms, a => Assert.Contains("--no-optional-locks", a));
        Assert.Contains(arms, a => Regex.IsMatch(a, @"\bgit\.exe\b") && a.Contains("$winroot", StringComparison.Ordinal));

        // A git command word outside an echoed message (a message that NAMES git runs none), on a line naming the tree.
        var direct = code.Where(l => !arms.Contains(l)
                                     && Regex.IsMatch(Regex.Replace(l, @"\becho\s+""[^""]*""", ""), @"(^|[\s;(|&])git(\.exe)?\s")
                                     && Regex.IsMatch(l, @"\$\{?(tree|winroot)\b")).ToList();
        Assert.True(direct.Count == 0, "scripts/linux-gate.sh runs git on the tree outside wgit: "
            + string.Join(" | ", direct.Select(l => l.Trim())));
    }
}
