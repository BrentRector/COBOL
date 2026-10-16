// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Editions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The P2.4/P2.5 drift check: <c>tests/version-matrix/reserved-words.json</c> (the canonical per-edition word
/// flags, emitted by <c>scripts/gen-reserved-words.ps1</c>) and the generated C# table
/// (<c>Validation/ReservedWords.Table.cs</c>, emitted by the SAME script) must agree in BOTH directions — a
/// hand edit to either, or a regeneration that touched only one, fails here. Content-filter rule: this test
/// never prints word lists — mismatches report counts and at most a few offending words.
/// </summary>
public sealed class ReservedWordsDriftTests
{
    private sealed record JsonRow(string word, bool r85, bool r2002, bool r2014, bool r2023, string confidence, string provenance);

    [Fact]
    public void Table_Matches_CanonicalJson_BothDirections()
    {
        string path = TestRepo.VersionMatrix("reserved-words.json");
        Assert.True(File.Exists(path), $"canonical json missing: {path} — run scripts/gen-reserved-words.ps1");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var rows = doc.RootElement.GetProperty("words").EnumerateArray()
            .Select(e => new JsonRow(
                e.GetProperty("word").GetString()!,
                e.GetProperty("r85").GetBoolean(),
                e.GetProperty("r2002").GetBoolean(),
                e.GetProperty("r2014").GetBoolean(),
                e.GetProperty("r2023").GetBoolean(),
                e.GetProperty("confidence").GetString()!,
                e.GetProperty("provenance").GetString()!))
            .ToDictionary(r => r.word, StringComparer.Ordinal);

        var table = ReservedWords.Entries.ToDictionary(e => e.Word, StringComparer.Ordinal);

        // Same universe (both directions via set equality on counts + membership).
        Assert.Equal(rows.Count, table.Count);
        var missingFromTable = rows.Keys.Where(w => !table.ContainsKey(w)).Take(3).ToList();
        var missingFromJson = table.Keys.Where(w => !rows.ContainsKey(w)).Take(3).ToList();
        Assert.True(missingFromTable.Count == 0 && missingFromJson.Count == 0,
            $"drift: json-only [{string.Join(", ", missingFromTable)}] table-only [{string.Join(", ", missingFromJson)}]");

        // Field-level equality per word.
        int mismatches = 0;
        string? firstMismatch = null;
        foreach (var (word, r) in rows)
        {
            var t = table[word];
            if (t.R85 != r.r85 || t.R2002 != r.r2002 || t.R2014 != r.r2014 || t.R2023 != r.r2023
                || t.Confidence != r.confidence || t.Provenance != r.provenance)
            {
                mismatches++;
                firstMismatch ??= word;
            }
        }
        Assert.True(mismatches == 0, $"drift: {mismatches} mismatching rows (first: {firstMismatch})");
    }

    [Fact]
    public void Table_SanityInvariants()
    {
        // The structural facts the generator guarantees — a regression here means the script or its inputs broke.
        var entries = ReservedWords.Entries;
        Assert.InRange(entries.Length, 420, 560);        // union of the four per-edition source lists
        Assert.Equal(entries.Length, entries.Select(e => e.Word).Distinct(StringComparer.Ordinal).Count());
        var high = entries.Where(e => e.Confidence == "high").ToList();
        // Annex E.2 item 25: the 16 words 2023 added, user-defined at 2014 (E.1 compares 2023 with the PREVIOUS
        // standard only, so the item says nothing about 2002; kb/Work PB656).
        Assert.Equal(16, high.Count(e => !e.R2014 && e.R2023));
        // The 1985 RE-reservations among the 16 — the communication trio incl. the END- scope terminator
        // (mechanically derived, DEVLOG 585; the recall-based classification knew only two of them), still reserved
        // at 2002 by the GnuCOBOL 2002 list like the rest of that facility's vocabulary (obsolete there; kb/Work PB656).
        Assert.Equal(3, high.Count(e => e.R85 && e.R2002 && !e.R2014 && e.R2023));
        Assert.True(high.Count(e => e.R85 && !e.R2023) >= 20, "the removed-post-85 group should be substantial");
        Assert.True(high.Count(e => !e.R85 && !e.R2002 && e.R2014 && e.R2023) >= 5,
            "the 2014 additions (IEEE family among them) should be present");
        Assert.All(entries, e => Assert.True(e.Word == e.Word.ToUpperInvariant(), $"not uppercase: {e.Word}"));
        Assert.All(entries, e => Assert.True(e.Confidence is "high" or "medium", $"bad confidence: {e.Word}"));
    }

    /// <summary>A row's provenance may not contradict its own flags (kb/Work PB656): the generator once labelled every
    /// word reserved at 85 and not at 2023 "removed post-85", whatever its 2002/2014 flags said, so 35 rows — COBOL and
    /// SEGMENT-LIMIT among them — read as unreserved at editions their flags reserve them at, and a reader of the
    /// label (the registrar's refuter on SR-8.3.2.1-2) could not tell which was wrong. Each label's claim about an
    /// edition is checked against that edition's flag.</summary>
    [Fact]
    public void Provenance_AgreesWithTheFlagsItDescribes()
    {
        var contradictions = ReservedWords.Entries.Where(e =>
                (e.Provenance.Contains("removed post-85", StringComparison.Ordinal) && (e.R2002 || e.R2014 || e.R2023))
                || (e.Provenance.Contains("unreserved 2002", StringComparison.Ordinal) && e.R2002)
                || (e.Provenance.Contains("unreserved 2014", StringComparison.Ordinal) && e.R2014)
                || (e.Provenance.Contains("removed by 2014", StringComparison.Ordinal) && (!e.R2002 || e.R2014))
                || (e.Provenance.Contains("through 2014", StringComparison.Ordinal) && !(e.R2002 && e.R2014))
                || (e.Provenance.StartsWith("1985", StringComparison.Ordinal) && !e.R85))
            .Select(e => e.Word).ToList();
        Assert.True(contradictions.Count == 0,
            $"{contradictions.Count} provenance label(s) contradict their flags (first: {contradictions.FirstOrDefault()}) — fix scripts/gen-reserved-words.ps1 step 5 and regenerate");
    }
}
