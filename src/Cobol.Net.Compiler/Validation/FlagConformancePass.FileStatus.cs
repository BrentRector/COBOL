// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding;              // DiagnosticCursorAt.At(sink, ctx)
using CobolNet.Binding.Procedure;    // ConditionBinder.SoleDataRef (the canonical bare-operand unwrap)
using CobolNet.Frontend.Cst;         // DataReferenceCst / SpecialRegister
using CobolNet.Frontend.Generated;   // CobolParserCore
using CobolNet.Frontend.Preprocessor;// FlagOption

namespace CobolNet.Validation;

/// <summary>
/// FLAG-14 e I-O-STATUS-04 / f I-O-STATUS-07 (ISO §7.3.15.4 GR4 e/f): "A reference to a data item specified in a FILE
/// STATUS clause that tests for '04' / '07' shall be flagged." A reference TESTS for a status value in every place the
/// language lets a condition compare — so the population of tests is the population of RELATIONS and CONDITION-NAMES,
/// wherever §8.8.4 and §14.9.13 let them be written:
/// <list type="bullet">
/// <item>a relation `FS = "04"` (either operand order, any relational operator) — the leading simple condition of a
/// condition;</item>
/// <item>a SUCCEEDING simple condition whose subject is elided: the abbreviated combined relation
/// `FS = "00" OR "04"` / `FS = "00" OR = "04"` (§8.8.4.12.4 GR1 inserts the last preceding stated subject, so the elided
/// object is a test of the FILE STATUS item exactly as the written one is);</item>
/// <item>a level-88 condition-name defined on the FILE STATUS item whose VALUE is the status;</item>
/// <item>an EVALUATE selection-subject / selection-object pair `EVALUATE FS WHEN "04"` and its partial expression
/// `WHEN = "04"` (§14.9.13.3 SR5/SR8: the object is "treated as though" preceded by the subject), and a bare
/// condition-name object `EVALUATE TRUE WHEN FS-AVAIL`.</item>
/// </list>
/// A range (<c>WHEN "00" THRU "09"</c>, an 88 with a THRU) is deliberately NOT a test FOR '04': whether it contains the
/// value depends on the collating sequence it is ordered in, and it names no status.
/// </summary>
internal sealed partial class FlagConformancePass
{
    private bool FileStatusReferencesPossible =>
        _fileStatusNames.Count > 0 || _fileStatus88Is04.Count > 0 || _fileStatus88Is07.Count > 0;

    /// <summary>The subject of an abbreviated combined relation in flight (§8.8.4.12.4 GR1): the leftmost operand of the
    /// last preceding stated relation of the SAME condition, which every succeeding elided relation takes as its own.</summary>
    private sealed class RelationSubject
    {
        public CobolParserCore.ValueOperandContext? Operand { get; set; }
    }

    /// <summary>A condition — an IF / PERFORM UNTIL / SEARCH / EVALUATE WHEN condition and each parenthesised
    /// sub-condition, which is its own abbreviation scope (the binder's carry restarts inside parentheses) — is the
    /// one host of relations and condition-names; scan its tiers in source order.</summary>
    public override object? VisitCondition(CobolParserCore.ConditionContext ctx)
    {
        if (FileStatusReferencesPossible)
            ScanConditionTree(ctx.logicalOrExpression(), new RelationSubject());
        return base.VisitChildren(ctx);
    }

    /// <summary>EVALUATE's selection subjects and objects (§14.9.13): each WHEN phrase's objects are paired with the
    /// subjects by position (§14.9.13.3 SR2 — one object per subject, joined by ALSO). A value object is compared
    /// with its subject; a partial expression is a condition with the subject elided, scanned with that subject
    /// carried in; a condition object is an ordinary condition (<see cref="VisitCondition"/>).</summary>
    public override object? VisitEvaluateStatement(CobolParserCore.EvaluateStatementContext ctx)
    {
        if (FileStatusReferencesPossible)
        {
            var subjects = ctx.evaluateSubject();
            foreach (var clause in ctx.evaluateWhenClause())
                foreach (var phrase in clause.evaluateWhenPhrase())
                {
                    var objects = phrase.evaluateWhenGroup();
                    for (int i = 0; i < objects.Length; i++)
                        ScanEvaluateObject(i < subjects.Length ? subjects[i].valueOperand() : null,
                            objects[i].evaluateWhenItem());
                }
        }
        return base.VisitChildren(ctx);
    }

    private void ScanEvaluateObject(CobolParserCore.ValueOperandContext? subject,
        CobolParserCore.EvaluateWhenItemContext selectionObject)
    {
        if (selectionObject.valueOperand() is { } value)
        {
            FileStatusRelation(subject, value, selectionObject);                       // EVALUATE FS WHEN "04"
            FileStatusConditionName(BareRefName(value), selectionObject);              // EVALUATE TRUE WHEN FS-AVAIL
        }
        else if (selectionObject.partialExpression() is { } partial)
            ScanConditionTree(partial, new RelationSubject { Operand = subject });     // WHEN = "04"
    }

    /// <summary>Walk the tiers of one condition (or partial expression) in source order, reading each simple condition
    /// against the subject carried so far. A parenthesised condition is NOT entered: it is a <c>ConditionContext</c>
    /// of its own and <see cref="VisitCondition"/> reaches it with a fresh carry.</summary>
    private void ScanConditionTree(IParseTree node, RelationSubject subject)
    {
        switch (node)
        {
            case CobolParserCore.ConditionContext:
                return;
            case CobolParserCore.ComparisonExpressionContext comparison:
                ScanComparison(comparison, subject);
                return;
            case CobolParserCore.AbbreviatedRelationContext abbreviated:
                FileStatusRelation(subject.Operand, abbreviated.comparisonOperand().valueOperand(), abbreviated);
                return;
        }
        for (int i = 0; i < node.ChildCount; i++)
            ScanConditionTree(node.GetChild(i), subject);
    }

    private void ScanComparison(CobolParserCore.ComparisonExpressionContext comparison, RelationSubject subject)
    {
        var operands = comparison.comparisonOperand();
        if (comparison.comparisonOperator() is not null && operands.Length == 2)
        {
            // A relation: one side the FILE-STATUS item, the other the literal '04' / '07' (either order). Its left
            // operand is the subject any elided succeeding relation takes (§8.8.4.12.4 GR1).
            var left = operands[0].valueOperand();
            var right = operands[1].valueOperand();
            FileStatusRelation(left, right, comparison);
            FileStatusRelation(right, left, comparison);
            subject.Operand = left;
        }
        else if (comparison.comparisonOperator() is null && comparison.className() is null
                 && comparison.POSITIVE() is null && comparison.NEGATIVE() is null && comparison.ZERO() is null
                 && comparison.OMITTED() is null && operands.Length == 1)
        {
            // A bare operand: the object of an abbreviated relation whose subject is carried (`FS = "00" OR "04"`),
            // or a level-88 condition-name on a FILE-STATUS item whose VALUE is '04' / '07'.
            var operand = operands[0].valueOperand();
            FileStatusRelation(subject.Operand, operand, comparison);
            FileStatusConditionName(BareRefName(operand), comparison);
        }
    }

    /// <summary>Flag the relation when <paramref name="subject"/> is a bare reference to a FILE-STATUS item and
    /// <paramref name="value"/> is the nonnumeric literal '04' (I-O-STATUS-04) or '07' (I-O-STATUS-07).</summary>
    private void FileStatusRelation(CobolParserCore.ValueOperandContext? subject,
        CobolParserCore.ValueOperandContext? value, ParserRuleContext site)
    {
        if (BareRefName(subject) is not { } name || !_fileStatusNames.Contains(name)) return;
        switch (OperandLiteral(value))
        {
            case "04": FlagAt(site, FlagOption.Flag14IoStatus04, "a relation testing a FILE STATUS item for '04'"); break;
            case "07": FlagAt(site, FlagOption.Flag14IoStatus07, "a relation testing a FILE STATUS item for '07'"); break;
        }
    }

    private void FileStatusConditionName(string? name, ParserRuleContext site)
    {
        if (name is null) return;
        if (_fileStatus88Is04.Contains(name))
            FlagAt(site, FlagOption.Flag14IoStatus04, "a reference to a FILE STATUS condition-name that tests for '04'");
        if (_fileStatus88Is07.Contains(name))
            FlagAt(site, FlagOption.Flag14IoStatus07, "a reference to a FILE STATUS condition-name that tests for '07'");
    }

    /// <summary><see cref="Flag"/> positioned at <paramref name="site"/> — a construct the walk reaches through its
    /// enclosing condition or EVALUATE, whose own start (a later line of a multi-line condition) is the place to point.</summary>
    private void FlagAt(ParserRuleContext site, FlagOption option, string where)
    {
        using var _ = Sink.At(site);
        Flag(option, site.Start.Line, where);
    }

    /// <summary>The base data-name when the operand is a SOLE data reference (the canonical
    /// <see cref="ConditionBinder.SoleDataRef"/> unwrap — non-null only when the arithmetic operand is a lone
    /// reference, not an expression), else null.</summary>
    private static string? BareRefName(CobolParserCore.ValueOperandContext? operand)
    {
        if (operand?.arithmeticExpression() is not { } arithmetic
            || ConditionBinder.SoleDataRef(arithmetic) is not { } dref) return null;
        DataReferenceCst r = dref;
        return r.Register == SpecialRegister.None ? r.BaseName : null;
    }

    /// <summary>The stripped text of a SOLE nonnumeric string-literal operand (§8.3.3.2), else null.</summary>
    private static string? OperandLiteral(CobolParserCore.ValueOperandContext? operand)
        => operand?.nonNumericLiteral()?.STRINGLIT() is { } literal ? StripLiteral(literal.GetText()) : null;
}
