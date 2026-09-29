// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A GATE MAY TRIM A PASSING TEST LEG, NEVER A FAILING ONE (kb/Work/PB1573). <c>build-local.ps1</c> kept the
/// last 20 lines of a leg matching <c>^(Passed!|Failed!)|error|\[FAIL\]</c>: a failing test's NAME and its
/// <c>Error Message:</c> label survived, the message under the label and every stack frame did not, and every red
/// arrived unattributable (the PB1564 grammar-diagram red; a pb64t5 red under load). <c>build-local.sh</c> and
/// <c>guard-fast.sh</c> carried the same filter, the second in CI, where the full log dies with the runner.
/// </summary>
/// <remarks>
/// <para>
/// The rule now lives in ONE place, <c>scripts/test_leg_report.py</c>: a GREEN leg prints its verdict line, a RED
/// one its complete output. This class holds that shape in place from both ends — the reporter's own
/// <c>--self-test</c> must still prove, arm by arm, that a planted failure's message and stack reach the report
/// (and that both retired filters lose them, which is what makes the proof mean anything); and no script may
/// grow a keyword filter over test output again, which is how the defect was written three times.
/// </para>
/// </remarks>
public sealed class TestLegReportDriftTests
{
    private const string Reporter = "test_leg_report.py";

    /// <summary>
    /// The reporter's self-test passes, and still drives every arm by name — a self-test quietly reduced to its
    /// happy path also exits 0.
    /// </summary>
    [Fact]
    public void TheReporter_ProvesAPlantedFailuresTextReachesTheLog()
    {
        string path = TestRepo.Scripts(Reporter);
        Assert.True(File.Exists(path), $"the leg reporter is missing: {path}");

        ProcessObservation r = PythonInstrument.Run(path, "--self-test");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("SELF-TEST: PASS", StringComparison.Ordinal),
            $"`{Reporter} --self-test` failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
        foreach (string arm in new[]
                 {
                     "calibration: the retired build-local.ps1 filter DROPS the planted message",
                     "calibration: the retired build-local.sh filter DROPS the planted message",
                     "fires MESSAGE", "fires STACK", "fires WHOLE", "fires NO-VERDICT", "fires CLI",
                     "fires UNREADABLE", "silent on GREEN", "silent CLI",
                 })
        {
            Assert.True(r.Stdout.Contains("PASS  " + arm, StringComparison.Ordinal),
                $"`{Reporter} --self-test` no longer drives '{arm}' — a check that has never been seen to fail "
                + $"is not evidence.\n{r.Stdout}");
        }
    }

    /// <summary>
    /// ⛔ No script selects lines out of a test run's output by keyword. The retired shape — a verdict token
    /// (<c>Passed!</c>/<c>Failed!</c>) OR'd with a failure keyword (<c>error</c>, <c>[FAIL]</c>) — is exactly the
    /// filter that dropped every failure's message, and it was written three times before it was noticed. Code
    /// lines only (whole-line <c>#</c> comments are prose and quote the retired filter on purpose); the reporter
    /// itself is exempt, since its self-test re-implements both retired filters as its calibration.
    /// </summary>
    [Fact]
    public void NoScriptFiltersTestOutputByKeyword_EveryGateLegUsesTheReporter()
    {
        var retired = new Regex(@"(Passed!|Failed!).*(error|\[FAIL\])|(error|\[FAIL\]).*(Passed!|Failed!)",
            RegexOptions.CultureInvariant);
        var offenders = new List<string>();
        var callers = new List<string>();

        foreach (string file in Directory.EnumerateFiles(TestRepo.Scripts(), "*", SearchOption.AllDirectories))
        {
            string rel = "scripts/" + Path.GetRelativePath(TestRepo.Scripts(), file).Replace('\\', '/');
            if (rel.Contains("__pycache__", StringComparison.Ordinal) || rel.EndsWith(Reporter, StringComparison.Ordinal))
                continue;

            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith('#')) continue;
                if (retired.IsMatch(lines[i])) offenders.Add($"{rel}:{i + 1}: {lines[i].Trim()}");
                if (lines[i].Contains(Reporter, StringComparison.Ordinal) && !callers.Contains(rel)) callers.Add(rel);
            }
        }

        // ⛔ THE POPULATION: the two scripts that run and print `dotnet test` legs — the gate driver (both
        // build-local twins call it, kb/Work PB1721) and guard-fast.sh — must each route through the reporter. A scan
        // that stopped seeing them would otherwise report a clean tree over nothing.
        Assert.Contains("scripts/run_gate_legs.py", callers);
        Assert.Contains("scripts/guard-fast.sh", callers);

        Assert.True(offenders.Count == 0,
            "A script selects lines out of a test run's output by keyword — the filter that kept a failing test's "
            + "name and dropped its message and stack (kb/Work/PB1573). Pass the leg's log through "
            + $"scripts/{Reporter} instead: it prints a green leg's verdict and a red leg's COMPLETE output.\n  "
            + string.Join("\n  ", offenders));
    }
}
