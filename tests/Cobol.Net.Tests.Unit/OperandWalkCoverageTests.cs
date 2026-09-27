// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY OPERAND WRAPPER THE WALK SERVES MUST HOLD SOMETHING THE WALK CAN BIND (kb/Work PB171).
///
/// <para><b>The defect this exists for.</b> <c>ExpressionBinder.BindOperandExprCore</c> is a breadth-first walk
/// over an operand wrapper's subtree with an arm per bindable shape, and its fallback used to
/// <c>return new BoundNumLiteral("0")</c> — so a grammar alternative nobody wrote an arm for did not fail, it
/// became ZERO. The grammar's <c>valueOperand : arithmeticExpression | nonNumericLiteral</c> names
/// <c>nonNumericLiteral</c> DIRECTLY, bypassing the <c>literal</c> wrapper the walk knew, and that alternative
/// had no arm: <c>IF "ABC" IS POSITIVE</c> compiled clean and evaluated <c>0 &gt; 0</c>.</para>
///
/// <para><b>⚠ AND THE GUARD THAT WAS CREDITED WITH PREVENTING IT NEVER LOOKED.</b> The fallback's own comment
/// said "ArithmeticSendingOperandDriftTests exists to keep the grammar from growing one this walk cannot see".
/// That test compares the four ARITHMETIC operand rules' bodies to each other and never mentions
/// <c>comparisonOperand</c> or <c>valueOperand</c> — the rule the walk was actually failing on. A green guard
/// over the wrong subject is worse than none, because it stops anyone looking
/// (feedback_green_gates_arent_evidence). THIS test guards the property that comment claimed.</para>
///
/// <para><b>The property, stated exactly:</b> for every rule whose subtree the walk can be handed, EVERY
/// alternative must reach at least one arm of the walk — directly, or through a wrapper rule that itself has the
/// property. An alternative that reaches none is a shape the walk drains on, which is now a
/// <c>BoundExprError</c> at bind time rather than a silent zero, but is still a hole in the binder and a
/// BUILD-time failure here. That is the shape that makes the NEXT case automatic: a new <c>valueOperand</c>
/// alternative fails this test instead of degrading to a wrong answer in the field.</para>
///
/// <para>⚠ It reads the GRAMMAR SOURCE, not the generated parser: the property is about what the <c>.g4</c>
/// can produce, and a generated-parser check would pass on a rule that merely happens not to be exercised.</para>
/// </summary>
public sealed class OperandWalkCoverageTests
{
    /// <summary>The rules whose subtree reaches <c>BindOperandExprCore</c>: the public
    /// <c>BindOperandExpr</c> entry (ConditionBinder's sign condition, via <c>comparisonOperand</c>) and
    /// <c>BindExprCore</c>'s <c>_ =&gt;</c> default arm (the arithmetic operand wrappers — all four mixed-role
    /// rules included, since their Format-2 sending operand binds through the wrapper: ADD's
    /// <c>addToPhrase</c> and SUBTRACT's <c>subtractFromOperand</c> as well as the BY / INTO twins; kb/Work
    /// PB1142).</summary>
    private static readonly string[] ServedRules =
    [
        "comparisonOperand", "valueOperand",
        "addOperand", "subtractOperand", "multiplyOperand", "divideOperand",
        "addToPhrase", "subtractFromOperand", "divideIntoOperand", "multiplyByOperand",
    ];

    /// <summary>The walk's arms, in <c>BindOperandExprCore</c>'s own order. Keep this list in step with that
    /// method — it is the ONE place the two must agree, and the test names the file to edit when they do not.
    /// </summary>
    private static readonly string[] WalkArms =
        ["arithmeticExpression", "functionCall", "inlineMethodInvocation", "nonNumericLiteral",
         "literal", "dataReference"];

    private static readonly string[] GrammarFiles =
    [
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "CobolParserCore.g4"),
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolExpressions.g4"),
    ];

    /// <summary>rule-name → its alternatives, each as the list of RULE references it makes (lowercase-initial
    /// words; terminals and keywords are irrelevant to "can the walk bind something here").</summary>
    private static Dictionary<string, List<List<string>>> LoadRules()
    {
        var rules = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal);
        foreach (string rel in GrammarFiles)
        {
            string path = Path.Combine(TestRepo.Root, rel);
            if (!File.Exists(path)) continue;
            string g4 = File.ReadAllText(path);
            // Strip comments so a rule name mentioned in prose is never read as a reference.
            g4 = Regex.Replace(g4, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            g4 = Regex.Replace(g4, @"//[^\n]*", " ");
            foreach (Match m in Regex.Matches(g4,
                @"^(?<name>[a-z][A-Za-z0-9_]*)\s*\r?\n?\s*:(?<body>.*?);", RegexOptions.Multiline | RegexOptions.Singleline))
            {
                string name = m.Groups["name"].Value;
                if (rules.ContainsKey(name)) continue;   // the first definition wins; imports do not redefine
                var alts = m.Groups["body"].Value
                    .Split('|')
                    .Select(a => Regex.Matches(a, @"\b[a-z][A-Za-z0-9_]*\b").Select(x => x.Value).ToList())
                    .ToList();
                rules[name] = alts;
            }
        }
        return rules;
    }

    /// <summary>True when every alternative of <paramref name="rule"/> reaches a walk arm.
    /// <para>⛔ <paramref name="memo"/> MEMOIZES THE VERDICT, NOT THE VISIT (kb/Work PB224). The first cut
    /// threaded one <c>HashSet&lt;string&gt; seen</c> and returned <c>true</c> on re-entry — "a cycle cannot be
    /// the reason a leaf is uncovered" — which silently extends to a rule that was visited, found UNCOVERED, and
    /// then absorbed: <c>alt.Any(…)</c> stops at the first alternative member that answers true, leaving the
    /// rejected one in <c>seen</c>, and the next query for it answers <c>true</c>. That is a guard reporting a
    /// hole as covered — the exact green-over-the-wrong-subject shape this file's own docstring indicts. It is
    /// LATENT on today's grammar (measured: no <c>Covered</c> call returns false, so nothing is ever absorbed),
    /// and latent is not fixed. A rule still IN PROGRESS is seeded optimistically, so a genuine cycle keeps the
    /// old behaviour; the real verdict overwrites the seed on the way out, so a SECOND query gets the truth.</para></summary>
    private static bool Covered(string rule, Dictionary<string, List<List<string>>> rules,
        Dictionary<string, bool> memo, out string why)
    {
        why = "";
        if (WalkArms.Contains(rule)) return true;
        if (memo.TryGetValue(rule, out bool cached))
        {
            if (!cached) why = $"rule '{rule}' reaches none of the walk's arms (already determined)";
            return cached;
        }
        memo[rule] = true;                                 // a cycle cannot be the reason a leaf is uncovered
        if (!rules.TryGetValue(rule, out var alts))
        {
            why = $"rule '{rule}' is not defined in the grammar";
            return memo[rule] = false;
        }
        foreach (var alt in alts)
        {
            if (alt.Any(r => WalkArms.Contains(r))) continue;
            if (alt.Any(r => r != rule && Covered(r, rules, memo, out _))) continue;
            why = $"alternative [{string.Join(" ", alt.DefaultIfEmpty("(empty)"))}] of rule '{rule}' reaches none "
                + $"of the walk's arms ({string.Join(" | ", WalkArms)})";
            return memo[rule] = false;
        }
        return memo[rule] = true;
    }

    /// <summary>⛔ THE FACT PB171 WAS HIDING BEHIND. Before the <c>nonNumericLiteralContext</c> arm was added to
    /// <c>BindOperandExprCore</c>, <c>valueOperand</c>'s second alternative reached NO arm and this fact fails —
    /// which is how the guard was proved to guard (feedback_green_gates_arent_evidence: a coverage test that has
    /// never failed is the exact thing this defect was hiding behind; the arm was deleted once and this test went
    /// red before it was trusted).</summary>
    [Fact]
    public void EveryOperandWrapperTheWalkServes_ReachesABindableArm()
    {
        var rules = LoadRules();
        foreach (string served in ServedRules)
        {
            Assert.True(rules.ContainsKey(served),
                $"grammar rule '{served}' not found — if it was renamed, this guard must move with it, because "
                + "the walk still serves whatever replaced it (ExpressionBinder.BindOperandExprCore)");
            // A FRESH memo per served rule: a cached `false` cannot carry the alternative-level `why` that names
            // the shape to fix, and the walk is tiny, so the precise message is worth the re-derivation.
            bool ok = Covered(served, rules, [], out string why);
            Assert.True(ok,
                $"ExpressionBinder.BindOperandExprCore has no arm for a shape '{served}' can produce: {why}. "
                + "Add the arm to the walk (and route it through an EXISTING screen — a second copy of a literal "
                + "dispatch is DA3's defect), or this operand degrades at bind time.");
        }
    }

    /// <summary>The walk's arm list above is a hand-written mirror of the binder's, so it is pinned to the binder
    /// SOURCE: every name in <see cref="WalkArms"/> must actually appear as a matched context type in
    /// <c>BindOperandExprCore</c>. Without this, the coverage fact could pass by listing arms the walk does not
    /// have — a well-formed and worthless guard.</summary>
    [Fact]
    public void TheWalkArmsList_MatchesTheBindersOwnArms()
    {
        string src = File.ReadAllText(Path.Combine(TestRepo.Root,
            "src", "Cobol.Net.Compiler", "Binding", "Procedure", "ExpressionBinder.cs"));
        int start = src.IndexOf("private BoundExpr BindOperandExprCore", StringComparison.Ordinal);
        Assert.True(start >= 0, "BindOperandExprCore not found — the walk moved; move this guard with it");
        string body = src[start..];
        int end = body.IndexOf("\n    }", StringComparison.Ordinal);
        if (end > 0) body = body[..end];

        foreach (string arm in WalkArms)
        {
            string ctx = "Core." + char.ToUpperInvariant(arm[0]) + arm[1..] + "Context";
            Assert.True(body.Contains(ctx, StringComparison.Ordinal),
                $"'{arm}' is listed as a walk arm here but BindOperandExprCore does not match {ctx}. The two "
                + "lists must agree or the coverage fact above is measuring nothing.");
        }
    }
}
