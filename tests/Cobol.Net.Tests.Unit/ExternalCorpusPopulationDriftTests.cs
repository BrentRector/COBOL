// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GATE THAT KEEPS "THE CORPUS" ONE POPULATION — PB209.
/// </summary>
/// <remarks>
/// <para>
/// The reachability question ("does any program we test against have this shape?") and the external differential
/// gate ("compile all 1,323 of them") used to measure DIFFERENT populations through DIFFERENT readers. The sweep
/// spelled the corpus as <c>find tests/external/gnucobol -name '*.cob'</c>, which finds exactly TWO files,
/// because the 1,323 GnuCOBOL programs live as <c>AT_DATA</c> heredocs inside 36 <c>.at</c> autotest wrappers.
/// Two landing waves therefore shipped new bind-time REJECTIONS on a blast radius stated as empty, and the very
/// next differential found both shapes in the corpus the sweep was named after — one of them in a case titled
/// <i>"REDEFINES: with OCCURS"</i> (§13.18.44.3 SR5 sentence 1 → COBOLNET1701; §13.18.63.3 SR13 sentence 2 →
/// COBOLNET1702).
/// </para>
/// <para>
/// The structural fix is ONE extractor (<c>scripts/gnucobol_extract.py</c>: <c>differential_cases</c> and
/// <c>iter_programs</c>) shared by <c>scripts/gnucobol_differential.py</c> and <c>scripts/corpus_sweep.py</c>.
/// This class is the "automatic" that keeps it true (CLAUDE.md rule 5): the sweep's external population,
/// recomputed live from the <c>.at</c> wrappers, must equal the differential's COMMITTED per-case baseline. The
/// two sides are produced independently — one read live from the corpus, one written by the gate and committed —
/// so their agreement is evidence rather than a tautology. If either reader stops seeing the corpus, this is red.
/// </para>
/// <para>
/// ⚠ <see cref="TheDriftCheck_ActuallyFails_WhenTheExtractionIsEmptied"/> is not a formality
/// (<c>feedback_green_gates_arent_evidence</c>). A check observed only passing is indistinguishable from one
/// that inspects nothing — and "inspects nothing, reports a clean zero" is exactly the defect being closed. It
/// drives the same instrument against an emptied extraction and requires it to go red AND to refuse to report
/// hit counts.
/// </para>
/// <para>
/// The sweep is a Python instrument, so this gate runs it. A missing interpreter is a LOUD failure and not a
/// skip: every gate in this repo already depends on Python, and a silent green from a check that never ran is
/// the failure mode under repair (<c>feedback_verdict_evidence_invariant</c>).
/// </para>
/// </remarks>
public sealed class ExternalCorpusPopulationDriftTests
{
    private const string Baseline = "gnucobol-verdict-baseline.tsv";

    /// <summary>
    /// The interpreter name that actually launches here, resolved once. <c>ProcessObserver.Observe</c> is the
    /// non-throwing form on purpose — a probe wants LaunchFailed REPORTED, not raised and retried.
    /// </summary>
    private static readonly Lazy<string> Python = new(() =>
    {
        foreach (string exe in new[] { "python", "python3" })
        {
            var psi = new ProcessStartInfo(exe);
            psi.ArgumentList.Add("--version");
            if (ProcessObserver.Observe(psi, null, 30_000).Outcome != ProcessOutcome.LaunchFailed) return exe;
        }

        throw new InvalidOperationException(
            "neither `python` nor `python3` launches here — scripts/corpus_sweep.py is a Python instrument and "
            + "this gate cannot run without it. That is a hard failure rather than a skip on purpose: an unrun "
            + "population check reporting green is precisely the defect (PB209) this test exists to prevent.");
    });

    private static ProcessObservation RunSweep(params string[] args)
    {
        string script = TestRepo.Scripts("corpus_sweep.py");
        Assert.True(File.Exists(script), $"the sweep instrument is missing: {script}");

        var psi = new ProcessStartInfo(Python.Value) { WorkingDirectory = TestRepo.Root };
        psi.ArgumentList.Add(script);
        foreach (string a in args) psi.ArgumentList.Add(a);
        // ProcessObserver decodes both streams as UTF-8; tell CPython to encode them that way rather than
        // falling back to the host ANSI code page, or a corpus title with a non-ASCII byte reads back mangled.
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        return ProcessObserver.ObserveOrThrow(psi);
    }

    private static JsonElement JsonLineOf(ProcessObservation r)
    {
        string? line = r.Stdout.Split('\n').Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("JSON ", StringComparison.Ordinal));
        Assert.True(line is not null,
            $"corpus_sweep.py --json emitted no JSON line.\nstdout:\n{r.Stdout}\nstderr:\n{r.Stderr}");
        return JsonDocument.Parse(line!["JSON ".Length..]).RootElement.Clone();
    }

    /// <summary>The differential's <c>cases run</c>, as COMMITTED — one data row per case.</summary>
    private static int BaselineCaseCount()
    {
        string path = TestRepo.Tests("external", Baseline);
        Assert.True(File.Exists(path), $"the per-case verdict baseline is missing: {path}");
        return File.ReadLines(path).Count(l => l.Trim().Length > 0 && !l.StartsWith('#'));
    }

    /// <summary>⛔ THE INVARIANT: the sweep reads the SAME external population the differential compiles.</summary>
    [Fact]
    public void SweepExternalPopulation_EqualsTheDifferentialsCommittedCaseCount()
    {
        int committed = BaselineCaseCount();
        var r = RunSweep("--verify-population", "--json");
        var j = JsonLineOf(r);
        string state = j.GetProperty("state").GetString()!;

        Assert.True(state != "absent",
            "the external GnuCOBOL corpus is not present, so this gate could not measure the population it "
            + "exists to measure. Run python scripts/external_corpus.py ensure (GPL, never committed). A missing "
            + $"population is not an empty one, and it is not a pass.\n{r.Stdout}{r.Stderr}");

        int external = j.GetProperty("external").GetInt32();
        Assert.True(external == committed,
            $"POPULATION DRIFT: scripts/corpus_sweep.py reads {external} external case(s) live from the .at "
            + $"wrappers, but tests/external/{Baseline} records {committed}. The reachability sweep and the "
            + "differential gate are measuring different corpora again — that is PB209 recurring, and every "
            + $"\"zero candidates\" claim made against the external corpus while this is red is worthless.\n"
            + $"{r.Stdout}{r.Stderr}");
        Assert.Equal(committed, j.GetProperty("baseline").GetInt32());
        Assert.Equal("ok", state);
        Assert.Equal(0, r.ExitCode);
    }

    /// <summary>
    /// ⛔ THE EVIDENCE THAT THIS GATE INSPECTS ANYTHING. Driven against an emptied extraction the instrument must
    /// go RED, and must REFUSE to report hit counts rather than return the clean zero that let two rejections
    /// ship on a blast radius nobody had measured.
    /// </summary>
    [Fact]
    public void TheDriftCheck_ActuallyFails_WhenTheExtractionIsEmptied()
    {
        string empty = Path.Combine(Path.GetTempPath(), "cobolnet-pb209-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(empty);
        try
        {
            // (a) the population check alone goes red, and NAMES the drift rather than reporting a zero.
            var check = RunSweep("--verify-population", "--json", "--src", empty);
            var j = JsonLineOf(check);
            Assert.Equal("drift", j.GetProperty("state").GetString());
            Assert.Equal(0, j.GetProperty("external").GetInt32());
            Assert.Equal(BaselineCaseCount(), j.GetProperty("baseline").GetInt32());
            Assert.NotEqual(0, check.ExitCode);
            Assert.Contains("POPULATION DRIFT", check.Stdout, StringComparison.Ordinal);

            // (b) and a PATTERN sweep over that broken population refuses to answer at all.
            var sweep = RunSweep("--pattern", "REDEFINES", "--src", empty);
            Assert.NotEqual(0, sweep.ExitCode);
            Assert.Contains("REFUSING TO REPORT HITS", sweep.Stderr, StringComparison.Ordinal);
            Assert.DoesNotContain("=== SWEEP:", sweep.Stdout, StringComparison.Ordinal);

            // …while the census is still printed, so a population contributing nothing can never be invisible.
            Assert.Contains("SWEEP POPULATION CENSUS", sweep.Stdout, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(empty, recursive: true); } catch (IOException) { /* best effort */ }
        }
    }

    /// <summary>
    /// The census always names every population and its program count — the omission that let a contribution of
    /// TWO files be written up as "the GnuCOBOL testsuite sources".
    /// </summary>
    [Fact]
    public void TheCensus_NamesEveryPopulationAndItsProgramCount()
    {
        var r = RunSweep();
        Assert.Equal(0, r.ExitCode);
        foreach (string pop in new[]
                 {
                     "conformance", "nist-programs", "nist-copylib", "characterization",
                     "version-matrix", "differential", "gnucobol-external",
                 })
        {
            Assert.Contains(pop, r.Stdout, StringComparison.Ordinal);
        }

        // The external line reports PROGRAMS extracted from .at wrappers — at least one per case, and never the
        // 2 that a *.cob glob over the very same tree returns.
        string line = r.Stdout.Split('\n').First(l => l.Contains("gnucobol-external", StringComparison.Ordinal));
        int count = int.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
        Assert.True(count >= BaselineCaseCount(),
            $"the external population line reports {count} program(s) — fewer than the {BaselineCaseCount()} "
            + "cases the differential compiles, so the sweep is globbing files again instead of extracting "
            + $"AT_DATA.\n{line}");
    }
}
