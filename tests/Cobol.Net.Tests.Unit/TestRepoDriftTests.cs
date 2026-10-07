// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Keeps the repo-root locator collapsed to ONE implementation.
/// </summary>
/// <remarks>
/// <para>
/// This existed as NINETEEN private root-finders across five test projects, in SEVEN mechanisms: walkers keyed on
/// <c>Cobol.Net.sln</c>, on <c>tests/version-matrix</c>, on <c>tests/nist</c>, on <c>src/Cobol.Net.Compiler</c>,
/// on <c>PROMPT.md</c> and on <c>.git</c>, plus two sites that counted <c>".."</c> hops up from the output
/// directory. None was wrong on the day it was written; they accumulated, because writing a six-line walker is
/// cheaper in the moment than finding the one that already exists. That is how a duplicated rule grows back, so
/// the extraction alone is not the fix — this test is the half that makes it stay fixed
/// (<c>feedback_one_rule_one_place</c>, <c>feedback_scan_all_similar</c>).
/// </para>
/// <para>
/// ⚠ The signature below is deliberately wider than the first draft of this test, and the sweep is why. That draft
/// looked for <c>AppContext.BaseDirectory</c> beside <c>.Parent</c> — which is the shape thirteen of the nineteen
/// happened to have, and would have silently blessed the other six. The <c>.git</c> walker started from
/// <c>Directory.GetCurrentDirectory()</c>, and the hop-counters started from
/// <c>AppDomain.CurrentDomain.BaseDirectory</c> and never touched <c>.Parent</c> at all. So the test pairs ANY
/// base-directory anchor with ANY upward move: it is the combination that means "finding the repo root the long
/// way round". Either half alone stays legal — locating an asset beside the test assembly is a different question.
/// </para>
/// </remarks>
public sealed class TestRepoDriftTests
{
    /// <summary>Where a hand-rolled root-finder starts from.</summary>
    private static readonly string[] Anchors =
        ["AppContext.BaseDirectory", "AppDomain.CurrentDomain.BaseDirectory", "Directory.GetCurrentDirectory("];

    /// <summary>How it then moves upward — climbing parents, or counting hops.</summary>
    private static readonly string[] Ascents = [".Parent", "Directory.GetParent", "\"..\""];

    // The two files that legitimately name the pattern: the locator itself, and this test, which must quote the
    // needles it searches for.
    private static readonly string[] Exempt = ["TestRepo.cs", "TestRepoDriftTests.cs"];

    private static IEnumerable<string> TestSources() =>
        Directory.EnumerateFiles(TestRepo.Tests(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !Exempt.Contains(Path.GetFileName(f)));

    [Fact]
    public void NoTestSource_FindsTheRepoRootItself()
    {
        var offenders = new List<string>();
        foreach (string file in TestSources())
        {
            string body = File.ReadAllText(file);
            string? anchor = Anchors.FirstOrDefault(a => body.Contains(a, StringComparison.Ordinal));
            string? ascent = Ascents.FirstOrDefault(a => body.Contains(a, StringComparison.Ordinal));
            if (anchor is not null && ascent is not null)
                offenders.Add($"{Path.GetRelativePath(TestRepo.Root, file)}  ({anchor} … {ascent})");
        }

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} test source(s) find the repo root themselves instead of using "
            + $"CobolNet.Tests.Shared.TestRepo:{Environment.NewLine}"
            + string.Join(Environment.NewLine, offenders.Select(o => "    " + o))
            + $"{Environment.NewLine}Use TestRepo.Root / .Src() / .Tests() / .Docs() / .Scripts() / .Specs() / "
            + ".VersionMatrix() / .Nist() — see tests/_shared/TestRepo.cs.");
    }

    /// <summary>The locator resolves to a tree that really is this repo, not merely to some directory.</summary>
    [Fact]
    public void TestRepoRoot_IsThisRepository()
    {
        Assert.True(File.Exists(TestRepo.At("Cobol.Net.sln")), $"no Cobol.Net.sln at {TestRepo.Root}");
        Assert.True(Directory.Exists(TestRepo.Src("Cobol.Net.Compiler")), "src/Cobol.Net.Compiler is missing");
        Assert.True(File.Exists(TestRepo.VersionMatrix("constructs.json")), "constructs.json is missing");
        Assert.True(File.Exists(TestRepo.Nist("corpus.tsv")), "tests/nist/corpus.tsv is missing");
        Assert.True(File.Exists(TestRepo.Docs("COBOLNET_REARCHITECTURE_PLAN.md")), "the plan is missing");
        Assert.True(File.Exists(TestRepo.Specs("ISO_COBOL.md")),
            "specs/ISO_COBOL.md is missing — git submodule update --init --recursive");
        Assert.True(Directory.Exists(TestRepo.Scripts("spec")), "scripts/spec is missing");
    }
}
