// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Expressions;

/// <summary>
/// ⭐ THE boolean-operator INTRODUCTION gate — ONE rule, asked once per SITE that hosts a top-level boolean expression,
/// in BOTH lanes. B-AND / B-OR / B-XOR / B-NOT are a COBOL-2002 introduction (ISO §8.7.2) and the four boolean SHIFT
/// operators a COBOL-2023 one (§8.8.2 rule 8; Annex E.2 item 3), each checked through the ONE
/// <see cref="ConstructRegistry"/> funnel so the code and the §4.2 severity are the registry's.
/// <para><b>Why it lives in the frontend.</b> A compile-time boolean expression (ISO §7.3.7.2 — "formed in accordance
/// with 8.8.2") is evaluated by the conditional-compilation stage, before any compiler pass exists, from a directive
/// FRAGMENT parse the compilation-unit walk never reaches. The gate used to be a private body of
/// <c>VersionConformancePass</c>, so <c>&gt;&gt;DEFINE G AS B"1100" B-SHIFT-L 1</c> compiled clean at 2002 and 2014
/// while the runtime twin <c>COMPUTE B1 = B1 B-SHIFT-L 1</c> was COBOLNET0900 (kb/Work PB1370). Both lanes now call
/// THIS body: the pass per grammar site, the directive stage per evaluated fragment.
/// <c>BooleanExpressionGateSiteDriftTests</c> enumerates the grammar sites and pins that each is gated and that the
/// body is written once.</para>
/// <para><b>Per site, not per node</b>: the tiers nest through parentheses and the relation form, so a gate hanging off
/// the tier rule would fire once per nesting level; one call per site with that site's operand(s) fires once.</para>
/// </summary>
public static class BooleanOperatorGate
{
    /// <summary>Report each boolean-operator construct <paramref name="operands"/> use that
    /// <paramref name="edition"/> has not introduced.</summary>
    /// <param name="edition">The targeted edition.</param>
    /// <param name="sink">The caller's positioned diagnostic sink.</param>
    /// <param name="operands">The site's operand subtree(s); a null entry (an absent optional operand) is skipped.</param>
    public static void Check(EditionInfo edition, IDiagnosticSink sink, params IParseTree?[] operands)
    {
        if (operands.Any(t => t is not null && Contains(t, IsBinaryOrNot)))
            ConstructRegistry.Check(edition, sink, Constructs.BooleanOperators2002,
                "the boolean operators (B-AND/B-OR/B-XOR/B-NOT)");
        if (operands.Any(t => t is not null && Contains(t, IsShift)))
            ConstructRegistry.Check(edition, sink, Constructs.BooleanShiftOperators2023,
                "the boolean shift operators (B-SHIFT-L/R/LC/RC)");
    }

    /// <summary>The COBOL-2002 operator tokens (§8.7.2).</summary>
    private static bool IsBinaryOrNot(int type) =>
        type is CobolLexer.B_AND or CobolLexer.B_OR or CobolLexer.B_XOR or CobolLexer.B_NOT;

    /// <summary>The COBOL-2023 shift-operator tokens (§8.8.2 rule 8) — a DISTINCT construct from the 2002 set, so a
    /// program using only shift operators at 2002 is told about the shift, not the 2002 operators.</summary>
    private static bool IsShift(int type) =>
        type is CobolLexer.B_SHIFT_L or CobolLexer.B_SHIFT_R or CobolLexer.B_SHIFT_LC or CobolLexer.B_SHIFT_RC;

    private static bool Contains(IParseTree t, Func<int, bool> isOperator)
    {
        if (t is ITerminalNode term) return isOperator(term.Symbol.Type);
        for (int i = 0; i < t.ChildCount; i++)
            if (Contains(t.GetChild(i), isOperator)) return true;
        return false;
    }
}
