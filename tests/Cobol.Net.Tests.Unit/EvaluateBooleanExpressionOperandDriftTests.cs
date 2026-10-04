// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN EVALUATE SELECTION OPERAND THAT CARRIES A BOOLEAN OPERATOR IS TABLE 15's BOOLEAN-EXPRESSION KIND, NOT A
/// CONDITION (kb/Work PB1412). ISO §14.9.13.2 prints <c>boolean-expression-1</c> as a selection subject and
/// <c>[ NOT ] boolean-expression-2</c> as a selection object, and §14.9.13.3 SR10's Table 15 has a Boolean-expression
/// column and row. The grammar's <c>evaluateSubject</c> and <c>evaluateWhenItem</c> had no such alternative, so
/// <c>EVALUATE A B-AND C WHEN B"1000"</c> parsed as a <c>condition</c> and was refused (COBOLNET1511 + COBOLNET1634) on
/// legal source.
/// <para>This pins the three facts that keep the shape right, none of which a golden can see: (1) the alternative
/// exists in BOTH rules, behind the same <c>{boolExprAhead()}?</c> discriminator every other boolean-expression host
/// uses, and sits AFTER <c>valueOperand</c> and BEFORE <c>condition</c> — before it, an operator-free subject would
/// stop parsing as the value operand it always was; after it, the alternative would never be reached; (2) the parse
/// tree actually lands in it (an operator-bearing operand), and stays out of it where it must (a relation tail, a bare
/// operand beside a later operator-bearing one — the scan stops at ALSO); (3) EVALUATE's binder READS every
/// alternative of both rules, derived from the grammar, so the next alternative added without a Table 15 row for it
/// fails here instead of binding as the nearest neighbour.</para>
/// </summary>
public sealed class EvaluateBooleanExpressionOperandDriftTests
{
    private static string Grammar() => File.ReadAllText(Path.Combine(TestRepo.Root,
        "src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolControlFlow.g4"));

    private static string Binder() => File.ReadAllText(Path.Combine(TestRepo.Root,
        "src", "Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "EvaluateBinder.cs"));

    /// <summary>The rule's alternatives, comments stripped, as written.</summary>
    private static string[] Alternatives(string rule)
    {
        var m = Regex.Match(Grammar(), $@"^{rule}\s*\r?\n\s*:(?<body>.*?);", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, $"grammar rule '{rule}' not found — if it was renamed this guard must move with it");
        return Regex.Replace(m.Groups["body"].Value, @"//[^\r\n]*", "")
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim()).Where(a => a.Length > 0).ToArray();
    }

    private const string Gate = "{boolExprAhead()}? booleanExpression";

    [Theory]
    [InlineData("evaluateSubject")]
    [InlineData("evaluateWhenItem")]
    public void TheBooleanExpressionAlternative_IsGated_AndSitsBetweenValueOperandAndCondition(string rule)
    {
        var alts = Alternatives(rule).Select(a => Regex.Replace(a, @"\s+", " ")).ToArray();
        int value = Array.IndexOf(alts, "valueOperand"), boolean = Array.IndexOf(alts, Gate),
            condition = Array.IndexOf(alts, "condition");
        Assert.True(value >= 0 && boolean >= 0 && condition >= 0,
            $"{rule} must carry valueOperand, `{Gate}` and condition (ISO §14.9.13.2 prints boolean-expression-1/-2 "
            + $"beside condition-1/-2); got [{string.Join(" | ", alts)}]");
        Assert.True(value < boolean && boolean < condition,
            $"{rule}: the booleanExpression alternative must follow valueOperand (an operator-free operand keeps its "
            + "parse) and precede condition (an operator-bearing operand with no relational tail is Table 15's "
            + $"Boolean-expression kind, not a condition). Order was [{string.Join(" | ", alts)}]");
    }

    /// <summary>Every alternative of both rules is READ by EvaluateBinder's accessor — a rule alternative the binder
    /// never asks about is a parse shape that reaches the nearest neighbour's arm.</summary>
    [Theory]
    [InlineData("evaluateSubject")]
    [InlineData("evaluateWhenItem")]
    public void EveryAlternative_IsReadByTheBinder(string rule)
    {
        string binder = Binder();
        foreach (string alt in Alternatives(rule))
        {
            string name = Regex.Replace(alt, @"\{[^}]*\}\?", "").Trim();
            Assert.Matches(@"^\w+$", name);
            Assert.True(binder.Contains("." + name + "()", StringComparison.Ordinal),
                $"{rule}'s alternative `{name}` is never read as `.{name}()` in EvaluateBinder.cs — add its Table 15 "
                + "classification (SubjectKind / ObjectKind) and its bind arm, or the shape binds as a neighbour.");
        }
    }

    private static CobolParserCore.EvaluateStatementContext ParseEvaluate(string evaluate)
    {
        string src = "IDENTIFICATION DIVISION.\nPROGRAM-ID. EVALBOOL.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n"
            + "01 A PIC 1(4) VALUE B\"1100\".\n01 C PIC 1(4) VALUE B\"1010\".\n01 X PIC 1(4) VALUE B\"1000\".\n"
            + "PROCEDURE DIVISION.\nMAIN.\n" + evaluate + "\n";
        string path = Path.Combine(Path.GetTempPath(), "cn_evalbool_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics.Select(d => d.ToString())));
            Assert.NotNull(tree);
            var ev = Descendants(tree).OfType<CobolParserCore.EvaluateStatementContext>().SingleOrDefault();
            Assert.NotNull(ev);
            return ev;
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    private static IEnumerable<IParseTree> Descendants(IParseTree node)
    {
        yield return node;
        for (int i = 0; i < node.ChildCount; i++)
            foreach (var d in Descendants(node.GetChild(i))) yield return d;
    }

    [Fact]
    public void AnOperatorBearingSubjectAndObject_ParseAsBooleanExpressions()
    {
        var ev = ParseEvaluate("EVALUATE A B-AND C\nWHEN X B-OR C CONTINUE\nWHEN OTHER CONTINUE\nEND-EVALUATE.");
        var subject = Assert.Single(ev.evaluateSubject());
        Assert.NotNull(subject.booleanExpression());
        Assert.Null(subject.condition());
        var item = ev.evaluateWhenClause(0).evaluateWhenPhrase(0).evaluateWhenGroup(0).evaluateWhenItem();
        Assert.NotNull(item.booleanExpression());
        Assert.Null(item.condition());
    }

    [Fact]
    public void ARelationTail_StaysACondition()
    {
        var ev = ParseEvaluate("EVALUATE A B-AND C = X\nWHEN TRUE CONTINUE\nEND-EVALUATE.");
        var subject = Assert.Single(ev.evaluateSubject());
        Assert.NotNull(subject.condition());
        Assert.Null(subject.booleanExpression());
    }

    /// <summary>The scan stops at ALSO: the first subject is a bare operand and keeps its value-operand parse,
    /// the second carries the operator.</summary>
    [Fact]
    public void ABareSubject_BesideALaterOperatorBearingOne_KeepsItsValueOperandParse()
    {
        var ev = ParseEvaluate("EVALUATE X ALSO A B-AND C\nWHEN X ALSO B\"1000\" CONTINUE\nEND-EVALUATE.");
        var subjects = ev.evaluateSubject();
        Assert.Equal(2, subjects.Length);
        Assert.NotNull(subjects[0].valueOperand());
        Assert.Null(subjects[0].booleanExpression());
        Assert.NotNull(subjects[1].booleanExpression());
    }

    [Fact]
    public void ABareBooleanOperand_KeepsItsValueOperandParse()
    {
        var ev = ParseEvaluate("EVALUATE X\nWHEN B\"1000\" CONTINUE\nEND-EVALUATE.");
        Assert.NotNull(Assert.Single(ev.evaluateSubject()).valueOperand());
        Assert.NotNull(ev.evaluateWhenClause(0).evaluateWhenPhrase(0).evaluateWhenGroup(0).evaluateWhenItem().valueOperand());
    }
}
