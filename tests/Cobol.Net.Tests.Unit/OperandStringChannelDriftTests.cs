// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Holds the TWO string-image channels for a <c>BoundOperand</c> in their intended relationship, leaf by leaf
/// (fix-queue PB49).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS EXISTS. <c>OperandText.AsString</c> (the DISPLAY / MOVE / STRING channel) and
/// <c>IntrinsicRenderer.StrArgVisitor</c> (the intrinsic-argument channel) are two visitors over the same leaf
/// set. <c>StrArgVisitor</c> delegates only SOME leaves and answers the rest itself, so it inherited neither
/// <c>AsString</c>'s figurative arm nor its ALL-literal arm — and <c>FUNCTION LOWER-CASE(SPACE)</c>, legal
/// source, compiled clean and aborted at RUN TIME with "intrinsic string argument 'BoundFigurative'"
/// (fix-queue PB25). That was ONE RULE WRITTEN IN TWO PLACES with only one copy right. PB25 routed the two
/// offending leaves to <c>AsString</c>; the guard its own evidence file prescribed was never built, and this
/// is it.
/// </para>
/// <para>
/// ⚠ <b>THE TWO CHANNELS ARE NOT REQUIRED TO AGREE, AND A TEST ASSERTING THAT WOULD BE WRONG.</b> A numeric
/// literal is a legal DISPLAY operand and NOT a legal string-argument operand; the computed-operand arms differ
/// because the intrinsic channel re-enters <c>RenderString</c> for a nested string-class function where the
/// DISPLAY channel has a loud stage. So this encodes a PER-LEAF EXPECTATION with the intended answer stated,
/// exactly as PB49 required — never a blanket "these two functions are equal".
/// </para>
/// <para>
/// ⚙ <b>AND PB49's SECOND QUESTION HAS AN ANSWER RATHER THAN A GUARD.</b> It asked for the same check on the
/// NUMERIC channels. Measured: the whole compiler has four <c>IBoundOperandVisitor</c> implementations —
/// <c>StrArgVisitor</c> and <c>AsStringVisitor</c> (string), <c>IsStringVisitor</c> (a bool classifier), and
/// <c>NumericRenderer</c>. <b>NumericRenderer is the ONLY numeric one</b>; <c>OperandText.NumericIntrinsicText</c>
/// is a helper it is called from, not a parallel visitor. There is no numeric twin to drift from, so there is
/// nothing to guard — asserted below so that stops being true loudly rather than silently.
/// </para>
/// </remarks>
public sealed class OperandStringChannelDriftTests
{
    private static string IntrinsicRendererSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));

    private static string OperandTextSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "OperandText.cs"));

    /// <summary>The body of <c>StrArgVisitor</c> — the intrinsic-argument string channel.</summary>
    private static string StrArgVisitorBody()
    {
        string src = IntrinsicRendererSource();
        int at = src.IndexOf("private sealed class StrArgVisitor", StringComparison.Ordinal);
        Assert.True(at > 0, "StrArgVisitor is gone from IntrinsicRenderer — the intrinsic-argument string "
            + "channel was restructured; re-point this guard at whatever replaced it.");
        return src[at..];
    }

    /// <summary>The leaf arms that MUST route to <c>OperandText.AsString</c> — the PB25 fix, and the exact
    /// thing that rots if someone re-implements a leaf locally "to avoid the dependency".</summary>
    [Theory]
    // ⚠ The field arm delegates to OperandText.AsTransferString — AsString plus the one-way group image a
    // strongly-typed group with a pointer/object leaf needs (kb/Work PB1901); it IS the shared channel, one call deeper.
    [InlineData("BoundFieldOperand", "a field's display image is width- and category-sensitive; the local copy "
        + "would have to re-derive DE-EDITING, the sign convention and the float check", "OperandText.AsTransferString")]
    [InlineData("BoundFigurative", "§8.3.3.6.4 GR3's PCS-aware materialisation — the arm PB25 added, and the one "
        + "whose absence aborted FUNCTION LOWER-CASE(SPACE) at run time", "OperandText.AsString")]
    [InlineData("BoundAllLiteral", "§8.3.3.6.4 GR3c — ALL literal-1 is the literal ONCE in a length-unspecified "
        + "context, which a local copy has previously got wrong", "OperandText.AsString")]
    public void StrArgVisitor_DelegatesTheSharedLeaves_ToOperandTextAsString(string leaf, string why, string channel)
    {
        string body = StrArgVisitorBody();
        var m = Regex.Match(body, $@"public string Visit\({leaf} n\)\s*=>(?<rhs>[^;]*);");
        Assert.True(m.Success, $"StrArgVisitor no longer has a Visit({leaf}) arm — the generated visitor is "
            + "exhaustive, so this means the LEAF was renamed or removed; update this guard deliberately.");
        Assert.True(m.Groups["rhs"].Value.Contains(channel, StringComparison.Ordinal),
            $"StrArgVisitor.Visit({leaf}) no longer delegates to {channel}. {why}. This is the PB25 "
            + "shape: one rule written in two places, and the second copy silently wrong — it shipped as a "
            + "clean compile that aborted at RUN TIME on legal source.");
    }

    /// <summary>
    /// The leaves the two channels are meant to answer DIFFERENTLY, asserted so nobody "fixes" them into
    /// agreement. <c>BoundBoolOperand</c> — a boolean EXPRESSION argument, §8.4.3.2.3 SR8 (kb/Work PB65) — images
    /// through the ONE <c>BooleanRenderer</c>, never a local re-derivation and never a loud stage (it was a stage
    /// before the grammar admitted the argument). <c>BoundNumericLiteral</c> is ADMITTED
    /// PER-FUNCTION since PB59 (§15.12.3 r1 / §15.18.3 r1 admit numeric literals): the arm must stay
    /// CONDITIONAL — <c>admitNumeric</c> selecting the shared OperandText image, <c>Loud</c> as the DEFAULT —
    /// because NUMVAL/NUMVAL-F carry open rows (AR-15.67.3-1 / AR-15.69.3-1) demanding a compile-time
    /// rejection; an unconditional delegation would turn their wrong-stage defect into a silently-wrong one.
    /// </summary>
    [Fact]
    public void StrArgVisitor_NumericLiteral_AdmittedPerFunction_LoudByDefault()
    {
        string body = StrArgVisitorBody();

        var num = Regex.Match(body, @"public string Visit\(BoundNumericLiteral n\)\s*=>(?<rhs>[^;]*);");
        Assert.True(num.Success, "StrArgVisitor lost its Visit(BoundNumericLiteral) arm");
        string rhs = num.Groups["rhs"].Value;
        Assert.True(rhs.Contains("admitNumeric", StringComparison.Ordinal)
                && rhs.Contains("Loud", StringComparison.Ordinal),
            "Visit(BoundNumericLiteral) must stay the CONDITIONAL PB59 shape — admitNumeric selecting the "
            + "shared OperandText image with Loud as the default. Unconditionally Loud re-opens "
            + "RV-15.12.4-1/RV-15.18.4-1 (legal literals abort at run time); unconditionally delegating "
            + "silently admits a numeric literal where §15.67.3/§15.69.3 exclude it.");
        Assert.True(rhs.Contains("OperandText.AsString", StringComparison.Ordinal),
            "the admitting branch must delegate to the ONE OperandText.AsString image (PB25's rule: never a "
            + "local re-derivation of the literal image)");

        var bol = Regex.Match(body, @"public string Visit\(BoundBoolOperand n\)\s*=>(?<rhs>[^;]*);");
        Assert.True(bol.Success, "StrArgVisitor lost its Visit(BoundBoolOperand) arm");
        Assert.True(bol.Groups["rhs"].Value.Contains("BooleanRenderer.Render", StringComparison.Ordinal),
            "Visit(BoundBoolOperand) must image a boolean-expression argument through the ONE BooleanRenderer "
            + "(§8.4.3.2.3 SR8 admits a boolean expression as an argument — kb/Work PB65); a loud stage here "
            + "re-opens FMT-15.45.2, a local '0'/'1' derivation is a second boolean renderer.");
    }

    /// <summary>The admitting entry (<c>StrNum</c> / the admitting <c>StrArgList</c>) is reached from EXACTLY
    /// the functions whose §15.x.3 rules admit a numeric literal — BASECONVERT argument-1 and CONCAT. A new
    /// caller is a new spec claim and must be added here WITH its rule; a blanket route through the admitting
    /// entry is the over-acceptance this file exists to prevent.</summary>
    [Fact]
    public void StrNum_IsReachedFromExactlyTheAdmittingFunctions()
    {
        string src = IntrinsicRendererSource();
        int visitorAt = src.IndexOf("private sealed class StrArgVisitor", StringComparison.Ordinal);
        string renderers = src[..visitorAt];   // the arm switches live above the visitor class

        // Each admitting CALL SITE (StrNum(ic… / the admitting StrArgList) is attributed to its enclosing
        // switch arm by the nearest preceding "Name" => label. The token shapes are call-site-precise so the
        // StrNum/StrArgVisitor helper declarations (which precede every arm) cannot false-attribute, and the
        // attribution deliberately does NOT scan forward through arm text — the arms' own comments contain
        // ';', which sank the first draft of this guard (it matched nothing and would have passed green on an
        // empty caller set had the expected list not been asserted).
        var callers = Regex.Matches(renderers, @"StrNum\(ic|StrArgList\(ic,\s*admitNumeric:\s*true")
            .Select(tok => Regex.Matches(renderers[..tok.Index], @"""(?<fn>\w+)""\s*=>")
                .Select(m => m.Groups["fn"].Value).LastOrDefault())
            .Where(fn => fn is not null).Distinct().Order().ToList();
        Assert.True(callers.SequenceEqual(new[] { "BaseConvert", "Concat" }),
            $"the numeric-admitting string entry is reached from [{string.Join(", ", callers)}] — expected "
            + "exactly [BaseConvert, Concat] (§15.12.3 r1's unsigned-integer-literal argument-1; §15.18.3 r1's "
            + "class-numeric list). If a new function legitimately admits numeric literals, add it here WITH "
            + "the §15.x.3 rule that says so.");
    }

    /// <summary>Both string channels cover the SAME leaf set. The generated visitor interface already forces
    /// this at compile time; asserting it here is what makes the per-leaf table above provably COMPLETE — a new
    /// <c>BoundOperand</c> leaf must be classified as shared-or-divergent, not quietly added to one side.</summary>
    [Fact]
    public void BothStringChannels_CoverTheSameLeafSet_AndTheTableAccountsForEveryOne()
    {
        static HashSet<string> Leaves(string body) =>
            [.. Regex.Matches(body, @"public string Visit\((?<t>Bound\w+) n\)").Select(m => m.Groups["t"].Value)];

        string opText = OperandTextSource();
        int at = opText.IndexOf("private sealed class AsStringVisitor", StringComparison.Ordinal);
        Assert.True(at > 0, "AsStringVisitor is gone from OperandText");

        var strArg = Leaves(StrArgVisitorBody());
        var display = Leaves(opText[at..]);
        Assert.True(strArg.SetEquals(display),
            $"the two string channels no longer cover the same leaves — only in StrArgVisitor: "
            + $"[{string.Join(", ", strArg.Except(display).Order())}]; only in AsStringVisitor: "
            + $"[{string.Join(", ", display.Except(strArg).Order())}]");

        // Every leaf is DELEGATED (the shared rule), DIVERGENT BY DESIGN, or ADMITTED PER-FUNCTION (PB59 —
        // the conditional arm the pin test above owns). A leaf in no bucket is one nobody has decided about,
        // which is how PB25 shipped.
        // BoundCurrentRecord (kb/Work PB339) is DELEGATED: its image is the ISO §14.9.30.4 GR4 b) current
        // record — the record area sliced to the §13.18.43.4 GR16 byte count — written down once in
        // OperandText.CurrentRecordImage, so the intrinsic-argument channel forwards rather than going loud.
        // No intrinsic argument can BE one today (it is built only for a READ/RETURN INTO implicit MOVE), and
        // a loud arm for an unreachable-but-imageable operand is precisely PB25's wrong-stage failure.
        // BoundPredefinedNull (kb/Work PB1427) is DELEGATED to the one rule, which THROWS: the predefined NULL is
        // bound only in a §8.4.3.10.3 SR1 context and no intrinsic argument is one (COBOLNET2576 refuses it first).
        string[] delegated = ["BoundFieldOperand", "BoundFigurative", "BoundAllLiteral", "BoundCurrentRecord",
                              "BoundPredefinedNull"];
        // BoundAddressOperand (kb/Work PB1021) is class pointer: neither channel can image it, and both answer LOUD.
        string[] divergent = ["BoundBoolOperand", "BoundComputedOperand",
                              "BoundStringLiteral", "BoundOperandError", "BoundAddressOperand"];
        string[] admittedPerFunction = ["BoundNumericLiteral"];
        var unclassified = strArg.Except(delegated).Except(divergent).Except(admittedPerFunction).Order().ToList();
        Assert.True(unclassified.Count == 0,
            $"BoundOperand leaf/leaves [{string.Join(", ", unclassified)}] are handled by both string channels "
            + "but classified by neither bucket in this guard. Decide explicitly: does the intrinsic-argument "
            + "channel DELEGATE to OperandText.AsString (a shared rule), or answer differently on purpose? "
            + "Leaving it undecided is exactly how PB25's figurative arm went missing.");
    }

    /// <summary>PB49's second question, answered as a FACT: there is no second NUMERIC operand channel, so there
    /// is no numeric drift to guard. If a second one appears, this fails and the per-leaf treatment above has to
    /// be repeated for it.</summary>
    [Fact]
    public void ThereIsExactlyOneNumericOperandChannel()
    {
        var impls = new List<string>();
        foreach (string f in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs",
                     SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Src("Cobol.Net.Compiler"), f).Replace('\\', '/');
            if (rel.StartsWith("obj/", StringComparison.Ordinal) || rel.StartsWith("bin/", StringComparison.Ordinal))
                continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(f),
                         @"class (?<c>\w+)[^\n]*IBoundOperandVisitor<(?<t>[^>]+)>"))
                if (m.Groups["t"].Value.Trim() is "NumX")
                    impls.Add($"{m.Groups["c"].Value} ({rel})");
        }
        Assert.True(impls.Count == 1,
            $"expected exactly ONE numeric BoundOperand visitor (NumericRenderer) and found {impls.Count}: "
            + $"[{string.Join(", ", impls)}]. PB49 asked whether the numeric channels could drift the way the "
            + "string ones did; the answer was 'there is only one'. A second means that answer has expired and "
            + "the per-leaf agreement table above must be repeated for the numeric pair.");
    }
}
