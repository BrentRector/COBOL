// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE CLASS CONDITION'S ALTERNATIVES ARE ONE LIST, AND THIS TEST IS WHY "ONE" STAYS TRUE.
/// ISO §8.8.4.4.2 prints fourteen alternatives in a single brace group. Before kb/Work PB571 + PB590 they were
/// written down FOUR times — the <c>classConditionName</c> grammar rule, a second grammar rule <c>classCondition</c>
/// serving <c>evaluateSubject</c> (which offered ALPHANUMERIC, not one of the fourteen, and omitted BOOLEAN,
/// class-name-1 and alphabet-name-1), and a kind decode in each of two binders — and the lists DISAGREED, so
/// the same class test meant different things in an IF and as an EVALUATE selection subject.
/// <para>The alternatives now live in <c>classConditionName</c>; their §8.8.4.4.3 operand rules live in
/// <c>ClassConditionModel</c>; their truth values live in <c>ConditionRenderer.RenderClass</c>. Adding an
/// alternative to any ONE of the three and not the others is exactly the drift that produced a
/// <c>NotImplementedCobolFeatureException</c> at run time for a class condition the grammar accepted, so the
/// three are asserted to agree here rather than remembered.</para>
/// </summary>
public sealed class ClassConditionTableDriftTests
{
    /// <summary>Grammar rules that enumerate class words and are NOT the class condition's list, each with the
    /// reason. Adding a name here is an adjudication — it must be a different CONSTRUCT with its own general
    /// format in the standard, not a convenient second copy of §8.8.4.4.2's.</summary>
    private static readonly Dictionary<string, string> Exempt = new(StringComparer.Ordinal)
    {
        ["validateClassOperand"] =
            "ISO §13.18.11.2's CLASS clause of a DATA DESCRIPTION entry (the VALIDATE band) — a different "
            + "construct with its own printed brace group (PDF p412 / folio 382), which also offers "
            + "alphabet-name-1 and class-name-1 but is a data-description clause, not a condition. The clause "
            + "is DECLINED as a whole (Core/CobolDeclined.g4), so its operand arms are never bound.",
    };

    /// <summary>The <c>classConditionName</c> rule body, comments stripped.</summary>
    private static string ClassNameRuleBody()
    {
        string path = TestRepo.Src(Path.Combine(
            "Cobol.Net.Frontend", "Grammar", "Core", "CobolExpressions.g4"));
        string text = Regex.Replace(File.ReadAllText(path), @"//[^\r\n]*", "");
        foreach (string chunk in text.Split(';'))
        {
            var m = Regex.Match(chunk, @"\A\s*classConditionName\s*:(?<body>.*)\z", RegexOptions.Singleline);
            if (m.Success) return m.Groups["body"].Value;
        }
        throw new Xunit.Sdk.XunitException(
            "the classConditionName rule was not found in Core/CobolExpressions.g4 — the scan is broken, not the grammar");
    }

    /// <summary>The KEYWORD alternatives of <c>classConditionName</c> (an all-caps token reference on its own
    /// alternative), which are §8.8.4.4.2's underlined words. <c>cobolWord</c> is the two user-defined-word
    /// alternatives, alphabet-name-1 and class-name-1, told apart at bind.</summary>
    private static List<string> KeywordAlternatives() =>
        [.. ClassNameRuleBody()
            .Split('|')
            .Select(a => a.Trim())
            .Where(a => Regex.IsMatch(a, @"\A[A-Z][A-Z0-9_]*\z"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(a => a, StringComparer.Ordinal)];

    /// <summary>Every keyword alternative the grammar offers has a row in the operand-rule table, reached by
    /// the accessor the binder calls (<c>cls.&lt;TOKEN&gt;()</c>) and tagged with a kind constant.</summary>
    [Fact]
    public void EveryGrammarAlternative_HasABinderArmAndAModelRow()
    {
        var keywords = KeywordAlternatives();
        // The scan must FIND the alternatives — an empty result would make every assertion below vacuous
        // (feedback_verdict_evidence_invariant: a run must assert its population).
        Assert.True(keywords.Count >= 5,
            $"the grammar scan found only {keywords.Count} keyword alternative(s) of classConditionName "
            + $"({string.Join(", ", keywords)}) — the scan itself is broken, not the grammar");

        string binder = File.ReadAllText(TestRepo.Src(Path.Combine(
            "Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "ConditionBinder.cs")));
        var missing = keywords.Where(k => !binder.Contains($"cls.{k}() is not null", StringComparison.Ordinal)).ToList();
        Assert.True(missing.Count == 0,
            $"classConditionName offers keyword alternative(s) the class-condition binder never decodes: "
            + $"{string.Join(", ", missing)}.{Environment.NewLine}The parser accepts them and "
            + $"BindClassConditionOn falls through, which is the silent-compile / loud-runtime staging PB590 "
            + $"closed. Add the arm in ConditionBinder.BindClassConditionOn, a row in ClassConditionModel with "
            + $"the §8.8.4.4.3 rules that name it, and an arm in ConditionRenderer.RenderClass.");
    }

    /// <summary>Every kind the model declares is rendered: <c>RenderClass</c>'s switch has an arm for it, so no
    /// alternative reaches the loud run-time default the renderer keeps for a recovery node.</summary>
    [Fact]
    public void EveryModelKind_HasARendererArm()
    {
        string model = File.ReadAllText(TestRepo.Src(Path.Combine(
            "Cobol.Net.Compiler", "Binding", "ClassConditionModel.cs")));
        // The kinds carried on a BoundClassCondition (the renderer's switch subject). The class-name and
        // alphabet-name alternatives have their own bound nodes and their own Visit methods, so they are not
        // RenderClass's business — they are named here so the exclusion is stated, not silent.
        var rendered = Regex.Matches(model, @"public const char (?<name>\w+) = '(?<ch>.)';")
            .Select(m => (Name: m.Groups["name"].Value, Ch: m.Groups["ch"].Value))
            .Where(k => k.Name is not ("ClassName" or "AlphabetName"))
            .ToList();
        Assert.True(rendered.Count >= 5,
            $"the model scan found only {rendered.Count} renderable kind constant(s) — the scan is broken");

        string renderer = File.ReadAllText(TestRepo.Src(Path.Combine(
            "Cobol.Net.Compiler", "CodeGen", "Emit", "ConditionRenderer.cs")));
        int switchAt = renderer.IndexOf("string test = c.ClassKind switch", StringComparison.Ordinal);
        Assert.True(switchAt >= 0, "RenderClass's ClassKind switch was not found — the scan is broken");
        string body = renderer[switchAt..renderer.IndexOf("};", switchAt, StringComparison.Ordinal)];

        // An arm names its kind either as the character literal or as the model's own constant (the seven
        // COBOL-2014 arms do the latter, some of them two kinds to one arm with `or` — kb/Work PB225).
        var unrendered = rendered.Where(k => !body.Contains($"'{k.Ch}' =>", StringComparison.Ordinal)
            && !Regex.IsMatch(body, $@"ClassConditionModel\.{k.Name}\b(\s+or\s+ClassConditionModel\.\w+)*\s*=>")
            && !Regex.IsMatch(body, $@"ClassConditionModel\.\w+\s+or\s+ClassConditionModel\.{k.Name}\s*=>")).ToList();
        Assert.True(unrendered.Count == 0,
            $"ClassConditionModel declares kind(s) RenderClass has no arm for: "
            + $"{string.Join(", ", unrendered.Select(k => $"{k.Name} '{k.Ch}'"))}."
            + $"{Environment.NewLine}The default arm is EmitText.LoudValue — a class condition the binder "
            + $"accepts would compile and then abort at run time (kb/Work PB590).");
    }

    /// <summary>⛔ THE SECOND LIST STAYS DELETED. <c>classCondition</c> was a private copy of these alternatives
    /// that served <c>evaluateSubject</c> alone; reintroducing any rule that enumerates NUMERIC/ALPHABETIC
    /// beside <c>classConditionName</c> is the defect, not a convenience.</summary>
    [Fact]
    public void NoSecondAlternativeList_ExistsInTheGrammar()
    {
        string grammarDir = TestRepo.Src(Path.Combine("Cobol.Net.Frontend", "Grammar"));
        var offenders = new List<string>();
        var stillPresent = new List<string>();
        foreach (string file in Directory.EnumerateFiles(grammarDir, "*.g4", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.Combine("obj", ""), StringComparison.Ordinal)) continue;
            string text = Regex.Replace(File.ReadAllText(file), @"//[^\r\n]*", "");
            foreach (string chunk in text.Split(';'))
            {
                var m = Regex.Match(chunk, @"\A\s*(?<name>[a-z]\w*)\s*:(?<body>.*)\z", RegexOptions.Singleline);
                if (!m.Success || m.Groups["name"].Value == "classConditionName") continue;
                string body = m.Groups["body"].Value;
                // A rule that alternates NUMERIC with an ALPHABETIC form is a class-condition list, whatever it
                // is called. The PICTURE/REPLACING category words (CobolData.g4) name ALPHANUMERIC-EDITED and
                // NUMERIC-EDITED too, which §8.8.4.4.2 never offers, so they are not this shape.
                if (!Regex.IsMatch(body, @"\bNUMERIC\b") || !Regex.IsMatch(body, @"\bALPHABETIC(_LOWER|_UPPER)?\b")
                    || Regex.IsMatch(body, @"\b(NUMERIC_EDITED|ALPHANUMERIC_EDITED)\b")) continue;
                string name = m.Groups["name"].Value;
                if (Exempt.ContainsKey(name)) stillPresent.Add(name);
                else offenders.Add($"{Path.GetFileName(file)}:{name}");
            }
        }
        // The exemptions stay honest: a name that no longer has this shape is stale and would silently excuse a
        // future rule that reuses it (the BooleanExpressionGateSiteDriftTests discipline).
        var stale = Exempt.Keys.Where(k => !stillPresent.Contains(k, StringComparer.Ordinal)).ToList();
        Assert.True(stale.Count == 0,
            $"stale exemption(s) in this test: {string.Join(", ", stale)} — the grammar rule is gone or no "
            + $"longer enumerates class words. Delete the entry.");
        Assert.True(offenders.Count == 0,
            $"a SECOND class-condition alternative list appeared in the grammar: {string.Join(", ", offenders)}."
            + $"{Environment.NewLine}ISO §8.8.4.4.2 prints ONE list; the deleted `classCondition` rule is why "
            + $"`EVALUATE X IS <user-class>` did not parse while `IF X IS <user-class>` did (kb/Work PB590). "
            + $"Point the new site at `classConditionName` instead.");
    }
}
