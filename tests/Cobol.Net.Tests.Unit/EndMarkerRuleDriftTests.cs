// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §10.7.3's end-marker rules are asked ONCE, by <c>Validation/EndMarkerPass</c>, of every source unit
/// (kb/Work PB988). Before it the name comparison was written five times — END CLASS and END METHOD twice in
/// <c>OoClassTable.Build</c>, END INTERFACE there and again for parameterized skeletons in <c>OoExpansion</c> — while
/// an interface's method prototypes, every GET/SET PROPERTY method and every PROGRAM / FUNCTION marker were asked by
/// none of them. No other compiler file may compare an end marker's name, and no other file may report
/// COBOLNET2793.
/// </summary>
public sealed class EndMarkerRuleDriftTests
{
    private const string Home = "EndMarkerPass.cs";

    // An end-marker name accessor and a name comparison on one line: the shape every retired copy had.
    private static readonly Regex EndNameComparison = new(
        @"CobolNames\.Same\(.*(endClassHeader\(\)|endProgramHeader\(\)|methodName\(1\)|interfaceName\(\)\[\^1\]"
        + @"|interfaceName\(\)\.Length - 1)", RegexOptions.Compiled);

    private static IEnumerable<string> CompilerSources() =>
        Directory.EnumerateFiles(Path.Combine(TestRepo.Root, "src", "Cobol.Net.Compiler"), "*.cs",
                SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    [Fact]
    public void NoOtherFile_ComparesAnEndMarkerName()
    {
        var offenders = CompilerSources()
            .Where(f => Path.GetFileName(f) != Home)
            .SelectMany(f => File.ReadLines(f).Select((l, i) => (f, l, i)))
            .Where(x => EndNameComparison.IsMatch(x.l))
            .Select(x => $"{Path.GetRelativePath(TestRepo.Root, x.f)}:{x.i + 1}: {x.l.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "an end-marker name is compared outside Validation/EndMarkerPass (ISO §10.7.3 has one home):\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void OnlyTheHome_ReportsTheEndMarkerRule()
    {
        var reporters = CompilerSources()
            .Where(f => File.ReadAllText(f).Contains(nameof(DiagnosticCatalog.EndMarkerRule)))
            .Select(Path.GetFileName)
            .ToList();
        Assert.Equal([Home], reporters);
    }
}
