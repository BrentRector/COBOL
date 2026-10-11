// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Editions.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The P2.10 diagnostic-registry drift check (mirrors <c>ConstructRegistryDriftTests</c> /
/// <c>ReservedWordsDriftTests</c>): the first-class <see cref="DiagnosticCatalog"/> is THE source, so
/// (a) every descriptor's stable <see cref="DiagnosticDescriptor.Id"/> is unique, (b) no compiler emit site
/// still uses a BARE split-code string literal where a descriptor now exists (the <c>COBOLNET0899</c> catch-all
/// and the reused <c>COBOLNET1533</c>), and (c) <c>docs/DIAGNOSTICS.md</c> is a RENDERED VIEW of the catalogue
/// (kb/Work PB3026, <c>scripts/rendered_views.py</c>): gitignored and never committed, because two pull requests that each
/// add a diagnostic would both rewrite it. This test is that view's render check: the catalogue must render one row per
/// descriptor. Render it: <c>pwsh scripts/gen-diagnostics-doc.ps1</c> (or run this test with
/// <c>COBOLNET_WRITE_DIAGNOSTICS_DOC=1</c>).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DiagnosticRegistryDriftTests
{
    /// <summary>The split codes the 0899/1533 migration moved onto descriptors — no bare
    /// <c>.Error("&lt;code&gt;"</c> literal for these may survive in the compiler.</summary>
    private static readonly string[] SplitCodes = ["COBOLNET0899", "COBOLNET1533"];

    /// <summary>Codes DELIBERATELY shared by multiple descriptors: the split families. Any other shared code is a
    /// collision — COBOLNET1535 was once listed here, its second descriptor (the staged RENAMES-in-TYPEDEF stage) having
    /// been a collision nobody had registered; the stage was implemented and the descriptor deleted (kb/Work PB1304,
    /// PB608), so the code now has one meaning like every other.</summary>
    private static readonly string[] SharedCodes = ["COBOLNET0899", "COBOLNET1533"];

    [Fact]
    public void EveryDescriptor_HasUniqueId()
    {
        var dupes = DiagnosticCatalog.All
            .GroupBy(d => d.Id, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        Assert.True(dupes.Count == 0, $"duplicate descriptor Id(s): [{string.Join(", ", dupes)}]");
    }

    /// <summary>No two descriptors may share a COBOLNET Code — one user-visible code, one meaning — except the
    /// deliberate split-code families (<see cref="SplitCodes"/>: many staged-loud descriptors share 0899/1533 by
    /// design). Id-uniqueness alone cannot catch a code collision (two kebab names, one code) — the shipped
    /// COBOLNET1573 collision class.</summary>
    [Fact]
    public void EveryDescriptor_HasUniqueCode_OutsideSplitFamilies()
    {
        var dupes = DiagnosticCatalog.All
            .Where(d => !SharedCodes.Contains(d.Code))
            .GroupBy(d => d.Code, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} [{string.Join(", ", g.Select(d => d.Id))}]")
            .ToList();
        Assert.True(dupes.Count == 0, $"descriptor code collision(s): {string.Join("; ", dupes)}");
    }

    [Fact]
    public void SplitCodes_HaveNoBareEmitLiteral_InCompiler()
    {
        string compiler = TestRepo.Src("Cobol.Net.Compiler");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(compiler, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                foreach (var code in SplitCodes)
                    if (lines[i].Contains($"Error(\"{code}\"", StringComparison.Ordinal))
                        offenders.Add($"{Path.GetFileName(file)}:{i + 1}");
        }
        Assert.True(offenders.Count == 0,
            $"bare split-code emit literal(s) — route through DiagnosticCatalog.Error(descriptor, …): "
            + string.Join(", ", offenders));
    }

    /// <summary>Every COBOLNETnnnn id string EMITTED anywhere in src (compiler, frontend, editions, runtime)
    /// must exist as a catalog descriptor — a bare literal outside the catalog is invisible to the next-free
    /// allocation scan and to DIAGNOSTICS.md, which is exactly how the shipped COBOLNET1573 two-meanings
    /// collision happened (the P13 plan-vs-spec review finding C1: the frontend's REF-MOD-ZERO-LENGTH
    /// malformed-operand emit bypassed the catalog, and Wave E's catalog-only scan re-allocated 1573).</summary>
    [Fact]
    public void EveryEmittedCode_IsACatalogDescriptor()
    {
        var known = DiagnosticCatalog.All.Select(d => d.Code).ToHashSet(StringComparer.Ordinal);
        var offenders = new List<string>();
        string src = TestRepo.Src();
        var emitRx = new System.Text.RegularExpressions.Regex(
            "Report(?:Error|Warning)\\(\\s*\"(COBOLNET\\d{4})\"|(?<![\\w.])(?:Error|Warning)\\(\\s*\"(COBOLNET\\d{4})\"");
        foreach (var file in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains("Generated", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                foreach (System.Text.RegularExpressions.Match match in emitRx.Matches(lines[i]))
                {
                    string code = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                    if (!known.Contains(code))
                        offenders.Add($"{Path.GetFileName(file)}:{i + 1} {code}");
                }
        }
        Assert.True(offenders.Count == 0,
            "emitted diagnostic code(s) with NO DiagnosticCatalog descriptor — register a descriptor (allocation "
            + "and DIAGNOSTICS.md are catalog-driven; a bare literal is how the 1573 collision shipped): "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void DiagnosticsDoc_RendersTheWholeCatalogue()
    {
        string rendered = RenderMarkdown();

        if (Environment.GetEnvironmentVariable("COBOLNET_WRITE_DIAGNOSTICS_DOC") == "1")
        {
            File.WriteAllText(TestRepo.Docs("DIAGNOSTICS.md"), rendered);
            return;
        }

        // The view is not compared with a copy on disk (a copy is not promised to be current, and is gitignored):
        // what can go wrong is the renderer, so each descriptor must come out as exactly one table row.
        string[] rows = rendered.Split('\n').Where(l => l.StartsWith("| ", StringComparison.Ordinal)).Skip(2).ToArray();
        Assert.Equal(DiagnosticCatalog.All.Count, rows.Length);
        Assert.All(DiagnosticCatalog.All, d =>
            Assert.Contains(rows, r => r.Contains($"| {Cell(d.Id)} |", StringComparison.Ordinal)));
    }

    /// <summary>Render the catalogue as the canonical <c>docs/DIAGNOSTICS.md</c> content.</summary>
    internal static string RenderMarkdown()
    {
        var sb = new StringBuilder();
        sb.Append("# WiseOwl COBOL diagnostics\n\n");
        sb.Append("> **Rendered view** of `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs` — gitignored, never committed or edited.\n");
        sb.Append("> Render: `pwsh scripts/gen-diagnostics-doc.ps1` (or run `DiagnosticRegistryDriftTests` with\n");
        sb.Append("> `COBOLNET_WRITE_DIAGNOSTICS_DOC=1`).\n\n");
        sb.Append("This catalogue is the first-class registry that replaced bare `COBOLNETnnnn` string literals\n");
        sb.Append("(rearch PHASE 02, P2.10). A **Code** may repeat across rows — the emitted number is byte-stable, the\n");
        sb.Append("stable **Id** is the identity. The `recognized-not-implemented` suppress key groups every\n");
        sb.Append("legal-but-deferred feature so it can be muted as a group. Scope: the edition band + the `COBOLNET0899`\n");
        sb.Append("split + the reused `COBOLNET1533`; the broader every-code→descriptor migration is the P7 follow-on.\n\n");
        sb.Append("| Code | Id | Severity | ISO § | Suppress key | Title |\n");
        sb.Append("| --- | --- | --- | --- | --- | --- |\n");
        foreach (var d in DiagnosticCatalog.All)
            sb.Append($"| {Cell(d.Code)} | {Cell(d.Id)} | {d.Severity} | {Cell(d.IsoSection)} "
                + $"| {Cell(d.ResolvedSuppressKey)} | {Cell(d.Title)} |\n");
        return sb.ToString();
    }

    /// <summary>Escape a markdown table cell (pipes + collapse any stray newlines).</summary>
    private static string Cell(string s) => s.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");
}
