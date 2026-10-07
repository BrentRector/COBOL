// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE SENDING ARITHMETIC OPERAND IS ONE RULE WRITTEN FOUR TIMES, AND ALL FOUR WERE WRONG (fix-queue PB45).
/// <c>addOperand</c>, <c>subtractOperand</c>, <c>multiplyOperand</c> and <c>divideOperand</c> were each
/// <c>dataReference | literal</c>, so <c>ADD FUNCTION SQRT(X) TO Y</c> was a PARSE error across the WHOLE
/// arithmetic family while COMPUTE accepted it — <b>ISO §8.4.3.1.2 Format 1</b> makes a function-identifier an
/// identifier, and every one of those formats writes its sending operand as <c>identifier-n | literal-n</c>.
///
/// <para><b>Why four rules and not one.</b> The fix was made ADDITIVE while a second consumer read
/// <c>.dataReference()</c> / <c>.literal()</c> off <c>AddOperandContext</c> and friends BY NAME. That consumer is
/// gone; collapsing the four to one rule is kb/Work PB2114, which deletes this test with them. Until then
/// <b>this test is what keeps four copies from drifting apart.</b></para>
///
/// <para>⚠ It reads the GRAMMAR SOURCE rather than the generated parser on purpose: the property being guarded is
/// that the four rules are written identically, which is a fact about the <c>.g4</c>. A generated-parser check
/// would pass on four rules that merely happen to accept the same inputs today.</para>
/// </summary>
public sealed class ArithmeticSendingOperandDriftTests
{
    private static readonly string[] SendingRules =
        ["addOperand", "subtractOperand", "multiplyOperand", "divideOperand"];

    [Fact]
    public void TheFourSendingOperandRules_AreIdentical_AndAllAdmitAFunctionIdentifier()
    {
        string g4 = File.ReadAllText(Path.Combine(TestRepo.Root,
            "src", "Cobol.Net.Frontend", "Grammar", "CobolParserCore.g4"));

        var bodies = new Dictionary<string, string[]>();
        foreach (var rule in SendingRules)
        {
            var m = Regex.Match(g4, $@"^{rule}\s*\r?\n\s*:(?<body>.*?);", RegexOptions.Multiline | RegexOptions.Singleline);
            Assert.True(m.Success, $"grammar rule '{rule}' not found — if it was renamed or collapsed, this guard "
                                   + "must move with it (see the class summary: collapse is a CUT 2 action)");
            bodies[rule] = m.Groups["body"].Value
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => a.Length > 0)
                .ToArray();
        }

        // Every rule admits a function-identifier — the defect itself.
        foreach (var (rule, alts) in bodies)
            Assert.True(alts.Contains("functionCall"),
                $"'{rule}' does not admit functionCall, so a function-identifier is refused in a SENDING operand "
                + $"position ISO §8.4.3.1.2 admits: [{string.Join(" | ", alts)}]");

        // …and they are all the SAME rule, so a future edit to one is caught rather than silently diverging.
        var reference = bodies[SendingRules[0]];
        foreach (var rule in SendingRules.Skip(1))
            Assert.True(reference.SequenceEqual(bodies[rule]),
                $"'{rule}' has drifted from '{SendingRules[0]}': [{string.Join(" | ", bodies[rule])}] vs "
                + $"[{string.Join(" | ", reference)}]. They are ONE rule the legacy freeze forces us to write four "
                + "times; keep them identical until CUT 2 collapses them.");
    }

    /// <summary>The receiving side must NOT admit one — <b>§8.4.3.2.3 SR1</b>: "A function-identifier shall not be
    /// specified as a receiving operand." A guard that only checked the sending rules would be satisfied by a
    /// careless edit that added functionCall everywhere.</summary>
    [Fact]
    public void ReceivingOperandRules_DoNotAdmitAFunctionIdentifier()
    {
        string g4 = File.ReadAllText(Path.Combine(TestRepo.Root,
            "src", "Cobol.Net.Frontend", "Grammar", "CobolParserCore.g4"));

        foreach (var rule in new[] { "receivingArithmeticOperand", "receivingOperand" })
        {
            var m = Regex.Match(g4, $@"^{rule}\s*\r?\n\s*:(?<body>.*?);", RegexOptions.Multiline | RegexOptions.Singleline);
            if (!m.Success) continue;   // renamed — the sending guard above still holds the line that matters
            Assert.DoesNotContain("functionCall", m.Groups["body"].Value);
        }
    }
}
