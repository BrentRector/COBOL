// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// PB59: <c>IntrinsicBinder.KnownWidth</c> must stay TOTAL over the <c>BoundOperand</c> hierarchy — every
/// concrete leaf either has a switch arm or is in the adjudicated runtime-only list below. The defect this
/// guards against: the pre-PB59 three-arm partial returned null for a GROUP, a REF-MOD view, an ALL literal
/// and a figurative, and the one call site's <c>is { }</c> guard read every null as "skip the §15.26.3 r2 /
/// §15.66.3 r2 one-position screen" — a partial function silently disabling a rule for exactly the shapes
/// nobody wrote a fixture for. A NEW BoundOperand leaf added without deciding its width story lands here.
/// </summary>
public sealed class KnownWidthTotalityDriftTests
{
    /// <summary>Leaves whose width GENUINELY exists only at run time, each with its reason. An entry here is
    /// an adjudication, not a default — a new leaf goes here only with a written reason.</summary>
    private static readonly Dictionary<string, string> RuntimeOnly = new()
    {
        ["BoundComputedOperand"] = "a computed expression's width is its runtime value's",
        ["BoundOperandError"] = "an error operand has no width to know",
        ["BoundNumericLiteral"] = "a numeric literal is not a string-channel width subject (the §15.3 kind "
            + "rules screen it before any width rule applies)",
        ["BoundBoolOperand"] = "a boolean operand does not cross the width-screened argument positions",
        ["BoundAddressOperand"] = "an address-identifier is class pointer and has no character width; the grammar "
            + "admits it only as a relation operand and a CALL / INVOKE argument, never a function argument "
            + "(kb/Work PB1021)",
        ["BoundCurrentRecord"] = "the current record's width IS its ISO §13.18.43.4 GR16 byte count — the "
            + "DEPENDING item's content or the connector's last-read length, both runtime values (kb/Work "
            + "PB339); the operand is also built only for a READ/RETURN INTO implicit MOVE, so it never "
            + "reaches a §15.26.3 r2 / §15.66.3 r2 argument position at all",
    };

    [Fact]
    public void KnownWidth_CoversEveryBoundOperandLeaf_OrAdjudicatesIt()
    {
        string tree = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Bound", "BoundTree.cs"));
        var leaves = Regex.Matches(tree, @"public sealed record (?<t>Bound\w+)(\([^)]*\))?\s*:\s*BoundOperand")
            .Select(m => m.Groups["t"].Value).ToHashSet();
        Assert.True(leaves.Count >= 8, $"the BoundOperand leaf scan found only {leaves.Count} — the regex "
            + "no longer matches BoundTree.cs's record shapes; fix the scan, not the assertion");

        string binder = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs",
            "IntrinsicBinder.cs"));
        var m2 = Regex.Match(binder,
            @"private static int\? KnownWidth\(BoundOperand op\) => op switch\s*\{(?<body>.*?)\n    \};",
            RegexOptions.Singleline);
        Assert.True(m2.Success, "KnownWidth's switch is gone or reshaped — re-point this guard deliberately.");
        var armed = Regex.Matches(m2.Groups["body"].Value, @"(?<t>Bound\w+)")
            .Select(x => x.Groups["t"].Value).ToHashSet();

        var undecided = leaves.Where(l => !armed.Contains(l) && !RuntimeOnly.ContainsKey(l)).Order().ToList();
        Assert.True(undecided.Count == 0,
            $"BoundOperand leaf/leaves [{string.Join(", ", undecided)}] have neither a KnownWidth arm nor a "
            + "RuntimeOnly adjudication. Decide the width story explicitly — a silent null re-opens the "
            + "PB59 partial-function hole (§15.26.3 r2 / §15.66.3 r2 screens skipped with no diagnostic).");
    }
}
