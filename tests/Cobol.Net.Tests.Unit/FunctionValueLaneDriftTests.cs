// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding.Procedure;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ONE LIST THE BIND-TIME LANE PREDICTION RESTATES IS THE RENDERER'S OWN (kb/Work PB3268, review R1-1).
/// <c>FunctionValueLane.ReturnsBinary64</c> answers, before the code generator has rendered anything, whether a
/// numeric function's value arrives as a binary64 — the fact that decides whether the §14.9.25.4 GR1 intermediate
/// result item is a COMP-2 or a fixed-point item. It restates <c>IntrinsicRenderer.RenderNum</c>'s dispatch, and the
/// ONE piece of it that is a list rather than a rule is the set of functions that have an SDIDI body under NATIVE
/// arithmetic (<c>RenderDec</c>'s switch): given an SDIDI-carried argument (a floating-point literal), those functions
/// compute on the SDIDI and leave the float lane. A body added to the renderer's switch without its name in
/// <see cref="FunctionValueLane.NativeSdidiBodies"/> would send that function's float-literal call to a COMP-2
/// intermediate that rounds its SDIDI value, which is the defect this file holds shut.
/// </summary>
/// <remarks>The matrix that holds the whole prediction against the renderer's actual answer is the
/// <c>pb3268_function_value_lane_matrix</c> golden (a multi-receiver MOVE stores what the single-receiver MOVE stores,
/// over function x argument shape x receiver set); this test holds the list that golden cannot enumerate by itself.</remarks>
[Trait("Sweep", "tree")]
public sealed class FunctionValueLaneDriftTests
{
    /// <summary>A <c>"Name" =></c> or <c>"Name" when … =></c> arm of the <c>RenderDec</c> switch.</summary>
    private static readonly Regex Arm = new(
        "^\\s*\"(?<name>\\w+)\"(?<guard>\\s+when\\s+[^=]+?)?\\s*=>", RegexOptions.Compiled);

    /// <summary>The two other function sets the prediction reads off the renderer's own members
    /// (<c>IntrinsicRenderer.CrossAlignedNativeArms</c>, <see cref="FunctionValueLane.ValueFixedByDefinition"/>) route a call
    /// to its SDIDI body, so each member needs one in <see cref="FunctionValueLane.NativeSdidiBodies"/> - else the
    /// prediction would name a lane the renderer cannot take (kb/Work PB3268, review R2-1).</summary>
    [Fact]
    public void TheSdidiRoutedSets_AreSubsetsOfNativeSdidiBodies()
    {
        var crossAligned = CobolNet.CodeGen.Emit.IntrinsicRenderer.CrossAlignedNativeArms;
        Assert.True(crossAligned.Count > 0);
        foreach (string arm in crossAligned)
            Assert.True(FunctionValueLane.NativeSdidiBodies.Contains(arm),
                $"CrossAlignedNativeArms names {arm}, which has no native SDIDI body in NativeSdidiBodies");
        foreach (string arm in new[] { "Numval", "NumvalC" })
        {
            Assert.True(FunctionValueLane.ValueFixedByDefinition(arm), $"{arm} is a value-fixed-by-definition function (§15.67.4 r1 / §15.68.4 r1)");
            Assert.True(FunctionValueLane.NativeSdidiBodies.Contains(arm), $"{arm} has no native SDIDI body in NativeSdidiBodies");
        }
    }

    [Fact]
    public void NativeSdidiBodies_AreTheArmsOfRenderDec_ThatAreNotStandardDecimalOnly()
    {
        string source = File.ReadAllText(Path.Combine(
            TestRepo.Src("Cobol.Net.Compiler"), "CodeGen", "Emit", "IntrinsicRenderer.cs"));
        int start = source.IndexOf("private NumX? RenderDec(BoundIntrinsicCall ic)", StringComparison.Ordinal);
        int end = source.IndexOf("static NumX Dec(string expr)", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "RenderDec's body could not be located in IntrinsicRenderer.cs");

        var native = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string line in source[start..end].Split('\n'))
        {
            var m = Arm.Match(line);
            if (!m.Success) continue;
            // `"Sqrt" when num.StandardDecimal` / `"Factorial" when num.StandardDecimal` are standard-mode arms: a NATIVE
            // call to either keeps the float / exact lane (IntrinsicRenderer: "an unguarded arm would flip native float
            // SQRT onto the Dec lane").
            if (m.Groups["guard"].Value.Contains("num.StandardDecimal", StringComparison.Ordinal)) continue;
            native.Add(m.Groups["name"].Value);
        }

        var restated = new SortedSet<string>(FunctionValueLane.NativeSdidiBodies, StringComparer.Ordinal);
        Assert.True(native.Count > 10, "the RenderDec arm scan found suspiciously few arms: " + string.Join(", ", native));
        Assert.True(native.SetEquals(restated),
            "FunctionValueLane.NativeSdidiBodies must be exactly RenderDec's native arms (kb/Work PB3268).\n"
            + "  in RenderDec only: [" + string.Join(", ", native.Except(restated)) + "]\n"
            + "  in NativeSdidiBodies only: [" + string.Join(", ", restated.Except(native)) + "]");
    }
}
