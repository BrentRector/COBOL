// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Expressions;

using Core = CobolParserCore;

/// <summary>
/// The ONE boolean-expression precedence resolver (ISO/IEC 1989:2023 §8.8.2). It walks a parsed
/// <c>booleanExpression</c> and combines its operands in the order §8.8.2 rule 7 requires, returning a caller-typed
/// result. It exists because the <b>context-inherited precedence of the shift operators cannot be expressed by a
/// context-free grammar</b>: rule 7b makes a boolean shift take the precedence of the operator immediately
/// preceding it (B-AND if none), so the grouping of e.g. <c>A B-AND B B-SHIFT-L 2</c> depends on a token that a CFG
/// tier cannot see. The grammar tiers (<c>booleanExpression → booleanXorTerm → booleanAndTerm → booleanShiftTerm →
/// booleanFactor</c>) therefore establish only the operand/operator SEQUENCE and the parenthesis structure; this
/// resolver re-derives the correct grouping from that sequence.
///
/// It is generic over the combine operations (<c>leaf</c> / <c>not</c> / <c>binary</c> / <c>shift</c>) so the SAME
/// grouping serves every consumer of §8.8.2 (the singular-pattern rule):
/// <list type="bullet">
/// <item>the compile-time boolean evaluator folds constant bit strings (<c>T = BitString</c>);</item>
/// <item>the runtime COMPUTE-Format-2 boolean binder builds bound nodes (<c>T = BoundBoolExpr</c>).</item>
/// </list>
///
/// Precedence (rule 7b): B-NOT (handled at the <c>booleanFactor</c> level, tightest) &gt; B-AND &gt; B-XOR &gt;
/// B-OR; a shift inherits the precedence of the operation lexically before it — a B-NOT included — or B-AND
/// when it is the first operation. Equal precedence associates left-to-right (rule 7c). Parentheses evaluate first (rule 7a) — a
/// parenthesized sub-expression is a fresh <c>booleanExpression</c> resolved recursively through the
/// <c>booleanFactor</c> leaf.
/// </summary>
public static class BooleanExpressionResolver
{
    /// <summary>Boolean operation precedence (higher binds tighter): B-NOT(4) &gt; B-AND(3) &gt; B-XOR(2) &gt;
    /// B-OR(1) — §8.8.2 rule 7b's 1st…4th. A shift is assigned the precedence of the operation preceding it,
    /// defaulting to B-AND's when it is first (rule 7b tail). <see cref="PrecNot"/> is never stacked (B-NOT is
    /// folded at the factor); it exists because a B-NOT IS an operation a following shift inherits from.</summary>
    private const int PrecNot = 4, PrecAnd = 3, PrecXor = 2, PrecOr = 1, PrecShiftDefault = PrecAnd;

    /// <summary>Resolve <paramref name="ctx"/> to a single value of type <typeparamref name="T"/>, combining its
    /// operands per §8.8.2 rule 7.</summary>
    /// <param name="leaf">Resolve a leaf boolean operand — a <c>valueOperand</c> (boolean literal / figurative /
    /// category-boolean item). B-NOT and parentheses are handled by the resolver, not this callback.</param>
    /// <param name="not">Apply the unary <c>B-NOT</c> (§8.8.2 rule 7b, 1st precedence).</param>
    /// <param name="binary">Combine two operands with a binary operator: <c>'&amp;'</c> (B-AND), <c>'^'</c>
    /// (B-XOR), or <c>'|'</c> (B-OR). Rule 4 (both operands not both <c>ALL</c> literal) is the callback's to
    /// enforce.</param>
    /// <param name="shift">Apply a shift suffix (<c>(B-SHIFT-L|R|LC|RC) integer</c>) to its left operand. Rule 5
    /// (first operand not <c>ALL</c> literal; second operand integer) is the callback's to enforce.</param>
    public static T Resolve<T>(
        Core.BooleanExpressionContext ctx,
        Func<Core.ValueOperandContext, T> leaf,
        Func<T, T> not,
        Func<T, char, T, T> binary,
        Func<T, Core.BooleanShiftSuffixContext, T> shift)
    {
        // 1) Flatten the tiered parse tree into its lexical sequence of operands and operators. A factor is
        //    resolved to T eagerly (recursing through B-NOT and parentheses); binary operators and shift suffixes
        //    become operator items in source order.
        var items = new List<Item<T>>();
        FlattenExpr(ctx, items, leaf, not, binary, shift);

        // 2) Precedence-climb the flat sequence (a shunting-yard over an operand stack and an operator stack),
        //    assigning each shift the precedence of the operator that preceded it (§8.8.2 rule 7b), popping
        //    equal-or-higher precedence first (left-to-right at equal precedence, rule 7c).
        var operands = new Stack<T>();
        var ops = new Stack<PendingOp>();
        int prevPrec = PrecShiftDefault;   // precedence for a shift that is the first operation (rule 7b tail)

        foreach (var item in items)
        {
            if (item.IsOperand)
            {
                operands.Push(item.Operand!);
                // ⛔ A NEGATED OPERAND ENDS WITH AN OPERATION TOO (kb/Work PB1370). Rule 7b's ladder names negation
                // (B-NOT) 1st, and "the preceding operation" of `A B-AND B-NOT B B-SHIFT-R 1`'s shift is that
                // B-NOT, not the B-AND: the shift takes negation's precedence and applies to (B-NOT B) alone, so
                // the value is A B-AND ((B-NOT B) B-SHIFT-R 1). Tracking only BINARY operators here gave the shift
                // B-AND's precedence and shifted (A B-AND B-NOT B) instead — a wrong answer in BOTH lanes, since
                // the compile-time fold and the runtime binder share this resolver.
                if (item.Negated) prevPrec = PrecNot;
                continue;
            }

            if (item.ShiftSuffix is { } suf)
            {
                // A boolean SHIFT is a POSTFIX operator on its LEFT operand (its right operand — the count — is
                // captured in the suffix). Rule 7b: it takes the precedence of the operation immediately before it
                // (B-AND if none). So reduce the left operand down to that precedence, then apply the shift
                // IMMEDIATELY — never defer it on the operator stack, or a HIGHER-precedence FOLLOWING operator
                // (e.g. the B-AND in `A B-OR B B-SHIFT-L n B-AND C`) would wrongly fold its right operand INTO the
                // shift's operand. A following shift inherits the same preceding-operation precedence, so prevPrec
                // is left unchanged.
                while (ops.Count > 0 && ops.Peek().Prec >= prevPrec)
                    Apply(ops.Pop(), operands, binary);
                operands.Push(shift(operands.Pop(), suf));
                continue;
            }

            int prec = BinaryPrec(item.BinaryOp);
            while (ops.Count > 0 && ops.Peek().Prec >= prec)
                Apply(ops.Pop(), operands, binary);
            ops.Push(new PendingOp(item.BinaryOp, prec));
            prevPrec = prec;
        }
        while (ops.Count > 0)
            Apply(ops.Pop(), operands, binary);

        return operands.Pop();
    }

    private static int BinaryPrec(char op) => op switch
    {
        '&' => PrecAnd,
        '^' => PrecXor,
        _ => PrecOr,     // '|'
    };

    private static void Apply<T>(PendingOp op, Stack<T> operands, Func<T, char, T, T> binary)
    {
        T right = operands.Pop();
        T left = operands.Pop();
        operands.Push(binary(left, op.BinaryOp, right));
    }

    // ── flattening (tiered tree → lexical operand/operator sequence) ──────────────────────────────────────────

    private static void FlattenExpr<T>(Core.BooleanExpressionContext e, List<Item<T>> outp,
        Func<Core.ValueOperandContext, T> leaf, Func<T, T> not,
        Func<T, char, T, T> binary, Func<T, Core.BooleanShiftSuffixContext, T> shift)
    {
        // booleanExpression : booleanXorTerm ( B_OR booleanXorTerm )* — walk children in source order.
        foreach (var child in e.children)
        {
            if (child is Core.BooleanXorTermContext x) FlattenXor(x, outp, leaf, not, binary, shift);
            else outp.Add(Item<T>.Op('|'));   // a B_OR terminal
        }
    }

    private static void FlattenXor<T>(Core.BooleanXorTermContext x, List<Item<T>> outp,
        Func<Core.ValueOperandContext, T> leaf, Func<T, T> not,
        Func<T, char, T, T> binary, Func<T, Core.BooleanShiftSuffixContext, T> shift)
    {
        // booleanXorTerm : booleanAndTerm ( B_XOR booleanAndTerm )*
        foreach (var child in x.children)
        {
            if (child is Core.BooleanAndTermContext a) FlattenAnd(a, outp, leaf, not, binary, shift);
            else outp.Add(Item<T>.Op('^'));   // a B_XOR terminal
        }
    }

    private static void FlattenAnd<T>(Core.BooleanAndTermContext a, List<Item<T>> outp,
        Func<Core.ValueOperandContext, T> leaf, Func<T, T> not,
        Func<T, char, T, T> binary, Func<T, Core.BooleanShiftSuffixContext, T> shift)
    {
        // booleanAndTerm : booleanShiftTerm ( B_AND booleanShiftTerm )*
        foreach (var child in a.children)
        {
            if (child is Core.BooleanShiftTermContext s) FlattenShift(s, outp, leaf, not, binary, shift);
            else outp.Add(Item<T>.Op('&'));   // a B_AND terminal
        }
    }

    private static void FlattenShift<T>(Core.BooleanShiftTermContext s, List<Item<T>> outp,
        Func<Core.ValueOperandContext, T> leaf, Func<T, T> not,
        Func<T, char, T, T> binary, Func<T, Core.BooleanShiftSuffixContext, T> shift)
    {
        // booleanShiftTerm : booleanFactor booleanShiftSuffix* — the factor is one operand; each suffix is a
        // shift operator whose right operand is the integer inside the suffix.
        var factor = s.booleanFactor();
        outp.Add(Item<T>.Val(ResolveFactor(factor, leaf, not, binary, shift), negated: factor.B_NOT() is not null));
        foreach (var suf in s.booleanShiftSuffix())
            outp.Add(Item<T>.Shift(suf));
    }

    /// <summary>Resolve a <c>booleanFactor</c> leaf to <typeparamref name="T"/>: the unary <c>B-NOT</c> (tightest
    /// precedence, rule 7b 1st) and parentheses (a fresh sub-expression, rule 7a) are handled here; a bare
    /// operand goes to the <paramref name="leaf"/> callback.</summary>
    private static T ResolveFactor<T>(Core.BooleanFactorContext f,
        Func<Core.ValueOperandContext, T> leaf, Func<T, T> not,
        Func<T, char, T, T> binary, Func<T, Core.BooleanShiftSuffixContext, T> shift)
    {
        if (f.B_NOT() is not null)
            return not(ResolveFactor(f.booleanFactor(), leaf, not, binary, shift));
        if (f.booleanExpression() is { } paren)
            return Resolve(paren, leaf, not, binary, shift);
        return leaf(f.valueOperand());
    }

    // ── flat-sequence item + pending operator ────────────────────────────────────────────────────────────────

    /// <summary>One element of the flattened lexical sequence: a resolved operand, a binary operator, or a shift
    /// suffix. <see cref="Negated"/> marks an operand whose factor is a B-NOT — the operation a following shift
    /// inherits its precedence from (§8.8.2 rule 7b).</summary>
    private readonly struct Item<T>
    {
        public bool IsOperand { get; private init; }
        public T? Operand { get; private init; }
        public bool Negated { get; private init; }
        public char BinaryOp { get; private init; }
        public Core.BooleanShiftSuffixContext? ShiftSuffix { get; private init; }

        public static Item<T> Val(T v, bool negated) => new() { IsOperand = true, Operand = v, Negated = negated };
        public static Item<T> Op(char c) => new() { BinaryOp = c };
        public static Item<T> Shift(Core.BooleanShiftSuffixContext s) => new() { ShiftSuffix = s };
    }

    /// <summary>A BINARY operator awaiting its right operand on the shunting-yard stack, with its precedence. (A
    /// shift is postfix — applied immediately, never stacked — so it does not appear here.)</summary>
    private readonly record struct PendingOp(char BinaryOp, int Prec);
}
