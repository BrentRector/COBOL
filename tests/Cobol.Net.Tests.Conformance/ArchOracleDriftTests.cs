// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE ORACLE'S POPULATION IS THE SUITES' POPULATION (kb/Work PB2116; docs/rearchitecture/
/// DESIGN-architecture-review.md §4): every <c>[PartitionedRowSource]</c> of this assembly is enrolled in
/// <see cref="ArchOracle.Sources"/>, every row it yields is an oracle case (only the empty-manifest sentinel is not),
/// two rows of one source are never one case, and an observation carries no machine-specific path or clock reading —
/// so a new golden, a new partitioned population or a non-portable capture cannot escape the oracle a restructuring
/// wave compares against.
/// </summary>
/// <remarks>The drift is pinned on the ENUMERATOR, never on a recorded capture: nothing is committed (kb/Work PB3107).
/// A capture is a pure function of its commit, so <c>scripts/arch/compare_oracle.py</c> compares a change with the
/// capture of its own base commit, made on demand; a committed baseline could describe only the tree it was recorded
/// on, never the tree GitHub's merge queue builds a pull request into.</remarks>
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
        string Positive(string text) => population.Keys.First(id => id.Split('/') is ["suite", var edition, var test]
            && File.ReadAllText(Path.Combine(SuiteManifest.Root, SuiteManifest.Manifest.Editions[edition][test].Source))
                .Contains(text));
        string missingLibrary = population.Keys.First(id => id.StartsWith("refusal/", StringComparison.Ordinal)
            && SuiteManifest.Manifest.Refusals[id["refusal/".Length..].Split('@')[0]].Expect.Contains("CBL3620"));

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
}
