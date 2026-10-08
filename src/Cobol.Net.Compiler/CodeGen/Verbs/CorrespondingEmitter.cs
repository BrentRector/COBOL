// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The MOVE/ADD/SUBTRACT CORRESPONDING emitter (P7 Step 9c — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the CSharpEmitter partial of the same name).</summary>
internal sealed class CorrespondingEmitter(EmitContext ctx, NumericRenderer num, MoveEmitter move, ArithmeticEmitter arith, EcEmitter ec)
{
    /// <summary>
    /// MOVE/ADD/SUBTRACT CORRESPONDING (ISO §14.7.6): render the bind-time-expanded pairs as the per-pair implied
    /// statements, in D1 declaration order. MOVE pairs are ordinary MOVEs (MOVE GR11 §14.9.25.4 — "the same as if
    /// the user had referred to each pair … in separate MOVE statements"), reusing the one MOVE emission path.
    /// ADD/SUBTRACT pairs run inside ONE statement-level ON SIZE ERROR region (§14.7.6): the shared
    /// <c>EmitArith</c> flag latches across the per-pair checked stores, an erring pair's receiver stays
    /// UNCHANGED while the remaining pairs still execute (§14.7.5), the ON imperative dispatches ONCE after all
    /// pairs complete, and the NOT branch is suppressed when any pair erred — never a per-pair phrase.
    /// </summary>
    public void Emit(BoundCorresponding c)
    {
        if (c.Verb is CorrVerb.Move)
        {
            EmitHoists(c.Hoists);
            Deferred(() =>
            {
                foreach (var p in c.Pairs)
                    move.Emit(p.Move!);   // BOUND with the pair (kb/Work PB880); never null for CorrVerb.Move
            });
            return;
        }
        // ADD GR3 (§14.9.2.4): target ← target + source. SUBTRACT GR3 (§14.9.44.4): "data items in identifier-4
        // are subtracted from and stored in corresponding items in identifier-5" — target ← target − source (GR5's
        // reduction to separate `SUBTRACT a FROM b` statements settles the operand order over the inverted-looking
        // standard-arithmetic print at GR3).
        string op = c.Verb is CorrVerb.Add ? "+" : "-";
        arith.EmitArith(c.SizeError, ise =>
        {
            EmitHoists(c.Hoists);
            Deferred(() =>
            {
                foreach (var p in c.Pairs)
                {
                    // ONE rounded-phrase mode for every pair (§14.7.4).
                    var rcv = arith.RcvFor(new Receiver(p.Target, c.Rounding), ise);
                    arith.StoreArith(p.Target,
                        num.Combine(num.FieldNum(p.Target), op, num.FieldNum(p.Source), rcv),
                        c.Rounding);
                }
            });
        });
    }

    /// <summary>
    /// Run the implied statements inside the §14.7.6 EC-DATA-INCOMPATIBLE DEFERRAL region: "For any statement
    /// with the CORRESPONDING phrase, if any of the implied statements would set the EC-DATA-INCOMPATIBLE
    /// exception condition to exist, the EC-DATA-INCOMPATIBLE exception condition is set to exist AFTER ALL OF
    /// THE IMPLIED STATEMENTS ARE COMPLETED."
    /// <para>Without the region a pair-1 raise would abandon pairs 2..n — exactly what that sentence forbids —
    /// because EC-DATA-INCOMPATIBLE is fatal (Table 13) and the per-pair sending reads raise inline. The runtime
    /// LATCHES the first detail instead; the region is left in a <c>finally</c> so an unrelated fatal still exits
    /// it, and the deferred raise is emitted AFTER the try so it can never displace an exception already in
    /// flight. The shape is §14.7.6's SIZE ERROR paragraph's: one latching flag, one dispatch after all pairs.</para>
    /// <para>Emitted only when EC-DATA-INCOMPATIBLE checking is enabled for this statement — with checking off
    /// no pair can raise, so the region would be pure overhead and the output stays byte-identical.</para>
    /// </summary>
    private void Deferred(Action emitPairs)
    {
        if (!ec.Enabled("EC-DATA-INCOMPATIBLE")) { emitPairs(); return; }
        var w = ctx.Writer;
        string pending = $"__corrInc{ctx.Names.NextEc()}";
        w.Line($"string? {pending} = null;");
        w.Line("ExceptionState.DataIncompatibleDeferBegin();");
        using (w.Block("try")) emitPairs();
        w.Line($"finally {{ {pending} = ExceptionState.DataIncompatibleDeferEnd(); }}");
        w.Line($"if ({pending} is not null) ExceptionState.DataIncompatibleError({pending});");
    }

    /// <summary>Anchor each group operand ONCE before the first pair (§14.7.6 — all item identification, including
    /// the group operands' subscripts, is done at the START of the statement, not per implied statement): a
    /// <c>ref var</c> local aliases a member-path group (evaluating the ref-returning table-subscript accessor
    /// (<c>At</c>) exactly once, so the alias is a true lvalue the pair stores write through); a
    /// <c>long</c> local pins a Tier-B REDEFINES view group's computed window offset.</summary>
    private void EmitHoists(IReadOnlyList<CorrespondingHoist> hoists)
    {
        foreach (var h in hoists)
            ctx.Writer.Line(h switch
            {
                { RefGroup: { } g } => $"ref var {h.Local} = ref {PlaceRenderer.Read(g)};",
                { ViewOffset: { } offset } => $"long {h.Local} = (long)({PositionRenderer.Render(offset)});",
                _ => throw new System.InvalidOperationException(
                    $"CorrespondingHoist '{h.Local}' sets neither RefGroup nor ViewOffset — exactly one is set by construction"),
            });
    }
}
