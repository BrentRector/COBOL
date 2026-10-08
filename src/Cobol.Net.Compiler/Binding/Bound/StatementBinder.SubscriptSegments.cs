// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;

namespace CobolNet.Binding.Bound;

/// <summary>
/// D18 — a FUNCTION-IDENTIFIER in a SUBSCRIPT or REFERENCE-MODIFIER position (fix-queue PB17).
///
/// <para><b>The problem.</b> <c>MOVE W-E(FUNCTION INTEGER(3)) TO W-R</c> and
/// <c>MOVE W-A(FUNCTION INTEGER(3):2) TO W-R</c> are LEGAL SOURCE that compiled clean and threw
/// <c>NotImplementedCobolFeatureException</c> at run time — the PB7/DA7 wrong-stage family. The chain, every
/// link mechanically checked: <b>§8.4.3.1.2</b> Format 1 makes a function-identifier an identifier →
/// <b>§15.4</b> "the evaluation of a function produces a returned value in a temporary elementary data item" →
/// <b>§15.2</b> items 5–6 put integer and numeric functions "of the class and category numeric" →
/// <b>§8.8.1.1</b> "an arithmetic expression may be an identifier referencing a numeric data item" →
/// <b>§8.4.2.3.2</b> + <b>§8.4.2.3.4 GR1b</b> admit <c>arithmetic-expression-1</c> as a subscript, and
/// <b>§8.4.3.3.3 SR4</b> as a leftmost-position/length.</para>
///
/// <para>⚠ <b>§8.4.3.2.3 SR11/SR12 do NOT bar it.</b> They bar functions where an <i>integer</i> or
/// <i>unsigned integer</i> is REQUIRED, and a subscript is neither: GR1b sets EC-BOUND-SUBSCRIPT when the
/// expression does not evaluate to an integer, a RUN-TIME condition that would be pointless if the position
/// required one syntactically (SR14 confirms it from the other side, having to impose that restriction
/// <i>specially</i> for a BY REFERENCE bit item).</para>
///
/// <para><b>The decision.</b> Materialize what §15.4 already describes, rather than teaching the resolver's direct
/// position walk to read function calls, which would make it a THIRD expression compiler beside
/// <c>ExpressionBinder</c> and <c>IntrinsicRenderer</c>. The position's PARSE NODE — its
/// <c>arithmeticExpression</c>, where the grammar parsed it — binds through the ONE
/// <c>ExpressionBinder.BindExpr</c> (documented as the entry "for COMPUTE, the arithmetic verbs, subscripts,
/// reference-modifier offsets"), so a USER-defined function, a nested function, and a keyword-omitted reference
/// all fall out of the same route. The value lands in a compiler temp via <c>DataBinder.CreateCompilerTemp</c> —
/// "THE ONE synthesized-compiler-temp constructor" — whose store registers as a statement-scoped pending PRE-op on
/// the shared <c>DataBinder.PendingPreOps</c>, drained at the <c>BindStatement</c> chokepoint by the mark-on-entry /
/// drain-own-suffix protocol that already served the UDF and object-property clients.</para>
///
/// <para>⛔ <b>One parse, one tree (kb/Work PB2151, D10's second half).</b> Until PB2151 the route recovered the
/// segment's verbatim TEXT and re-parsed it through a second, isolated grammar entry, so one position was parsed
/// twice and its temporary was named for the fragment's line 1. The node is now bound where it was parsed, and the resolver reads the temporary back as a typed <c>PositionItemRead</c>
/// (DESIGN-binder-bound-tree.md §3.9): the route is the general arm of the typed position carrier, not a
/// transitional one.</para>
/// </summary>
public sealed partial class StatementBinder
{
    /// <summary>The description of the temporary that holds a materialized segment's ORDINAL POSITION — an
    /// integer, because the integrality rule has already been asked by the time anything is stored in it.
    /// <para>⛔ <b>WHY THE TEST RUNS BEFORE THE STORE, NOT AT THE POSITION READ</b> (kb/Work PB1890). GR1b says a
    /// subscript whose expression "does not result in an integer" SETS EC-BOUND-SUBSCRIPT, and §8.4.3.3.4 rule 5)c)
    /// says the same for a ref-mod position. This temp used to be the §15.4 function-value item (21 integer + 9
    /// fraction digits, TRUNCATION) with the test left to the position read, so a value whose fraction lay past the
    /// 9th digit was an integer by the time it was asked: <c>T(IX + 0.0000000001)</c> and <c>T(F)</c> with
    /// <c>F COMP-2 2.0000000001</c> selected occurrence 2 with checking on. No fixed fraction width is exact for an
    /// arbitrary native intermediate, an SDIDI or a binary64, so <see cref="BoundPositionValue"/> asks the question
    /// of the value on its own carrier (docs/CONFORMANCE.md DOC-A.1-124) and stores only the answer.</para>
    /// <para>19 digits, signed: the position intakes SATURATE to <c>long</c> (<c>CobolNum.Position</c> — an
    /// out-of-range position must stay out of range, never wrap back into 1..n), and <c>long</c>'s range fits 19
    /// digits. The item takes the <c>Int128</c> wide tier (<c>PicInfo.IsWide</c>, &gt;18 digits), which the position
    /// read accepts (<see cref="ReferenceResolver.NumericPositionCarriers"/>). The temp is synthetic and never enters
    /// <c>DataBinder.ConformanceForest</c>, so the edition digit-capacity gates do not apply to it.</para></summary>
    private static readonly PicInfo SegmentTempPic =
        new(PicCategory.Numeric, Usage.Display, Length: 19, Digits: 19, Scale: 0, Signed: true);

    /// <summary>Materialize one position the resolver's direct walk cannot read — a function-bearing subscript or
    /// ref-mod bound, a division, an exponentiation, a decimal literal (the D18 route; the
    /// <c>ReferenceResolver.MaterializeSegment</c> hook), given the position's own <c>arithmeticExpression</c> parse
    /// node (kb/Work PB2151: the node is bound where the grammar parsed it). Returns the §15.4 temporary the value
    /// lands in, or <see langword="null"/> to leave the caller's loud posture untouched. A position that is not an
    /// arithmetic expression never arrives: the resolver refuses it by name (COBOLNET2363, kb/Work PB1030).
    ///
    /// <para><b>Ordering is why this binds HERE and not at the drain.</b> A nested segment
    /// (<c>W-E(FUNCTION INTEGER(W-F(FUNCTION INTEGER(2))))</c>) resolves its inner reference while THIS bind
    /// descends, so the inner temp's store registers on <c>PendingPreOps</c> BEFORE this one — exactly the
    /// property UdfBinder relies on ("a nested call registers while its consumer's arguments bind, so it precedes
    /// the consumer in the sequence"). Deferring the bind to the drain would append them in the wrong order.</para>
    ///
    /// <para><b>The store is a <see cref="BoundPositionValue"/></b>: the expression renders through the ONE
    /// numeric renderer and the ONE arithmetic store path, but the integrality question is asked of the value on
    /// its own carrier BEFORE the store, which a <c>BoundCompute</c> into a fixed-scale temp could not do (kb/Work
    /// PB1890). The temp is the saturated integer position, so a size error at the store is unreachable.</para></summary>
    private DataItem? BindPositionTemporary(Frontend.Generated.CobolParserCore.ArithmeticExpressionContext expr,
        SegmentPosition position, int line)
    {
        // ⛔ THE POSITION DECIDES THE CONTEXT (kb/Work PB170/PB172). ISO §13.18.38.3 r7 lists five contexts in
        // which an index-name may be referenced — "as a subscript; in the VARYING phrase of a PERFORM statement;
        // in the VARYING phrase of a SEARCH statement; in the SET statement; as an operand in a relation
        // condition" — and a REFERENCE-MODIFICATION position is not among them. This site hard-coded
        // BindIndexWindowExpr for BOTH positions under the comment "a SUBSCRIPT is an r7 window", which is true
        // of the position it names and false of the other one it serves: `W(IX:2)` was wrongly admitted. §8.4.3.3.3
        // SR4 makes both ref-mod bounds plain arithmetic expressions, so they bind under Arithmetic, where the
        // r7 screen fires.
        var value = position == SegmentPosition.Subscript
            ? Expr.BindIndexNameWindowExpr(expr)   // a SUBSCRIPT: r7 yes, SR10 no (kb/Work R29, PB215)
            : Expr.BindExpr(expr);             // a ref-mod bound is NOT (§8.4.3.3.3 SR4)
        if (value is BoundExprError err)
        {
            // Refused: the expression binder reported it. Unbuilt: an operand of the segment is a deferred shape, so
            // the REFERENCE is deferred too, not refused (kb/Work PB1030) — the resolver reads that from here.
            if (err.IsUnbuilt) refs.NoteSegmentDeferred();
            return null;
        }

        // The MODEL is a description carrier only: CreateCompilerTemp clones its Pic (and, for a group, its
        // subtree) and mints the temp's own names, so the model's own names are never emitted or registered.
        var model = new DataItem { Level = 1, CobolName = "__SUBEXPR-MODEL", CsName = "__subexprModel",
                                  Pic = SegmentTempPic };
        var temp = data.CreateCompilerTemp(model, "__SUBEXPR-", "__subexpr", $"L{line}");
        if (Ctx.Refs.ResolveItem(temp) is not { } place) return null;

        data.PendingPreOps.Add(new BoundPositionValue(value, place, RefMod: position == SegmentPosition.RefMod));
        return temp;
    }

    /// <summary>⛔ THE ODO LENGTH BEFORE THE REFERENCE MODIFIER (ISO §14.6.4 steps 6 and 7; the
    /// <c>ReferenceResolver.FreezeOdoExtent</c> hook; kb/Work PB1123). The reference modifier's position was
    /// materialized above as pre-ops from <paramref name="preOpMark"/> on, and a pre-op that activates a function can
    /// change the DEPENDING ON object, so the object's value is copied into a compiler temp as a pre-op INSERTED at
    /// <paramref name="preOpMark"/> — ahead of every one of them — and the group's extent reads the temp. The copy
    /// is a plain MOVE of the object into a temp of its own description, so the length it pins is exactly the value
    /// the operand site would have read had no pre-op run first.</summary>
    private Place FreezeOdoExtentBeforeRefMod(OdoGroupPlace odo, int preOpMark, int line)
    {
        var temp = data.CreateCompilerTemp(odo.Depending.Item, "__REFODO-", "__refodo", $"L{line}");
        if (Ctx.Refs.ResolveItem(temp) is not { } frozen) return odo;
        data.PendingPreOps.Insert(preOpMark, new BoundMove(new BoundFieldOperand(odo.Depending), [frozen]));
        return odo with { Depending = frozen };
    }
}
