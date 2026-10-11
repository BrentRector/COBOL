// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The P3.6 VERSION_CHANGE_REFERENCE (VCR) audit gate — the Tier-1 STRUCTURAL spine that makes the ledger's status
/// DERIVED (never hand-ticked): each change row carries a machine anchor in its gating cell
/// (<c>&lt;!-- gate:construct-id --&gt;</c> / <c>ref-only</c> / <c>pin-to-spec</c> / <c>todo</c>), and the
/// generated "Gating status index" block (between <c>&lt;!-- GEN:VCR-STATUS START/END --&gt;</c>) is rendered from
/// those anchors + <c>constructs.json</c>. This test both RENDERS the block (write mode, via
/// <c>scripts/gen-vcr.ps1</c> → <c>COBOLNET_WRITE_VCR=1</c>) and, in normal CI, ASSERTS it is in sync, that every
/// <c>gate:</c> anchor resolves to a real construct (forward coverage), and that every SPEC CITATION resolves —
/// the clause exists and the appendix's quoted fragment is still inside it, the <c>cite.py --check</c> contract.
/// Mirrors <c>DiagnosticRegistryDriftTests</c> (ONE renderer, in the test).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class VcrDriftTests
{
    private const string Start = "<!-- GEN:VCR-STATUS START -->";
    private const string End = "<!-- GEN:VCR-STATUS END -->";

    private static string VcrPath => TestRepo.Docs("VERSION_CHANGE_REFERENCE.md");

    private sealed record Con(string Id, int IntroducedIn, int? RemovedIn, int? ObsoleteIn, string DiagnosticCode, string Status);

    private static Dictionary<string, Con> LoadConstructs()
    {
        string p = TestRepo.VersionMatrix("constructs.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(p));
        var d = new Dictionary<string, Con>(StringComparer.Ordinal);
        foreach (var e in doc.RootElement.GetProperty("constructs").EnumerateArray())
        {
            string id = e.GetProperty("id").GetString()!;
            d[id] = new Con(id,
                e.GetProperty("introducedIn").GetInt32(),
                e.TryGetProperty("removedIn", out var r) && r.ValueKind != JsonValueKind.Null ? r.GetInt32() : null,
                e.TryGetProperty("obsoleteIn", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetInt32() : null,
                e.GetProperty("diagnosticCode").GetString()!,
                e.TryGetProperty("status", out var s) && s.ValueKind != JsonValueKind.Null ? s.GetString()! : "active");
        }
        return d;
    }

    private static readonly Regex AnchorRx = new(@"<!--\s*((?:gate:[^\s>]+\s*)+)-->", RegexOptions.Compiled);
    // A VCR row number is `28`, `130e` (Tables 1–6) or `7.20` / `7.20a` (Table 7's dotted per-table numbering).
    // ⛔ The dotted form was NOT matched until kb/Work PB300, so every gate anchor on a Table 7 row was DEAD
    // MARKUP: invisible to the generated status index AND to EveryGateAnchor_ResolvesToARealConstruct, which
    // means a Table 7 anchor naming a construct that does not exist would never have been caught
    // (feedback_a_dead_lookup_is_also_unverified). Table 7 is where every pre-2023 delta lives, so this was the
    // half of the doc the forward-coverage check could not see.
    private static readonly Regex RowNumRx = new(@"^\|\s*([0-9]+(?:\.[0-9]+)?[a-z]?)\s*\|", RegexOptions.Compiled);

    /// <summary>Every VCR <c>gate:</c> anchor with the change row's number that carries it.</summary>
    private static List<(string Row, string[] Ids)> GateAnchors(string[] lines)
    {
        var res = new List<(string, string[])>();
        foreach (var ln in lines)
        {
            // ONLY real change-table rows carry anchors — a line must start "| <rownum> |". This excludes the
            // legend prose (which shows `<!-- gate:CONSTRUCT-ID -->` etc. as EXAMPLES) from being parsed as gates.
            var rm = RowNumRx.Match(ln);
            if (!rm.Success) continue;
            // ⛔ EVERY anchor comment on the row, not the first (kb/Work PB725). A row may write its gates as one
            // comment (`<!-- gate:a gate:b -->`) or as several (`<!-- gate:a --> <!-- gate:b -->`); both spellings
            // are in the doc and only the first was read, so row 28's second anchor —
            // `arithmetic-standard-2002`, Annex E.2 item 21's Standard Arithmetic leg — had been silently absent
            // from the generated index and from the forward-coverage check that this method feeds. A construct
            // MISSING from a coverage table looks exactly like a construct nobody claimed.
            var ids = AnchorRx.Matches(ln)
                .SelectMany(m => m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Where(t => t.StartsWith("gate:", StringComparison.Ordinal))
                .Select(t => t["gate:".Length..]).ToArray();
            if (ids.Length == 0) continue;
            res.Add((rm.Groups[1].Value, ids));
        }
        return res;
    }

    private static int RowSortKey(string row)
    {
        var digits = new string(row.TakeWhile(char.IsDigit).ToArray());
        return digits.Length > 0 ? int.Parse(digits) : int.MaxValue;
    }

    /// <summary>Render the generated status-index block body from the VCR anchors + the catalogue.</summary>
    private static string RenderIndex(string[] lines, Dictionary<string, Con> cons)
    {
        var byId = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var (row, ids) in GateAnchors(lines))
            foreach (var id in ids)
            {
                if (!byId.TryGetValue(id, out var set)) byId[id] = set = new SortedSet<string>();
                set.Add(row);
            }
        var sb = new StringBuilder();
        sb.Append("| Construct | Introduced | Removed/obsolete | Diagnostic | Status | VCR row(s) |\n");
        sb.Append("| --- | --- | --- | --- | --- | --- |\n");
        foreach (var (id, rows) in byId)
        {
            var c = cons[id];   // forward-coverage test guarantees existence; KeyNotFound here = a real drift
            string window = c.RemovedIn is { } r ? $"removed {r}"
                : c.ObsoleteIn is { } ob ? $"obsolete {ob}" : "—";
            string status = c.Status == "active" ? "done" : "pending";
            string rowList = string.Join(", ", rows.OrderBy(RowSortKey).ThenBy(x => x, StringComparer.Ordinal));
            sb.Append($"| {id} | {c.IntroducedIn} | {window} | {c.DiagnosticCode} | {status} | {rowList} |\n");
        }
        return sb.ToString().TrimEnd('\n');
    }

    [Fact]
    public void EveryGateAnchor_ResolvesToARealConstruct()
    {
        var cons = LoadConstructs();
        var bad = new List<string>();
        foreach (var (row, ids) in GateAnchors(File.ReadAllLines(VcrPath)))
            foreach (var id in ids)
                if (!cons.ContainsKey(id)) bad.Add($"row {row}: gate:{id}");
        Assert.True(bad.Count == 0,
            "VCR gate anchor(s) naming a construct absent from constructs.json (forward-coverage): " + string.Join(", ", bad));
    }

    // A clause number is either the body's dotted decimal (14.9.39.4) or an annex's letter-headed form
    // (E.2, A.4.14, D.2.2.5.1); the TITLE is optional because all 178 headings of clause 3 (Terms and
    // definitions) are a bare number. Same grammar as scripts/spec/cite.py — one definition of "a clause".
    private static readonly Regex SpecHeadingRx =
        new(@"^#{2,6}\s+([0-9]+(?:\.[0-9]+)*|[A-Z](?:\.[0-9]+)+)(?:\s+.*?)?\s*$", RegexOptions.Compiled);

    /// <summary>Every `§clause` mentioned anywhere in the VCR, with the line it sits on.</summary>
    private static readonly Regex SpecRefRx = new(@"§([0-9A-Z][0-9A-Za-z.]*)", RegexOptions.Compiled);

    /// <summary>An appendix citation: the clause, then the verbatim fragment that pins it inside the clause.</summary>
    private static readonly Regex CitationRx = new(@"§([0-9A-Z][0-9A-Za-z.]*)\s+`([^`]+)`", RegexOptions.Compiled);

    /// <summary>⛔ A LINE NUMBER IS NOT A CITATION — `@27372` and friends must never come back.</summary>
    private static readonly Regex LineRefRx = new(@"@[0-9]{3,6}(\s*[-–]\s*[0-9]{3,6})?", RegexOptions.Compiled);

    private static string SpecPath => TestRepo.Specs("ISO_COBOL.md");

    /// <summary>Compare on words only — dashes, quotes, emphasis and spacing are typography, not content.</summary>
    private static string NormalizeForCitation(string s) =>
        Regex.Replace(Regex.Replace(s, @"[^\w\s]", " "), @"\s+", " ").Trim().ToLowerInvariant();

    /// <summary>clause number → the normalized text of its own region, ending at the next clause heading.</summary>
    private static Dictionary<string, List<string>> SpecClauseRegions()
    {
        var lines = File.ReadAllLines(SpecPath);
        var heads = new List<(string Num, int At)>();
        for (int i = 0; i < lines.Length; i++)
            if (SpecHeadingRx.Match(lines[i]) is { Success: true } m)
                heads.Add((m.Groups[1].Value, i));

        var regions = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        for (int k = 0; k < heads.Count; k++)
        {
            int end = k + 1 < heads.Count ? heads[k + 1].At : lines.Length;
            if (!regions.TryGetValue(heads[k].Num, out var body)) regions[heads[k].Num] = body = new List<string>();
            for (int i = heads[k].At; i < end; i++) body.Add(NormalizeForCitation(lines[i]));
        }
        return regions;
    }

    /// <summary>
    /// Every spec reference in the VCR resolves: the clause exists, and where the appendix quotes the sentence
    /// the row was written from, that sentence is still inside that clause. This is the `cite.py --check`
    /// contract in the battery. ⛔ It replaced a check on spec LINE NUMBERS, which is what let ~180 citations
    /// rot silently — they pointed at the wrong sentence for months before they pointed past the end of the file.
    /// </summary>
    [Fact]
    public void EverySpecCitation_ResolvesInTheSpec()
    {
        Assert.True(File.Exists(SpecPath), $"specs/ISO_COBOL.md is missing at {SpecPath} — it is tracked in this repo, so this is not a skip");
        var regions = SpecClauseRegions();
        var lines = File.ReadAllLines(VcrPath);
        var bad = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            foreach (Match m in SpecRefRx.Matches(lines[i]))
            {
                string clause = m.Groups[1].Value.TrimEnd('.');
                if (!regions.ContainsKey(clause)) bad.Add($"line {i + 1}: §{clause} is not a clause of the standard");
            }
            foreach (Match m in CitationRx.Matches(lines[i]))
            {
                string clause = m.Groups[1].Value.TrimEnd('.');
                if (!regions.TryGetValue(clause, out var body)) continue;   // already reported above
                string needle = NormalizeForCitation(m.Groups[2].Value);
                if (!body.Any(l => l.Contains(needle, StringComparison.Ordinal)))
                    bad.Add($"line {i + 1}: §{clause} exists but does not contain `{m.Groups[2].Value}`");
            }
        }

        Assert.True(bad.Count == 0,
            "VERSION_CHANGE_REFERENCE.md spec citation(s) that do not resolve — re-derive with "
            + "`python scripts/spec/cite.py --find \"<text>\"`:\n  " + string.Join("\n  ", bad));
    }

    /// <summary>
    /// ⛔ A LINE NUMBER IS NOT A CITATION. The appendix carried `specLines` into the transcription and every one
    /// of them dangled once the spec was repaired and de-paged. Cite the CLAUSE — the standard's own identifier,
    /// which does not move — and quote the sentence. This test is what stops the habit returning.
    /// </summary>
    [Fact]
    public void NoSpecLineNumberIsCited_InTheVcr()
    {
        var lines = File.ReadAllLines(VcrPath);
        var bad = new List<string>();
        for (int i = 0; i < lines.Length; i++)
            foreach (Match m in LineRefRx.Matches(lines[i]))
            {
                // `@2023` / `@2014` name an EDITION, not a line.
                string digits = m.Value.TrimStart('@');
                if (digits.Length == 4 && digits[0] == '2' && int.Parse(digits) is >= 1985 and <= 2100) continue;
                bad.Add($"line {i + 1}: {m.Value}");
            }
        Assert.True(bad.Count == 0,
            "VERSION_CHANGE_REFERENCE.md cites spec LINE numbers, which do not survive a spec edit — "
            + "cite the clause and quote the sentence instead:\n  " + string.Join("\n  ", bad));
    }

    [Fact]
    public void GeneratedStatusIndex_IsInSync()
    {
        var lines = File.ReadAllLines(VcrPath);
        string rendered = RenderIndex(lines, LoadConstructs());
        string full = File.ReadAllText(VcrPath).Replace("\r\n", "\n");
        int s = full.IndexOf(Start, StringComparison.Ordinal);
        int e = full.IndexOf(End, StringComparison.Ordinal);
        Assert.True(s >= 0 && e > s, $"VCR generated-block markers not found ({Start} … {End})");

        if (Environment.GetEnvironmentVariable("COBOLNET_WRITE_VCR") == "1")
        {
            string updated = full[..(s + Start.Length)] + "\n" + rendered + "\n" + full[e..];
            File.WriteAllText(VcrPath, updated);
            return;
        }

        string committed = full[(s + Start.Length)..e].Trim('\n');
        Assert.True(committed == rendered,
            "docs/VERSION_CHANGE_REFERENCE.md gating status index is out of sync — regenerate: pwsh scripts/gen-vcr.ps1\n"
            + $"--- expected ---\n{rendered}\n\n--- committed ---\n{committed}");
    }
}
