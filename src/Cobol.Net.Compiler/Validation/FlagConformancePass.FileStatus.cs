// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding;              // DiagnosticCursorAt.At(sink, ctx)
using CobolNet.Binding.Model;        // DataItem / Condition88
using CobolNet.Binding.Procedure;    // ConditionBinder.SoleDataRef (the canonical bare-operand unwrap)
using CobolNet.Common;               // CobolLiteral / LiteralClass
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
/// <para>⛔ A REFERENCE IS RESOLVED, NEVER MATCHED BY SPELLING (kb/Work PB3288). "A data item specified in a FILE STATUS
/// clause" is a data item, not a word: the operand is resolved in the current unit's name scope (qualifiers honoured,
/// <c>ResolveItem</c> for a data-name, <c>DataBinder.QualifiedConditionCandidates</c> for a condition-name) and is the
/// reference exactly when the item it resolves to IS (reference identity) some visible file's FILE STATUS item, or the
/// condition-name's conditional variable is. A same-named item of another program, or of another group, is not.</para>
/// <para>The value tested for is the operand literal's VALUE, decoded once by <see cref="CobolLiteral.Decode"/>
/// (kb/Work PB3234): <c>"0'4"</c> is three characters, <c>X"3034"</c> is <c>04</c>.</para>
/// </summary>
internal sealed partial class FlagConformancePass
{
    /// <summary>Whether any file visible from the current unit — its own, or a container's — names a FILE STATUS item
    /// (the cheap guard before a condition is scanned; a level-88 can only hang on such an item).</summary>
    private bool FileStatusReferencesPossible
    {
        get
        {
            for (var unit = _currentUnit; unit is not null; unit = unit.Parent)
                if (unit.Data.Files.Any(f => f.FileStatusItem is not null)) return true;
            return false;
        }
    }

    /// <summary>Whether <paramref name="item"/> IS the data item some file in the current unit or a containing program
    /// specified in its FILE STATUS clause (the data item itself: reference identity, never its name). A container's
    /// FD need not be GLOBAL for its status item to be referenced from a contained program (the item's own GLOBAL
    /// clause makes it visible), so every file of the chain counts here, unlike <see cref="VisibleFiles"/>.</summary>
    private bool IsFileStatusItem(DataItem item)
    {
        for (var unit = _currentUnit; unit is not null; unit = unit.Parent)
            foreach (var file in unit.Data.Files)
                if (ReferenceEquals(file.FileStatusItem, item)) return true;
        return false;
    }

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
                    var objects = phrase.evaluateWhenItem();
                    for (int i = 0; i < objects.Length; i++)
                        ScanEvaluateObject(i < subjects.Length ? subjects[i].valueOperand() : null, objects[i]);
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
            FileStatusConditionName(value, selectionObject);                           // EVALUATE TRUE WHEN FS-AVAIL
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
        else if (comparison.comparisonOperator() is null && comparison.classConditionName() is null
                 && comparison.POSITIVE() is null && comparison.NEGATIVE() is null && comparison.ZERO() is null
                 && comparison.OMITTED() is null && operands.Length == 1)
        {
            // A bare operand: the object of an abbreviated relation whose subject is carried (`FS = "00" OR "04"`),
            // or a level-88 condition-name on a FILE-STATUS item whose VALUE is '04' / '07'.
            var operand = operands[0].valueOperand();
            FileStatusRelation(subject.Operand, operand, comparison);
            FileStatusConditionName(operand, comparison);
        }
    }

    /// <summary>Flag the relation when <paramref name="subject"/> is a bare reference to a FILE-STATUS item and
    /// <paramref name="value"/> is the nonnumeric literal '04' (I-O-STATUS-04) or '07' (I-O-STATUS-07).</summary>
    private void FileStatusRelation(CobolParserCore.ValueOperandContext? subject,
        CobolParserCore.ValueOperandContext? value, ParserRuleContext site)
    {
        if (BareItem(subject) is not { } item || !IsFileStatusItem(item)) return;
        switch (OperandLiteral(value))
        {
            case "04": FlagAt(site, FlagOption.Flag14IoStatus04, "a relation testing a FILE STATUS item for '04'"); break;
            case "07": FlagAt(site, FlagOption.Flag14IoStatus07, "a relation testing a FILE STATUS item for '07'"); break;
        }
    }

    /// <summary>Flag a reference to a level-88 condition-name whose conditional variable IS a FILE-STATUS item and one
    /// of whose single VALUEs is '04' / '07' — "a reference … that tests for" the status (a THRU range names none).</summary>
    private void FileStatusConditionName(CobolParserCore.ValueOperandContext? operand, ParserRuleContext site)
    {
        if (BareCondition(operand) is not { } condition || !IsFileStatusItem(condition.Parent)) return;
        bool tests04 = false, tests07 = false;
        foreach (var (low, high) in condition.Values)
            if (high is null)   // a singleton VALUE tests for exactly that status
                switch (StatusLiteral(low))
                {
                    case "04": tests04 = true; break;
                    case "07": tests07 = true; break;
                }
        if (tests04)
            FlagAt(site, FlagOption.Flag14IoStatus04, "a reference to a FILE STATUS condition-name that tests for '04'");
        if (tests07)
            FlagAt(site, FlagOption.Flag14IoStatus07, "a reference to a FILE STATUS condition-name that tests for '07'");
    }

    /// <summary><see cref="Flag"/> positioned at <paramref name="site"/> — a construct the walk reaches through its
    /// enclosing condition or EVALUATE, whose own start (a later line of a multi-line condition) is the place to point.</summary>
    private void FlagAt(ParserRuleContext site, FlagOption option, string where)
    {
        using var _ = Sink.At(site);
        Flag(option, site.Start.Line, where);
    }

    /// <summary>The data item a SOLE data reference operand resolves to in the current unit's scope (the canonical
    /// <see cref="ConditionBinder.SoleDataRef"/> unwrap — non-null only when the arithmetic operand is a lone
    /// reference, not an expression), else null. Qualifiers are honoured (<c>FS OF G1</c>); subscripts and a
    /// reference modifier select within the described item and are ignored, as in <see cref="ResolveItem"/>.</summary>
    private DataItem? BareItem(CobolParserCore.ValueOperandContext? operand)
        => SoleReference(operand) is { } dref ? ResolveItem(dref) : null;

    /// <summary>The level-88 condition-name a SOLE data reference operand names in the current unit's scope, else
    /// null — one candidate of <see cref="DataBinder.QualifiedConditionCandidates"/> (the resolution the condition
    /// binder reads); several is an ambiguity the binder has already refused, so no condition is named.</summary>
    private Condition88? BareCondition(CobolParserCore.ValueOperandContext? operand)
    {
        if (_currentData is null || SoleReference(operand) is not { } dref
            || dref.cobolWord()?.GetText() is not { } name) return null;
        return _currentData.QualifiedConditionCandidates(name, ConditionBinder.QualifiersOf(dref), _currentData.ActiveScope)
            is [var only] ? only : null;
    }

    private static CobolParserCore.DataReferenceContext? SoleReference(CobolParserCore.ValueOperandContext? operand)
        => operand?.arithmeticExpression() is { } arithmetic ? ConditionBinder.SoleDataRef(arithmetic) : null;

    /// <summary>The VALUE of a SOLE alphanumeric (plain or hexadecimal) or national literal operand (§8.3.3.2, §8.3.3.5) —
    /// decoded once, by the one literal decoder — else null.</summary>
    private static string? OperandLiteral(CobolParserCore.ValueOperandContext? operand)
        => operand?.nonNumericLiteral() is { } literal && CharacterLiteralText(literal) is { } text ? StatusLiteral(text) : null;

    /// <summary>The character value of a raw alphanumeric or national literal operand (a relation's or a level-88's), else
    /// null: <see cref="CobolLiteral.Decode"/> keeps only what §8.3.3.2.4 GR1 keeps (the delimiters are not part of the
    /// value) and what §8.3.3.2.3 SR3 / §8.3.3.5.3 SR3 say (a doubled delimiter is one character), and decodes
    /// the hexadecimal form (§8.3.3.2.4 GR4).</summary>
    private static string? StatusLiteral(string raw)
        => CobolLiteral.ClassOf(raw) is LiteralClass.Alphanumeric or LiteralClass.National ? CobolLiteral.Decode(raw) : null;
}
