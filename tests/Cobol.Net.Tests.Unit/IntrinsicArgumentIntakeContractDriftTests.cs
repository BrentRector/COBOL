// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY INTRINSIC-ARGUMENT INTAKE DECLARES WHAT IT DOES TO THE ARGUMENT'S VALUE (kb/Work PB251, row
/// RV-15.4.1-2).
/// </summary>
/// <remarks>
/// <para>
/// <c>IntrinsicRenderer</c> has ONE entry into the numeric operand renderer — <c>num.AsNum(operand, receiver)</c>
/// — and several INTAKES built over it whose value semantics differ: <c>RawArg</c> preserves the operand exactly,
/// <c>Arg</c> LANDS it into the exact <c>Int128</c> lane at the receiver's working scale and truncates past it,
/// <c>DecArg</c> lifts it to the SDIDI, <c>Dbl</c> approximates it in binary64, <c>IntArg</c> rescales it to
/// scale 0. A traceability row about "what happens to an argument" was adjudicated from ONE of them, on the
/// stated premise that it was the only one: the sentence "it renders an argument WITHOUT redefining its value" is
/// true of <c>RawArg</c> and false of <c>Arg</c>, and the row it settled sat wrong for months.
/// </para>
/// <para>
/// ⚠ A LIST OF TODAY'S INTAKES WOULD BE THE SAME MISTAKE ONE LEVEL UP. This guard names none of them: it reads
/// the renderer, finds every member whose body calls <c>num.AsNum(</c>, and requires that member's own doc
/// comment to declare an <c>INTAKE(&lt;class&gt;)</c> from the vocabulary the file documents. A SIXTH seam is
/// therefore not a hole in a list — it is a red test until its author writes down what it preserves.
/// </para>
/// </remarks>
public sealed class IntrinsicArgumentIntakeContractDriftTests
{
    /// <summary>The declared vocabulary — mirrored from <c>IntrinsicRenderer</c>'s "Argument rendering" banner so
    /// that inventing a class name is a deliberate act in two files rather than a typo in one.</summary>
    private static readonly HashSet<string> Classes =
        ["EXACT", "LIFTED", "ALIGNED", "LANDED", "INTEGRAL", "APPROXIMATED", "PREDICATE"];

    private static string RendererSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));

    /// <summary>A member declaration inside the class: an accessibility keyword at exactly one indent level.
    /// Local functions and lambdas are deeper-indented and so are never mistaken for members.</summary>
    private static readonly Regex MemberDecl =
        new(@"^    (?:private|public|internal|protected)\b[^\n]*?\b(?<name>\w+)\s*(?:\(|=>|\{|$)",
            RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>Every member of the renderer, as (name, declaration index, body-end index) in source order.</summary>
    private static List<(string Name, int Start, int End)> Members(string src)
    {
        var decls = MemberDecl.Matches(src).Select(m => (Name: m.Groups["name"].Value, Start: m.Index)).ToList();
        return [.. decls.Select((d, i) => (d.Name, d.Start, End: i + 1 < decls.Count ? decls[i + 1].Start : src.Length))];
    }

    /// <summary>A member's CODE — its span with every whole-line comment removed. ⛔ The renderer's own
    /// "Argument rendering" banner spells <c>num.AsNum(</c> while explaining this very contract, and without this
    /// the guard attributed that prose to whichever member happened to precede the banner and demanded a contract
    /// from it. A guard that reads comments as code reports the file's DOCUMENTATION as a defect.</summary>
    private static string CodeOf(string src, int start, int end) =>
        string.Concat(src[start..end].Split('\n')
            .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));

    /// <summary>The doc comment immediately above a declaration — the contiguous run of <c>///</c> lines.</summary>
    private static string DocAbove(string src, int declStart)
    {
        int end = declStart;
        int start = declStart;
        while (true)
        {
            int prev = src.LastIndexOf('\n', start - 2);
            if (prev < 0) break;
            string line = src[(prev + 1)..start];
            if (!line.TrimStart().StartsWith("///", StringComparison.Ordinal)) break;
            start = prev + 1;
        }
        return src[start..end];
    }

    /// <summary>
    /// ⛔ THE GUARD: a member that reaches the operand renderer declares what it does to the value.
    /// </summary>
    [Fact]
    public void EveryArgumentIntake_DeclaresItsValueContract()
    {
        string src = RendererSource();
        var intakes = Members(src)
            .Where(m => CodeOf(src, m.Start, m.End).Contains("num.AsNum(", StringComparison.Ordinal))
            .ToList();

        // ⛔ A RUN MUST ASSERT ITS POPULATION (feedback_verdict_evidence_invariant): if the member regex ever
        // stops matching, an empty set would pass this test silently and the contract would be unguarded.
        // Eight since kb/Work PB651: Arg, LeadArg and the lane predicates read the indexed operands through RawArg (the
        // one entry that takes a §15.3 type-6 operand's integer), so they no longer call num.AsNum themselves.
        Assert.True(intakes.Count >= 8,
            $"expected at least 8 argument intakes in IntrinsicRenderer, found {intakes.Count} " +
            $"([{string.Join(", ", intakes.Select(m => m.Name))}]) — the member scan is blind");

        var undeclared = intakes
            .Where(m => !Regex.IsMatch(DocAbove(src, m.Start), @"INTAKE\((?<c>[A-Z]+)\)"))
            .Select(m => m.Name)
            .ToList();
        Assert.True(undeclared.Count == 0,
            "these IntrinsicRenderer members call num.AsNum( and declare no INTAKE(<class>) contract — say what "
            + "each does to the argument's VALUE (kb/Work PB251): " + string.Join(", ", undeclared));

        var wrongClass = Regex.Matches(src, @"INTAKE\((?<c>[A-Za-z]+)\)")
            .Select(m => m.Groups["c"].Value)
            .Where(c => !Classes.Contains(c))
            .Distinct()
            .ToList();
        Assert.True(wrongClass.Count == 0,
            "INTAKE class names outside the documented vocabulary: " + string.Join(", ", wrongClass));
    }

    /// <summary>
    /// ⛔ THE ONE INTAKE THAT CAN CHANGE A VALUE STAYS NAMED AS SUCH. <c>Arg</c> is the only intake that lands an
    /// SDIDI operand at a compile-time working scale, and PB251's defect was a function whose §15.67.4 r1 value
    /// the definition FIXES being materialized through a receiver-derived scale. If <c>Arg</c> ever stops being
    /// LANDED, or a second member acquires that class, the reasoning above it has to be redone.
    /// </summary>
    [Fact]
    public void OnlyArg_IsTheLandingIntake()
    {
        string src = RendererSource();
        var landed = Members(src)
            .Where(m => Regex.IsMatch(DocAbove(src, m.Start), @"INTAKE\(LANDED\)"))
            .Select(m => m.Name)
            .ToList();
        Assert.Equal(["Arg"], landed);
    }

    /// <summary>
    /// ⛔ THE NUMVAL FAMILY DOES NOT REACH THE LANDING INTAKE AT ALL. §15.67.4 r1 / §15.68.4 r1 fix the returned
    /// value with no arithmetic-mode qualification, and §15.4.1's implementor latitude reaches only a function
    /// whose definition does NOT otherwise specify it — so NUMVAL and NUMVAL-C render on the SDIDI carrier in
    /// EVERY mode and no compile-time working scale exists for them. This asserts the shape that makes that true:
    /// their dispatch stands BEFORE the arithmetic-mode branch, and no <c>WorkingScale</c> call is left in the
    /// renderer at all. (A working scale in this file is what truncated
    /// <c>DISPLAY FUNCTION NUMVAL("0.1234567")</c> to 0.123456.)
    /// </summary>
    [Fact]
    public void NumvalFamily_HasNoCompileTimeWorkingScale()
    {
        string src = RendererSource();
        Assert.DoesNotContain("WorkingScale(ReceiverContext.SdidiLandingScaleFloor)", src, StringComparison.Ordinal);

        int fixedArm = src.IndexOf("IntrinsicCarrierRules.ValueFixedByDefinition(sig.RuntimeMethod) && RenderDec(ic)", StringComparison.Ordinal);
        int modeBranch = src.IndexOf("if (num.StandardDecimal)", StringComparison.Ordinal);
        Assert.True(fixedArm > 0, "the §15.4.1 value-fixed-by-definition arm is gone from IntrinsicRenderer");
        Assert.True(modeBranch > 0, "IntrinsicRenderer no longer branches on the arithmetic mode");
        Assert.True(fixedArm < modeBranch,
            "the NUMVAL/NUMVAL-C arm must run BEFORE the arithmetic-mode dispatch — §15.67.4 r1 carries no mode "
            + "qualification, so putting it inside the standard branch is the PB251 defect returning");
    }
}
