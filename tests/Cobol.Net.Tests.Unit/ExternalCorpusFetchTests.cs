// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GATE OVER THE FETCH THAT FEEDS <see cref="ExternalCorpusPopulationDriftTests"/> — kb/Work PB897.
/// </summary>
/// <remarks>
/// <para>
/// <c>scripts/fetch-gnucobol-tests.ps1</c> populates the GPL corpus that the population drift gate measures. It
/// carried three independent faults that together made that gate RED in every fresh worktree, permanently:
/// it DELETED <c>tests/external/gnucobol</c> before an extraction it then failed to perform; it passed the
/// archive as a drive-letter path, which GNU tar reads as <c>host:path</c> (<c>Cannot connect to E: resolve
/// failed</c>, exit 128) and Windows' bsdtar accepts; and Git-for-Windows' GNU tar cannot read a <c>.tar.xz</c>
/// at all because it shells out to an <c>xz</c> binary it does not ship. Which <c>tar</c> happened to be first
/// on PATH therefore decided whether the corpus existed, per machine — so the two population reds became "the
/// known worktree shape" every implementer brief told agents to ignore. <b>A red everyone is told to ignore is
/// a gate that has stopped gating</b> (<c>feedback_green_gates_arent_evidence</c>), which is why the fetch now
/// has a gate of its own.
/// </para>
/// <para>
/// The script's <c>-SelfTest</c> switch drives <c>Invoke-CorpusExtraction</c> — the REAL function the real fetch
/// calls, not a copy — against a synthetic <c>.tar.xz</c> in a temporary sandbox: no network, no repository
/// state, and the FAILURE branches are exercised, not merely the success one. This fact runs it. Asserting the
/// individual check lines (not just the verdict) is deliberate: a self-test quietly reduced to its happy path
/// would still print PASS.
/// </para>
/// <para>
/// A missing PowerShell is a LOUD failure and not a skip, exactly as a missing Python is for the drift gate
/// next door: <c>pwsh</c> is how both CI legs already run this script (<c>shell: pwsh</c> in
/// <c>.github/workflows/build-and-test.yml</c>), so a host without it cannot fetch the corpus either
/// (<c>feedback_verdict_evidence_invariant</c>).
/// </para>
/// </remarks>
public sealed class ExternalCorpusFetchTests
{
    /// <summary>The PowerShell that actually launches here, resolved once. <c>Observe</c> is the non-throwing
    /// form on purpose — a probe wants LaunchFailed REPORTED rather than raised and retried.</summary>
    private static readonly Lazy<string> PowerShell = new(() =>
    {
        foreach (string exe in new[] { "pwsh", "powershell" })
        {
            var psi = new ProcessStartInfo(exe);
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add("exit 0");
            if (ProcessObserver.Observe(psi, null, 60_000).Outcome != ProcessOutcome.LaunchFailed) return exe;
        }

        throw new InvalidOperationException(
            "neither `pwsh` nor `powershell` launches here — scripts/fetch-gnucobol-tests.ps1 is a PowerShell "
            + "instrument and both CI legs invoke it with `shell: pwsh`, so a host without one cannot populate "
            + "the external corpus at all. That is a hard failure rather than a skip on purpose: the defect "
            + "under repair (PB897) is precisely a corpus gate that reported nothing and was ignored.");
    });

    /// <summary>⛔ THE FETCH'S OWN GATE: the extraction is swap-on-success, and a FAILED extraction leaves the
    /// previous corpus intact. Both are driven, in both directions, by the script itself.</summary>
    [Fact]
    public void TheFetchScript_SelfTest_ProvesTheExtractionNeverDestroysWhatItCannotReplace()
    {
        string script = TestRepo.Scripts("fetch-gnucobol-tests.ps1");
        Assert.True(File.Exists(script), $"the corpus retrieval script is missing: {script}");

        var psi = new ProcessStartInfo(PowerShell.Value) { WorkingDirectory = TestRepo.Root };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(script);
        psi.ArgumentList.Add("-SelfTest");
        var r = ProcessObserver.ObserveOrThrow(psi);

        // The named checks, so a self-test reduced to its happy path cannot keep printing PASS. Each line is
        // one of PB897's faults, driven.
        foreach (string check in new[]
                 {
                     "a .tar.xz fixture can be produced",              // fault 3 — some tar here speaks xz
                     "a good archive extracts and swaps in",           // fault 2 — and fault 1 on a GNU-tar host
                     "the PREVIOUS corpus survived the failed extraction",   // fault 2 — the whole of it
                     "the corpus survived the missing-member failure",
                     "no staging directory is left behind",
                 })
        {
            Assert.True(r.Stdout.Contains(check, StringComparison.Ordinal),
                $"the fetch self-test no longer drives '{check}' — a check that was deleted cannot fail.\n"
                + $"{r.Stdout}{r.Stderr}");
        }

        Assert.True(r.Stdout.Contains("=== FETCH SELF-TEST: PASS", StringComparison.Ordinal),
            $"scripts/fetch-gnucobol-tests.ps1 -SelfTest did not pass — the external corpus fetch is broken, "
            + $"and ExternalCorpusPopulationDriftTests will be red in every fresh worktree until it is fixed "
            + $"(PB897).\nstdout:\n{r.Stdout}\nstderr:\n{r.Stderr}");
        Assert.Equal(0, r.ExitCode);
    }

    /// <summary>⛔ THE ONE CORPUS RULE FIRES EVERY ARM (kb/Work PB2611): <c>scripts/external_corpus.py --self-test</c>
    /// drives a present corpus (no fetch), an absent one fetched, and the three ways a fetch fails — non-zero exit,
    /// exit 0 with no corpus, and no <c>pwsh</c> at all — each the one named red, never a green and never a crash.</summary>
    [Fact]
    public void TheCorpusRule_SelfTest_FiresEveryArm()
    {
        string path = TestRepo.Scripts("external_corpus.py");
        Assert.True(File.Exists(path), $"the corpus rule is missing: {path}");

        ProcessObservation r = PythonInstrument.Run(path, "--self-test");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("external_corpus SELF-TEST: PASS", StringComparison.Ordinal),
            $"`external_corpus.py --self-test` failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
        foreach (string arm in new[]
                 {
                     "a present corpus runs NO fetch and is not red",
                     "an absent corpus runs the fetch script once, in that tree, and is not red when it lands",
                     "a fetch that exits non-zero is the named FETCH FAILED red",
                     "a fetch that exits 0 but leaves no marker is the named FETCH FAILED red, never a green",
                     "a fetch that cannot start (no pwsh) is the named FETCH FAILED red, never a crash",
                 })
        {
            Assert.True(r.Stdout.Contains("PASS  " + arm, StringComparison.Ordinal),
                $"`external_corpus.py --self-test` no longer drives '{arm}' — a check that has never been seen to "
                + $"fail is not evidence.\n{r.Stdout}");
        }
    }

    /// <summary>
    /// ⛔ ONE FETCH RULE IN ONE PLACE (kb/Work PB2611): no script under <c>scripts/</c> names the fetch script outside a
    /// comment except the rule itself (<c>scripts/external_corpus.py</c>) and the fetch script, so every gate, the
    /// battery, the impact-map recorder, the Linux gate and the cloud session hook obtain the corpus the same way.
    /// Battery #89 read NOT GREEN at a green tree because <c>battery.sh</c> had no fetch while the gate driver had one,
    /// and three more scripts each carried their own spelling of the rule. CI's two fetch steps are the one recorded
    /// exception: a fresh checkout never holds the corpus, and the step's exit status is the red.
    /// </summary>
    [Fact]
    public void OnlyTheCorpusRule_InvokesTheFetchScript()
    {
        const string Fetch = "fetch-gnucobol-tests.ps1";
        string[] owners = ["scripts/external_corpus.py", "scripts/" + Fetch];
        var seen = new List<string>();
        var strays = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Scripts(), "*", SearchOption.AllDirectories)
                     .Where(f => f.EndsWith(".py", StringComparison.Ordinal) || f.EndsWith(".sh", StringComparison.Ordinal)
                                 || f.EndsWith(".ps1", StringComparison.Ordinal) || f.EndsWith(".psm1", StringComparison.Ordinal)))
        {
            string rel = Path.GetRelativePath(TestRepo.Root, file).Replace('\\', '/');
            foreach (string line in File.ReadLines(file))
            {
                if (!line.Contains(Fetch, StringComparison.Ordinal) || line.TrimStart().StartsWith('#')) continue;
                if (owners.Contains(rel)) seen.Add(rel);
                else strays.Add($"{rel}: {line.Trim()}");
            }
        }

        Assert.True(seen.Contains("scripts/external_corpus.py"),
            "the scan no longer sees the rule's own reference to the fetch script — it is reading for a shape that no "
            + "longer exists, so it asserts nothing (feedback_a_dead_lookup_is_also_unverified).");
        Assert.True(strays.Count == 0,
            "a script names the corpus fetch directly instead of applying THE one rule (kb/Work PB2611): import "
            + "`external_corpus.ensure` (python) or run `python scripts/external_corpus.py ensure` (shell), which "
            + "fetches when absent and names a failed fetch `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED`.\n"
            + string.Join("\n", strays));
    }

    /// <summary>⛔ THE REPAIR THAT MUST NOT BE MADE. <c>--force-local</c> fixes GNU tar's <c>host:path</c>
    /// misreading and is rejected outright by Windows' bsdtar (<c>Option --force-local is not supported</c>,
    /// exit 1), so it converts a failure on one machine into a failure on the other. The fix is a BARE archive
    /// name relative to its own directory, which neither implementation misreads; this keeps the wrong repair
    /// from being re-introduced by someone reading only GNU tar's manual.</summary>
    [Fact]
    public void TheFetchScript_DoesNotPassForceLocal()
    {
        // Keyed on the INVOCATIONS, not on the file: the prose that explains why the flag is wrong legitimately
        // names it, and a test that banned the word would delete its own explanation.
        var invocations = File.ReadLines(TestRepo.Scripts("fetch-gnucobol-tests.ps1"))
            .Where(l => l.Contains("$tarExe ", StringComparison.Ordinal) || l.Contains("$mkTar ", StringComparison.Ordinal))
            .ToList();
        Assert.True(invocations.Count >= 2,
            "no tar invocation found in scripts/fetch-gnucobol-tests.ps1 — this test is reading for a shape that "
            + "no longer exists, so it is asserting nothing (feedback_a_dead_lookup_is_also_unverified).");
        foreach (string line in invocations)
        {
            Assert.False(line.Contains("--force-local", StringComparison.Ordinal),
                $"scripts/fetch-gnucobol-tests.ps1 passes --force-local to tar — bsdtar rejects it outright, so "
                + $"this moves the failure to the other machine rather than fixing it (PB897):\n{line}");
        }
    }
}
