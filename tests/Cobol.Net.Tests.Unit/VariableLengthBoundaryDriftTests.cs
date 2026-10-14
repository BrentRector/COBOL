// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE VARIABLE-LENGTH ACTIVATION-BOUNDARY DRIFT LOCK (kb/Work PB204).
/// <para>ISO §14.8.2.2 and §14.8.3.2 ADMIT a variable-length group across a Format-2 CALL / CALL RETURNING /
/// INVOKE boundary "subject to compatibility as described in 8.5.1.12". What made that admission a rejection
/// for years was not a missing rule text but a PREDICATE: every boundary screen and emit guard asked
/// <c>IsImageCapable</c>, which is false for such a group BY DEFINITION, so conforming source was refused.
/// The structural fix is that a boundary asks <see cref="CobolNet.Binding.Model.DataItem.BoundaryImageCapable"/>
/// — "fixed record window OR current-extent carrier" — and this test is what keeps that true.</para>
/// <para>Two facts are pinned, and each is a way the mechanism can silently regress:</para>
/// <list type="number">
/// <item>THE CAPABILITY IS DEFINED ONCE. <c>CurrentExtentImageCapable</c> lives on <c>DataItem</c> and nowhere
/// else. It began life inside <c>GroupImageCodec</c> and had to move when a BIND-time screen started asking it;
/// a second definition would let the DISPLAY format and the crossing disagree about which groups have a defined
/// current extent — the exact two-arm shape this repo keeps producing.</item>
/// <item>THE BOUNDARY SCREENS ASK THE BOUNDARY PREDICATE. The two sites that decide whether a group crossing is
/// admitted (<c>OoConformance.DescriptionMismatch</c>'s two group arms, and <c>CallEmitter.ArgText</c>'s
/// stage guard) must spell <c>BoundaryImageCapable</c>. Reverting either one to <c>IsImageCapable</c> restores
/// the defect with no test failing anywhere else — the goldens would still pass, because a REJECTION at bind
/// looks like any other diagnostic to a corpus runner that never compiles the program.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class VariableLengthBoundaryDriftTests
{
    /// <summary>Count occurrences in CODE only — these predicates are discussed at length in doc comments, so a
    /// raw text count is a false positive (the <see cref="DisplayUsageUnionDriftTests"/> convention).</summary>
    private static int CodeOccurrences(string src, string needle)
    {
        var code = new List<string>();
        foreach (string raw in src.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string t = line.TrimStart();
            if (t.StartsWith("///") || t.StartsWith("//")) continue;
            int i = line.IndexOf("//", StringComparison.Ordinal);
            code.Add(i >= 0 ? line[..i] : line);
        }
        return Regex.Matches(string.Join("\n", code), Regex.Escape(needle)).Count;
    }

    [Fact]
    public void CurrentExtentCapability_IsDefinedExactlyOnce()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        var definitions = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "Generated" + Path.DirectorySeparatorChar)) continue;
            string src = File.ReadAllText(file);
            // A DEFINITION is a property or method declaration of that name; a USE is a member access.
            if (CodeOccurrences(src, "bool CurrentExtentImageCapable") > 0)
                definitions.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
        }
        Assert.Equal(["Binding/Model/DataItem.cs"], definitions);
    }

    [Fact]
    public void BoundaryScreens_AskTheBoundaryPredicate()
    {
        // OoConformance: the group arms of the pair-mode comparator — the argument-group screen and the formal-group
        // screen. All must admit a compatible variable-length group. The BY CONTENT argument-group screen (PB165,
        // closing PB818: it used to ask IsImageCapable) left with the BY CONTENT comparator (kb/Work PB3101): a BY
        // CONTENT group is described by ActivationDescriptions.Of, whose variable-length arm asks
        // CurrentExtentImageCapable before IsImageCapable — the two halves of BoundaryImageCapable.
        string oo = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "Oo", "OoConformance.cs"));
        Assert.Equal(3, CodeOccurrences(oo, "BoundaryImageCapable"));
        string descriptions = File.ReadAllText(
            TestRepo.At("src", "Cobol.Net.Compiler", "Oo", "ActivationDescriptions.cs"));
        Assert.True(CodeOccurrences(descriptions, "item.CurrentExtentImageCapable") > 0
                    && CodeOccurrences(descriptions, "item.IsImageCapable") > 0,
            "ActivationDescriptions.Of no longer describes a group by both halves of BoundaryImageCapable — a compatible "
            + "variable-length group argument BY CONTENT would have no description and be refused (kb/Work PB818).");
        Assert.Equal(0, CodeOccurrences(oo, "arg.IsImageCapable"));
        Assert.Equal(0, CodeOccurrences(oo, "formal.IsImageCapable"));

        // CallEmitter: the stage guard that used to refuse the crossing outright.
        string call = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "CodeGen", "Verbs", "CallEmitter.cs"));
        Assert.Equal(1, CodeOccurrences(call, "BoundaryImageCapable"));
        Assert.Equal(0, CodeOccurrences(call, "p.Item.IsGroup && !p.Item.IsImageCapable"));

        // And the compatibility relation is consulted from the comparator, not re-implemented at a call site
        // (two consultations: the argument arm and the formal arm; PB165's BY CONTENT arm is the run-time relation's
        // §8.5.1.12 walk now, ActivationRelations.VariableLengthViolation over the descriptions' atoms, kb/Work PB3101).
        Assert.Equal(2, CodeOccurrences(oo, "VariableLengthCompatibility.Mismatch"));
    }
}
