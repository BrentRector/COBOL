// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN EVALUATE WHEN GROUP HOLDS EXACTLY ONE SELECTION OBJECT, AND THE UNLICENSED REPETITION SILENTLY
/// MISCOMPILED LEGAL SOURCE (fix-queue PB45).
///
/// <para><b>ISO §14.9.13.2</b>'s general format is
/// <c>{ { WHEN selection-object [ ALSO selection-object ] … } … imperative-statement-1 } …</c> — selection objects
/// repeat ONLY through ALSO (which is <c>evaluateWhenPhrase</c>'s own loop), never by juxtaposition — and
/// <b>§14.9.13.3 SR2</b> fixes the count against the subjects. <c>evaluateWhenGroup</c> was nevertheless written
/// <c>NOT? evaluateWhenItem+</c>.</para>
///
/// <para><b>What the extra repetition bought.</b> It gave <c>WHEN FUNCTION SQRT(W-Z) &gt; 1</c> a second parse:
/// take <c>FUNCTION SQRT</c> as a bare zero-argument object and re-read the ARGUMENT PARENTHESIS as a second,
/// parenthesised object <c>(W-Z) &gt; 1</c>. The correct reading cannot consume the trailing <c>&gt; 1</c> once the
/// item ends, so under <c>+</c> it was not viable and only the peel survived — binding as a VALUE object under an
/// <c>EVALUATE TRUE</c> subject: a clean compile that threw at RUN TIME. <c>FUNCTION PI &gt; 1</c> always worked,
/// having no parenthesis to peel, which is what identified the parenthesis as the discriminator.</para>
///
/// <para>⛔ <b>THE FIX WAS THE ARITY, AND MUST NOT BECOME AN ALTERNATIVE REORDER.</b> Putting <c>condition</c>
/// before <c>valueOperand</c> in <c>evaluateWhenItem</c> would also retarget <c>EVALUATE X / WHEN Y</c> where Y is
/// a level-88 condition-name — a value comparison (§14.9.13.4 GR4a6) silently becoming a truth-value test —
/// because <b>Table 15</b> makes the object's legality depend on the SUBJECT, which no context-free ordering can
/// express. The subject-dependent half lives in <c>EvaluateBinder.BindWhenItem</c> via
/// <c>ConditionBinder.BareOperandAsCondition</c>. Both facts are pinned below.</para>
/// </summary>
public sealed class EvaluateSelectionObjectArityDriftTests
{
    private static string Grammar() => File.ReadAllText(Path.Combine(TestRepo.Root,
        "src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolControlFlow.g4"));

    private static string RuleBody(string rule)
    {
        var m = Regex.Match(Grammar(), $@"^{rule}\s*\r?\n\s*:(?<body>.*?);", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, $"grammar rule '{rule}' not found — if it was renamed this guard must move with it");
        return m.Groups["body"].Value;
    }

    /// <summary>The arity itself: ONE selection object per position (§14.9.13.2). A regrown <c>+</c> or <c>*</c>
    /// re-opens the peel.</summary>
    [Fact]
    public void WhenGroup_HoldsExactlyOneSelectionObject()
    {
        string body = RuleBody("evaluateWhenGroup").Trim();
        Assert.False(Regex.IsMatch(body, @"evaluateWhenItem\s*[+*]"),
            "evaluateWhenGroup repeats evaluateWhenItem. ISO §14.9.13.2 repeats selection objects ONLY through "
            + $"ALSO, and the repetition is what let `WHEN FUNCTION SQRT(X) > 1` parse as TWO objects. Body: {body}");
        Assert.Contains("evaluateWhenItem", body, StringComparison.Ordinal);
    }

    /// <summary>The ALSO repetition, which the standard DOES license, must stay — otherwise this guard could be
    /// "satisfied" by deleting multi-object EVALUATE altogether.</summary>
    [Fact]
    public void WhenPhrase_StillRepeatsObjectsThroughAlso()
    {
        Assert.Matches(@"ALSO\s+evaluateWhenGroup\s*\)\s*\*", RuleBody("evaluateWhenPhrase"));
    }

    /// <summary>⛔ The alternative ORDER is load-bearing and deliberately unchanged: <c>valueOperand</c> precedes
    /// <c>condition</c>, so a bare name under a VALUE subject stays an equality operand (§14.9.13.4 GR4a6). The
    /// subject-dependent reclassification is the binder's job, not the parser's.</summary>
    [Fact]
    public void WhenItem_KeepsValueOperandBeforeCondition()
    {
        var alts = RuleBody("evaluateWhenItem")
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => Regex.Replace(a, @"//.*", "").Trim())
            .Where(a => a.Length > 0).ToArray();
        int value = Array.IndexOf(alts, "valueOperand"), cond = Array.IndexOf(alts, "condition");
        Assert.True(value >= 0 && cond >= 0, $"expected both arms; got [{string.Join(" | ", alts)}]");
        Assert.True(value < cond,
            "evaluateWhenItem's alternatives were reordered so `condition` precedes `valueOperand`. ISO Table 15 "
            + "(§14.9.13.3 SR10) makes a selection object's legality depend on the SUBJECT, so this order cannot "
            + "encode it: the reorder silently turns `EVALUATE X / WHEN <level-88>` from the GR4a6 equality "
            + "comparison into a truth-value test. The subject-dependent arm belongs in EvaluateBinder.");
    }

    /// <summary>The defect end-to-end at the PARSE level: the function's argument list must belong to the
    /// function. A peeled parse leaves <c>functionArgList</c> null with a '(' still standing to its right.</summary>
    [Theory]
    [InlineData("EVALUATE TRUE\nWHEN FUNCTION SQRT(W-Z) > 1\nCONTINUE\nEND-EVALUATE.")]
    [InlineData("EVALUATE TRUE\nWHEN FUNCTION UPPER-CASE(\"a\") = \"A\"\nCONTINUE\nEND-EVALUATE.")]
    [InlineData("EVALUATE TRUE ALSO TRUE\nWHEN FUNCTION SQRT(W-Z) > 1 ALSO W-Z = 4\nCONTINUE\nEND-EVALUATE.")]
    public void ArgumentParenthesis_BelongsToTheFunction_NotToASecondSelectionObject(string body)
    {
        string src = "IDENTIFICATION DIVISION.\nPROGRAM-ID. EVALARITY.\nDATA DIVISION.\n"
            + "WORKING-STORAGE SECTION.\n01 W-Z PIC 9 VALUE 4.\nPROCEDURE DIVISION.\nMAIN.\n" + body + "\n";
        string path = Path.Combine(Path.GetTempPath(), "cn_evalarity_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics.Select(d => d.ToString())));
            Assert.NotNull(tree);

            var calls = Descendants(tree).OfType<CobolParserCore.FunctionCallContext>().ToList();
            Assert.NotEmpty(calls);
            foreach (var call in calls)
                Assert.True(call.functionArgList() is not null,
                    $"the function-identifier '{call.GetText()}' took NO argument list — its argument parenthesis "
                    + "was peeled off and re-read as a second selection object (PB45). ISO §8.4.3.2 SR6: when a "
                    + "function's definition permits arguments, that left parenthesis is always its argument list.");
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    private static IEnumerable<IParseTree> Descendants(IParseTree node)
    {
        yield return node;
        for (int i = 0; i < node.ChildCount; i++)
            foreach (var d in Descendants(node.GetChild(i))) yield return d;
    }
}
