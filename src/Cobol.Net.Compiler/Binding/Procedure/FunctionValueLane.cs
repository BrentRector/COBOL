// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;
using CobolNet.Common;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// ⛔ <b>WHICH CARRIER A NUMERIC FUNCTION'S RETURNED VALUE ARRIVES ON, ASKED AT BIND TIME</b> (kb/Work PB3268) — the
/// question the intermediate result item of ISO §14.9.25.4 GR1 has to answer before the code generator has rendered
/// anything: is the value a <b>binary64</b> (a COMP-2 intermediate keeps it whole and each receiver's own float
/// landing quantizes it once, at its own scale), or an exact value (a fixed-point intermediate described by the
/// receivers)? §15.4.1: "When native arithmetic is in effect, the characteristics and representation of the returned
/// value are defined by the implementor", and the implementor's determination is made by
/// <c>IntrinsicRenderer.RenderNum</c>'s dispatch — three carriers (the exact scaled <c>Int128</c>, the SDIDI, binary64)
/// chosen from the function and from the carriers of its ARGUMENTS.
/// <para><b>The function-level rule is restated here once</b> (<see cref="ReturnsBinary64"/>), in the renderer's own
/// order: a function with a native SDIDI body (<see cref="NativeSdidiBodies"/>) takes an SDIDI-carried argument — a
/// floating-point literal (owner decision D-B), a floating-point numeric-edited sender, an integer power — onto the
/// SDIDI lane, ahead of the float lane; a mixed selection list (MAX / MIN / ORD-MAX / ORD-MIN / MEDIAN over a float
/// beside a fixed-point item) rides the SDIDI too; the §15.4.1 floating-math family (<see cref="IntrinsicSig.Float"/>)
/// and any numeric-argument function given a binary64 argument then take the binary64 lane; everything else is exact.
/// A decimal-engine arithmetic mode has no binary64 lane at all (§15.4.1: the returned value is "contained in a
/// temporary standard data item in the intermediate form defined for the arithmetic mode in effect").</para>
/// <para><b>The argument-level facts are read from the BOUND TREE</b> by <see cref="ArgumentCarrier"/>, a visitor over
/// the same two interfaces <c>NumericRenderer</c> implements — so a bound node added tomorrow is a COMPILE error here
/// as it is there — following <c>NumericRenderer.CombineCore</c> and <c>Power</c> for the arithmetic around a float.
/// A function whose value is fixed by its definition (NUMVAL, NUMVAL-C) is SDIDI-carried in every mode, and a nested
/// cross-aligning call (SUM, RANGE, MEAN, MEDIAN, MIDRANGE) is SDIDI-carried when <see cref="AlignmentMayLeaveCarrier"/>
/// says its aligned arguments may leave the <c>Int128</c> carrier (the digit bounds of the field and literal arguments
/// are read here as the renderer reads them; an argument whose bound only the renderer's arithmetic knows is unproven,
/// as it is there). What it cannot see is a run-time fact (a wide exact intermediate that overflows to the SDIDI, a
/// data-dependent width): those only move a value between the two NON-binary64 carriers, which this question does not
/// distinguish.
/// <c>FunctionValueLaneDriftTests</c> pins the one list restated here against the renderer's switch, and the
/// <c>pb3268_function_value_lane_matrix</c> golden holds the whole prediction against the renderer's actual answer
/// (a multi-receiver MOVE stores what the single-receiver MOVE stores) over a function × argument-shape matrix.</para>
/// </summary>
internal static class FunctionValueLane
{
    /// <summary>The runtime-method names <c>IntrinsicRenderer.RenderDec</c> has a body for under NATIVE arithmetic — the
    /// functions that, handed an SDIDI-carried argument, compute on the SDIDI instead of in binary64.
    /// <c>FunctionValueLaneDriftTests</c> derives the set from the renderer's switch, so a body added there without a
    /// line here fails the build of the gate. (<c>Sqrt</c> and <c>Factorial</c> have arms too, but only
    /// <c>when num.StandardDecimal</c>: a native call to either keeps its lane.)</summary>
    internal static readonly FrozenSet<string> NativeSdidiBodies = new[]
    {
        "Numval", "NumvalC", "NumvalF", "CombinedDatetime", "SignOf", "AbsScaled", "Floor", "Truncate", "FractionPart",
        "ModScaled", "RemScaled", "MaxScaled", "MinScaled", "OrdMax", "OrdMin", "SumScaled", "RangeScaled", "MeanScaled",
        "MedianScaled", "MidrangeScaled", "Variance", "StandardDeviation", "Annuity", "PresentValue",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The pure selections, which a MIXED list takes onto the SDIDI under native arithmetic so the compared and
    /// selected argument keeps its own carrier: MAX / MIN return "the content of the argument-1 having the greatest
    /// [least] value" (§15.59.4 r1 / §15.63.4 r1), ORD-MAX / ORD-MIN "the ordinal number that corresponds to the position
    /// of the argument-1 having the greatest [least] value" (§15.71.4 r1 / §15.72.4 r1), and MEDIAN over an odd count the
    /// middle argument (§15.61.1 "returns the content of the argument whose value is the middle value"; §15.61.4 r2 makes
    /// an even count the arithmetic mean of the two middle values, which is not a selection). Every comparison is made
    /// "according to the rules for simple conditions" (§15.59.4 r1 / §15.63.4 r1 / §15.71.4 r2 / §15.72.4 r2 / §15.61.4 r3).</summary>
    private static readonly FrozenSet<string> Selections = new[]
    {
        "MaxScaled", "MinScaled", "OrdMax", "OrdMin", "MedianScaled",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>True when <paramref name="call"/>'s returned value is a binary64 wherever no fixed-point scale governs the
    /// render — the lane <c>IntrinsicRenderer.RenderNum</c> reaches through <c>RenderFloat</c> under NATIVE arithmetic.
    /// Always false under a decimal-engine mode and for an INTEGER function (§15.2 item 5).</summary>
    internal static bool ReturnsBinary64(BoundIntrinsicCall call, ArithmeticMode mode) =>
        !ArithmeticModes.IsDecimalEngine(mode) && Of(call) is CarrierKind.Real or CarrierKind.ApproximateReal;

    /// <summary>The carrier the native renderer returns a numeric function's value on, as far as the kind is concerned.</summary>
    private static CarrierKind Of(BoundIntrinsicCall call)
    {
        var sig = call.Sig;
        // NUMVAL / NUMVAL-C return the SDIDI in EVERY mode — RenderNum's first arm, ahead of the mode dispatch and of
        // whatever their arguments are (the value is fixed by the function's definition).
        if (ValueFixedByDefinition(sig.RuntimeMethod)) return CarrierKind.Dec;
        // An INTEGER function (§15.2 item 5) and a function of a non-numeric result (reachable in a numeric context only
        // under --permissive) are exact values; the renderer never returns them on a binary64.
        if (call.ResultCategory is not PicCategory.Numeric || IsInteger(call)) return CarrierKind.Fixed;
        var args = call.Args.Select((a, i) => AtPosition(sig.Name, i, a.Accept(ArgumentCarrier.Instance))).ToList();
        bool anyDec = args.Any(a => a.Kind == CarrierKind.Dec);
        bool anyReal = args.Any(a => a.IsReal);
        bool allReal = args.Count > 0 && args.All(a => a.IsReal);
        bool sdidiBody = NativeSdidiBodies.Contains(sig.RuntimeMethod);
        // The SDIDI lane precedes the float lane: an SDIDI-carried argument (a floating-point literal, ...) is computed
        // on the SDIDI by a function that has a body for it (RenderNum's `AnyDecRaw(ic) && RenderDec(ic)` arm).
        if (anyDec && sdidiBody) return CarrierKind.Dec;
        // A cross-aligning arm (SUM / RANGE / MEAN / MEDIAN / MIDRANGE) whose aligned arguments may leave the Int128 carrier
        // takes the SDIDI (RenderNum's PB621 arm) — ahead of the float lane, whatever the arithmetic around it.
        if (IntrinsicRenderer.CrossAlignedNativeArms.Contains(sig.RuntimeMethod)
            && AlignmentMayLeaveCarrier(sig.RuntimeMethod, args.Select(a => a.Aligned).ToList()))
            return CarrierKind.Dec;
        // A mixed selection list rides the SDIDI too (RenderNum's selection arm).
        if (Selections.Contains(sig.RuntimeMethod) && anyReal && !allReal && sdidiBody) return CarrierKind.Dec;
        if (sig.Float) return CarrierKind.ApproximateReal;
        // RenderNum's last dispatch: a binary64 argument at a numeric position moves the whole call onto the float lane.
        if (anyReal && IntrinsicArgumentRules.ArgumentRunIsAllNumeric(sig.Name, args.Count)) return CarrierKind.ApproximateReal;
        return CarrierKind.Fixed;
    }

    /// <summary>The argument as the renderer's intake (<c>IntrinsicRenderer.RawArg</c>) hands it to the lane predicates: at a
    /// §15.3 type-6 (integer) position a binary64 or fractional fixed-point operand is already its INTEGER on the exact
    /// carrier, so it moves no call onto the float lane; an SDIDI operand stays on the SDIDI (as its integer).</summary>
    private static Carrier AtPosition(string functionName, int position, Carrier argument) =>
        argument.Kind != CarrierKind.Dec && !(argument.Kind == CarrierKind.Fixed && argument.Integral)
            && IntrinsicArgumentRules.IsIntegerPosition(functionName, position)
            ? Carrier.Exact(true)
            : argument;

    /// <summary>
    /// The functions whose DEFINITION fixes the returned value in EVERY arithmetic mode, so §15.4.1's
    /// implementor latitude never reaches them and no working scale may be imposed (kb/Work PB251). Asked by
    /// <c>IntrinsicRenderer.RenderNum</c> BEFORE the arithmetic-mode dispatch.
    /// </summary>
    /// <remarks>
    /// <para>⛔ THE SET IS TWO, AND IT WAS DERIVED BY SWEEPING CLAUSE 15 RATHER THAN BY RECALL — the refuted
    /// adjudication's screen ("standard-decimal arithmetic is in effect" + "34 digits") could only find
    /// MODE-CONDITIONED specifications and so missed the one function that states its value unconditionally.
    /// The sound screen is: a §15.x "Returned value rule(s)" clause that mentions NEITHER an arithmetic mode,
    /// NOR "implementor", NOR "approximation", NOR an equivalent arithmetic expression. Over clause 15 that
    /// yields 38 functions, and every one but these two returns an INTEGER, a STRING, or a selected argument's
    /// own content — shapes no fraction-digit working scale can damage. E (§15.27.3 r1), PI (§15.73.3 r1),
    /// SQRT (§15.84.4 r4) and NUMVAL-F (§15.69.4 r2) each say "native … implementor-defined approximation" in
    /// so many words, which is why they keep the float lane and are NOT here.</para>
    /// <para>The members: NUMVAL §15.67.4 r1 and NUMVAL-C §15.68.4 r1, both "The returned value is the numeric
    /// value represented by argument-1". <c>IntrinsicRenderer.RenderDec</c> holds their SDIDI arms (its <c>alwaysDec</c>
    /// list already named them), so this predicate only decides WHEN that lane is taken, never what it does.
    /// If it ever returned <c>null</c> here the switch's <c>default</c> stages LOUD — never a quiet fallback to
    /// a working-scale render.</para>
    /// </remarks>
    internal static bool ValueFixedByDefinition(string runtimeMethod) => runtimeMethod is "Numval" or "NumvalC";

    /// <summary>The call's RESOLVED type is Integer — the ONE reader (<see cref="IntrinsicResultType.Resolve"/>).</summary>
    private static bool IsInteger(BoundIntrinsicCall call) =>
        IntrinsicResultType.Resolve(call.Sig, call.Args) is IntrinsicType.Integer;

    /// <summary>The carrier a value is held on while the renderer evaluates an expression: the exact scaled
    /// <c>Int128</c>, the SDIDI, a binary64 that is a float ITEM's content, or a binary64 the engine produced as an
    /// approximation (<c>NumX.Approximate</c> — a function's returned value, a non-integer power).</summary>
    internal enum CarrierKind { Fixed, Dec, Real, ApproximateReal }

    /// <summary>A carrier, whether the value is known to have scale 0 (an integer — the question
    /// <c>NumericRenderer.Power</c> asks of its exponent), and — for an exact operand whose digit bound the renderer
    /// states (<c>NumX.Digits</c>) — that bound.</summary>
    private readonly record struct Carrier(CarrierKind Kind, bool Integral = false, AlignedOperand? Aligned = null)
    {
        public bool IsReal => Kind is CarrierKind.Real or CarrierKind.ApproximateReal;
        public static Carrier Exact(bool integral, AlignedOperand? aligned = null) => new(CarrierKind.Fixed, integral, aligned);
    }

    /// <summary>An exact scaled operand whose digit bound is known: its unscaled digits (<c>NumX.Digits</c>), its scale
    /// and whether it is a table(ALL) argument, which stands for an unknown number of elements.</summary>
    internal readonly record struct AlignedOperand(int Digits, int Scale, bool TableAll);

    /// <summary>Can aligning a cross-aligning call's arguments to their common scale — and accumulating the aligned values —
    /// leave the <see cref="Int128"/> carrier? True only when EVERY argument is an exact scaled operand with a KNOWN
    /// digit bound (<paramref name="operands"/>: <c>null</c> for an SDIDI or binary64 operand or one of unknown bound,
    /// for which nothing is provable and the escape-checked native arm (PB252) keeps its documented behaviour) and the
    /// sum of the magnitudes the function adds, each bounded by its aligned digits, exceeds
    /// <see cref="Int128.MaxValue"/>. THE ONE READER of the question: <c>IntrinsicRenderer.RenderNum</c> asks it of the
    /// RAW operands it is about to render, and <see cref="Of"/> asks it of the same facts read from the bound tree, so
    /// the carrier a nested SUM / RANGE / MEAN / MEDIAN / MIDRANGE answers on is predicted by the rule that picks it.</summary>
    /// <remarks>
    /// <para>Which magnitudes are added is a property of the FUNCTION: SUM and MEAN add every argument (a table(ALL)
    /// argument, whose run-time count is unknown, is charged 9 more digits); RANGE subtracts two; MEDIAN and
    /// MIDRANGE add two and halve through ×10; MOD and REM align a pair and return one of them.</para>
    /// <para>The aligned value's own digits are an UPPER bound by construction: an argument's unscaled digits plus
    /// the scale gap it is shifted by.</para></remarks>
    internal static bool AlignmentMayLeaveCarrier(string runtimeMethod, IReadOnlyList<AlignedOperand?> operands)
    {
        // ⛔ MOD / REM ARE NOT ROUTED. Their value is a DIFFERENCE of nearly equal magnitudes: MOD(9E30, 0.12345678) is
        // 0.1194939, which needs 39 digits of the dividend, and the SDIDI body keeps 34 — it would answer an
        // approximation SILENTLY where the native arm refuses LOUDLY (EC-SIZE-OVERFLOW at the carrier). The summing
        // family's value, in contrast, is as large as its arguments, so every digit a receiver can store (≤ 31) lies
        // inside the SDIDI's 34. (PB621 names SUM, RANGE, MEAN, MEDIAN and MIDRANGE only.)
        if (runtimeMethod is "ModScaled" or "RemScaled") return false;
        if (operands.Count == 0 || operands.Any(x => x is null)) return false;
        var xs = operands.Select(x => x!.Value).ToList();
        int s = xs.Max(x => x.Scale);
        // ⛔ THE BOUND IS ON THE VALUE, NOT ON THE WIDEST ARGUMENT PLUS A DIGIT COUNT. Argument i, aligned to the common
        // scale, is below 10^D_i (D_i its unscaled digits plus the scale gap — a table(ALL) argument stands for an
        // unknown number of elements, so it is charged nine more digits); the function's value is bounded by the sum of
        // the magnitudes it adds: every argument for SUM and MEAN, the two largest for RANGE, and those two times ten
        // for MEDIAN and MIDRANGE (added, then halved through ×10). `9(29)` beside `V9(9)` is 38 + 9 digits of two
        // values whose sum is 1.0000000010e38 — the Int128 carrier holds it, and the old "widest + digits of n"
        // allowance sent it to the SDIDI, which keeps 34 digits (kb/Work PB621's review follow-up).
        var terms = xs.Select(x => Pow10Bound(x.Digits + (s - x.Scale) + (x.TableAll ? 9 : 0)))
            .OrderByDescending(t => t).ToList();
        UInt128? bound = runtimeMethod switch
        {
            "SumScaled" or "MeanScaled" => SumOf(terms),
            "RangeScaled" => SumOf(terms.Take(2)),
            _ => SumOf(terms.Take(2)) is { } two && two <= UInt128.MaxValue / 10 ? two * 10 : null,   // MEDIAN / MIDRANGE
        };
        return bound is not { } b || b > (UInt128)Int128.MaxValue;

        // 10^digits − 1 — the largest magnitude of `digits` digits; null once it is past UInt128 (never fits).
        static UInt128? Pow10Bound(int digits)
        {
            if (digits is < 0 or > 38) return null;
            UInt128 p = 1;
            for (int i = 0; i < digits; i++) p *= 10;
            return p - 1;
        }
        static UInt128? SumOf(IEnumerable<UInt128?> terms)
        {
            UInt128 total = 0;
            foreach (var t in terms)
            {
                if (t is not { } v || total > UInt128.MaxValue - v) return null;
                total += v;
            }
            return total;
        }
    }

    /// <summary>The carrier classification of an argument — a second visitor over the operand and expression trees,
    /// beside <c>NumericRenderer</c>'s (the generated visitor interfaces are exhaustive, so a new bound node is a
    /// compile error in both).</summary>
    private sealed class ArgumentCarrier : IBoundExprVisitor<Carrier>, IBoundOperandVisitor<Carrier>
    {
        public static readonly ArgumentCarrier Instance = new();

        // ── Expressions ─────────────────────────────────────────────────────────────────────────────────────
        public Carrier Visit(BoundNumLiteral n) => Literal(n.Text);
        public Carrier Visit(BoundNumRef n) => OfPlace(n.Place);
        public Carrier Visit(BoundIndexRef n) => Carrier.Exact(true);
        public Carrier Visit(BoundLinageCounterRef n) => Carrier.Exact(true);
        public Carrier Visit(BoundReportCounterRef n) => Carrier.Exact(true);
        public Carrier Visit(BoundReportVaryingRef n) => Carrier.Exact(true);
        public Carrier Visit(BoundOdoExtent n) => Carrier.Exact(true);
        public Carrier Visit(BoundBinary n) => Combine(n.Left.Accept(this), n.Op, n.Right.Accept(this));
        public Carrier Visit(BoundNegate n) => n.Operand.Accept(this);
        public Carrier Visit(BoundPower n) => Power(n.Base.Accept(this), n.Exp.Accept(this));
        public Carrier Visit(BoundIntrinsicCall n) =>
            Of(n) switch
            {
                CarrierKind.Fixed => Carrier.Exact(IsInteger(n)),
                var kind => new Carrier(kind),
            };
        public Carrier Visit(BoundExprError n) => Carrier.Exact(true);
        public Carrier Visit(BoundUdfEvaluatedExpr n) => Carrier.Exact(true);

        // ── Operands ────────────────────────────────────────────────────────────────────────────────────────
        public Carrier Visit(BoundNumericLiteral n) => Literal(n.Text);
        public Carrier Visit(BoundFieldOperand n) => OfPlace(n.Place, tableAllOperand: n.Place is TableAllPlace);
        public Carrier Visit(BoundComputedOperand n) => n.Expr.Accept(this);
        public Carrier Visit(BoundFigurative n) => Carrier.Exact(true);
        public Carrier Visit(BoundStringLiteral n) => Carrier.Exact(true);   // an unsigned integer (Table 16)
        public Carrier Visit(BoundOperandError n) => Carrier.Exact(true);
        public Carrier Visit(BoundAddressOperand n) => Carrier.Exact(true);
        public Carrier Visit(BoundCurrentRecord n) => Carrier.Exact(true);
        public Carrier Visit(BoundPredefinedNull n) => Carrier.Exact(true);
        public Carrier Visit(BoundAllLiteral n) => Carrier.Exact(true);
        public Carrier Visit(BoundBoolOperand n) => Carrier.Exact(true);

        /// <summary><c>NumericRenderer.LiteralNum</c>: a floating-point literal is an SDIDI operand (owner decision D-B);
        /// any other numeric literal is an exact scaled one.</summary>
        private static Carrier Literal(string text) =>
            NumericLiteral.IsFloatingPointForm(text) && NumericLiteral.TryParseExact(text, out _, out _)
                ? new Carrier(CarrierKind.Dec)
                : Carrier.Exact(NumericLiteral.TryGetIntegerValue(text, out _), LiteralBound(text));

        /// <summary>The literal's own digit bound, from the ONE reader the renderer's operand uses
        /// (<c>EmitText.UnscaledLit</c> — leading zeros carry no magnitude).</summary>
        private static AlignedOperand? LiteralBound(string text) =>
            EmitText.UnscaledLit(text) is { Real: false, Digits: > 0 } lit ? new AlignedOperand(lit.Digits, lit.Scale, false) : null;

        /// <summary><c>NumericRenderer.FieldNum</c>: a floating-point ITEM is a binary64, a floating-point numeric-EDITED
        /// sender de-edits onto the SDIDI, every other numeric item is an exact scaled value (a reference-modified
        /// result and a group decode as an unsigned integer). <paramref name="tableAllOperand"/> is the
        /// <c>table(ALL)</c> argument of a cross-aligning call, whose element count is unknown.</summary>
        private static Carrier OfPlace(Place place, bool tableAllOperand = false)
        {
            if (place is TableAllPlace all) place = all.Element;
            if (place is RefModPlace || place.DenotedItem is null) return Carrier.Exact(true);
            var item = place.Item;
            if (item.Pic is not { } pic) return Carrier.Exact(true);
            if (pic.IsFloat) return new Carrier(CarrierKind.Real);
            if (!item.StoreAsImage && pic is { Category: PicCategory.NumericEdited, IsFloatEdited: true })
                return new Carrier(CarrierKind.Dec);
            // The renderer states the digit bound of an exact numeric item it reads as a value (a windowed or image-decoded
            // read states none): PicInfo.UnscaledDigitBound at the picture's scale.
            AlignedOperand? bound = !item.StoreAsImage && pic.Category is PicCategory.Numeric
                ? new AlignedOperand(pic.UnscaledDigitBound, pic.Scale, tableAllOperand)
                : null;
            return Carrier.Exact(pic.ReceiverScale() == 0, bound);
        }

        /// <summary><c>NumericRenderer.CombineCore</c> under NATIVE arithmetic: a binary64 that is the engine's own
        /// approximation is lifted to the SDIDI beside operands that are not float ITEMS; an SDIDI operand takes the
        /// operation onto the SDIDI unless a binary64 is also present (D16: the float lane keeps precedence).</summary>
        private static Carrier Combine(Carrier left, char op, Carrier right)
        {
            bool floatItem = left.Kind == CarrierKind.Real || right.Kind == CarrierKind.Real;
            if (!floatItem)
            {
                if (left.Kind == CarrierKind.ApproximateReal) left = new Carrier(CarrierKind.Dec);
                if (right.Kind == CarrierKind.ApproximateReal) right = new Carrier(CarrierKind.Dec);
            }
            if ((left.Kind == CarrierKind.Dec || right.Kind == CarrierKind.Dec) && !left.IsReal && !right.IsReal)
                return new Carrier(CarrierKind.Dec);
            if (left.IsReal || right.IsReal) return new Carrier(CarrierKind.Real);
            return Carrier.Exact(left.Integral && right.Integral && op != '/');
        }

        /// <summary><c>NumericRenderer.Power</c> under NATIVE arithmetic: an exponentiation by an INTEGER with no binary64
        /// operand is exact and lands on the SDIDI (or its wide form, which the renderer settles onto the SDIDI as an
        /// operand); every other exponentiation is an implementor-defined binary64 approximation, which is a float
        /// ITEM's content only when an operand is one.</summary>
        private static Carrier Power(Carrier b, Carrier e)
        {
            if (!b.IsReal && !e.IsReal && e.Kind != CarrierKind.Dec && e.Integral) return new Carrier(CarrierKind.Dec);
            bool itemContent = b.Kind == CarrierKind.Real || e.Kind == CarrierKind.Real;
            return new Carrier(itemContent ? CarrierKind.Real : CarrierKind.ApproximateReal);
        }
    }
}
