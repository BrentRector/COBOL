// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Conformance;

/// <summary>One row of <c>tests/nist/corpus.tsv</c>.</summary>
/// <param name="Name">The NIST program name (e.g. <c>NC101A</c>).</param>
/// <param name="Suite">The suite prefix (<c>NC</c>/<c>ST</c>/<c>IX</c>/…).</param>
/// <param name="Status"><c>green</c> | <c>divergent</c> | <c>pending</c>.</param>
/// <param name="ChainPreds">Producer predecessors to run first (in order); empty for a standalone program.</param>
/// <param name="HasGolden">True iff a <c>tests/nist/valid/&lt;name&gt;.txt</c> golden exists.</param>
/// <param name="Note">A free-text note; for a <c>divergent</c> row this carries the ISO § citation.</param>
public sealed record CorpusRow(string Name, string Suite, string Status, string[] ChainPreds, bool HasGolden, string Note)
{
    /// <summary>The TERMINATES marker's grammar, written down ONCE for C#. <c>scripts/guard-population.sh</c>'s
    /// <c>GUARD_TERMINATES_MARKER</c> is the same grammar for the shell guards (kb/Work PB1955), and
    /// <c>CorpusManifestTests.TerminatesMarker_IsOneGrammarForBothReaders</c> holds the two equal.</summary>
    public const string TerminatesMarkerPattern = @"^TERMINATES (EC-[A-Z0-9-]+)";

    private static readonly System.Text.RegularExpressions.Regex TerminatesMarker =
        new(TerminatesMarkerPattern, System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>The exception-name a <c>divergent</c> row declares its program TERMINATES on: a note beginning
    /// <c>TERMINATES EC-…</c> (the marker, like <c>CCVS-DEFECT</c>, is a token of the note, and the row's ISO §
    /// citation is still enforced). The program depends on an implementor-defined continuation after a fatal I-O
    /// status (§9.1.13.1: "The implementor may either continue or terminate the execution of the run unit"; Annex A.1
    /// item 103 is WiseOwl COBOL's documented choice), so its golden records a run that continued and the run
    /// unit now ends abnormally instead. <c>null</c> for every other row.</summary>
    public string? ExpectedTermination =>
        Status == "divergent" && TerminatesMarker.Match(Note) is { Success: true } m ? m.Groups[1].Value : null;
}

/// <summary>
/// Loads <c>tests/nist/corpus.tsv</c> — the ONE source of truth for the green NIST set + producer/consumer chains
/// (folds the former <c>NistDifferentialTests</c> <c>[InlineData]</c> list and <c>tests/nist/chains.tsv</c>). A green
/// program is a manifest ROW, not a code edit.
/// </summary>
public static class CorpusManifest
{
    /// <summary>The NIST tree every NIST compile reads from (the manifest and the programs): the one spelling of the
    /// path, which the architecture oracle also records as an input (<see cref="ArchOracle.RowSource"/>).</summary>
    internal static string Root { get; } = TestRepo.Nist();

    private static string ManifestPath => Path.Combine(Root, "corpus.tsv");

    /// <summary>Every manifest row (comment/blank lines skipped).</summary>
    public static IReadOnlyList<CorpusRow> Rows { get; } = Load();

    private static IReadOnlyList<CorpusRow> Load()
    {
        var rows = new List<CorpusRow>();
        foreach (string raw in File.ReadLines(ManifestPath))
        {
            if (string.IsNullOrWhiteSpace(raw) || raw[0] == '#') continue;
            string[] f = raw.Split('\t');
            string[] preds = f[3] == "-" ? [] : f[3].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            rows.Add(new CorpusRow(f[0], f[1], f[2], preds, f[4] == "valid", f.Length > 5 ? f[5] : ""));
        }
        return rows;
    }

    /// <summary>The green ∪ divergent set — the programs <c>NistDifferentialTests</c> asserts. A <c>divergent</c>
    /// program matches its golden like a green one; the flag records that the golden departs from the output first
    /// recorded for it (a re-baseline to the conforming run, a CCVS defect, or a declared termination), and the
    /// note cites the ISO § that makes the golden right.</summary>
    public static IEnumerable<CorpusRow> Green() => Rows.Where(r => r.Status is "green" or "divergent");

    /// <summary>name → chain predecessors (in run order) — replaces the private <c>Chains</c> lazy formerly in
    /// <c>NistDifferentialTests</c> (which read <c>chains.tsv</c> directly).</summary>
    public static IReadOnlyDictionary<string, string[]> Chains { get; } =
        Rows.Where(r => r.ChainPreds.Length > 0)
            .ToDictionary(r => r.Name, r => r.ChainPreds, StringComparer.OrdinalIgnoreCase);

    /// <summary>xunit <c>[MemberData]</c> source: the green∪divergent names (the NistDifferentialTests theory rows).</summary>
    public static IEnumerable<object[]> GreenData() => Green().Select(r => new object[] { r.Name });

    /// <summary>The per-edition override for the INV-1-STRONG behavioral leg (the roadmap's fatal-challenge fix,
    /// attached to the P2.7 flip; promoted to a G7 exit criterion at Phase 8): <c>COBOLNET_NIST_STD</c>
    /// (85|2002|2014|2023, default 85) + <c>COBOLNET_NIST_PERMISSIVE=1</c> re-target the WHOLE golden run — e.g.
    /// <c>COBOLNET_NIST_STD=2023 COBOLNET_NIST_PERMISSIVE=1</c> compiles AND RUNS all 318 goldens at the
    /// shipping default edition in migration mode, asserting byte-identical output.</summary>
    internal static int GoldenRunEdition { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("COBOLNET_NIST_STD"), out int v) ? v : 85;

    /// <summary>The golden run's severity axis (<c>COBOLNET_NIST_PERMISSIVE=1</c>; see <see cref="GoldenRunEdition"/>).</summary>
    internal static bool GoldenRunPermissive { get; } =
        Environment.GetEnvironmentVariable("COBOLNET_NIST_PERMISSIVE") == "1";

    /// <summary>The compile options of the golden run (<c>NistDifferentialTests</c>, chain predecessors included) —
    /// <see cref="GoldenRunEdition"/> on <see cref="GoldenRunPermissive"/>'s axis.</summary>
    internal static CompilerDriver.Options CompileOptions(string testName, string dll) =>
        CompileOptions(testName, dll, GoldenRunEdition, GoldenRunPermissive, checkOnly: false);

    /// <summary>THE compile options of a NIST CCVS program — the X-card preprocessing its name switches on, its
    /// edition and severity axis, and whether the Roslyn backend runs — written once, so the golden run, the
    /// per-edition harness (<see cref="EditionHarness.CompileNist"/>) and the architecture review's oracle
    /// (<see cref="ArchOracle"/>, kb/Work PB2116) compile a NIST program identically.</summary>
    internal static CompilerDriver.Options CompileOptions(
        string testName, string dll, int edition, bool permissive, bool checkOnly) =>
        new(Path.Combine(Root, "programs", testName + ".cob"), dll, NistTestName: testName, DialectLevel: edition,
            Permissive: permissive, CheckOnly: checkOnly);
}
