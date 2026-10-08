// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;
using CobolNet.Editions;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// The boolean-expression / boolean-operator binder (Phase-4 track (a) increment 2; ISO §8.8.2 the operators
/// B-AND/B-OR/B-XOR/B-NOT, §14.9.8 Format-2 boolean COMPUTE, §8.8.4.2.2 the boolean relation, §8.8.4.3 the
/// simple boolean condition). A boolean expression binds into the <see cref="BoundBoolExpr"/> value channel
/// (a '0'/'1' string world, D-B1) — never the numeric or DISPLAY operand channels. Operand SHAPES are validated
/// here (the COBOLNET1511 constraint band); the {is2002()}?-gated grammar tiers enforce the §8.8.2 formation
/// rules structurally, and the operators are unreachable below 2002 (so no binder-side introduction gate — the
/// XOR precedent: the grammar predicate + the parse-layer hint ARE the gate).
/// P7 Step 10o: the WHOLE condition/relation/boolean channel as ONE collaborator over
/// <see cref="BinderContext"/> — the .Boolean partial + the core channel (AbbrevCarry / BindCondition family
/// / BindComparison / BindSoleOperandCondition / <see cref="CheckedRelational"/> [THE one BoundRelational
/// checkpoint] / ConditionOf / MapOperator / SoleDataRef / SoleNumLiteral) merged, because the two halves are
/// bidirectionally coupled (BindPrimary→BindPrimaryBoolean; the boolean alt→CheckedRelational/
/// ComparisonOperandOf). AbbrevCarry is hoisted out of the god class WITH its source-order threading intact.
/// RECORDED deviations from the plan text: (a) the collaborator host edges did NOT flip here — the host keeps
/// forwarders and everything flips at once at 10t (less churn, identical behavior); (b) the 1511/relational
/// SR bodies stay INSIDE this binder (no longer god-class inline — the pure StatementValidation lift is
/// deferred to the 10t sweep). The boolean-operator token set is <see cref="BooleanOperatorTokens"/>, read by this
/// binder's <c>HasBoolOp</c>, the parser's look-ahead predicates and the introduction gate alike (kb/Work PB1412 —
/// the VersionConformancePass duplicate this remark once called deliberate is gone).</summary>
internal sealed class ConditionBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind a <c>booleanExpression</c> (ISO §8.8.2) to a bound boolean tree via the ONE shared
    /// <see cref="BooleanExpressionResolver"/> (the same mechanism the compile-time expression evaluator uses).
    /// Precedence and grouping are resolved there — B-NOT &gt; B-AND &gt; B-XOR &gt; B-OR (rule 7b), left-to-right at
    /// equal precedence (rule 7c), and the context-inherited SHIFT precedence a context-free grammar cannot express
    /// (a shift takes the precedence of the operator before it, B-AND if none). So the mixed shift-with-binary form
    /// is ACCEPTED and grouped per the standard rather than refused. The combine operations build the bound nodes:
    /// a leaf operand via <see cref="BindBoolOperandValue"/>; B-NOT with the ALL-fold (§8.3.3.6.4 — ALL is
    /// positionless, so flip the pattern); a binary op via <see cref="MakeBoolBinary"/> (rule 4 — not both operands
    /// ALL); and a shift via <see cref="BindBoolShiftSuffix"/> (rule 5).</summary>
    internal BoundBoolExpr BindBoolExpr(Core.BooleanExpressionContext ctx) =>
        BooleanExpressionResolver.Resolve<BoundBoolExpr>(
            ctx,
            leaf: BindBoolOperandValue,
            // The fold CLEARS IsAllLiteral: `B-NOT ALL B"1"` no longer "consists solely of the ALL literal"
            // and is not itself the first operand rule 5 names (kb/Work PB157).
            not: inner => inner is BoundBoolAll all ? new BoundBoolAll(FlipBits(all.Bits)) : new BoundBoolNot(inner),
            binary: MakeBoolBinary,
            shift: BindBoolShiftSuffix);

    /// <summary>Apply one boolean shift suffix (<c>(B-SHIFT-L|R|LC|RC) integer</c>) to <paramref name="operand"/>
    /// (ISO §8.8.2 rule 8, COBOL-2023). Rule 5 has two halves: the first operand shall not be the figurative ALL
    /// literal (COBOLNET1511, the boolean-constraint band), and "The second operand shall be an integer operand"
    /// (COBOLNET2513, kb/Work PB1413) — asked through the ONE integer classifier (<see cref="IntrinsicResultType.IsIntegerOperand(BoundExpr)"/>:
    /// §5.5 2)'s integer literal, integer data item, or integer-type function; a numeric function is refused even
    /// when its value is integral, §8.4.3.2.3 SR11). The count's SHAPE — Table 4's single identifier or literal —
    /// is <see cref="ArithmeticFormationRules.ShiftCountNotSoleOperand"/>'s, reported by ExpressionFormationPass
    /// (COBOLNET1719); a malformed count is not asked the integer question too, so it draws one diagnostic. The 2023
    /// introduction is gated in the VersionConformancePass parse arm, so there is no binder-side edition gate here.</summary>
    private BoundBoolExpr BindBoolShiftSuffix(BoundBoolExpr operand, Core.BooleanShiftSuffixContext suf)
    {
        var kind = suf.B_SHIFT_LC() is not null ? BoolShiftKind.LeftCircular
                 : suf.B_SHIFT_RC() is not null ? BoolShiftKind.RightCircular
                 : suf.B_SHIFT_L() is not null ? BoolShiftKind.Left
                 : BoolShiftKind.Right;
        if (operand is BoundBoolAll { IsAllLiteral: true })   // NOT figurative ZERO — a disjoint §8.8.2 operand (kb/Work PB157)
            ctx.Edition.Error("COBOLNET1511", "the first operand of a boolean shift operation shall not be the "
                + "figurative constant ALL literal (ISO §8.8.2 rule 5)");
        var count = host.Expr.BindExpr(suf.arithmeticExpression());
        if (count is not BoundExprError && ArithmeticFormationRules.ShiftCountNotSoleOperand(suf) is null
            && !IntrinsicResultType.IsIntegerOperand(count))
            ctx.Edition.Error(DiagnosticCatalog.BooleanShiftCountNotInteger, $"the second operand of a boolean shift operation shall be an integer "
                + $"operand (ISO §8.8.2 rule 5) — '{DataBinder.WrittenText(suf.arithmeticExpression())}' is not: an integer operand "
                + "is an integer literal (no decimal point, §8.3.3.3.2), an integer data item, or an integer-type "
                + "function (ISO §5.5 2); a numeric function is not one even when its value is integral, "
                + "§8.4.3.2.3 SR11)");
        return new BoundBoolShift(operand, kind, count);
    }

    /// <summary>Rule 4 (§8.8.2 :9364): both operands of a binary boolean op shall not both be ALL "literal".</summary>
    private BoundBoolExpr MakeBoolBinary(BoundBoolExpr left, char op, BoundBoolExpr right)
    {
        if (left is BoundBoolAll { IsAllLiteral: true } && right is BoundBoolAll { IsAllLiteral: true })
            // Rule 4 restricts the Format-6 ALL literal, not figurative ZERO — `ZERO B-AND ALL B"1"` has ONE
            // ALL-literal operand and is legal (kb/Work PB157).
            ctx.Edition.Error("COBOLNET1511", "both operands of a boolean operator shall not be ALL literals "
                + "(ISO §8.8.2 rule 4)");
        return new BoundBoolBinary(left, op, right);
    }

    /// <summary>Resolve a boolean-expression leaf operand (ISO §8.8.2 operand list): a boolean literal, the
    /// figurative ZERO / <c>ALL B"…"</c>, or a category-boolean data item. Anything else — a non-boolean item,
    /// a plain string, an arithmetic expression, another figurative — is COBOLNET1511.</summary>
    private BoundBoolExpr BindBoolOperandValue(Core.ValueOperandContext vo)
    {
        var nn = vo.nonNumericLiteral();
        // A boolean concatenation expression (B"01" & B"10", ISO §8.8.3) folds to its equivalent single
        // boolean literal at compile time (§8.8.3.3 GR3) — the §8.8.2 operand list admits a boolean literal,
        // and the folded result IS one. A non-boolean concat is not a boolean operand (1511, below).
        if (nn?.concatenationExpression() is { } ce && ConcatFolder.ClassOf(ce, ctx.Data.LiteralEnv) is PicCategory.Boolean)
            return new BoundBoolLiteral(ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.LiteralEnv).Value);
        if (nn?.BOOLLIT() is { } bl)
            // BooleanData2002 (the B"…" literal introduction) gates on RECOGNITION in the VersionConformancePass
            // parse-arm (VisitNonNumericLiteral, statement-scoped); Step 14h.4b.
            return new BoundBoolLiteral(CobolLiteral.Decode(bl.GetText()));
        // The predefined NULL is no boolean operand and a boolean expression is no §8.4.3.10.3 SR1 context: the ONE
        // SR1 refusal (COBOLNET2576), through the literal mapping that owns it (kb/Work PB1427).
        if (nn?.predefinedNull() is not null && host.Expr.NonNumericLiteralOperand(nn) is BoundOperandError nullErr)
            return BoundBoolError.Carry(nullErr.Feature, nullErr.IsUnbuilt);
        if (nn?.figurativeConstant() is { } fig)
        {
            if (fig.zeroWord() is not null) return new BoundBoolAll("0");   // figurative ZERO — boolean zeros by context (§8.3.3.6.4 GR4)
            // ALL literal-1 of class boolean (a concatenated or constant-name literal-1 folds through the ONE Format 6
            // reader, classed by §8.8.3.3 GR1 — kb/Work PB71, PB1627). The routing predicate IsBooleanValueOperand
            // asked the SAME fold diagnostic-free, so the route and the reported class cannot disagree.
            if (ConcatFolder.FoldAllLiteral(fig, ctx.Data.LiteralEnv, ctx.Edition) is { Category: PicCategory.Boolean } literal1)
                return new BoundBoolAll(literal1.Value, IsAllLiteral: true);   // the ONE Format-6 construction site (kb/Work PB157)
            // kb/Work PB1029 — this refusal carried no diagnostic (`COMPUTE B = B B-AND SPACE` compiled clean and
            // aborted the run unit); it is the §8.8.2 operand list's figurative half, reported as its siblings are.
            ctx.Edition.Error("COBOLNET1511", $"'{ConcatFolder.Spelling(fig)}' is not a valid boolean operand — the only figurative "
                + "constants a boolean expression admits are ZERO (ZEROS, ZEROES) and ALL literal where the literal is a "
                + "boolean literal (ISO §8.8.2)");
            return BoundBoolError.Refused(ctx.Edition, $"figurative constant '{ConcatFolder.Spelling(fig)}' in a boolean expression "
                + "(ISO §8.8.2)");
        }
        // A sole data reference to a category-boolean item — through the enclosing parentheses, as the routing
        // predicate IsBooleanValueOperand reads it (§8.8.2; kb/Work PB1464).
        if (ConditionOperandExpression(vo) is { } expr && SoleDataRef(expr) is { } dref)
        {
            // The resolver's own answer when it built no place (kb/Work PB1030) — never the 1511 default below,
            // which told a user whose name was undefined (already COBOLNET1639) that it was "not a boolean operand".
            if (host.Expr.ResolveSending(dref) is var r && r.Place is not { } p) return r.BoolError(ctx.Edition);
            // THE ONE category reader (D20/PB79): a ref-mod view's category (GR6), else the item's own picture or a
            // bit group's as-if PICTURE 1(m) — a bit group IS a boolean operand (§13.18.29.4 GR1a).
            var cat = p is RefModPlace rm ? rm.Category : p.Item.OperandPic?.Category;
            if (cat is PicCategory.Boolean) return new BoundBoolRef(p);
            ctx.Edition.Error("COBOLNET1511", $"operand '{DataBinder.WrittenText(dref)}' in a boolean expression is not a "
                + "boolean data item (ISO §8.8.2 — boolean operands only)");
            return BoundBoolError.Refused(ctx.Edition, $"non-boolean operand '{DataBinder.WrittenText(dref)}'");
        }
        // A sole FUNCTION reference whose result is class boolean — §8.8.2's "an identifier referencing a boolean
        // data item": a function-identifier IS an identifier (§8.4.3.1.2) referencing a temporary data item
        // (§8.4.3.2.4 GR1) whose category is the function's type (§15.13.1 BOOLEAN-OF-INTEGER; kb/Work PB68 — it
        // fell to the 1511 default below, "not a valid boolean operand", on legal source).
        if (ConditionOperandExpression(vo) is { } fexpr && SoleFunctionCall(fexpr) is { } fc)
        {
            var bound = host.Intrinsic.BindIntrinsic(fc);
            if (bound is BoundIntrinsicCall { ResultCategory: PicCategory.Boolean } bic) return new BoundBoolCall(bic);
            // A USER-DEFINED function is the same identifier: §8.4.3.2.4 GR1 gives its temporary the description,
            // class and category of the RETURNING item, so a boolean RETURNING item is a boolean data item here
            // exactly as a declared one is (kb/Work PB1419 — the refusal that kept this channel from half-wiring).
            if (bound is BoundNumRef { Place: var resultTemp } && resultTemp.Item.OperandPic?.Category is PicCategory.Boolean)
                return new BoundBoolRef(resultTemp);
            if (bound is BoundExprError err) return BoundBoolError.Carry(err.Feature, err.IsUnbuilt);   // already loud
            ctx.Edition.Error("COBOLNET1511", $"operand '{fc.GetText()}' in a boolean expression is not a "
                + "boolean function — its result is not class boolean (ISO §8.8.2 — boolean operands only)");
            return BoundBoolError.Refused(ctx.Edition, $"non-boolean function operand '{fc.GetText()}'");
        }
        ctx.Edition.Error("COBOLNET1511", $"'{vo.GetText()}' is not a valid boolean operand — a boolean "
            + "expression admits boolean items, boolean literals, and the figurative ZERO / ALL B\"…\" only "
            + "(ISO §8.8.2)");
        return BoundBoolError.Refused(ctx.Edition, $"boolean operand '{vo.GetText()}'");
    }

    /// <summary>True when a <c>comparisonOperand</c> / <c>valueOperand</c> is a BOOLEAN-valued operand — a
    /// boolean expression (B-op tier) OR a sole category-boolean item / boolean literal — so the relation
    /// binder routes it through the boolean channel. (A bare category-boolean item parses as a valueOperand,
    /// not booleanExpression, so this inspects both.)</summary>
    /// <para>⚠ Also read by EVALUATE's Table-15 screen, which DECLINES to classify a boolean operand: §14.9.13.3
    /// SR6 reclassifies one by the SUBJECT and by whether it "results in one boolean character", and guessing
    /// wrong there rejects legal source (fix-queue PB47 — it did, and the gate caught it).</para>
    public bool IsBooleanValueOperand(Core.ValueOperandContext vo)
    {
        var nn = vo.nonNumericLiteral();
        if (nn?.BOOLLIT() is not null) return true;
        if (nn?.figurativeConstant() is { } fig
            && ConcatFolder.FoldAllLiteral(fig, ctx.Data.LiteralEnv, report: null) is { Category: PicCategory.Boolean }) return true;   // ALL B"…" (kb/Work PB71, PB1627)
        // A concatenation expression whose class is boolean (§8.8.3.3 GR1) routes through the boolean channel
        // like the equivalent single B"…" literal it folds to (GR3). ClassOf is diagnostic-free — the fold
        // (and its SR diagnostics) happens exactly once, on the bind path this predicate selects.
        if (nn?.concatenationExpression() is { } ce) return ConcatFolder.ClassOf(ce, ctx.Data.LiteralEnv) is PicCategory.Boolean;
        // Through the enclosing parentheses: "a boolean expression enclosed in parentheses" is a boolean expression
        // (§8.8.2) — `(BW)` is the boolean item BW (kb/Work PB1464; ConditionOperandExpression).
        if (ConditionOperandExpression(vo) is { } expr && SoleDataRef(expr) is { } dref
            && ctx.Refs.Probe(dref) is { } p)   // Probe — a predicate is diagnostic-free (R30)
            // ProbeResult.OperandCategory IS the one category reader for a probed reference (kb/Work PB157 +
            // PB221): §8.4.3.3.4 GR6 for a ref-modified reference, else DataItem.OperandPic, so a bit group
            // routes boolean like the bind path at line ~106 already does. kb/Work PB173 REMOVED the
            // `is not { IsGroup: true }` exclusion that kept a bit-group ref-mod on the general channel: that
            // channel compared the PACKED byte image at bit offsets (a silent wrong answer, e.g.
            // `IF G(1:3) = B"110"` read the three characters of a one-character image), and the slice is now a
            // BitImagePlace in boolean positions, so GR6's unique item really does keep usage bit / boolean.
            return p.OperandCategory is PicCategory.Boolean;
        // A sole FUNCTION-keyword reference to a catalogued BOOLEAN-typed function (§15.2 type 2 — today
        // BOOLEAN-OF-INTEGER): diagnostic-free, from the catalog's declared type (kb/Work PB68).
        if (ConditionOperandExpression(vo) is { } fx && SoleFunctionCall(fx) is { } sfc && sfc.functionName() is { } fn
            && IntrinsicCatalog.TryGet(fn.GetText(), out var fsig) && fsig.Type == IntrinsicType.Boolean)
            return true;
        // …and a sole reference to a USER-DEFINED function whose RETURNING item is category boolean (§8.4.3.2.4 GR1:
        // the temporary's category IS the RETURNING item's; kb/Work PB1419), asked from the same definition the
        // activation binds from, diagnostic-free.
        return vo.arithmeticExpression() is { } ux && SoleFunctionCall(ux)?.functionName() is { } un
            && IsBooleanUserFunction(un.GetText());
    }

    /// <summary>True when <paramref name="written"/> names a REPOSITORY-declared user-defined function (or the
    /// containing function itself) whose RETURNING item is category boolean — the one answer the routing predicate
    /// <see cref="IsBooleanValueOperand"/> reads, from the definition <c>UdfBinder.UdfActivate</c> clones.</summary>
    private bool IsBooleanUserFunction(string written) =>
        ctx.CobolWords.Resolve(written) is { } name
        && host.Intrinsic.ReturningItemOf(name)?.OperandPic?.Category is PicCategory.Boolean;

    /// <summary>The sole <c>functionCall</c> primary of an arithmetic expression (no operators, signs or
    /// parentheses around it), or null — the function-identifier twin of <see cref="SoleDataRef"/>, over the ONE
    /// <see cref="SolePrimary"/> descent.
    /// <para>⛔ IT ALREADY EXISTED, AND THAT IS THE POINT (kb/Work PB172). PB68 wrote it for the BOOLEAN operand
    /// path — "a function-identifier IS an identifier referencing a temporary data item whose category is the
    /// function's type" — which is the identical §15.2 argument the RELATION path needed. The comparand binder
    /// simply never called it, so a sole function comparand went down the expression spine into
    /// <c>BindPrimary</c>'s arithmetic screen. The missing thing was never the helper; it was the fourth member
    /// of <see cref="ComparisonOperandOf"/>'s short-circuit family.</para></summary>
    public static Core.FunctionCallContext? SoleFunctionCall(Core.ArithmeticExpressionContext expr) =>
        SolePrimary(expr)?.functionCall();

    // ⚠ THREE LENGTH QUESTIONS RIDE THIS NODE SHAPE, AND THEY ARE THREE DIFFERENT RULES — do not fold them.
    // §14.9.8.4 GR3's COMPUTE store width counts ITEMS only and is a RUN-TIME quantity, so it is not answered
    // here at all: BooleanRenderer.RenderAtItemWidth carries it beside the value (kb/Work PB589 deleted the
    // bind-time Gr3Width, which counted a run-time-length slice at its inner item's FULL length).
    // BoolResultLength is §8.8.2 rules 9/10's RESULT length, which counts a literal's own positions (§14.9.13.3
    // SR6's "results in one boolean character" turns on it). BoolExprAllLengthOne is §8.8.4.3.3 SR1's "shall
    // reference only boolean items of length 1", a property of EVERY referenced item rather than of the result.
    // The two static ones read an operand's length through ONE reader, StaticBoolRefLength.

    /// <summary>The static width of a boolean-result function reference — BOOLEAN-OF-INTEGER's argument-2 (§15.13.4
    /// r1 "a boolean item of length argument-2") when it is a numeric literal; null when the length is a runtime
    /// value (the §8.8.4.3 SR1 length-1 test and the §14.9.13.3 SR6 test then fail OPEN — no false rejection).</summary>
    private static int? StaticBoolCallWidth(BoundBoolCall c) =>
        c.Call.Sig.Name == "BOOLEAN-OF-INTEGER" && c.Call.Args.Count == 2
        && c.Call.Args[1] is BoundNumericLiteral { Text: { } t }
        // THE ONE integer-literal reader (kb/Work PB1579): an int.TryParse read an 11-digit length as "runtime" and
        // the length-1 tests failed open; saturated, it is a static width that is not 1.
        && CobolNet.Validation.IntegerOperandRules.TryHostValue(t, out int w) ? w : null;

    /// <summary>The COMPILE-TIME length of a boolean item operand, or null when it is a run-time quantity. A
    /// reference-modified operand is the §8.4.3.3.4 GR5 unique data item of the SLICE's positions
    /// (<see cref="RefModPlace.StaticLength"/>) — never its inner item's full length, which is what the deleted
    /// <c>RefModLen</c> substituted for an unknown slice (kb/Work PB589: `IF B(1:N)` over a PIC 1(8) item was
    /// rejected by SR1 as an 8-position operand). OperandPic — a bit group's as-if 1(m) (kb/Work PB157).</summary>
    private static int? StaticBoolRefLength(BoundBoolRef r) =>
        r.Place is RefModPlace rm ? rm.StaticLength(rm.Inner.Item.OperandPic?.Length) : r.Place.Item.OperandPic?.Length;

    private static string FlipBits(string bits)
    {
        var arr = bits.ToCharArray();
        for (int i = 0; i < arr.Length; i++) arr[i] = arr[i] == '1' ? '0' : '1';
        return new string(arr);
    }

    /// <summary>Bind a SIMPLE boolean condition (ISO §8.8.4.3): a bare boolean expression used as a condition,
    /// true iff its value is boolean 1 (GR1). SR1 (:9810) — every referenced boolean item AND literal shall be
    /// of length 1; a wider operand is COBOLNET1511. Called from the sole-operand comparison path.</summary>
    private BoundCondition BindSimpleBooleanCondition(BoundBoolExpr expr)
    {
        if (!BoolExprAllLengthOne(expr))
            ctx.Edition.Error("COBOLNET1511", "a simple boolean condition shall reference only boolean items "
                + "and literals of length 1 (ISO §8.8.4.3.3 SR1)");
        return new BoundBooleanCondition(expr);
    }

    /// <summary>SR1 check: every ITEM and LITERAL in the boolean expression has length 1 (§8.8.4.3 SR1). ALL
    /// figuratives are positionless (they materialize to length 1 against a length-1 sibling) — they pass.</summary>
    private static bool BoolExprAllLengthOne(BoundBoolExpr e) => e switch
    {
        BoundBoolLiteral l => l.Bits.Length == 1,
        // A run-time-length slice fails OPEN, exactly as a run-time-length function result does below: SR1 is a
        // syntax rule and its length is unknowable at compile time (kb/Work PB589).
        BoundBoolRef r => StaticBoolRefLength(r) is { } len ? len == 1 : r.Place is RefModPlace,
        BoundBoolBinary b => BoolExprAllLengthOne(b.Left) && BoolExprAllLengthOne(b.Right),
        BoundBoolNot n => BoolExprAllLengthOne(n.Operand),
        BoundBoolShift s => BoolExprAllLengthOne(s.Operand),   // shift preserves length (rule 9)
        BoundBoolCall c => (StaticBoolCallWidth(c) ?? 1) == 1,   // a runtime-length result fails open (kb/Work PB68)
        BoundBoolAll => true,   // positionless — materializes to the sibling's length
        _ => true,              // error nodes already reported
    };

    /// <summary>Bind the boolExprAhead()-gated <c>primaryCondition</c> boolean alternative (ISO §8.8.4.2.2
    /// relation / §8.8.4.3 simple condition): <c>booleanExpression (comparisonOperator booleanExpression)?</c>.
    /// Only an expression that ACTUALLY contains a B-operator uses the boolean channel — the predicate
    /// guarantees at least one B-op somewhere, but an INDIVIDUAL relation operand may still be B-op-free (e.g.
    /// the RHS literal in `(a B-AND b) = c`), so each side unwraps to a normal operand when it has no B-op.</summary>
    private BoundCondition BindPrimaryBoolean(Core.BooleanExpressionContext[] be, Core.ComparisonOperatorContext? opCtx, AbbrevCarry carry)
    {
        carry.Reset();
        // The COBOL-2002 boolean-operator introduction gate (BooleanOperators2002) fires on RECOGNITION in the
        // VersionConformancePass parse-arm (VisitPrimaryCondition, be.Any(HasBoolOp) at the same altitude); Step
        // 14h.4b. A B-op-free relation operand rides its own channel here regardless.
        if (opCtx is not null && be.Length >= 2)
        {
            BoundOperand left = BindBoolOrValueOperand(be[0]);
            string op = MapOperator(opCtx.GetText());
            BoundOperand right = BindBoolOrValueOperand(be[1]);
            // A relation of boolean expressions is a boolean relation (§8.8.4.2.2 Format 2): no abbreviation follows
            // it (§8.8.4.12.3 SR1; kb/Work PB1391). The carry holds no subject — the relation is not one an
            // abbreviation may insert from — and says why, so the refusal names the rule.
            carry.BooleanRelation = true;
            // A boolean relation (§8.8.4.2.2 Format 2): equality-only, both operands boolean-valued.
            if (left is BoundBoolOperand || right is BoundBoolOperand)
            {
                if (op is not ("==" or "!="))
                    ctx.Edition.Error("COBOLNET1511", "a boolean relation admits only [NOT] EQUAL / '=' / '<>' "
                        + "(ISO §8.8.4.2.2 Format 2 — no ordering is defined for boolean values)");
                else if (!BoolValued(left) || !BoolValued(right))
                    ctx.Edition.Error("COBOLNET1511", "both operands of a boolean relation shall be "
                        + "boolean-valued (ISO §8.8.4.2.2)");
            }
            return WrittenRelational(left, op, right);
        }
        // A bare boolean expression ⇒ a simple boolean condition (§8.8.4.3).
        if (HasBoolOp(be[0])) return BindSimpleBooleanCondition(BindBoolExpr(be[0]));
        // Defensive: a B-op-free bare operand (the predicate should have excluded it) unwraps to the normal
        // sole-operand resolution.
        var vo = UnwrapBareBool(be[0]);
        return BindSoleOperandCondition(vo, () => vo is not null ? ComparisonOperandOf(vo) : BoundOperandError.Refused(ctx.Edition, "operand"), carry);
    }

    /// <summary>Bind a boolean-expression RELATION operand: a real boolean expression (contains a B-op) →
    /// <see cref="BoundBoolOperand"/>; a B-op-free operand → its normal comparison-operand binding (so a plain
    /// boolean-item / literal / numeric operand rides its existing channel).</summary>
    private BoundOperand BindBoolOrValueOperand(Core.BooleanExpressionContext ctx) =>
        HasBoolOp(ctx) ? new BoundBoolOperand(BindBoolExpr(ctx)) : ComparisonOperandOf(UnwrapBareBool(ctx));

    /// <summary>True when a bound relation operand is of CLASS boolean — <see cref="BoolValued"/> without figurative
    /// ZERO, which is boolean only BY CONTEXT (§8.3.3.6.4 GR4: <c>N = ZERO</c> over a numeric item is a general
    /// relation, <c>B = ZERO</c> a boolean one). §8.8.4.2.1 calls a relation "involving operands of class boolean" a
    /// boolean relation condition, so this is the question that seeds <see cref="AbbrevCarry.BooleanRelation"/>.</summary>
    private static bool IsBooleanClassOperand(BoundOperand o) => BoolValued(o) && o is not BoundFigurative;

    /// <summary>True when a bound relation operand is BOOLEAN-VALUED (a boolean expression, a category-boolean
    /// item incl. ref-mod, a boolean literal, or figurative ZERO — boolean by context, §8.3.3.6.4 GR4).</summary>
    private static bool BoolValued(BoundOperand o) => o switch
    {
        BoundBoolOperand => true,
        BoundStringLiteral { Category: PicCategory.Boolean } => true,
        BoundFigurative { Kind: 'Z' } => true,
        // OperandPic / rm.Category — THE ONE category reader (kb/Work PB157): a GROUP-USAGE BIT item is
        // boolean-valued in a relation too (§13.18.29.4 GR1a/b). kb/Work PB173 REMOVED the
        // `is not { IsGroup: true }` exclusion (the twin of the one in the bind-path predicate above): the
        // "both sides byte-image, consistent" justification described the CHANNEL, not the ANSWER — the
        // general channel sliced the PACKED image at bit offsets and compared the wrong characters. With the
        // slice in boolean positions (BitImagePlace, §8.4.3.3.4 GR5a) the boolean channel is simply correct.
        BoundFieldOperand { Place: RefModPlace rm } => rm.Category is PicCategory.Boolean,
        BoundFieldOperand f => f.Place.Item.OperandPic?.Category is PicCategory.Boolean,
        _ => false,
    };

    /// <summary>True when a boolean-expression subtree contains any boolean-operator token — the discriminator
    /// between a genuine boolean expression and a bare operand parsed through the booleanExpression rule. The
    /// token set is <see cref="BooleanOperatorTokens"/>, the ONE list the parser's look-ahead predicates and the
    /// introduction gate read too: it named only B-AND / B-OR / B-XOR / B-NOT, so a SHIFT-only expression
    /// (<c>IF A B-SHIFT-L 1 = B"1000"</c>) was taken for a bare operand and refused (kb/Work PB1412).</summary>
    private static bool HasBoolOp(Antlr4.Runtime.Tree.IParseTree t) => BooleanOperatorTokens.ContainsOperator(t);

    /// <summary>The single leaf <c>valueOperand</c> of a B-op-FREE boolean expression (walking single-child
    /// tiers and paren groups), for re-binding as a normal operand; null if the shape is not a bare operand.</summary>
    /// <remarks>THE discriminator for every <c>{boolExprAhead()}?</c>-gated alternative, not just this one:
    /// the predicate's scan is condition-shaped and may fire on a B-operator that belongs elsewhere in the
    /// statement, so a parse tree reaching a boolean alternative is not proof of a boolean expression. A
    /// non-null result means "this operand carries NO boolean operator and is a bare operand" — reduce it and
    /// bind it through the ordinary channel. Also read by <c>OoBinder</c>'s INVOKE BY CONTENT operand
    /// normalization (fix-queue PB46).</remarks>
    internal static Core.ValueOperandContext? UnwrapBareBool(Core.BooleanExpressionContext ctx)
    {
        var xor = ctx.booleanXorTerm();
        if (xor.Length != 1) return null;
        var and = xor[0].booleanAndTerm();
        if (and.Length != 1) return null;
        var shift = and[0].booleanShiftTerm();
        // A bare operand has a single shift term with NO shift suffix (a shift op means it is a real expression).
        if (shift.Length != 1 || shift[0].booleanShiftSuffix().Length != 0) return null;
        return UnwrapFactor(shift[0].booleanFactor());
    }

    private static Core.ValueOperandContext? UnwrapFactor(Core.BooleanFactorContext f)
    {
        if (f.valueOperand() is { } vo) return vo;
        if (f.booleanExpression() is { } inner) return UnwrapBareBool(inner);   // ( … ) group
        return null;   // B-NOT factor — has a B-op, never reaches here
    }

    /// <summary>The carried subject + relational operator for ABBREVIATED COMBINED RELATION CONDITIONS (ISO §8.8.4.12).
    /// In a paren-free sequence of relations joined by AND/OR/XOR, a succeeding relation may omit the subject (operator
    /// stated, e.g. the <c>&lt; C</c> in <c>A &gt; B OR &lt; C</c>) or the subject AND operator (a bare operand, e.g.
    /// <c>A = B AND C</c> ≡ <c>A = C</c>). GR1 inserts the last STATED subject and the last STATED operator.
    /// <see cref="Subject"/> is set only by a fully-stated relation; <see cref="Op"/> by a full OR an abbreviated
    /// relation. A complete non-relational simple condition (class / sign / condition-name / parenthesized) terminates
    /// the insertion. Threaded left-to-right (source order) as a mutable holder.</summary>
    private sealed class AbbrevCarry
    {
        public BoundOperand? Subject;
        public string? Op;
        /// <summary>ISO §14.9.13.3 SR8 — the EVALUATE SELECTION SUBJECT that the leftmost, elided portion of a
        /// partial-expression is spliced from ("condition-2 is the conditional expression that results from
        /// preceding partial-expression-1 by the selection subject"). Null in every ordinary condition context,
        /// and consumed — set back to null — by the ONE leading <c>partialComparison</c> that reads it, because
        /// SR5 elides the LEFTMOST portion only: everything after it is an ordinary §8.8.4.12 tail already
        /// served by <see cref="Subject"/>. Deliberately NOT cleared by <see cref="Reset"/>, which models the
        /// abbreviation terminating mid-sequence — a thing that cannot have happened before the leading
        /// portion is bound.</summary>
        public PartialSubjectOperand? PartialSubject;
        /// <summary>⛔ ISO §8.8.4.12.3 SR1 — "Relation-condition-1 shall not be a boolean relation condition": the
        /// relation the sequence's abbreviations would inherit from (the last STATED relation, §8.8.4.12.4 GR1) involves
        /// operands of class boolean (§8.8.4.2.1 — "A relation condition involving operands of class boolean is a
        /// boolean relation condition"), so no succeeding relation may be abbreviated after it. It is a property of
        /// the relation's CATEGORY, set where the carry is seeded — never a by-product of which parse path bound the
        /// relation (kb/Work PB1391: <c>B1 = B2 OR &lt;&gt; B3</c> rides the ordinary comparison arm and was silently
        /// expanded). Cleared, like the subject and the operator, when a complete non-relational simple condition
        /// ends the insertion.</summary>
        public bool BooleanRelation;
        public void Reset() { Subject = null; Op = null; BooleanRelation = false; }
    }

    public BoundCondition BindCondition(IParseTree node) => BindCondition(node, new AbbrevCarry());

    /// <summary>⛔ ISO §14.9.13.3 SR8 — THE partial-expression rewrite, and the only place it happens: "If a
    /// selection object is specified by partial-expression-1, that selection object is treated as though it were
    /// specified as condition-2, where condition-2 is the conditional expression that results from preceding
    /// partial-expression-1 by the selection subject." §14.9.13.4 GR4 a) 2. then evaluates that expression and
    /// makes its truth value the result of the pair's analysis, and SR8's second sentence ("the corresponding
    /// selection subject is treated as though it were specified by the word TRUE") is what makes the pair's term
    /// the condition ITSELF rather than a comparison against the subject — so the caller returns this unchanged.
    /// <para>⛔ THE REWRITE IS THE WHOLE IMPLEMENTATION (kb/Work PB398). SR5 (the definition), SR7 d) (the
    /// well-formedness test — "were it preceded by the corresponding selection subject, a conditional expression
    /// would result") and GR4 a) 2. (the semantics) are not three checks: splice the subject in and either an
    /// ordinary condition results or the ordinary condition binder refuses it, with the ordinary diagnostic. The
    /// splice IS §8.8.4.12's subject insertion — the leading relational operator's missing operand is carried in
    /// exactly the field a later abbreviated relation reads — which is why <c>WHEN &gt; 5 AND &lt; 10</c> works
    /// with no code of its own: SR7 d) licenses it and §8.8.4.12.4 GR1 already knew how.</para></summary>
    /// <param name="pe">The partial-expression parse node.</param>
    /// <param name="subject">The corresponding selection subject (§14.9.13.3 SR7 — "the selection subject having
    /// the same ordinal position"), ALREADY BOUND by the caller — see <see cref="PartialSubjectOperand"/>.</param>
    /// <remarks><paramref name="pe"/> is a <c>partialExpression</c>, or a <c>condition</c> whose leftmost leaf is a
    /// bare class-name (<see cref="LeadingBareClassWord"/>) — the same rewrite over the other parse of SR5's
    /// class shape (kb/Work PB843).</remarks>
    public BoundCondition BindPartialExpression(ParserRuleContext pe, PartialSubjectOperand subject) =>
        BindCondition(pe, new AbbrevCarry { PartialSubject = subject });

    /// <summary>The selection subject SR8 splices into a partial-expression, as the ONE value §14.9.13.4 GR3
    /// assigned it "at the beginning of the execution of the EVALUATE statement" — never re-bound from its parse
    /// node (kb/Work PB912's sibling). The rewrite used to take the NODE and bind it again inside every
    /// partial-expression object, so a subject was evaluated once more PER WHEN on top of the statement's own
    /// evaluation: measured, <c>EVALUATE FUNCTION CNT(1) WHEN &gt; 5 … WHEN = 1 …</c> over a user function
    /// returning its activation count activated it FIVE times and selected WHEN OTHER, a branch no single subject
    /// value selects — and the residue stage meant to refuse that shape never fired, because each object's own
    /// per-evaluation attachment had already drained the activations it counted.
    /// <para><paramref name="Value"/> is the subject's assigned value (its intermediate result item when the
    /// statement reads it more than once) and serves the relation and sign shapes. <paramref name="Content"/>
    /// serves the CLASS shape, which tests CHARACTER CONTENT (§8.8.4.4): a data item is read in place, because
    /// its numeric intermediate would normalize the very content <c>IS NUMERIC</c> exists to find invalid; a
    /// computed subject is read from its one materialization. <paramref name="Node"/> remains only for the
    /// syntactic facts a bound operand no longer carries (§8.8.4.7.3 Format 2's "bare standard floating-point
    /// data-name") and for the SR1 diagnostic path of a sign test on a non-numeric subject.</para></summary>
    public readonly record struct PartialSubjectOperand(Core.ValueOperandContext? Node, BoundOperand Value, BoundOperand Content);

    private BoundCondition BindCondition(IParseTree node, AbbrevCarry carry) => node switch
    {
        Core.ConditionContext c => BindCondition(c.GetChild(0), carry),
        // The condition tiers (§8.8.4.9; NOT > AND > XOR > OR, §8.8.4.11.3), in their LEADING spelling (the first
        // simple condition, never abbreviated) and their SUCCEEDING spelling (every connective's operand, whose
        // leftmost element may be §8.8.4.12's abbreviated relation — kb/Work PB1390). Both fold with the ONE
        // sequence binder, which threads the abbreviation carry through the whole sequence in source order.
        Core.LogicalOrExpressionContext orExpr => BindFlatSequence(orExpr, "||", carry),
        Core.LogicalXorExpressionContext xorExpr => BindFlatSequence(xorExpr, "^", carry),
        Core.LogicalAndExpressionContext andExpr => BindFlatSequence(andExpr, "&&", carry),
        Core.SucceedingXorExpressionContext sXor => BindFlatSequence(sXor, "^", carry),
        Core.SucceedingAndExpressionContext sAnd => BindFlatSequence(sAnd, "&&", carry),
        Core.SucceedingConditionContext sc => BindCondition(sc.GetChild(0), carry),
        // §14.9.13.3 SR5's spine — the SAME three logical tiers with only the leading element elided, so they
        // fold with the SAME sequence binder and inherit its short-circuit / user-function cardinality rules.
        Core.PartialExpressionContext pOr => BindFlatSequence(pOr, "||", carry),
        Core.PartialXorExpressionContext pXor => BindFlatSequence(pXor, "^", carry),
        Core.PartialAndExpressionContext pAnd => BindFlatSequence(pAnd, "&&", carry),
        Core.PartialComparisonContext pc => BindPartialComparison(pc, carry),
        Core.UnaryLogicalExpressionContext u => u.NOT() is not null
            ? new BoundNot(BindCondition(u.primaryCondition(), carry)) : BindCondition(u.primaryCondition(), carry),
        Core.AbbreviatedRelationContext ar => BindAbbreviatedRelation(ar, carry),
        Core.PrimaryConditionContext p => BindPrimary(p, carry),
        _ => Refused("unsupported condition form"),
    };

    /// <summary>The LEFTMOST, elided portion of a partial-expression (ISO §14.9.13.3 SR5): the selection subject
    /// is supplied as the operand the written form omits, and the result is the ordinary condition that rule's
    /// three shapes name — a relation, a class condition, or a sign condition. Each shape is bound by the SAME
    /// body the fully-written form uses (<see cref="BindClassConditionOn"/> / <see cref="BindSignConditionOn"/> /
    /// <see cref="BindAbbreviatedRelation"/>), so no rule attached to any of them — §8.8.4.4.3's boolean-operand
    /// guard, §8.8.4.7.3 SR1's operand closure, the LOCALE-alphabet refusal, the §8.8.4.2 relation band — has a
    /// second copy that could drift from the written one.</summary>
    private BoundCondition BindPartialComparison(Core.PartialComparisonContext pc, AbbrevCarry carry)
    {
        var subject = carry.PartialSubject;
        carry.PartialSubject = null;   // SR5 elides the LEFTMOST portion only
        bool not = pc.NOT() is not null;
        if (subject is not { } subj) return Refused("partial-expression with no selection subject");
        if (pc.classConditionName() is { } cls) return BindClassConditionOn(cls, not, () => subj.Content, carry);
        if (pc.POSITIVE() is not null || pc.NEGATIVE() is not null || pc.ZERO() is not null)
        {
            char kind = pc.POSITIVE() is not null ? 'P' : pc.NEGATIVE() is not null ? 'N' : 'Z';
            // A numeric subject value is the sign test's operand as it stands; anything else is refused by the
            // ONE sign body's §8.8.4.7.3 SR1 diagnostic, over the written node (a failing compile — no second
            // activation can run).
            if (SignOperandOf(subj.Value) is not { } signExpr)
            {
                // A boolean-expression selection subject (kb/Work PB1412) has no operand node: it is a boolean
                // value, never the numeric operand §8.8.4.7.3 SR1 admits, so the sign shape is refused by name.
                if (subj.Node is null)
                {
                    carry.Reset();
                    ctx.Edition.Error(DiagnosticCatalog.OperandIsNotACondition,
                        "the selection subject is a boolean expression, and a sign condition's operand shall be \"any "
                        + "single numeric data item described with a usage other than a standard floating-point usage, "
                        + "or any form of arithmetic expression\" (ISO §8.8.4.7.3 SR1; §14.9.13.3 SR8 splices the "
                        + "subject into the partial-expression)");
                    return Refused("sign condition over a boolean-expression selection subject");
                }
                return BindSignConditionOn(kind, not, subj.Node, carry);
            }
            carry.Reset();
            return new BoundSignCondition(signExpr, kind, not, IsFormat2FloatSign(subj.Node));
        }
        // The relational shape. SR8's splice and §8.8.4.12.4 GR1's subject insertion are the SAME operation, so
        // the subject is seeded as the carried subject and the ordinary abbreviated-relation arm does the rest —
        // which is also what carries it on to a following `AND < 10`.
        carry.Subject = subj.Value;
        var spliced = BindAbbreviatedRelation(pc.abbreviatedRelation(), carry);
        // The spliced relation IS relation-condition-1 (SR8: the subject precedes the partial-expression), so it is
        // the NEXT abbreviation that §8.8.4.12.3 SR1 refuses after a boolean one — set after this one is bound.
        carry.BooleanRelation = IsBooleanClassOperand(subj.Value);
        return spliced;
    }

    /// <summary>Bind a left-to-right logical sequence (an OR / XOR / AND chain, leading or succeeding), threading
    /// the abbreviation <paramref name="carry"/> through every operand in SOURCE ORDER so a later abbreviated relation
    /// sees the subject / operator an earlier one established. A lone operand returns its own condition (no wrapper).
    /// A user-function reference in a NON-FIRST operand of an AND/OR chain is CONDITIONALLY evaluated
    /// (§8.8.4.13 r1 — evaluation stops when the hierarchical level's truth value is determined; r2 — functions
    /// are evaluated "if and when the conditions containing them are evaluated"), so that operand's activations
    /// attach to the operand itself (<c>UdfAttachPerEvaluation</c>) and run only when C#'s matching
    /// short-circuit reaches it. The FIRST operand always evaluates — its activations stay statement-hoisted
    /// (exact) unless an ENCLOSING repeated window drains them. XOR is exempt: both operands are always
    /// required (§8.8.4.9), so a hoist is exact for every XOR operand.</summary>
    private BoundCondition BindFlatSequence(IParseTree ctx, string op, AbbrevCarry carry)
    {
        var parts = new List<BoundCondition>();
        for (int i = 0; i < ctx.ChildCount; i++)
        {
            var ch = ctx.GetChild(i);
            // The AND / OR connective tokens, and the one xorOperator node (XOR / EXCLUSIVE-OR — kb/Work PB1390).
            if (ch is ITerminalNode or Core.XorOperatorContext) continue;
            var udfMark = host.Udf.Mark;
            parts.Add(BindCondition(ch, carry));
            if (parts.Count > 1 && op != "^")
                parts[^1] = host.Udf.UdfAttachPerEvaluation(parts[^1], udfMark);
        }
        return parts.Count == 1 ? parts[0] : new BoundLogical(op, parts);
    }

    private BoundCondition BindPrimary(Core.PrimaryConditionContext p, AbbrevCarry carry)
    {
        // COBOL-2002 boolean forms (the boolExprAhead()-gated primaryCondition alt) — a boolean relation
        // (§8.8.4.2.2) or a simple boolean condition (§8.8.4.3).
        if (p.booleanExpression() is { Length: > 0 } be) return BindPrimaryBoolean(be, p.comparisonOperator(), carry);
        if (p.comparisonExpression() is { } cmp) return BindComparison(cmp, carry);
        if (p.condition() is { } inner)
        {
            // A parenthesized condition is a complete simple condition: a FRESH abbreviation scope inside, and the
            // insertion terminates for the enclosing sequence (ISO §8.8.4.12.4 GR1).
            var bound = BindCondition(inner, new AbbrevCarry());
            carry.Reset();
            return bound;
        }
        // The one alternative left is `booleanLiteral`: the WORDS TRUE and FALSE. They are no condition —
        // §8.8.4.2.1's list of the simple conditions has no member that is a bare truth word — and they are
        // written only as an EVALUATE selection subject or selection object (§14.9.13.3 SR7 b); GR3 f), GR4 a) 4.).
        // EVALUATE decides its own TRUE/FALSE pairs by Table 15 and never binds one as a condition
        // (EvaluateBinder.BindWhenItem), so a word that reaches here is in an IF, a PERFORM UNTIL, a SEARCH WHEN or
        // an operand of NOT / AND / OR / XOR, where the standard has no meaning for it (kb/Work PB1668).
        var truthWord = p.booleanLiteral().GetText().ToUpperInvariant();
        ReportNotACondition(truthWord, $"the word {truthWord}, which is written only as an EVALUATE selection subject or "
            + "selection object (ISO §14.9.13.3 SR7 b)");
        return Refused($"condition '{truthWord}'");
    }

    /// <summary>An abbreviated relation with the subject omitted (<c>comparisonOperator comparisonOperand</c>): the
    /// carried subject is inserted and the newly-stated operator becomes the carried operator (ISO §8.8.4.12.4 GR1).
    /// With no carried subject — GR1's insertion "terminates once a complete simple condition is encountered", and a
    /// parenthesized group opens a fresh scope — the relation has nothing to insert: COBOLNET2552 (kb/Work PB1390;
    /// it used to be refused unreported, which the COBOLNET2319 internal-error net caught).</summary>
    private BoundCondition BindAbbreviatedRelation(Core.AbbreviatedRelationContext ar, AbbrevCarry carry)
    {
        if (carry.BooleanRelation) return RefuseAbbreviationAfterBooleanRelation(DataBinder.WrittenText(ar));
        if (carry.Subject is not { } subject)
        {
            ctx.Edition.Error(DiagnosticCatalog.AbbreviatedRelationWithoutSubject,
                $"'{DataBinder.WrittenText(ar)}' omits the subject of its relation, but no subject precedes it to insert: the "
                + "insertion of an omitted subject terminates at a complete simple condition that is not a relation, and "
                + "does not cross a parenthesis (ISO §8.8.4.12.1; §8.8.4.12.4 GR1)");
            return Refused("abbreviated relation with no preceding relation subject");
        }
        string op = MapOperator(ar.comparisonOperator().GetText());
        carry.Op = op;
        return WrittenRelational(subject, op, ComparisonOperand(ar.comparisonOperand()));
    }

    /// <summary>⛔ ISO §8.8.4.12.3 SR1 — a succeeding relation that omits its subject (or its subject and operator)
    /// after a BOOLEAN relation condition (<see cref="AbbrevCarry.BooleanRelation"/>). Both consumers of the carry —
    /// <see cref="BindAbbreviatedRelation"/> and the bare object of <see cref="BindSoleOperandCondition"/> — ask it
    /// here, so the rule is reported once, in one wording (kb/Work PB1391).</summary>
    private BoundCondition RefuseAbbreviationAfterBooleanRelation(string written)
    {
        ctx.Edition.Error(DiagnosticCatalog.AbbreviatedRelationAfterBooleanRelation,
            $"'{written}' abbreviates a relation after a boolean relation condition, and the first relation condition of "
            + "an abbreviated combined relation condition shall not be a boolean relation condition (ISO §8.8.4.12.3 SR1; "
            + "a relation involving operands of class boolean is a boolean relation, §8.8.4.2.1)");
        return Refused($"abbreviated relation '{written}' after a boolean relation");
    }

    /// <summary>⛔ THE ONE ISO §8.8.4.4.3 OPERAND SCREEN for a class condition — SR1, which names EVERY
    /// alternative, and then the rules that name THIS alternative, read off
    /// <see cref="ClassConditionModel"/>'s table in its own order (category rules before usage rules, so an
    /// operand breaking both is reported against the rule that speaks about its category). The FIRST broken
    /// rule is reported and the screen stops: one condition, one diagnostic.
    /// <para>⛔ IT USED TO BE THE BOOLEAN-CATEGORY SCREEN ONLY (kb/Work PB571). After the strong-group arm it
    /// read <c>if (pic is not { Category: PicCategory.Boolean }) return;</c> — so SR1's class list was never
    /// asked of ANY operand, and <c>IF IX IS NUMERIC</c> over a USAGE INDEX item compiled clean and printed
    /// TRUE. An index data item's storage profile carries category NUMERIC (the occurrence number is a number),
    /// which is exactly why the class question may not be answered from the storage category:
    /// §13.18.60.4 GR10, "The class and category of an index data item are index". SR3 and SR5 had no arm at
    /// all, and SR4's arm tested one of the three categories the rule names.</para></summary>
    public void CheckClassConditionOperand(BoundOperand op, char kind)
    {
        // §8.8.4.4.3 SR1 (data-model D17): a strongly-typed group item may not appear in a class condition — it has
        // its own unique class and category (the type-name), not one of the general classes a class condition tests.
        // This arm keeps the COBOLNET1533 strong-typing family's own code; SR1's other two arms are COBOLNET2200.
        if (op is BoundFieldOperand fg && StrongTypeModel.IsStrongGroup(fg.Place.Item))
        {
            ctx.Edition.Error(DiagnosticCatalog.StrongClassCondition, "a strongly-typed group item may not appear in a class condition — "
                + "it has its own unique class and category (ISO §8.8.4.4.3 SR1)");
            return;
        }
        if (ClassConditionModel.Sr1Refusal(op) is { } why)
        {
            ctx.Edition.Error(DiagnosticCatalog.ClassConditionOperandClass,
                $"a class condition's identifier-1 shall not reference {why} (ISO §8.8.4.4.3 SR1)");
            return;
        }
        if (ClassConditionModel.For(kind) is not { } alt) return;
        // The operand's CATEGORY through THE ONE operand-category reader, and its USAGE off its item's OperandPic.
        // Both are the OPERAND's, never a raw Pic: a GROUP-USAGE BIT operand IS category boolean with as-if usage
        // bit (§13.18.29.4 GR1a/b — raw Pic let `IF BIT-GROUP IS NUMERIC/ALPHABETIC` compile while the elementary
        // twin was rejected, kb/Work PB157's sweep), and a reference-modified slice has its ITEM's usage but the
        // category §8.4.3.3.4 GR6 c) gives it — alphanumeric (national when the usage is national) — so
        // `IF NUM (1:2) IS ALPHABETIC` is not an SR4 violation and `IF NUM (1:2) IS NEAREST-TO-ZERO` IS an SR6
        // one (kb/Work PB823, whose renderer half reads the same reader).
        // ⛔ EVERY OPERAND SHAPE IS CLASSIFIED, NOT ONLY A DATA-ITEM REFERENCE (kb/Work PB1401). §8.5.2.12 items
        // 3–7 make LINAGE-COUNTER, LINE-COUNTER, PAGE-COUNTER and every numeric / integer function a category-numeric
        // data item, and they bind as COMPUTED operands; the screen used to hand them a null category, so SR4–SR6
        // failed open over them and `IF FUNCTION UPPER-CASE(A) IS NEAREST-TO-ZERO` reached a renderer that has no
        // numeric description to test. The one reader already answers them (numeric for a register, the §15.2
        // type for a function), and the renderer asks the same reader, so the two halves cannot disagree.
        PicCategory? category = IntrinsicResultType.OperandCategory(op);
        Usage? usage = op switch
        {
            BoundFieldOperand { Place: RefModPlace rm } => rm.Inner.Item.OperandPic?.Usage,
            BoundFieldOperand f => f.Place.Item.OperandPic?.Usage,
            // §15.2 items 1–3: an alphanumeric function has "an implicit usage display", a boolean function "an
            // implicit usage bit", a national function "an implicit usage national"; items 4–5 give a numeric or
            // integer function none, and SR3's function sentence covers it by type below.
            BoundComputedOperand { Expr: BoundIntrinsicCall ic } => ic.ResultCategory switch
            {
                PicCategory.Alphanumeric => Usage.Display,
                PicCategory.Boolean => Usage.Bit,
                PicCategory.National => Usage.National,
                _ => null,
            },
            _ => null,
        };
        bool function = op is BoundComputedOperand { Expr: BoundIntrinsicCall };
        foreach (var rule in alt.Rules)
            if (ClassConditionModel.Violates(rule, category, usage, function))
            {
                var (code, clause, text) = ClassConditionModel.Wording(rule);
                ctx.Edition.Error(code, $"the {alt.Spelling} class condition over '{OperandName(op)}': {text} ({clause})");
                return;
            }
    }

    /// <summary>The operand's COBOL spelling for a class-condition diagnostic, or a neutral phrase for an
    /// operand that is not a data-item reference (a function result — §8.8.4.4.3 SR3's second sentence).</summary>
    private static string OperandName(BoundOperand op) =>
        op is BoundFieldOperand f ? f.Place.Item.CobolName ?? "identifier-1" : "identifier-1";

    /// <summary>⛔ THE ONE class-condition body (ISO §8.8.4.4), over an operand the CALLER names. THREE callers
    /// ask the same question about different spellings of the same condition: <see cref="BindComparison"/> for
    /// the written form <c>identifier-1 IS [NOT] class</c>, <see cref="BindPartialComparison"/> for
    /// §14.9.13.3 SR5's "class condition without the identifier", and <see cref="BindClassCondition"/> for the
    /// EVALUATE selection subject's own class test. Extracted rather than copied because FIVE rules live in
    /// it — the §8.8.4.4.3 operand screen, the SR2 LOCALE-alphabet refusal, the §12.3.7 user-class membership
    /// and the §8.8.4.4.4 GR3 a) coded-set class — and a second copy is five chances for the spellings to
    /// diverge. It WAS copied: <c>EvaluateBinder.SubjectAsCondition</c> carried its own kind decode over its
    /// own grammar rule, so the EVALUATE subject had no BOOLEAN, no class-name and no alphabet-name arm and
    /// offered an ALPHANUMERIC one the general format does not print (kb/Work PB590).
    /// <para><paramref name="operand"/> is a thunk so the operand is bound only on the arms that reach it: the
    /// LOCALE refusal reports a rule about the CLASS-NAME and must not also drag the operand's own diagnostics
    /// into the same statement.</para></summary>
    public BoundCondition BindClassCondition(Core.ClassConditionNameContext cls, bool not, System.Func<BoundOperand> operand) =>
        BindClassConditionOn(cls, not, operand, new AbbrevCarry());

    private BoundCondition BindClassConditionOn(Core.ClassConditionNameContext cls, bool not,
        System.Func<BoundOperand> operand, AbbrevCarry carry)
    {
        carry.Reset();   // a class condition is a complete simple condition — terminates the abbreviation
        // The §8.8.4.4.2 alternatives this compiler offers, tagged by ClassConditionModel's own kind constants —
        // the table that also carries each alternative's §8.8.4.4.3 operand rules, so a new alternative cannot
        // arrive here without arriving there.
        char? kind = cls.NUMERIC() is not null ? ClassConditionModel.Numeric
            : cls.ALPHABETIC() is not null ? ClassConditionModel.Alphabetic
            : cls.ALPHABETIC_UPPER() is not null ? ClassConditionModel.AlphabeticUpper
            : cls.ALPHABETIC_LOWER() is not null ? ClassConditionModel.AlphabeticLower
            : cls.BOOLEAN() is not null ? ClassConditionModel.Boolean
            // The seven COBOL-2014 numeric-content alternatives (§8.8.4.4.4 GR3 g)–m); kb/Work PB225). Keywords
            // at 2014+ only — below that the spelling is a SPECIAL-NAMES class-name (the edition check below).
            : cls.FARTHEST_FROM_ZERO() is not null ? ClassConditionModel.FarthestFromZero
            : cls.FLOAT_INFINITY() is not null ? ClassConditionModel.FloatInfinity
            : cls.FLOAT_NOT_A_NUMBER() is not null ? ClassConditionModel.FloatNotANumber
            : cls.FLOAT_NOT_A_NUMBER_QUIET() is not null ? ClassConditionModel.FloatNotANumberQuiet
            : cls.FLOAT_NOT_A_NUMBER_SIGNALING() is not null ? ClassConditionModel.FloatNotANumberSignaling
            : cls.IN_ARITHMETIC_RANGE() is not null ? ClassConditionModel.InArithmeticRange
            : cls.NEAREST_TO_ZERO() is not null ? ClassConditionModel.NearestToZero
            : null;
        // ⛔ A KEYWORD ALTERNATIVE IS ITS KEYWORD ONLY WHERE §8.9 RESERVES THE WORD (kb/Work PB655). BOOLEAN (2002)
        // and the seven 2014 words used to be unreachable here below their edition because the reservation gate
        // was a cobolWord PREDICATE and cobolWord comes first; the gate is token-level now, so a DECLARED class of
        // that spelling arrives as an IDENTIFIER, and a keyword token that still reaches this rule at an edition
        // where §8.9 leaves the word free is an UNDECLARED class-name-1 / alphabet-name-1 reference — §8.8.4.4.2
        // offers no other reading there. It falls to the name arms below and draws their §8.4.2.1 COBOLNET1639
        // (`IF X IS BOOLEAN` at 85 used to compile clean for a moment on this branch — measured).
        string? userWord = cls.cobolWord()?.GetText();
        if (kind is not null && cls.Start.Text.ToUpperInvariant() is var kw
            && !Editions.ReservedWordSet.Default.RejectsAt(kw, ctx.Edition.Edition.Year))
        {
            kind = null;
            userWord = cls.Start.Text;
        }
        if (kind is { } k)
        {
            var opnd = operand();
            CheckClassConditionOperand(opnd, k);
            return new BoundClassCondition(opnd, k, not);
        }
        // Every other alternative is a user-defined word — the class condition's alphabet-name-1 / class-name-1.
        return BindUserWordClassCondition(userWord ?? cls.GetText(), not, operand);
    }

    /// <summary>§14.9.13.3 SR5's "class condition without the identifier" written as a BARE user-defined word
    /// (<c>EVALUATE WS-X WHEN MY-CLASS</c>) — the spelling the grammar cannot tell from identifier-2, because
    /// <c>evaluateWhenItem</c>'s <c>valueOperand</c> and <c>partialComparison</c>'s <c>classConditionName</c> both match
    /// one word. <see cref="AnalyzeBareOperand"/> has already resolved the word to a class-name or alphabet-name
    /// (<see cref="BareOperandForm.ClassName"/>); SR8 then splices the selection subject in, exactly as for the
    /// <c>IS</c>-led spelling, and the ONE user-word class body binds it (kb/Work PB843).</summary>
    public BoundCondition BindPartialClassName(string word, PartialSubjectOperand subject) =>
        BindUserWordClassCondition(word, not: false, () => subject.Content);

    /// <summary>The §8.8.4.4.2 alternatives that are USER-DEFINED WORDS — alphabet-name-1 and class-name-1 — over
    /// an operand the caller names. One body for the written <c>classConditionName</c> spelling and the bare EVALUATE
    /// object (kb/Work PB843), so the LOCALE refusal, the operand screens and the undeclared-name diagnostic
    /// cannot drift between them.</summary>
    private BoundCondition BindUserWordClassCondition(string word, bool not, System.Func<BoundOperand> operand)
    {
        // §8.8.4.4.3 SR2 — "Alphabet-name-1 shall not reference an alphabet associated with a locale": a LOCALE
        // alphabet is a collating sequence, not a coded character set (Table 6), so it names no character set a
        // class condition could test membership of (kb/Work PB64 T5; the same rule family as §12.3.7.3 SR16g/SR17d
        // — DataBinder.IsLocaleAlphabet is the one predicate, over BOTH classes of alphabet).
        if (ctx.Data.IsLocaleAlphabet(word))
        {
            ctx.Edition.Error(DiagnosticCatalog.LocaleAlphabetNotACharacterSet, $"class condition '{word}': "
                + "alphabet-name-1 shall not reference an alphabet associated with a locale (ISO §8.8.4.4.3 SR2) — "
                + "an ALPHABET … IS LOCALE defines a collating sequence, not a coded character set (§12.3.7.4 GR7 Table 6)");
            return Refused($"class condition '{word}'");
        }
        // A SPECIAL-NAMES user-defined class (§12.3.7): membership over the expanded character set.
        if (ctx.Data.UserClasses.TryGetValue(word, out var userClass))
        {
            var opnd = operand();
            CheckClassConditionOperand(opnd, ClassConditionModel.ClassName);   // SR3 + SR4 name class-name-1
            return new BoundUserClassCondition(opnd, userClass.Members, not);
        }
        // An ALPHABET-NAME class (§8.8.4.4.4 GR3 a — kb/Work PB109): membership of the CODED CHARACTER SET the
        // alphabet identifies (the LOCALE refusal above already took Table 6's blank row). It used to fall to the
        // loud staged BoundConditionError.
        if ((ctx.Data.Alphabets.TryGetValue(word, out var aDef) && aDef.CodedSet is { } aSet
                ? aSet : ctx.Data.NationalAlphabets.TryGetValue(word, out var nDef) ? nDef.CodedSet : null) is { } set)
        {
            var opnd = operand();
            // ⛔ ITS OWN KIND, NOT THE CLASS-NAME'S. SR3 names alphabet-name-1 beside class-name-1, but SR4 —
            // "ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, or class-name-1 shall not be specified if the
            // category … is boolean, numeric, or numeric-edited" — does NOT, so sharing the class-name kind
            // would reject `IF NUM-ITEM IS SOME-ALPHABET`, which the standard admits.
            CheckClassConditionOperand(opnd, ClassConditionModel.AlphabetName);
            string setKind = set.Phrase switch
            {
                "STANDARD-1" or "STANDARD-2" => "Ascii",
                "UCS-4" or "UTF-8" => "ScalarValues",
                _ => "AllNative",   // NATIVE / UTF-16 / a literal phrase — GR7 k4's total set
            };
            return new BoundCodedSetClassCondition(opnd, setKind, not);
        }
        // ⛔ A NAME THAT DECLARES NOTHING IS A BIND ERROR, NOT A RUN-TIME ONE. §8.8.4.4.2 offers exactly two
        // user-defined words here — alphabet-name-1 and class-name-1 — and both arms above have refused this
        // spelling, so it identifies no resource: ISO §8.4.2.1, "In order to use a resource, a statement shall
        // contain a reference that uniquely identifies that resource". That is COBOLNET1639's OWN rule, and
        // this is one more site of it rather than a new code.
        // ⛔ IT USED TO FALL TO BoundConditionError, which is the SILENT-COMPILE / LOUD-RUNTIME staging:
        // `IF X IS NOSUCHCLASS` compiled with zero diagnostics and aborted with
        // NotImplementedCobolFeatureException "class condition 'NOSUCHCLASS'". Measured on this worktree
        // before the change, at 2023 and (for the COBOL-85 spelling of BOOLEAN, which is an ordinary user word
        // below 2002) at 85.
        ctx.Edition.Error(DiagnosticCatalog.UndefinedReference, $"class condition '{word}': the word names "
            + "neither an alphabet-name nor a class-name declared in the SPECIAL-NAMES paragraph — the class "
            + "condition's only user-defined-word alternatives (ISO §8.8.4.4.2; §8.4.2.1 — \"In order to use a "
            + "resource, a statement shall contain a reference that uniquely identifies that resource\")");
        return Refused($"class condition '{word}'");
    }

    /// <summary>⛔ THE ONE sign-condition body (ISO §8.8.4.7), over an operand node the CALLER names — the written
    /// form's own <c>comparisonOperand</c>, or (§14.9.13.3 SR5's "sign condition without the identifier, or a sign
    /// condition without the arithmetic expression") the EVALUATE selection subject. Both of SR5's phrasings name
    /// the SAME elided position — §8.8.4.7.2's format offers identifier-1 or arithmetic-expression-1 there — so
    /// they are one alternative here, not two.</summary>
    private BoundCondition BindSignConditionOn(char kind, bool not, Core.ValueOperandContext? operand, AbbrevCarry carry)
    {
        carry.Reset();
        // ISO §8.8.4.7.3 SR1 closes the operand to "any single numeric data item described with a usage other
        // than a standard floating-point usage, or any form of arithmetic expression" — named here so a
        // non-numeric operand is sent to the rule it broke, not to §8.8.1.1 alone (kb/Work PB171).
        if (operand is null) return Refused("sign condition with no operand");
        return new BoundSignCondition(
            host.Expr.BindOperandExpr(operand,
                "ISO §8.8.4.7.3 SR1 admits only a single numeric data item or an arithmetic expression as a "
                + "sign-condition operand"),
            kind, not, IsFormat2FloatSign(operand));
    }

    private BoundCondition BindComparison(Core.ComparisonExpressionContext cmp, AbbrevCarry carry)
    {
        var operands = cmp.comparisonOperand();
        bool not = cmp.NOT() is not null;

        if (cmp.classConditionName() is { } cls)
            return BindClassConditionOn(cls, not, () => ComparisonOperand(operands[0]), carry);

        if (cmp.OMITTED() is not null)
        {
            carry.Reset();
            // §8.8.4.8 (kb/Work PB133 wave C; the method arm kb/Work PB757): "data-name-1 IS [NOT] OMITTED" —
            // SR1: data-name-1 shall be a formal parameter defined in the source element in which this condition
            // is specified. Inside a METHOD that element is the method (§14.9.23.4 GR9 — the condition "shall
            // be true in the invoked method"), so its own formals are the ones searched, never the containing
            // unit's; elsewhere the program/function formals are. Either way the test renders the formal's ONE
            // presence fact (OmittedProbe): spelled, trailing and GR1c-transitive omission all arrive there.
            string fname = operands.Length >= 1 ? operands[0].GetText().Trim() : "";
            OmittedProbe? formal = null;
            if (ctx.CurrentMethodScope is { } ms)
            {
                foreach (var f in ms.Formals)
                    if (CobolNames.Same(f.Item.CobolName, fname)) { formal = f.Probe; break; }
            }
            else
                foreach (var f in ctx.Data.LinkageFormals)
                    if (CobolNames.Same(f.Item.CobolName, fname)) { formal = f.Probe; break; }
            if (formal is null)
            {
                ctx.Edition.Error(DiagnosticCatalog.OmittedConditionOperand,
                    $"omitted-argument condition '{fname} IS OMITTED': data-name-1 shall be a formal parameter "
                    + "defined in the source element in which this condition is specified (ISO §8.8.4.8.3 SR1)");
                return Refused($"omitted-argument condition '{fname}'");
            }
            return new BoundOmittedCondition(formal, not);
        }

        if (cmp.POSITIVE() is not null || cmp.NEGATIVE() is not null || cmp.ZERO() is not null)
        {
            // An address-identifier is class pointer (§8.4.3.11.4 GR1 / §8.4.3.13.4 GR1), never the "single
            // numeric data item … or any form of arithmetic expression" §8.8.4.7.3 SR1 admits (kb/Work PB1021).
            if (operands[0].addressIdentifier() is { } signAddr)
            {
                carry.Reset();
                ctx.Edition.Error(DiagnosticCatalog.OperandIsNotACondition,
                    $"'{DataBinder.WrittenText(signAddr)}' is an address-identifier — a data item of class pointer "
                    + "(ISO §8.4.3.11.4 GR1 / §8.4.3.13.4 GR1) — and a sign condition's operand shall be \"any single "
                    + "numeric data item described with a usage other than a standard floating-point usage, or any "
                    + "form of arithmetic expression\" (ISO §8.8.4.7.3 SR1)");
                return Refused($"sign condition over '{DataBinder.WrittenText(signAddr)}'");
            }
            return BindSignConditionOn(cmp.POSITIVE() is not null ? 'P' : cmp.NEGATIVE() is not null ? 'N' : 'Z',
                not, operands[0].valueOperand(), carry);
        }


        if (cmp.comparisonOperator() is { } opCtx && operands.Length >= 2)
        {
            // A fully-stated relation establishes the subject + operator for any following abbreviated relation in the
            // sequence (ISO §8.8.4.12.4 GR1 — "the last preceding stated subject … and the last stated operator").
            BoundOperand subject = ComparisonOperand(operands[0]);
            string op = MapOperator(opCtx.GetText());
            carry.Subject = subject;
            carry.Op = op;
            BoundOperand right = ComparisonOperand(operands[1]);
            // §8.8.4.12.3 SR1 is decided HERE, on the relation's operand class, where the carry is seeded (PB1391).
            carry.BooleanRelation = IsBooleanClassOperand(subject) || IsBooleanClassOperand(right);
            // ⛔ THE OBJECT-REFERENCE (§8.8.4.2.1 Format 3 / SR5) AND DATA-POINTER (§8.8.4.2.16) BANDS USED TO BE
            // WRITTEN HERE, in the relation arm, ABOVE the checkpoint (kb/Work PB399). They are now in
            // StatementValidation.CheckRelationalOperands, beside the class-boolean and strongly-typed-group
            // rules of the same §8.8.4.2 band, because a rule about what may be COMPARED belongs to the ONE
            // BoundRelational construction site and not to one of its callers: §14.9.13.3 SR7 a) makes an EVALUATE
            // pair's objects "valid operands for comparison … in accordance with 8.8.4.2", and
            // written here the bands screened `IF P >= Q` and said nothing about `EVALUATE P WHEN Q THRU R` or
            // `EVALUATE P WHEN X` — the second of which reached the BACKEND and failed as a raw C# CS1503.
            // SEARCH WHEN, PERFORM UNTIL and the abbreviated-relation path had the same hole.
            // (A boolean-EXPRESSION relation — `IF (a B-AND b) = c` — is staged residue this increment; the
            // item↔item boolean compares of the data increment ride CheckedRelational's 0844 guard below.)
            return WrittenRelational(subject, op, right);
        }

        // A bare single operand — resolve as a sole-operand condition (88 / switch / simple-boolean / abbreviated).
        if (operands.Length == 1)
        {
            // A bare address-identifier is the OBJECT of an abbreviated relation (§8.8.4.12) when a subject and
            // operator are carried, and otherwise no condition at all (§8.8.4.2.1) — kb/Work PB1021.
            if (operands[0].addressIdentifier() is { } bareAddr && carry is not { Subject: not null, Op: not null })
            {
                carry.Reset();
                ReportNotACondition(DataBinder.WrittenText(bareAddr), "an address-identifier — a data item of class "
                    + "pointer (ISO §8.4.3.11.4 GR1 / §8.4.3.13.4 GR1)");
                return Refused($"condition '{DataBinder.WrittenText(bareAddr)}'");
            }
            return BindSoleOperandCondition(operands[0].valueOperand(), () => ComparisonOperand(operands[0]), carry);
        }

        return Refused($"condition '{cmp.GetText()}'");
    }

    /// <summary>A bare single operand is either a level-88 condition-name (a complete simple condition —
    /// terminates the abbreviation), a switch-status condition-name (§8.8.4.6), a SIMPLE BOOLEAN CONDITION over
    /// a length-1 boolean item/literal (§8.8.4.3), or — within an abbreviated sequence — a relation with BOTH
    /// subject and operator omitted (§8.8.4.12; the trailing C in `A = B AND C` ≡ `A = C`). A condition-name
    /// takes precedence. Shared by the generic path and the boolean-alt unwrap (a B-op-free bare operand).</summary>
    private BoundCondition BindSoleOperandCondition(Core.ValueOperandContext? vo, System.Func<BoundOperand> bindOperand, AbbrevCarry carry)
    {
        // §14.9.13.3 SR5/SR8 — under a partial-expression rewrite the LEFTMOST leaf may be the bare class-name /
        // alphabet-name of "a class condition without the identifier" (LeadingBareClassWord); the selection
        // subject is spliced in as its identifier, exactly as partialComparison's `IS? NOT? classConditionName` does.
        if (carry.PartialSubject is { } ps && BareClassWord(vo) is { } classWord)
        {
            carry.PartialSubject = null;
            carry.Reset();
            return BindUserWordClassCondition(classWord, not: false, () => ps.Content);
        }
        if (BareOperandAsCondition(vo) is { } sole)
        {
            carry.Reset();
            return sole;
        }
        // A bare object after a boolean relation would be an abbreviation (§8.8.4.12.1 2), which SR1 forbids.
        if (carry.BooleanRelation) return RefuseAbbreviationAfterBooleanRelation(vo is null ? "operand" : DataBinder.WrittenText(vo));
        if (carry is { Subject: { } subject, Op: { } op })
            return WrittenRelational(subject, op, bindOperand());
        return RefuseNonCondition(vo);
    }

    /// <summary>⛔ A BARE OPERAND THAT IS NONE OF THE CONDITIONS IS A COMPILE-TIME ERROR (kb/Work PB982). ISO
    /// §8.8.4.2.1: "The simple conditions are the relation, boolean, class, condition-name, switch-status, sign,
    /// and omitted-argument conditions", and a complex condition is built only from them (§8.8.4.1 — "There are
    /// two categories of conditions associated with conditional expressions: simple conditions and complex
    /// conditions"). By the time this runs the operand has been refused as a condition-name, a switch-status
    /// condition-name and a simple boolean condition (<see cref="AnalyzeBareOperand"/>), and no abbreviated
    /// relation is in progress to make it an object (§8.8.4.12) — so `IF WS-X` over a PIC X item, a bare
    /// class-name outside an EVALUATE selection object, a switch's mnemonic-name, a literal, a figurative or an
    /// arithmetic expression is simply not a conditional expression. It used to return a diagnostic-less
    /// <see cref="BoundConditionError"/>: the program compiled clean and aborted the run unit with
    /// NotImplementedCobolFeatureException when the condition was reached. The message names what the operand
    /// IS, because "not a condition" alone does not tell a programmer which condition they meant to write.</summary>
    private BoundConditionError RefuseNonCondition(Core.ValueOperandContext? vo)
    {
        string text = vo is null ? "operand" : DataBinder.WrittenText(vo);
        var dref = ConditionOperandExpression(vo) is { } expr ? SoleDataRef(expr) : null;   // `(X)` names X (kb/Work PB1464)
        string? what = null;
        if (BareClassWord(vo) is { } classWord)
            what = ctx.Data.IsAlphabetName(classWord)
                ? $"alphabet-name '{classWord}', which forms a class condition only after the identifier it tests — "
                  + "\"identifier-1 IS [NOT] alphabet-name-1\" (ISO §8.8.4.4.2)"
                : $"class-name '{classWord}', which forms a class condition only after the identifier it tests — "
                  + "\"identifier-1 IS [NOT] class-name-1\" (ISO §8.8.4.4.2)";
        else if (dref is { } d && d.dataReferenceSuffix().Length == 0 && d.cobolWord()?.GetText() is { } word
                 && ctx.Mnemonics.Of(d).TryGetValue(word, out var mnemonicRow) && mnemonicRow.Kind == SystemNameKind.Switch)
            what = $"the mnemonic-name '{word}' of a switch; a switch-status condition is written with a condition-name "
                + "the SPECIAL-NAMES paragraph associates with the switch's ON or OFF status (ISO §8.8.4.6.1; the §8.8.4.6.2 format is condition-name-1)";
        else if (dref is { } dd && ctx.Refs.Probe(dd) is { } probe)
            what = $"data item '{probe.Item.CobolName ?? text}'"
                + (probe.OperandCategory is { } cat ? $" of category {cat.ToString().ToLowerInvariant()}" : "");
        else if (dref is { } undefined)
        {
            // A word that names nothing is the resolver's own diagnostic (COBOLNET1639), not a second one here —
            // and a shape it DEFERS is announced, not refused (kb/Work PB1030).
            return Unresolved(host.Expr.ResolveSending(undefined), $"condition '{text}'");
        }
        ReportNotACondition(text, what ?? "a literal, a figurative constant, a function reference or an arithmetic expression");
        return Refused($"condition '{text}'");
    }

    /// <summary>⛔ THE ONE WORDING OF "THIS OPERAND IS WRITTEN WHERE A CONDITION IS REQUIRED" (COBOLNET2318). Every
    /// arm that finds a bare operand, an address-identifier or a truth word in a condition position names what it
    /// IS and the rule it breaks through here, so the §8.8.4.2.1 list of the simple conditions is written once.</summary>
    private void ReportNotACondition(string text, string what) =>
        ctx.Edition.Error(DiagnosticCatalog.OperandIsNotACondition,
            $"'{text}' is used as a condition, but it is {what}: a conditional expression is a relation, boolean, class, "
            + "condition-name, switch-status, sign or omitted-argument condition, or a combination of them "
            + "(ISO §8.8.4.2.1; §8.8.4.1)");

    /// <summary>⛔ THE ONE CONSTRUCTION SITE OF <see cref="BoundConditionError"/> (kb/Work PB982). An error node in a
    /// condition position is lowered by the emitter to a run-time <c>NotImplemented</c> throw, so a refusal that
    /// forgot its diagnostic compiled clean and aborted the run unit — the silent-compile / loud-run channel PB982
    /// found open at <see cref="BindSoleOperandCondition"/>. Every refusal now comes through here, and when the
    /// compilation has recorded NO failing diagnostic yet this reports COBOLNET2319: an internal-error diagnostic
    /// naming the refused form, so no condition error node can ever reach a successful compile. The invariant is
    /// exactly "a program that compiles contains no condition error node", which is why the test is the sink's
    /// <see cref="EditionContext.HasErrors"/> rather than a per-site flag. <c>ConditionErrorConstructionDriftTests</c>
    /// holds every other <c>Refused(</c> out of the compiler.</summary>
    internal BoundConditionError Refused(string feature)
    {
        ctx.Edition.NoteRefusal(DiagnosticCatalog.UnreportedConditionRefusal, $"the condition form {feature}");
        return ConditionErrorNode(feature);
    }

    /// <summary>The condition node for a reference the resolver built no place for, chosen FROM its closed answer
    /// (kb/Work PB1030): a DEFERRED shape is already on the unbuilt ledger and is announced by the statement funnel
    /// (COBOLNET1756) — it is not a refusal of the source; a REPORTED one is <see cref="Refused"/>.</summary>
    internal BoundConditionError Unresolved(RefResolution answer, string feature) =>
        answer.Outcome == RefOutcome.Deferred ? ConditionErrorNode(answer.Feature) : Refused(feature);

    /// <summary>The ONE construction of <see cref="BoundConditionError"/> — reached only from <see cref="Refused"/>
    /// and <see cref="Unresolved"/> (<c>ConditionErrorConstructionDriftTests</c>).</summary>
    private static BoundConditionError ConditionErrorNode(string feature)
    {
        return new BoundConditionError(feature);
    }

    /// <summary>⛔ THE ONE ANALYSIS OF A BARE OPERAND — which of the three shapes it is, resolved ONCE
    /// (kb/Work PB400). A caller that must first CLASSIFY the operand and only then BIND it — EVALUATE's
    /// Table-15 screen, whose §14.9.13.3 SR6 decides a boolean operand's kind from the OTHER side of the pair —
    /// would otherwise resolve the same symbol twice, doubling <see cref="ConditionOf"/>'s §8.4.2.2 ambiguity
    /// diagnostic and binding the boolean operand as a simple boolean condition (with its §8.8.4.3 SR1
    /// length-1 screen) before knowing whether it IS one.
    /// <para>⛔ AND IT IS A SYMBOL-TABLE QUESTION, WHICH IS WHY IT CANNOT LIVE IN THE GRAMMAR. A bare word is
    /// equally a condition-name and an <c>arithmeticExpression</c>, so it always arrives through the
    /// <c>valueOperand</c> arm; only the resolved symbol says which it is.</para></summary>
    public BareOperandAnalysis AnalyzeBareOperand(Core.ValueOperandContext? vo)
    {
        // ⛔ PARENTHESES AROUND A SIMPLE CONDITION CHANGE NOTHING (§8.8.4.2.1; kb/Work PB1464). The grammar takes
        // `(X1)` as a parenthesized arithmetic operand (primaryCondition tries comparisonExpression first, and
        // reordering it is what the DEVLOG-621 regression forbids), so the seam that classifies the operand looks
        // THROUGH its enclosing parentheses: `(X1)`, `NOT (S1-OFF)` and `(BW)` are the level-88, the switch-status
        // condition-name and the boolean item they enclose.
        var inner = ConditionOperandExpression(vo);
        if (inner is { } expr && SoleDataRef(expr) is { } dref && ConditionOf(dref) is { } cond)
            // The reference's subscripts identify the CONDITIONAL VARIABLE's occurrence (§8.4.2.3 Format 2).
            // Capture EC-RANGE-INVALID checking (§14.7.8 rule 2 — an inverted alphanumeric/national VALUE THRU range).
            return BareOperandAnalysis.OfCondition(BareOperandForm.ConditionName,
                ctx.Refs.ResolveForItem(dref, cond.Parent) is var parentAnswer && parentAnswer.Place is { } parent
                    ? new BoundCondition88(parent, cond,
                        ctx.EcState.Turn.Enabled("EC-RANGE-INVALID", null, dref.Start.Line))
                    : Unresolved(parentAnswer, $"condition-name '{cond.Name}' (unresolvable conditional variable)"));
        // A switch-status condition-name — resolved AFTER level-88 (NC211A: a name defined as both → the 88
        // wins), BEFORE the abbreviated-carry fallback.
        if (inner is { } swx && SoleDataRef(swx) is { } swr && host.Alter.SwitchCondOf(swr) is { } swCond)
            return BareOperandAnalysis.OfCondition(BareOperandForm.SwitchStatus, swCond);
        // ⛔ §13.16.3 SR23's NEGATIVE side (kb/Work PB567): a declared level-88 written under qualifiers its
        // conditional variable is not subordinate to — and that no DATA-name under those qualifiers answers
        // either — is a condition-name reference that identifies nothing. It is classified as the CONDITION-NAME
        // it was written as (so EVALUATE's Table-15 screen does not re-read it as an identifier and report a
        // pairing error about the wrong thing) and reported ONCE, through the one wording.
        if (inner is { } mqx && SoleDataRef(mqx) is { } mqr && ctx.Refs.Probe(mqr) is null
            && ReportMisqualifiedCondition(mqr))
            return BareOperandAnalysis.OfCondition(BareOperandForm.ConditionName,
                Refused($"condition-name '{DataBinder.WrittenText(mqr)}'"));
        // A class-name-1 / alphabet-name-1 (§8.8.4.4.2) written bare — a word, never qualified, subscripted or
        // reference-modified (neither name is a data-name, so neither takes a qualifier or a subscript). §8.3.2.2
        // makes the name classes disjoint ("a given user-defined word may be used as only one type of user-defined
        // word"), so the resolved SYMBOL decides it; the Probe keeps a word that ALSO resolves as a data item (an
        // already non-conforming program) on the value reading it always had (kb/Work PB843).
        if (BareClassWord(vo) is { } classWord) return BareOperandAnalysis.OfClassName(classWord);
        // A BOOLEAN operand (§8.8.2). Whether it is a §8.8.4.3 simple boolean CONDITION is the CALLER's
        // question in EVALUATE (SR6) and settled here everywhere else, so the expression is carried bound but
        // unwrapped, with its §8.8.2 rules 9/10 result length.
        if (vo is not null && IsBooleanValueOperand(vo)) return BooleanAnalysis(BindBoolOperandValue(vo));
        return default;   // BareOperandForm.Value
    }

    /// <summary>The <see cref="BareOperandForm.Boolean"/> analysis of an already-bound boolean expression, carrying
    /// its §8.8.2 rules 9/10 result length — the ONE construction, shared by a bare boolean operand
    /// (<see cref="AnalyzeBareOperand"/>) and an operator-bearing boolean expression
    /// (<see cref="AnalyzeBooleanExpression"/>), so §14.9.13.3 SR6's "results in one boolean character" is asked of
    /// both through the same number.</summary>
    private static BareOperandAnalysis BooleanAnalysis(BoundBoolExpr b) =>
        new(BareOperandForm.Boolean, null, b, BoolResultLength(b));

    /// <summary>⛔ THE ONE ANALYSIS OF A <c>booleanExpression</c> WRITTEN AS A SELECTION OPERAND (kb/Work PB1412) —
    /// the EVALUATE subject <c>boolean-expression-1</c> and object <c>boolean-expression-2</c> of ISO §14.9.13.2.
    /// An expression that carries a boolean operator is a <see cref="BareOperandForm.Boolean"/> operand whose bound
    /// tree and result length are held for Table 15 and §14.9.13.3 SR6, exactly as a bare boolean item's are; the
    /// grammar's <c>{boolExprAhead()}?</c> gate can also take an OPERATOR-FREE one (<c>(B"1")</c>), which reduces to
    /// its bare operand and is analysed as that operand — the relation channel's own reading
    /// (<see cref="BindBoolOrValueOperand"/>).</summary>
    public BareOperandAnalysis AnalyzeBooleanExpression(Core.BooleanExpressionContext be) =>
        OperatorBearing(be) is not null ? BooleanAnalysis(BindBoolExpr(be)) : AnalyzeBareOperand(BareOperandOf(be));

    /// <summary>The boolean expression when it carries a boolean operator — a GENUINE boolean expression — else null
    /// (a bare operand parsed through the <c>booleanExpression</c> rule). The ONE discriminator
    /// <see cref="BindBoolOrValueOperand"/> and EVALUATE's selection operands both ask.</summary>
    internal static Core.BooleanExpressionContext? OperatorBearing(Core.BooleanExpressionContext? be) =>
        be is not null && HasBoolOp(be) ? be : null;

    /// <summary>The bare operand a boolean-expression node reduces to when it carries NO boolean operator, else null
    /// — <see cref="OperatorBearing"/>'s complement, over <see cref="UnwrapBareBool"/>.</summary>
    internal static Core.ValueOperandContext? BareOperandOf(Core.BooleanExpressionContext? be) =>
        be is not null && !HasBoolOp(be) ? UnwrapBareBool(be) : null;

    /// <summary>The expression a bare operand denotes AS A CONDITION OPERAND: its arithmetic expression with the
    /// parentheses that enclose the whole of it removed, because §8.8.4.2.1 says parentheses around a simple
    /// condition change nothing (kb/Work PB1464). The ONE read of that fact for the bare-operand classifier, the
    /// boolean-operand predicate and the not-a-condition report. A class-name stays parenthesis-opaque
    /// (<see cref="BareClassWord"/>): "(MY-CLASS)" is no class condition, which needs its identifier.</summary>
    public static Core.ArithmeticExpressionContext? ConditionOperandExpression(Core.ValueOperandContext? vo) =>
        SoleOperand.Unparenthesized(vo?.arithmeticExpression());

    /// <summary>The class-name-1 / alphabet-name-1 (ISO §8.8.4.4.2) a bare operand names, or null — the ONE symbol
    /// test behind <see cref="BareOperandForm.ClassName"/>. A word only: neither name is a data-name, so neither
    /// takes a qualifier, a subscript or a reference modifier. PURE (dictionary lookups and a diagnostic-free
    /// <see cref="ReferenceResolver.Probe"/>), so a classifier may ask it before anything is bound.</summary>
    public string? BareClassWord(Core.ValueOperandContext? vo) =>
        vo?.arithmeticExpression() is { } expr && SoleDataRef(expr) is { } dref && dref.dataReferenceSuffix().Length == 0
            && dref.cobolWord() is { } word && ctx.Data.IsClassConditionWord(word.GetText()) && ctx.Refs.Probe(dref) is null
            ? word.GetText() : null;

    /// <summary>§14.9.13.3 SR5 over a selection object the grammar parsed as a whole <c>condition</c>: the
    /// class-name / alphabet-name its LEFTMOST simple condition is written as, bare, or null. <c>WHEN MY-CLASS AND
    /// WS-F = "Y"</c> is a partial-expression — its leftmost portion is "a class condition without the
    /// identifier" — but the bare word is also a complete comparison operand, so the grammar's <c>condition</c>
    /// alternative claims the object before <c>partialExpression</c> is tried. The descent follows exactly the
    /// leftmost path SR5 names: through the logical tiers and a leading <c>NOT</c> (§8.8.4.4.2's own
    /// <c>[ NOT ]</c>), never into a parenthesis (a parenthesised leftmost portion is not a class condition).</summary>
    public string? LeadingBareClassWord(Core.ConditionContext cond)
    {
        IParseTree n = cond;
        while (true)
        {
            switch (n)
            {
                case Core.ConditionContext or Core.LogicalOrExpressionContext or Core.LogicalXorExpressionContext
                    or Core.LogicalAndExpressionContext:
                    n = n.GetChild(0);
                    continue;
                case Core.UnaryLogicalExpressionContext u:
                    n = u.primaryCondition();
                    continue;
                case Core.PrimaryConditionContext p when p.comparisonExpression() is { } cmp:
                    n = cmp;
                    continue;
                case Core.ComparisonExpressionContext cmp when cmp.ChildCount == 1:
                    return BareClassWord(cmp.comparisonOperand(0).valueOperand());
                default:
                    return null;
            }
        }
    }

    /// <summary>The three shapes in which a BARE operand IS itself a complete condition — a level-88
    /// condition-name (§8.8.4.2.7 r2), a switch-status condition-name (§8.8.4.6), or a simple boolean condition over
    /// a length-1 boolean item/literal (§8.8.4.3) — or <c>null</c> when the operand is a plain VALUE. The
    /// resolution is <see cref="AnalyzeBareOperand"/>'s; this is the arm that WRAPS a boolean expression as the
    /// §8.8.4.3 condition, which is where that clause's SR1 length screen belongs.</summary>
    public BoundCondition? BareOperandAsCondition(Core.ValueOperandContext? vo) =>
        AsCondition(AnalyzeBareOperand(vo));

    /// <inheritdoc cref="BareOperandAsCondition"/>
    public BoundCondition? AsCondition(in BareOperandAnalysis a) => a.Form switch
    {
        BareOperandForm.ConditionName or BareOperandForm.SwitchStatus => a.Condition,
        BareOperandForm.Boolean => BindSimpleBooleanCondition(a.Boolean!),
        _ => null,
    };

    /// <summary>ISO §8.8.2 rules 9 and 10 — the number of boolean positions the RESULT of a boolean expression
    /// occupies: r10 "a boolean value whose length shall be the number of boolean positions of the larger item
    /// referenced in that operation", r9 the shift's "the number of boolean positions of the first item
    /// referenced". <see langword="null"/> when the length is POSITIONLESS (figurative ZERO / <c>ALL B"…"</c>,
    /// which materialize to the sibling's length — §8.3.3.6.4 GR4) or is not statically known (a boolean
    /// function whose length argument is a run-time value).
    /// <para>This is the length §14.9.13.3 SR6 turns on ("a boolean expression that results in one boolean
    /// character"). It is a SIBLING of <see cref="Gr3Width"/>, not the same function: GR3's COMPUTE store width
    /// counts ITEMS only and gives a literal 0, while a boolean literal's own length is exactly what SR6 needs
    /// (<c>EVALUATE TRUE WHEN B"1"</c> vs <c>WHEN B"01"</c> differ on it, and the difference is a Table-15
    /// verdict).</para></summary>
    internal static int? BoolResultLength(BoundBoolExpr e) => e switch
    {
        BoundBoolLiteral l => l.Bits.Length,
        BoundBoolRef r => StaticBoolRefLength(r),              // null for a run-time-length slice (kb/Work PB589)
        BoundBoolNot n => BoolResultLength(n.Operand),
        BoundBoolShift s => BoolResultLength(s.Operand),      // r9 — the FIRST item's positions
        BoundBoolBinary b => LargerOf(BoolResultLength(b.Left), BoolResultLength(b.Right)),   // r10
        BoundBoolCall c => StaticBoolCallWidth(c),
        BoundBoolAll => null,                                  // positionless (§8.3.3.6.4 GR4)
        _ => null,                                             // error nodes already reported
    };

    /// <summary>r10's "the larger item referenced" with a positionless operand contributing nothing.</summary>
    private static int? LargerOf(int? a, int? b) => a is null ? b : b is null ? a : System.Math.Max(a.Value, b.Value);

    /// <summary>A relation CONDITION as the program wrote it — IF, PERFORM UNTIL, SEARCH WHEN, a boolean relation, an
    /// abbreviated relation's inserted subject, an EVALUATE partial-expression's spliced subject (§14.9.13.3 SR8). Asks
    /// the one rule that is about a written condition and not about a compared pair — ISO §8.8.4.2.1's "at least one
    /// reference to an operand that is not a literal" (<see cref="StatementValidation.CheckRelationHasNonLiteralOperand"/>,
    /// kb/Work PB1470), of the operands as WRITTEN (an EVALUATE subject's intermediate reads through to the operand it
    /// holds) — then builds the node through <see cref="CheckedRelational"/>. EVALUATE's subject/object pairs and
    /// ranges reach <see cref="CheckedRelational"/> directly: §14.9.13.3 SR10's Table 15 governs them.</summary>
    private BoundCondition WrittenRelational(BoundOperand left, string op, BoundOperand right)
    {
        ctx.Validation.CheckRelationHasNonLiteralOperand(Written(left), Written(right));
        return CheckedRelational(left, op, right);
    }

    /// <summary>The ONE <see cref="BoundRelational"/> construction checkpoint — the §8.8.4.2.2 boolean
    /// relation rules ride every site (IF / EVALUATE pairing + ranges / PERFORM UNTIL / SEARCH): a boolean
    /// operand compares only with another boolean operand (§8.8.4.2.1 F1 SR2/SR3 exclude class boolean from
    /// the general relation — a class mix is 0844) and only for [in]equality (Format 2 — an ordering operator
    /// on boolean operands is 0844; an EVALUATE THRU range over a boolean subject trips the same check,
    /// §14.9.13.3 SR4). Figurative ZERO is boolean zeros by context (§8.3.3.6.4 GR4); every other figurative
    /// against a boolean operand is non-boolean.</summary>
    public BoundCondition CheckedRelational(BoundOperand left, string op, BoundOperand right)
    {
        // The edition-invariant §8.8.4.2.2 / §8.8.4.2.3 SR band (class-boolean comparability + the
        // strongly-typed-group rules) is the ONE StatementValidation home (10t/3 — the 10o deviation-(b)
        // pure-lift discharged); the node is always built (a PURE emission check, no verdict).
        ctx.Validation.CheckRelationalOperands(Written(left), op, Written(right));
        // ⛔ WHICH COMPARISON THE PAIR IS, asked of the ONE classifier the screens above also ask (kb/Work PB1469):
        // two same-type strongly-typed groups compare ELEMENT BY ELEMENT (ISO §8.8.4.2.12), which is lowered here
        // to the relations between their elementary items — never the whole-group image a Flat pair compares.
        if (RelationPair.Classify(left, right) is RelationPairShape.StrongElementOrder
            && left is BoundFieldOperand { Place: var lp } && right is BoundFieldOperand { Place: var rp })
        {
            if (_strongGroups.Lower(lp, rp, op, out string? unbuilt) is { } elementOrder) return elementOrder;
            ctx.Edition.Error(DiagnosticCatalog.StrongGroupComparisonMember, $"the comparison of the strongly-typed "
                + $"groups '{lp.Item.CobolName}' and '{rp.Item.CobolName}' reaches {unbuilt}, which this "
                + "implementation cannot yet compare element by element (ISO §8.8.4.2.12)");
            return Refused($"strongly-typed group comparison over {unbuilt}");
        }
        return new BoundRelational(left, op, right);
    }

    /// <summary>The OTHER comparison node's construction, through the SAME checkpoint (kb/Work PB1468). An EVALUATE
    /// THROUGH range lowers to <see cref="BoundRangeMembership"/> instead of the inclusive
    /// <see cref="CheckedRelational"/> pair when it carries an IN alphabet-name or EC-RANGE-INVALID checking — but it
    /// is still §14.9.13.4 GR4 a) 5.'s "selection-subject &gt;= left-part AND selection-subject &lt;= right-part", so
    /// every §8.8.4.2 operand rule is owed to both of those relations. Built anywhere else, the node skipped the
    /// checkpoint: `EVALUATE NB WHEN "0001" THRU "0020" IN AL` (NB COMP) and `EVALUATE I1 WHEN "1" THRU "5" IN AL`
    /// (I1 an index-name) compiled clean while the identical ranges without the phrase were refused.</summary>
    public BoundCondition CheckedRangeMembership(BoundOperand subject, BoundOperand lo, BoundOperand hi, bool checkInvalid,
        string? alphabet)
    {
        ctx.Validation.CheckRelationalOperands(Written(subject), ">=", lo);
        ctx.Validation.CheckRelationalOperands(Written(subject), "<=", hi);
        return new BoundRangeMembership(subject, lo, hi, CheckInvalid: checkInvalid, Alphabet: alphabet);
    }

    /// <summary>The operand each implementor's intermediate result item stands for (§14.9.13.4 GR3's EVALUATE subject
    /// value — <c>EvaluateBinder.BindSubjectValue</c>), keyed by the intermediate operand INSTANCE the statement's
    /// pairs all share. ⛔ THE §8.8.4.2 OPERAND RULES ARE ABOUT THE OPERAND THE PROGRAMMER WROTE, the principle
    /// <c>SendingValueTemp.Materialize</c> states for its own store: re-running them against the intermediate judges a
    /// description the implementor chose. An index-name subject's intermediate is a numeric item, so without this
    /// `EVALUATE I1 WHEN I2 WHEN IDA` — two §8.8.4.2.13 pairs — was refused as numeric-against-index (kb/Work
    /// PB1468). The EMITTED node keeps the intermediate; only the checkpoint reads through.</summary>
    private readonly Dictionary<BoundOperand, BoundOperand> _writtenOperand = new(ReferenceEqualityComparer.Instance);

    /// <summary>Record that <paramref name="intermediate"/> holds the value of the written <paramref name="written"/>.</summary>
    public void RecordIntermediate(BoundOperand intermediate, BoundOperand written) => _writtenOperand[intermediate] = written;

    /// <summary>The operand as written: <paramref name="o"/> itself unless it is a recorded intermediate.</summary>
    private BoundOperand Written(BoundOperand o) => _writtenOperand.TryGetValue(o, out var w) ? w : o;

    /// <summary>The §8.8.4.2.12 element-order lowering (kb/Work PB1469) — per unit, stateless beyond the context.</summary>
    private readonly StrongGroupComparison _strongGroups = new(ctx);

    /// <summary>ISO §8.8.4.7.3 SR2 — a sign condition is Format 2 (the IEEE sign-bit test, §8.8.4.7.4 GR2) iff
    /// data-name-1 is a single data item of a standard floating-point usage that is NOT enclosed in parentheses.
    /// A parenthesized float (SR1 makes <c>(FL) IS POSITIVE</c> Format 1), a non-float item, or any compound /
    /// unary-signed expression stays Format 1 (the algebraic test). The paren distinction is invisible in the
    /// bound tree (<c>(FL)</c> and <c>FL</c> bind identically), so it is decided on the PARSE shape here.</summary>
    /// <summary>The numeric expression an already-bound selection-subject VALUE supplies as a sign-condition
    /// operand (§8.8.4.7.3 SR1 — "any single numeric data item … or any form of arithmetic expression"), or null
    /// when the value is not numeric (the caller then takes the SR1 diagnostic path).</summary>
    private static BoundExpr? SignOperandOf(BoundOperand value) => value switch
    {
        BoundFieldOperand { Place: var p } when p.Item.OperandPic?.Category is PicCategory.Numeric => new BoundNumRef(p),
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: not PicCategory.Numeric } } => null,
        BoundComputedOperand c => c.Expr,
        _ => null,
    };

    private bool IsFormat2FloatSign(Core.ValueOperandContext? operand) =>
        SoleDataReference(operand?.arithmeticExpression()) is { } dref
        && ctx.Refs.Probe(dref) is { Item.Pic: { } pic }      // Probe — a routing predicate is diagnostic-free (R30)
        // ⛔ STANDARD float (§3.166 / §3.167), never PicInfo.IsFloat: FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED and
        // COMP-1 / COMP-2 are floating-point but NOT standard, so SR1 makes a bare one arithmetic-expression-1 and
        // GR1's ALGEBRAIC test answers (a FLOAT-LONG zero IS POSITIVE is false — kb/Work PB1471).
        && UsageFamilies.IsStandardFloat(pic.Usage);

    /// <summary>The operand's sole unparenthesized data reference, or null when the arithmetic expression carries
    /// any operator, a unary sign, or enclosing parentheses (the list patterns fail for ≥2 sub-terms, and
    /// <c>primaryExpression().dataReference()</c> is null for the unary-sign and <c>LPAREN … RPAREN</c> primaries —
    /// GROUPING-PAREN-ONLY: a <c>primaryExpression</c> parenthesis is always the arithmetic grouping one, never
    /// the §8.4.3.2.3 SR6 argument-list twin, which belongs to <c>functionCall</c>)
    /// — the SR2 "single data item … not enclosed in parentheses" reduction, mirroring
    /// <see cref="IntrinsicBinder"/>.SoleDataReference. Subscript/ref-mod parens inside the dataReference are fine.
    /// <para>⚠ NULL-TOLERANT WRAPPER over the ONE <see cref="SolePrimary"/> descent. It used to be a second,
    /// independently-written implementation (tier-by-tier list patterns) that agreed with
    /// <see cref="SoleDataRef"/> by coincidence rather than by construction — one mechanism written twice, and a
    /// live drift hazard the moment the family grew a third member (kb/Work PB172).</para></summary>
    internal static Core.DataReferenceContext? SoleDataReference(Core.ArithmeticExpressionContext? arith) =>
        SolePrimary(arith)?.dataReference();

    /// <summary>The FOURTH member of the sole-operand family — an expression that is nothing but an INLINE
    /// METHOD INVOCATION (ISO §8.4.3.1.2 Format 4; kb/Work PB428). Same reason as its three siblings: the
    /// construct is an IDENTIFIER, <c>arithmeticExpression</c> subsumes it, and a caller that reads the parse
    /// node instead of the meaning applies an expression rule to an identifier — which is precisely how an
    /// alphanumeric-returning invocation came to be refused as a method ARGUMENT (§14.8.2.3.3 rule 2a's
    /// COMPUTE lane instead of rule 2d's MOVE lane). Over the ONE <see cref="SolePrimary"/> descent, never a
    /// second walk.
    /// <para>⛔ AND ITS TWO OO SIBLINGS RIDE WITH IT (kb/Work PB1425): an object-view (Format 5) and SELF / SUPER
    /// (Format 6) are identifiers the expression tier now carries too, and a sole one is an identifier of class
    /// object, never an arithmetic expression. Every consumer of the family reads all three through this one
    /// reduction and binds them through the ONE door, <c>OoBinder.OoBindOoIdentifier</c>; a consumer whose rule
    /// treats SELF by name (SET Format 5, the INVOKE argument) asks <see cref="SolePredefinedObject"/> first.</para></summary>
    internal static Antlr4.Runtime.ParserRuleContext? SoleOoIdentifier(Core.ArithmeticExpressionContext? arith) =>
        SolePrimary(arith) is { } p
            ? (Antlr4.Runtime.ParserRuleContext?)p.inlineMethodInvocation()
              ?? (Antlr4.Runtime.ParserRuleContext?)p.objectView() ?? p.selfAndSuper()
            : null;

    /// <summary>An expression that is nothing but SELF or [object-class-name-1 OF] SUPER (§8.4.3.1.2 Format 6) — for
    /// the positions whose rules name SELF itself (§14.9.39.3's SELF sender, §14.8.2.3.3's SET-rule argument).</summary>
    internal static Core.SelfAndSuperContext? SolePredefinedObject(Core.ArithmeticExpressionContext? arith) =>
        SolePrimary(arith)?.selfAndSuper();

    /// <summary>Bind a comparison operand: a non-numeric literal, a sole data reference, or a numeric expression.</summary>
    /// <para>⛔ AN ADDRESS-IDENTIFIER IS A RELATION OPERAND (kb/Work PB1021): §8.8.4.2.2 Format 3 prints
    /// identifier-3 / identifier-4, §8.4.3.1.2 identifier FORMAT 9 is an identifier, and §8.4.3.11.4 GR1 /
    /// §8.4.3.13.4 GR1 make it "a unique data item of class pointer" — so it binds through the ONE
    /// address-identifier binder into a <see cref="BoundAddressOperand"/>, and the §8.8.4.2.3 SR5 band
    /// (<c>StatementValidation.CheckRelationalOperands</c>) screens its class and category like any pointer
    /// operand. A refused operand has been reported by that binder.</para>
    private BoundOperand ComparisonOperand(Core.ComparisonOperandContext operand) =>
        operand.addressIdentifier() is { } ai
            ? host.Ptr.AddressIdentifierOperand(ai, "a relation condition")
            : ComparisonOperandOf(operand.valueOperand());

    /// <summary>⛔ A BOOLEAN OPERAND ENCLOSED IN PARENTHESES, as a relation or EVALUATE operand (kb/Work PB1464):
    /// §8.8.2 lists "a boolean expression enclosed in parentheses" as a boolean expression, so <c>(BW)</c> — or a
    /// parenthesized boolean function — IS the boolean operand it encloses, in the boolean relation of §8.8.4.2.2
    /// Format 2. It used to be read as the arithmetic expression the grammar's parenthesized primary spells, and
    /// `IF (BW) = BZ` drew COBOLNET0844 "not a numeric operand". An enclosed NON-boolean
    /// operand stays an arithmetic expression (§8.8.1.1), and null says so. The ONE reading both operand binders
    /// ask (ComparisonOperandOf and EVALUATE's BindValueOperand answer one question, kb/Work PB224).</summary>
    internal BoundOperand? EnclosedBooleanOperand(Core.ValueOperandContext? vo)
    {
        if (vo?.arithmeticExpression() is not { } whole || ConditionOperandExpression(vo) is not { } enclosed
            || ReferenceEquals(whole, enclosed) || !IsBooleanValueOperand(vo))
            return null;
        return SoleDataRef(enclosed) is { } enclosedRef ? host.Expr.FieldOperand(enclosedRef)
            : new BoundBoolOperand(BindBoolOperandValue(vo));
    }

    /// <summary>Bind a <c>valueOperand</c> as a comparison operand (the shared body of <see cref="ComparisonOperand"/>
    /// and the boolean-alt unwrap path — feedback_one_mechanism_per_job).</summary>
    private BoundOperand ComparisonOperandOf(Core.ValueOperandContext? vo)
    {
        // §8.8.3.3 GR3: a concatenation expression folds to (and compares as) the equivalent single literal.
        // Through the ONE literal mapping. ⛔ THIS was the copy that lacked the hexadecimal arm, so
        // `IF A = X"6162"` — conforming source, since §8.8.4.2.1 bars only a relation whose operands are BOTH
        // literals (the "§8.8.4.1.1" this used to cite is a phantom clause — kb/Work PB182; the rule is the
        // clause's UNNUMBERED closing sentence, so the citation is bare — never "r13", which is "Two compatible
        // variable-length groups" and which cite.py's rule_path nonetheless prints, kb/Work PB222) and
        // §8.3.3.2 Format 2 makes X"…" an alphanumeric literal — fell past every arm here and staged loud as
        // "comparison operand" at run time, while the SAME literal worked in a MOVE (DA3).
        // §8.4.3.10.3 SR1: a relation condition is a context NULL may occupy — the checkpoint's §8.8.4.2.3 SR5
        // band then decides it against the other operand (pointer / object / message-tag only, COBOLNET0869).
        if (host.Expr.NullAdmittingOperand(vo?.nonNumericLiteral()) is { } litOp) return litOp;
        if (EnclosedBooleanOperand(vo) is { } enclosedBoolean) return enclosedBoolean;
        if (vo?.arithmeticExpression() is { } expr)
            return SoleDataRef(expr) is { } dref ? host.Expr.FieldOperand(dref)
                // ⛔ THE SOLE-FUNCTION SHORT-CIRCUIT (kb/Work PB172), the fourth member of the family above and
                // the one whose absence made the §8.8.1.1 intrinsic screen unwidenable. §15.2 gives a function
                // "the class and category" of its result and says it "may be used anywhere a sending data item
                // of that class and category may be specified", so a SOLE alphanumeric function is a legal
                // §8.8.4.2.1 relation operand — `IF FUNCTION LOWER-CASE(X) = Y`, six NIST IF-suite programs.
                // Reaching it through the expression spine instead handed it to BindPrimary's arithmetic screen,
                // which is why PB155's widening had to be reverted. A PARENTHESIZED form is deliberately NOT
                // short-circuited: `(FUNCTION LOWER-CASE(X))` is "an arithmetic expression enclosed in
                // parentheses" (§8.8.1.1) and the single-child descent gives that for free.
                : SoleFunctionCall(expr) is { } sfc
                    ? IntrinsicBinder.OperandOf(host.Intrinsic.BindIntrinsic(sfc))
                // Its §8.4.3.1.2 Format-4 twin (kb/Work PB1142): a SOLE inline method invocation "references a
                // temporary data item with the same class, category, and content as the temp-identifier" (ISO
                // §8.4.3.4.4 GR1), so an alphanumeric-returning one is a legal relation operand exactly as a sole
                // alphanumeric function is. The expression spine's §8.8.1.1 screen now reads an invocation's class
                // too, so the sole form must be short-circuited here for the same reason PB172 gave the function.
                // An object-view and SELF ride the same reduction (kb/Work PB1425): §8.8.4.2.15, "An operand of class
                // object may be compared with another operand of class object" — `IF O = SELF`, `IF U AS C = D`.
                : SoleOoIdentifier(expr) is { } soi ? host.Oo.OoIdentifierOperand(soi)
                // A sole numeric LITERAL stays a literal operand — against an alphanumeric/group operand it
                // participates as its WRITTEN character form, leading zeros intact (ISO §8.8.4.2.1), which a
                // computed wrapper would lose.
                : SoleNumLiteral(expr) is { } lit ? host.Expr.NumericLiteralOperand(lit)
                // The ONE expression→operand mapping (IntrinsicBinder.OperandOf): a user-function reference
                // binds to a BoundNumRef over its result temp, which MUST surface as a FIELD operand here so
                // the temp's cloned category (§8.4.3.2.4 GR1) drives the relation's class dispatch — a raw
                // computed wrapper would compare an alphanumeric/national result NUMERICALLY. Numeric
                // renderings are identical either way (AsNum unwraps both to the same FieldNum read).
                : IntrinsicBinder.OperandOf(host.Expr.BindExpr(expr));   // a COMPOUND relation operand is an arithmetic expression (§8.8.1.1): no index-name or index data item inside it (kb/Work PB2018)
        return BoundOperandError.Refused(ctx.Edition, "comparison operand");
    }

    /// <summary>The condition-name references already reported as ambiguous — one report per source reference.</summary>
    private readonly HashSet<Core.DataReferenceContext> _condDiagnosed = [];

    /// <summary>The condition-name references already reported as §13.7.3 SR4 e) violations.</summary>
    private readonly HashSet<Core.DataReferenceContext> _linkageCondDiagnosed = [];

    /// <summary>Resolve a condition-name reference, honoring OF/IN qualifiers (ISO §8.4.2.2 Format 2: a
    /// condition-name qualifies by its conditional variable and/or the variable's containing groups, innermost
    /// first) — duplicate 88 names across tables select by the qualifier chain.</summary>
    public Condition88? ConditionOf(Core.DataReferenceContext dref)
    {
        string name = dref.cobolWord()?.GetText() ?? dref.GetText();
        // §11.7 GR5 — a method-local 88 shadows object data; the overlay-first precedence lives in the ONE
        // scope-aware SymbolTable (P7 Step 10a, the DEVLOG-773 pickup), no longer duplicated inline here.
        if (!ctx.Symbols.TryResolveCondition(name, ctx.ActiveScope, out var list)) return null;
        var qualifiers = QualifiersOf(dref);
        // §8.4.2.2 Format 2 (kb/Work R33's sweep — the condition-name sibling of the data-name fix): the
        // reference must identify EXACTLY ONE level-88. TryResolveCondition returns one namespace tier, so a
        // plural SURVIVOR set — unqualified with duplicate 88 names, or qualifiers matching more than one —
        // is genuine ambiguity, previously resolved silently to the first declaration. Strict: report and
        // bind the first anyway (the compile already fails; no caller fallback runs a different reading).
        // --permissive: the traditional first match, warned.
        var matches = qualifiers.Count == 0
            ? list
            : list.Where(c => ctx.Data.ConditionQualifierChainMatches(c, qualifiers)).ToList();
        // §8.4.6.2.1 3) after qualification — a condition-name of this source element hides a container's global
        // one, a nearer container's a farther one (kb/Work PB1047); placed by its conditional variable.
        matches = ctx.Symbols.NearestDeclaring(matches, static c => c.Parent);
        // A lapsed type-declaration condition-name (§8.4.2.2.1 rule 4; DataBinder.WithLapsedTypeConditions) only
        // COUNTS against a condition-name the reference also reaches; alone it names nothing to test.
        if (matches.TrueForAll(DataBinder.IsTypeDeclarationCondition)) return null;
        // The NEGATIVE side of §13.16.3 SR23 (kb/Work PB567): no condition-name of this spelling is subordinate to
        // the written qualifiers, so this reference names no condition-name. It is not reported HERE because the
        // same word may still be a DATA-name those qualifiers do reach; the caller falls through to the data
        // resolution, whose one unidentified-reference report (ReferenceResolver.ReportUnidentified) words the
        // declared-but-misqualified condition-name case — or a caller that admits only a condition-name asks
        // ReportMisqualifiedCondition.
        if (matches.Count == 0) return null;
        // ONE report per SOURCE reference (the ReferenceResolver._diagnosed discipline, kb/Work PB70/PB443): a
        // reference is now resolved here more than once — the SEARCH ALL Format-2 screen asks which level-88 a
        // WHEN operand names BEFORE the condition binds it (§14.9.37.3 SR9/SR11 are about that very 88) — and the
        // ambiguity is one fact about one reference, not one per asker.
        if (matches.Count > 1 && _condDiagnosed.Add(dref))
        {
            if (ctx.Edition.Permissive)
                ctx.Edition.Warning(DiagnosticCatalog.UndefinedReference,
                    $"the condition-name '{DataBinder.WrittenText(dref)}' matches {matches.Count} level-88 declarations "
                    + "(ISO §8.4.2.2 — qualification shall establish uniqueness); --permissive resolves to "
                    + "the first declaration");
            else
                ctx.Edition.Error(DiagnosticCatalog.UndefinedReference,
                    $"the condition-name '{DataBinder.WrittenText(dref)}' does not uniquely identify a level-88 — "
                    + $"{matches.Count} declarations match (ISO §8.4.2.2 Format 2 — qualification shall "
                    + "establish uniqueness). Qualify by the conditional variable or its containing groups");
        }
        // §13.7.3 SR4 e): a condition-name of a linkage item is referenceable exactly when its conditional variable
        // is — the same decision the data-name arm asks (ReferenceResolver.ScreenLinkageReference), once per reference.
        if (ctx.Data.UnreferenceableLinkageRecord(matches[0].Parent) is { } record && _linkageCondDiagnosed.Add(dref))
            ctx.Refs.ReportUnreferenceableLinkage(DataBinder.WrittenText(dref), record);
        return matches[0];
    }

    /// <summary>For a position that admits ONLY a condition-name (SET Format 4's condition-name-1): when
    /// <paramref name="dref"/>'s word IS a declared level-88 condition-name but <see cref="ConditionOf"/> found
    /// none under the written qualifiers, report that through the ONE wording
    /// (<see cref="ReferenceResolver.MisqualifiedConditionText"/>) and answer true; otherwise report nothing and
    /// answer false, leaving the caller's own not-a-condition-name verdict (kb/Work PB567).</summary>
    public bool ReportMisqualifiedCondition(Core.DataReferenceContext dref)
    {
        string name = dref.cobolWord()?.GetText() ?? dref.GetText();
        if (!ctx.Symbols.TryResolveCondition(name, ctx.ActiveScope, out _)) return false;
        if (_condDiagnosed.Add(dref))
            ctx.Edition.Error(DiagnosticCatalog.UndefinedReference,
                ReferenceResolver.MisqualifiedConditionText(DataBinder.WrittenText(dref), name, QualifiersOf(dref)));
        return true;
    }

    /// <summary>The written qualifier words of <paramref name="dref"/>, innermost first.</summary>
    internal static List<string> QualifiersOf(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Select(sfx => sfx.qualification()?.cobolWord().GetText()).OfType<string>().ToList();

    // ── Operator mapping + helpers (ported from the former emitter) ──────────────────────────────────────────

    /// <summary>Fold a written relational operator to its canonical comparison. The operator SET is the grammar's
    /// <c>comparisonOperator</c>, which is §8.8.4.2.2 Format 1's printed alternatives and nothing else (kb/Work
    /// PB1034), so every spelling that reaches here is one the standard prints and the fold is total.</summary>
    public static string MapOperator(string raw)
    {
        string t = raw.ToUpperInvariant().Replace("IS", "").Replace("THAN", "").Replace("TO", "");
        if (t.Contains("<>")) return "!=";
        bool not = t.Contains("NOT");
        bool orEqual = t.Contains(">=") || t.Contains("<=") || t.Contains("OREQUAL");
        string baseOp =
            t.Contains('>') || t.Contains("GREATER") ? (orEqual ? ">=" : ">")
            : t.Contains('<') || t.Contains("LESS") ? (orEqual ? "<=" : "<")
            : "==";
        if (!not) return baseOp;
        return baseOp switch { ">" => "<=", ">=" => "<", "<" => ">=", "<=" => ">", "==" => "!=", _ => "==" };
    }

    public static Core.DataReferenceContext? SoleDataRef(Core.ArithmeticExpressionContext expr) =>
        SolePrimary(expr)?.dataReference();

    /// <summary>The raw text of an arithmetic expression that CONSISTS OF a sole numeric literal — sign
    /// included — else null. Figurative ZERO in an arithmetic context is that literal's <c>"0"</c>
    /// (§8.3.3.6.4 GR1).
    /// <para>⛔ THIS MEMBER DOES NOT RIDE <see cref="SolePrimary"/>, AND THAT IS THE RULE, NOT AN EXCEPTION
    /// (kb/Work PB400). Its siblings answer §8.8.4.7.3 SR2's "a single data item … not enclosed in
    /// parentheses", for which a leading sign makes the operand compound; this one answers "consists of a
    /// single literal", and §8.3.3.3.2 rule 2 puts an ADJACENT sign INSIDE the literal. Sharing the
    /// primary-only descent answered the second question with the first one's answer, so
    /// <c>EVALUATE -5 WHEN 6</c> classified as an arithmetic expression (Table 15 accepted a blank cell) and
    /// <c>EVALUATE WS-X WHEN -5</c> over a <c>PIC X</c> item bound the object as a COMPUTED expression instead
    /// of the literal §8.8.4.2.1 makes participate as its written character form — a run-time fault on
    /// conforming source, in EVALUATE and in the plain relation alike. The contiguity test itself is
    /// <see cref="SoleOperand.NumericLiteral"/>, shared with the §7.3.11.4 GR5 / §13.10.3 SR1 arms.</para></summary>
    public static string? SoleNumLiteral(Core.ArithmeticExpressionContext expr) =>
        SoleOperand.NumericLiteral(expr)
        ?? (SolePrimary(expr)?.ZERO_ARITH() is not null ? "0" : null);

    /// <summary>⛔ THE ONE "is this expression a single unparenthesized primary" DESCENT — the shared body of
    /// <see cref="SoleDataRef"/>, <see cref="SoleNumLiteral"/>, <see cref="SoleFunctionCall"/> and
    /// <see cref="SoleDataReference"/>. Any operator, unary sign, or enclosing parenthesis gives the node more
    /// than one child and stops the descent, which IS the §8.8.4.7.3 SR2 "single data item … not enclosed in
    /// parentheses" reduction and the §8.8.1.1 sole-vs-compound boundary at once.
    /// <para>⚠ IT REPLACES FOUR COPIES OF ITSELF, not the two the cluster analysis predicted — the fourth,
    /// <see cref="SoleFunctionCall"/>, was found only because adding a fifth collided with it at compile time
    /// (kb/Work PB172). Three were this single-child descent written out again; the former
    /// <see cref="SoleDataReference"/> was an independently-written tier-by-tier list-pattern version that agreed
    /// with them by coincidence rather than by construction. A family of four near-duplicates around a boundary
    /// this load-bearing is a drift hazard, and the collision was luck: two of them were byte-identical bodies
    /// differing only in the accessor on the last line (feedback_one_mechanism_per_job).</para>
    /// <para>⛔ AND THE BODY NOW LIVES ONE LAYER DOWN, in <see cref="SoleOperand"/> (kb/Work PB224). Two more
    /// copies survived the PB172 collapse because they were out of reach of a compiler-side helper —
    /// <c>IntrinsicBinder.SoleDataReference(FunctionArgumentContext)</c> (same assembly, a list-pattern
    /// re-implementation) and <c>CompileTimeExpressionEvaluator.SoleDataRef</c> (a DIFFERENT assembly,
    /// <c>Cobol.Net.Frontend</c>). The descent reads PARSE TREES and nothing else, so the frontend is its
    /// correct home and both layers now read the same body.</para></summary>
    private static Core.PrimaryExpressionContext? SolePrimary(Core.ArithmeticExpressionContext? expr) =>
        SoleOperand.Primary(expr);
}
