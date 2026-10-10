// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A §8.5.1.12 ATOM ARRAY IS WRITTEN INTO EMITTED C# IN ONE PLACE, AND ONCE PER DISTINCT SHAPE (kb/Work PB2690). Every
/// shape a variable-length MOVE, comparison, CALL or INVOKE hands the runtime is a <see cref="GroupAtom"/> array the
/// compiler knows in full, so the module states each distinct array as a <c>static readonly</c> field of
/// <c>__GroupAtoms</c> (<c>GroupAtomTable</c>) and every use names the field. An inline literal allocated the array and
/// one record per atom at every execution, and the runtime's per-pair caches (<c>CobolVarGroup</c>'s correspondence
/// plans and geometries) key on the array's reference, which an inline literal never repeats. So the rendering of an
/// array (<c>RuntimeApi.GroupAtomsNew</c>) is reached from the table and the one static description renderer only, and
/// a generated module holds no <c>new GroupAtom[]</c> outside <c>__GroupAtoms</c>.
/// </summary>
public sealed class GroupAtomTableDriftTests : CobolNetTestBase
{
    [Fact]
    public void TheArrayRendering_IsReachedFromTheTableAndTheStaticDescriptionRendererOnly()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler", "CodeGen");
        var callers = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadLines(f).Any(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)
                                                   && l.Contains("GroupAtomsNew(", StringComparison.Ordinal)))
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.Equal(["GroupAtomTable.cs", "Roslyn/RuntimeApi.cs"], callers);
        // …and inside RuntimeApi, only its own definition and the one renderer of a static ActivationDescription (a
        // field initialised once per class, not per execution).
        string api = File.ReadAllText(Path.Combine(root, "Roslyn", "RuntimeApi.cs"));
        int uses = Regex.Matches(api, @"[^/\r\n]*\bGroupAtomsNew\(").Count(m => !m.Value.TrimStart().StartsWith("///", StringComparison.Ordinal)
                                                                              && !m.Value.TrimStart().StartsWith("//", StringComparison.Ordinal));
        Assert.True(uses <= 3, $"RuntimeApi names GroupAtomsNew {uses} times: its definition (with its recursion into an element) and the "
                               + "ActivationDescription renderer are the only places that may");
    }

    [Fact]
    public void AGeneratedModule_StatesEachDistinctArrayOnce_AndNamesItEverywhereElse()
    {
        string source = File.ReadAllText(TestRepo.Tests("conformance", "2014", "pb2690_fixed_group_vlg_element_tables_call.cob"));
        string src = Path.Combine(TempDir, "pb2690_atoms.cob");
        File.WriteAllText(src, source);
        var result = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: 2014, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        string cs = File.ReadAllText(result.GeneratedCsPath!);

        int table = cs.IndexOf("internal static class __GroupAtoms", StringComparison.Ordinal);
        Assert.True(table >= 0, "the module holds no __GroupAtoms class although its CALLs pass variable-length groups");
        string outside = cs[..table] + cs[(cs.IndexOf("\n}\n", table, StringComparison.Ordinal) + 3)..];
        Assert.DoesNotContain("new GroupAtom[]", outside, StringComparison.Ordinal);

        // The fixture has two shapes: the fixed group FA and the variable-length group (the caller's and the callees'
        // descriptions of it are one shape). Each is one field, and every field is used.
        var fields = Regex.Matches(cs[table..], @"internal static readonly GroupAtom\[\] (A\d+) =").Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(["A0", "A1"], fields);
        foreach (var f in fields)
            Assert.Contains($"__GroupAtoms.{f}", outside, StringComparison.Ordinal);
    }
}
