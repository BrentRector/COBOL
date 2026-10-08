// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE SENDING OPERAND <c>{identifier-n | literal-n}</c> IS WRITTEN ONCE, AS <c>sendingOperand</c> (kb/Work PB2114).
///
/// <para><b>The defect this exists for.</b> Every general format that writes an operand as <c>identifier-n |
/// literal-n</c> in a SENDING role admits the same four grammar shapes: a literal, a data reference, a
/// function-identifier (ISO §8.4.3.1.2 Format 1)
/// and an inline method invocation (§8.4.3.1.2 Format 4). The grammar used to
/// write that set out NINE times — the four arithmetic operands, MOVE's, the WRITE / REWRITE / RELEASE FROM
/// phrases, INITIALIZE's REPLACING item and INSPECT's operand. Each copy was added separately (fix-queue PB45 and PB10 widened each one for the function-identifier, PB428 for Format 4), and
/// each copy had its own binder dispatch over its own generated accessors, so one shape was bound in five places.
/// They are one rule now, bound once (<c>MoveBinder.SendingOperand</c>).</para>
///
/// <para><b>The property, stated exactly:</b> no rule other than <c>sendingOperand</c> offers exactly those four
/// shapes as its sibling alternatives — at its top level or inside any parenthesized group. A position that needs
/// the set names the rule. A position that needs a DIFFERENT set (an identifier only; WRITE ADVANCING's identifier
/// or integer, whose <c>integerLiteral</c> arm decides the format; the arithmetic expression spine) is not a copy.
/// The one recorded exemption is <c>displayStatement</c>: naming the rule there parses every legal DISPLAY
/// identically but moves the error-recovery boundary, so <c>DISPLAY COLUMN.</c> counts more syntax errors on the way
/// to its named §8.9 diagnostic, and a unification that changes a parse is not one (see the grammar's note).</para>
///
/// <para>⚠ It reads the GRAMMAR SOURCE, not the generated parser: a re-inlined copy accepts exactly the same input,
/// so only the <c>.g4</c> can show it.</para>
/// </summary>
public sealed class SendingOperandDriftTests
{
    private const string SendingRule = "sendingOperand";

    private static readonly string[] SendingShapes = ["literal", "functionCall", "inlineMethodInvocation", "dataReference"];

    /// <summary>Rules that still write the set out, each for the reason the class summary records.</summary>
    private static readonly string[] Exempt = ["displayStatement"];

    private static readonly string GrammarDir = Path.Combine("src", "Cobol.Net.Frontend", "Grammar");

    /// <summary>rule-name → body, over every grammar file, comments stripped.</summary>
    private static Dictionary<string, string> GrammarRules()
    {
        var rules = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(Path.Combine(TestRepo.Root, GrammarDir), "*.g4",
                     SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            foreach (var (name, body) in Rules(File.ReadAllText(path)))
                rules.TryAdd(name, body);
        return rules;
    }

    private static IEnumerable<(string Name, string Body)> Rules(string g4)
    {
        g4 = Regex.Replace(g4, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        g4 = Regex.Replace(g4, @"//[^\n]*", " ");
        foreach (Match m in Regex.Matches(g4, @"^(?<name>[a-z][A-Za-z0-9_]*)\s*\r?\n?\s*:(?<body>.*?);",
                     RegexOptions.Multiline | RegexOptions.Singleline))
            yield return (m.Groups["name"].Value, m.Groups["body"].Value);
    }

    /// <summary>The alternative lists of <paramref name="body"/>: its top level and every parenthesized group, each
    /// split on <c>|</c> at its own depth, semantic predicates (<c>{…}?</c>) removed.</summary>
    private static IEnumerable<List<string>> AlternativeLists(string body)
    {
        body = Regex.Replace(body, @"\{[^{}]*\}\??", " ");
        var stack = new Stack<List<System.Text.StringBuilder>>();
        stack.Push([new()]);
        foreach (char c in body)
        {
            switch (c)
            {
                case '(':
                    stack.Peek()[^1].Append(' ');
                    stack.Push([new()]);
                    break;
                case ')' when stack.Count > 1:
                    yield return Close(stack.Pop());
                    break;
                case '|':
                    stack.Peek().Add(new());
                    break;
                default:
                    stack.Peek()[^1].Append(c);
                    break;
            }
        }
        while (stack.Count > 0) yield return Close(stack.Pop());

        static List<string> Close(List<System.Text.StringBuilder> alts) =>
            alts.Select(a => Regex.Replace(a.ToString(), @"\s+", " ").Trim()).ToList();
    }

    /// <summary>The rules (other than <see cref="SendingRule"/>) holding an alternative list whose alternatives are
    /// exactly the four sending shapes — a copy of the rule.</summary>
    private static List<string> Copies(IEnumerable<(string Name, string Body)> rules) =>
        rules.Where(r => r.Name != SendingRule
                         && AlternativeLists(r.Body).Any(alts => alts.ToHashSet().SetEquals(SendingShapes)))
             .Select(r => r.Name)
             .Order(StringComparer.Ordinal)
             .ToList();

    [Fact]
    public void TheSendingOperandRule_OffersExactlyTheFourShapes()
    {
        var rules = GrammarRules();
        Assert.True(rules.TryGetValue(SendingRule, out string? body), $"the {SendingRule} rule is gone");
        var alts = AlternativeLists(body!).First();
        Assert.Equal(SendingShapes.Order(StringComparer.Ordinal), alts.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void NoOtherRule_WritesTheSendingOperandOut()
    {
        var rules = GrammarRules();
        var found = Copies(rules.Select(kv => (kv.Key, kv.Value)));
        // An exemption that no longer names a copy exempts nothing, and would hide the next one under its name.
        foreach (string exempt in Exempt)
            Assert.True(found.Contains(exempt), $"Exempt names '{exempt}', which no longer writes the set out: drop it");
        var copies = found.Except(Exempt).ToList();
        Assert.True(copies.Count == 0,
            $"these grammar rules write the sending operand `{{identifier | literal}}` out instead of naming "
            + $"`{SendingRule}`: {string.Join(", ", copies)}. Use the rule, and bind its node through "
            + "MoveBinder.SendingOperand (kb/Work PB2114).");
        // The witness: the scan reached the positions that name the rule, so a zero above is a measurement.
        int users = rules.Count(kv => Regex.IsMatch(kv.Value, $@"\b{SendingRule}\b"));
        Assert.True(users >= 12,
            $"only {users} grammar rules name {SendingRule} — the scan lost sites, so the zero above proves nothing "
            + "(feedback_verdict_evidence_invariant)");
    }

    /// <summary>The detector, on the shapes it must catch: an inline group in any order, a top-level list, and a
    /// guarded alternative — and not on a set that lacks one of the four (an identifier-only position).</summary>
    [Theory]
    [InlineData("recordFromPhrase\n    : FROM (functionCall | inlineMethodInvocation | dataReference | literal)\n    ;", true)]
    [InlineData("x\n    : literal\n    | functionCall\n    | inlineMethodInvocation\n    | dataReference\n    ;", true)]
    [InlineData("x\n    : DISPLAY ({!p()}? (inlineMethodInvocation | dataReference | literal | functionCall))*\n    ;", true)]
    [InlineData("x\n    : INSPECT (functionCall | inlineMethodInvocation | dataReference) TALLYING\n    ;", false)]
    [InlineData("x\n    : ADVANCING (functionCall | inlineMethodInvocation | dataReference | integerLiteral | literal)\n    ;", false)]
    [InlineData("x\n    : FROM sendingOperand\n    ;", false)]
    public void TheDetector_FindsAPlantedCopy(string grammar, bool isCopy) =>
        Assert.Equal(isCopy, Copies(Rules(grammar)).Count == 1);

    /// <summary>The receiving side admits neither identifier format — ISO §8.4.3.2.3 SR1, "A function-identifier
    /// shall not be specified as a receiving operand", and §8.4.3.4.3 SR1, "Inline method invocation shall not be
    /// specified as a receiving operand".</summary>
    [Fact]
    public void ReceivingOperandRules_AdmitNoComputedIdentifier()
    {
        var rules = GrammarRules();
        foreach (string rule in new[] { "receivingArithmeticOperand", "receivingOperand" })
        {
            Assert.True(rules.TryGetValue(rule, out string? body), $"the receiving rule '{rule}' is gone");
            Assert.DoesNotMatch(@"\bfunctionCall\b", body!);
            Assert.DoesNotMatch(@"\binlineMethodInvocation\b", body!);
        }
    }
}
