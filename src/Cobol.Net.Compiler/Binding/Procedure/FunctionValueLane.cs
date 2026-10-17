// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
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
/// cross-aligning call (SUM, RANGE, MEAN, MEDIAN, MIDRANGE) is SDIDI-carried when <see cref="IntrinsicCarrierRules.AlignmentMayLeaveCarrier"/>
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

    /// <summary>The native SDIDI bodies whose value <c>IntrinsicRenderer.RenderDec</c> returns as an EXACT scale-0 value
    /// rather than on the SDIDI: SIGN reads an SDIDI argument and returns +1, 0 or -1 (§15.81.1), and ORD-MAX / ORD-MIN
    /// compare on the SDIDI and return "the ordinal number that corresponds to the position of the argument-1 having the
    /// greatest [least] value" (§15.71.4 r1 / §15.72.4 r1). Every other member of <see cref="NativeSdidiBodies"/> returns
    /// the SDIDI. <c>FunctionValueLaneDriftTests</c> derives this set from the renderer's switch as well (the native arms
    /// not wrapped in its <c>Dec(...)</c>).</summary>
    internal static readonly FrozenSet<string> NativeSdidiBodiesWithExactResult = new[]
    {
        "SignOf", "OrdMax", "OrdMin",
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
    /// Always false under a decimal-engine mode. An INTEGER function (§15.2 item 5) is answered as any other is, because
    /// the renderer has no integer early-out (a binary64 argument takes INTEGER onto <c>FloorReal</c>); the intermediate
    /// of a TOP-LEVEL integer function asks it too, before its scale-0 item (<c>SendingValueTemp.IntermediatePic</c>,
    /// review R1-1).</summary>
    internal static bool ReturnsBinary64(BoundIntrinsicCall call, ArithmeticMode mode) =>
        !ArithmeticModes.IsDecimalEngine(mode) && Of(call) is CarrierKind.Real or CarrierKind.ApproximateReal;

    /// <summary>The carrier the native renderer returns a numeric function's value on, as far as the kind is concerned.</summary>
    private static CarrierKind Of(BoundIntrinsicCall call)
    {
        var sig = call.Sig;
        // NUMVAL / NUMVAL-C return the SDIDI in EVERY mode — RenderNum's first arm, ahead of the mode dispatch and of
        // whatever their arguments are (the value is fixed by the function's definition).
        if (IntrinsicCarrierRules.ValueFixedByDefinition(sig.RuntimeMethod)) return CarrierKind.Dec;
        // A function of a non-numeric result (reachable in a numeric context only under --permissive) is an exact value
        // (RenderNum's alphanumeric-decode arm). ⛔ AN INTEGER FUNCTION IS NOT EXEMPT (kb/Work PB3268, review R3-1):
        // RenderNum has no integer early-out, so INTEGER(1.5E0) computes FloorDec on the SDIDI, a SUM over wide integer
        // items takes the PB621 arm onto the SDIDI, and INTEGER of a COMP-2 item takes FloorReal. §15.2 item 5 fixes the
        // value's TYPE ("an integer function has an operational sign and no digits to the right of the decimal point"),
        // never its carrier. The exemption that stood here predicted an exact value for a nested call the renderer hands
        // its caller on the SDIDI, so ANNUITY(INTEGER(1.5E0) 3) got a COMP-2 intermediate that rounded AnnuityDec's value
        // in a multi-receiver MOVE (§15.4.1: "the returned value is the same for all instances of a given function").
        if (call.ResultCategory is not PicCategory.Numeric) return CarrierKind.Fixed;
        var args = call.Args.Select((a, i) => AtPosition(sig.Name, i, a.Accept(ArgumentCarrier.Instance))).ToList();
        bool anyDec = args.Any(a => a.Kind == CarrierKind.Dec);
        bool anyReal = args.Any(a => a.IsReal);
        bool allReal = args.Count > 0 && args.All(a => a.IsReal);
        bool sdidiBody = NativeSdidiBodies.Contains(sig.RuntimeMethod);
        // The SDIDI lane precedes the float lane: an SDIDI-carried argument (a floating-point literal, ...) is computed
        // on the SDIDI by a function that has a body for it (RenderNum's `AnyDecRaw(ic) && RenderDec(ic)` arm).
        if (anyDec && sdidiBody) return OnSdidiBody(sig.RuntimeMethod);
        // A cross-aligning arm (SUM / RANGE / MEAN / MEDIAN / MIDRANGE) whose aligned arguments may leave the Int128 carrier
        // takes the SDIDI (RenderNum's PB621 arm) — ahead of the float lane, whatever the arithmetic around it.
        if (IntrinsicCarrierRules.CrossAlignedNativeArms.Contains(sig.RuntimeMethod)
            && IntrinsicCarrierRules.AlignmentMayLeaveCarrier(sig.RuntimeMethod, args.Select(a => a.Aligned).ToList()))
            return OnSdidiBody(sig.RuntimeMethod);
        // A mixed selection list rides the SDIDI too (RenderNum's selection arm).
        if (Selections.Contains(sig.RuntimeMethod) && anyReal && !allReal && sdidiBody) return OnSdidiBody(sig.RuntimeMethod);
        if (sig.Float) return CarrierKind.ApproximateReal;
        // RenderNum's last dispatch: a binary64 argument at a numeric position moves the whole call onto the float lane.
        if (anyReal && IntrinsicArgumentRules.ArgumentRunIsAllNumeric(sig.Name, args.Count)) return CarrierKind.ApproximateReal;
        return CarrierKind.Fixed;
    }

    /// <summary>The carrier of the value an SDIDI body returns (<c>RenderDec</c>'s arm for the function): the SDIDI, or an
    /// exact scale-0 value for <see cref="NativeSdidiBodiesWithExactResult"/>.</summary>
    private static CarrierKind OnSdidiBody(string runtimeMethod) =>
        NativeSdidiBodiesWithExactResult.Contains(runtimeMethod) ? CarrierKind.Fixed : CarrierKind.Dec;

    /// <summary>The argument as the renderer's intake (<c>IntrinsicRenderer.RawArg</c>) hands it to the lane predicates: at a
    /// §15.3 type-6 (integer) position a binary64 or fractional fixed-point operand is already its INTEGER on the exact
    /// carrier, so it moves no call onto the float lane; an SDIDI operand stays on the SDIDI (as its integer).</summary>
    private static Carrier AtPosition(string functionName, int position, Carrier argument) =>
        argument.Kind != CarrierKind.Dec && !(argument.Kind == CarrierKind.Fixed && argument.Integral)
            && IntrinsicArgumentRules.IsIntegerPosition(functionName, position)
            ? Carrier.Exact(true)
            : argument;

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
    private readonly record struct Carrier(CarrierKind Kind, bool Integral = false, IntrinsicCarrierRules.AlignedOperand? Aligned = null)
    {
        public bool IsReal => Kind is CarrierKind.Real or CarrierKind.ApproximateReal;
        public static Carrier Exact(bool integral, IntrinsicCarrierRules.AlignedOperand? aligned = null) => new(CarrierKind.Fixed, integral, aligned);
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

        /// <summary>The literal's own digit bound, from the ONE decomposition and the ONE bound reader the renderer's
        /// operand (<c>EmitText.UnscaledLit</c>) uses: <see cref="NumericLiteral.TryUnscaledParts"/> and
        /// <see cref="NumericLiteral.UnscaledDigitBound"/> (leading zeros carry no magnitude).</summary>
        private static IntrinsicCarrierRules.AlignedOperand? LiteralBound(string text) =>
            NumericLiteral.TryUnscaledParts(text, out string digits, out int scale)
                ? new IntrinsicCarrierRules.AlignedOperand(NumericLiteral.UnscaledDigitBound(digits), scale, false)
                : null;

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
            IntrinsicCarrierRules.AlignedOperand? bound = !item.StoreAsImage && pic.Category is PicCategory.Numeric
                ? new IntrinsicCarrierRules.AlignedOperand(pic.UnscaledDigitBound, pic.Scale, tableAllOperand)
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
