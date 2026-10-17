// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Common;
using CobolNet.Tests.Shared;
using Xunit;
using FrontendDiagnostic = CobolNet.Frontend.Diagnostics.Diagnostic;
using FrontendDescriptors = CobolNet.Frontend.Diagnostics.DiagnosticDescriptors;
using FrontendDescriptor = CobolNet.Frontend.Diagnostics.DiagnosticDescriptor;
using FrontendSeverity = CobolNet.Frontend.Diagnostics.DiagnosticSeverity;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The drift guard for WiseOwl COBOL's conformance-suite adapter, <c>conformance-suite/adapters/wiseowl.json</c>
/// (DESIGN-conformance-suite.md §5): its <c>diagnostic</c> pattern, which <c>run.py</c> reads a flagged test's
/// diagnostics with, matches every diagnostic the compiler can report, as the compiler's own formatters render it —
/// the parse layer's <see cref="FrontendDiagnostic"/> (the COBOL<i>n</i> parser and CBL<i>n</i> COPY codes, and the
/// parse-layer COBOLNET twins) and the binder's <see cref="EditionContext"/> (every <see cref="DiagnosticCatalog"/>
/// code) — and reads the file and the line from it. A pattern that missed a code would judge a flagged test FAIL
/// ("no diagnostic reported") although the compiler flagged the marked line: the pattern once required COBOLNET and
/// so never saw a parse error (kb/Work PB3175, review R1-2).
/// </summary>
public sealed class ConformanceSuiteAdapterDriftTests
{
    /// <summary>The adapter's pattern, a Python regular expression, read as .NET's: the two agree on everything the
    /// pattern uses but the named group, <c>(?P&lt;name&gt;</c> in Python and <c>(?&lt;name&gt;</c> in .NET.</summary>
    private static Regex AdapterPattern()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.At("conformance-suite", "adapters", "wiseowl.json")));
        string python = doc.RootElement.GetProperty("diagnostic").GetString()!;
        return new Regex(python.Replace("(?P<", "(?<", StringComparison.Ordinal), RegexOptions.CultureInvariant);
    }

    /// <summary>⛔ THE OBLIGATION, with its population: both registries enumerate to their known size and span the three
    /// code families, and every code, at every severity it can be reported at, rendered by its own formatter, is
    /// matched with its file and line.</summary>
    [Fact]
    public void TheWiseOwlAdapterPattern_MatchesEveryDiagnosticTheCompilerReports()
    {
        var pattern = AdapterPattern();
        var frontend = typeof(FrontendDescriptors).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(FrontendDescriptor))
            .Select(f => ((FrontendDescriptor)f.GetValue(null)!).Code).Distinct(StringComparer.Ordinal).ToArray();
        var catalog = DiagnosticCatalog.All.Select(d => d.Code).Distinct(StringComparer.Ordinal).ToArray();
        Assert.True(frontend.Length >= 20, $"DiagnosticDescriptors enumerated to {frontend.Length} code(s); the reflection is broken");
        Assert.True(catalog.Length >= 500, $"DiagnosticCatalog.All enumerated to {catalog.Length} code(s)");
        foreach (string family in new[] { "COBOL0", "CBL", "COBOLNET" })
            Assert.Contains(frontend.Concat(catalog), c => c.StartsWith(family, StringComparison.Ordinal));

        var problems = new List<string>();
        void Expect(string rendered, string code, string file, int? line)
        {
            var m = pattern.Match("  " + rendered);   // the CLI indents a warning line by two spaces
            if (!m.Success)
                problems.Add($"{code}: no match for {rendered}");
            else if (Path.GetFileName(m.Groups["file"].Value.Trim()) != file)
                problems.Add($"{code}: file {m.Groups["file"].Value} read from {rendered}");
            else if (line is { } want && m.Groups["line"].Value != want.ToString(System.Globalization.CultureInfo.InvariantCulture))
                problems.Add($"{code}: line {m.Groups["line"].Value} read from {rendered}, not {want}");
        }

        var location = new SourceLocation("PROG.cob", 0, 3, 11);   // 0-based line 3 renders as line 4
        foreach (string code in frontend.Concat(catalog))
            foreach (var severity in new[] { FrontendSeverity.Error, FrontendSeverity.Warning, FrontendSeverity.Info })
                Expect(new FrontendDiagnostic(code, severity, "a message", location, default).ToString(), code, "PROG.cob", 4);

        foreach (string code in catalog)
        {
            var edition = new EditionContext(2023) { SourceFile = "PROG.cob", Cursor = new DiagnosticCursor(4, 12) };
            edition.Error(code, "a message");
            edition.Warning(code, "a message");
            foreach (string rendered in edition.Diagnostics.Concat(edition.Warnings))
                Expect(rendered, code, "PROG.cob", null);
            if (edition.Diagnostics.Count + edition.Warnings.Count != 2)
                problems.Add($"{code}: EditionContext recorded {edition.Diagnostics.Count + edition.Warnings.Count} line(s), not 2");
        }

        Assert.True(problems.Count == 0,
            "conformance-suite/adapters/wiseowl.json's diagnostic pattern misses diagnostics the compiler reports "
            + $"({problems.Count}):\n  " + string.Join("\n  ", problems.Take(20)));
    }
}
