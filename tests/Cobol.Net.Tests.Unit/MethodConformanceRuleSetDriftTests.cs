// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.IO;
using System.Linq;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §9.3.8.2.3's per-method conformance rules are written ONCE — <c>OoConformance.MethodConformanceMismatches</c>
/// (plus its rule-9 helper <c>RaisingMismatches</c>) — and every asker CALLS it (kb/Work PB972). The §11.7.3 SR9
/// override pass <c>ValidateOverrideSignatures</c> was a second hand-written copy of rules 1–6 and 8 with its own
/// wording, and rule 9 (RAISING) was missing from BOTH copies. This pins the shape: the askers' bodies call the one
/// rule set and contain no per-rule comparison or rule citation of their own.
/// </summary>
public sealed class MethodConformanceRuleSetDriftTests
{
    private static string Body(string text, string signature)
    {
        int start = text.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{signature}' not found in OoConformance.cs — update this drift test with the rename");
        int end = text.IndexOf("    /// <summary>", start, StringComparison.Ordinal);
        return text[start..(end < 0 ? text.Length : end)];
    }

    [Theory]
    [InlineData("public static void ValidateOverrideSignatures(")]
    [InlineData("public static IReadOnlyList<AdapterPair> ValidateImplements(")]
    [InlineData("public static bool InterfaceConformsTo(")]
    public void EveryAsker_CallsTheOneRuleSet_AndSpellsNoRuleOfItsOwn(string signature)
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Oo", "OoConformance.cs"));
        string body = Body(text, signature);
        Assert.Contains("MethodConformanceMismatches(", body);
        foreach (string own in new[] { "DescriptionMismatch(", "OptionalMismatch(", "ObjectRefAssignmentMismatch(",
                                       ".Raising", "§9.3.8.2.3 rule", "§9.3.8.2)" })
            Assert.False(body.Contains(own, StringComparison.Ordinal),
                $"{signature.Trim()} spells '{own}' itself — a second copy of a §9.3.8.2.3 rule; ask "
                + "MethodConformanceMismatches instead");
    }

    [Fact]
    public void TheRuleSet_CarriesRule9()
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Oo", "OoConformance.cs"));
        string body = Body(text, "internal static IEnumerable<string> MethodConformanceMismatches(");
        Assert.Contains("RaisingMismatches(", body);
        string rule9 = Body(text, "internal static IEnumerable<string> RaisingMismatches(");
        foreach (string arm in new[] { "RaisingTargetKind.ExceptionName", "RaisingTargetKind.ObjectClass", "rule 9 {arm}" })
            Assert.Contains(arm, rule9);
    }

    /// <summary>§9.3.8.2.3's closing sentence — the mutual-reference ban between the two RETURNING descriptions (kb/Work
    /// PB1498, owner decision R63) — is one of the rule set's rules, so every asker applies it, and every asker names
    /// interface-1 and interface-2 for it.</summary>
    [Fact]
    public void TheRuleSet_CarriesTheReturningCircularitySentence()
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Oo", "OoConformance.cs"));
        string body = Body(text, "internal static IEnumerable<string> MethodConformanceMismatches(");
        Assert.Contains("ReturningCircularity(", body);
        Assert.Contains("string interface1, string interface2", body);
    }
}
