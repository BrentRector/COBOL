// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// THE ARMS OF THIS CLASS are numbered as docs/rearchitecture/DESIGN-test-build-ci.md §3.14.4 numbers them, and each
// is ONE self-contained test method whose name starts with its arm, so the mechanisms that land them separately
// (DESIGN §3.14.9) merge by union:
//   (1) LegOf is total and deterministic; NameKey strips exactly the partition suffix ........... M12
//   (2) each orderer's output is a permutation of its input ...................................... M12
//   (3) run_gate_legs.py --self-test (M13) and test_population.py --self-test (M14) fire every arm
//   (4) every test project under tests/ that the gate runs links GateLegs.cs .................... M12
//   (5) no discovered display name in a gated assembly contains the repository root ............. M12
//   (6) every `dotnet test` caller outside the driver scrubs COBOLNET_GATE_* ..................... M14

using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ORDERED GATE IS SOUND BY CONSTRUCTION ONLY IF EVERY WHOLE-ASSEMBLY RUN PROVES ITS POPULATION AND NO RUN BUT
/// THE GATE'S LEGS CAN BE NARROWED (kb/Work PB1708, PB1718; DESIGN-test-build-ci.md §3.14.3–3.14.4): the one
/// population check (<c>scripts/test_population.py</c>) fires on a dropped, a duplicated and a foreign case, and every
/// <c>dotnet test</c> caller under <c>scripts/</c> or <c>.github/workflows/</c> scrubs the leg handshake and the
/// MSBuild VSTest properties from its environment.
/// </summary>
/// <remarks>
/// <para>
/// A run narrowed through the ENVIRONMENT is invisible to its own verdict line: <c>VSTestTestCaseFilter</c> (MSBuild
/// imports every environment variable as a property) narrowed Characterization from 33 cases to 31 in the run AND
/// in <c>--list-tests</c>, exit 0 — measured 2026-09-28, and a runsettings file named by <c>RunSettingsFilePath</c>
/// did the same. So the population is always listed SCRUBBED, and every other caller runs scrubbed too; the gate's
/// own three-variable handshake (M12) is the one narrowing that is ever meant to reach a test host.
/// </para>
/// </remarks>
public sealed class GateLegDriftTests
{
    private const string PopulationTool = "test_population.py";

    /// <summary>
    /// Arm (3), the population half: <c>test_population.py --self-test</c> passes and still drives every arm by
    /// name — a self-test quietly reduced to its happy path also exits 0.
    /// </summary>
    [Fact]
    public void Arm3_ThePopulationCheck_FiresEveryArmOnPlantedInputs()
    {
        string path = TestRepo.Scripts(PopulationTool);
        Assert.True(File.Exists(path), $"the population check is missing: {path}");

        ProcessObservation r = PythonInstrument.Run(path, "--self-test");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("SELF-TEST: PASS", StringComparison.Ordinal),
            $"`{PopulationTool} --self-test` failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
        foreach (string arm in new[]
                 {
                     "short: a dropped case is NEVER RAN",
                     "over: a case twice is RAN TWICE",
                     "shard overlap that KEEPS THE COUNT",
                     "skipped: a NotExecuted-only definition is counted skipped",
                     "definitions, not result names",
                     "a definition the listing never printed is NOT IN THE POPULATION",
                     "stopped early",
                     "two listings that disagree are an ERROR",
                     "an EMPTY population is an ERROR",
                     "`scrubbed <command>` runs the command without a planted handshake",
                     "audit-callers flags every unscrubbed shape",
                 })
        {
            Assert.True(r.Stdout.Contains("PASS  " + arm, StringComparison.Ordinal),
                $"`{PopulationTool} --self-test` no longer drives '{arm}' — a check that has never been seen to fail "
                + $"is not evidence.\n{r.Stdout}");
        }
    }

    /// <summary>
    /// Arm (6): every <c>dotnet test</c> caller under <c>scripts/</c> runs scrubbed, and no CI workflow sets a
    /// variable that narrows a test run.
    /// </summary>
    [Fact]
    public void Arm6_EveryDotnetTestCaller_ScrubsTheGateEnvironment()
    {
        ProcessObservation r = PythonInstrument.Run(TestRepo.Scripts(PopulationTool), "audit-callers");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== CALLER AUDIT: CLEAN", StringComparison.Ordinal),
            "A `dotnet test` caller can run with a gate leg handshake (COBOLNET_GATE_*) or a VSTest*/"
            + "RunSettingsFilePath variable in its environment, which narrows the run to a PART of its assembly while "
            + "it exits 0 (kb/Work PB1718). Run it as `python scripts/test_population.py scrubbed dotnet test …` "
            + "(a python caller passes `env=scrubbed_env()`), and never set one of those variables in a workflow.\n"
            + $"(exit {r.ExitCode})\n{r.Stdout}{r.Stderr}");

        // ⛔ THE SCAN MUST ASSERT ITS OWN POPULATION: the callers §3.14.3 names, plus the gate twins. A scan that
        // stopped seeing them would report a clean tree over nothing.
        foreach (string site in new[]
                 {
                     "scripts/battery.sh", "scripts/build-local.ps1", "scripts/build-local.sh", "scripts/gen-vcr.ps1",
                     "scripts/gen-diagnostics-doc.ps1", "scripts/spec/record_verdicts.py",
                     "scripts/spec/record_impact_map.py", ".github/workflows/build-and-test.yml",
                 })
        {
            Assert.True(r.Stdout.Contains("  site  " + site + "\n", StringComparison.Ordinal)
                        || r.Stdout.Contains("  site  " + site + "\r\n", StringComparison.Ordinal),
                $"the caller audit no longer sees {site} — the scan is broken, not the tree clean.\n{r.Stdout}");
        }
    }
}
