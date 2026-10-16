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
/// What it cannot see is a run-time fact (a wide exact intermediate that overflows to the SDIDI, a data-dependent
/// width): those only move a value between the two NON-binary64 carriers, which this question does not distinguish.
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

    /// <summary>The pure selections (§15.59.4 / §15.63.4 / §15.71.4 / §15.72.4 / §15.61.4: "the content of" an argument),
    /// which a MIXED list takes onto the SDIDI under native arithmetic so the selected argument keeps its own carrier.</summary>
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
        // An INTEGER function (§15.2 item 5) and a function of a non-numeric result (reachable in a numeric context only
        // under --permissive) are exact values; the renderer never returns them on a binary64.
        if (call.ResultCategory is not PicCategory.Numeric || IsInteger(call)) return CarrierKind.Fixed;
        var sig = call.Sig;
        var args = call.Args.Select(a => a.Accept(ArgumentCarrier.Instance)).ToList();
        bool anyDec = args.Any(a => a.Kind == CarrierKind.Dec);
        bool anyReal = args.Any(a => a.IsReal);
        bool allReal = args.Count > 0 && args.All(a => a.IsReal);
        bool sdidiBody = NativeSdidiBodies.Contains(sig.RuntimeMethod);
        // The SDIDI lane precedes the float lane: an SDIDI-carried argument (a floating-point literal, ...) is computed
        // on the SDIDI by a function that has a body for it (RenderNum's `AnyDecRaw(ic) && RenderDec(ic)` arm).
        if (anyDec && sdidiBody) return CarrierKind.Dec;
        // A mixed selection list rides the SDIDI too (RenderNum's selection arm).
        if (Selections.Contains(sig.RuntimeMethod) && anyReal && !allReal && sdidiBody) return CarrierKind.Dec;
        if (sig.Float) return CarrierKind.ApproximateReal;
        // RenderNum's last dispatch: a binary64 argument at a numeric position moves the whole call onto the float lane.
        if (anyReal && IntrinsicArgumentRules.ArgumentRunIsAllNumeric(sig.Name, args.Count)) return CarrierKind.ApproximateReal;
        return CarrierKind.Fixed;
    }

    /// <summary>The call's RESOLVED type is Integer — the ONE reader (<see cref="IntrinsicResultType.Resolve"/>).</summary>
    private static bool IsInteger(BoundIntrinsicCall call) =>
        IntrinsicResultType.Resolve(call.Sig, call.Args) is IntrinsicType.Integer;

    /// <summary>The carrier a value is held on while the renderer evaluates an expression: the exact scaled
    /// <c>Int128</c>, the SDIDI, a binary64 that is a float ITEM's content, or a binary64 the engine produced as an
    /// approximation (<c>NumX.Approximate</c> — a function's returned value, a non-integer power).</summary>
    internal enum CarrierKind { Fixed, Dec, Real, ApproximateReal }

    /// <summary>A carrier and whether the value is known to have scale 0 (an integer — the question
    /// <c>NumericRenderer.Power</c> asks of its exponent).</summary>
    private readonly record struct Carrier(CarrierKind Kind, bool Integral = false)
    {
        public bool IsReal => Kind is CarrierKind.Real or CarrierKind.ApproximateReal;
        public static Carrier Exact(bool integral) => new(CarrierKind.Fixed, integral);
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
        public Carrier Visit(BoundFieldOperand n) => OfPlace(n.Place);
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
                : Carrier.Exact(NumericLiteral.TryGetIntegerValue(text, out _));

        /// <summary><c>NumericRenderer.FieldNum</c>: a floating-point ITEM is a binary64, a floating-point numeric-EDITED
        /// sender de-edits onto the SDIDI, every other numeric item is an exact scaled value (a reference-modified
        /// result and a group decode as an unsigned integer).</summary>
        private static Carrier OfPlace(Place place)
        {
            if (place is TableAllPlace all) place = all.Element;
            if (place is RefModPlace || place.DenotedItem is null) return Carrier.Exact(true);
            var item = place.Item;
            if (item.Pic is not { } pic) return Carrier.Exact(true);
            if (pic.IsFloat) return new Carrier(CarrierKind.Real);
            if (!item.StoreAsImage && pic is { Category: PicCategory.NumericEdited, IsFloatEdited: true })
                return new Carrier(CarrierKind.Dec);
            return Carrier.Exact(pic.ReceiverScale() == 0);
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
