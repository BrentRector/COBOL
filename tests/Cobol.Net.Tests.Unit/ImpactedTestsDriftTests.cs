// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE IMPACT MAP ORDERS THE GATE AND NEVER SELECTS IT (kb/Work PB1708, PB1717, PB1712):
/// <c>scripts/spec/impacted_tests.py</c> only TIERS a change and prints no vstest filter, even for a base WITH a map —
/// <c>scripts/gate_plan.py</c> gives every discovered case exactly one leg by its name key, the
/// partition suffix that key strips appears only on partition classes, and the recorder covers every product
/// assembly.
/// </summary>
/// <remarks>
/// <para>
/// PB1712 measured the SELECTION this replaced: the recorder cannot see static FIELD reads, so a change to one
/// <c>DiagnosticCatalog</c> descriptor selected 80 tests where the methods reporting it reach up to 9,225 — a silent
/// skip, armed the moment a map was recorded for a base. Under ordering the same blindness costs a later red, never a
/// missed one (DESIGN-test-build-ci.md sections 3.13 and 3.14.2).
/// </para>
/// <para>
/// Five facts are pinned. (1) Each script's own <c>--self-test</c> drives every arm by name — an arm deleted from it
/// is red here. (2) Through the REAL command line, against a map this test writes, a mapped change is TIERED (the
/// <c>--plan</c> file shows it) and nothing it prints is a vstest filter: the gate runs the whole population in
/// ordered legs, and M13 deleted the filter line and <c>--plus</c> with every caller (kb/Work PB1721). (3) An unmapped file puts every test in tier 1 and says why.
/// (4) <c>NameKey</c> strips <c>_P&lt;k&gt;</c> from a class segment, so that suffix must mean exactly "partition k
/// of a <c>TestPartitioning</c> family": every class so named in <c>tests/</c> is one. (5) Every product project
/// under <c>src/</c> is compiled with the probe and instrumented.
/// </para>
/// </remarks>
public sealed class ImpactedTestsDriftTests
{
    /// <summary>A vstest filter term — which the impact analysis must never print (kb/Work PB1721).</summary>
    private const string FilterTerm = "FullyQualifiedName";

    [Fact]
    public void ImpactedTestsSelfTest_DrivesEveryTierArm()
    {
        AssertSelfTest(TestRepo.Scripts("spec", "impacted_tests.py"),
            "a mapped file (file level) puts its tests in tier 2", "an UNMAPPED (new) src file puts every test in tier 1",
            "a known file no test executed", "an ambient-only file", "a grammar file", "a props file",
            "a runtime data file", "an added test method is named for tier 0a",
            "a hunk inside a reached method puts its tests in tier 1", "code inserted between methods is a declaration change",
            "a changed static constructor", "no map for the base puts every test in tier 1",
            "a documentation-only older map is used", "a STALE map is returned for its durations");
    }

    [Fact]
    public void GatePlanSelfTest_DrivesEveryArm()
    {
        AssertSelfTest(TestRepo.Scripts("gate_plan.py"),
            "NameKey strips the partition suffix", "NameKey never touches a theory's arguments",
            "tier 1: the cheapest cases fill leg 1 within LEG_ONE_BUDGET", "the collection cap",
            "tier 0a: an added test method, a touched golden and a previous red", "tiers 2 and 3 never enter leg 1",
            "tier 0u: unknown cases are charged the median", "a partition-renamed row keeps its timing",
            "no map: every case is tier 1", "a stale map", "no timings: leg 1 is tier 0a alone",
            "an empty leg 1 makes the assembly one leg", "an assembly whose whole recorded time fits the collection cap",
            "leg 2 runs collections longest first", "cases sharing a truncated name share one key",
            "a theory listed as one case", "leg_of is total", "a --list-tests output reads back as its display names",
            "the plan carries the SHA-256 of its content",
            // kb/Work PB2527: the shared timings store, so a fresh worktree's plan runs leg 2 longest first
            "the shared timings store: empty before any gate publishes",
            "a published gate's durations time a fresh worktree's plan",
            "timed from the store, leg 2 starts with the longest collection",
            "a malformed store is refused on read and REPLACED (named) on publish");
    }

    [Fact]
    public void AMappedChange_IsTiered_AndNoFilterIsPrinted()
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
            string plan = Path.Combine(dir, "tiers.json");
            var mapped = PythonInstrument.Run(script, "--map", map, "--plan", plan, "src/Cobol.Net.Compiler/Mapped.cs");
            Assert.Equal(0, mapped.ExitCode);
            Assert.DoesNotContain(FilterTerm, mapped.Stdout, StringComparison.Ordinal);
            Assert.Contains("impacted_tests: map exact", mapped.Stdout, StringComparison.Ordinal);
            using (var tiers = JsonDocument.Parse(File.ReadAllText(plan)))
            {
                // Named without a diff, the file is taken at the file level: its one test is tier 2 — the map WAS used.
                Assert.Equal(2, tiers.RootElement.GetProperty("tiers").GetProperty("Conformance").GetProperty("N.C.M").GetInt32());
                Assert.Equal(0, tiers.RootElement.GetProperty("every_tier1").GetArrayLength());
            }

            var unmapped = PythonInstrument.Run(script, "--map", map, "src/Cobol.Net.Compiler/NeverRecorded.cs");
            Assert.Equal(0, unmapped.ExitCode);
            Assert.DoesNotContain(FilterTerm, unmapped.Stdout, StringComparison.Ordinal);
            Assert.Contains("never seen", unmapped.Stderr, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ThePartitionSuffix_IsOnlyOnPartitionClasses()
    {
        // The twin first: the classifier accepts a partition and refuses any other `_P<k>` class. (The planted lines
        // are split so that this file's own scan below never matches them.)
        const string P = "_P";
        var planted = SuffixedClasses(
        [
            "public sealed class FooTests" + P + "2 : FooTestsBase<Slot2>;",
            "public sealed class BarTests" + P + "1 : BarTestsBase<Slot3>;",
            "internal sealed class Baz" + P + "0 { }",
            "/// <c>sealed class Family" + P + "3 : FamilyBase&lt;Slot3&gt;;</c>",
        ]).ToList();
        Assert.Equal([true, false, false], planted.Select(c => c.IsPartition));

        var violations = new List<string>();
        int partitions = 0;
        foreach (string file in Directory.EnumerateFiles(TestRepo.Tests(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Root, file).Replace('\\', '/');
            if (rel.Contains("/bin/", StringComparison.Ordinal) || rel.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var (declaration, isPartition) in SuffixedClasses(File.ReadLines(file)))
            {
                if (isPartition)
                {
                    partitions++;
                }
                else
                {
                    violations.Add($"{rel}: {declaration}");
                }
            }
        }

        Assert.True(partitions > 0, "no partition class found — this test is asserting nothing");
        Assert.True(violations.Count == 0,
            "gate_plan.py's NameKey strips `_P<k>` from a class name, so only a TestPartitioning partition may end in "
            + "it — another class so named would share its plan keys with a different class:\n  "
            + string.Join("\n  ", violations));
    }

    /// <summary>Every CODE line's class whose name ends in <c>_P&lt;k&gt;</c>, and whether it is a partition —
    /// <c>class Family_P&lt;k&gt; : FamilyBase&lt;Slot&lt;k&gt;&gt;</c> (TestPartitioning.cs). Comment lines are
    /// skipped: the partitioning docs describe that very shape.</summary>
    private static IEnumerable<(string Declaration, bool IsPartition)> SuffixedClasses(IEnumerable<string> lines)
    {
        var named = new Regex(@"\bclass\s+(?<name>\w+_P(?<k>\d+))\b(?<rest>[^;{]*)", RegexOptions.None, TimeSpan.FromSeconds(5));
        foreach (Match m in lines.Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal))
                     .SelectMany(l => named.Matches(l)))
        {
            string k = m.Groups["k"].Value;
            yield return ($"class {m.Groups["name"].Value}{m.Groups["rest"].Value.TrimEnd()}",
                Regex.IsMatch(m.Groups["rest"].Value, $@"^\s*:\s*\w+<Slot{k}>\s*$", RegexOptions.None, TimeSpan.FromSeconds(5)));
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
            // answers any change to it by putting every test in tier 1 instead.
            if (text.Contains("IsRoslynComponent", StringComparison.Ordinal)
                || text.Contains("netstandard2.0", StringComparison.Ordinal))
            {
                continue;
            }

            var asm = Regex.Match(text, "<AssemblyName>([^<]+)</AssemblyName>", RegexOptions.None, TimeSpan.FromSeconds(5));
            string assembly = asm.Success ? asm.Groups[1].Value : project;
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

    private static void AssertSelfTest(string script, params string[] arms)
    {
        Assert.True(File.Exists(script), $"missing: {script}");
        var r = PythonInstrument.Run(script, "--self-test");
        Assert.Equal(0, r.ExitCode);
        Assert.Contains("ALL GREEN", r.Stdout, StringComparison.Ordinal);
        foreach (string arm in arms)
        {
            Assert.True(r.Stdout.Contains("PASS  " + arm, StringComparison.Ordinal),
                $"`{Path.GetFileName(script)} --self-test` no longer drives '{arm}' — an arm never seen to fire is not "
                + $"evidence that it works.\n{r.Stdout}{r.Stderr}");
        }
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
}
