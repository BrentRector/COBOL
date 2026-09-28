// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE IMPACT-DERIVED GATE FILTER NEVER SILENTLY NARROWS (kb/Work PB1683): <c>scripts/spec/impacted_tests.py</c>
/// answers a change it cannot attribute — a file the recorded map has never seen, a grammar or build input, a file
/// no test executed, a missing or stale map — with the WHOLE-assembly filter, and the recorder covers every product
/// assembly.
/// </summary>
/// <remarks>
/// <para>
/// An implementer's gate filter is derived from the per-test impact map (DESIGN-test-build-ci.md §3.13) instead of
/// being guessed from names; train 68b dropped two groups on whole-assembly reds that name-guessed filters never ran.
/// The derivation is only safe while every arm that CANNOT attribute a change falls back to <c>~.</c>. A filter that
/// narrows past what the map proves is the PB708 false green in a new shape: a gate that passes because it ran the
/// wrong tests.
/// </para>
/// <para>
/// Three facts are pinned. (1) The script's own <c>--self-test</c> drives every conservative arm by name, so an arm
/// deleted from it is red here. (2) Through the REAL command line, against a map written by this test, an unmapped
/// source file prints <c>FullyQualifiedName~.</c> as the last stdout line — the line <c>build-local.ps1 -Filter</c>
/// consumes. (3) Every product project under <c>src/</c> is recorded: its project name is in
/// <c>tools/impact/ImpactRecording.targets</c> and its assembly name in <c>record_impact_map.py</c>'s <c>PROBED</c>
/// list — a new project not added to both would be invisible to the map, so every change to it would read as
/// "never seen" at best and, if the lists drifted apart, as "executed by no test".
/// </para>
/// </remarks>
public sealed class ImpactedTestsDriftTests
{
    private const string Whole = "FullyQualifiedName~.";

    [Fact]
    public void SelfTest_DrivesEveryConservativeArm()
    {
        string script = TestRepo.Scripts("spec", "impacted_tests.py");
        Assert.True(File.Exists(script), $"the impact lookup is missing: {script}");
        var r = PythonInstrument.Run(script, "--self-test");
        Assert.Equal(0, r.ExitCode);
        Assert.Contains("ALL GREEN", r.Stdout, StringComparison.Ordinal);
        foreach (string arm in new[]
                 {
                     "an UNMAPPED (new) src file", "a known file no test executed", "an ambient-only file",
                     "a grammar file", "a props file", "a runtime data file", "no map for the base",
                     "a mapped file selects its tests",
                     // the METHOD level: inside a method selects its tests; code between methods falls back
                     "a hunk inside a reached method selects its tests",
                     "code inserted between methods is a declaration change",
                 })
        {
            Assert.True(r.Stdout.Contains(arm, StringComparison.Ordinal),
                $"`impacted_tests.py --self-test` no longer drives '{arm}' — an arm never seen to fire is not "
                + $"evidence that the filter is conservative.\n{r.Stdout}{r.Stderr}");
        }
    }

    [Fact]
    public void AnUnmappedSourceFile_PrintsTheWholeAssemblyFilter()
    {
        string dir = Path.Combine(Path.GetTempPath(), "impact-drift-" + Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(dir);
        try
        {
            string map = Path.Combine(dir, "map.json.gz");
            // Schema 2 (record_impact_map.py): one method entry owning lines 1-5 of Mapped.cs, and one Conformance
            // test whose recorded bitset has that entry's bit (bit 0) set — little-endian bytes, zlib, base64.
            var content = new
            {
                schema = 2,
                commit = "0000000000000000000000000000000000000000",
                partial = false,
                entries = new object[] { new object[] { "N.C::M()", new object[] { new object[] { "src/Cobol.Net.Compiler/Mapped.cs", 1, 5 } } } },
                implies = new Dictionary<string, int[]>(),
                known_files = new[] { "src/Cobol.Net.Compiler/Mapped.cs" },
                tests = new object[][] { ["Conformance", "N.C.M", "N.C.M", "N.C", "Conformance:c", 1.0, true] },
                test_bits = new[] { Bitset(0x01) },
                class_bits = new Dictionary<string, string>(),
                collection_bits = new Dictionary<string, string>(),
                ambient_bits = "",
            };
            using (var gz = new GZipStream(File.Create(map), CompressionLevel.Fastest))
            {
                JsonSerializer.Serialize(gz, content);
            }

            string script = TestRepo.Scripts("spec", "impacted_tests.py");
            var unmapped = PythonInstrument.Run(script, "--map", map, "src/Cobol.Net.Compiler/NeverRecorded.cs");
            Assert.Equal(0, unmapped.ExitCode);
            Assert.Equal(Whole, LastLine(unmapped.Stdout));
            Assert.Contains("never seen", unmapped.Stderr, StringComparison.Ordinal);

            // The twin: the mapped file narrows — the fallback above is a decision, not the only answer it knows.
            var mapped = PythonInstrument.Run(script, "--map", map, "src/Cobol.Net.Compiler/Mapped.cs");
            Assert.Equal(0, mapped.ExitCode);
            string filter = LastLine(mapped.Stdout);
            Assert.NotEqual(Whole, filter);
            Assert.Contains("FullyQualifiedName=N.C.M", filter.Split('|'));
            Assert.Contains("FullyQualifiedName~Drift", filter.Split('|'));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void EveryProductProject_IsRecorded()
    {
        string targets = File.ReadAllText(TestRepo.At("tools", "impact", "ImpactRecording.targets"));
        string recorder = File.ReadAllText(TestRepo.Scripts("spec", "record_impact_map.py"));
        var probed = Regex.Match(recorder, @"PROBED = \[(?<body>[^\]]*)\]", RegexOptions.None, TimeSpan.FromSeconds(5));
        Assert.True(probed.Success, "record_impact_map.py no longer declares its PROBED list");
        var names = Regex.Matches(probed.Groups["body"].Value, "\"([^\"]+)\"", RegexOptions.None, TimeSpan.FromSeconds(5))
            .Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        var missing = new List<string>();
        foreach (string proj in Directory.EnumerateFiles(TestRepo.Src(), "*.csproj", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(proj);
            string project = Path.GetFileNameWithoutExtension(proj);
            // A Roslyn component (the source generator) runs inside the BUILD, not a test: impacted_tests.py
            // answers any change to it with the whole assembly instead.
            if (text.Contains("IsRoslynComponent", StringComparison.Ordinal)
                || text.Contains("netstandard2.0", StringComparison.Ordinal))
            {
                continue;
            }

            var asm = Regex.Match(text, "<AssemblyName>([^<]+)</AssemblyName>", RegexOptions.None, TimeSpan.FromSeconds(5));
            string assembly = asm.Success ? asm.Groups[1].Value : project;
            // A project whose own output is not loaded by any gated test (the legacy CLI) is exempt only by name
            // here, where the exemption is visible.
            if (project == "CobolSharp.CLI")
            {
                continue;
            }

            if (!targets.Contains($"'{project}'", StringComparison.Ordinal))
            {
                missing.Add($"{project}: not compiled with the probe (tools/impact/ImpactRecording.targets)");
            }

            if (!names.Contains(assembly))
            {
                missing.Add($"{assembly}: not instrumented (record_impact_map.py PROBED)");
            }
        }

        Assert.True(missing.Count == 0,
            "a product project the impact map does not record makes every change to it unattributable:\n  "
            + string.Join("\n  ", missing));
    }

    private static string Bitset(byte bits)
    {
        using var ms = new MemoryStream();
        using (var z = new ZLibStream(ms, CompressionLevel.Fastest))
        {
            z.WriteByte(bits);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    private static string LastLine(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? "";
}
