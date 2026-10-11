// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ SIX §15 CLAUSES SAY "argument-N shall not be a zero-length literal" AND EXACTLY ONE WAS ENFORCED
/// (fix-queue PB35). The one that was — NATIONAL-OF's — is hand-written inside <c>CheckRepertoireArgs</c>, so
/// nothing about it generalized: <c>FUNCTION MAX("" "AB")</c>, <c>MIN</c>, <c>ORD-MAX</c> and <c>ORD-MIN</c> all
/// compiled clean at every edition, and the queue entry named only MAX/MIN.
/// <para>The rule now rides the argument SCHEMA (<see cref="ArgRule.NoZeroLengthClause"/>), which is where the
/// per-position class rules already live — so enforcement is one shared line rather than six copies. This test
/// is the half that keeps it true: it DERIVES the list of functions carrying the rule from
/// <c>specs/ISO_COBOL.md</c> and fails when one is neither declared nor explicitly adjudicated. A seventh clause
/// then costs a table row instead of going unnoticed for as long as these five did.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ZeroLengthLiteralArgumentDriftTests
{
    /// <summary>Functions whose clause carries the rule but whose schema deliberately does not, each with the
    /// reason. An entry here is an adjudication, not a formality.</summary>
    private static readonly Dictionary<string, string> Adjudicated = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NATIONAL-OF"] =
            "enforced in IntrinsicBinder.CheckRepertoireArgs, where its NARROWER class rule already lives: "
            + "§15.66.3 r1 admits argument-1 of class alphabetic or alphanumeric but NOT national, which no "
            + "single ArgKind code expresses ('s' spans national). Moving only the zero-length half into the "
            + "schema would split ONE function's argument rules across two mechanisms to unify one rule across "
            + "six — a worse trade. Verified enforced by SpecZeroLengthArguments below.",
        // ⛔ STANDARD-COMPARE WAS ADJUDICATED OUT HERE AND IS NOT ANY MORE (kb/Work PB101 T7). Its reason read
        // "documented non-support, Bind = Unsupported, so COBOLNET1518 fires before any argument screen and the
        // row would be a dead lookup" — TRUE while support was declined under A.3 item 25, and FALSE the moment
        // it was claimed. An adjudication is a fact about the compiler, not a permanent exemption: leaving it
        // would have kept this guard green while `FUNCTION STANDARD-COMPARE("" "AB")` — which §15.85.3 r4
        // forbids — compiled clean. The schema now declares the rule on positions 1 and 2.
    };

    /// <summary>Every §15 function whose ARGUMENT rules prohibit a zero-length literal, read off the spec.</summary>
    /// <remarks>
    /// ⚠ Scoped to the ARGUMENT-RULES subclause on purpose. §15.93/§15.94/§15.95 (the TEST-NUMVAL family) mention
    /// "argument-1 is zero-length" in their RETURNED-VALUE rules as one of the errors the function REPORTS — a
    /// value the function computes, the opposite of a prohibition. Matching the phrase anywhere in clause 15
    /// would have swept those three in and demanded a screen that rejects source the standard explicitly defines
    /// behaviour for.
    /// </remarks>
    public static IEnumerable<string> SpecFunctionsProhibitingZeroLengthArguments()
    {
        string spec = File.ReadAllText(TestRepo.Specs("ISO_COBOL.md"));
        // Each `### 15.N NAME function` heading and the text up to the next one.
        var sections = Regex.Matches(spec, @"^### 15\.\d+ (?<name>[A-Z0-9-]+) function\s*$", RegexOptions.Multiline)
            .ToList();
        for (int i = 0; i < sections.Count; i++)
        {
            int start = sections[i].Index;
            int end = i + 1 < sections.Count ? sections[i + 1].Index : spec.Length;
            string body = spec[start..end];
            // ONLY the "#### 15.N.3 Argument rules" subclause of this function.
            var argRules = Regex.Match(body, @"####\s+15\.[\d.]+\s+Argument rules(?<r>.*?)(?=\n####|\z)",
                RegexOptions.Singleline);
            if (!argRules.Success) continue;
            // ⛔ MATCH THE PHRASE, NOT ONE OF ITS SENTENCE SHAPES. The first cut of this scan looked for
            // "shall not be a zero-length literal" and found FIVE of the six, because §15.85.3 r4 states the
            // identical rule as "NEITHER argument-1 NOR argument-2 shall be a zero-length literal". A drift test
            // whose own pattern has a blind spot is worse than none — it reports coverage it does not have.
            // Inside an ARGUMENT-RULES subclause every mention of the term is a prohibition; the returned-value
            // mentions (the TEST-NUMVAL family) are excluded by the subclause scoping above, not by wording.
            if (Regex.IsMatch(argRules.Groups["r"].Value, @"zero-length literal", RegexOptions.IgnoreCase))
                yield return sections[i].Groups["name"].Value;
        }
    }

    /// <summary>The scan finds the clauses at all — an empty or tiny result would make the assertion vacuous
    /// (feedback_verdict_evidence_invariant: a run must assert its population).</summary>
    [Fact]
    public void TheSpecScan_FindsTheKnownClauses()
    {
        var found = SpecFunctionsProhibitingZeroLengthArguments().ToList();
        Assert.True(found.Count >= 6,
            $"the spec scan found only {found.Count} function(s) prohibiting a zero-length literal argument "
            + $"({string.Join(", ", found)}) — the scan is broken, not the table");
        foreach (string expected in new[] { "MAX", "MIN", "NATIONAL-OF", "ORD-MAX", "ORD-MIN", "STANDARD-COMPARE" })
            Assert.Contains(expected, found, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>⛔ THE RETURNED-VALUE MENTIONS ARE NOT PROHIBITIONS. TEST-NUMVAL and its two siblings describe
    /// "argument-1 is zero-length" as an error their RESULT reports, so a screen rejecting it would refuse source
    /// the standard defines behaviour for — the PB1 failure mode. The scan must not sweep them in.</summary>
    [Fact]
    public void TheTestNumvalFamily_IsNotSweptIn()
    {
        var found = SpecFunctionsProhibitingZeroLengthArguments().ToList();
        foreach (string notAProhibition in new[] { "TEST-NUMVAL", "TEST-NUMVAL-C", "TEST-NUMVAL-F" })
            Assert.DoesNotContain(notAProhibition, found, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Every function the spec says carries the rule either declares it on its schema or is adjudicated
    /// out WITH a reason. This is the assertion that makes a seventh clause cost a table row.</summary>
    [Fact]
    public void EveryProhibitingFunction_DeclaresTheRuleOrIsAdjudicated()
    {
        var missing = new List<string>();
        foreach (string name in SpecFunctionsProhibitingZeroLengthArguments())
        {
            if (Adjudicated.ContainsKey(name)) continue;
            if (!IntrinsicArgumentRules.Verified.TryGetValue(name, out var schema)
                || !DeclaresZeroLengthRule(schema))
                missing.Add(name);
        }

        Assert.True(missing.Count == 0,
            $"§15 prohibits a zero-length literal argument for {string.Join(", ", missing)}, but the schema does "
            + $"not declare it — so `FUNCTION {missing.FirstOrDefault() ?? "X"}(\"\" …)` compiles clean at every "
            + $"edition.{Environment.NewLine}Add `noZeroLen: \"§15.N.3 rM\"` to the row (the rule's OWN ordinal, "
            + "which is NOT the class rule's), or adjudicate it into this test's Adjudicated map with its reason.");

        static bool DeclaresZeroLengthRule(ArgSchema schema) =>
            schema.Tail?.NoZeroLengthClause is not null
            || schema.Positions.Any(p => p.NoZeroLengthClause is not null);
    }

    /// <summary>⛔ THE CITED ORDINAL MUST BE THE ZERO-LENGTH RULE'S OWN, NOT THE CLASS RULE'S. MAX's class rule is
    /// §15.59.3 r1 and its zero-length rule is r3; a diagnostic citing r1 for an r3 violation would pass
    /// <c>cite.py --check</c> — the text is really in the standard — while pointing the reader at the wrong rule.
    /// That is why the schema carries a clause STRING here rather than a bool.</summary>
    [Fact]
    public void TheCitedClause_IsNotTheClassRulesClause()
    {
        foreach (var (name, schema) in IntrinsicArgumentRules.Verified)
        {
            foreach (var rule in schema.Positions.Concat(schema.Tail is { } t ? [t] : Array.Empty<ArgRule>()))
            {
                if (rule.NoZeroLengthClause is not { } zl) continue;
                Assert.True(zl != rule.Clause,
                    $"{name} cites the same clause ({zl}) for its zero-length rule as for its class rule — one of "
                    + "them is pointing at the wrong ordinal");
                Assert.Matches(@"^§15\.\d+\.3 r\d+$", zl);
            }
        }
    }
}
