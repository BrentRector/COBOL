// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Condition binding: BindCondition, BindLogicalOr/And, BindComparison,
/// abbreviated conditions, sign/class conditions, condition-name resolution.
/// </summary>
internal sealed class ConditionBinder
{
    private readonly BindingContext _ctx;

    internal ConditionBinder(BindingContext ctx) => _ctx = ctx;

    /// <summary>
    /// If expr is a bare identifier or unresolved string that matches a level-88
    /// condition name, return a BoundConditionNameExpression; otherwise return expr unchanged.
    /// </summary>
    internal BoundExpression TryResolveConditionName(BoundExpression expr)
    {
        string? name = null;
        if (expr is BoundIdentifierExpression idExpr)
            name = idExpr.Symbol.Name;
        else if (expr is BoundLiteralExpression litExpr && litExpr.Value is string s)
            name = s;

        if (name != null)
        {
            var condSym = _ctx.Semantic.ResolveConditionName(name);
            if (condSym != null)
                return new BoundConditionNameExpression(condSym);
        }
        return expr;
    }

    /// <summary>
    /// Bind a full condition expression with AND/OR/NOT and relational operators.
    /// condition -> logicalOrExpression -> logicalAndExpression -> logicalNotExpression -> relationalExpression
    /// </summary>
    internal BoundExpression BindCondition(CobolParserCore.ConditionContext ctx)
    {
        var orExpr = ctx.logicalOrExpression();
        if (orExpr == null)
            return new BoundLiteralExpression(true, CobolCategory.Unknown);
        var bound = BindLogicalOr(orExpr);
        return ExpandAbbreviatedConditions(bound);
    }

    // The condition tiers (ISO §8.8.4.9; NOT > AND > XOR > OR, §8.8.4.11.3). Each LEADING tier holds the first
    // simple condition; every connective's operand is a SUCCEEDING tier whose leftmost element may be an
    // abbreviated relation (§8.8.4.12; kb/Work PB1390). The abbreviations are expanded afterwards by
    // ExpandAbbreviatedConditions, so the tiers only fold.
    internal BoundExpression BindLogicalOr(CobolParserCore.LogicalOrExpressionContext ctx)
    {
        var result = BindLogicalXor(ctx.logicalXorExpression());
        foreach (var s in ctx.succeedingXorExpression())
            result = new BoundBinaryExpression(result, BoundBinaryOperatorKind.Or, BindSucceedingXor(s),
                CobolCategory.Unknown);
        return result;
    }

    // COBOL-2023 logical exclusive-or (ISO §8.8.4.9; precedence between OR and AND). XOR / EXCLUSIVE-OR are
    // equivalent; the truth value is true iff exactly one operand condition is true.
    internal BoundExpression BindLogicalXor(CobolParserCore.LogicalXorExpressionContext ctx)
    {
        var result = BindLogicalAnd(ctx.logicalAndExpression());
        foreach (var s in ctx.succeedingAndExpression())
            result = new BoundBinaryExpression(result, BoundBinaryOperatorKind.Xor, BindSucceedingAnd(s),
                CobolCategory.Unknown);
        return result;
    }

    internal BoundExpression BindLogicalAnd(CobolParserCore.LogicalAndExpressionContext ctx)
    {
        var result = BindUnaryLogical(ctx.unaryLogicalExpression());
        foreach (var s in ctx.succeedingCondition())
            result = new BoundBinaryExpression(result, BoundBinaryOperatorKind.And, BindSucceedingCondition(s),
                CobolCategory.Unknown);
        return result;
    }

    internal BoundExpression BindSucceedingXor(CobolParserCore.SucceedingXorExpressionContext ctx)
    {
        var ands = ctx.succeedingAndExpression();
        var result = BindSucceedingAnd(ands[0]);
        for (int i = 1; i < ands.Length; i++)
            result = new BoundBinaryExpression(result, BoundBinaryOperatorKind.Xor, BindSucceedingAnd(ands[i]),
                CobolCategory.Unknown);
        return result;
    }

    internal BoundExpression BindSucceedingAnd(CobolParserCore.SucceedingAndExpressionContext ctx)
    {
        var conds = ctx.succeedingCondition();
        var result = BindSucceedingCondition(conds[0]);
        for (int i = 1; i < conds.Length; i++)
            result = new BoundBinaryExpression(result, BoundBinaryOperatorKind.And, BindSucceedingCondition(conds[i]),
                CobolCategory.Unknown);
        return result;
    }

    /// <summary>A non-first simple condition: an abbreviated relation (§8.8.4.12) or an ordinary (NOT-)condition.</summary>
    internal BoundExpression BindSucceedingCondition(CobolParserCore.SucceedingConditionContext ctx) =>
        ctx.abbreviatedRelation() is { } abbrev ? BindAbbreviatedRelation(abbrev) : BindUnaryLogical(ctx.unaryLogicalExpression());

    /// <summary>
    /// Bind an abbreviated relational condition (COBOL-85 section 6.3.4.2).
    /// An abbreviated relation has an optional operator and a comparisonOperand.
    /// The missing left operand (and possibly missing operator) will be filled in
    /// by RewriteAbbreviatedRelations after the entire condition is bound.
    /// </summary>
    internal BoundExpression BindAbbreviatedRelation(CobolParserCore.AbbreviatedRelationContext ctx)
    {
        var operandCtx = ctx.comparisonOperand();
        var operatorCtx = ctx.comparisonOperator();

        var right = BindComparisonOperand(operandCtx);
        var op = ParseComparisonOperator(operatorCtx);

        // Use a sentinel BoundAbbreviatedExpression to mark this for rewriting.
        // The right operand is the value; the operator is parsed.
        // Left operand will be filled from context by RewriteAbbreviatedRelations.
        return new BoundAbbreviatedExpression(op, right);
    }

    internal BoundExpression BindUnaryLogical(CobolParserCore.UnaryLogicalExpressionContext ctx)
    {
        // NOT primaryCondition (non-recursive per COBOL-85 section 6.3.4)
        if (ctx.NOT() != null && ctx.primaryCondition() is { } negated)
        {
            var inner = BindPrimaryCondition(negated);
            return new BoundBinaryExpression(inner, BoundBinaryOperatorKind.Not, inner, CobolCategory.Unknown);
        }

        var primary = ctx.primaryCondition();
        if (primary == null)
            return new BoundLiteralExpression(true, CobolCategory.Unknown);

        return BindPrimaryCondition(primary);
    }

    internal BoundExpression BindPrimaryCondition(CobolParserCore.PrimaryConditionContext primary)
    {
        // Comparison expression (relational, class condition, sign condition, bare identifier)
        // Sign condition was merged into comparisonExpression to eliminate ANTLR prediction ambiguity.
        if (primary.comparisonExpression() is { } comp)
            return BindComparison(comp);

        // Boolean literal: TRUE/FALSE
        if (primary.booleanLiteral() is { } boolLit)
        {
            bool value = boolLit.TRUE_() != null;
            return new BoundLiteralExpression(value, CobolCategory.Unknown);
        }

        // Parenthesized condition: (condition)
        if (primary.condition() is { } parenCond)
            return BindCondition(parenCond);

        return new BoundLiteralExpression(true, CobolCategory.Unknown);
    }

    /// <summary>
    /// Bind a sign condition from within comparisonExpression (after grammar merge).
    /// The sign condition alternative is: comparisonOperand IS? NOT? (POSITIVE | NEGATIVE | ZERO)
    /// </summary>
    internal BoundExpression BindSignConditionFromComparison(
        CobolParserCore.ComparisonExpressionContext ctx,
        CobolParserCore.ComparisonOperandContext operandCtx)
    {
        var subject = BindComparisonOperand(operandCtx);

        bool isNegated = ctx.NOT() != null;
        var kind = ctx.POSITIVE() != null ? SignConditionKind.Positive
            : ctx.NEGATIVE() != null ? SignConditionKind.Negative
            : SignConditionKind.Zero;

        // Check 12: Sign condition requires a numeric operand (ISO section 6.3.4.1)
        if (!subject.Category.IsNumericLike() && subject.Category != CobolCategory.Unknown)
        {
            var loc = new SourceLocation(_ctx.SourceName, 0, ctx.Start?.Line ?? 0, 0);
            var span = TextSpan.Empty;
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL2606, loc, span);
        }

        return new BoundSignConditionExpression(subject, kind, isNegated);
    }

    internal BoundExpression BindComparison(CobolParserCore.ComparisonExpressionContext ctx)
    {
        var operands = ctx.comparisonOperand();
        var relOp = ctx.comparisonOperator();
        var classNameCtx = ctx.className();

        // Sign condition (merged from signCondition): operand IS? NOT? POSITIVE/NEGATIVE/ZERO
        if ((ctx.POSITIVE() != null || ctx.NEGATIVE() != null || ctx.ZERO() != null)
            && classNameCtx == null && operands.Length >= 1)
        {
            return BindSignConditionFromComparison(ctx, operands[0]);
        }

        // Class condition: operand IS? NOT? className
        if (classNameCtx != null && operands.Length >= 1)
        {
            var subject = BindComparisonOperand(operands[0]);
            bool isNegated = ctx.NOT() != null;
            var classText = classNameCtx.GetText().ToUpperInvariant();
            ClassConditionKind? kind = classText switch
            {
                "NUMERIC" => ClassConditionKind.Numeric,
                "ALPHABETIC" => ClassConditionKind.Alphabetic,
                "ALPHABETIC-LOWER" => ClassConditionKind.AlphabeticLower,
                "ALPHABETIC-UPPER" => ClassConditionKind.AlphabeticUpper,
                _ => null
            };
            if (kind == null)
            {
                // Check for user-defined CLASS from SPECIAL-NAMES
                var classDef = _ctx.Semantic.ResolveClassDefinition(classText);
                if (classDef != null)
                    return new BoundUserClassConditionExpression(subject, classDef, isNegated);

                _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0413,
                    SourceLocation.None, TextSpan.Empty, classNameCtx.GetText());
                return new BoundLiteralExpression(false, CobolCategory.Unknown);
            }
            return new BoundClassConditionExpression(subject, kind.Value, isNegated);
        }

        if (operands.Length == 0)
            return new BoundLiteralExpression(true, CobolCategory.Unknown);

        var left = BindComparisonOperand(operands[0]);

        if (operands.Length < 2 || relOp == null)
        {
            // Check if bare identifier is a level-88 condition name or switch condition
            string? condNameStr = null;
            if (left is BoundIdentifierExpression idExpr)
                condNameStr = idExpr.Symbol.Name;
            else if (left is BoundLiteralExpression litExpr && litExpr.Value is string s)
                condNameStr = s;

            if (condNameStr != null)
            {
                var condSym = _ctx.Semantic.ResolveConditionName(condNameStr);
                if (condSym != null)
                {
                    // For subscripted condition names (e.g., EQUALS-M OF TABLE (13)),
                    // create a parent expression using the parent DATA item with the
                    // condition name's subscripts, so the lowering resolves to the
                    // correct subscripted element.
                    BoundExpression? parentExpr = null;
                    if (left is BoundIdentifierExpression condId && condId.IsSubscripted
                        && condSym.ParentDataItem != null)
                    {
                        parentExpr = new BoundIdentifierExpression(
                            condSym.ParentDataItem, condId.Category, condId.Subscripts);
                    }
                    return new BoundConditionNameExpression(condSym, parentExpression: parentExpr);
                }

                var swCond = _ctx.Semantic.ResolveSwitchCondition(condNameStr);
                if (swCond != null)
                    return new BoundSwitchConditionExpression(swCond.Value.Switch, swCond.Value.IsOn);
            }
            // Bare expression: IF A (means A <> 0 for numeric, A <> SPACE for alpha)
            return left;
        }

        var right = BindComparisonOperand(operands[1]);
        var op = ParseComparisonOperator(relOp);

        return new BoundBinaryExpression(left, op, right, CobolCategory.Unknown);
    }

    /// <summary>
    /// Parse a comparison operator context into a BoundBinaryOperatorKind.
    /// Shared by BindComparison and BindAbbreviatedRelation.
    /// </summary>
    internal static BoundBinaryOperatorKind ParseComparisonOperator(CobolParserCore.ComparisonOperatorContext relOp)
    {
        string opText = relOp.GetText().ToUpperInvariant()
            .Replace("IS", "").Replace("TO", "").Replace("THAN", "").Trim();
        return opText switch
        {
            "=" or "EQUAL" => BoundBinaryOperatorKind.Equal,
            "NOT=" or "NOTEQUAL" or "<>" => BoundBinaryOperatorKind.NotEqual,
            ">" or "GREATER" => BoundBinaryOperatorKind.Greater,
            ">=" or "GREATEROREQUAL" => BoundBinaryOperatorKind.GreaterOrEqual,
            "<" or "LESS" => BoundBinaryOperatorKind.Less,
            "<=" or "LESSOREQUAL" => BoundBinaryOperatorKind.LessOrEqual,
            "NOT>" => BoundBinaryOperatorKind.LessOrEqual,       // NOT > means <=
            "NOT<" => BoundBinaryOperatorKind.GreaterOrEqual,    // NOT < means >=
            "NOT>=" => BoundBinaryOperatorKind.Less,             // NOT >= means <
            "NOT<=" => BoundBinaryOperatorKind.Greater,          // NOT <= means >
            _ when opText.Contains("NOT") && opText.Contains("GREATER") => BoundBinaryOperatorKind.LessOrEqual,
            _ when opText.Contains("NOT") && opText.Contains("LESS") => BoundBinaryOperatorKind.GreaterOrEqual,
            _ when opText.Contains("NOT") && opText.Contains("EQUAL") => BoundBinaryOperatorKind.NotEqual,
            _ when opText.Contains("EQUAL") => BoundBinaryOperatorKind.Equal,
            _ when opText.Contains("GREATER") => BoundBinaryOperatorKind.Greater,
            _ when opText.Contains("LESS") => BoundBinaryOperatorKind.Less,
            _ => BoundBinaryOperatorKind.Equal
        };
    }

    /// <summary>
    /// Negate a relational operator: = -> !=, > -> <=, etc.
    /// Used for abbreviated NOT conditions (IF A > B AND NOT &lt; C).
    /// </summary>
    internal static BoundBinaryOperatorKind NegateOperator(BoundBinaryOperatorKind op) => op switch
    {
        BoundBinaryOperatorKind.Equal => BoundBinaryOperatorKind.NotEqual,
        BoundBinaryOperatorKind.NotEqual => BoundBinaryOperatorKind.Equal,
        BoundBinaryOperatorKind.Greater => BoundBinaryOperatorKind.LessOrEqual,
        BoundBinaryOperatorKind.GreaterOrEqual => BoundBinaryOperatorKind.Less,
        BoundBinaryOperatorKind.Less => BoundBinaryOperatorKind.GreaterOrEqual,
        BoundBinaryOperatorKind.LessOrEqual => BoundBinaryOperatorKind.Greater,
        _ => op
    };

    // ═══════════════════════════════════
    // Abbreviated relation rewriting
    // ═══════════════════════════════════
    // COBOL allows abbreviated relational conditions:
    //   IF A = B OR C         -> (A = B) OR (A = C)
    //   IF A < B AND C        -> (A < B) AND (A < C)
    //   IF A < B < C          -> (A < B) AND (B < C)    [chained, not abbreviated]
    //
    // After binding, abbreviated forms appear as:
    //   BoundBinaryExpression(Or/And, relational_expr, bare_operand)
    // where bare_operand is an identifier or literal with no relational operator.
    //
    // The rewrite propagates the left relation's operator and subject to the
    // bare operand, producing an explicit relational expression.

    /// <summary>
    /// Expand abbreviated combined relation conditions per COBOL-85 section 6.3.4.2.
    /// Walks the bound expression tree top-down, maintaining (subject, operator)
    /// context from the most recently encountered relation condition. Bare operands
    /// and BoundAbbreviatedExpression nodes are expanded into full relationals.
    /// Condition-names, class, sign, and switch conditions are left untouched --
    /// they are complete simple conditions that do not participate in abbreviation.
    /// </summary>
    internal static BoundExpression ExpandAbbreviatedConditions(BoundExpression root)
        => ExpandAbbrev(root, null, default);

    internal static BoundExpression ExpandAbbrev(
        BoundExpression expr,
        BoundExpression? subject,
        BoundBinaryOperatorKind op)
    {
        // Already-resolved simple conditions: never participate in abbreviation
        if (expr is BoundConditionNameExpression
                or BoundSwitchConditionExpression
                or BoundClassConditionExpression
                or BoundUserClassConditionExpression
                or BoundSignConditionExpression)
            return expr;

        // Grammar-level abbreviated (operator + right operand, no subject)
        if (expr is BoundAbbreviatedExpression abbrev)
        {
            if (subject == null) return expr;
            return new BoundBinaryExpression(subject, abbrev.OperatorKind, abbrev.Right, CobolCategory.Unknown);
        }

        // Bare operand (identifier/literal/arithmetic not resolved as condition-name):
        // expand using inherited context if available
        if (expr is BoundIdentifierExpression or BoundLiteralExpression)
        {
            if (subject != null && IsRelational(op))
                return new BoundBinaryExpression(subject, op, expr, CobolCategory.Unknown);
            return expr;
        }

        if (expr is not BoundBinaryExpression bin)
            return expr;

        // Arithmetic expression used as abbreviated operand (e.g., IF A = B OR C - 1)
        if (IsArithmeticOp(bin.OperatorKind))
        {
            if (subject != null && IsRelational(op))
                return new BoundBinaryExpression(subject, op, expr, CobolCategory.Unknown);
            return expr;
        }

        // AND/OR: propagate context through children
        if (bin.OperatorKind is BoundBinaryOperatorKind.Or
                             or BoundBinaryOperatorKind.And)
        {
            var left = ExpandAbbrev(bin.Left, subject, op);

            // Extract relational context from expanded left for use by right
            var (newSubject, newOp) = ExtractContext(left);
            newSubject ??= subject;
            if (!IsRelational(newOp)) newOp = op;

            var right = ExpandAbbrev(bin.Right, newSubject, newOp);

            if (ReferenceEquals(left, bin.Left) && ReferenceEquals(right, bin.Right))
                return bin;
            return new BoundBinaryExpression(left, bin.OperatorKind, right, CobolCategory.Unknown);
        }

        // NOT: expand inner expression with inherited context
        if (bin.OperatorKind == BoundBinaryOperatorKind.Not)
        {
            var inner = ExpandAbbrev(bin.Left, subject, op);
            if (ReferenceEquals(inner, bin.Left))
                return bin;
            return new BoundBinaryExpression(inner, BoundBinaryOperatorKind.Not, inner, CobolCategory.Unknown);
        }

        // Relational: already a complete comparison -- no expansion needed
        return bin;
    }

    /// <summary>
    /// Extract relational context (subject, operator) from an expanded expression.
    /// Used to carry subject/operator forward through AND/OR chains.
    /// </summary>
    internal static (BoundExpression? Subject, BoundBinaryOperatorKind Op) ExtractContext(
        BoundExpression expr)
    {
        if (expr is BoundBinaryExpression bin)
        {
            if (IsRelational(bin.OperatorKind))
                return (bin.Left, bin.OperatorKind);

            // Look through NOT to find the inner relation
            if (bin.OperatorKind == BoundBinaryOperatorKind.Not)
                return ExtractContext(bin.Left);

            // For AND/OR chains, the rightmost child carries the most recent context
            if (bin.OperatorKind is BoundBinaryOperatorKind.And
                                or BoundBinaryOperatorKind.Or)
                return ExtractContext(bin.Right);
        }
        return (null, default);
    }

    internal static bool IsRelational(BoundBinaryOperatorKind kind) =>
        kind is BoundBinaryOperatorKind.Equal
            or BoundBinaryOperatorKind.NotEqual
            or BoundBinaryOperatorKind.Less
            or BoundBinaryOperatorKind.LessOrEqual
            or BoundBinaryOperatorKind.Greater
            or BoundBinaryOperatorKind.GreaterOrEqual;

    internal static bool IsArithmeticOp(BoundBinaryOperatorKind kind) =>
        kind is BoundBinaryOperatorKind.Add
            or BoundBinaryOperatorKind.Subtract
            or BoundBinaryOperatorKind.Multiply
            or BoundBinaryOperatorKind.Divide
            or BoundBinaryOperatorKind.Power;

    internal BoundExpression BindComparisonOperand(CobolParserCore.ComparisonOperandContext ctx)
    {
        var vo = ctx.valueOperand();
        if (vo.nonNumericLiteral() is { } nonNumCtx)
            return _ctx.Expression.BindNonNumericLiteral(nonNumCtx);
        return _ctx.Expression.BindAdditiveExpression(vo.arithmeticExpression().additiveExpression());
    }
}
