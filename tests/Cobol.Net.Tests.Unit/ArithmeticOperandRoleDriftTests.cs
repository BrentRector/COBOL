// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Procedure;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY ARITHMETIC VERB ASKS ONE RECEIVER CLASSIFICATION, AND IT COVERS EVERY OPERAND ARM THE GRAMMAR WRITES
/// (kb/Work PB1142, CLAUDE.md rule 5).
///
/// <para><b>The defect this exists for.</b> ISO §14.9.2.2 / §14.9.44.2 / §14.9.26.2 / §14.9.12.2 print the
/// TO / FROM / BY / INTO operand as a RECEIVER in Format 1 and as ONE sending operand in Format 2, and the grammar
/// parses the union in mixed rules (four then; two since kb/Work PB2114 made ADD, SUBTRACT and DIVIDE share one).
/// Each verb binder used to screen Format 1 with its own hand-written list of
/// NON-receivers — <c>literal() || functionCall()</c> — and when kb/Work PB428 gave all four rules an
/// <c>inlineMethodInvocation</c> alternative, none of the four lists grew: <c>ADD A TO O :: "M"</c> compiled clean
/// with the receiver silently dropped, and the DIVIDE twin crashed the emitter. §8.4.3.4.3 SR1 ("Inline method
/// invocation shall not be specified as a receiving operand") was credited as holding STRUCTURALLY, but these
/// rules are mixed-role, so the binder screen is the only enforcement.</para>
///
/// <para><b>The property, stated exactly:</b> <c>ArithmeticOperandRole</c> defines the receiver POSITIVELY — a
/// node produced through a receiving rule, and nothing else — so a sending alternative any mixed rule gains is a
/// non-receiver with no edit. What can still drift is the classifier's two lists: a NEW mixed rule the verbs do
/// not hand it, or a NEW receiving rule it would misread as sending (a loud rejection of legal source). Both are
/// pinned here against the <c>.g4</c>, and the partition itself is pinned on real parse trees for every verb and
/// every operand arm.</para>
///
/// <para>⚠ The rule sets are read from the GRAMMAR SOURCE (comments stripped): the property is about what the
/// <c>.g4</c> admits, not about what some test happens to exercise.</para>
/// </summary>
public sealed class ArithmeticOperandRoleDriftTests
{
    private static readonly string GrammarDir = Path.Combine("src", "Cobol.Net.Frontend", "Grammar");

    /// <summary>rule-name → the rule names its body references, over every grammar file, comments stripped.</summary>
    private static Dictionary<string, HashSet<string>> RuleReferences()
    {
        var rules = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(Path.Combine(TestRepo.Root, GrammarDir), "*.g4",
                     SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string g4 = File.ReadAllText(path);
            g4 = Regex.Replace(g4, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            g4 = Regex.Replace(g4, @"//[^\n]*", " ");
            foreach (Match m in Regex.Matches(g4, @"^(?<name>[a-z][A-Za-z0-9_]*)\s*\r?\n?\s*:(?<body>.*?);",
                         RegexOptions.Multiline | RegexOptions.Singleline))
                rules.TryAdd(m.Groups["name"].Value, Regex.Matches(m.Groups["body"].Value, @"\b[a-z][A-Za-z0-9_]*\b")
                    .Select(x => x.Value).ToHashSet(StringComparer.Ordinal));
        }
        return rules;
    }

    /// <summary>The grammar rule name a generated context type stands for (<c>AddToPhraseContext</c> →
    /// <c>addToPhrase</c>).</summary>
    private static string RuleName(Type contextType)
    {
        string n = contextType.Name;
        Assert.EndsWith("Context", n);
        return char.ToLowerInvariant(n[0]) + n[1..^"Context".Length];
    }

    private static bool IsReceivingRule(string rule) => rule.StartsWith("receiving", StringComparison.Ordinal);

    private static readonly string[] SendingIdentifierFormats = ["functionCall", "inlineMethodInvocation"];

    /// <summary>⛔ THE SET OF MIXED RULES IS THE GRAMMAR'S, NOT A HAND-KEPT LIST. A rule is mixed when it offers a
    /// receiving rule beside a sending identifier format (§8.4.3.1.2 Format 1 or Format 4). A new one added to
    /// the grammar fails here until it joins <c>ArithmeticOperandRole.MixedRules</c> — the point at which its
    /// author is told the rule needs its verb binder's <c>Format1Receivers</c> call (this fact cannot see the call
    /// itself; <c>TheClassifier_PartitionsEveryOperandArm</c> and the Conformance matrix cover behaviour). Proved
    /// to guard: dropping <c>MultiplyByOperandContext</c> from <c>MixedRules</c> fails this fact by name.</summary>
    [Fact]
    public void MixedRules_AreExactlyTheGrammarRulesOfferingBothRoles()
    {
        var rules = RuleReferences();
        var fromGrammar = rules
            .Where(kv => kv.Value.Any(IsReceivingRule) && kv.Value.Overlaps(SendingIdentifierFormats))
            .Select(kv => kv.Key)
            .Order(StringComparer.Ordinal)
            .ToList();
        var classified = ArithmeticOperandRole.MixedRules.Select(RuleName).Order(StringComparer.Ordinal).ToList();
        Assert.True(fromGrammar.Count >= 2,
            $"only {fromGrammar.Count} mixed-role rules found — the grammar scan lost sites, so the comparison below "
            + "proves nothing (feedback_verdict_evidence_invariant)");
        Assert.True(fromGrammar.SequenceEqual(classified),
            $"the grammar's mixed-role operand rules [{string.Join(", ", fromGrammar)}] differ from "
            + $"ArithmeticOperandRole.MixedRules [{string.Join(", ", classified)}]. A rule that offers a receiver "
            + "beside a sending identifier needs the Format-1 receiver screen (ISO §8.4.3.2.3 SR1 / §8.4.3.4.3 SR1), "
            + "or its sending arm is silently dropped as a receiver (kb/Work PB1142).");
    }

    /// <summary>Every receiving rule a mixed rule references is one the classifier's positive definition names —
    /// otherwise a receiver written through a NEW receiving rule would be reported as a sending operand and legal
    /// source rejected.</summary>
    [Fact]
    public void EveryReceivingRuleOfAMixedRule_IsOneTheClassifierRecognizes()
    {
        var rules = RuleReferences();
        var known = ArithmeticOperandRole.ReceiverRules.Select(RuleName).ToHashSet(StringComparer.Ordinal);
        foreach (var mixed in ArithmeticOperandRole.MixedRules.Select(RuleName))
        {
            Assert.True(rules.TryGetValue(mixed, out var refs), $"mixed rule '{mixed}' is not in the grammar");
            var unknown = refs!.Where(IsReceivingRule).Where(r => !known.Contains(r)).ToList();
            Assert.True(unknown.Count == 0,
                $"'{mixed}' references receiving rule(s) [{string.Join(", ", unknown)}] that "
                + "ArithmeticOperandRole.IsReceiver does not recognize — add the arm and the ReceiverRules entry.");
        }
        foreach (var r in known)
            Assert.True(rules.ContainsKey(r), $"ReceiverRules names '{r}', which is not a grammar rule");
    }

    /// <summary>The partition on REAL parse trees, for all four verbs and every operand arm the mixed rules
    /// write: a data reference is a receiver; a literal, a function-identifier and an inline method invocation
    /// are not — and a MULTIPLY with a receiver BEFORE the bad operand still finds it (the silent-drop shape).
    /// </summary>
    [Theory]
    [InlineData("ADD A TO B.", null)]
    [InlineData("ADD A TO B C ROUNDED.", null)]
    [InlineData("ADD A TO 3.", typeof(CobolParserCore.LiteralContext))]
    [InlineData("ADD A TO FUNCTION SQRT(4).", typeof(CobolParserCore.FunctionCallContext))]
    [InlineData("ADD A TO O :: \"M\".", typeof(CobolParserCore.InlineMethodInvocationContext))]
    [InlineData("SUBTRACT A FROM B.", null)]
    [InlineData("SUBTRACT A FROM 3.", typeof(CobolParserCore.LiteralContext))]
    [InlineData("SUBTRACT A FROM FUNCTION SQRT(4).", typeof(CobolParserCore.FunctionCallContext))]
    [InlineData("SUBTRACT A FROM O :: \"M\".", typeof(CobolParserCore.InlineMethodInvocationContext))]
    [InlineData("MULTIPLY A BY B.", null)]
    [InlineData("MULTIPLY A BY 3.", typeof(CobolParserCore.ReceivingOperandContext))]
    [InlineData("MULTIPLY A BY FUNCTION SQRT(4).", typeof(CobolParserCore.FunctionCallContext))]
    [InlineData("MULTIPLY A BY O :: \"M\".", typeof(CobolParserCore.InlineMethodInvocationContext))]
    [InlineData("MULTIPLY A BY B O :: \"M\".", typeof(CobolParserCore.InlineMethodInvocationContext))]
    [InlineData("DIVIDE A INTO B.", null)]
    [InlineData("DIVIDE A INTO 3.", typeof(CobolParserCore.LiteralContext))]
    [InlineData("DIVIDE A INTO FUNCTION SQRT(4).", typeof(CobolParserCore.FunctionCallContext))]
    [InlineData("DIVIDE A INTO O :: \"M\".", typeof(CobolParserCore.InlineMethodInvocationContext))]
    public void TheClassifier_PartitionsEveryOperandArm(string statement, Type? expectedNonReceiver)
    {
        var mixedNodes = MixedNodes(statement);
        Assert.NotEmpty(mixedNodes);   // the witness: the statement reached a mixed rule at all
        var found = mixedNodes.Select(ArithmeticOperandRole.FirstNonReceiver).FirstOrDefault(n => n is not null);
        if (expectedNonReceiver is null)
            Assert.Null(found);
        else
        {
            Assert.NotNull(found);
            Assert.IsType(expectedNonReceiver, found);
            Assert.False(ArithmeticOperandRole.IsReceiver(found!));
        }
    }

    /// <summary>Parse <paramref name="statement"/> in a minimal 2023 program and return its mixed-rule nodes.
    /// Nothing is bound: the assertion is about the parse tree and the classifier only.</summary>
    private static List<ParserRuleContext> MixedNodes(string statement)
    {
        string src =
            "IDENTIFICATION DIVISION.\n" +
            "PROGRAM-ID. AORDRIFT.\n" +
            "DATA DIVISION.\n" +
            "WORKING-STORAGE SECTION.\n" +
            "01 O USAGE OBJECT REFERENCE.\n" +
            "01 A PIC 9(4).\n01 B PIC 9(4).\n01 C PIC 9(4).\n" +
            "PROCEDURE DIVISION.\n" +
            "MAIN.\n    " + statement + "\n    STOP RUN.\n";
        string path = Path.Combine(Path.GetTempPath(), "cn_aor_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.NotNull(tree);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics.Select(d => d.ToString())));
            var found = new List<ParserRuleContext>();
            Collect(tree!, found);
            return found;
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    private static void Collect(IParseTree node, List<ParserRuleContext> into)
    {
        if (node is ParserRuleContext prc && ArithmeticOperandRole.MixedRules.Contains(prc.GetType())) into.Add(prc);
        for (int i = 0; i < node.ChildCount; i++) Collect(node.GetChild(i), into);
    }
}
