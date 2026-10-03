// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The arithmetic emitter (P7 Step 9h — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the orchestrator partial): ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE
/// bodies plus the shared services every numeric-storing verb consumes — the <c>EmitArith</c> ON SIZE ERROR /
/// EC-SIZE two-phase wrapper (writes the <see cref="EcState"/> statement scratch the checked stores read — the
/// EC↔arithmetic interlock) and the <c>StoreArith</c> store funnel.</summary>
internal sealed class ArithmeticEmitter(EmitContext ctx, NumericRenderer num, EcState ecState, EcEmitter ec)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the phrase bodies
    /// nest arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>In-place arithmetic (ADD TO / SUBTRACT FROM / MULTIPLY BY): each receiver ← receiver op Σoperands,
    /// rounded by the receiver's ROUNDED mode (ISO §14.7.4), under the statement's ON SIZE ERROR phrase if any.</summary>
    public void EmitInPlace(IReadOnlyList<Receiver> targets, string op, IReadOnlyList<BoundExpr> operands, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // The operand sum is the ONE initial evaluation (ISO §14.7.7 GR4 + NOTE 3): with several receivers it
            // must be materialized, or a receiver that aliases an operand (ADD A TO B A C) would poison the
            // receivers stored after it when the inlined expression re-reads the field. The fold itself is
            // receiver-INDEPENDENT (GR4 -- the initial evaluation precedes any receiver's involvement): None.
            NumX value = num.Fold(operands, ReceiverContext.None with { InSizeError = ise });
            if (targets.Count > 1) value = Snapshot(value);
            // Each receiver's FieldNum read is ALSO a sending reference (A = A + value; §14.6.13.2 NOTE 2), so a
            // non-finite float receiver is caught by its CobolFloat.Sending wrap. Precise follow-on (parallel to the
            // "precise ContainsRefMod filter" note): with MULTIPLE float receivers under EC-DATA-NOT-FINITE checking, a
            // later receiver's non-finite read raises AFTER earlier receivers already stored — a half-commit that is
            // only observable under USE-F3 + RESUME NEXT STATEMENT, where a fatal-EC-interrupted statement leaves
            // undefined results anyway (§14.6.13.1.3). Pre-snapshotting all receiver reads before the first store would
            // close it; deferred to avoid restructuring the byte-critical arithmetic store loop for an undefined case.
            foreach (var r in targets)
                GuardedStore(ise, () =>
                    // The combine IS the final transfer to r (receiver ← receiver op value): outermost, so a MULTIPLY
                    // BY product past the carrier rounds once at r's scale with r's mode (NumericRenderer.Multiply).
                    StoreArith(r.Place, num.Combine(num.FieldNum(r.Place), op, value, RcvFor(r, ise), outermost: true), r.Rounding));
        });

    /// <summary>GIVING arithmetic: the value is computed once and stored into each receiver, rounded by that
    /// receiver's own ROUNDED mode (ISO §14.7.5 rule 4 — one value, stored left-to-right into each resultant).</summary>
    public void EmitGiving(IReadOnlyList<Receiver> targets, Func<ReceiverContext, NumX> value, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // The RHS is computed FOR the receiver SET -- the widest receiver scale, Real only when EVERY
            // receiver is float -- the §14.7.7 GR4 one-initial-evaluation shape EmitCompute's multi-target path
            // established (D16). Pre-P7.3 this RHS rendered under the PREVIOUS statement's leftover Target*
            // state (EmitGiving never called SetTarget) -- the H1 staleness ReceiverContext kills.
            // The float-quantization headroom is the MOST CONSTRAINING receiver's — the widest integer part —
            // since one rendered value has to land in all of them (PB13).
            var rcv = new ReceiverContext(targets.Max(t => ScaleOf(t.Place)),
                targets.All(t => t.Place.Item.Pic is { IsFloat: true }), CobolRounding.Truncation, ise,
                targets.Max(t => IntDigitsOf(t.Place)));
            NumX v = value(rcv);
            // ONE initial evaluation (§14.7.7 GR4 + NOTE 3): materialized with several receivers so a receiver
            // aliasing a sender cannot change the value the remaining receivers store.
            if (targets.Count > 1) v = Snapshot(v);
            foreach (var r in targets) GuardedStore(ise, () => StoreArith(r.Place, v, r.Rounding));
        });

    /// <summary><c>MULTIPLY a BY b GIVING r…</c> (ISO §14.9.26): the product is each receiver's FINAL TRANSFER, so — like the
    /// quotient of <see cref="EmitDivide"/> — it renders PER RECEIVER at that receiver's scale and ROUNDED mode
    /// (<see cref="NumericRenderer.Multiply"/>'s outermost arm: a product past the Int128 carrier is the exact product
    /// rounded once, whatever width the receiver is — kb/Work PB1143's review finding N1), from operands that are
    /// identified and evaluated ONCE (§14.7.7 GR4 + NOTE 3), materialized with several receivers so a receiver that
    /// aliases an operand cannot poison the products stored after it.</summary>
    public void EmitMultiplyGiving(IReadOnlyList<Receiver> targets, BoundExpr a, BoundExpr b, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // Operand sub-expressions render for the receiver SET (the widest scale, Real only when EVERY receiver is
            // float — D16), as EmitDivide's operands do.
            var opRcv = targets.Count == 1 ? RcvFor(targets[0], ise)
                : new ReceiverContext(targets.Max(t => ScaleOf(t.Place)),
                    targets.All(t => t.Place.Item.Pic is { IsFloat: true }), CobolRounding.Truncation, ise,
                    targets.Max(t => IntDigitsOf(t.Place)));
            NumX ax = num.Render(a, opRcv), bx = num.Render(b, opRcv);
            if (targets.Count > 1) { ax = Snapshot(ax); bx = Snapshot(bx); }
            foreach (var r in targets)
                GuardedStore(ise, () =>
                    StoreArith(r.Place, num.Combine(ax, "*", bx, RcvFor(r, ise), outermost: true), r.Rounding));
        });

    public void EmitDivide(IReadOnlyList<Receiver> targets, BoundExpr? dividend, BoundExpr divisor, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // The SENDERS are identified/evaluated ONCE (ISO §14.9.12.4 GR5 → §14.7.7 GR4 + NOTE 3): with several
            // receivers both operands are materialized, so a receiver that aliases the dividend or the divisor
            // (DIVIDE b INTO a GIVING x a y — NC172A/NC173A) cannot poison the quotients stored after it. The
            // DIVISION itself still renders per receiver, at that receiver's scale + ROUNDED mode — equal to
            // rounding the spec's intermediate to each resultant. Sub-expression quotients inside the operands
            // render at the widest receiver scale (the intermediate must not lose receiver-visible digits).
            var opRcv = new ReceiverContext(targets.Max(t => ScaleOf(t.Place)),
                targets.All(t => t.Place.Item.Pic is { IsFloat: true }), CobolRounding.Truncation, ise,
                targets.Max(t => IntDigitsOf(t.Place)));
            NumX divisorX = num.Render(divisor, opRcv);
            NumX? dividendX = dividend is not null ? num.Render(dividend, opRcv) : null;
            if (targets.Count > 1)
            {
                divisorX = Snapshot(divisorX);
                if (dividendX is { } dx) dividendX = Snapshot(dx);
            }
            foreach (var r in targets)
            {
                // The quotient renders at the receiver's OWN scale + ROUNDED mode.
                NumX q = dividendX ?? num.FieldNum(r.Place);            // INTO-no-GIVING divides the target
                // The DIVIDE top-level quotient IS the final transfer to r — outermost, so it rounds at r's scale +
                // ROUNDED mode (§14.9.12.4 / §14.7.4). Operand sub-divisions (rendered above at :75-76) stay nested.
                // Each receiver's quotient can raise its OWN size error (a PROHIBITED-inexact at r's scale) —
                // guarded per receiver so the raise does not abandon the receivers to its right (PB129).
                GuardedStore(ise, () =>
                    StoreArith(r.Place, num.Combine(q, "/", divisorX, RcvFor(r, ise), outermost: true), r.Rounding));
            }
        });

    /// <summary><c>DIVIDE … GIVING q REMAINDER r</c> (ISO §14.9.12 GR7): the remainder is defined from the
    /// INTERMEDIATE quotient TRUNCATED at the quotient receiver's scale — even when the stored quotient is ROUNDED
    /// — as <c>remainder = dividend − (intermediate quotient × divisor)</c>; the subtraction aligns scales exactly.
    /// The quotient stores with its OWN rounding (recomputed at the receiver's mode when not truncation).
    /// Under STANDARD / STANDARD-DECIMAL (P10 Step 12) the subsidiary quotient stays this EXACT receiver-scale
    /// kernel division: GR6c truncates it at the receiver's digits, and the exact integer-remainder truncation
    /// equals the SDIDI-quotient-then-truncate result for every case except a true quotient carrying 34+
    /// consecutive nines at the rounding boundary (the same documented extra-precision residue class as the
    /// >34-digit exact intrinsics — CobolIntrinsics.Exact.cs header); the back-multiply/subtract DO evaluate
    /// in SDIDI form through the mode-aware <c>Combine</c>.</summary>
    public void EmitDivideRemainder(BoundDivideRemainder d)
        => EmitArith(d.SizeError, ise =>
        {
            var w = ctx.Writer;
            int qs = ScaleOf(d.Quotient.Place);
            // The senders render FOR the quotient receiver (its scale governs the intermediate, GR6c/GR7);
            // pre-P7.3 they rendered under the previous statement's leftover Target* state (H1).
            var rcv = new ReceiverContext(qs, d.Quotient.Place.Item.Pic is { IsFloat: true },
                CobolRounding.Truncation, ise, IntDigitsOf(d.Quotient.Place));
            // Both senders are materialized (§14.9.12.4 GR5 — one item identification/evaluation): each appears in
            // SEVERAL emitted expressions (kernel call(s) + the remainder back-multiply), and the quotient stores
            // BEFORE the remainder is formed — a quotient receiver aliasing a sender must not poison the remainder.
            // …in the exact Int128 lane the kernel below computes in (kb/Work PB85): a FLOAT-LONG dividend or
            // divisor lands TRUNCATED at the quotient receiver's float working scale (native — §14.9.12.4 GR7's
            // subsidiary quotient is then the fixed kernel's, an implementor approximation §15.4.1 permits for a
            // float sender; under a standard mode the float converts to SDIDI first and lands the same way); an
            // unsigned-wide read funnels through Widen. It was `Int128 __ie = (double)` — CS0266 on legal source.
            NumX dividend = Snapshot(num.FixedLane(num.Render(d.Dividend, rcv), rcv)),
                 divisor = Snapshot(num.FixedLane(num.Render(d.Divisor, rcv), rcv));
            // The SUBSIDIARY quotient is truncated to the GIVING receiver's digits/scale (ISO §14.9.12 GR6c) —
            // a DIRECT kernel call at EXACTLY the receiver scale, not the renderer's working-scale promotion
            // (which yields the quotient at the dividend's higher scale and poisons the remainder multiply).
            string qt = $"__q{ctx.Names.NextStoreTmp()}";
            w.Line($"Int128 {qt} = {RuntimeApi.NumDivide(ise, dividend.Expr, $"{dividend.Scale}", divisor.Expr, $"{divisor.Scale}", $"{qs}", CobolRounding.Truncation)};");
            // §14.9.12.4 GR6c (kb/Work PB129): the subsidiary quotient has "the same number of digits as
            // identifier-3" — cap it at the GIVING receiver's DIGIT capacity (the low-order digits, the
            // §14.7.5 no-phrase store's own disposition), or a quotient wider than identifier-3 poisons the
            // remainder (Q PIC 9 stored 3 while the back-multiply used 333).
            // The CAP applies to the REMAINDER's subsidiary quotient only — the quotient STORE below must
            // still see the uncapped value so an over-wide quotient raises ITS size error (§14.7.5).
            string qtc = $"__qc{ctx.Names.NextStoreTmp()}";
            w.Line($"Int128 {qtc} = {RuntimeApi.NumCapDigits(qt, DigitsOf(d.Quotient.Place))};");
            // The back-multiply rides Combine — the checked MulChecked fork under checking, the Dec lane under a
            // standard mode — instead of a raw C# splice whose Int128 product wrapped silently (PB129).
            NumX remainder = num.Combine(dividend, "-",
                num.Combine(new NumX(qtc, qs), "*", divisor, rcv), rcv);   // GR7: dividend − subsidiaryQuotient × divisor
            GuardedStore(ise, () => StoreArith(d.Quotient.Place,
                d.Quotient.Rounding == CobolRounding.Truncation
                    ? new NumX(qt, qs)
                    : new NumX(RuntimeApi.NumDivide(ise, dividend.Expr, $"{dividend.Scale}", divisor.Expr, $"{divisor.Scale}", $"{qs}", d.Quotient.Rounding), qs),
                d.Quotient.Rounding));
            // GR6c stores identifier-4 only "if the size error condition is not raised" — gated on the
            // statement's flag when checking is on (the quotient's raise leaves the remainder untouched).
            if (ise)
                using (w.Block($"if (!{ecState.SizeErrVar})"))
                    GuardedStore(true, () => StoreArith(d.Remainder, remainder, CobolRounding.Truncation));
            else
                StoreArith(d.Remainder, remainder, CobolRounding.Truncation);   // REMAINDER has no ROUNDED phrase
        });

    /// <summary>COMPUTE: the RHS is rendered per receiver (so a quotient is computed at that receiver's scale + mode)
    /// then stored, rounded by the receiver's ROUNDED mode, under the ON SIZE ERROR phrase if any.</summary>
    /// <summary>COMPUTE Format 2 — boolean-compute (ISO §14.9.8): render the boolean RHS ONCE at the §14.9.8.4
    /// GR3 width (the largest boolean ITEM referenced, carried at run time — <see cref="BooleanRenderer.RenderAtItemWidth"/>;
    /// an all-literal expression keeps its own length and the per-receiver store fits it), then store into each
    /// elementary boolean receiver with the §14.6.8.6 left-align / zero-fill / truncate discipline (the string
    /// store, pad '0'; JUSTIFIED honored). A multi-receiver COMPUTE materializes the value once (the
    /// §14.7.7-shaped once-evaluation).</summary>
    public void EmitComputeBoolean(BoundComputeBoolean cb)
    {
        string value = BooleanRenderer.RenderAtItemWidth(cb.Rhs, num);
        // One evaluation for multiple receivers (a boolean expr can read an item a prior receiver aliases).
        if (cb.Targets.Count > 1)
        {
            string tmp = $"__be{ctx.Names.NextStoreTmp()}";
            ctx.Writer.Line($"string {tmp} = {value};");
            value = tmp;
        }
        foreach (var t in cb.Targets)
        {
            // OperandPic (kb/Work PB157): a GROUP-USAGE BIT receiver's store width is its as-if PICTURE 1(m)
            // length (§13.18.29.4 GR1b) — raw Pic gave 0 and the store truncated the whole value.
            int width = t is RefModPlace ? -1 : t.Item.OperandPic?.Length ?? 0;
            // A SOLE positionless RHS — figurative ZERO, or a B-NOT fold of one (`COMPUTE B = B-NOT ZERO`,
            // reachable since kb/Work PB157 un-conflated ZERO from the ALL literal) — REPLICATES its pattern
            // to the receiver's width (§8.3.3.6.4 GR2 "repeated as necessary"); the '0'-padding store would
            // turn all-ones into 100…0.
            string store = cb.Rhs is BoundBoolAll all && width > 0
                ? EmitText.CsLiteral(ReplicateBits(all.Bits, width))
                : width < 0
                    ? value   // a ref-mod boolean receiver — the slice write fits via SpliceInto (pad '0')
                    : RuntimeApi.StrStoreBoolean(value, $"{width}", t.Item.Justified);
            ctx.Writer.Line(PlaceRenderer.Write(t, store));
        }
    }

    public void EmitCompute(BoundCompute c)
        => EmitArith(c.SizeError, ise =>
        {
            if (c.Targets.Count > 1)
            {
                // ONE initial evaluation (§14.7.7 GR4 + NOTE 3): the RHS renders ONCE — at the widest receiver
                // scale so no receiver-visible digit is lost — is materialized, and every receiver stores from the
                // temp with its own ROUNDED mode. Re-rendering per receiver would re-read senders a prior
                // receiver may alias. Real only when EVERY target is float (D16).
                var rcv = new ReceiverContext(c.Targets.Max(t => ScaleOf(t.Place)),
                    c.Targets.All(t => t.Place.Item.Pic is { IsFloat: true }), CobolRounding.Truncation, ise,
                    c.Targets.Max(t => IntDigitsOf(t.Place)));
                // A PRODUCT at the root of the RHS is each receiver's FINAL TRANSFER, so it renders per receiver like
                // MULTIPLY GIVING's (NumericRenderer.Multiply's outermost arm: exact, rounded once at the receiver's
                // scale with its mode — whatever width it is), from operands that are still evaluated ONCE.
                if (c.Rhs is BoundBinary { Op: '*' } product)
                {
                    NumX ax = Snapshot(num.Render(product.Left, rcv)), bx = Snapshot(num.Render(product.Right, rcv));
                    foreach (var r in c.Targets)
                        GuardedStore(ise, () =>
                            StoreArith(r.Place, num.Combine(ax, "*", bx, RcvFor(r, ise), outermost: true), r.Rounding));
                    return;
                }
                NumX v = Snapshot(num.Render(c.Rhs, rcv));
                foreach (var r in c.Targets)
                    GuardedStore(ise, () => StoreArith(r.Place, v, r.Rounding));
                return;
            }
            foreach (var r in c.Targets)
                // Single receiver: the RHS's top-level division (if any) IS the final transfer to r — render it
                // outermost so an outermost quotient rounds at r's scale + mode and a nested quotient does not
                // inherit r's mode (CA5; §14.7.7 rule 3 NOTE 1).
                GuardedStore(ise, () =>
                    StoreArith(r.Place, num.Render(c.Rhs, RcvFor(r, ise), outermost: true), r.Rounding));
        });

    /// <summary>A subscript / reference-modifier bound's pre-operation (kb/Work PB1890): the expression renders
    /// RECEIVER-LESS — no resultant scale truncates it, a nested quotient keeps its guard digits — and its value
    /// reaches the position intake on its own carrier (<see cref="NumericRenderer.IntakeCarrier"/>), which raises
    /// EC-BOUND-SUBSCRIPT / EC-BOUND-REF-MOD for a non-integer (§8.4.2.3.4 GR1b, §8.4.3.3.4 rule 5)c)) and yields the
    /// saturated integer position the integer temporary stores. It rides <see cref="EmitArith"/> with no phrase, so
    /// the expression's own size-error conditions (a zero divisor) keep the arithmetic statements' discipline.</summary>
    public void EmitPositionValue(BoundPositionValue p)
        => EmitArith(null, ise =>
        {
            var (carrier, value) = NumericRenderer.IntakeCarrier(
                num.Render(p.Value, ReceiverContext.None with { InSizeError = ise }));
            var position = new NumX(RuntimeApi.PositionValueOf(carrier, value.Expr, value.Scale, p.RefMod), 0);
            GuardedStore(ise, () => StoreArith(p.Temp, position, CobolRounding.Truncation));
        });

    /// <summary>The size-error catch pair (kb/Work PB129 — factored so the outer statement try and every
    /// per-receiver <see cref="GuardedStore"/> emit the identical discipline).</summary>
    private void WriteSizeCatches(string flag, string? ecnVar)
    {
        var w = ctx.Writer;
        if (ecnVar is not null)
        {
            int cid = ctx.Names.NextEc();
            w.Line($"catch (CobolSizeError __cse{cid}) {{ {flag} = true; {ecnVar} = __cse{cid}.EcName; }}");
            w.Line($"catch (System.OverflowException) {{ {flag} = true; {ecnVar} = \"EC-SIZE-OVERFLOW\"; }}");
        }
        else
        {
            w.Line($"catch (CobolSizeError) {{ {flag} = true; }}");
            w.Line($"catch (System.OverflowException) {{ {flag} = true; }}");
        }
    }

    /// <summary>Emit ONE receiver's compute+store inside its own try when size-error checking is on (kb/Work
    /// PB129): §14.7.7 r4b makes a raised size error PER-RECEIVER — "processing proceeds to the next resulting
    /// data item to the right" — and §14.7.5 storing rule 2 requires the untouched receivers to hold the values
    /// they would have had. The old single statement-wide try let one receiver's PROHIBITED/overflow raise
    /// abandon every receiver to its right. The unchecked fast path is byte-identical to before.</summary>
    private void GuardedStore(bool inSizeError, Action emitOne)
    {
        if (!inSizeError) { emitOne(); return; }
        using (ctx.Writer.Block("try")) emitOne();
        WriteSizeCatches(ecState.SizeErrVar!, ecState.SizeErrEcVar);
    }

    /// <summary>The <see cref="ReceiverContext"/> for receiver <paramref name="r"/> (P7 Step 3 — the pure
    /// factory replacing the mutable <c>SetTarget</c> context writes).</summary>
    /// <remarks>⛔ <c>FloatEdited</c> is carried because <see cref="ScaleOf"/> gives a FLOATING-POINT
    /// numeric-edited receiver its mask's SIGNIFICAND scale — a working-scale hint, not a scale the ROUNDED
    /// transfer rounds at (that resultant normalizes into the mask instead, D21/PB66). Only
    /// <see cref="ReceiverContext.FloatLanding"/> reads it (kb/Work PB647).</remarks>
    public ReceiverContext RcvFor(Receiver r, bool inSizeError) =>
        new(ScaleOf(r.Place), r.Place.Item.Pic is { IsFloat: true }, r.Rounding, inSizeError, IntDigitsOf(r.Place),
            FloatEdited: r.Place.Item.Pic is { IsFloatEdited: true });

    /// <summary>The optional <c>blankWhenZero</c> argument text for a numeric-edited store when the receiver
    /// carries BLANK WHEN ZERO (ISO §13.18.8 — zero stores all spaces, MOVE and arithmetic alike).</summary>
    public static string BwzFlag(DataItem item) => item.BlankWhenZero ? ", blankWhenZero: true" : "";

    /// <summary>The receiver's editing-config arguments (<see cref="EmitContext.EditCfg"/> — the mask's currency
    /// string + the unit's DECIMAL-POINT IS COMMA).</summary>
    private string EditCfg(PicInfo? pic) => ctx.EditCfg(pic);

    /// <summary>Materialize a rendered sender/initial-evaluation into a local temp (ISO §14.7.7 GR4 + NOTE 3 —
    /// ONE initial evaluation; results independent of sender/receiver storage overlap). Inlining the expression
    /// into each receiver's store would re-read its fields after earlier receivers stored.</summary>
    private NumX Snapshot(NumX v)
    {
        string t = $"__ie{ctx.Names.NextStoreTmp()}";
        // Total over the carriers (kb/Work PB85): a float sender snapshotted as `Int128 t = double` was CS0266.
        string cs = v.Dec ? "CobolDec" : v.Real ? "double" : v.U ? "UInt128" : "Int128";
        ctx.Writer.Line($"{cs} {t} = {v.Expr};");
        return v with { Expr = t };
    }

    /// <summary>
    /// Run an arithmetic statement's per-receiver stores (<paramref name="emitStores"/>), wrapping them in the
    /// two-phase ON SIZE ERROR machinery (ISO §14.7.5) when <paramref name="sizeErr"/> is present: a <c>__sizeErr</c>
    /// flag is set by any per-receiver overflow (<c>TryStore</c> false — phase b, the other receivers still store,
    /// rule 2) or by a <c>CobolSizeError</c> raised during evaluation (e.g. a zero divisor — phase a, no receiver
    /// changes, rule 4); the ON / NOT ON SIZE ERROR imperative then runs once. With no phrase the stores run
    /// unchecked (the plain <c>Store</c> path) — behavior unchanged.
    /// </summary>
    public void EmitArith(SizeErrorPhrase? sizeErr, Action<bool> emitStores)
    {
        var w = ctx.Writer;
        // EC-SIZE checking (>>TURN … EC-SIZE … CHECKING ON, ISO §7.3.25): an ENABLED statement routes through
        // the same two-phase TryStore/try-catch shape even WITHOUT the phrase, latching WHICH Table 13 condition
        // occurred so the §14.9.49 F3 selection and the fatal default see the precise level-3 name. Checking off
        // + no phrase = the unchecked fast path, byte-identical (deep-dive D10 / SSOT §18.16).
        var ecSize = ec.EnabledSizeNames();
        if (sizeErr is null && ecSize.Count == 0) { emitStores(false); return; }

        string flag = $"__sizeErr{ctx.Names.NextSizeErr()}";
        w.Line($"bool {flag} = false;");
        string? ecnVar = null;
        if (ecSize.Count > 0)
        {
            ecnVar = $"__sizeEc{ctx.Names.NextEc()}";
            w.Line($"string {ecnVar} = \"\";");
            ecState.SizeErrEcVar = ecnVar;
        }
        ecState.SizeErrVar = flag;
        using (w.Block("try")) emitStores(true);   // checked renders: DivideOrThrow / MulChecked (§14.7.5)
        // A zero divisor / PROHIBITED-inexact quotient raises CobolSizeError; an intermediate that overflows the
        // long engine raises OverflowException (the checked(...) the store wraps the value in). Both are the
        // statement's size error condition (ISO §14.7.5 — the phrase ENABLES checking, incl. case 5 intermediate
        // overflow). >long-range overflow still needs the Int128 carrier (G3). This OUTER try now catches only
        // the INITIAL evaluation's raises (a statement with no formable initial value has no results to store);
        // each receiver's compute+store sits in its own GuardedStore try (kb/Work PB129 — §14.7.7 r4b:
        // "processing proceeds to the next resulting data item to the right", §14.7.5 storing rule 2).
        WriteSizeCatches(flag, ecnVar);
        ecState.SizeErrVar = null;
        ecState.SizeErrEcVar = null;

        if (ecnVar is not null)
            ec.EmitSizeHandling(flag, ecnVar, ecSize, hasPhrase: sizeErr?.OnError is not null);

        if (sizeErr?.OnError is { } on)
        {
            using (w.Block($"if ({flag})")) Statements.EmitStatementList(on);
            if (sizeErr.NotOnError is { } notAlso)
                using (w.Block("else")) Statements.EmitStatementList(notAlso);
        }
        else if (sizeErr?.NotOnError is { } not)
            using (w.Block($"if (!{flag})")) Statements.EmitStatementList(not);
    }

    /// <summary>Store an arithmetic result into a numeric target place, rounding to the receiver scale with
    /// <paramref name="mode"/> (the receiver's ROUNDED phrase, ISO §14.7.4). Inside an ON SIZE ERROR statement
    /// (<see cref="EcState.SizeErrVar"/> set) it uses the checked <c>TryStore</c> — on overflow / PROHIBITED-inexact
    /// it sets the flag and leaves the receiver unchanged (§14.7.5); otherwise the plain <c>Store</c>.</summary>
    public void StoreArith(Place target, NumX value, CobolRounding mode)
    {
        var w = ctx.Writer;
        // ⛔ ONE INITIAL EVALUATION, AND ONE §14.7.4.3 r7 GATE OVER IT, FOR EVERY FIXED-SCALE RECEIVER CATEGORY
        // (kb/Work PB654). Both halves used to live inside the plain-numeric arm alone, and both were wrong there.
        //
        // THE EVALUATION. §14.7.7 rule 4 a): "The initial evaluation of the statement is done and the result of
        // this operation is placed in an intermediate data item"; rule 4 b): "the intermediate data item is stored
        // in or combined with and then stored in each single resulting data item". The r7 test below and the store
        // that follows are TWO READS OF THAT ONE INTERMEDIATE, so the emitter must write the expression ONCE. It
        // wrote it twice, and for a function reference whose successive references are not the same value —
        // FUNCTION RANDOM with no argument, §15.75.3 rule 5: "In each case, subsequent references without
        // specifying argument-1 return the next number in the current sequence" — the test examined one returned
        // value and the store landed the NEXT one (measured on seed 7: the statement consumed elements 2 AND 3).
        // Snapshot is the same materialization EmitCompute's multi-receiver arm already performs for rule 4, and
        // carries PB85's carrier total.
        //
        // THE GATE. §14.7.4.3 rule 7: "If the PROHIBITED phrase is specified, and the arithmetic value cannot be
        // represented exactly in the resultant identifier, the EC-SIZE-TRUNCATION exception condition is set to
        // exist, the size error condition exists" — and the receiver is left unchanged. CobolFloat.ToScaled lands
        // a PROHIBITED transfer TRUNCATED by design (its own doc says the emitter gates the store), so the test
        // has to precede the store or the discarded fraction is already gone. The plain-numeric arm had the gate;
        // the numeric-EDITED and LOCALE-edited arms did not, and both silently stored the truncated value:
        // `COMPUTE E ROUNDED MODE IS PROHIBITED = C2 * 1` with C2 = SQRT(3) into `PIC ZZ9.999` stored 1.732 and
        // raised nothing (feedback_two_arm_dispatch — one dispatch, three arms, one of them gated). Writing the
        // gate ONCE here, keyed on the ONE receiver-scale rule (PicInfo.ReceiverScale through ScaleOf), is
        // what makes the next receiver category inherit it.
        //
        // A FLOATING-POINT numeric-edited receiver is deliberately OUT: it has no fixed fraction scale to test
        // against (the significand truncates into the mask, D21/PB66), so r7's "cannot be represented exactly in
        // the resultant identifier" is a different predicate there and is not this gate's to answer.
        string? r7Inexact = null;
        if (ecState.SizeErrVar is not null && value.Real && mode == CobolRounding.Prohibited
            && target.Item.Pic is { IsFloat: false, IsFloatEdited: false } gpic
            && (gpic.LocaleEdit is not null || gpic.EditMask is not null || gpic.Category == PicCategory.Numeric))
        {
            value = Snapshot(value);
            r7Inexact = RuntimeApi.FloatInexactAtScale(value.Expr, $"{ScaleOf(target)}");
        }

        // The keyword an arm's own first test opens with: `if`, or `else if` once the r7 gate has written the
        // leading `if`. Idempotent — the gate is emitted at most once per store, whichever arm asks first.
        string Gate(string onFail)
        {
            if (r7Inexact is not { } test) return "if";
            r7Inexact = null;
            w.Line($"if ({test}) {onFail}");
            return "else if";
        }
        // A numeric-edited receiver stores the EDITED image of the result (ISO §14.7.7 — arithmetic results store
        // per the MOVE editing rules). ROUNDED applies BEFORE editing: the value is rescaled to the mask's
        // fraction scale with the receiver's mode (§14.7.4), then formatted.
        if (target.Item.Pic is { Category: PicCategory.NumericEdited, IsFloatEdited: true } fpic)
        {
            // A FLOATING-POINT numeric-edited resultant (D21/PB66): the exact result normalizes into the mask; ROUNDED
            // has no fixed scale to round to (the significand is truncated to the mask's digits, §14.6.8.4 GR2 →
            // §13.18.40's alignment rules). Under ON SIZE ERROR / EC-SIZE both directions out of range are the size
            // error condition (§14.7.5 cases 3 AND 4 — the receiver unchanged); without them the MOVE disposition
            // (overflow → the pinned image, underflow → zero) stands.
            string fcfg = BwzFlag(target.Item) + EditCfg(target.Item.Pic);
            if (ecState.SizeErrVar is { } fflag)
            {
                string fimg = $"__sv{ctx.Names.NextStoreTmp()}";
                string fOnFail = ecState.SizeErrEcVar is { } fecn ? $"{{ {fflag} = true; {fecn} = \"EC-SIZE-OVERFLOW\"; }}" : $"{fflag} = true;";
                w.Line($"if (!{RuntimeApi.EditTryFormatFloat(fpic, value, fimg, fcfg)}) {fOnFail}");
                w.Line($"else {PlaceRenderer.Write(target, fimg)}");
                return;
            }
            w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormatFor(fpic, value, "", "", fcfg)));
            return;
        }
        // A format-2 (LOCALE) numeric-edited resultant (kb/Work PB64 T6): the edit is the locale's LC_MONETARY at
        // store time (§13.18.40.5 r9–r15) and the scale the PICTURE's. Under ON SIZE ERROR / EC-SIZE the digit
        // CAPACITY (the picture's integer positions) is the size error condition — EC-SIZE-TRUNCATION — while
        // EC-LOCALE-SIZE (r14 b's character truncation into the SIZE-declared item) is a DISTINCT condition the
        // edit itself raises; both can arise on one store. This arm sits BEFORE the mask arm: a locale item has
        // no EditMask, and without it the store fell to the "arithmetic into a non-fixed-point target" LoudStmt.
        if (target.Item.Pic is { LocaleEdit: not null } lpic)
        {
            int ls = lpic.Scale;
            string lAligned(bool checkedPath) =>
                value.Real ? RuntimeApi.FloatToScaled(value.Expr, $"{ls}", mode, checkedPath)
                : value.Dec ? (checkedPath ? RuntimeApi.DecToUnscaledChecked(value.Expr, $"{ls}", mode)
                                           : RuntimeApi.DecToUnscaled(value.Expr, $"{ls}", mode))
                : value.Scale == ls ? value.Expr
                : RuntimeApi.NumRescaleStore(value.Expr, $"{value.Scale}", $"{ls}", mode, checkedPath);
            string lcfg = BwzFlag(target.Item);   // no EditCfg, no EditsArg — a locale item takes neither
            if (ecState.SizeErrVar is { } lflag)
            {
                string limg = $"__sv{ctx.Names.NextStoreTmp()}";
                string lOnFail = ecState.SizeErrEcVar is { } lecn ? $"{{ {lflag} = true; {lecn} = \"EC-SIZE-TRUNCATION\"; }}" : $"{lflag} = true;";
                w.Line($"{Gate(lOnFail)} (!{RuntimeApi.EditTryFormatLocale(lpic, lAligned(true), $"{ls}", limg, lcfg)}) {lOnFail}");
                w.Line($"else {PlaceRenderer.Write(target, limg)}");
                return;
            }
            w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormatFor(lpic, value, lAligned(false), $"{ls}", lcfg)));
            return;
        }
        if (target.Item.Pic is { Category: PicCategory.NumericEdited, EditMask: { } mask })
        {
            int ms = RuntimeApi.MaskScale(target.Item.Pic!, mask, '$', ctx.Data.DecimalPointIsComma);
            // The narrowing rescale: under ON SIZE ERROR / EC-SIZE, a PROHIBITED-inexact transfer to an edited
            // receiver is a size error (ISO §14.7.4.3 r7 — the receiver stays UNCHANGED). The Dec path's
            // .ToUnscaled and the numeric path's TryStore already throw/flag on that; the Int128 edited path used
            // plain Rescale (silent truncation) — the DEVLOG-610-audited PROHIBITED leak. Use RescaleChecked in
            // the checked branch so all three receiver categories agree; the unchecked branch stays silent
            // (matching the numeric Store path's no-phrase behavior). The Dec arm's checked form is likewise
            // ToUnscaledChecked (kb/Work PB74): the unchecked ToUnscaled hands EditTryFormat the low-order digits
            // of an out-of-carrier value — 0 for 10 ** 100 — which the mask then "fits", no size error.
            string Aligned(bool checkedPath) =>
                // A float (Real) result lands at the mask scale via the runtime's ToScaled with the receiver's ROUNDED
                // mode (D16 review: the edited-receiver arithmetic path was missed by the Real integration → CS1503) —
                // the CHECKED landing under the phrase, the low-order digits without it (kb/Work PB77: the Real arm
                // ignored checkedPath, so a no-phrase edited store of a value past the carrier formatted the sentinel).
                value.Real ? RuntimeApi.FloatToScaled(value.Expr, $"{ms}", mode, checkedPath)
                : value.Dec ? (checkedPath ? RuntimeApi.DecToUnscaledChecked(value.Expr, $"{ms}", mode)
                                           : RuntimeApi.DecToUnscaled(value.Expr, $"{ms}", mode))
                : value.Scale == ms ? value.Expr
                : RuntimeApi.NumRescaleStore(value.Expr, $"{value.Scale}", $"{ms}", mode, checkedPath);
            // Under ON SIZE ERROR an edited resultant is capacity-checked too (ISO §14.7.5 case 3 + storing rule
            // 2): an aligned |value| exceeding the mask's digit positions sets the flag and leaves the receiver
            // UNCHANGED — Format's silent high-order truncation is MOVE behavior only (§14.9.25).
            if (ecState.SizeErrVar is { } eflag)
            {
                string img = $"__sv{ctx.Names.NextStoreTmp()}";
                // EC-SIZE checking latches the Table 13 condition: a store whose significant digits do not fit
                // the receiver is EC-SIZE-TRUNCATION ("significant digits truncated in store").
                string onFail = ecState.SizeErrEcVar is { } ecn1 ? $"{{ {eflag} = true; {ecn1} = \"EC-SIZE-TRUNCATION\"; }}" : $"{eflag} = true;";
                w.Line($"{Gate(onFail)} (!{RuntimeApi.EditTryFormat(Aligned(true), $"{ms}", CsLiteral(mask), img, BwzFlag(target.Item) + EditCfg(target.Item.Pic))}) {onFail}");
                w.Line($"else {PlaceRenderer.Write(target, img)}");
                return;
            }
            w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormat(Aligned(false), $"{ms}", CsLiteral(mask), BwzFlag(target.Item) + EditCfg(target.Item.Pic))));
            return;
        }
        // A FLOATING-POINT resultant identifier (COMP-1/2, FLOAT-*, D16) — the float arm of THIS store, sharing the
        // fixed arm's two rules rather than restating them (kb/Work PB1196): the receiver's §14.7.4.3 mode rounds
        // the value into the binary format (rule 10's implied TRUNCATION, rules 3–9, rule 7's PROHIBITED as an
        // exactness test), and under ON SIZE ERROR / EC-SIZE checking a value further from zero than the format
        // permits is the §14.7.5 case-3 size error, receiver unchanged, EC-SIZE-TRUNCATION (no-phrase rule 4) —
        // the SAME condition name the fixed arm latches below. It used to be a bare IEEE round-to-nearest cast that
        // ignored the mode and never raised. The rounding itself is ONE runtime rule, FloatResultant.
        if (target.Item.Pic is { IsFloat: true } floatPic)
        {
            // A WINDOWED float receiver (Tier-B / image-stored — the Step D arm-1 dissolution) re-encodes the
            // landed value as its IEEE window bytes; a native receiver keeps the bare cast (the scout's pre-loaded
            // CS1503: a float expression assigned into a string window).
            string Encode(string landed)
            {
                string algebraic = $"({floatPic.ClrType})({landed})";
                return target.Item.StoreAsImage
                    ? RuntimeApi.NumFormatImageFloat(algebraic, target.Item.ProfileName, floatPic.IsSingle)
                    : algebraic;
            }
            if (ecState.SizeErrVar is { } fflag)
            {
                string tmp = $"__sv{ctx.Names.NextStoreTmp()}";
                string onFail = ecState.SizeErrEcVar is { } fecn ? $"{{ {fflag} = true; {fecn} = \"EC-SIZE-TRUNCATION\"; }}" : $"{fflag} = true;";
                w.Line($"if (!{RuntimeApi.FloatResultantStore(value, mode, floatPic.IsSingle, tmp)}) {onFail}");
                w.Line($"else {PlaceRenderer.Write(target, Encode(tmp))}");
                return;
            }
            w.Line(PlaceRenderer.Write(target, Encode(RuntimeApi.FloatResultantStore(value, mode, floatPic.IsSingle))));
            return;
        }
        if (target.Item.Pic is not { Category: PicCategory.Numeric, IsFloat: false })
        {
            w.Line(LoudStmt($"arithmetic into a non-fixed-point target '{target.Item.CobolName ?? PlaceRenderer.Read(target)}'"));
            return;
        }
        string profile = target.Item.ProfileName;
        // A float (Real) arithmetic result lands into this FIXED receiver via the runtime's ToScaled at the receiver
        // scale with the receiver's ROUNDED mode (D16), then flows through the ordinary store funnel (rescale
        // identity ⇒ no double-rounding; capacity + SIZE ERROR still apply). A STANDARD-DECIMAL intermediate stores
        // through the SDIDI overloads (the §14.7 final transfer).
        int recvScale = target.Item.Pic!.Scale;
        // The ONE carrier switch (PB84); the landing's form is the statement's (PB77) — CHECKED under ON SIZE ERROR /
        // EC-SIZE checking (the branch below), the low-order digits for the no-phrase store.
        string args = NumericRenderer.StoreArgs(value, recvScale, mode, profile, checkedLanding: ecState.SizeErrVar is not null);
        if (ecState.SizeErrVar is { } flag)
        {
            string tmp = $"__sv{ctx.Names.NextStoreTmp()}";
            // Intermediate long-engine overflow is detected upstream by the checked multiply the renderer emits in a
            // size-error context (the checked multiply → OverflowException, caught by the statement's try, §14.7.5
            // case 5). We do NOT wrap the value in checked(...) here: a constant subexpression would then overflow at
            // COMPILE time (CS0220) and reject valid COBOL — the runtime helper avoids that by not constant-folding.
            // Under EC-SIZE checking the receiver-capacity failure latches EC-SIZE-TRUNCATION (Table 13 —
            // "significant digits truncated in store"; the §14.7.5 size error on the final transfer).
            string onFail = ecState.SizeErrEcVar is { } ecn2 ? $"{{ {flag} = true; {ecn2} = \"EC-SIZE-TRUNCATION\"; }}" : $"{flag} = true;";
            // A float (Real) source under ROUNDED MODE PROHIBITED: an inexact transfer is a size error and leaves the
            // receiver UNCHANGED (§14.7.4.3 item 7 — NOT §14.7.5, which this cited until kb/Work PB623 re-derived it;
            // §14.7.5 is the SIZE ERROR phrase, §14.7.4.3 is the ROUNDED phrase's per-mode general rules). The gate
            // is written ONCE, at the top of this method, for all three fixed-scale receiver categories (PB654);
            // Gate hands back the keyword this test opens with.
            w.Line($"{Gate(onFail)} (!{RuntimeApi.NumTryStore(args, mode, tmp, value.U)}) {onFail}");
            // On success store the value (a whole-group-aliased numeric-DISPLAY receiver stores its character image).
            w.Line($"else {PlaceRenderer.Write(target, target.Item.StoreAsImage ? RuntimeApi.NumFormatImage(tmp, profile) : Narrow(tmp, target.Item))}");
            return;
        }
        string stored = RuntimeApi.NumStoreRounded(args, mode, value.U);
        w.Line(PlaceRenderer.Write(target, target.Item.StoreAsImage ? RuntimeApi.NumFormatImage(stored, profile) : Narrow(stored, target.Item)));
    }

    /// <summary>The receiver's working scale: an edited receiver's is its MASK's fraction scale (a `.`-pointed
    /// mask has PicInfo.Scale 0 — the point lives in the mask, not in V); a numeric item's is its PIC scale.</summary>
    /// <summary>Wrap a wide (Int128) stored value for assignment into the receiver's CARRIER type: a ≤18-digit
    /// item stores as native <c>long</c> (the value is already truncated/rounded to the receiver's digits, so the
    /// cast is exact); an unsigned 8-byte BinaryCapacity item casts to its <c>ulong</c> carrier (the stored
    /// residue is in [0, 2^64) — exact); a 16-byte UNSIGNED BinaryCapacity item reinterprets the store's
    /// container BITS as <c>UInt128</c> (<c>CobolNum.WrapBinary</c>'s documented 16-byte contract — kb/Work R10);
    /// a signed 19+-digit item (the 2002+ wide tier) stores the Int128 directly. Keyed on the CARRIER, never the
    /// digit count — the digit-count key is exactly how the F75 fold/storage disagreement shipped.</summary>
    public static string Narrow(string expr, DataItem item) => item.Pic switch
    {
        { IsUnsignedWideBinary: true } => $"unchecked((UInt128)({expr}))",
        { IsUnsignedLongBinary: true } => $"(ulong)({expr})",
        { Digits: > 18 } => expr,
        _ => $"(long)({expr})",
    };

    private int ScaleOf(Place p) =>
        // The ONE receiver-scale rule (PicInfo.ReceiverScale; PB64 T6 — this copy and MoveEmitter's
        // SenderContext were the same rule written twice, and both fell to pic.Scale = 0 for a LOCALE item).
        p.Item.Pic is { } pic ? pic.ReceiverScale(ctx.Data.DecimalPointIsComma) : 0;

    /// <summary>The receiver's INTEGER digit positions — <see cref="ReceiverContext.IntegerDigits"/>, which caps
    /// the float quantization working scale (PB13). Measured from <c>DigitPositions</c> (ISO §13.18.40.3 SR14 —
    /// '9' plus Z/*/floating-sign/P positions), never from the '9' count alone: for a numeric-EDITED receiver
    /// <c>Digits</c> counts only '9', so <c>PIC ZZZ9</c> would report 1 integer digit where it holds 4, and
    /// UNDER-counting is the one direction that breaks <see cref="ReceiverContext.FloatWorkingScale"/>'s
    /// saturation-is-visible invariant. A place with no PICTURE (a float item, an index) reports 0 = unconstrained;
    /// a float receiver never reaches the quantizer at all.</summary>
    private int IntDigitsOf(Place p) =>
        p.Item.Pic is { } pic ? Math.Max(0, pic.DigitPositions - ScaleOf(p)) : 0;

    /// <summary>The receiver's total DIGIT capacity (§14.9.12.4 GR6c's "same number of digits as
    /// identifier-3" — the subsidiary-quotient cap, kb/Work PB129).</summary>
    private static int DigitsOf(Place p) => p.Item.Pic?.DigitPositions ?? 0;

    /// <summary>A positionless boolean pattern repeated to <paramref name="width"/> positions and truncated
    /// on the right (ISO §8.3.3.6.4 GR2 — "repeated as necessary"); compile-time, the pattern is a bound
    /// constant (kb/Work PB157).</summary>
    private static string ReplicateBits(string bits, int width)
    {
        var sb = new System.Text.StringBuilder(width);
        while (sb.Length < width) sb.Append(bits);
        return sb.ToString(0, width);
    }
}
