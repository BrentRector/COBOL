// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Expressions;

/// <summary>
/// ⭐ THE logical-operator INTRODUCTION gate — ONE rule, asked by BOTH lanes. The exclusive-or connective
/// ('EXCLUSIVE-OR' or 'XOR', ISO §8.8.4.9) is a COBOL-2023 introduction (Annex E.2 item 25), checked through the ONE
/// <see cref="ConstructRegistry"/> funnel so the code and the §4.2 severity are the registry's.
/// <para><b>One node to recognize.</b> Every tier that admits the connective — the condition's leading and succeeding
/// tiers, the EVALUATE partial-expression spine and the constant-conditional-expression tiers — spells it through the
/// one <c>xorOperator</c> grammar rule, so the gate asks for that node and never for a tier: a tier-keyed gate is a
/// hand-kept list of tiers, and the partial-expression XOR was the tier it had forgotten (kb/Work PB1390).</para>
/// <para><b>Both lanes.</b> <c>VersionConformancePass</c> calls it per <c>xorOperator</c> node of the compilation
/// unit; the conditional-compilation stage calls it per constant-conditional-expression FRAGMENT, which the
/// compilation-unit walk never reaches — a >>IF operand is "a complex condition as specified in 8.8.4.9" (§7.3.8.2
/// SR1 d)) of the targeted edition, so <c>&gt;&gt;IF A = 1 XOR B = 2</c> below 2023 is the same COBOLNET0900 its
/// runtime twin draws (kb/Work PB1371). The <see cref="BooleanOperatorGate"/> precedent (kb/Work PB1370).</para>
/// </summary>
public static class LogicalOperatorGate
{
    /// <summary>Report the exclusive-or connective if <paramref name="tree"/> contains one and
    /// <paramref name="edition"/> has not introduced it — once per call, however many the tree holds.</summary>
    /// <param name="edition">The targeted edition.</param>
    /// <param name="sink">The caller's positioned diagnostic sink.</param>
    /// <param name="tree">An <c>xorOperator</c> node, or a fragment subtree to scan.</param>
    public static void Check(EditionInfo edition, IDiagnosticSink sink, IParseTree tree)
    {
        if (Contains(tree))
            ConstructRegistry.Check(edition, sink, Constructs.LogicalXorOperator2023, "the logical XOR operator");
    }

    private static bool Contains(IParseTree t)
    {
        if (t is CobolParserCore.XorOperatorContext) return true;
        for (int i = 0; i < t.ChildCount; i++)
            if (Contains(t.GetChild(i))) return true;
        return false;
    }
}
