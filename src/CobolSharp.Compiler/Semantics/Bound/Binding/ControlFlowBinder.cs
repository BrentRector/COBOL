// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Antlr4.Runtime;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Control flow binding: BindPerform, BindEvaluate, BindIf, BindGoTo, BindAlter,
/// BindSearch, BindSearchAll, plus PERFORM VARYING and EVALUATE WHEN helpers.
/// </summary>
internal sealed class ControlFlowBinder
{
    private readonly BindingContext _ctx;

    internal ControlFlowBinder(BindingContext ctx) => _ctx = ctx;

    private SourceLocation MakeLocation(ParserRuleContext ctx) =>
        new(_ctx.SourceName, 0, ctx.Start.Line, ctx.Start.Column);
    private static TextSpan MakeSpan(ParserRuleContext ctx) =>
        new(ctx.Start.StartIndex, ctx.Stop?.StopIndex ?? ctx.Start.StopIndex);

    private (SourceLocation loc, TextSpan span) DiagAt(int line)
        => (new SourceLocation(_ctx.SourceName, 0, line, 0), TextSpan.Empty);

    // ── PERFORM ──

    internal BoundPerformStatement? BindPerform(CobolParserCore.PerformStatementContext ctx)
    {
        var procNames = ctx.procedureName();

        // Inline forms: no procedureName
        if (procNames.Length == 0)
        {
            BoundExpression? untilCond = null;
            BoundPerformVarying? varying = null;
            bool isTestAfter = false;

            BoundExpression? timesExpr = null;
            // The inline loop-control options moved under performInlineHead (the Formats-2/3 grammar merge).
            var options = ctx.performInlineHead()?.performOptions();
            if (options != null)
            {
                foreach (var opt in options)
                {
                    if (opt.performTimes() is { } inlineTimesCtx)
                    {
                        if (inlineTimesCtx.integerLiteral() != null)
                            timesExpr = new BoundLiteralExpression(
                                decimal.Parse(inlineTimesCtx.integerLiteral().GetText()),
                                CobolCategory.Numeric);
                        else if (inlineTimesCtx.dataReference() != null)
                            timesExpr = _ctx.Expression.BindDataReferenceWithSubscripts(inlineTimesCtx.dataReference());
                    }
                    if (opt.performUntil() is { } untilCtx)
                    {
                        untilCond = _ctx.Condition.BindCondition(untilCtx.condition());
                        if (untilCtx.AFTER() != null)
                            isTestAfter = true;
                    }
                    if (opt.performVarying() is { } varyCtx)
                    {
                        varying = BindPerformVaryingOption(varyCtx);
                        if (varyCtx.AFTER() != null)
                            isTestAfter = true;
                    }
                }
            }

            var inlineStmts = new List<BoundStatement>();
            // imperative-statement-1 is a SINGLE required block (ISO §14.9.28.2 Formats 2/3; kb/Work PB396).
            foreach (var stmt in ctx.statementBlock().statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) inlineStmts.Add(bound);
            }

            if (varying != null)
                untilCond = varying.UntilCondition;

            return new BoundPerformStatement(null, null, timesExpr, untilCond, varying, inlineStmts,
                isTestAfter);
        }

        // Out-of-line: first procedureName is the target (paragraph or section)
        var (name, qualifier) = ProcedureNameResolver.ExtractProcedureNameWithQualifier(procNames[0]);
        var (paraSym, sectionLastPara) = _ctx.ProcedureName.ResolveProcedureNameForPerform(name, qualifier);
        if (paraSym == null) return null;

        // PERFORM para N TIMES
        if (ctx.performTimes() is { } timesCtx)
        {
            BoundExpression timesExpr;
            if (timesCtx.integerLiteral() != null)
                timesExpr = new BoundLiteralExpression(
                    decimal.Parse(timesCtx.integerLiteral().GetText()),
                    CobolCategory.Numeric);
            else
                timesExpr = _ctx.Expression.BindDataReferenceWithSubscripts(timesCtx.dataReference());

            return new BoundPerformStatement(paraSym, sectionLastPara, timesExpression: timesExpr);
        }

        // PERFORM para UNTIL cond
        if (ctx.performUntil() is { } untilCtx2)
        {
            var cond = _ctx.Condition.BindCondition(untilCtx2.condition());
            return new BoundPerformStatement(paraSym, sectionLastPara, untilCondition: cond,
                isTestAfter: untilCtx2.AFTER() != null);
        }

        // PERFORM para VARYING ...
        if (ctx.performVarying() is { } varyCtx2)
        {
            var varying = BindPerformVaryingOption(varyCtx2);
            return new BoundPerformStatement(paraSym, sectionLastPara, varying: varying,
                untilCondition: varying?.UntilCondition,
                isTestAfter: varyCtx2.AFTER() != null);
        }

        // PERFORM para THRU para2 [options]
        if (procNames.Length > 1)
        {
            var (thruName, thruQualifier) = ProcedureNameResolver.ExtractProcedureNameWithQualifier(procNames[1]);
            var thruSym = _ctx.ProcedureName.ResolveProcedureNameForThruEnd(thruName, thruQualifier);

            // Check for options on the THRU form
            BoundExpression? timesExpr2 = null;
            BoundExpression? untilCond = null;
            BoundPerformVarying? varyOpt = null;
            bool isTestAfter = false;
            var opt = ctx.performOptions();   // the THRU-form performOptions? is a single direct child
            if (opt != null)
            {
                if (opt.performTimes() is { } thruTimesCtx)
                {
                    if (thruTimesCtx.integerLiteral() != null)
                        timesExpr2 = new BoundLiteralExpression(
                            decimal.Parse(thruTimesCtx.integerLiteral().GetText()),
                            CobolCategory.Numeric);
                    else if (thruTimesCtx.dataReference() != null)
                        timesExpr2 = _ctx.Expression.BindDataReferenceWithSubscripts(thruTimesCtx.dataReference());
                }
                if (opt.performUntil() is { } u)
                {
                    untilCond = _ctx.Condition.BindCondition(u.condition());
                    if (u.AFTER() != null) isTestAfter = true;
                }
                if (opt.performVarying() is { } v)
                {
                    varyOpt = BindPerformVaryingOption(v);
                    untilCond = varyOpt?.UntilCondition;
                    if (v.AFTER() != null) isTestAfter = true;
                }
            }

            return new BoundPerformStatement(paraSym, thruSym, timesExpr2, untilCond, varyOpt,
                isTestAfter: isTestAfter);
        }

        // Simple PERFORM para (or PERFORM section → implicit THRU)
        return new BoundPerformStatement(paraSym, sectionLastPara);
    }

    /// <summary>The varying-phrase FROM/BY slot at index <paramref name="i"/>, as the additive expression this
    /// LEGACY oracle binds. ISO 14.9.28.2's varying-phrase prints a brace group there, and the grammar spells the
    /// slot `valueOperand` since kb/Work PB432 so the figurative ZERO the format admits can reach the greenfield
    /// binder at all. This engine has no arm for a figurative-literal operand and none for an omitted BY, so it
    /// DECLINES such a statement (null) rather than binding something else - it is a differential oracle kept
    /// alive until the P15 cut-over, not a compiler under test.</summary>
    private static CobolParserCore.AdditiveExpressionContext? Additive(CobolParserCore.ValueOperandContext[] ops, int i) =>
        i < ops.Length ? ops[i].arithmeticExpression()?.additiveExpression() : null;

    internal BoundPerformVarying? BindPerformVaryingOption(CobolParserCore.PerformVaryingContext ctx)
    {
        // Build innermost AFTER clauses first, then chain outward
        BoundPerformVarying? inner = null;
        var afterClauses = ctx.performVaryingAfter();
        if (afterClauses != null)
        {
            // Process AFTER clauses in reverse to build inside-out chain
            for (int i = afterClauses.Length - 1; i >= 0; i--)
            {
                var afterCtx = afterClauses[i];
                var afterExpr = _ctx.Expression.BindDataReferenceWithSubscripts(afterCtx.dataReference());
                var afterSym = ValidatePerformIndex(afterExpr);
                if (afterSym == null) continue;
                var afterExprs = afterCtx.valueOperand();
                if (Additive(afterExprs, 0) is not { } afterFrom || Additive(afterExprs, 1) is not { } afterBy) continue;
                var afterInit = _ctx.Expression.BindAdditiveExpression(afterFrom);
                var afterStep = _ctx.Expression.BindAdditiveExpression(afterBy);
                var afterUntil = _ctx.Condition.BindCondition(afterCtx.condition());
                inner = new BoundPerformVarying(afterSym, afterExpr, afterInit, afterStep, afterUntil, inner);
            }
        }

        // Outer VARYING
        var indexExpr = _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());
        var indexSym = ValidatePerformIndex(indexExpr);
        if (indexSym == null) return null;
        var arithExprs = ctx.valueOperand();
        if (Additive(arithExprs, 0) is not { } from || Additive(arithExprs, 1) is not { } by) return null;
        var initial = _ctx.Expression.BindAdditiveExpression(from);  // FROM
        var step = _ctx.Expression.BindAdditiveExpression(by);       // BY
        var untilCond = _ctx.Condition.BindCondition(ctx.condition());

        return new BoundPerformVarying(indexSym, indexExpr, initial, step, untilCond, inner);
    }

    /// <summary>
    /// Validate a PERFORM VARYING index expression: must be a non-subscripted
    /// elementary numeric identifier. Returns the DataSymbol or null on failure.
    /// </summary>
    internal DataSymbol? ValidatePerformIndex(BoundExpression indexExpr)
    {
        if (indexExpr is not BoundIdentifierExpression id)
        {
            // CS0860: PERFORM VARYING index must be an identifier
            // (ref-mod or other expression not allowed)
            return null;
        }

        return id.Symbol;
    }

    // ── EVALUATE ──

    /// <summary>The sole comparisonExpression a condition reduces to through single-child tiers (no AND/OR/NOT),
    /// or null — the one condition-1 shape this oracle models as an EVALUATE subject (kb/Work PB842).</summary>
    private static CobolParserCore.ComparisonExpressionContext? SoleComparison(CobolParserCore.ConditionContext? cond)
    {
        Antlr4.Runtime.Tree.IParseTree? n = cond;
        while (n is not null and not CobolParserCore.ComparisonExpressionContext)
            n = n.ChildCount == 1 ? n.GetChild(0) : null;
        return n as CobolParserCore.ComparisonExpressionContext;
    }

    internal BoundEvaluateStatement BindEvaluate(CobolParserCore.EvaluateStatementContext ctx)
    {
        // Bind subjects — track per-subject type (Value, True, False)
        var subjects = new List<BoundExpression>();
        var subjectKinds = new List<EvaluateSubjectKind>();
        // Per-subject class condition (only for class-condition subjects like EVALUATE X NUMERIC)
        var classConditions = new List<BoundClassConditionExpression?>();

        foreach (var subCtx in ctx.evaluateSubject())
        {
            if (subCtx.booleanLiteral()?.TRUE_() != null)
            {
                subjectKinds.Add(EvaluateSubjectKind.True);
                subjects.Add(null!); // placeholder — not used for TRUE subjects
                classConditions.Add(null);
                continue;
            }
            if (subCtx.booleanLiteral()?.FALSE_() != null)
            {
                subjectKinds.Add(EvaluateSubjectKind.False);
                subjects.Add(null!); // placeholder — not used for FALSE subjects
                classConditions.Add(null);
                continue;
            }
            // Check for class condition: EVALUATE X NUMERIC → treat as EVALUATE TRUE
            // where the implicit condition is "X IS [NOT] NUMERIC/ALPHABETIC"
            // The EVALUATE subject's class test now names the ONE §8.8.4.4.2 alternative list (`className`);
            // the private `classCondition` rule this read was deleted with kb/Work PB590. The kind decode is
            // EXPLICITLY PARTIAL and says so: this 85-era oracle models four of the alternatives, and the
            // fallback it used to carry (`: ClassConditionKind.Numeric`) silently turned every other one into a
            // NUMERIC test. A BOOLEAN / class-name / alphabet-name subject now falls out of this arm instead.
            // kb/Work PB842: condition-1 is now the grammar's ONE `condition` rule, so the class test arrives as a
            // sole comparisonExpression under it; this oracle still models only that one condition shape.
            var subjCmp = SoleComparison(subCtx.condition());
            if (subjCmp?.className() is { } classCtx
                && subjCmp.comparisonOperand() is [{ } classOperand] && classOperand.valueOperand() is { } classVo
                && (classCtx.NUMERIC() != null ? ClassConditionKind.Numeric
                    : classCtx.ALPHABETIC() != null ? ClassConditionKind.Alphabetic
                    : classCtx.ALPHABETIC_LOWER() != null ? ClassConditionKind.AlphabeticLower
                    : classCtx.ALPHABETIC_UPPER() != null ? ClassConditionKind.AlphabeticUpper
                    : (ClassConditionKind?)null) is { } classKind)
            {
                BoundExpression classSubject;
                if (classVo.arithmeticExpression() is { } classArith)
                    classSubject = _ctx.Expression.BindAdditiveExpression(classArith.additiveExpression());
                else if (classVo.nonNumericLiteral() is { } classNonNum)
                    classSubject = _ctx.Expression.BindNonNumericLiteral(classNonNum);
                else
                    continue;
                bool isClassNot = subjCmp.NOT() != null;
                subjectKinds.Add(EvaluateSubjectKind.True);
                subjects.Add(null!); // placeholder
                classConditions.Add(new BoundClassConditionExpression(classSubject, classKind, isClassNot));
                continue;
            }
            BoundExpression? subjectExpr = null;
            if (subCtx.valueOperand()?.arithmeticExpression() is { } arithCtx)
                subjectExpr = _ctx.Expression.BindAdditiveExpression(arithCtx.additiveExpression());
            else if (subCtx.valueOperand()?.nonNumericLiteral() is { } nonNumCtx)
                subjectExpr = _ctx.Expression.BindNonNumericLiteral(nonNumCtx);
            if (subjectExpr == null)
                continue;

            // Condition-name subjects are boolean — treat like EVALUATE TRUE for this position
            if (subjectExpr is BoundConditionNameExpression)
            {
                subjectKinds.Add(EvaluateSubjectKind.True);
                subjects.Add(subjectExpr); // condition-name expr used as implicit condition
                classConditions.Add(null);
            }
            else
            {
                subjectKinds.Add(EvaluateSubjectKind.Value);
                subjects.Add(subjectExpr);
                classConditions.Add(null);
            }
        }

        int subjectCount = subjects.Count;

        // Bind WHEN clauses
        var whens = new List<BoundEvaluateWhen>();
        List<BoundStatement>? whenOther = null;

        foreach (var whenClause in ctx.evaluateWhenClause())
        {
            // Bind the shared imperative once: consecutive WHEN phrases (WHEN a WHEN b ...)
            // before a single statement list all execute that list (ISO 14.8.4 — the
            // phrases are OR'd). Each phrase becomes its own match arm over this body, so
            // the first phrase that matches runs it.
            var stmts = new List<BoundStatement>();
            foreach (var stmt in whenClause.statementBlock().statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) stmts.Add(bound);
            }

            foreach (var phrase in whenClause.evaluateWhenPhrase())
            {
                // Per-subject groups (separated by ALSO) within this phrase.
                var groups = phrase.evaluateWhenGroup();
                var subjectConditions = new List<BoundEvaluateCondition>();

                for (int i = 0; i < subjectCount && i < groups.Length; i++)
                {
                    var kind = subjectKinds[i];
                    bool isCondSubject = kind == EvaluateSubjectKind.True || kind == EvaluateSubjectKind.False;
                    // For condition-name subjects, pass the subject expression as the implicit condition
                    BoundExpression? condNameSubject = (isCondSubject && subjects[i] is BoundConditionNameExpression)
                        ? subjects[i] : null;
                    subjectConditions.Add(BindEvaluateWhenGroup(groups[i], isCondSubject, classConditions[i], condNameSubject));
                }
                // If fewer groups than subjects → semantic error; fill with "never match"
                // so the WHEN clause doesn't fire (missing subjects are non-matching)
                for (int i = groups.Length; i < subjectCount; i++)
                    subjectConditions.Add(new BoundEvaluateValueCondition(
                        Array.Empty<BoundExpression>(), Array.Empty<BoundEvaluateRange>(), isAny: false));

                whens.Add(new BoundEvaluateWhen(subjectConditions, stmts));
            }
        }

        // `[ WHEN OTHER imperative-statement-2 ]` is ONE optional trailing phrase of the statement, not a member
        // of the repeated clause (ISO §14.9.13.2 with §5.2.7; kb/Work PB396). Bound AFTER the clause loop so the
        // statements still bind in SOURCE order.
        if (ctx.evaluateWhenOther() is { } otherClause)
        {
            var otherStmts = new List<BoundStatement>();
            foreach (var stmt in otherClause.statementBlock().statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) otherStmts.Add(bound);
            }
            whenOther = otherStmts;
        }

        return new BoundEvaluateStatement(subjects, subjectKinds, whens, whenOther);
    }

    internal BoundEvaluateCondition BindEvaluateWhenGroup(
        CobolParserCore.EvaluateWhenGroupContext groupCtx, bool isConditionSubject,
        BoundClassConditionExpression? classConditionSubject = null,
        BoundExpression? condNameSubject = null)
    {
        // ONE selection-object per position (§14.9.13.2; the grammar's `evaluateWhenItem+` became a single item with
        // fix-queue PB45 — objects repeat only through ALSO, which is the caller's `evaluateWhenGroup` loop). This is
        // the mechanical adaptation of the frozen oracle to that arity: for legal source the list could never hold
        // more than one item, so every reading below is unchanged.
        var item0 = groupCtx.evaluateWhenItem();
        CobolParserCore.EvaluateWhenItemContext[] items = item0 is null ? [] : [item0];

        if (isConditionSubject)
        {
            // Handle ANY first — always matches regardless of subject type
            if (items.Length > 0 && items[0].ANY() != null)
                return new BoundEvaluateValueCondition(
                    Array.Empty<BoundExpression>(), Array.Empty<BoundEvaluateRange>(), isAny: true);

            // For class condition subjects (e.g., EVALUATE X NUMERIC WHEN TRUE/FALSE),
            // inject the class condition as the WHEN's condition
            if (classConditionSubject != null && items.Length > 0)
            {
                var item = items[0];
                // Check if WHEN item is TRUE or FALSE via condition → booleanLiteral
                var boolLit = item.condition()?.logicalOrExpression()?.logicalXorExpression()?.logicalAndExpression()
                    ?.unaryLogicalExpression()?.primaryCondition()?.booleanLiteral();
                if (boolLit?.TRUE_() != null)
                    return new BoundEvaluateConditionWhen(classConditionSubject);
                if (boolLit?.FALSE_() != null)
                    return new BoundEvaluateConditionWhen(
                        new BoundClassConditionExpression(classConditionSubject.Subject,
                            classConditionSubject.ClassKind, !classConditionSubject.IsNegated));
            }
            // For condition-name subjects (e.g., EVALUATE condName WHEN TRUE/FALSE),
            // inject the condition-name as the WHEN's condition
            if (condNameSubject is BoundConditionNameExpression condNameExpr && items.Length > 0)
            {
                var item = items[0];
                var boolLit = item.condition()?.logicalOrExpression()?.logicalXorExpression()?.logicalAndExpression()
                    ?.unaryLogicalExpression()?.primaryCondition()?.booleanLiteral();
                if (boolLit?.TRUE_() != null)
                    return new BoundEvaluateConditionWhen(condNameExpr);
                if (boolLit?.FALSE_() != null)
                    return new BoundEvaluateConditionWhen(
                        new BoundConditionNameExpression(condNameExpr.Condition, !condNameExpr.IsNegated,
                            condNameExpr.ParentExpression));
            }
            // For EVALUATE TRUE/FALSE, the WHEN item is a condition
            if (items.Length > 0 && items[0].condition() is { } condCtx)
                return new BoundEvaluateConditionWhen(_ctx.Condition.BindCondition(condCtx));
            // Fallback: try as valueOperand (bare identifier → condition name)
            if (items.Length > 0 && items[0].valueOperand()?.arithmeticExpression() is { } arithFallback)
            {
                var expr = _ctx.Expression.BindAdditiveExpression(arithFallback.additiveExpression());
                // Check if the result resolves to a condition name
                expr = _ctx.Condition.TryResolveConditionName(expr);
                return new BoundEvaluateConditionWhen(expr);
            }
            return new BoundEvaluateValueCondition(
                Array.Empty<BoundExpression>(), Array.Empty<BoundEvaluateRange>(), isAny: true);
        }

        // Normal EVALUATE: bind values and ranges
        var values = new List<BoundExpression>();
        var ranges = new List<BoundEvaluateRange>();
        bool isAny = false;

        foreach (var item in items)
        {
            if (item.ANY() != null)
            {
                isAny = true;
                continue;
            }

            if (item.valueRange() is { } rangeCtx)
            {
                // Range: value THRU value
                var from = BindValueOperand(rangeCtx.valueOperand(0));
                var to = BindValueOperand(rangeCtx.valueOperand(1));
                ranges.Add(new BoundEvaluateRange(from, to));
            }
            else if (item.valueOperand() is { } voCtx)
            {
                values.Add(BindValueOperand(voCtx));
            }
            else if (item.condition() is { } condCtx)
            {
                values.Add(_ctx.Condition.BindCondition(condCtx));
            }
        }

        bool isNot = groupCtx.NOT() != null;
        return new BoundEvaluateValueCondition(values, ranges, isAny, isNot);
    }

    internal BoundExpression BindValueOperand(CobolParserCore.ValueOperandContext vo)
    {
        if (vo.nonNumericLiteral() is { } nonNumCtx)
            return _ctx.Expression.BindNonNumericLiteral(nonNumCtx);
        return _ctx.Expression.BindAdditiveExpression(vo.arithmeticExpression().additiveExpression());
    }

    // ── IF ──

    internal BoundIfStatement? BindIf(CobolParserCore.IfStatementContext ctx)
    {
        var condCtx = ctx.condition();
        if (condCtx == null) return null;

        // Try to bind a real condition
        var condition = _ctx.Condition.BindCondition(condCtx);

        var thenStmts = new List<BoundStatement>();
        var elseStmts = new List<BoundStatement>();

        // Walk imperativeStatement* children
        var impStmts = ctx.statementBlock();
        if (impStmts.Length > 0)
        {
            foreach (var stmt in impStmts[0].statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) thenStmts.Add(bound);
            }
        }

        // ELSE block
        if (impStmts.Length > 1)
        {
            foreach (var stmt in impStmts[1].statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) elseStmts.Add(bound);
            }
        }

        return new BoundIfStatement(condition, thenStmts, elseStmts.Count > 0 ? elseStmts : null);
    }

    // ── GO TO ──

    internal BoundGoToStatement? BindGoTo(CobolParserCore.GoToStatementContext ctx)
    {
        var procNames = ctx.procedureName();

        // Bare GO TO (no target) — target set by ALTER at runtime
        if (procNames == null || procNames.Length == 0)
        {
            if (_ctx.Options.Config.IsCobol2002OrLater)
            {
                _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3605,
                    MakeLocation(ctx), MakeSpan(ctx), _ctx.Options.Config.DisplayName);
                return null;
            }
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3606,
                MakeLocation(ctx), MakeSpan(ctx));
            return new BoundGoToStatement([]);
        }

        var targets = new List<ParagraphSymbol>();

        foreach (var pn in procNames)
        {
            var (name, qual) = ProcedureNameResolver.ExtractProcedureNameWithQualifier(pn);
            var paraSym = _ctx.ProcedureName.ResolveProcedureName(name, qual);
            if (paraSym != null) targets.Add(paraSym);
        }

        if (targets.Count == 0) return null;

        // DEPENDING ON identifier (optional)
        BoundIdentifierExpression? dependingOn = null;
        if (ctx.DEPENDING() != null && ctx.dataReference() != null)
        {
            var depExpr = _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());
            dependingOn = depExpr as BoundIdentifierExpression;
        }

        return new BoundGoToStatement(targets, dependingOn);
    }

    // ── ALTER ──

    internal BoundAlterStatement? BindAlter(CobolParserCore.AlterStatementContext ctx)
    {
        // Dialect check: ALTER deleted from COBOL-2002+
        if (_ctx.Options.Config.IsCobol2002OrLater)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3601,
                MakeLocation(ctx), MakeSpan(ctx), _ctx.Options.Config.DisplayName);
            return null;
        }

        // Obsolete warning in COBOL-85 / Default mode
        _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3602,
            MakeLocation(ctx), MakeSpan(ctx));

        var entries = new List<BoundAlterEntry>();
        foreach (var entry in ctx.alterEntry())
        {
            var procNames = entry.procedureName();
            if (procNames.Length < 2) continue;

            var (targetName, targetQual) = ProcedureNameResolver.ExtractProcedureNameWithQualifier(procNames[0]);
            var (destName, destQual) = ProcedureNameResolver.ExtractProcedureNameWithQualifier(procNames[1]);

            var targetSym = _ctx.ProcedureName.ResolveProcedureName(targetName, targetQual);
            var destSym = _ctx.ProcedureName.ResolveProcedureName(destName, destQual);

            if (targetSym == null)
            {
                _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3603,
                    MakeLocation(entry), MakeSpan(entry), targetName);
                continue;
            }
            if (destSym == null)
            {
                _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3603,
                    MakeLocation(entry), MakeSpan(entry), destName);
                continue;
            }

            entries.Add(new BoundAlterEntry(targetSym, destSym));
        }

        return entries.Count > 0 ? new BoundAlterStatement(entries) : null;
    }

    // ── SEARCH ──

    internal BoundStatement? BindSearch(CobolParserCore.SearchStatementContext ctx)
    {
        var dataRefs = ctx.dataReference();
        var tableExpr = _ctx.Expression.BindDataReferenceWithSubscripts(dataRefs[0]);
        if (tableExpr is not BoundIdentifierExpression tableId) return null;

        // Bind VARYING identifier (second dataReference, if present)
        BoundIdentifierExpression? varyingExpr = null;
        if (dataRefs.Length > 1)
        {
            var varyBound = _ctx.Expression.BindDataReferenceWithSubscripts(dataRefs[1]);
            if (varyBound is BoundIdentifierExpression varyId)
                varyingExpr = varyId;
        }

        // Bind WHEN clauses
        var whens = new List<BoundSearchWhenClause>();
        foreach (var whenCtx in ctx.searchWhenClause())
        {
            var cond = _ctx.Condition.BindCondition(whenCtx.condition());
            var stmts = new List<BoundStatement>();
            // `{ imperative-statement-2 | NEXT SENTENCE }` — a single required block (ISO §14.9.37.2; PB396).
            foreach (var stmt in whenCtx.statementBlock().statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) stmts.Add(bound);
            }
            whens.Add(new BoundSearchWhenClause(cond, stmts));
        }

        // Bind AT END
        var atEnd = new List<BoundStatement>();
        if (ctx.searchAtEndClause() is { } atEndCtx)
        {
            foreach (var imp in new[] { atEndCtx.statementBlock() })
                foreach (var stmt in imp.statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
        }

        // Extract index: find the first subscript used on a table element in WHEN conditions
        var index = ExtractSearchIndex(tableId.Symbol, whens);

        var searchStmt = new BoundSearchStatement(tableId, index, varyingExpr, whens, atEnd);
        ValidateSearchStatement(searchStmt, ctx.Start?.Line ?? 0);
        return searchStmt;
    }

    internal BoundStatement? BindSearchAll(CobolParserCore.SearchAllStatementContext ctx)
    {
        var tableExpr = _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());
        if (tableExpr is not BoundIdentifierExpression tableId) return null;

        // Bind WHEN clauses
        var whens = new List<BoundSearchWhenClause>();
        // Format 2 prints exactly ONE WHEN phrase (ISO §14.9.37.2; the shared grammar spells it — kb/Work PB446).
        if (ctx.searchAllWhenClause() is { } whenCtx)
        {
            var cond = _ctx.Condition.BindCondition(whenCtx.condition());
            var stmts = new List<BoundStatement>();
            // `{ imperative-statement-2 | NEXT SENTENCE }` — a single required block (ISO §14.9.37.2; PB396).
            foreach (var stmt in whenCtx.statementBlock().statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) stmts.Add(bound);
            }
            whens.Add(new BoundSearchWhenClause(cond, stmts));
        }

        // Bind AT END
        var atEnd = new List<BoundStatement>();
        if (ctx.searchAtEndClause() is { } atEndCtx)
        {
            foreach (var imp in new[] { atEndCtx.statementBlock() })
                foreach (var stmt in imp.statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
        }

        // Extract index from WHEN conditions
        var index = ExtractSearchIndex(tableId.Symbol, whens);

        var searchAllStmt = new BoundSearchAllStatement(tableId, index, whens, atEnd);
        ValidateSearchAllStatement(searchAllStmt, ctx.Start?.Line ?? 0);
        return searchAllStmt;
    }

    /// <summary>
    /// Walk WHEN conditions to find the subscript variable used on the table.
    /// For WHEN TBL(IDX) = 3, extracts IDX as the search index.
    /// </summary>
    internal BoundIdentifierExpression? ExtractSearchIndex(
        DataSymbol tableSymbol, List<BoundSearchWhenClause> whens)
    {
        foreach (var when in whens)
        {
            var idx = FindSubscriptOnTable(tableSymbol, when.Condition);
            if (idx != null) return idx;
        }
        return null;
    }

    internal BoundIdentifierExpression? FindSubscriptOnTable(
        DataSymbol tableSymbol, BoundExpression expr)
    {
        switch (expr)
        {
            case BoundIdentifierExpression id when id.IsSubscripted:
                if (IsTableElement(id.Symbol, tableSymbol))
                {
                    // Find which subscript corresponds to the SEARCH table's dimension.
                    // Walk the parent chain collecting OCCURS levels (outermost first).
                    // Subscripts are positional: subscripts[0]→outermost, subscripts[N-1]→innermost.
                    // The SEARCH table's position in the OCCURS list gives the subscript index.
                    var occursLevels = new List<DataSymbol>();
                    var current = id.Symbol;
                    while (current != null)
                    {
                        if (current.Occurs != null)
                            occursLevels.Insert(0, current);
                        current = current.Parent;
                    }

                    for (int i = 0; i < occursLevels.Count && i < id.Subscripts!.Count; i++)
                    {
                        if (occursLevels[i] == tableSymbol
                            && id.Subscripts[i] is BoundIdentifierExpression subId)
                            return subId;
                    }
                }
                break;

            case BoundBinaryExpression bin:
                return FindSubscriptOnTable(tableSymbol, bin.Left)
                    ?? FindSubscriptOnTable(tableSymbol, bin.Right);

            case BoundReferenceModificationExpression refMod:
                return FindSubscriptOnTable(tableSymbol, refMod.Base);
        }
        return null;
    }

    /// <summary>
    /// Check if sym is the table symbol itself, or a child (subordinate) of it.
    /// For OCCURS groups like TBL containing VAL and FLAG children,
    /// a reference to VAL(IDX) should still identify IDX as the table index.
    /// </summary>
    internal static bool IsTableElement(DataSymbol sym, DataSymbol tableSymbol)
    {
        var current = sym;
        while (current != null)
        {
            if (current == tableSymbol) return true;
            current = current.Parent;
        }
        return false;
    }

    internal void ValidateSearchStatement(BoundSearchStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // Table must have OCCURS
        if (stmt.Table.Symbol.Occurs == null)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1105, loc, span, stmt.Table.Symbol.DisplayName);
    }

    internal void ValidateSearchAllStatement(BoundSearchAllStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // Table must have OCCURS
        if (stmt.Table.Symbol.Occurs == null)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1202, loc, span, stmt.Table.Symbol.DisplayName);
        // Table must have KEY clause
        var occurs = stmt.Table.Symbol.Occurs;
        if (occurs != null && occurs.AscendingKeys.Count == 0 && occurs.DescendingKeys.Count == 0)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1204, loc, span, stmt.Table.Symbol.DisplayName);

        // Check 10: SEARCH ALL WHEN must be equality comparison on key fields
        foreach (var when in stmt.Whens)
        {
            if (!IsSearchAllEqualityCondition(when.Condition))
                _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1206, loc, span);
        }
    }

    /// <summary>
    /// Returns true if the condition is a valid SEARCH ALL WHEN condition:
    /// a simple equality comparison, an AND of equality comparisons,
    /// or a condition-name (level-88) check.
    /// </summary>
    internal static bool IsSearchAllEqualityCondition(BoundExpression condition)
    {
        return condition switch
        {
            BoundBinaryExpression { OperatorKind: BoundBinaryOperatorKind.Equal } => true,
            BoundBinaryExpression { OperatorKind: BoundBinaryOperatorKind.And } and_ =>
                IsSearchAllEqualityCondition(and_.Left) && IsSearchAllEqualityCondition(and_.Right),
            BoundConditionNameExpression => true, // Level-88 condition-name
            _ => false,
        };
    }
}
