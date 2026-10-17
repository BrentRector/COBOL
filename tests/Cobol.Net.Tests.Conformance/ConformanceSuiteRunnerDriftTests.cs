// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The drift guard for the conformance suite's two runners (DESIGN-conformance-suite.md §5, §9; kb/Work PB3176): the
/// in-process runner (<see cref="ConformanceSuiteRunner"/>) and <c>conformance-suite/harness/run.py</c> driving the
/// <c>cobol</c> command through the WiseOwl adapter give EVERY test of the per-edition suites the same verdict, and
/// run exactly the same tests; and the two output normalizations, <see cref="CutRunner.Normalize"/> and
/// <c>run.py</c>'s <c>normalize()</c>, both map every case of the one shared fixture
/// (<c>conformance-suite/harness/normalization-fixture.json</c>) to its stated result; and the WiseOwl adapter runs a
/// program under the same default locales as <see cref="CutRunner"/>.
/// </summary>
/// <remarks>
/// <para><b>Why both runners exist and why they must agree.</b> <c>run.py</c> is what any compiler runs (Python standard
/// library only, z/OS included); the in-process runner is WiseOwl's fast one, compiling through the compiled-program
/// cache. Each states the verdict logic once in its own language, and each reads the adapter's diagnostic pattern and
/// the profile its own way; a difference between them would make WiseOwl's own gate green on a suite a user's
/// <c>run.py</c> run reports red. This test runs both on the same generated suites and names every test whose verdict
/// differs, with both reasons.</para>
/// <para><b>Its cost grows with the suite</b>: <c>run.py</c> starts one <c>cobol</c> process per compile. It runs the
/// whole suite while the suite is the seed; the move (slice S2, kb/Work PB3177) decides its shape at the corpus's
/// size.</para>
/// </remarks>
public sealed class ConformanceSuiteRunnerDriftTests
{
    [Fact]
    public void BothRunners_GiveEverySuiteTestTheSameVerdict()
    {
        string cli = TestRepo.SrcBin("Cobol.Net.Cli", OperatingSystem.IsWindows() ? "cobol.exe" : "cobol");
        Assert.True(File.Exists(cli),
            $"{cli} does not exist: the Conformance project builds the cobol command (its ProjectReference to "
            + "Cobol.Net.Cli) so that run.py can drive it");
        string scratch = CutRunner.NewTempDir("suitedrift");
        try
        {
            string suites = Path.Combine(scratch, "suites");
            ConformanceSuite.Generate(suites);
            string results = Path.Combine(scratch, "results");
            ProcessObservation run = PythonInstrument.Run(ConformanceSuite.Harness, "--compiler", ConformanceSuite.Compiler,
                "--set", $"cobol={cli}", "--suites", suites, "--out", results);
            // 0 GREEN, 1 RED (a FAIL: the in-process runner must then say FAIL too); 2 is a usage or configuration error.
            Assert.True(run.ExitCode is 0 or 1, $"run.py could not run the suite (exit {run.ExitCode}):\n{run.Stdout}\n{run.Stderr}");

            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(results, "results.json")));
            var theirs = doc.RootElement.GetProperty("tests").EnumerateArray().ToDictionary(
                t => (Edition: t.GetProperty("edition").GetString()!, Id: t.GetProperty("id").GetString()!),
                t => new SuiteResult(t.GetProperty("verdict").GetString()!, t.GetProperty("reason").GetString()!));

            var ours = ConformanceSuite.Manifest.Editions
                .SelectMany(e => e.Value.Values.Where(t => t.InSuite).Select(t => (Edition: e.Key, t.Id, Test: t)))
                .ToDictionary(x => (x.Edition, x.Id), x => ConformanceSuiteRunner.Judge(x.Edition, x.Test));

            Assert.NotEmpty(ours);
            var onlyOurs = ours.Keys.Except(theirs.Keys).Order().ToList();
            var onlyTheirs = theirs.Keys.Except(ours.Keys).Order().ToList();
            Assert.True(onlyOurs.Count == 0 && onlyTheirs.Count == 0,
                "the two runners ran different tests: the wiseowl manifest's in-suite tests and the per-edition suites "
                + $"disagree. Only in-process: {string.Join(", ", onlyOurs)}. Only run.py: {string.Join(", ", onlyTheirs)}.");

            var differing = ours.Where(o => o.Value.Verdict != theirs[o.Key].Verdict)
                .OrderBy(o => o.Key)
                .Select(o => $"  {o.Key.Edition}/{o.Key.Id}: in-process {o.Value.Verdict} ({o.Value.Reason}); "
                    + $"run.py {theirs[o.Key].Verdict} ({theirs[o.Key].Reason})")
                .ToList();
            Assert.True(differing.Count == 0,
                $"{differing.Count} test(s) of the suite have different verdicts in the two runners:\n{string.Join("\n", differing)}");
        }
        finally { CutRunner.TryDelete(scratch); }
    }

    /// <summary>The two runners run a program under the same default locales: <see cref="CutRunner.RunExit"/> pins
    /// both to <see cref="CutRunner.LocalePin"/>, and the WiseOwl adapter's <c>environment</c> must set the same pin
    /// for <c>run.py</c>, or a host's regional setting would reach one runner's verdict and not the other's
    /// (docs/CONFORMANCE.md DOC-A.1-118: the defaults are the environment's).</summary>
    [Fact]
    public void BothRunners_RunAProgramUnderTheSameDefaultLocales()
    {
        string adapter = Path.Combine(ConformanceSuite.Root, "adapters", ConformanceSuite.Compiler + ".json");
        using var doc = JsonDocument.Parse(File.ReadAllText(adapter));
        Assert.True(doc.RootElement.TryGetProperty("environment", out JsonElement environment),
            $"{adapter} sets no environment, so run.py runs a program under the host's default locales");
        foreach (string variable in new[] { CobolNet.Runtime.LocaleState.UserDefaultVariable, CobolNet.Runtime.LocaleState.SystemDefaultVariable })
        {
            Assert.True(environment.TryGetProperty(variable, out JsonElement value) && value.GetString() == CutRunner.LocalePin,
                $"{adapter} must set {variable} to {CutRunner.LocalePin}, the pin of CutRunner.RunExit");
        }
    }

    [Fact]
    public void BothNormalizations_MapTheSharedFixtureToItsStatedResults()
    {
        string fixture = TestRepo.At("conformance-suite", "harness", "normalization-fixture.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(fixture));
        var cases = doc.RootElement.GetProperty("cases").EnumerateArray()
            .Select(c => (Name: c.GetProperty("name").GetString()!, Input: c.GetProperty("input").GetString()!,
                Normalized: c.GetProperty("normalized").GetString()!))
            .ToList();
        Assert.NotEmpty(cases);

        // Python: run.py's own normalize(), imported from the harness, over the same inputs. json.dumps escapes every
        // non-ASCII character, so the console's encoding cannot alter what comes back.
        const string Code = "import json, sys; sys.path.insert(0, sys.argv[2]); import run; "
            + "cases = json.load(open(sys.argv[1], encoding='utf-8'))['cases']; "
            + "print(json.dumps([run.normalize(c['input']) for c in cases]))";
        ProcessObservation python = PythonInstrument.Run("-c", Code, fixture, Path.GetDirectoryName(ConformanceSuite.Harness)!);
        Assert.True(python.ExitCode == 0, $"run.py's normalize() could not be run on the fixture:\n{python.Stdout}\n{python.Stderr}");
        string[] pythonResults = JsonSerializer.Deserialize<string[]>(python.Stdout.Trim())!;
        Assert.Equal(cases.Count, pythonResults.Length);

        var wrong = new List<string>();
        for (int i = 0; i < cases.Count; i++)
        {
            var (name, input, normalized) = cases[i];
            string csharp = CutRunner.Normalize(input);
            if (csharp != normalized) wrong.Add($"  CutRunner.Normalize, '{name}': {JsonSerializer.Serialize(csharp)}");
            if (pythonResults[i] != normalized) wrong.Add($"  run.py normalize(), '{name}': {JsonSerializer.Serialize(pythonResults[i])}");
        }
        Assert.True(wrong.Count == 0,
            $"the output normalizations differ from the shared fixture {fixture} (each line: the normalization, the "
            + $"case, what it produced):\n{string.Join("\n", wrong)}");
    }
}
