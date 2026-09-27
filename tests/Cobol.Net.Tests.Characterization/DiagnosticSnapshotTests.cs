// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Characterization;

/// <summary>
/// Gate 2 (rearchitecture Phase 0): snapshot the WiseOwl COBOL DIAGNOSTIC surface for every corpus program — the outcome
/// (ok=true/false) plus the set of diagnostics a later refactor must NOT change. Uses a CheckOnly compile (no Roslyn).
/// A positive program snapshots as <c>ok=true</c> (usually zero diagnostics); a negative program snapshots its expected
/// failure. Seed/re-baseline with <c>COBOLNET_UPDATE_SNAPSHOTS=1</c> (local only — CI always compares).
/// </summary>
public sealed class DiagnosticSnapshotTests
{
    public static IEnumerable<object[]> Corpus() => CharacterizationCorpus.All();

    [Theory]
    [MemberData(nameof(Corpus))]
    public void Diagnostics_MatchSnapshot(string name, int edition, bool negative)
    {
        string path = CharacterizationCorpus.SourcePath(name, negative);
        var probe = CompilerProbe.Probe(path, edition, emit: false);
        string actual = Snapshot.FormatDiagnostics(probe.Ok, probe.Diagnostics);
        string file = System.IO.Path.Combine(CharacterizationCorpus.SnapshotDir, $"{name}.{edition}.diag.txt");
        Snapshot.Assert(file, actual);
    }
}
