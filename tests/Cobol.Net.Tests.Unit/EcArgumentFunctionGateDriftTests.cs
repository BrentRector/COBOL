// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using CobolNet.Binding.Bound;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE EC-ARGUMENT-FUNCTION AMBIENT GATE MUST NOT DEPEND ON WHICH STATEMENT ENCLOSES THE FUNCTION REFERENCE
/// (fix-queue PB26). <b>ISO §15.3 item 14</b> attaches the condition to the reference — "If the evaluation of an
/// argument results in an incorrect value for that argument or for the returned value according to the rules
/// specified in the function definition … the EC-ARGUMENT-FUNCTION exception condition is set to exist" — with no
/// statement-kind qualification anywhere in it.
///
/// <para><b>What went wrong.</b> The former <c>EcBinder.DirectIntrinsic</c> was a hand-written switch over ~17 statement
/// kinds ending in <c>_ => false</c>, so the gate reached a statement only if someone had remembered its arm:
/// <c>FUNCTION LOG10(0)</c> raised in COMPUTE/MOVE/DISPLAY/IF and was SILENT in STRING. The list is now the
/// generated <c>BoundStatementTree.OwnValueParts</c>, discovered from the semantic model.</para>
///
/// <para>⚠ <b>WHAT THIS FILE DOES AND DOES NOT GUARD, stated because the honest boundary matters.</b> It pins the
/// SHAPE that broke — a value reached through a LIST OF HELPER RECORDS, which both the old switch and any
/// one-level walk miss. **Breadth across statement kinds is guarded BEHAVIOURALLY by the conformance golden**
/// <c>pb26_ec_argument_function_every_statement</c>, which runs a domain violation through each kind and asserts
/// the condition is raised. Two earlier attempts at a broad reflection guard here were UNSOUND and are recorded so
/// nobody rebuilds them: probing an uninitialized node cannot detect a missing arm, because the generated
/// <c>V</c>/<c>Vz</c> helpers are null-safe — an armless leaf and an armed-but-null leaf both yield nothing.</para>
/// </summary>
public sealed class EcArgumentFunctionGateDriftTests
{
    /// <summary>The exact PB26 shape: STRING holds its sending operands in
    /// <c>IReadOnlyList&lt;BoundStringSending&gt;</c>, so the operand sits TWO hops away — through a list, then
    /// through a helper record. The value is injected rather than bound so the test needs no binder, no Place and
    /// no source text; what is under test is the WALK, not the binding.</summary>
    [Fact]
    public void OwnValueParts_ReachesAnOperandThroughAListOfHelperRecords()
    {
        var operand = (BoundOperand)RuntimeHelpers.GetUninitializedObject(
            typeof(BoundStatement).Assembly.GetType("CobolNet.Binding.Bound.BoundComputedOperand")!);

        var sending = (object)RuntimeHelpers.GetUninitializedObject(typeof(BoundStringSending));
        Set(sending, "Value", operand);

        var list = Array.CreateInstance(typeof(BoundStringSending), 1);
        list.SetValue(sending, 0);

        var stmt = (BoundStatement)RuntimeHelpers.GetUninitializedObject(typeof(BoundStringStmt));
        Set(stmt, "Sendings", list);

        var parts = stmt.OwnValueParts().ToList();
        Assert.Contains(operand, parts);
    }

    /// <summary>The same walk must NOT descend into nested statements — those belong to
    /// <c>StatementChildren</c>, and double-counting them would make the EC wrap fire on operands that are not
    /// this statement's own. A STRING with an ON OVERFLOW body yields only its own sending operand.</summary>
    [Fact]
    public void OwnValueParts_DoesNotDescendIntoNestedStatements()
    {
        var inner = (BoundStatement)RuntimeHelpers.GetUninitializedObject(typeof(BoundStringStmt));
        var innerOperand = (BoundOperand)RuntimeHelpers.GetUninitializedObject(
            typeof(BoundStatement).Assembly.GetType("CobolNet.Binding.Bound.BoundComputedOperand")!);
        var innerSending = (object)RuntimeHelpers.GetUninitializedObject(typeof(BoundStringSending));
        Set(innerSending, "Value", innerOperand);
        var innerList = Array.CreateInstance(typeof(BoundStringSending), 1);
        innerList.SetValue(innerSending, 0);
        Set(inner, "Sendings", innerList);

        var outer = (BoundStatement)RuntimeHelpers.GetUninitializedObject(typeof(BoundStringStmt));
        var overflow = Array.CreateInstance(typeof(BoundStatement), 1);
        overflow.SetValue(inner, 0);
        Set(outer, "OnOverflow", overflow);

        Assert.DoesNotContain(innerOperand, outer.OwnValueParts().ToList());
    }

    /// <summary>Assign a record's init-only property through its compiler-generated backing field.</summary>
    private static void Set(object target, string propertyName, object? value)
    {
        var f = target.GetType().GetField($"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.True(f is not null, $"{target.GetType().Name} has no backing field for {propertyName} — "
                                   + "the record's shape changed and this guard must follow it");
        f!.SetValue(target, value);
    }

    /// <summary>⛔ EVERY <c>[BoundNode]</c> ROOT IS EITHER A VALUE THE CONSUMER INTERPRETS OR A CONTAINER THE WALK
    /// GOES THROUGH — and getting that wrong is not hypothetical: the first cut of <c>OwnValueParts</c> treated
    /// <c>BoundPerformControl</c> as a VALUE, yielded the container and stopped, and left
    /// <c>PERFORM UNTIL FUNCTION LOG10(0)</c> silent — the very defect PB26 set out to remove, one level in.
    /// <para>This pins the PARTITION so a NEW root forces a decision instead of silently defaulting. If it fails,
    /// do not edit the expected set until you have answered: does <c>EcBinder.PartHasIntrinsic</c> interpret the
    /// new root (then it is TERMINAL and needs an arm there), or does it merely hold values (then it is a
    /// CONTAINER and the generator's <c>terminalNames</c> must keep excluding it)?</para></summary>
    [Fact]
    public void BoundNodeRootPartition_IsExplicit_SoANewRootForcesADecision()
    {
        var asm = typeof(BoundStatement).Assembly;
        var roots = asm.GetTypes()
            .Where(t => t.IsAbstract && t.Namespace == "CobolNet.Binding.Bound"
                        && t.GetCustomAttributes(false).Any(a => a.GetType().Name == "BoundNodeAttribute"))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        string[] expected =
        [
            "BoundBoolExpr",        // TERMINAL — EcBinder.PartHasIntrinsic walks it
            "BoundCondition",       // TERMINAL
            "BoundExpr",            // TERMINAL
            "BoundOperand",         // TERMINAL
            "BoundPerformControl",  // CONTAINER — holds a PERFORM's UNTIL condition / TIMES count
            "BoundSetTarget",       // CONTAINER — holds a SET's target
            "BoundStatement",       // the walk's subject, never a part
        ];
        Assert.Equal(expected, roots);
    }
}
