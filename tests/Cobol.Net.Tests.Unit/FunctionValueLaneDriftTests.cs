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
    /// <summary>A <c>"Name" =></c> or <c>"Name" when … =></c> arm of the <c>RenderDec</c> switch, and the text of its
    /// value expression that follows on the same line (empty when the expression starts on the next one).</summary>
    private static readonly Regex Arm = new(
        "^\\s*\"(?<name>\\w+)\"(?<guard>\\s+when\\s+[^=]+?)?\\s*=>\\s*(?<body>.*)$", RegexOptions.Compiled);

    /// <summary>The native arms of <c>RenderDec</c>'s switch (the standard-decimal-only arms excluded), each with whether
    /// its value is built by the switch's local <c>Dec(...)</c> (an SDIDI value) or otherwise (an exact one).</summary>
    private static SortedDictionary<string, bool> NativeRenderDecArms()
    {
        string source = File.ReadAllText(Path.Combine(
            TestRepo.Src("Cobol.Net.Compiler"), "CodeGen", "Emit", "IntrinsicRenderer.cs"));
        int start = source.IndexOf("private NumX? RenderDec(BoundIntrinsicCall ic)", StringComparison.Ordinal);
        int end = source.IndexOf("static NumX Dec(string expr)", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "RenderDec's body could not be located in IntrinsicRenderer.cs");

        var native = new SortedDictionary<string, bool>(StringComparer.Ordinal);
        string[] lines = source[start..end].Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var m = Arm.Match(lines[i].TrimEnd('\r'));
            if (!m.Success) continue;
            // `"Sqrt" when num.StandardDecimal` / `"Factorial" when num.StandardDecimal` are standard-mode arms: a NATIVE
            // call to either keeps the float / exact lane (IntrinsicRenderer: "an unguarded arm would flip native float
            // SQRT onto the Dec lane").
            if (m.Groups["guard"].Value.Contains("num.StandardDecimal", StringComparison.Ordinal)) continue;
            string body = m.Groups["body"].Value.Trim();
            if (body.Length == 0 && i + 1 < lines.Length) body = lines[i + 1].Trim();
            bool sdidi = body.StartsWith("Dec(", StringComparison.Ordinal);
            // A name with several arms (NUMVAL-C's LOCALE arm and its plain one) is SDIDI only when every arm is.
            native[m.Groups["name"].Value] = native.TryGetValue(m.Groups["name"].Value, out bool prior) ? prior && sdidi : sdidi;
        }
        Assert.True(native.Count > 10, "the RenderDec arm scan found suspiciously few arms: " + string.Join(", ", native.Keys));
        return native;
    }

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
        var native = new SortedSet<string>(NativeRenderDecArms().Keys, StringComparer.Ordinal);
        var restated = new SortedSet<string>(FunctionValueLane.NativeSdidiBodies, StringComparer.Ordinal);
        Assert.True(native.SetEquals(restated),
            "FunctionValueLane.NativeSdidiBodies must be exactly RenderDec's native arms (kb/Work PB3268).\n"
            + "  in RenderDec only: [" + string.Join(", ", native.Except(restated)) + "]\n"
            + "  in NativeSdidiBodies only: [" + string.Join(", ", restated.Except(native)) + "]");
    }

    /// <summary>The native SDIDI bodies whose value the renderer returns as an EXACT scale-0 value (SIGN, ORD-MAX, ORD-MIN:
    /// not built by <c>RenderDec</c>'s <c>Dec(...)</c>) are exactly <see cref="FunctionValueLane.NativeSdidiBodiesWithExactResult"/>
    /// — so a nested call the lane prediction routes onto an SDIDI body is handed to its caller on the carrier the
    /// renderer's arm really returns (kb/Work PB3268, review R3-1: an INTEGER function is no longer exempted wholesale).</summary>
    [Fact]
    public void NativeSdidiBodiesWithExactResult_AreTheRenderDecArmsNotBuiltByDec()
    {
        var exact = new SortedSet<string>(NativeRenderDecArms().Where(a => !a.Value).Select(a => a.Key), StringComparer.Ordinal);
        var restated = new SortedSet<string>(FunctionValueLane.NativeSdidiBodiesWithExactResult, StringComparer.Ordinal);
        Assert.NotEmpty(exact);
        Assert.True(exact.SetEquals(restated),
            "FunctionValueLane.NativeSdidiBodiesWithExactResult must be exactly RenderDec's native arms not built by Dec(...).\n"
            + "  in RenderDec only: [" + string.Join(", ", exact.Except(restated)) + "]\n"
            + "  in NativeSdidiBodiesWithExactResult only: [" + string.Join(", ", restated.Except(exact)) + "]");
    }
}
