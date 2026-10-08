// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY SCRIPT SELF-TEST RUNS IN EVERY GATE AND IN CI, THROUGH ONE DISCOVERING RUNNER (kb/Work PB2563):
/// <c>scripts/self_tests.py</c> finds every self-test under <c>scripts/</c>, and the local gate's audits, the Linux gate
/// and CI's jobs all run that runner and name no self-test by hand.
/// </summary>
/// <remarks>
/// Until PB2563 the gates ran self-tests from hand lists: the gate driver's AUDITS named five, CI's audits job six,
/// linux-gate.sh three, and eleven of the fourteen orchestrator self-tests ran in no gate and no CI job, so a regression
/// in the loop's unit choice, the allocator's lock or the wave planner passed everything. <c>landing_check.py</c> ran
/// only in CI, a red no local gate could show first (the CI invariant, kb/Work PB1957). These facts hold the three
/// halves of the fix: the runner's discovery misses no self-test a broader net finds, every gate entry point runs the
/// runner (and none names a self-test itself), and the runner's own arms, a planted failing self-test turning it RED
/// among them, still fire.
/// </remarks>
public sealed class SelfTestDiscoveryDriftTests
{
    private static readonly Regex QuotedFlag = new(@"[""']--self-test[""']", RegexOptions.Compiled);
    private static readonly Regex ShellFlagArm = new(@"(^|\s)--self-test\)|=\s*[""']--self-test[""']", RegexOptions.Compiled);
    private static readonly Regex StandaloneName = new(@"^test_[^/]*\.(py|ps1)$", RegexOptions.Compiled);

    /// <summary>What the runner discovered here: each self-test's repository path and whether it runs with the flag.</summary>
    private static IReadOnlyDictionary<string, bool> Discovered()
    {
        string runner = TestRepo.Scripts("self_tests.py");
        Assert.True(File.Exists(runner), $"the self-test runner is missing: {runner}");
        var r = PythonInstrument.Run(runner, "--list");
        Assert.True(r.ExitCode == 0, $"`self_tests.py --list` failed (exit {r.ExitCode}).\n{r.Stdout}\n{r.Stderr}");
        var found = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (string line in r.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("scripts/", StringComparison.Ordinal))
            {
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                found[parts[0]] = parts.Length > 1 && parts[1] == "--self-test";
            }
        }

        return found;
    }

    /// <summary>
    /// The runner's discovery reads Python's syntax tree; this net reads text, more broadly: any tracked file under
    /// <c>scripts/</c> named <c>test_*.py</c>/<c>test_*.ps1</c>, any Python line that is not a comment and holds a quoted
    /// <c>--self-test</c> beside <c>argv</c> or <c>add_argument</c>, and any shell <c>--self-test)</c> arm or
    /// <c>= "--self-test"</c> test. A self-test written in a shape the runner's rule misses is red here, by name.
    /// </summary>
    [Fact]
    public void EverySelfTestTheNetFinds_IsDiscoveredByTheRunner()
    {
        var discovered = Discovered();
        var net = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string rel in TrackedTree.Files())
        {
            if (!rel.StartsWith("scripts/", StringComparison.Ordinal) || !(rel.EndsWith(".py", StringComparison.Ordinal)
                || rel.EndsWith(".sh", StringComparison.Ordinal) || rel.EndsWith(".ps1", StringComparison.Ordinal)))
            {
                continue;
            }

            if (StandaloneName.IsMatch(rel[(rel.LastIndexOf('/') + 1)..]))
            {
                net.Add(rel);
                continue;
            }

            bool python = rel.EndsWith(".py", StringComparison.Ordinal);
            bool shell = rel.EndsWith(".sh", StringComparison.Ordinal);
            foreach (string raw in File.ReadLines(TestRepo.At(rel)))
            {
                string line = raw.Trim();
                if (line.StartsWith('#'))
                {
                    continue;
                }

                if ((python && QuotedFlag.IsMatch(line) && (line.Contains("argv", StringComparison.Ordinal)
                        || line.Contains("add_argument", StringComparison.Ordinal)))
                    || (shell && ShellFlagArm.IsMatch(line)))
                {
                    net.Add(rel);
                    break;
                }
            }
        }

        // The population is real: the orchestrator's self-tests, the hooks', the gate driver's and the runner's own.
        foreach (string known in new[]
                 {
                     "scripts/orchestrator/test_alloc.py", "scripts/orchestrator/test_orchestrate_core.ps1",
                     "scripts/orchestrator/landing_check.py", "scripts/hooks/test_forbidden_commands.py",
                     "scripts/guard-population.sh", "scripts/run_gate_legs.py", "scripts/self_tests.py",
                 })
        {
            Assert.True(net.Contains(known), $"the net no longer finds the known self-test {known}: the net is broken.");
        }

        var missed = net.Where(p => !discovered.ContainsKey(p)).ToList();
        Assert.True(missed.Count == 0,
            "self-tests the runner (scripts/self_tests.py) does not discover, so no gate and no CI job runs them: "
            + string.Join(", ", missed) + ". Widen the runner's discovery rule (`classify`), never a list (kb/Work PB2563).");
    }

    /// <summary>
    /// Every gate entry point runs the runner: the local gate's AUDITS (before the slot) and its post-build half, the
    /// Linux gate's two legs, CI's audits job on Linux, greenfield-unit's post-build step and windows-build-test on
    /// Windows. And none of them runs a discovered self-test itself: a self-test named in a gate is a hand list again,
    /// and a list goes stale the day a self-test is added.
    /// </summary>
    [Fact]
    public void EveryGateEntryPoint_RunsTheRunner_AndNamesNoSelfTestByHand()
    {
        string driver = File.ReadAllText(TestRepo.Scripts("run_gate_legs.py"));
        string linux = File.ReadAllText(TestRepo.Scripts("linux-gate.sh"));
        string workflow = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml"));

        Match audits = Regex.Match(driver, @"^AUDITS = \((.*?)^\)", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(audits.Success, "scripts/run_gate_legs.py no longer declares its audits as `AUDITS = (…)`");
        Assert.Contains(@"(""SELF-TESTS"", [""scripts/self_tests.py""])", audits.Groups[1].Value, StringComparison.Ordinal);
        Assert.Matches(@"(?m)^BUILT_SELF_TESTS = \[""scripts/self_tests.py"", ""--built""\]\r?$", driver);
        Assert.Matches(@"(?m)^selftests_leg selftests\r?$", linux);
        Assert.Matches(@"(?m)^\s+selftests_leg selftests-built --built\r?$", linux);

        var steps = Jobs(workflow);
        Assert.Contains("run: python3 scripts/self_tests.py", RunLines(steps["audits"]));
        Assert.Contains("run: python3 scripts/self_tests.py --built", RunLines(steps["greenfield-unit"]));
        Assert.Contains("run: python scripts/self_tests.py", RunLines(steps["windows-build-test"]));
        Assert.Matches(@"(?m)^\s+runs-on:\s*ubuntu", steps["audits"]);
        Assert.Matches(@"(?m)^\s+runs-on:\s*ubuntu", steps["greenfield-unit"]);
        Assert.Matches(@"(?m)^\s+runs-on:\s*windows", steps["windows-build-test"]);

        // No entry point runs a self-test by hand: a flag-handling self-test named with `--self-test`, or a standalone
        // test_* script named at all, in a line that runs something (comments excluded).
        var discovered = Discovered();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["scripts/run_gate_legs.py AUDITS"] = audits.Groups[1].Value,
            ["scripts/linux-gate.sh"] = linux,
            ["scripts/build-local.ps1"] = File.ReadAllText(TestRepo.Scripts("build-local.ps1")),
            ["scripts/build-local.sh"] = File.ReadAllText(TestRepo.Scripts("build-local.sh")),
            ["scripts/battery.sh"] = File.ReadAllText(TestRepo.Scripts("battery.sh")),
            [".github/workflows/build-and-test.yml"] = workflow,
        };
        var named = new List<string>();
        foreach (var (where, text) in sources)
        {
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith('#'))
                {
                    continue;
                }

                foreach (var (path, handlesFlag) in discovered)
                {
                    if (path == "scripts/self_tests.py")
                    {
                        continue;
                    }

                    if (handlesFlag ? line.Contains(path + " --self-test", StringComparison.Ordinal)
                                    : line.Contains(path, StringComparison.Ordinal))
                    {
                        named.Add($"{where}: {path} in `{line}`");
                    }
                }
            }
        }

        Assert.True(named.Count == 0,
            "a gate entry point runs a self-test by hand instead of through scripts/self_tests.py: "
            + string.Join("; ", named) + " (kb/Work PB2563).");
    }

    /// <summary>
    /// The runner's own arms, by name: a self-test reduced to its happy path still exits 0. The planted-tree arms are
    /// the "a failing self-test turns the gate red" proof; the gate driver's audit arm (GateLegDriftTests) carries a red
    /// audit to a RED gate before the slot.
    /// </summary>
    [Fact]
    public void TheRunner_ProvesEveryArm()
    {
        var r = PythonInstrument.Run(TestRepo.Scripts("self_tests.py"), "--self-test");
        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("self_tests.py --self-test: ALL GREEN", StringComparison.Ordinal),
            $"`self_tests.py --self-test` failed (exit {r.ExitCode}).\n{r.Stdout}\n{r.Stderr}");
        foreach (string arm in new[]
                 {
                     "a script that only PASSES --self-test to another is not",
                     "a docstring or comment mention is not",
                     "a bash `--self-test)` case arm is a self-test",
                     "test_*.ps1 is a standalone self-test",
                     "a windows-only self-test does not run on linux",
                     "a marker SHOWN past the header (a docstring example) declares nothing",
                     "--built runs exactly the build-needing ones, the default run the rest",
                     "discovery finds the planted tree's three self-tests",
                     "a tracked script discovery cannot read (broken.py) fails discovery, never drops out",
                     "a tracked script discovery cannot read (latin1.py) fails discovery, never drops out",
                     "each self-test gets a private COBOL_COORD_DIR",
                     "each self-test commits under the private global config's identity, never the caller's",
                     "a failing self-test is RED",
                     "a failing self-test's output reaches the console",
                     "a self-test for another platform is NOT RUN, not run",
                     "one red makes the verdict RED",
                     "the verdict names the red and the not-run one with its reason",
                 })
        {
            Assert.True(r.Stdout.Contains("ok   arm: " + arm, StringComparison.Ordinal),
                $"`self_tests.py --self-test` no longer passes '{arm}'.\n{r.Stdout}{r.Stderr}");
        }
    }

    /// <summary>The workflow's jobs, by id: each job's text up to the next job.</summary>
    private static Dictionary<string, string> Jobs(string workflow)
    {
        var jobs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string job in Regex.Split(workflow, @"^  (?=[A-Za-z0-9_-]+:\s*$)", RegexOptions.Multiline))
        {
            Match id = Regex.Match(job, @"^([A-Za-z0-9_-]+):\s*$", RegexOptions.Multiline);
            if (id.Success && id.Index == 0)
            {
                jobs[id.Groups[1].Value] = job;
            }
        }

        return jobs;
    }

    private static List<string> RunLines(string job) =>
        job.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("run: ", StringComparison.Ordinal)).ToList();
}
