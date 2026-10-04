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
/// (folds the former <c>NistDifferentialTests</c> <c>[InlineData]</c> list, <c>tests/nist/chains.tsv</c>, and
/// <c>scripts/guard.sh</c> <c>LEGACY_DIVERGENT</c>). A green program is now a manifest ROW, not a code edit.
/// </summary>
public static class CorpusManifest
{
    private static string ManifestPath => TestRepo.Nist("corpus.tsv");

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
    /// program still matches its ISO golden in the greenfield harness; the flag only records that a LIVE legacy run
    /// would differ (the note cites the ISO § the legacy is non-conforming to).</summary>
    public static IEnumerable<CorpusRow> Green() => Rows.Where(r => r.Status is "green" or "divergent");

    /// <summary>name → chain predecessors (in run order) — replaces the private <c>Chains</c> lazy formerly in
    /// <c>NistDifferentialTests</c> (which read <c>chains.tsv</c> directly).</summary>
    public static IReadOnlyDictionary<string, string[]> Chains { get; } =
        Rows.Where(r => r.ChainPreds.Length > 0)
            .ToDictionary(r => r.Name, r => r.ChainPreds, StringComparer.OrdinalIgnoreCase);

    /// <summary>xunit <c>[MemberData]</c> source: the green∪divergent names (the NistDifferentialTests theory rows).</summary>
    public static IEnumerable<object[]> GreenData() => Green().Select(r => new object[] { r.Name });
}
