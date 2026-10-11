// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE ORACLE'S POPULATION IS THE SUITES' POPULATION (kb/Work PB2116; docs/rearchitecture/
/// DESIGN-architecture-review.md §4): every <c>[PartitionedRowSource]</c> of this assembly is enrolled in
/// <see cref="ArchOracle.Sources"/>, every row it yields is an oracle case (only the empty-manifest sentinel is not),
/// two rows of one source are never one case, an observation carries no machine-specific path or clock reading, and
/// the recorded baseline is exactly one well-formed manifest — so a new golden, a new partitioned population or a
/// non-portable capture cannot escape the oracle a restructuring wave compares against.
/// </summary>
/// <remarks>The drift is pinned on the ENUMERATOR, not on the recorded manifest: the manifest is a dated baseline that
/// every golden-adding fix legitimately outgrows until it is re-recorded (by <c>scripts/arch/capture_oracle.py
/// --record</c>, after a fix-lane train), and <c>scripts/arch/compare_oracle.py</c> names each case added since as
/// <c>ADDED</c>.</remarks>
public sealed class ArchOracleDriftTests
{
    /// <summary>The partitioned theory families' row sources, from the partition audit's own census.</summary>
    [Fact]
    public void EveryPartitionedRowSource_IsEnrolledInTheOracle()
    {
        var audit = TestPartitionAudit.Audit(typeof(ArchOracle).Assembly);
        Assert.NotEmpty(audit.Families);
        var declared = audit.Families
            .SelectMany(f => f.RowSources.Select(m => $"{f.BaseDefinition.Name}.{m}"))
            .Order(StringComparer.Ordinal).ToList();
        var enrolled = ArchOracle.Sources.Select(s => $"{s.Family.Name}.{s.Member}").Order(StringComparer.Ordinal).ToList();
        var missing = declared.Except(enrolled).ToList();
        var stale = enrolled.Except(declared).ToList();
        Assert.True(missing.Count == 0 && stale.Count == 0,
            "the oracle's sources must be exactly the assembly's partitioned row sources.\n"
            + $"  not enrolled (add to ArchOracle.Sources with the compile its theory performs): {string.Join(", ", missing)}\n"
            + $"  enrolled but no longer a row source: {string.Join(", ", stale)}");
    }

    /// <summary>Count and identity: every non-sentinel row of every source is a case, one row is one case, and the
    /// population is exactly the union of the sources' cases, once each.</summary>
    [Fact]
    public void ThePopulation_IsTheSourcesRows_CaseForCase()
    {
        var union = new HashSet<string>(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (var source in ArchOracle.Sources)
        {
            var rowIds = new Dictionary<string, string>(StringComparer.Ordinal);
            int rows = 0;
            foreach (object[] row in source.Rows())
            {
                string display = string.Join(", ", row);
                var cases = source.ToCases(row).ToList();
                if (cases.Count == 0)
                {
                    if (row[0] is not ("shell" or "sentinel"))
                        problems.Add($"{source.Member}: row ({display}) yields no oracle case");
                    continue;
                }

                rows++;
                union.UnionWith(cases.Select(c => c.Id));
                // A row's own case is its last (a NIST row yields its chain predecessors first).
                if (!rowIds.TryAdd(cases[^1].Id, display))
                    problems.Add($"{source.Member}: rows ({rowIds[cases[^1].Id]}) and ({display}) are one case {cases[^1].Id}");
            }

            if (rows == 0) problems.Add($"{source.Member}: yields no rows at all — the oracle would assert nothing for it");
        }

        var population = ArchOracle.Population().Select(c => c.Id).ToList();
        Assert.Equal(population.Count, population.Distinct(StringComparer.Ordinal).Count());
        Assert.True(problems.Count == 0, string.Join("\n", problems));
        Assert.True(union.SetEquals(population),
            $"population and the sources' cases differ: {string.Join(", ", union.Except(population).Concat(population.Except(union)).Take(10))}");
    }

    /// <summary>An observation is the same on every machine and every run: no absolute repository or temporary path
    /// survives it, and a compilation that reads the compile clock is masked. Exercised on the three shapes that carry
    /// one — a run-time source position (EXCEPTION-LOCATION), a default-library search that names the working
    /// directory (CBL3620), and WHEN-COMPILED.</summary>
    [Fact]
    public void AnObservation_IsPortable_AndRepeatable()
    {
        var population = ArchOracle.Population().ToDictionary(c => c.Id, StringComparer.Ordinal);
        string Positive(string text) => population.Keys.First(id => id.StartsWith("corpus/", StringComparison.Ordinal)
            && File.ReadAllText(Path.Combine(ConformanceCorpus.Root, id["corpus/".Length..] + ".cob")).Contains(text));
        string missingLibrary = population.Keys.First(id => id.StartsWith("negative/", StringComparison.Ordinal)
            && File.ReadAllText(Path.Combine(ConformanceCorpus.Root, "negative", id["negative/".Length..] + ".err")).Contains("CBL3620"));

        string[] machineSpecific = [TestRepo.Root, Path.GetTempPath()];
        foreach (string id in new[] { Positive("EXCEPTION-LOCATION"), Positive("WHEN-COMPILED"), missingLibrary })
        {
            var first = population[id].Observe();
            var second = population[id].Observe();
            Assert.Equal(first, second);
            foreach (string text in new[] { first.Diagnostics, first.CSharp ?? "" })
            {
                foreach (string path in machineSpecific)
                {
                    string trimmed = Path.TrimEndingDirectorySeparator(path);
                    Assert.False(text.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                                 || text.Contains(trimmed.Replace(@"\", @"\\"), StringComparison.OrdinalIgnoreCase),
                        $"{id}: the observation carries the machine path {trimmed}");
                }
            }
        }
    }

    /// <summary>The recorded baseline is ONE manifest (two would leave a wave guessing which it is compared against),
    /// of the current schema, of a clean commit, with a SHA-256 per field of every case.</summary>
    [Fact]
    public void TheRecordedBaseline_IsOneWellFormedManifest()
    {
        string[] manifests = Directory.GetFiles(
            TestRepo.At("docs", "rearchitecture", "evidence", "arch-oracle"), "*.manifest.json");
        Assert.Single(manifests);
        using var doc = JsonDocument.Parse(File.ReadAllText(manifests[0]));
        var root = doc.RootElement;
        Assert.Equal(1, root.GetProperty("schema").GetInt32());
        Assert.False(root.GetProperty("dirty").GetBoolean());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("platform").GetString()));   // a wave compares one platform
        string commit = root.GetProperty("commit").GetString()!;
        Assert.Equal(commit[..12] + ".manifest.json", Path.GetFileName(manifests[0]));
        var sha = new Regex("^[0-9a-f]{64}$");
        int cases = 0;
        foreach (var c in root.GetProperty("cases").EnumerateObject())
        {
            cases++;
            Assert.Equal(2, c.Value.GetArrayLength());
            Assert.True(c.Value[0].ValueKind == JsonValueKind.Null || sha.IsMatch(c.Value[0].GetString()!), c.Name);
            Assert.Matches(sha, c.Value[1].GetString()!);
        }

        Assert.Equal(root.GetProperty("cases_total").GetInt32(), cases);
        Assert.Equal(cases, root.GetProperty("population").EnumerateObject().Sum(p => p.Value.GetInt32()));

        // Its INPUTS (kb/Work PB2885): the paths landing_oracle.py holds the baseline to at every landing. Each is a
        // real repository path, and every data root the row sources read now is among them (or under one).
        var inputs = root.GetProperty("inputs").EnumerateArray().Select(i => i.GetString()!).ToList();
        Assert.NotEmpty(inputs);
        Assert.All(inputs, i => Assert.True(File.Exists(TestRepo.At(i)) || Directory.Exists(TestRepo.At(i)), i));
        Assert.All(ArchOracle.DataInputs(), d => Assert.Contains(inputs, i => d == i || d.StartsWith(i + "/", StringComparison.Ordinal)));
    }

    /// <summary>Every enrolled source names the data its cases are read from, and each is a real repository path:
    /// a source with none would let its data change under a baseline that landing_oracle.py then calls current.</summary>
    [Fact]
    public void EveryRowSource_DeclaresTheDataItReads()
    {
        Assert.All(ArchOracle.Sources, s =>
        {
            Assert.NotEmpty(s.DataRoots);
            Assert.All(s.DataRoots, p => Assert.True(
                (File.Exists(p) || Directory.Exists(p)) && !Path.GetRelativePath(TestRepo.Root, p).StartsWith("..", StringComparison.Ordinal),
                $"{s.Member}: {p} is not a path inside the repository"));
        });
    }
}
