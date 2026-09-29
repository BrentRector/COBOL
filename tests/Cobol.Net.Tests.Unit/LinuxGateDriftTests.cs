// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE LOCAL LINUX GATE RUNS WHAT CI'S LINUX JOBS RUN (kb/Work PB1732): every test project a Linux job of
/// <c>.github/workflows/build-and-test.yml</c> runs with <c>dotnet test</c> is a leg of <c>scripts/linux-gate.sh</c>.
/// </summary>
/// <remarks>
/// Train 71b's first push went red in CI's Linux unit job on a Windows path literal that was green on Windows, and
/// the ~30-minute CI round trip found what a local WSL run finds in minutes. The WSL gate is only a substitute for
/// CI's Linux jobs while it runs the same projects. A Linux job that gains a test project the gate does not know
/// would be exactly the silent gap the gate exists to close, so that addition is red here until the gate learns it.
/// </remarks>
public sealed class LinuxGateDriftTests
{
    [Fact]
    public void EveryTestProjectCisLinuxJobsRun_IsALegOfTheLinuxGate()
    {
        string workflow = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml"));
        string gate = File.ReadAllText(TestRepo.Scripts("linux-gate.sh"));

        var linuxProjects = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string job in Regex.Split(workflow, @"^  (?=[A-Za-z0-9_-]+:\s*$)", RegexOptions.Multiline))
        {
            if (!Regex.IsMatch(job, @"^\s+runs-on:\s*ubuntu", RegexOptions.Multiline))
            {
                continue;
            }

            foreach (Match m in Regex.Matches(job, @"dotnet test (tests/[^\s]+\.csproj)"))
            {
                linuxProjects.Add(m.Groups[1].Value);
            }
        }

        var gateProjects = Regex.Matches(gate, @"proj=""(tests/[^""]+\.csproj)""").Select(m => m.Groups[1].Value).ToHashSet();

        Assert.NotEmpty(linuxProjects);
        var missing = linuxProjects.Where(p => !gateProjects.Contains(p)).ToList();
        Assert.True(missing.Count == 0,
            "CI's Linux jobs run test projects scripts/linux-gate.sh has no leg for: " + string.Join(", ", missing)
            + ". Add a leg, or the local Linux gate no longer stands in for CI's Linux jobs (kb/Work PB1732).");
    }
}
