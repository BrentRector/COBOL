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
/// <b>§14.9.13.3 SR2</b> fixes the count against the subjects. The selection object was nevertheless written
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
///
/// <para>⛔ <b>THE BRACKETED NOT BELONGS TO THE FIVE ALTERNATIVES THAT PRINT IT, NEVER TO THE WHOLE OBJECT (kb/Work
/// PB2722).</b> §14.9.13.2's figure note: "The bracketed `NOT` applies only to the first five alternatives, exactly as
/// stacked". The NOT was once hoisted over every alternative (<c>NOT? evaluateWhenItem</c>), and because ANTLR enters a
/// greedy <c>?</c> whenever both readings are viable it stole the leading NOT of every condition-2 and
/// partial-expression-1, which own their own NOT: <c>WHEN NOT A &gt; B AND A &gt; B</c> bound as
/// <c>NOT (A &gt; B AND A &gt; B)</c> instead of §8.8.4.11.3's <c>(NOT A &gt; B) AND A &gt; B</c> — a silent wrong
/// answer — and <c>WHEN NOT ANY</c> compiled clean. So <c>NOT?</c> stands on the operand alternatives (plus TRUE/FALSE
/// and ANY, parsed only to be refused by name as COBOLNET3710), never on <c>condition</c> or <c>partialExpression</c>,
/// and never outside the object.</para>
/// </summary>
public sealed class EvaluateSelectionObjectArityDriftTests
{
    /// <summary>The grammar with its line comments removed BEFORE a rule is located: a prose comment may hold a ';'
    /// (a §-reference's punctuation), which would otherwise end the rule body early (PartialExpressionSpineDriftTests'
    /// measured lesson).</summary>
    private static string Grammar() => Regex.Replace(File.ReadAllText(Path.Combine(TestRepo.Root,
        "src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolControlFlow.g4")), @"//[^\r\n]*", "");

    private static string RuleBody(string rule)
    {
        var m = Regex.Match(Grammar(), $@"^{rule}\s*\r?\n\s*:(?<body>.*?);", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, $"grammar rule '{rule}' not found — if it was renamed this guard must move with it");
        return m.Groups["body"].Value;
    }

    /// <summary>The arity itself: ONE selection object per position (§14.9.13.2). A regrown <c>+</c> or <c>*</c>
    /// re-opens the peel.</summary>
    [Fact]
    public void WhenPhrase_HoldsExactlyOneSelectionObjectPerPosition()
    {
        string body = RuleBody("evaluateWhenPhrase").Trim();
        Assert.False(Regex.IsMatch(body, @"evaluateWhenItem\s*[+*]"),
            "evaluateWhenPhrase repeats evaluateWhenItem by juxtaposition. ISO §14.9.13.2 repeats selection objects "
            + $"ONLY through ALSO, and the repetition is what let `WHEN FUNCTION SQRT(X) > 1` parse as TWO objects. Body: {body}");
        Assert.Matches(@"^WHEN\s+evaluateWhenItem\b", body);
    }

    /// <summary>The ALSO repetition, which the standard DOES license, must stay — otherwise this guard could be
    /// "satisfied" by deleting multi-object EVALUATE altogether.</summary>
    [Fact]
    public void WhenPhrase_StillRepeatsObjectsThroughAlso()
    {
        Assert.Matches(@"ALSO\s+evaluateWhenItem\s*\)\s*\*", RuleBody("evaluateWhenPhrase"));
    }

    /// <summary>The selection object's alternatives, comments stripped, each with its leading <c>NOT?</c> (if any)
    /// separated from the shape it precedes. A left-edge predicate stays on the shape.</summary>
    private static (bool Not, string Shape)[] ObjectAlternatives() =>
        RuleBody("evaluateWhenItem")
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => Regex.Replace(a, @"//.*", "").Trim())
            .Where(a => a.Length > 0)
            .Select(a => (Regex.IsMatch(a, @"\bNOT\?"), Regex.Replace(Regex.Replace(a, @"\bNOT\?\s*", ""), @"\s+", " ").Trim()))
            .ToArray();

    /// <summary>⛔ kb/Work PB2722 — the bracketed NOT stands on exactly the alternatives §14.9.13.2 prints it on (the
    /// four operand shapes that spell identifier-2 / literal-2 / arithmetic-expression-2 / boolean-expression-2 /
    /// range-expression) plus the two words parsed only so COBOLNET3710 can refuse them by name — and on NEITHER
    /// <c>condition</c> nor <c>partialExpression</c>, whose own parse owns their leading NOT. A <c>NOT?</c> on either of
    /// those two, or outside the object, is the greedy hoist that bound <c>NOT A &gt; B AND A &gt; B</c> as
    /// <c>NOT (A &gt; B AND A &gt; B)</c>.</summary>
    [Fact]
    public void BracketedNot_StandsOnThePrintedAlternativesOnly()
    {
        var alts = ObjectAlternatives();
        string shown = string.Join(" | ", alts.Select(a => (a.Not ? "NOT? " : "") + a.Shape));
        string[] negatable = ["valueRange", "addressIdentifier", "valueOperand", "{boolExprAhead()}? booleanExpression",
            "booleanLiteral", "ANY"];
        string[] ownTheirNot = ["condition", "partialExpression"];
        Assert.Equal(negatable.Concat(ownTheirNot).OrderBy(s => s, StringComparer.Ordinal),
            alts.Select(a => a.Shape).OrderBy(s => s, StringComparer.Ordinal));
        foreach (var (not, shape) in alts)
            Assert.True(not == negatable.Contains(shape),
                $"evaluateWhenItem's `{shape}` alternative {(not ? "carries" : "lacks")} the bracketed NOT?. ISO §14.9.13.2: "
                + "\"The bracketed `NOT` applies only to the first five alternatives, exactly as stacked\" — and condition-2 "
                + $"/ partial-expression-1 own their leading NOT (§8.8.4.10.1, SR5). Alternatives: [{shown}]");
        Assert.DoesNotContain("NOT", RuleBody("evaluateWhenPhrase"), StringComparison.Ordinal);
    }

    /// <summary>⛔ The alternative ORDER is load-bearing and deliberately unchanged: <c>valueOperand</c> precedes
    /// <c>condition</c>, so a bare name under a VALUE subject stays an equality operand (§14.9.13.4 GR4a6). The
    /// subject-dependent reclassification is the binder's job, not the parser's.</summary>
    [Fact]
    public void WhenItem_KeepsValueOperandBeforeCondition()
    {
        var alts = ObjectAlternatives().Select(a => a.Shape).ToArray();
        int value = Array.IndexOf(alts, "valueOperand"), cond = Array.IndexOf(alts, "condition");
        Assert.True(value >= 0 && cond >= 0, $"expected both arms; got [{string.Join(" | ", alts)}]");
        Assert.True(value < cond,
            "evaluateWhenItem's alternatives were reordered so `condition` precedes `valueOperand`. ISO Table 15 "
            + "(§14.9.13.3 SR10) makes a selection object's legality depend on the SUBJECT, so this order cannot "
            + "encode it: the reorder silently turns `EVALUATE X / WHEN <level-88>` from the GR4a6 equality "
            + "comparison into a truth-value test. The subject-dependent arm belongs in EvaluateBinder.");
        int word = Array.IndexOf(alts, "booleanLiteral");
        Assert.True(word >= 0 && word < cond,
            "evaluateWhenItem's booleanLiteral alternative must precede condition, so a bare TRUE / FALSE object is "
            + "the selection-object WORD (§14.9.13.2), not a one-word condition; got [" + string.Join(" | ", alts) + "]");
    }

    /// <summary>⛔ kb/Work PB2722 end-to-end at the PARSE level: whose NOT is it? On a bare operand, a range or a word
    /// it is the selection object's bracketed NOT; at the head of a condition or a partial expression it is that
    /// parse's own, and the object carries none.</summary>
    [Theory]
    [InlineData("EVALUATE W-Z\nWHEN NOT 4\nCONTINUE\nEND-EVALUATE.", true)]
    [InlineData("EVALUATE W-Z\nWHEN NOT 1 THRU 3\nCONTINUE\nEND-EVALUATE.", true)]
    [InlineData("EVALUATE W-Z\nWHEN NOT ANY\nCONTINUE\nEND-EVALUATE.", true)]
    [InlineData("EVALUATE TRUE\nWHEN NOT TRUE\nCONTINUE\nEND-EVALUATE.", true)]
    [InlineData("EVALUATE TRUE\nWHEN NOT W-Z > 1 AND W-Z > 1\nCONTINUE\nEND-EVALUATE.", false)]
    [InlineData("EVALUATE W-Z\nWHEN NOT > 5 AND < 2\nCONTINUE\nEND-EVALUATE.", false)]
    [InlineData("EVALUATE W-Z\nWHEN NOT NUMERIC\nCONTINUE\nEND-EVALUATE.", false)]
    public void ALeadingNot_BelongsToTheShapeThatPrintsIt(string body, bool objectsOwn)
    {
        var item = Assert.Single(Descendants(ParseClean(body)).OfType<CobolParserCore.EvaluateWhenItemContext>());
        Assert.True((item.NOT() is not null) == objectsOwn,
            $"`{item.GetText()}`: the leading NOT was read as {(item.NOT() is not null ? "the selection object's" : "the condition's / partial expression's")}"
            + " — ISO §14.9.13.2 prints the bracketed NOT on identifier-2, literal-2, arithmetic-expression-2, "
            + "boolean-expression-2 and range-expression only; a condition-2 or partial-expression-1 owns its own.");
        if (!objectsOwn)
            Assert.Contains(Descendants(item).OfType<ITerminalNode>(), t => t.Symbol.Type == CobolParserCore.NOT);
    }

    private static IParseTree ParseClean(string body)
    {
        string src = "IDENTIFICATION DIVISION.\nPROGRAM-ID. EVALNOT.\nDATA DIVISION.\n"
            + "WORKING-STORAGE SECTION.\n01 W-Z PIC 9 VALUE 4.\nPROCEDURE DIVISION.\nMAIN.\n" + body + "\n";
        string path = Path.Combine(Path.GetTempPath(), "cn_evalnot_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics.Select(d => d.ToString())));
            return Assert.IsAssignableFrom<IParseTree>(tree);
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
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
