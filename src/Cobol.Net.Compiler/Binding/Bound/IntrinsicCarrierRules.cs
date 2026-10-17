// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;

namespace CobolNet.Binding.Bound;

/// <summary>
/// ⛔ <b>THE INTRINSIC CARRIER RULES TWO LAYERS ASK</b> (kb/Work PB3268, review R1-2): which carrier a numeric function's
/// value is computed on is decided by <c>IntrinsicRenderer.RenderNum</c> (compiler.codegen) and PREDICTED at bind time
/// by <c>FunctionValueLane</c> (compiler.binding), which describes the §14.9.25.4 GR1 intermediate result item for it.
/// The facts both read are pure functions of a runtime-method name and of operand digit bounds, so they live in the
/// layer both may reach (DESIGN-architecture-review.md §8.1: compiler.codegen reaches compiler.bound, compiler.binding
/// reaches compiler.bound, and neither reaches the other) — written once, never restated by either side.
/// </summary>
internal static class IntrinsicCarrierRules
{
    /// <summary>
    /// ⛔ THE FIRST HALF OF THE ALWAYS-SDIDI SET, AND THE ONLY HALF WITH A STRUCTURAL DEFINITION: the native-switch
    /// arms in <c>IntrinsicRenderer.RenderNum</c> that CROSS-ALIGN their arguments to one common scale before evaluating.
    /// </summary>
    /// <remarks>
    /// <para>Under a standard mode §15.4.1 r1 makes each of these equal its equivalent arithmetic expression
    /// (§15.60.4 MEAN, §15.61.4 MEDIAN, §15.62.4 MIDRANGE, §15.64.4 MOD, §15.76.4 RANGE, §15.77.4 REM,
    /// §15.88.4 SUM), and each argument converts to the SDIDI INDIVIDUALLY (§8.8.1.5.2 r1) — so no common scale is
    /// ever formed. The NATIVE arms instead align first, on the Int128 carrier, and alignment MULTIPLIES: a
    /// 31-digit integer beside a scale-18 item needs 49 digits, so <c>NumericRenderer.Align</c>'s per-argument
    /// escape raises EC-SIZE-OVERFLOW where the SDIDI holds the answer exactly. That makes "does this arm
    /// cross-align?" the exact test for "must this arm be on the Dec lane under a standard mode".</para>
    /// <para>⛔ It was built one arm at a time and each pass believed itself complete: PB62 added the summing
    /// family for MEAN's <c>MEAN(10³⁰, 2.0)</c>; PB252 found MOD and REM still missing — the SAME clause, the SAME
    /// mechanism, the arm nobody swept (feedback_two_arm_dispatch). It is a SET rather than an <c>is … or …</c>
    /// pattern so that <c>ExactCarrierBoundaryDriftTests</c> can hold it against the switch itself: the test reads
    /// <c>IntrinsicRenderer.cs</c>, collects the case labels of every arm whose body calls
    /// <c>IntrinsicRenderer.AlignedArgs</c> / <c>IntrinsicRenderer.AlignedArgsEx</c> or <c>NumericRenderer.Align(x, s)</c> against a
    /// COMMON scale, and fails if any of them is missing here. A new aligning arm is therefore routed
    /// automatically or the build goes red — it can no longer be forgotten.</para>
    /// <para>MAX / MIN / ORD-MAX / ORD-MIN are deliberately ABSENT: <c>IntrinsicRenderer.RawArgPairs</c> aligns each argument
    /// to its OWN scale (an identity rescale), because §15.59.4 / §15.63.4 / §15.71.4 / §15.72.4 return the
    /// CONTENT or the ORDINAL of an argument — pure selection, no cross-argument arithmetic, no common scale to
    /// escape (kb/Work PB65).</para>
    /// </remarks>
    internal static readonly FrozenSet<string> CrossAlignedNativeArms = new[]
    {
        "ModScaled", "RemScaled",                                   // §15.64.4 / §15.77.4 — Align(a, s), Align(b, s)
        "SumScaled", "RangeScaled",                                 // §15.88.4 / §15.76.4 — AlignedArgs
        "MedianScaled", "MidrangeScaled",                           // §15.61.4 / §15.62.4 — AlignedArgs
        "MeanScaled",                                               // §15.60.4 — AlignedArgsEx
    }.ToFrozenSet(StringComparer.Ordinal);

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

    /// <summary>An exact scaled operand whose digit bound is known: its unscaled digits (<c>NumX.Digits</c>), its scale
    /// and whether it is a table(ALL) argument, which stands for an unknown number of elements.</summary>
    internal readonly record struct AlignedOperand(int Digits, int Scale, bool TableAll);

    /// <summary>Can aligning a cross-aligning call's arguments to their common scale — and accumulating the aligned values —
    /// leave the <see cref="Int128"/> carrier? True only when EVERY argument is an exact scaled operand with a KNOWN
    /// digit bound (<paramref name="operands"/>: <c>null</c> for an SDIDI or binary64 operand or one of unknown bound,
    /// for which nothing is provable and the escape-checked native arm (PB252) keeps its documented behaviour) and the
    /// sum of the magnitudes the function adds, each bounded by its aligned digits, exceeds
    /// <see cref="Int128.MaxValue"/>. THE ONE READER of the question: <c>IntrinsicRenderer.RenderNum</c> asks it of the
    /// RAW operands it is about to render, and <c>FunctionValueLane</c>'s prediction asks it of the same facts read from the bound tree, so
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
}
