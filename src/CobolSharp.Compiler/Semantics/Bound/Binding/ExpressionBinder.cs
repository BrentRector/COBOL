// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Expression binding: arithmetic expressions, literals, figurative constants,
/// function calls, data references with subscripts/ref-mod, qualified names.
/// </summary>
internal sealed class ExpressionBinder
{
    private readonly BindingContext _ctx;

    internal ExpressionBinder(BindingContext ctx) => _ctx = ctx;

    // ── Arithmetic expression tree walk ──

    /// <summary>
    /// Recursively bind an arithmetic expression tree for COMPUTE.
    /// Walks the parse tree: additiveExpression → multiplicativeExpression →
    /// powerExpression → unaryExpression → primaryExpression.
    /// </summary>
    internal BoundExpression BindAdditiveExpression(CobolParserCore.AdditiveExpressionContext ctx)
    {
        var terms = ctx.multiplicativeExpression();
        var ops = ctx.addOp();

        var left = BindMultiplicativeExpression(terms[0]);
        for (int i = 0; i < ops.Length; i++)
        {
            var right = BindMultiplicativeExpression(terms[i + 1]);
            var opKind = ops[i].GetText() == "+"
                ? BoundBinaryOperatorKind.Add
                : BoundBinaryOperatorKind.Subtract;
            left = new BoundBinaryExpression(left, opKind, right, CobolCategory.Numeric);
        }
        return left;
    }

    internal BoundExpression BindMultiplicativeExpression(CobolParserCore.MultiplicativeExpressionContext ctx)
    {
        var factors = ctx.powerExpression();
        var ops = ctx.mulOp();

        var left = BindPowerExpression(factors[0]);
        for (int i = 0; i < ops.Length; i++)
        {
            var right = BindPowerExpression(factors[i + 1]);
            var opKind = ops[i].GetText() == "*"
                ? BoundBinaryOperatorKind.Multiply
                : BoundBinaryOperatorKind.Divide;
            left = new BoundBinaryExpression(left, opKind, right, CobolCategory.Numeric);
        }
        return left;
    }

    internal BoundExpression BindPowerExpression(CobolParserCore.PowerExpressionContext ctx)
    {
        var unaries = ctx.unaryExpression();
        var left = BindUnaryExpression(unaries[0]);
        if (unaries.Length > 1)
        {
            // a ** b
            var right = BindUnaryExpression(unaries[1]);
            // Power is not a standard BoundBinaryOperatorKind; use Multiply as placeholder
            // and handle at emit time. For now, use a dedicated representation.
            // Simple approach: emit as Math.Pow at runtime
            left = new BoundBinaryExpression(left,
                BoundBinaryOperatorKind.Power,
                right, CobolCategory.Numeric);
        }
        return left;
    }

    internal BoundExpression BindUnaryExpression(CobolParserCore.UnaryExpressionContext ctx)
    {
        var addOp = ctx.addOp();
        if (addOp != null)
        {
            var inner = BindUnaryExpression(ctx.unaryExpression());
            if (addOp.GetText() == "-")
            {
                // Negate: 0 - inner
                return new BoundBinaryExpression(
                    new BoundLiteralExpression(0m, CobolCategory.Numeric),
                    BoundBinaryOperatorKind.Subtract,
                    inner, CobolCategory.Numeric);
            }
            return inner; // unary + is identity
        }
        return BindPrimaryExpression(ctx.primaryExpression());
    }

    internal BoundExpression BindPrimaryExpression(CobolParserCore.PrimaryExpressionContext ctx)
    {
        if (ctx.numericLiteral() != null)
            return BindNumericLiteral(ctx.numericLiteral());

        // ZERO_ARITH: figurative ZERO rewritten by token rewriter in arithmetic context
        if (ctx.ZERO_ARITH() != null)
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);

        if (ctx.dataReference() != null)
        {
            return BindDataReferenceWithSubscripts(ctx.dataReference());
        }

        if (ctx.arithmeticExpression() != null)
            return BindAdditiveExpression(ctx.arithmeticExpression().additiveExpression());

        // Intrinsic function call (1989 Amendment)
        if (ctx.functionCall() != null)
        {
            return BindFunctionCall(ctx.functionCall());
        }

        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    // ── FUNCTION CALL ──

    internal BoundExpression BindFunctionCall(CobolParserCore.FunctionCallContext ctx)
    {
        // FUNCTION functionName (FNARG_LPAREN functionArgList? FNARG_RPAREN)? — the function name comes from the
        // functionName rule (IDENTIFIER or a reserved-word alternative like SIGN/SUM/RANDOM).
        // P7 Step 12 reshaped the SHARED grammar: arguments now parse as DEFAULT-mode functionArgument
        // trees (one per argument — the grammar already did the comma/space separator split the old
        // SUBSCRIPT-mode capture left to InterpretSubscriptTokens). This frozen oracle keeps its proven
        // SUB-token segment binder by down-converting each argument subtree's leaf tokens to their
        // SUBSCRIPT-mode twins (MapFunctionArgTokens) — behavior-identical, guarded by the full NIST net.
        var funcName = ctx.functionName()?.GetText() ?? "UNKNOWN";

        var args = new List<BoundExpression>();
        if (ctx.functionArgList() is { } argList)
            foreach (var argCtx in argList.functionArgument())
            {
                var toks = new List<IToken>();
                CollectLeafTokens(argCtx, toks);
                args.Add(BindSubscriptSegment(MapFunctionArgTokens(toks)));
            }

        // Expand the ALL subscript (ISO §15.4): FUNCTION f(table(ALL)) passes every occurrence
        // of the table as a separate argument. Replace each table(ALL) reference in-place with
        // one element reference per occurrence (table(1), table(2), …, table(n)).
        if (args.Any(IsAllSubscriptedRef))
        {
            var expanded = new List<BoundExpression>(args.Count);
            foreach (var a in args)
                expanded.AddRange(ExpandAllSubscript(a, funcName));
            args = expanded;
        }

        // FUNCTION LENGTH returns the number of character positions in argument-1 (ISO §15.24).
        // For a fixed operand this is the defined size, folded at bind time; for a
        // reference-modified operand the length may be runtime (x(s:l) → l), so BindLength
        // returns a runtime expression in that case.
        if (funcName.Equals("LENGTH", StringComparison.OrdinalIgnoreCase) && args.Count == 1)
            return BindLength(args[0]);

        var category = BindingContext.AlphanumericFunctions.Contains(funcName)
            ? CobolCategory.Alphanumeric
            : CobolCategory.Numeric;

        // MAX and MIN are category-polymorphic (ISO §15.x): their result category follows the
        // arguments. With all-alphanumeric arguments they return the selected string, so the
        // result must be treated as alphanumeric (otherwise the string result is unboxed to
        // decimal at the call site). ORD-MAX/ORD-MIN always return a numeric ordinal.
        if ((funcName.Equals("MAX", StringComparison.OrdinalIgnoreCase)
                || funcName.Equals("MIN", StringComparison.OrdinalIgnoreCase))
            && args.Count > 0 && args.All(a => !a.Category.IsNumericLike()))
        {
            category = CobolCategory.Alphanumeric;
        }

        return new BoundFunctionCallExpression(funcName, args.AsReadOnly(), category);
    }

    /// <summary>
    /// Compute FUNCTION LENGTH at bind time (ISO §15.24: number of character positions in
    /// argument-1). The length-preserving string functions (REVERSE/UPPER-CASE/LOWER-CASE)
    /// report the length of their own single argument, so LENGTH(REVERSE(x)) == LENGTH(x) and
    /// nested forms recurse. A numeric literal that survived from a folded nested function is
    /// already its own value.
    /// </summary>
    /// <summary>
    /// Bind FUNCTION LENGTH(arg). A reference-modified operand x(s:l) has length l — which may be
    /// a runtime expression — and x(s:) (rest of field) has length (defined-size − s + 1); both
    /// are returned as expressions. Every other operand has a compile-time-constant length folded
    /// by <see cref="StaticLength"/>.
    /// </summary>
    private BoundExpression BindLength(BoundExpression arg)
    {
        if (arg is BoundReferenceModificationExpression refMod)
        {
            // Explicit length: x(start:length) → length (literal or runtime).
            if (refMod.Length != null)
                return refMod.Length;

            // Rest of field: defined-size − (start − 1).
            decimal baseLen = refMod.Base.Symbol.ElementSize;
            var startMinus1 = new BoundBinaryExpression(
                refMod.Start, BoundBinaryOperatorKind.Subtract,
                new BoundLiteralExpression(1m, CobolCategory.Numeric), CobolCategory.Numeric);
            return new BoundBinaryExpression(
                new BoundLiteralExpression(baseLen, CobolCategory.Numeric),
                BoundBinaryOperatorKind.Subtract, startMinus1, CobolCategory.Numeric);
        }

        // A group (or table) with a subordinate OCCURS … DEPENDING ON item is variable-length:
        // ISO §15.50.4 rule 4(b)/rule 7 — its length uses the CURRENT depending-on value(s), not
        // the maximum (compile-time) layout. Emit a runtime length expression in that case.
        if (arg is BoundIdentifierExpression idExpr
            && BuildVariableLengthExpression(idExpr) is { } variableLength)
            return variableLength;

        return new BoundLiteralExpression(StaticLength(arg), CobolCategory.Numeric);
    }

    private static decimal StaticLength(BoundExpression arg) => arg switch
    {
        BoundIdentifierExpression idExpr => idExpr.Symbol.ElementSize,
        BoundLiteralExpression { Value: string s } => s.Length,
        BoundLiteralExpression { Value: decimal d } => d,
        BoundFunctionCallExpression fn when fn.Arguments.Count == 1
            && (fn.FunctionName.Equals("REVERSE", StringComparison.OrdinalIgnoreCase)
                || fn.FunctionName.Equals("UPPER-CASE", StringComparison.OrdinalIgnoreCase)
                || fn.FunctionName.Equals("LOWER-CASE", StringComparison.OrdinalIgnoreCase))
            => StaticLength(fn.Arguments[0]),
        _ => 0
    };

    /// <summary>
    /// FUNCTION LENGTH of a variable-length group (ISO §15.50.4 rule 4(b), rule 7): when one or
    /// more subordinate items carry OCCURS … DEPENDING ON, the length is computed at runtime from
    /// the current depending-on value(s), not the maximum (compile-time) layout. The value is the
    /// maximum layout reduced by each variable table's unused tail —
    /// <c>maxLength − Σ (maxOccurs − dependingValue) × repetition × elementSize</c> — where
    /// <c>repetition</c> is the product of the (fixed) OCCURS counts of any tables enclosing the
    /// variable table within the argument. Returns null when no subordinate ODO table exists (the
    /// length is then a compile-time constant folded by <see cref="StaticLength"/>).
    /// </summary>
    private static BoundExpression? BuildVariableLengthExpression(BoundIdentifierExpression idExpr)
    {
        var tables = new List<(DataSymbol Table, int Repetition)>();
        CollectDependingTables(idExpr.Symbol, repetition: 1, tables);
        if (tables.Count == 0)
            return null;

        BoundExpression length = new BoundLiteralExpression(
            (decimal)idExpr.Symbol.ElementSize, CobolCategory.Numeric);

        foreach (var (table, repetition) in tables)
        {
            var dependingOn = table.Occurs!.DependingOnSymbol;
            if (dependingOn is null)
                return null; // unresolved DEPENDING ON — fall back to the constant fold

            var depCategory = dependingOn.ResolvedType?.Category ?? CobolCategory.Numeric;
            // unused occurrences = maxOccurs − currentOccurrences
            var unusedOccurrences = new BoundBinaryExpression(
                new BoundLiteralExpression((decimal)table.Occurs.MaxOccurs, CobolCategory.Numeric),
                BoundBinaryOperatorKind.Subtract,
                new BoundIdentifierExpression(dependingOn, depCategory),
                CobolCategory.Numeric);
            // unused bytes = unused occurrences × (repetition × elementSize)
            var unusedBytes = new BoundBinaryExpression(
                unusedOccurrences, BoundBinaryOperatorKind.Multiply,
                new BoundLiteralExpression((decimal)(repetition * table.ElementSize), CobolCategory.Numeric),
                CobolCategory.Numeric);
            length = new BoundBinaryExpression(
                length, BoundBinaryOperatorKind.Subtract, unusedBytes, CobolCategory.Numeric);
        }

        return length;
    }

    /// <summary>
    /// Walk the subordinate items of <paramref name="node"/>, collecting every table described with
    /// OCCURS … DEPENDING ON together with its repetition factor — the product of the maximum OCCURS
    /// counts of the tables enclosing it within the LENGTH argument. RENAMES (66) and condition-name
    /// (88) entries are aliases, not storage, and are skipped. (Complex ODO — a DEPENDING ON table
    /// nested inside another DEPENDING ON table, COBOL-2002+ — uses the inner table's maximum
    /// repetition rather than the outer current count; this is approximate but rare.)
    /// </summary>
    private static void CollectDependingTables(
        DataSymbol node, int repetition, List<(DataSymbol, int)> acc)
    {
        foreach (var child in node.Children)
        {
            if (child.LevelNumber is 66 or 88)
                continue;

            int childRepetition = repetition;
            if (child.Occurs is { } occurs)
            {
                if (occurs.DependingOnName != null)
                    acc.Add((child, repetition));
                childRepetition = repetition * occurs.MaxOccurs;
            }

            CollectDependingTables(child, childRepetition, acc);
        }
    }

    /// <summary>True if the expression is a table reference whose subscript is the ALL keyword.</summary>
    private static bool IsAllSubscriptedRef(BoundExpression e) =>
        e is BoundIdentifierExpression { Subscripts: { } subs }
        && subs.Any(s => s is BoundLiteralExpression { Value: string sv }
            && string.Equals(sv, "ALL", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Expand a table(ALL) reference (ISO §15.4) into one reference per occurrence. ALL may appear
    /// in any/all subscript positions of a multi-dimensional table, so the expansion is the
    /// cartesian product over every ALL position: each position's range comes from the OCCURS
    /// bound of the table level corresponding to that subscript position (outermost subscript ↔
    /// outermost OCCURS). Fixed subscripts are kept; the references are produced in row-major
    /// (leftmost-varies-slowest) order.
    /// </summary>
    private static IEnumerable<BoundExpression> ExpandAllSubscript(BoundExpression e, string funcName)
    {
        if (e is not BoundIdentifierExpression id || id.Subscripts is null)
            return new[] { e };

        // OCCURS info per nesting level, outermost-first to align with subscript order.
        var levels = new List<OccursInfo>();
        for (var sym = id.Symbol; sym != null; sym = sym.Parent)
            if (sym.Occurs != null) levels.Add(sym.Occurs);
        levels.Reverse();

        // FUNCTION SUM over a table(ALL) whose OCCURS is DEPENDING ON ranges over the CURRENT depending value,
        // not the maximum (ISO §15.x / §8.4.2.3.4). We still expand statically to the maximum, but multiply
        // each occurrence beyond the level minimum by a runtime 1/0 active-mask so inactive tail slots add 0.
        // SUM-only: a 0-mask is correct for an additive reduction but would corrupt count-sensitive aggregates
        // (MEAN/MEDIAN/…); those keep the max-expansion for now (general runtime-range fix is a follow-up).
        bool maskOdo = funcName.Equals("SUM", StringComparison.OrdinalIgnoreCase);

        // Start with the original subscripts; expand each ALL position by its OCCURS maximum.
        var combos = new List<List<BoundExpression>> { new(id.Subscripts) };
        for (int pos = 0; pos < id.Subscripts.Count; pos++)
        {
            if (id.Subscripts[pos] is not BoundLiteralExpression { Value: string sv }
                || !string.Equals(sv, "ALL", StringComparison.OrdinalIgnoreCase))
                continue;

            int count = pos < levels.Count ? levels[pos].MaxOccurs : id.Symbol.Occurs?.MaxOccurs ?? 0;
            if (count <= 0) return new[] { e };

            var expanded = new List<List<BoundExpression>>(combos.Count * count);
            foreach (var combo in combos)
                for (int idx = 1; idx <= count; idx++)
                {
                    var next = new List<BoundExpression>(combo)
                    {
                        [pos] = new BoundLiteralExpression((decimal)idx, CobolCategory.Numeric)
                    };
                    expanded.Add(next);
                }
            combos = expanded;
        }

        return combos.Select(subs =>
        {
            BoundExpression elem = new BoundIdentifierExpression(id.Symbol, id.Category, subs);
            return maskOdo ? ApplyOdoActiveMask(elem, subs, id.Subscripts, levels, id.Symbol) : elem;
        });
    }

    /// <summary>
    /// For a SUM over a table(ALL), multiply the element reference by a runtime 1/0 active-mask for each ALL
    /// position whose OCCURS level is DEPENDING ON and whose chosen occurrence exceeds the level minimum:
    /// mask = MAX(0, MIN(1, depending - (idx-1))) — 1 when depending >= idx, else 0. So a statically
    /// max-expanded sum counts only the currently-active occurrences (ISO §15.x). Levels without a DEPENDING
    /// ON object, and occurrences at-or-below the minimum, are left unmasked (always active).
    /// </summary>
    private static BoundExpression ApplyOdoActiveMask(
        BoundExpression elem, List<BoundExpression> subs, IReadOnlyList<BoundExpression> originalSubs,
        List<OccursInfo> levels, DataSymbol symbol)
    {
        for (int pos = 0; pos < originalSubs.Count; pos++)
        {
            // Only positions that were ALL in the original reference can be masked.
            if (originalSubs[pos] is not BoundLiteralExpression { Value: string sv }
                || !string.Equals(sv, "ALL", StringComparison.OrdinalIgnoreCase))
                continue;
            var lvl = pos < levels.Count ? levels[pos] : symbol.Occurs;
            if (lvl?.DependingOnSymbol == null) continue;
            if (subs[pos] is not BoundLiteralExpression { Value: decimal idxDec }) continue;
            int idx = (int)idxDec;
            if (idx <= lvl.MinOccurs) continue; // always active — no guard needed

            // mask = MAX(0, MIN(1, depending - (idx-1)))
            BoundExpression dep = new BoundIdentifierExpression(lvl.DependingOnSymbol, CobolCategory.Numeric);
            BoundExpression diff = new BoundBinaryExpression(dep, BoundBinaryOperatorKind.Subtract,
                new BoundLiteralExpression((decimal)(idx - 1), CobolCategory.Numeric), CobolCategory.Numeric);
            BoundExpression minOne = new BoundFunctionCallExpression("MIN",
                new BoundExpression[] { new BoundLiteralExpression(1m, CobolCategory.Numeric), diff },
                CobolCategory.Numeric);
            BoundExpression mask = new BoundFunctionCallExpression("MAX",
                new BoundExpression[] { new BoundLiteralExpression(0m, CobolCategory.Numeric), minOne },
                CobolCategory.Numeric);
            elem = new BoundBinaryExpression(elem, BoundBinaryOperatorKind.Multiply, mask, elem.Category);
        }
        return elem;
    }

    // ── LITERALS ──

    internal BoundExpression BindLiteral(CobolParserCore.LiteralContext lit)
    {
        // literal: numericLiteral | nonNumericLiteral
        var numLit = lit.numericLiteral();
        if (numLit != null)
            return BindNumericLiteral(numLit);

        var nonNumLit = lit.nonNumericLiteral();
        if (nonNumLit != null)
            return BindNonNumericLiteral(nonNumLit);

        // Fallback
        return new BoundLiteralExpression(lit.GetText(), CobolCategory.Alphanumeric);
    }

    internal BoundExpression BindNumericLiteral(CobolParserCore.NumericLiteralContext numLit)
    {
        var normalized = SemanticBuilder.NormalizeNumericLiteralText(numLit);
        var originalText = numLit.GetText();
        if (decimal.TryParse(normalized, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return new BoundLiteralExpression(val, CobolCategory.Numeric, originalText: originalText);
        return new BoundLiteralExpression(originalText, CobolCategory.Alphanumeric);
    }

    internal BoundExpression BindNonNumericLiteral(CobolParserCore.NonNumericLiteralContext nonNum)
    {
        var s = nonNum.STRINGLIT();
        if (s != null)
        {
            var text = s.GetText();
            if (text.Length >= 2 &&
                ((text[0] == '"' && text[^1] == '"') ||
                 (text[0] == '\'' && text[^1] == '\'')))
            {
                char quoteChar = text[0];
                text = text[1..^1];
                // Un-escape doubled quotes: "" → " (ISO §8.3.3.2.3 r3)
                text = text.Replace(new string(quoteChar, 2), new string(quoteChar, 1));
            }
            return new BoundLiteralExpression(text, CobolCategory.Alphanumeric);
        }

        // National literal N"…" / N'…' (ISO §8.3.3.5). Strip the N prefix and the surrounding quotes,
        // un-escape doubled quotes, and carry category National so MOVE/DISPLAY use UTF-16 semantics.
        var nat = nonNum.NATLIT();
        if (nat != null)
        {
            var text = nat.GetText();
            if (text.Length >= 3 && (text[0] is 'N' or 'n') &&
                ((text[1] == '"' && text[^1] == '"') ||
                 (text[1] == '\'' && text[^1] == '\'')))
            {
                char quoteChar = text[1];
                text = text[2..^1];
                text = text.Replace(new string(quoteChar, 2), new string(quoteChar, 1));
            }
            return new BoundLiteralExpression(text, CobolCategory.National);
        }

        // Boolean literal B"0101" / B'0101' (ISO §8.3.3.4). Strip the B prefix and quotes; the content is
        // binary digits only (no doubled-quote escaping). Carry category Boolean so MOVE/compare use it.
        var boolLit = nonNum.BOOLLIT();
        if (boolLit != null)
        {
            var text = boolLit.GetText();
            if (text.Length >= 3 && (text[0] is 'B' or 'b') &&
                ((text[1] == '"' && text[^1] == '"') ||
                 (text[1] == '\'' && text[^1] == '\'')))
            {
                text = text[2..^1];
            }
            return new BoundLiteralExpression(text, CobolCategory.Boolean);
        }

        var figCtx = nonNum.figurativeConstant();
        if (figCtx != null)
            return BindFigurativeConstantExpression(figCtx);

        // HEXLIT, etc.
        return new BoundLiteralExpression(nonNum.GetText(), CobolCategory.Alphanumeric);
    }

    internal BoundExpression BindFigurativeConstantExpression(CobolParserCore.FigurativeConstantContext figCtx)
    {
        if (figCtx.ALL() != null)
        {
            // ALL STRINGLIT / ALL HEXLIT: repeating literal pattern
            var allStr = figCtx.allLiteral()?.STRINGLIT();
            if (allStr != null)
            {
                var raw = allStr.GetText();
                string allText = raw.Length >= 2 ? raw[1..^1] : "";
                return new BoundFigurativeExpression(FigurativeKind.None, allText);
            }
            var allHex = figCtx.allLiteral()?.HEXLIT();
            if (allHex != null)
            {
                var raw = allHex.GetText();
                if (raw.Length >= 3)
                {
                    var hexBody = raw[2..^1];
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i + 1 < hexBody.Length; i += 2)
                        sb.Append((char)Convert.ToByte(hexBody[i..(i + 2)], 16));
                    return new BoundFigurativeExpression(FigurativeKind.None, sb.ToString());
                }
            }

            // ALL ZERO / ALL SPACE / ALL HIGH-VALUE / ALL LOW-VALUE / ALL QUOTE
            // Per COBOL-85 §4.3.3, ALL applied to a figurative constant is
            // semantically identical to the figurative constant alone.
            if (figCtx.zeroWord() != null) return new BoundFigurativeExpression(FigurativeKind.Zero);
            if (figCtx.spaceWord() != null) return new BoundFigurativeExpression(FigurativeKind.Space);
            if (figCtx.highValueWord() != null) return new BoundFigurativeExpression(FigurativeKind.HighValue);
            if (figCtx.lowValueWord() != null) return new BoundFigurativeExpression(FigurativeKind.LowValue);
            if (figCtx.quoteWord() != null) return new BoundFigurativeExpression(FigurativeKind.Quote);

            // Fallback: should not reach here with valid grammar
            return new BoundFigurativeExpression(FigurativeKind.None, "");
        }

        string figText = figCtx.GetText().ToUpperInvariant();
        return figText switch
        {
            "SPACE" or "SPACES" => new BoundFigurativeExpression(FigurativeKind.Space),
            "ZERO" or "ZEROS" or "ZEROES" => new BoundFigurativeExpression(FigurativeKind.Zero),
            "HIGH-VALUE" or "HIGH-VALUES" => new BoundFigurativeExpression(FigurativeKind.HighValue),
            "LOW-VALUE" or "LOW-VALUES" => new BoundFigurativeExpression(FigurativeKind.LowValue),
            "QUOTE" or "QUOTES" => new BoundFigurativeExpression(FigurativeKind.Quote),
            // NULL/NULLS — the predefined null address (ISO §8.4.3.10); the all-zero pointer handle.
            "NULL" or "NULLS" => new BoundFigurativeExpression(FigurativeKind.Null),
            _ => new BoundLiteralExpression(figText, CobolCategory.Alphanumeric)
        };
    }

    // ── OPERAND BINDING ──

    /// <summary>
    /// Bind a givingReceiver (identifier | literal) — unified GIVING-form operand.
    /// </summary>
    internal BoundExpression BindReceivingOperand(CobolParserCore.ReceivingOperandContext ctx)
    {
        if (ctx.dataReference() != null)
            return BindDataReferenceWithSubscripts(ctx.dataReference());
        if (ctx.literal() != null)
            return BindLiteral(ctx.literal());
        throw new InvalidOperationException("givingReceiver has neither identifier nor literal");
    }

    /// <summary>
    /// Bind a simple operand (identifier or literal) from ADD/SUBTRACT/MULTIPLY/DIVIDE.
    /// These statements accept only simple operands, not full expressions.
    /// </summary>
    internal BoundExpression BindSimpleOperand(ParserRuleContext ctx)
    {
        // The rule is: identifier | literal
        // Check for identifier child first
        if (ctx is CobolParserCore.AddOperandContext addOp)
        {
            if (addOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(addOp.dataReference());
            if (addOp.literal() != null)
                return BindLiteral(addOp.literal());
        }
        else if (ctx is CobolParserCore.SubtractOperandContext subOp)
        {
            if (subOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(subOp.dataReference());
            if (subOp.literal() != null)
                return BindLiteral(subOp.literal());
        }
        else if (ctx is CobolParserCore.MultiplyOperandContext mulOp)
        {
            if (mulOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(mulOp.dataReference());
            if (mulOp.literal() != null)
                return BindLiteral(mulOp.literal());
        }
        else if (ctx is CobolParserCore.DivideOperandContext divOp)
        {
            if (divOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(divOp.dataReference());
            if (divOp.literal() != null)
                return BindLiteral(divOp.literal());
        }

        // Fallback: try to parse the text
        string text = ctx.GetText();
        return BindDataReferenceOrLiteral(text);
    }

    internal BoundExpression BindDataReferenceOrLiteral(string text)
    {
        if (decimal.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return new BoundLiteralExpression(val, CobolCategory.Numeric, originalText: text);

        var sym = _ctx.Semantic.ResolveData(text);
        if (sym != null)
            return new BoundIdentifierExpression(sym, CobolCategory.Alphanumeric);

        _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0110,
            SourceLocation.None, TextSpan.Empty,
            $"Unresolved identifier '{text}'");
        return new BoundLiteralExpression(text, CobolCategory.Alphanumeric);
    }

    internal BoundExpression BindArithmeticExpr(CobolParserCore.ArithmeticExpressionContext? ctx)
        => ctx != null ? BindAdditiveExpression(ctx.additiveExpression()) : new BoundLiteralExpression(0m, CobolCategory.Numeric);

    // ── DATA REFERENCE WITH SUBSCRIPTS ──

    /// Bind a data reference: IDENTIFIER with optional qualification (OF/IN),
    /// subscripts, and reference modification.
    /// Qualified names are resolved right-to-left: A OF B OF C → resolve C, then B in C, then A in B.
    /// </summary>
    internal BoundExpression BindDataReferenceWithSubscripts(CobolParserCore.DataReferenceContext idCtx)
    {
        // LINAGE-COUNTER special register (ISO §8.4.3.14): not a data name, so it has no cobolWord
        // base — resolve it to the (optionally file-qualified) LINAGE file's runtime counter.
        if (idCtx.LINAGE_COUNTER() != null)
        {
            var qual = idCtx.cobolWord(); // present only as the OF/IN file-name qualifier
            var linageFile = qual != null
                ? _ctx.Semantic.ResolveFile(qual.GetText())
                : _ctx.Semantic.FindLinageFile();
            if (linageFile != null)
                return new BoundLinageCounterExpression(linageFile);
            // No LINAGE file in scope — bind to 0 so analysis continues (no valid LINAGE-COUNTER).
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);
        }

        // LINE-COUNTER / PAGE-COUNTER special registers (ISO §8.4.3.15): the Report Writer's per-report line
        // and page counters, optionally qualified by report-name. Like LINAGE-COUNTER they have no cobolWord
        // base, so they are resolved here before generic data-name resolution dereferences a null cobolWord.
        if (idCtx.LINE_COUNTER() != null)
        {
            var qual = idCtx.cobolWord();
            var report = qual != null ? _ctx.Semantic.ResolveReport(qual.GetText()) : _ctx.Semantic.FindFirstReport();
            if (report != null) return new BoundLineCounterExpression(report);
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);
        }
        if (idCtx.PAGE_COUNTER() != null)
        {
            var qual = idCtx.cobolWord();
            var report = qual != null ? _ctx.Semantic.ResolveReport(qual.GetText()) : _ctx.Semantic.FindFirstReport();
            if (report != null) return new BoundPageCounterExpression(report);
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);
        }

        string name = idCtx.cobolWord().GetText();
        var tails = idCtx.dataReferenceSuffix();

        // Extract qualifications, subscripts, and refmod from dataNameTail*
        var qualifiers = new List<string>();
        CobolParserCore.SubscriptOrRefModContext? subOrRefMod = null;
        CobolParserCore.RefModSpecContext? refModCtx = null;

        foreach (var tail in tails)
        {
            if (tail.qualification() != null)
            {
                var qual = tail.qualification();
                qualifiers.Add(qual.cobolWord().GetText());
                // Extract subscripts/refmods attached to the qualifier (e.g., AX-2 IN AX(I))
                var qualSubs = qual.subscriptPart();
                if (qualSubs.Length > 0 && subOrRefMod == null)
                    subOrRefMod = qualSubs[0].subscriptOrRefMod();
                var qualRefMods = qual.refModPart();
                if (qualRefMods.Length > 0 && refModCtx == null)
                    refModCtx = qualRefMods[0].refModSpec();
            }
            else if (tail.subscriptPart() != null && subOrRefMod == null)
            {
                subOrRefMod = tail.subscriptPart().subscriptOrRefMod();
            }
            else if (tail.refModPart() != null && refModCtx == null)
            {
                refModCtx = tail.refModPart().refModSpec();
            }
        }

        // Resolve the data symbol — qualified or unqualified
        DataSymbol? sym;
        if (qualifiers.Count > 0)
        {
            // Right-to-left narrowing: resolve outermost qualifier first,
            // then walk inward to the leftmost identifier.
            sym = ResolveQualifiedName(name, qualifiers);
        }
        else
        {
            sym = _ctx.Semantic.ResolveData(name);
        }

        if (sym == null)
        {
            // Check for SYMBOLIC CHARACTER from SPECIAL-NAMES
            var symChar = _ctx.Semantic.ResolveSymbolicCharacter(name);
            if (symChar.HasValue)
            {
                // Symbolic character: produce a 1-byte string literal
                string charValue = ((char)symChar.Value).ToString();
                return _ctx.Typed(new BoundLiteralExpression(charValue, CobolCategory.Alphanumeric));
            }

            // Check if this is a condition name (level 88) — possibly qualified and/or subscripted
            var condSym = qualifiers.Count > 0
                ? _ctx.Semantic.ResolveQualifiedConditionName(name, qualifiers)
                : _ctx.Semantic.ResolveConditionName(name);
            if (condSym != null)
            {
                BoundExpression? parentExpr = null;
                if (condSym.ParentDataItem != null && subOrRefMod != null)
                {
                    var (condSubExprs, condIsRefMod) = InterpretSubscriptTokens(subOrRefMod);
                    if (!condIsRefMod && condSubExprs.Count > 0)
                    {
                        var parentCat = condSym.ParentDataItem.ResolvedType?.Category
                                        ?? CobolCategory.Alphanumeric;
                        parentExpr = new BoundIdentifierExpression(
                            condSym.ParentDataItem, parentCat, condSubExprs);
                    }
                }
                return new BoundConditionNameExpression(condSym, parentExpression: parentExpr);
            }

            return new BoundLiteralExpression(name, CobolCategory.Alphanumeric);
        }

        var cat = sym.ResolvedType?.Category ?? CobolCategory.Alphanumeric;

        if (subOrRefMod == null)
        {
            var plainId = new BoundIdentifierExpression(sym, cat);
            _ctx.Typed(plainId);
            if (refModCtx != null)
                return _ctx.Typed(BindReferenceModification(plainId, refModCtx));
            return plainId;
        }

        // Interpret the flat SUBSCRIPT-mode token sequence
        var (subExprs, isRefMod) = InterpretSubscriptTokens(subOrRefMod);

        if (isRefMod)
        {
            var startExpr = subExprs.Count > 0 ? subExprs[0] : new BoundLiteralExpression(1m, CobolCategory.Numeric);
            BoundExpression? lengthExpr = subExprs.Count > 1 ? subExprs[1] : null;
            var refModBase = new BoundIdentifierExpression(sym, cat);
            _ctx.Typed(refModBase);
            return _ctx.Typed(new BoundReferenceModificationExpression(refModBase, startExpr, lengthExpr));
        }

        var subs = subExprs;

        // ── Subscript validation (COBOL-85 semantic rules) ──

        int occursDepth = 0;
        var current = sym;
        while (current != null)
        {
            if (current.Occurs != null)
                occursDepth++;
            current = current.Parent;
        }

        int subscriptCount = subs.Count;
        int line = idCtx.Start?.Line ?? 0;
        var loc = new SourceLocation(_ctx.SourceName, 0, line, 0);
        var span = TextSpan.Empty;

        if (subscriptCount > 0 && occursDepth == 0)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0405, loc, span, sym.Name);

        if (subscriptCount > occursDepth && occursDepth > 0)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0406, loc, span, sym.Name, occursDepth, subscriptCount);

        // COBOL-85 standard specifies 3 OCCURS levels; we support up to 7 (NIST suite exercises 7).
        // Emit a warning (not error) beyond 3 levels to note departure from strict COBOL-85.
        if (occursDepth > 7)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0407, loc, span, sym.Name, occursDepth);

        if (subscriptCount > 7)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0408, loc, span, subscriptCount);

        if (sym.IsElementary && occursDepth > 0 && subscriptCount > 0 && subscriptCount < occursDepth)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0409, loc, span, sym.Name, occursDepth, subscriptCount);

        var baseId = new BoundIdentifierExpression(sym, cat, subs);
        _ctx.Typed(baseId);

        if (refModCtx != null)
            return _ctx.Typed(BindReferenceModification(baseId, refModCtx));

        return baseId;
    }

    // ── SUBSCRIPT TOKEN INTERPRETATION ──

    /// <summary>
    /// Interpret the flat SUBSCRIPT-mode token sequence into expressions.
    /// Returns (expressions, isRefMod). If SUB_COLON is present, it's ref-mod
    /// and expressions[0] = start, expressions[1] = length. Otherwise it's subscripts.
    /// </summary>
    /// <summary>P7 Step 12 down-conversion for the frozen oracle: one FUNCTION argument subtree's DEFAULT-mode
    /// leaf tokens re-typed to the SUBSCRIPT-mode twins the segment binder has always consumed. Tokens from a
    /// nested subscript group inside the argument arrive as genuine SUB_*/SIGNED_* types and pass through
    /// unchanged; a word-shaped keyword token (FUNCTION itself, a phrase word like LEADING, figurative ZERO)
    /// maps to SUB_IDENTIFIER with its text — exactly what SUBSCRIPT mode lexed before the reshape.</summary>
    private static List<IToken> MapFunctionArgTokens(List<IToken> toks)
    {
        var mapped = new List<IToken>(toks.Count);
        foreach (var t in toks)
        {
            int nt = t.Type switch
            {
                CobolParserCore.IDENTIFIER or CobolParserCore.FUNCTION => CobolParserCore.SUB_IDENTIFIER,
                CobolParserCore.INTEGERLIT => CobolParserCore.SUB_INTEGERLIT,
                CobolParserCore.DECIMALLIT => CobolParserCore.SUB_DECIMALLIT,
                CobolParserCore.STRINGLIT => CobolParserCore.SUB_STRINGLIT,
                CobolParserCore.NATLIT => CobolParserCore.SUB_NATLIT,
                CobolParserCore.BOOLLIT => CobolParserCore.SUB_BOOLLIT,
                CobolParserCore.PLUS => CobolParserCore.SUB_PLUS,
                CobolParserCore.MINUS => CobolParserCore.SUB_MINUS,
                CobolParserCore.STAR => CobolParserCore.SUB_STAR,
                CobolParserCore.SLASH => CobolParserCore.SUB_SLASH,
                CobolParserCore.POWER => CobolParserCore.SUB_POWER,
                CobolParserCore.LPAREN => CobolParserCore.SUB_LPAREN,
                CobolParserCore.RPAREN => CobolParserCore.SUB_RPAREN,
                // A NESTED call's own ARGUMENT-LIST parens inside this argument's subtree — the exact sibling of
                // the FNARG_SEPARATOR arm below, and omitting them cost 31 NIST IF-suite regressions (fix-queue
                // PB48). The lexer types the '(' after `FUNCTION <name>` as FNARG_LPAREN (ISO §8.4.3.2.3 SR6),
                // so `FUNCTION MAX(FUNCTION ABS(X) 3)` contributes FNARG parens here where it used to contribute
                // plain ones; unmapped they fell through to the segment binder as an unknown token and the
                // compiled program threw IndexOutOfRange / "Sequence contains no elements" at RUN TIME.
                CobolParserCore.FNARG_LPAREN => CobolParserCore.SUB_LPAREN,
                CobolParserCore.FNARG_RPAREN => CobolParserCore.SUB_RPAREN,
                CobolParserCore.COMMA => CobolParserCore.SUB_COMMA,
                // A NESTED call's argument separator inside this argument's subtree (the outer grammar consumed
                // the outer-level ones): the SUB twin keeps the inner argument split alive in the segment binder.
                CobolParserCore.FNARG_SEPARATOR => t.Text.StartsWith(';')
                    ? CobolParserCore.SUB_SEMICOLON : CobolParserCore.SUB_COMMA,
                CobolParserCore.OF => CobolParserCore.SUB_OF,
                CobolParserCore.IN => CobolParserCore.SUB_IN,
                var other => other,
            };
            if (nt != t.Type)
            {
                mapped.Add(new CommonToken(t) { Type = nt });
                continue;
            }
            // Already a SUBSCRIPT-mode / signed token (a nested subscript group) — pass through.
            if (t.Type is CobolParserCore.SUB_WS or CobolParserCore.SUB_OF or CobolParserCore.SUB_IN
                or CobolParserCore.SUB_ALL or CobolParserCore.SIGNED_DECIMALLIT or CobolParserCore.SIGNED_INTEGERLIT
                or CobolParserCore.SUB_INTEGERLIT or CobolParserCore.SUB_DECIMALLIT or CobolParserCore.SUB_STRINGLIT
                or CobolParserCore.SUB_NATLIT or CobolParserCore.SUB_BOOLLIT or CobolParserCore.SUB_IDENTIFIER
                or CobolParserCore.SUB_PLUS or CobolParserCore.SUB_MINUS or CobolParserCore.SUB_POWER
                or CobolParserCore.SUB_STAR or CobolParserCore.SUB_SLASH or CobolParserCore.SUB_COMMA
                or CobolParserCore.SUB_SEMICOLON or CobolParserCore.SUB_COLON or CobolParserCore.SUB_LPAREN
                or CobolParserCore.SUB_RPAREN)
            {
                mapped.Add(t);
                continue;
            }
            // Any other word-shaped keyword token (phrase words, figuratives) lexed SUB_IDENTIFIER before.
            mapped.Add(t.Text.Length > 0 && t.Text.All(c => char.IsLetterOrDigit(c) || c == '-')
                ? new CommonToken(t) { Type = CobolParserCore.SUB_IDENTIFIER }
                : t);
        }
        return mapped;
    }

    internal (List<BoundExpression> Exprs, bool IsRefMod) InterpretSubscriptTokens(
        CobolParserCore.SubscriptOrRefModContext ctx)
    {
        // Collect all leaf tokens from the subToken+ tree
        var tokens = new List<IToken>();
        CollectLeafTokens(ctx, tokens);

        // Check for a ref-mod colon — but only at depth 0. A colon inside nested parentheses
        // belongs to a subscript operand's own reference modification (e.g. LENGTH(WS(1:N)) or
        // f(T(I)(1:3))) and must be left for that operand's segment binding, not treated as the
        // outer operand's ref-mod separator.
        int colonIdx = -1;
        for (int i = 0, d = 0; i < tokens.Count; i++)
        {
            int tt = tokens[i].Type;
            if (tt == CobolParserCore.SUB_LPAREN) d++;
            else if (tt == CobolParserCore.SUB_RPAREN) { if (d > 0) d--; }
            else if (tt == CobolParserCore.SUB_COLON && d == 0) { colonIdx = i; break; }
        }
        if (colonIdx >= 0)
        {
            // Ref-mod: split on colon, parse each half as arithmetic expression
            var startTokens = tokens.GetRange(0, colonIdx);
            var lengthTokens = colonIdx + 1 < tokens.Count
                ? tokens.GetRange(colonIdx + 1, tokens.Count - colonIdx - 1)
                : new List<IToken>();
            var exprs = new List<BoundExpression>();
            exprs.Add(BindSubscriptTokensAsArithmetic(startTokens));
            if (lengthTokens.Any(t => t.Type != CobolParserCore.SUB_WS))
                exprs.Add(BindSubscriptTokensAsArithmetic(lengthTokens));
            return (exprs, true);
        }

        // Subscripts: split on multi-space (SUB_WS with 2+ chars) or SUB_COMMA boundaries
        var segments = SplitSubscriptTokens(tokens);
        var subs = new List<BoundExpression>();
        foreach (var seg in segments)
            subs.Add(BindSubscriptSegment(seg));
        return (subs, false);
    }

    internal static void CollectLeafTokens(IParseTree node, List<IToken> tokens)
    {
        if (node is ITerminalNode term)
        {
            tokens.Add(term.Symbol);
            return;
        }
        for (int i = 0; i < node.ChildCount; i++)
            CollectLeafTokens(node.GetChild(i), tokens);
    }

    /// <summary>Split token list into subscript segments on WS/COMMA boundaries.</summary>
    internal static List<List<IToken>> SplitSubscriptTokens(List<IToken> tokens)
    {
        var segments = new List<List<IToken>>();
        var current = new List<IToken>();
        int depth = 0;   // nested-paren depth: only split at depth 0

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == CobolParserCore.SUB_LPAREN) { depth++; current.Add(t); continue; }
            if (t.Type == CobolParserCore.SUB_RPAREN) { if (depth > 0) depth--; current.Add(t); continue; }

            if (depth == 0 && (t.Type == CobolParserCore.SUB_COMMA || t.Type == CobolParserCore.SUB_SEMICOLON))
            {
                if (current.Count > 0) segments.Add(current);
                current = new List<IToken>();
                continue;
            }
            if (depth == 0 && t.Type == CobolParserCore.SUB_WS)
            {
                // Multi-space is a subscript separator. Single space could be part of
                // relative subscripting (IDENT + N). Check: if next non-WS is a sign
                // token (SUB_PLUS/SUB_MINUS) and current ends with an identifier,
                // it MIGHT be relative. But SIGNED_INTEGERLIT already handled the
                // adjacent-sign case. If we see SUB_PLUS/SUB_MINUS after WS, it's
                // part of relative subscripting (operator separated by space).
                // Split only when what follows starts a new subscript:
                //   SIGNED_INTEGERLIT, SUB_IDENTIFIER, SUB_INTEGERLIT, SUB_ALL
                int next = i + 1;
                while (next < tokens.Count && tokens[next].Type == CobolParserCore.SUB_WS)
                    next++;
                if (next < tokens.Count && current.Count > 0)
                {
                    int nextType = tokens[next].Type;
                    // Only split if next token starts a new subscript AND current
                    // segment doesn't end with an operator (which would mean the
                    // WS is inside a relative subscript: IDENT + N)
                    var lastNonWs = current.FindLast(x => x.Type != CobolParserCore.SUB_WS);
                    // A trailing arithmetic operator means the WS is inside an expression
                    // (e.g. "9 * A", "B / 2", "X + N") — do not split the argument here.
                    bool endsWithOperator = lastNonWs != null &&
                        (lastNonWs.Type == CobolParserCore.SUB_PLUS || lastNonWs.Type == CobolParserCore.SUB_MINUS
                         || lastNonWs.Type == CobolParserCore.SUB_STAR || lastNonWs.Type == CobolParserCore.SUB_SLASH
                         || lastNonWs.Type == CobolParserCore.SUB_POWER);
                    // Don't split after OF/IN — these are qualification keywords
                    bool endsWithQualifier = lastNonWs != null &&
                        (lastNonWs.Type == CobolParserCore.SUB_OF || lastNonWs.Type == CobolParserCore.SUB_IN);
                    // Don't split after the FUNCTION keyword — the following name is part of a
                    // nested intrinsic-function call (e.g. ACOS(FUNCTION ACOS(D / D))).
                    bool endsWithFunction = lastNonWs != null
                        && lastNonWs.Type == CobolParserCore.SUB_IDENTIFIER
                        && string.Equals(lastNonWs.Text, "FUNCTION", StringComparison.OrdinalIgnoreCase);

                    if (!endsWithOperator && !endsWithQualifier && !endsWithFunction &&
                        (nextType == CobolParserCore.SIGNED_INTEGERLIT
                         || nextType == CobolParserCore.SIGNED_DECIMALLIT
                         || nextType == CobolParserCore.SUB_IDENTIFIER
                         || nextType == CobolParserCore.SUB_INTEGERLIT
                         // A string or unsigned-decimal literal is never an arithmetic operand, so a space
                         // before it always begins a new argument (e.g. CONCATENATE("ab" "cd" "ef")).
                         || nextType == CobolParserCore.SUB_STRINGLIT
                         || nextType == CobolParserCore.SUB_DECIMALLIT
                         || nextType == CobolParserCore.SUB_ALL))
                    {
                        segments.Add(current);
                        current = new List<IToken>();
                        i = next - 1; // skip consumed WS
                        continue;
                    }
                }
                // Part of relative subscripting — keep in current segment
                current.Add(t);
                continue;
            }
            current.Add(t);
        }
        if (current.Count > 0) segments.Add(current);
        return segments;
    }

    /// <summary>Bind a single subscript segment (list of SUBSCRIPT-mode tokens).</summary>
    internal BoundExpression BindSubscriptSegment(List<IToken> tokens)
    {
        // Remove leading/trailing WS
        while (tokens.Count > 0 && tokens[0].Type == CobolParserCore.SUB_WS)
            tokens.RemoveAt(0);
        while (tokens.Count > 0 && tokens[^1].Type == CobolParserCore.SUB_WS)
            tokens.RemoveAt(tokens.Count - 1);

        if (tokens.Count == 0)
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);

        // A segment that is a full arithmetic expression or a nested intrinsic-function call
        // (intrinsic-function arguments such as "9 * A", "B / 2", "E + .001", "B - 2",
        // "FUNCTION INTEGER(1.6)") is parsed by the arithmetic parser. This includes additive
        // operators: a relative subscript "IDENT ± integer" yields the same bound tree through
        // the arithmetic parser, while expressions like "E + .001" or "1 - .1" are ONLY handled
        // correctly there — the simpler relative-offset path below recognises just IDENT ± INT
        // and silently dropped a decimal or second operand. A single SIGNED literal (+8, -3) has
        // no separate SUB_PLUS/SUB_MINUS token, so it still takes the simple path.
        if (tokens.Any(t => t.Type is CobolParserCore.SUB_STAR or CobolParserCore.SUB_SLASH
                or CobolParserCore.SUB_POWER or CobolParserCore.SUB_PLUS or CobolParserCore.SUB_MINUS
                || (t.Type == CobolParserCore.SUB_IDENTIFIER
                    && string.Equals(t.Text, "FUNCTION", StringComparison.OrdinalIgnoreCase))))
            return BindSubscriptTokensAsArithmetic(tokens);

        // Single SIGNED_INTEGERLIT: +8, -3
        if (tokens.Count == 1 && tokens[0].Type == CobolParserCore.SIGNED_INTEGERLIT)
        {
            decimal value = decimal.Parse(tokens[0].Text, System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // Single SUB_INTEGERLIT: 1, 10
        if (tokens.Count == 1 && tokens[0].Type == CobolParserCore.SUB_INTEGERLIT)
        {
            decimal value = decimal.Parse(tokens[0].Text, System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // ALL
        if (tokens.Count == 1 && tokens[0].Type == CobolParserCore.SUB_ALL)
            return new BoundLiteralExpression("ALL", CobolCategory.Alphanumeric);

        // Identifier with optional qualification (OF/IN) and relative offset
        // Extract identifier and qualifiers first
        string? baseName = null;
        var qualNames = new List<string>();
        bool expectingQualifier = false;

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == CobolParserCore.SUB_WS) continue;
            if (t.Type == CobolParserCore.SUB_OF || t.Type == CobolParserCore.SUB_IN)
            {
                expectingQualifier = true;
                continue;
            }
            if (t.Type == CobolParserCore.SUB_IDENTIFIER)
            {
                if (baseName == null) baseName = t.Text;
                else if (expectingQualifier) { qualNames.Add(t.Text); expectingQualifier = false; }
                continue;
            }
            // Remaining tokens are operator + offset (relative subscript)
            break;
        }

        if (baseName == null)
            return BindSubscriptTokensAsArithmetic(tokens);

        DataSymbol? sym2;
        if (qualNames.Count > 0)
            sym2 = ResolveQualifiedName(baseName, qualNames);
        else
            sym2 = _ctx.Semantic.ResolveData(baseName);

        // Nested subscript or reference-modification on the base reference, e.g. an
        // intrinsic-function argument like MEDIAN(IND(1), IND(2)) or REVERSE(WS(1:3)).
        // The base identifier is followed by a balanced SUB_LPAREN … SUB_RPAREN group;
        // inside SUBSCRIPT mode a nested '(' pushes another SUBSCRIPT mode (CobolLexer.g4),
        // so the inner subscripts/colon arrive as ordinary SUBSCRIPT tokens. Without this,
        // the subscript was silently dropped and the whole base table was read.
        int lparenIdx = tokens.FindIndex(t => t.Type == CobolParserCore.SUB_LPAREN);
        if (sym2 != null && lparenIdx >= 0)
        {
            var inner = new List<IToken>();
            int pdepth = 0;
            for (int i = lparenIdx; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.Type == CobolParserCore.SUB_LPAREN) { pdepth++; if (pdepth == 1) continue; }
                else if (t.Type == CobolParserCore.SUB_RPAREN) { pdepth--; if (pdepth == 0) break; }
                inner.Add(t);
            }
            var cat2 = sym2.ResolvedType?.Category ?? CobolCategory.Numeric;

            int innerColon = inner.FindIndex(t => t.Type == CobolParserCore.SUB_COLON);
            if (innerColon >= 0)
            {
                var startToks = inner.GetRange(0, innerColon);
                var lenToks = innerColon + 1 < inner.Count
                    ? inner.GetRange(innerColon + 1, inner.Count - innerColon - 1)
                    : new List<IToken>();
                var startE = BindSubscriptTokensAsArithmetic(startToks);
                BoundExpression? lenE = lenToks.Any(t => t.Type != CobolParserCore.SUB_WS)
                    ? BindSubscriptTokensAsArithmetic(lenToks) : null;
                var rmBase = new BoundIdentifierExpression(sym2, cat2);
                _ctx.Typed(rmBase);
                return _ctx.Typed(new BoundReferenceModificationExpression(rmBase, startE, lenE));
            }

            var innerSubs = new List<BoundExpression>();
            foreach (var seg in SplitSubscriptTokens(inner))
                innerSubs.Add(BindSubscriptSegment(seg));
            var subscriptedId = new BoundIdentifierExpression(sym2, cat2, innerSubs);
            _ctx.Typed(subscriptedId);
            return subscriptedId;
        }

        BoundExpression baseExpr2 = sym2 != null
            ? new BoundIdentifierExpression(sym2, sym2.ResolvedType?.Category ?? CobolCategory.Numeric)
            : new BoundLiteralExpression(baseName, CobolCategory.Alphanumeric);

        // Check for relative offset (+/- integer) in remaining tokens
        var remaining = tokens.SkipWhile(t =>
            t.Type == CobolParserCore.SUB_WS || t.Type == CobolParserCore.SUB_IDENTIFIER
            || t.Type == CobolParserCore.SUB_OF || t.Type == CobolParserCore.SUB_IN).ToList();
        if (remaining.Count >= 2)
        {
            var opTok = remaining.FirstOrDefault(t => t.Type == CobolParserCore.SUB_PLUS || t.Type == CobolParserCore.SUB_MINUS);
            var numTok = remaining.FirstOrDefault(t => t.Type == CobolParserCore.SUB_INTEGERLIT);
            if (opTok != null && numTok != null)
            {
                var offset = decimal.Parse(numTok.Text, System.Globalization.CultureInfo.InvariantCulture);
                var op = opTok.Type == CobolParserCore.SUB_MINUS
                    ? BoundBinaryOperatorKind.Subtract : BoundBinaryOperatorKind.Add;
                return new BoundBinaryExpression(baseExpr2, op,
                    new BoundLiteralExpression(offset, CobolCategory.Numeric), CobolCategory.Numeric);
            }
        }

        return baseExpr2;
    }

    /// <summary>
    /// Bind a flat SUBSCRIPT-mode token list as a full arithmetic expression. Used for
    /// reference-modification operands, relative subscripts, and intrinsic-function arguments
    /// (ISO §15). Recursive descent with COBOL precedence: additive → multiplicative → power
    /// → unary → primary, where a primary is a parenthesized expression, a numeric literal, or
    /// a (possibly qualified and/or subscripted) data reference. Whitespace tokens are skipped.
    /// </summary>
    internal BoundExpression BindSubscriptTokensAsArithmetic(List<IToken> tokens)
    {
        int pos = 0;
        var expr = ParseSubAdditive(tokens, ref pos);
        return expr ?? new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    private static int SkipSubWs(List<IToken> toks, int pos)
    {
        while (pos < toks.Count && toks[pos].Type == CobolParserCore.SUB_WS) pos++;
        return pos;
    }

    private static int PeekSubType(List<IToken> toks, int pos)
    {
        pos = SkipSubWs(toks, pos);
        return pos < toks.Count ? toks[pos].Type : -1;
    }

    private BoundExpression ParseSubAdditive(List<IToken> toks, ref int pos)
    {
        var left = ParseSubMultiplicative(toks, ref pos);
        while (true)
        {
            int t = PeekSubType(toks, pos);
            if (t != CobolParserCore.SUB_PLUS && t != CobolParserCore.SUB_MINUS) break;
            pos = SkipSubWs(toks, pos) + 1;
            var right = ParseSubMultiplicative(toks, ref pos);
            var op = t == CobolParserCore.SUB_PLUS ? BoundBinaryOperatorKind.Add : BoundBinaryOperatorKind.Subtract;
            left = new BoundBinaryExpression(left, op, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression ParseSubMultiplicative(List<IToken> toks, ref int pos)
    {
        var left = ParseSubPower(toks, ref pos);
        while (true)
        {
            int t = PeekSubType(toks, pos);
            if (t != CobolParserCore.SUB_STAR && t != CobolParserCore.SUB_SLASH) break;
            pos = SkipSubWs(toks, pos) + 1;
            var right = ParseSubPower(toks, ref pos);
            var op = t == CobolParserCore.SUB_STAR ? BoundBinaryOperatorKind.Multiply : BoundBinaryOperatorKind.Divide;
            left = new BoundBinaryExpression(left, op, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression ParseSubPower(List<IToken> toks, ref int pos)
    {
        var left = ParseSubUnary(toks, ref pos);
        if (PeekSubType(toks, pos) == CobolParserCore.SUB_POWER)
        {
            pos = SkipSubWs(toks, pos) + 1;
            var right = ParseSubPower(toks, ref pos); // right-associative
            return new BoundBinaryExpression(left, BoundBinaryOperatorKind.Power, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression ParseSubUnary(List<IToken> toks, ref int pos)
    {
        int t = PeekSubType(toks, pos);
        if (t == CobolParserCore.SUB_PLUS || t == CobolParserCore.SUB_MINUS)
        {
            pos = SkipSubWs(toks, pos) + 1;
            var operand = ParseSubUnary(toks, ref pos);
            if (t == CobolParserCore.SUB_PLUS) return operand;
            return new BoundBinaryExpression(
                new BoundLiteralExpression(0m, CobolCategory.Numeric),
                BoundBinaryOperatorKind.Subtract, operand, CobolCategory.Numeric);
        }
        return ParseSubPrimary(toks, ref pos);
    }

    private BoundExpression ParseSubPrimary(List<IToken> toks, ref int pos)
    {
        pos = SkipSubWs(toks, pos);
        if (pos >= toks.Count) return new BoundLiteralExpression(0m, CobolCategory.Numeric);
        var tok = toks[pos];

        // Parenthesized sub-expression
        if (tok.Type == CobolParserCore.SUB_LPAREN)
        {
            pos++;
            var inner = ParseSubAdditive(toks, ref pos);
            pos = SkipSubWs(toks, pos);
            if (pos < toks.Count && toks[pos].Type == CobolParserCore.SUB_RPAREN) pos++;
            return inner;
        }

        if (tok.Type == CobolParserCore.SUB_INTEGERLIT || tok.Type == CobolParserCore.SUB_DECIMALLIT
            || tok.Type == CobolParserCore.SIGNED_INTEGERLIT || tok.Type == CobolParserCore.SIGNED_DECIMALLIT)
        {
            pos++;
            return new BoundLiteralExpression(
                decimal.Parse(tok.Text, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture),
                CobolCategory.Numeric);
        }

        // Alphanumeric literal argument (e.g. FUNCTION LOWER-CASE("ABC")).
        if (tok.Type == CobolParserCore.SUB_STRINGLIT)
        {
            pos++;
            string text = tok.Text;
            string value = text.Length >= 2
                ? text[1..^1].Replace(new string(text[0], 2), new string(text[0], 1))
                : text;
            return new BoundLiteralExpression(value, CobolCategory.Alphanumeric);
        }

        if (tok.Type == CobolParserCore.SUB_IDENTIFIER)
        {
            // Nested intrinsic-function call as an argument, e.g. ACOS(FUNCTION ACOS(D / D)).
            // In SUBSCRIPT mode the FUNCTION keyword and the function name are plain
            // SUB_IDENTIFIERs, so recognize "FUNCTION name (args)" here.
            if (string.Equals(tok.Text, "FUNCTION", StringComparison.OrdinalIgnoreCase))
            {
                pos++;
                pos = SkipSubWs(toks, pos);
                string fname = (pos < toks.Count && toks[pos].Type == CobolParserCore.SUB_IDENTIFIER)
                    ? toks[pos++].Text : "UNKNOWN";
                var fargs = new List<BoundExpression>();
                if (PeekSubType(toks, pos) == CobolParserCore.SUB_LPAREN)
                {
                    pos = SkipSubWs(toks, pos);
                    var innerToks = CollectBalancedSubTokens(toks, ref pos);
                    var (innerExprs, _) = InterpretCollectedSubscriptTokens(innerToks);
                    fargs.AddRange(innerExprs);
                }
                var fcat = BindingContext.AlphanumericFunctions.Contains(fname)
                    ? CobolCategory.Alphanumeric : CobolCategory.Numeric;
                return new BoundFunctionCallExpression(fname, fargs.AsReadOnly(), fcat);
            }

            pos++;
            string baseName = tok.Text;
            var qualNames = new List<string>();
            // Qualification: OF/IN data-name ...
            while (PeekSubType(toks, pos) is CobolParserCore.SUB_OF or CobolParserCore.SUB_IN)
            {
                pos = SkipSubWs(toks, pos) + 1;
                pos = SkipSubWs(toks, pos);
                if (pos < toks.Count && toks[pos].Type == CobolParserCore.SUB_IDENTIFIER)
                    qualNames.Add(toks[pos++].Text);
            }
            // Nested subscript: name(subscripts)
            List<BoundExpression>? subs = null;
            if (PeekSubType(toks, pos) == CobolParserCore.SUB_LPAREN)
            {
                pos = SkipSubWs(toks, pos);
                var inner = CollectBalancedSubTokens(toks, ref pos);
                var (innerExprs, _) = InterpretCollectedSubscriptTokens(inner);
                subs = innerExprs;
            }

            var sym = qualNames.Count > 0 ? ResolveQualifiedName(baseName, qualNames) : _ctx.Semantic.ResolveData(baseName);
            if (sym == null)
                return new BoundLiteralExpression(baseName, CobolCategory.Alphanumeric);
            var cat = sym.ResolvedType?.Category ?? CobolCategory.Numeric;
            return _ctx.Typed(new BoundIdentifierExpression(sym, cat, subs));
        }

        // Unknown token — consume and yield 0 so parsing can continue.
        pos++;
        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    /// <summary>Collect the tokens inside a balanced SUB_LPAREN…SUB_RPAREN (excluding the outer parens).</summary>
    private static List<IToken> CollectBalancedSubTokens(List<IToken> toks, ref int pos)
    {
        var inner = new List<IToken>();
        if (pos >= toks.Count || toks[pos].Type != CobolParserCore.SUB_LPAREN) return inner;
        pos++; // consume '('
        int depth = 1;
        while (pos < toks.Count && depth > 0)
        {
            int tt = toks[pos].Type;
            if (tt == CobolParserCore.SUB_LPAREN) depth++;
            else if (tt == CobolParserCore.SUB_RPAREN) { depth--; if (depth == 0) { pos++; break; } }
            inner.Add(toks[pos]);
            pos++;
        }
        return inner;
    }

    /// <summary>Interpret a collected inner token list as a subscript list (split on commas/spaces, bind each).</summary>
    private (List<BoundExpression> Exprs, bool IsRefMod) InterpretCollectedSubscriptTokens(List<IToken> tokens)
    {
        var segments = SplitSubscriptTokens(tokens);
        var subs = new List<BoundExpression>(segments.Count);
        foreach (var seg in segments)
            subs.Add(BindSubscriptSegment(seg));
        return (subs, false);
    }

    /// <summary>
    /// Bind a subscript entry per COBOL-85 §5.3. SUBSCRIPT lexer mode provides
    /// sign-adjacency disambiguation: SIGNED_INTEGERLIT (+N) vs SUB_PLUS SUB_WS SUB_INTEGERLIT (+ N).
    /// </summary>
    internal BoundExpression BindSubscriptEntry(CobolParserCore.SubscriptEntryContext ctx)
    {
        // Signed integer literal: +8, -3, +1 (sign adjacent to digits)
        if (ctx.SIGNED_INTEGERLIT() is { } signedLit)
        {
            string text = signedLit.GetText();
            decimal value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // Unsigned integer literal: 1, 10, 300
        if (ctx.SUB_INTEGERLIT() is { } intLit)
        {
            decimal value = decimal.Parse(intLit.GetText(), System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // ALL (for SEARCH ALL)
        if (ctx.SUB_ALL() != null)
            return new BoundLiteralExpression("ALL", CobolCategory.Alphanumeric);

        // Data-name / index-name with optional qualification and relative offset
        if (ctx.SUB_IDENTIFIER() is { } idToken)
        {
            string baseName = idToken.GetText();

            // Handle qualifications
            var quals = ctx.subscriptQualification();
            DataSymbol? baseSym;
            if (quals.Length > 0)
            {
                var qualNames = new List<string>();
                foreach (var q in quals)
                    qualNames.Add(q.SUB_IDENTIFIER().GetText());
                baseSym = ResolveQualifiedName(baseName, qualNames);
            }
            else
            {
                baseSym = _ctx.Semantic.ResolveData(baseName);
            }

            BoundExpression baseExpr;
            if (baseSym != null)
            {
                var baseCat = baseSym.ResolvedType?.Category ?? CobolCategory.Numeric;
                baseExpr = new BoundIdentifierExpression(baseSym, baseCat);
            }
            else
            {
                baseExpr = new BoundLiteralExpression(baseName, CobolCategory.Alphanumeric);
            }

            // Relative subscript offset: data-name + N or data-name - N
            if (ctx.relativeOffset() is { } relOff)
            {
                decimal offset = decimal.Parse(relOff.SUB_INTEGERLIT().GetText(),
                    System.Globalization.CultureInfo.InvariantCulture);
                var offsetLit = new BoundLiteralExpression(offset, CobolCategory.Numeric);
                var op = relOff.SUB_MINUS() != null
                    ? BoundBinaryOperatorKind.Subtract
                    : BoundBinaryOperatorKind.Add;
                return new BoundBinaryExpression(baseExpr, op, offsetLit, CobolCategory.Numeric);
            }

            return baseExpr;
        }

        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    // ── QUALIFIED NAME RESOLUTION ──

    /// <summary>
    /// Resolve a qualified name using right-to-left narrowing.
    /// A OF B OF C → resolve C (outermost), then B within C, then A within B.
    /// </summary>
    internal DataSymbol? ResolveQualifiedName(string name, List<string> qualifiers)
    {
        // Start from the rightmost (outermost) qualifier
        DataSymbol? context = _ctx.Semantic.ResolveData(qualifiers[^1]);
        if (context == null) return null;

        // Walk qualifiers right-to-left (skip the last one, already resolved)
        for (int i = qualifiers.Count - 2; i >= 0; i--)
        {
            context = FindChild(context, qualifiers[i]);
            if (context == null) return null;
        }

        // Resolve the target name within the final context
        return FindChild(context, name);
    }

    /// <summary>
    /// Find a child data symbol by name within a group item.
    /// Searches recursively through the group's children.
    /// </summary>
    internal static DataSymbol? FindChild(DataSymbol parent, string name)
    {
        foreach (var child in parent.Children)
        {
            if (string.Equals(child.DisplayName, name, StringComparison.OrdinalIgnoreCase))
                return child;
            // Search deeper (intermediate groups)
            var deep = FindChild(child, name);
            if (deep != null) return deep;
        }
        return null;
    }

    // ── REFERENCE MODIFICATION ──

    internal BoundExpression BindReferenceModification(
        BoundIdentifierExpression baseId,
        CobolParserCore.RefModSpecContext ctx)
    {
        var arithExprs = ctx.arithmeticExpression();
        var startExpr = BindArithmeticExpr(arithExprs[0]);

        BoundExpression? lengthExpr = null;
        if (arithExprs.Length > 1)
            lengthExpr = BindArithmeticExpr(arithExprs[1]);

        return new BoundReferenceModificationExpression(baseId, startExpr, lengthExpr);
    }
}
