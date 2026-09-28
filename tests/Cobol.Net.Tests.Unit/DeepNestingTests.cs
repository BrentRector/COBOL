// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet;
using CobolNet.CodeGen;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Regression for the deeply-nested-group emission blowup (DEVLOG 502): a CCVS NC test (NC126A/NC207A/NC246A) nests a
/// group ~49 levels deep with a leaf and a subgroup at each level. The DATA-DIVISION emitter recomputed each node's
/// physical width and composed initializer independently, each recursing — O(2^depth) time, so the compiler HUNG.
/// <see cref="PhysicalModel"/> now memoizes the per-item physical-field list, making emission linear.
/// <para>⛔ This test measures the GROWTH RATE, never the clock (kb/Work PB1590). It used to time a full compile
/// (Roslyn included) against a 20-second ceiling, and a loaded hosted CI runner turned that red twice with no compiler
/// change (26.4 s for a 1-second compile). It now counts the work the memo bounds —
/// <see cref="PhysicalModel.ListBuilds"/>, one physical-field list per group plus the root forest — at two depths and
/// asserts it grows by exactly one list per added level. The depths are deliberately SMALL: were the memo to regress,
/// depth 16 still finishes in about a second unmemoized (~2^16 builds), so the test FAILS with the count instead of
/// hanging the run; the 49-level CCVS programs themselves stay covered end to end by the NIST leg.</para>
/// </summary>
public sealed class DeepNestingTests : CobolNetTestBase
{
    [Fact]
    public void DeeplyNestedGroup_EmissionWorkIsLinearInDepth()
    {
        int shallow = ListBuildsAt(8), deep = ListBuildsAt(16);

        // depth d declares d-1 groups (G, GL-2 … GL-(d-1)); the memo builds one list per group plus the root forest.
        Assert.True(shallow <= 8 + 1, $"depth 8 built {shallow} physical-field lists; the memo allows at most 9");
        Assert.True(deep <= 16 + 1, $"depth 16 built {deep} physical-field lists; the memo allows at most 17 — "
            + "the nested-group emission blowup (DEVLOG 502) has regressed");
        Assert.Equal(16 - 8, deep - shallow);   // one more list per added nesting level — linear, not doubling
    }

    /// <summary>Compile the CCVS shape (a leaf AND a subgroup at every level) at <paramref name="depth"/> and return
    /// how many physical-field lists the DATA DIVISION emitter built.</summary>
    private int ListBuildsAt(int depth)
    {
        var sb = new StringBuilder();
        sb.AppendLine("IDENTIFICATION DIVISION.");
        sb.AppendLine($"PROGRAM-ID. DEEP{depth}.");
        sb.AppendLine("DATA DIVISION.");
        sb.AppendLine("WORKING-STORAGE SECTION.");
        sb.AppendLine("01 G.");
        for (int i = 2; i < depth; i++)
        {
            sb.AppendLine($"{i:00} GP-{i} PIC 9.");   // a leaf AND a subgroup at every level (the CCVS shape)
            sb.AppendLine($"{i:00} GL-{i}.");          // nesting is by level number — no indentation required
        }
        sb.AppendLine($"{depth:00} LEAF PIC 9.");
        sb.AppendLine("PROCEDURE DIVISION.");
        sb.AppendLine("MAIN. DISPLAY \"OK\". STOP RUN.");

        string srcPath = Path.Combine(TempDir, $"deep{depth}.cob");
        File.WriteAllText(srcPath, sb.ToString());

        var models = new List<PhysicalModel>();
        PhysicalModel.Observer.Value = models.Add;   // AsyncLocal: only this test's own compilation reports here
        try
        {
            var result = CompilerDriver.Compile(new CompilerDriver.Options(srcPath, Path.Combine(TempDir, $"deep{depth}.dll"), SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(result.Success, $"depth-{depth} compile failed: {result.Status}: {string.Join("\n", result.Errors)}");
        }
        finally { PhysicalModel.Observer.Value = null; }

        Assert.Single(models);   // one program, one DATA DIVISION model
        return models[0].ListBuilds;
    }
}
