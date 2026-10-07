// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ARCHITECTURE CENSUS CANNOT SILENTLY SKIP A PROJECT (kb/Work PB2115; docs/rearchitecture/DESIGN-architecture-review.md
/// §3 R0): the newest committed census record under <c>docs/rearchitecture/evidence/arch-census/</c> covers every product
/// project of <c>CobolSharp.sln</c> (each <c>src/Cobol.Net.*</c> project), its census population equals the built
/// assembly's compiled type population for each, and its type table holds exactly that many rows per project; and
/// <c>scripts/arch/census.py --self-test</c> still drives every arm of the scope rule, the population check and the
/// dead-artifact queries.
/// </summary>
/// <remarks>
/// <para>
/// A census that measured five of six projects would print a smaller, cleaner codebase, and nothing downstream could
/// tell: the god-class table, the unreachable-member count and the Delete program's input would all simply be short.
/// The instrument refuses that at run time — the Roslyn host refuses a solution its scope does not partition, and
/// the script refuses a run whose census types differ from the types the built assembly's METADATA defines (an
/// independent reader). This test is the other half: it reads the committed record against the solution as it is
/// today, so a product project added after the record — or a record edited by hand — is red until the census is
/// re-run (<c>python scripts/arch/census.py</c>).
/// </para>
/// </remarks>
public sealed class ArchCensusDriftTests
{
    private static readonly Regex SolutionProject = new(
        "^Project\\(\"\\{[0-9A-Fa-f-]+\\}\"\\)\\s*=\\s*\"(?<name>[^\"]+)\",\\s*\"(?<path>[^\"]+\\.csproj)\"",
        RegexOptions.Multiline, TimeSpan.FromSeconds(5));

    [Fact]
    public void CensusSelfTest_DrivesEveryPolicyArm()
    {
        string script = TestRepo.Scripts("arch", "census.py");
        Assert.True(File.Exists(script), $"the census instrument is missing: {script}");

        var r = PythonInstrument.Run(script, "--self-test");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("SELF-TEST: PASS", StringComparison.Ordinal),
            $"`census.py --self-test` failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
        foreach (string arm in new[]
                 {
                     "a NEW src/Cobol.Net.* project joins the census by the rule",
                     "a project the rule does not classify REFUSES the run",
                     "a compiled type the census did not see FAILS the run",
                     "a census type the assembly lacks FAILS the run",
                     "an EMPTY built assembly FAILS the run",
                     "a script no file but itself names is unreferenced",
                     "a drift literal naming a missing path or file is dangling",
                 })
        {
            Assert.True(r.Stdout.Contains("PASS  " + arm, StringComparison.Ordinal),
                $"`census.py --self-test` no longer drives '{arm}' — an arm never seen to fire is not evidence.\n{r.Stdout}");
        }
    }

    [Fact]
    public void TheNewestRecord_CoversEveryProductProject_AndItsWholeCompiledPopulation()
    {
        var product = SolutionProject.Matches(File.ReadAllText(TestRepo.At("CobolSharp.sln")))
            .Where(m => m.Groups["name"].Value.StartsWith("Cobol.Net.", StringComparison.Ordinal)
                        && m.Groups["path"].Value.Replace('\\', '/').StartsWith("src/", StringComparison.Ordinal))
            .Select(m => m.Groups["name"].Value)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(product);

        using JsonDocument record = NewestRecord();
        JsonElement root = record.RootElement;
        string commit = root.GetProperty("commit").GetString()!;
        var census = root.GetProperty("scope").GetProperty("census").EnumerateArray()
            .Select(e => e.GetString()!).Order(StringComparer.Ordinal).ToList();
        Assert.True(product.SequenceEqual(census),
            $"the census record {commit[..12]} measured [{string.Join(", ", census)}] but CobolSharp.sln's product projects "
            + $"are [{string.Join(", ", product)}] — re-run `python scripts/arch/census.py` and commit its record");

        var rowsPerProject = new Dictionary<string, int>(StringComparer.Ordinal);
        int projectColumn = root.GetProperty("typeColumns").EnumerateArray().Select(e => e.GetString()).ToList().IndexOf("project");
        Assert.True(projectColumn >= 0, "the record's type table has no project column");
        foreach (JsonElement row in root.GetProperty("types").EnumerateArray())
        {
            string project = row[projectColumn].GetString()!;
            rowsPerProject[project] = rowsPerProject.GetValueOrDefault(project) + 1;
        }

        var population = root.GetProperty("population").EnumerateArray().ToList();
        Assert.Equal(census, population.Select(p => p.GetProperty("project").GetString()!).Order(StringComparer.Ordinal));
        foreach (JsonElement p in population)
        {
            string name = p.GetProperty("project").GetString()!;
            int counted = p.GetProperty("census").GetInt32();
            int compiled = p.GetProperty("compiled").GetInt32();
            Assert.True(compiled > 0 && counted == compiled && rowsPerProject.GetValueOrDefault(name) == compiled,
                $"{name}: the record counts {counted} census types and {rowsPerProject.GetValueOrDefault(name)} type rows "
                + $"against {compiled} compiled types — a census that skipped part of a project");
        }
    }

    /// <summary>The record with the latest commit date (records are named by the commit they measured).</summary>
    private static JsonDocument NewestRecord()
    {
        string dir = TestRepo.Docs("rearchitecture", "evidence", "arch-census");
        var records = Directory.EnumerateFiles(dir, "*.json")
            .Where(f => !f.EndsWith(".findings.json", StringComparison.Ordinal))
            .Select(f => (Path: f, Doc: JsonDocument.Parse(File.ReadAllText(f))))
            .ToList();
        Assert.True(records.Count > 0, $"no census record under {dir}");
        var newest = records.MaxBy(r => DateTimeOffset.Parse(r.Doc.RootElement.GetProperty("commitDate").GetString()!,
            System.Globalization.CultureInfo.InvariantCulture));
        foreach (var r in records.Where(r => r.Path != newest.Path))
        {
            r.Doc.Dispose();
        }

        return newest.Doc;
    }
}
