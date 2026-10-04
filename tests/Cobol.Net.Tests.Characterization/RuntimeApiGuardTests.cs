// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Characterization;

/// <summary>
/// The P7 Step 4b RATCHET (DESIGN-codegen-backend §3): every C# fragment naming a runtime member must route
/// through the <c>nameof</c>-anchored <c>RuntimeApi</c> façade, so a runtime rename breaks ONE file at compile
/// time. The Step-9-final sweep (DEVLOG 810) drove every Step-9 emitter to ZERO; Step 12 routed
/// <c>IntrinsicRenderer</c> (the static-channel deletion) and deleted its entry. The remaining baselines are
/// the three expression/condition renderers, whose fragments route when the P9 <c>Roslyn/</c> consolidation
/// (ExpressionRenderer) restructures them.
/// A file NOT listed must have ZERO bare accesses; a listed file's count may only SHRINK (update the entry
/// downward, delete it at 0; when the table empties, flip this test to forbid-all).
/// <para><b>Counting rule (the Step-9-final recorded refinement):</b> comment lines (<c>//</c>-led) are
/// stripped before matching — a comment can never mis-emit text, and doc-comment <c>cref</c>s are already
/// compile-validated. <c>CobolRounding.</c> and <c>CobolPassMode.</c> member accesses are excluded — in
/// CodeGen those are always TYPED enum arguments (compile-checked); their EMITTED-text forms render
/// exclusively through the façade's <c>RoundingText</c>/<c>PassModeText</c> anchors, so a rename still breaks
/// <c>RuntimeApi.cs</c> at compile time. Reads files with <c>File.ReadAllText</c>, NOT a grep subprocess —
/// <c>ConditionRenderer.cs</c> historically carried a literal NUL that made it invisible to ripgrep
/// (DEVLOG 788/790).</para>
/// </summary>
public sealed class RuntimeApiGuardTests
{
    /// <summary>Matches a bare runtime member-access fragment. Excluded by construction: the <c>CobolNet.</c>
    /// namespace, the compiler-internal <c>CobolLiteral.</c> codec (not a runtime type), the two typed-enum
    /// runtime types (<c>CobolRounding.</c>/<c>CobolPassMode.</c> — see the counting rule above), and
    /// <c>CobolNames.</c>, the ONE Annex C word fold the emitters CALL at compile time to compare COBOL words (kb/Work
    /// PB1402) — never a fragment of emitted text.</summary>
    private static readonly Regex Bare = new(@"\bCobol(?!Net\.|Literal\.|Rounding\.|PassMode\.|Names\.)[A-Za-z0-9]+\.", RegexOptions.Compiled);

    /// <summary>The baseline: bare-count per CodeGen file. Post-Step-12 (IntrinsicRenderer routed → entry
    /// deleted), ONLY the three expression/condition renderers remain (they route at the P9 Roslyn/
    /// consolidation); every other CodeGen file is at ZERO and stays there.</summary>
    private static readonly Dictionary<string, int> Baseline = new(StringComparer.OrdinalIgnoreCase)
    {
        // ⛔ RATCHET DISCIPLINE: these are MEASURED counts, not budgets. Landing train 7 (2026-09-05) found
        // NumericRenderer and OperandText standing ABOVE their true counts — neither file is touched by that
        // train, so the slack was inherited from earlier landings that shrank them without ratcheting, and for
        // that whole time the guard could not have caught a new bare access in either file. Re-measure with
        // this test's own rule after any landing that touches CodeGen, and lower the entry.
        ["Emit/NumericRenderer.cs"] = 16,     // routes at P9 (ExpressionRenderer under Roslyn/); 17 → 16 measured
        ["Emit/ConditionRenderer.cs"] = 16,   // routes at P9 (17 → 16: PB297 routed the figurative relation's compare)
        ["Emit/OperandText.cs"] = 4,          // routes at P9; 7 → 4 measured
    };

    [Fact]
    public void Bare_runtime_accesses_do_not_increase()
    {
        string codeGen = TestRepo.Src("Cobol.Net.Compiler", "CodeGen");
        Assert.True(Directory.Exists(codeGen), codeGen);
        var over = new List<string>();
        foreach (string file in Directory.EnumerateFiles(codeGen, "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetRelativePath(codeGen, file).Replace('\\', '/');
            if (name is "Roslyn/RuntimeApi.cs") continue;   // the façade itself
            string code = string.Join("\n",
                File.ReadAllLines(file).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
            int count = Bare.Matches(code).Count;
            int allowed = Baseline.GetValueOrDefault(name, 0);
            if (count > allowed)
                over.Add($"{name}: {count} bare Cobol*. accesses (baseline {allowed}) — route new emission through RuntimeApi");
        }
        Assert.True(over.Count == 0, string.Join("\n", over));
    }
}
