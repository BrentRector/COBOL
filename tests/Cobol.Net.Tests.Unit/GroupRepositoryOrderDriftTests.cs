// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A REPOSITORY PROTOTYPE IS RESOLVED BY ONE SEARCH OF THE COMPILATION GROUP, RUN AS OF THE REFERENCING ELEMENT'S
/// POSITION (kb/Work PB989; ISO §12.3.8.4 GR10 / GR11). "Specified previously in the same compilation group" is a
/// POSITION rule: a definition counts for a) only when it starts before the source element whose REPOSITORY names it.
/// The binder used to build two order-blind tables once for the whole group (<c>BuildProgramDetailsTable</c>, keyed by
/// externalized name; <c>BuildUserFunctionTable</c>, keyed by WORD), so a definition that followed its caller supplied
/// the details a), and a function prototype was replaced by a definition that merely shared its word. The rule now
/// lives in <c>GroupRepository.Find</c>; this pins that no second reading of it, and no order-blind table, comes back.
/// </summary>
public sealed class GroupRepositoryOrderDriftTests
{
    private static readonly string[] SourceRoots = ["Cobol.Net.Compiler", "Cobol.Net.Editions", "Cobol.Net.Runtime"];

    private static IEnumerable<string> SourceFiles() =>
        SourceRoots.SelectMany(p => Directory.EnumerateFiles(TestRepo.Src(p), "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    /// <summary>The order-blind builders are deleted and nothing names them: not as a call, not as a doc reference that
    /// would send the next reader looking for a table that no longer exists.</summary>
    [Fact]
    public void TheOrderBlindGroupTables_AreGone()
    {
        string[] gone = ["BuildProgramDetailsTable", "BuildUserFunctionTable"];
        var hits = new List<string>();
        foreach (string f in SourceFiles())
        {
            int n = 0;
            foreach (string line in File.ReadLines(f))
            {
                n++;
                foreach (string name in gone)
                    if (line.Contains(name, StringComparison.Ordinal))
                        hits.Add($"{Path.GetFileName(f)}:{n}: {line.Trim()}");
            }
        }

        Assert.True(hits.Count == 0,
            "an order-blind REPOSITORY table (or a reference to one) reappeared; resolve through GroupRepository "
            + "(kb/Work PB989):" + Environment.NewLine + string.Join(Environment.NewLine, hits));
    }

    /// <summary>Only <c>GroupRepository</c> READS a unit's position; the driver that hands each element its own position
    /// to the two resolvers is the one other file that names it, and the model type defines it.</summary>
    [Fact]
    public void OnlyTheGroupRepository_ComparesSourcePositions()
    {
        string[] allowed = ["BoundUnit.cs", "BinderDriver.cs", "GroupRepository.cs"];
        var offenders = SourceFiles()
            .Where(f => File.ReadAllText(f).Contains("SourcePosition", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .Where(name => !allowed.Contains(name))
            .ToList();
        Assert.True(offenders.Count == 0,
            "BoundUnit.SourcePosition is read outside GroupRepository: " + string.Join(", ", offenders));

        string driver = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "BinderDriver.cs"));
        Assert.DoesNotMatch(new Regex(@"SourcePosition\s*[<>]"), driver);   // the driver passes a position; it never compares one
    }

    /// <summary>The search itself is ONE function with the rule's own order — a definition strictly BEFORE the position
    /// first (a), then the prototype definition (b) — and both twins (program and function) call it.</summary>
    [Fact]
    public void BothTwins_SearchThroughTheOneFind()
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "GroupRepository.cs"));
        Assert.Single(Regex.Matches(text, @"u\.SourcePosition\s*<\s*position"));
        Assert.Matches(new Regex(@"ProgramDetails\([^)]*\)\s*=>\s*Find\(_programs", RegexOptions.Singleline), text);
        Assert.Contains("Find(_functions, Externalized(externalized), position)", text, StringComparison.Ordinal);
    }
}
