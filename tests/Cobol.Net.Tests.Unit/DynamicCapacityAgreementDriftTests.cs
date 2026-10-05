// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A DYNAMIC-CAPACITY TABLE IS ONLY EVER BUILT BY THE DESCRIPTION THAT READS IT, WHICH IS WHY ISO §14.6.13.2 RULE 6
/// HAS NO RAISE SITE (kb/Work PB1118; docs/CONFORMANCE.md A.4.4). Rule 6 sets EC-DATA-INCOMPATIBLE "when the internal
/// format of a dynamic-capacity table, as defined by the implementor, is not correctly formed or does not agree with
/// the corresponding OCCURS clause". Its dynamic-LENGTH twin, rule 5, is reachable — a LINKAGE formal or a shared
/// cell-backed member is read through a DYNAMIC LENGTH clause other than the one that stored it, and
/// <c>CobolDynString.Agree</c> asks the question there. A table cannot get into that position: the implementor's
/// internal format is one <c>CobolDynTable</c> object, and every one a program reads was CONSTRUCTED from that
/// program's own OCCURS clause (its FROM minimum and TO expected capacity) or RECREATED into it by
/// <c>FromCurrentImage</c>, which applies the receiving description's minimum (§14.6.9.2). A file record cannot hold
/// one (§8.5.1.9.1 3)), and a LINKAGE one sits in a variable-length group formal, which crosses through that same
/// recreation. ONE storage form does share a table across descriptions — a CELL-BACKED area (kb/Work PB1042: two
/// programs describing one EXTERNAL record) — and there rule 6 is reachable and is asked on every reference:
/// <c>StorageCell.DynTableAt</c> takes the REFERENCING description's minimum and element width, carried by the
/// <c>CellTableSegment</c> every cell table reference is built from. Everywhere else rule 6 is unreachable BY
/// CONSTRUCTION, and these facts are what keep it so: a new path that built or adopted a table some other way would
/// have to say whose OCCURS clause it agrees with, and fails here first.
/// </summary>
public sealed class DynamicCapacityAgreementDriftTests
{
    private static readonly Regex Construction = new(@"new\s+CobolDynTable\s*<", RegexOptions.Compiled);
    private static readonly Regex Recreation = new(@"\.FromCurrentImage\s*\(", RegexOptions.Compiled);

    private static IEnumerable<(string Rel, string Code)> CompilerCode()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            var code = File.ReadLines(f).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal));
            yield return (Path.GetRelativePath(root, f), string.Join("\n", code));
        }
    }

    /// <summary>The emitted construction of a table is the DECLARATION's, from the item's own OCCURS clause
    /// (<c>ValueInitializer</c>), and nowhere else.</summary>
    [Fact]
    public void ATableIsConstructedOnlyFromItsOwnDescription()
    {
        var sites = CompilerCode().Where(c => Construction.IsMatch(c.Code)).Select(c => c.Rel).ToList();
        Assert.Equal([Path.Combine("CodeGen", "DataDivision", "ValueInitializer.cs")], sites);
    }

    /// <summary>The emitted recreation of a table from another description's content is the ONE group-image codec's
    /// (<c>GroupImageCodec</c> — the §14.9.25.4 GR9 MOVE and the activation-boundary transfer), which recreates it
    /// at the RECEIVING description.</summary>
    [Fact]
    public void ATableIsRecreatedOnlyThroughTheReceivingDescription()
    {
        var sites = CompilerCode().Where(c => Recreation.IsMatch(c.Code)).Select(c => c.Rel).ToList();
        Assert.Equal([Path.Combine("CodeGen", "DataDivision", "GroupImageCodec.cs")], sites);
    }

    /// <summary>The runtime type offers no way to ADOPT foreign storage: every public constructor takes the
    /// description's minimum and expected capacity, and no public member takes an element array.</summary>
    [Fact]
    public void TheRuntimeTableCannotAdoptForeignStorage()
    {
        var t = typeof(CobolDynTable<string>);
        foreach (var ctor in t.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            var names = ctor.GetParameters().Select(p => p.Name).ToList();
            Assert.Contains("min", names);
            Assert.Contains("expected", names);
        }
        var arrayTaking = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Concat<MethodBase>(t.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.GetParameters().Any(p => p.ParameterType.IsArray))
            .Select(m => m.Name).ToList();
        Assert.Empty(arrayTaking);
    }
}
