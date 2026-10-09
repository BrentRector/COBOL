// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;

using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.Runtime;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a <see cref="BoundIntrinsicCall"/> (ISO §15; COBOLNET_INTRINSICS_DESIGN spine 1), dispatching on the
/// catalog row — the SINGLE render channel (P7 Step 12: the former parallel static evaluator is deleted;
/// DESIGN-codegen-backend §2.5). Three §15.2-type shapes (deep-dive D1):
/// <list type="bullet">
///   <item><b>floating-math</b> (Float rows, §15.4.1 native-arithmetic license) — compute in double; a
///         FIXED-POINT receiver then quantizes through the ONE <c>CobolIntrinsics.FromDouble</c> at the landing
///         <see cref="ReceiverContext.FloatLanding"/> chooses (the resultant identifier's scale + the statement's
///         ROUNDED mode when this value IS the transfer — so the arithmetic channel and the MOVE channel land one
///         returned value identically, §15.4.1 / kb/Work PB647; otherwise the ≥9 float floor CAPPED at the
///         receiver's Int128 headroom, with TRUNCATION, so a saturation can never store silently — PB13), while a
///         FLOAT receiver and a
///         RECEIVER-LESS context both keep the binary64 value: §15.4.1 leaves the returned value's
///         representation to the implementor, and with no receiver there is no scale to quantize TO
///         (P7.3 <c>ReceiverContext.None</c> — IF conditions / EVALUATE subjects / DISPLAY operands);</item>
///   <item><b>exact numeric / integer</b> — unscaled Int128 values at a known scale, aligned with the same
///         <see cref="NumericRenderer.Align"/> machinery the arithmetic verbs use (§8.8.1);</item>
///   <item><b>alphanumeric</b> — the INSTANCE string channel (<see cref="RenderString"/>, reached from
///         <see cref="OperandText.AsString"/> with the per-unit renderer): argument shapes render through the
///         ONE <see cref="NumericRenderer"/> under the default receiver (<see cref="ReceiverContext.None"/> —
///         a string-channel call has no numeric receiver), so compound arithmetic, division, float items, and
///         numeric-edited de-edits all render — the former static channel's H3 loud channels are gone.</item>
/// </list>
/// Every runtime-member fragment routes through <see cref="RuntimeApi"/> (the Step-4b façade; the ratchet's
/// IntrinsicRenderer whitelist entry is deleted). A <see cref="IntrinsicBind.Deferred"/> row (catalogued
/// later-edition function with no runtime yet) renders a loud not-implemented guard naming the function —
/// never a wrong value.
/// </summary>
internal sealed class IntrinsicRenderer(EmitContext ctx, NumericRenderer num)
{
    /// <summary>The per-unit expression renderer, surfaced for the nested argument visitor (a primary-ctor
    /// capture is not reachable through an instance reference).</summary>
    private NumericRenderer Num => num;

    // ── The numeric channel (COMPUTE / arithmetic / numeric comparisons / MOVE-to-numeric) ──────────────────

    /// <summary>Render a numeric-result intrinsic as a scaled value.</summary>
    public NumX RenderNum(BoundIntrinsicCall ic)
    {
        var sig = ic.Sig;
        if (sig.Bind == IntrinsicBind.Deferred || sig.RuntimeMethod.Length == 0)
            return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (catalogued, not yet implemented)"), 0);
        // A string-class function result in a NUMERIC context reaches here only under --permissive (the binder's
        // §8.8.1.1 screen rejects it under strict conformance — kb/Work PB68): the DA6 leniency, the same
        // digit-character decode an alphanumeric DATA ITEM gets there (CobolNum.FromAlphanumeric over the
        // function's string image, an unsigned integer). It used to be a loud RUNTIME value on legal-shaped
        // source, i.e. an unhandled exception at the wrong stage.
        if (ic.ResultCategory is PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean)
            // sending: false — the §8.8.1.1 screen already rejected this shape under STRICT conformance, so
            // this arm IS the DA6 permissive leniency and not a MOVE; §14.9.25.4 GR6 d) 1's condition has no
            // scope over an arithmetic operand (kb/Work PB844).
            return new NumX(RuntimeApi.NumFromAlphanumeric(RenderString(ic), sending: false), 0);

        // ⛔ THE §15.4.1 NATIVE LATITUDE IS WITHDRAWN WHERE THE FUNCTION DEFINITION FIXES THE VALUE — SO THIS ARM
        // RUNS BEFORE THE ARITHMETIC-MODE DISPATCH, NOT INSIDE ITS STANDARD BRANCH (kb/Work PB251, rows
        // RV-15.4.1-2 / RV-15.4.1-L2.2). §15.4.1's closing sentence is a CONDITIONAL — "When a numeric or integer
        // function does not have an equivalent arithmetic expression, its returned value is implementor-defined
        // unless otherwise specified in the function definition" — and §15.67.4 r1 / §15.68.4 r1 are exactly such
        // a specification, stated once for every arithmetic mode with no qualification: "The returned value is the
        // numeric value represented by argument-1". NUMVAL and NUMVAL-C have no equivalent arithmetic expression,
        // so the latitude the sentence grants is REMOVED for them and their value is the parsed value EXACTLY —
        // under NATIVE arithmetic too. Only the REPRESENTATION stays the implementor's ("When native arithmetic is
        // in effect, the characteristics and representation of the returned value are defined by the implementor"),
        // and the SDIDI is the ONE representation that holds every conforming argument: §15.67.3 r3 admits 31
        // total digits under native and r4 admits 34 under a standard mode, both of which the SDIDI's runtime
        // exponent carries without a working scale at all.
        // ⛔ NO COMPILE-TIME WORKING SCALE CAN DO THIS, WHICH IS WHY THE FIX IS THE CARRIER AND NOT A BIGGER FLOOR.
        // A scaled Int128 temporary holds i + ws digits for an argument with i integer digits, and §15.67.3 r3
        // permits i = 31, so the only ws safe for EVERY conforming argument is 7 — while `NUMVAL("0.123456789")`
        // needs 9. The former arms took ws = max(receiver scale, 6), which made a function's value depend on the
        // SHAPE of its receiver and truncated in every channel with no receiver scale to inherit: measured at HEAD
        // before this landed, `DISPLAY FUNCTION NUMVAL("0.1234567")` printed 0.123456, and — a real receiver being
        // no protection, because the receiver bounds the STATEMENT's result and not an OPERAND's precision —
        // `COMPUTE R = FUNCTION NUMVAL("0.1234567") * 10000000` stored 1234560 into PIC 9(9) where r1 owes
        // 1234567. NUMVAL-F is NOT here and must not be: §15.69.4 r2 grants native arithmetic "an approximation of
        // the numeric value represented by argument-1", so its native lane keeps the float family's determination
        // (CONFORMANCE.md item 92, PB60/RV-15.69.4-2) — the r2/r3 split is the standard drawing this exact line.
        if (ValueFixedByDefinition(sig.RuntimeMethod) && RenderDec(ic) is { } fixedByDefinition)
            return fixedByDefinition;

        // ⛔ ROUTE ON THE ARGUMENT'S TYPE, NOT ONLY THE FUNCTION'S FAMILY (fix-queue PB2). The exact family
        // computes over scale-aligned Int128, which is right (deep-dive D1) — but a FLOATING-POINT argument is
        // legal for all of it (§15.7.3 r1 and siblings require class NUMERIC, and a COMP-2 item is class
        // numeric), and dispatching on sig.Float alone handed a double expression to an Int128 parameter. The
        // user saw the generated C# fail: "CS1503: cannot convert from 'double' to 'System.Int128'" — an
        // internal failure escaping as a backend error on legal source. Ten of eleven functions probed did it.
        // Under native arithmetic §15.4.1 makes the returned value implementor-defined and each of these
        // functions is an equivalent arithmetic expression over its own operands, so once an argument arrives as
        // binary64 the EAE *is* a binary64 evaluation — nothing exact is surrendered, because there was nothing
        // exact left to keep. The real-argument bodies are CobolIntrinsics.RealArgs.cs, deliberately sharing
        // these method names so this one line is the whole dispatch.
        // ⛔ THE STANDARD-MODE ARM RUNS BEFORE THE FLOAT DISPATCH (kb/Work R18 — ledger F15/F19). Four float-family
        // rows have their standard-mode value FIXED by the spec, with no §15.4.1 r2 latitude: FUNCTION E and
        // FUNCTION PI are exact 34-digit constants (§15.27.3 r3 / §15.73.3 r3), and EXP / EXP10 carry equivalent
        // arithmetic expressions — `FUNCTION E ** argument-1` (§15.34.4 r1) and `10 ** argument-1` (§15.35.4 r1) —
        // that §15.4.1 r1 requires the returned value to EQUAL under a standard mode. Each renders through the
        // SAME CobolDec.Pow the hand-written `**` uses (§8.8.1.5.4), over the SAME exact constants, so the
        // function and its spelled-out EAE agree by construction — binary64 Math.Pow is not SDIDI arithmetic, and
        // routing these through RenderFloat gave receiver-DEPENDENT answers (§15.4.1's same-for-all-instances
        // rule) wrong from the third significant digit's ulp up. A float ARGUMENT lifts via DecOperand →
        // CobolDec.FromDouble (§8.8.1.5.1), exactly as the landed exact-family lane does.
        // (An if-chain, deliberately NOT a `switch (sig.RuntimeMethod)`: IntrinsicRealArgDriftTests anchors its
        // exact-arm scan on the FIRST such switch, and these arms are not exact-family arms — a Real argument
        // lifts through DecOperand, so no `…Real` body exists or is needed.)
        if (num.StandardDecimal)
        {
            // ⛔ THE EXPONENT ARGUMENT LIFTS FROM THE RAW OPERAND, NOT THE LANDED ONE (fix-queue PB56).
            // DecOperand(Arg(...)) round-tripped a Dec operand through the interim unscaled landing — truncated
            // at the working scale — and only then lifted it back, which is the very quantization this arm
            // exists to avoid (the RV-15.34.4-1 triage row measured it).
            if (sig.RuntimeMethod == "E") return new NumX(RuntimeApi.DecE, 0, Dec: true);
            if (sig.RuntimeMethod == "Pi") return new NumX(RuntimeApi.DecPi, 0, Dec: true);
            if (sig.RuntimeMethod == "Exp")
                return new NumX(RuntimeApi.DecPow(RuntimeApi.DecE, DecArg(ic, 0), num.IntermediateMode), 0, Dec: true);
            if (sig.RuntimeMethod == "Exp10")
                return new NumX(RuntimeApi.DecPow(RuntimeApi.DecFrom("10", "0"), DecArg(ic, 0), num.IntermediateMode), 0, Dec: true);
            if (RenderDec(ic) is { } dec)
                return dec;
        }
        // NATIVE arithmetic with an SDIDI-carried ARGUMENT (kb/Work PB69): the function computes on the SDIDI
        // body when it has one (MOD/REM keep exact integers exact — CobolIntrinsics.ModDec's integer fast path),
        // rather than landing the argument into Int128 at a working scale that a 33-digit power already overflows.
        // ⚠ The native Dec producers are THREE, not one — an integer power (past or within the Int128 window), a
        // floating-point numeric-EDITED sender, and a floating-point LITERAL operand, which owner decision D-B
        // made much the most common of them. This comment named only the first, and so did CombineCore's
        // (kb/Work PB273); the census is written out there.
        else if (AnyDecRaw(ic) && RenderDec(ic) is { } decNative)
            return decNative;
        // ⛔ A CROSS-ALIGNING ARM WHOSE ALIGNED ARGUMENTS PROVABLY CANNOT STAY IN THE Int128 CARRIER TAKES THE SDIDI
        // (kb/Work PB621; the Int128 half of the boundary PB252 made honest). SUM / RANGE / MEAN / MEDIAN / MIDRANGE
        // align every argument to the LIST's maximum scale, and alignment multiplies: ±1.7e30 beside a
        // PIC SV9(8) item needs 39 digits aligned, so the native arm answered EC-SIZE-OVERFLOW for RANGE = 3.4e30 —
        // a value its PIC S9(31) receiver holds. The value is representable; only the carrier's width was the
        // problem, and the SDIDI carries its exponent at run time (SumDec / RangeDec …: each argument converts
        // exactly, §8.8.1.5.2 r1, and the equivalent arithmetic expression evaluates on 34 digits — more than any
        // receiver can store, §13.18.40.3 SR14). ⚠ NOT "ALIGN TO THE RECEIVER'S SCALE", which the note proposed:
        // RANGE(0.6, −0.5) into a scale-0 receiver is 1.1 → 1, but each argument cut to scale 0 first is 0 − 0 = 0 —
        // no guard-digit count survives a carry (0.95 + 0.05), so the arguments must keep their own digits and the
        // wider carrier must be the one that changes. The question is asked of the arguments' digit BOUNDS
        // (NumX.Digits) so an ordinary list keeps its Int128 body unchanged and only a list that may leave the
        // carrier moves; an argument whose bound is unknown keeps the escape-checked native arm.
        if (CrossAlignedNativeArms.Contains(sig.RuntimeMethod) && AlignmentMayLeaveCarrier(ic)
            && RenderDec(ic) is { } wideAligned)
            return wideAligned;
        if (sig.Float) return RenderFloat(ic, sig.RuntimeMethod);
        // (The float lane's two preconditions are <see cref="FloatLaneExempt"/> — a numeric argument run whose
        // RESULT binary64 cannot carry — and IntrinsicArgumentRules.ArgumentRunIsAllNumeric — an argument run
        // binary64 cannot carry. Both ride the dispatch below; each states its own rule at its own declaration.)
        // MAX / MIN / ORD-MAX / ORD-MIN — and MEDIAN over an odd count — are pure SELECTION: §15.59.4 r1 /
        // §15.63.4 r1 "the returned value is the CONTENT of the argument-1 having the greatest [least] value",
        // §15.61.4 r1 "the content of the argument-1 that is the middle value", each compared "according to the
        // rules for simple conditions"; no equivalent arithmetic expression, so §15.4.1's native latitude over the
        // representation never reaches the VALUE. A MIXED list (a float beside a fixed-point item) must deliver
        // the selected argument from ITS OWN carrier, never re-rendered through binary64 (kb/Work PB65
        // RV-15.59.4-1 D2: MAX(F1 N1) with N1 = 999999999999999999 returned 13 — the 18-digit content went
        // double → FromDouble at scale 9 → modular store; MEDIAN(F1 N1 N2) returned 0). The SDIDI carrier holds
        // a 38-digit fixed exactly and the float through the §8.8.1.5.1 conversion, and its compare is exact — so
        // a mixed selection list evaluates on the SDIDI under NATIVE too, and lands once at the receiver. An
        // all-float list stays in the float lane below (its content IS the double), and the arithmetic
        // statistical family (SUM / MEAN / RANGE / MIDRANGE — equivalent arithmetic expressions) keeps the D16
        // native float lane a float operand selects.
        if (sig.RuntimeMethod is "MaxScaled" or "MinScaled" or "OrdMax" or "OrdMin" or "MedianScaled"
            && AnyRealArgument(ic) && !AllRealArguments(ic) && RenderDec(ic) is { } decSelection)
            return decSelection;
        // ⛔ AND THE LANE IS A FACT ABOUT THE ARGUMENT RUN, NOT ABOUT ONE ARGUMENT (kb/Work PB635).
        // `AnyRealArgument` answers "is any argument floating"; the question this dispatch has to answer is "does
        // the WHOLE argument run render in binary64", because FloatBody converts EVERY argument through Dbl. For
        // a function whose §15.3 argument types are all numeric (type 6 "an integer data item", type 10 "a
        // numeric data item") those are the same question. For one that MIXES them they are not, and the gap was
        // a crash on conforming-shaped source: FIND-STRING takes two string operands (§15.37.3 r1 — "class
        // alphabetic, alphanumeric, or national") and an integer argument-3 (r3), so
        // `FIND-STRING(HAY NDL START AFTER <COMP-2>)` moved the whole call to the float lane — the two strings
        // became `(double)CobolNum.FromAlphanumeric(…)`, LAST/ANYCASE were dropped, and the emitted call named
        // `FindStringReal`, which the runtime does not contain (Roslyn CS0117). It should not: §15.37.4's
        // returned value is a character POSITION with no equivalent arithmetic expression, so §15.4.1's
        // "implementor-defined approximation of the value of that expression" — the whole licence the binary64
        // lane rests on — is not granted for it at all, and there is no binary64 carrier anywhere in the call.
        // IntrinsicArgumentRules.ArgumentRunIsAllNumeric derives the precondition from the SPEC-VERIFIED
        // argument table and fails open, so it can only ever take a call OUT of the lane on a cited rule.
        if (AnyRealArgument(ic) && !FloatLaneExempt.Contains(sig.RuntimeMethod)
            && IntrinsicArgumentRules.ArgumentRunIsAllNumeric(sig.Name, ic.Args.Count))
            return RenderFloat(ic, RealMethod(sig.RuntimeMethod));

        switch (sig.RuntimeMethod)
        {
            // Integer functions — scale-0 results (§15.2 type 5).
            case "Factorial":                                                   // §15.36 (Int128; 34! overflows → EC default 0)
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, IntArg(ic, 0)), 0);
            case "SignOf":                                                      // §15.81 — scale-independent sign
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, Arg(ic, 0).Expr), 0);
            case "Floor":                                                       // §15.44 INTEGER — floor to scale 0
            {
                NumX a = Arg(ic, 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{a.Expr}, {a.Scale}"), 0);
            }
            case "Truncate":                                                    // §15.49 INTEGER-PART — truncate to scale 0
            {
                NumX a = Arg(ic, 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{a.Expr}, {a.Scale}"), 0);
            }
            case "AbsScaled":                                                   // §15.7 ABS — argument's own scale
            {
                NumX a = Arg(ic, 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, a.Expr), a.Scale);
            }
            case "FractionPart":                                                // §15.42 — argument's own scale
            {
                NumX a = Arg(ic, 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{a.Expr}, {a.Scale}"), a.Scale);
            }
            case "ModScaled":                                                   // §15.64 — floored modulus (sign table)
            case "RemScaled":                                                   // §15.77 — truncated remainder
            {
                NumX a = Arg(ic, 0), b = Arg(ic, 1);
                int s = Math.Max(a.Scale, b.Scale);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{NumericRenderer.Align(a, s)}, {NumericRenderer.Align(b, s)}"), s);
            }

            // MAX/MIN are pure SELECTION (§15.59.4/§15.63.4: the returned value is the CONTENT of an argument)
            // — no alignment at all (fix-queue PB65): the §8.8.4.2 comparison is CobolNum.Compare's exact
            // non-widening compare over each argument AT ITS OWN SCALE, and only the ONE selected value
            // rescales (escape-checked) to the common scale. The aligned forms wrapped silently at 39 aligned
            // digits — MIN of two positive arguments returned a NEGATIVE value, the content of NO argument.
            case "MaxScaled" or "MinScaled":
            {
                var (vals, scales, s) = RawArgPairs(ic);
                // The result scale is the RECEIVER's when one is known (a store then rescales by identity and
                // the receiver's own §14.9.25.4 GR6 truncation semantics apply, via the store-cap); a
                // receiverless/float context keeps the common scale and stays LOUD past the escape boundary —
                // a capped value inside a comparison would silently compare the wrong number (PB13's
                // receiver-blind-scale lesson, applied in both directions). A FLOATING-POINT edited receiver is
                // the float context too: its Scale is only the mask's hint (ReceiverContext.RoundsAtItsScale).
                bool store = num.Receiver.RoundsAtItsScale;
                int to = store ? num.Receiver.Scale : s;
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod is "MaxScaled" ? "MaxAt" : "MinAt",
                    $"{to}, {(store ? "true" : "false")}, {vals}, {scales}"), to);
            }
            // SUM/RANGE genuinely ADD, so they keep aligned arguments — escape-CHECKED both at the per-argument
            // alignment (Align emits RescaleEscape) AND across the accumulation itself (CobolIntrinsics.SizeEscape,
            // kb/Work PB252): every step past the Int128 intermediate is the size-error condition, never a wrap.
            // ⛔ Both of these reach here ONLY under NATIVE arithmetic — CrossAlignedNativeArms routes them to
            // SumDec/RangeDec under a standard mode, where §15.4.1 r1 admits no approximation at all.
            case "SumScaled":                                                   // §15.88 — Σ at the common scale
            {
                var (argList, s) = AlignedArgs(ic);
                // SumScaled is the ONE exact body serving two §15.4.1 EAEs — §15.88.4's Σ and §15.60.4's
                // numerator — so it is told which function it is evaluating, for the D1 size-error message. A
                // body serving exactly one function (RangeScaled, MedianScaled, …) names itself instead.
                return new NumX(RuntimeApi.Intrinsic("SumScaled", $"\"{sig.Name}\", {argList}"), s);
            }
            case "RangeScaled":                                                 // §15.76 — MAX − MIN at the common scale
            {
                var (argList, s) = AlignedArgs(ic);
                return new NumX(RuntimeApi.Intrinsic("RangeScaled", argList), s);
            }
            case "MedianScaled" or "MidrangeScaled":                            // §15.61/62 — the /2 is exact at scale s+1 (×10/2)
            {
                var (argList, s) = AlignedArgs(ic);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, argList), s + 1);
            }
            case "MeanScaled":                                                  // §15.60 — Σ/n with the ÷ discipline of §8.8.1
            {
                // (Under a STANDARD mode MEAN never reaches here — it is a CrossAlignedNativeArms member, so RenderDec evaluates the
                // §15.60.4 equivalent arithmetic expression on the SDIDI carrier, kb/Work PB62 / RV-15.60.4-1.)
                var (argList, s, _) = AlignedArgsEx(ic);
                // Quotient quantized at ws = max(Receiver.Scale, s+1, 6): the receiver's scale when known, never
                // below the sum's own resolution + 1, with a fraction floor for receiver-less (scale-0) contexts.
                int ws = Math.Max(Math.Max(num.Receiver.Scale, s + 1), 6);
                // Same mode rule as NumericRenderer.Divide: AT the receiver scale the one exact RoundDiv applies
                // the receiver's mode; above it, truncate and let the receiver store round once (§14.7.4).
                CobolRounding mode = ws == num.Receiver.Scale ? num.Receiver.Rounding : CobolRounding.Truncation;
                // The divisor is the number of ARGUMENTS — a compile-time count unless a table(ALL) argument
                // ranges over a runtime count (ISO §15.3; kb/Work PB62), in which case the enumerated list is
                // bound ONCE (CobolTable.With) and both the sum and the count read it.
                if (StaticArgCount(ic) is { } n)
                    return new NumX(RuntimeApi.NumDivide(false, RuntimeApi.Intrinsic("SumScaled", $"\"{sig.Name}\", {argList}"),
                        s.ToString(), n.ToString(), "0", ws.ToString(), mode), ws);
                string xs = NextWithVar();
                return new NumX(RuntimeApi.With(argList, xs,
                    RuntimeApi.NumDivide(false, RuntimeApi.Intrinsic("SumScaled", $"\"{sig.Name}\", {xs}"), s.ToString(), $"{xs}.Length", "0", ws.ToString(), mode)), ws);
            }
            case "OrdMax" or "OrdMin":                                          // §15.71/72 — 1-based ordinal, tie = first
            {
                // Pure selection with NO result rescale — every legal argument list has its defined ordinal
                // (fix-queue PB65; the aligned form wrapped at 39 aligned digits and picked the wrong argument).
                var (vals, scales, _) = RawArgPairs(ic);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod is "OrdMax" ? "OrdMaxAt" : "OrdMinAt",
                    $"{vals}, {scales}"), 0);
            }
            case "OrdMaxString" or "OrdMinString":                              // all-string form (PCS via CollatePrefix, CA23)
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, CollatePrefix(ic) + StrArgList(ic)), 0);

            // (NUMVAL / NUMVAL-C have NO arm here in ANY arithmetic mode — §15.67.4 r1 / §15.68.4 r1 fix their
            // value, so RenderNum's §15.4.1 arm routes both to the SDIDI projection before the mode dispatch
            // and no compile-time working scale exists for them to be wrong about; kb/Work PB251.)

            // The §15.93/§15.94 TEST validators — 0 / first-error position / LENGTH+1, scale 0. The digit-cap
            // sub-notes are ARITHMETIC-MODE dependent (§15.93.4 r1b notes 2/4): 31 native, 34 under the SDIDI
            // standard modes (standard-binary's 35 is in ArithmeticModes.NumvalDigitCap and unreachable — the
            // mode is declined at bind on §4.2.6 processor-dependence, kb/Work PB198).
            case "TestNumval":
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}"), 0);
            case "TestNumvalC":
                // The LOCALE arm mirrors NumvalC's (§15.94.3 r1 imports §15.68.3 whole; PB64 T6).
                if (ic.LocaleWritten)
                    return new NumX(RuntimeApi.Intrinsic("TestNumvalCLocale",
                        $"{Str(ic.Args[0])}, {LocaleTagArg(ic)}{AnycaseFlag(ic)}{DigitCapFlag}"), 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{CommaFlag}{AnycaseFlag(ic)}{DigitCapFlag}{AnycaseClassification(ic)}"), 0);

            // The §15.90/§15.91 date validators — integer verdict chains (year → month → day), scale 0.
            // ⛔ THE WIDE INTAKE, because the two functions are TOTAL (kb/Work PB254 — see IntArgWide):
            // §15.90.3 r1 / §15.91.3 r1 constrain nothing but integer-ness and r1a is a CATCH-ALL.
            case "TestDateYyyymmdd" or "TestDayYyyyddd":
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, IntArgWide(ic, 0)), 0);

            case "FindString":                                                  // §15.37 FIND-STRING (2023) — 1-based position of argument-2 in argument-1
                // Argument-3 also takes the WIDE intake: §15.37.3 r3 places no value constraint and
                // §15.37.4 r2/r3 answer for every integer — ignore that many matches, else 0 (PB254).
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {(ic.FindLast ? "true" : "false")}, "
                    + $"{(ic.Args.Count > 2 ? IntArgWide(ic, 2) : "0")}, {(ic.Anycase ? "true" : "false")}{AnycaseClassification(ic)}"), 0);
            case "Ord":                                                         // §15.70 — PCS-relative ordinal (H5: weights only when flagged)
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}{Collate(ic)}"), 0);
            case "Length" or "ByteLength" when ic.OverCurrentImage && ic.Args[0] is BoundFieldOperand { Place: { } vg }:
                // §15.50.4 rule 7 / §15.14.4 rule 6 over a variable-length group's CURRENT-EXTENT IMAGE (kb/Work PB244):
                // the same string DISPLAY shows (A.1 item 57), whose storage characters are bytes.
                return new NumX(RuntimeApi.Intrinsic("ByteLength",
                    PlaceRenderer.VarGroupCurrentImage(vg, $"FUNCTION {sig.Name} of")), 0);
            case "Length":                                                      // §15.50 runtime shapes (kb/Work PB61)
                // (A group with a runtime length — an OCCURS DEPENDING table, dynamic-length items or
                // dynamic-capacity tables beneath it — never arrives here: the binder's VariableLengthGroupSum
                // builds the §15.50.4 r4b/r7 expression, whose ODO part is a BoundOdoExtent.)
                // r6 — a national DYNAMIC LENGTH item's CURRENT length in bytes (the storage image); every other
                // runtime shape (a ref-mod view, an ANY LENGTH / dynamic-length alphanumeric item, a nested
                // string-function result) is the character-position count over the string image.
                if (ic.LengthInBytes && ic.Args[0] is BoundFieldOperand { Place: { } lp })
                    return new NumX(RuntimeApi.Intrinsic("ByteLength", OperandText.AsStorageImage(lp)), 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, Str(ic.Args[0])), 0);
            case "ByteLength":                                                  // §15.14 runtime shapes (kb/Work PB61)
                if (ic.Args[0] is BoundFieldOperand { Place: { } bp })
                    return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, OperandText.AsStorageImage(bp)), 0);
                // A nested function result is not storage the renderer can slice; its string image is the value's
                // characters, and the BYTES those occupy follow its §15.2 category (kb/Work PB1398): a national
                // result is 2 bytes per position (D-N1), a boolean result (implicit usage bit, §15.2 item 2) packs 8
                // positions per byte and rounds up (§15.14.4 r4), an alphanumeric one is 1 byte per position.
                return new NumX(RuntimeApi.Intrinsic(
                    IntrinsicResultType.OperandCategory(ic.Args[0]) switch
                    {
                        PicCategory.National => "ByteLengthNational",
                        PicCategory.Boolean => "ByteLengthBoolean",
                        _ => sig.RuntimeMethod,
                    }, Str(ic.Args[0])), 0);

            // Date/time conversions (§15.22/24/46/47; integer date form §15.5.2). These four keep the NARROW
            // §15.3 landing: §15.5.2 BOUNDS the integer date form (1601-01-01 onward), so an argument the body
            // cannot represent really is an incorrect value and EC-ARGUMENT-FUNCTION is the standard's answer —
            // the exact opposite of the TEST- validators above (kb/Work PB22 / PB254).
            case "DateOfInteger" or "DayOfInteger" or "IntegerOfDate" or "IntegerOfDay":
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, IntArg(ic, 0)), 0);

            // The Y2K windowing trio (§15.23/§15.25/§15.100) — integer results; the optional trailing
            // argument-2 (a SIGNED offset) / argument-3 ride the runtime's C#-optional-parameter defaults
            // (50 / the execution-time year via the argument-3 = 0 sentinel), so only the present arguments
            // render.
            case "DateToYyyymmdd" or "DayToYyyyddd" or "YearToYyyy":
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod,
                    string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => IntArg(ic, i)))), 0);

            // SECONDS-PAST-MIDNIGHT (§15.80) — type NUMERIC in standard numeric time form: the runtime
            // returns the local time-of-day TICK count = the unscaled value at SCALE 7 (the documented
            // 100 ns WiseOwl COBOL precision, §15.80.3 r3).
            case "SecondsPastMidnight":
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, ""), 7);

            // ── The COBOL-2014 date/time + number family (§15.17/48/79/92; §15.69/95) ───────────────────────────
            case "CombinedDatetime":                                            // §15.17 — a1 + a2/100000
            {
                // ⛔ ARGUMENT-2 IS TAKEN EXACTLY, NEVER LANDED (kb/Work PB620). §15.17.3 r2 requires only
                // "standard numeric time form", which §15.5.5 defines by MAGNITUDE — no fraction-digit limit —
                // so a receiver-derived working scale has nothing to round to and §15.4.1 r1 makes the returned
                // value EQUAL the §15.17.4 r1 expression. A Dec/float argument-2 never reaches here at all: it
                // is claimed by RenderDec's own arm under either arithmetic mode, which is what lets this arm
                // consume the operand at its OWN scale.
                NumX t = RawArg(ic, 1);
                // A trailing-P argument (negative scale) is whole seconds first (the runtime's WholeSecondsAtScale), so
                // the exact result is read at max(scale, 0) + 5 (kb/Work PB2633).
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{IntArg(ic, 0)}, {t.Expr}, {t.Scale}{LeapSecondFlag}"), Math.Max(0, t.Scale) + 5);
            }
            case "IntegerOfFormattedDate":                                       // §15.48 — analyze a2 per format a1 → integer date
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{LeapSecondFlag}"), 0);
            case "SecondsFromFormattedTime":                                    // §15.79 — result scale = the format's fractional-second count
            {
                if (ic.Args[0] is not BoundStringLiteral fmt)
                    return new NumX(EmitText.LoudValue("long",
                        "FUNCTION SECONDS-FROM-FORMATTED-TIME requires a literal time format (§15.79.3 r1)"), 0);
                int fsc = RuntimeApi.DateFormatFractionDigits(fmt.Value);       // compile-time — the ONE format analyzer
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {fsc}{LeapSecondFlag}"), fsc);
            }
            case "TestFormattedDatetime":                                       // §15.92 — 0 (valid) or the 1-based error position
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{LeapSecondFlag}"), 0);
            // §15.69 NUMVAL-F — the ws floor follows the float-family precedent, and so does its CAP: this is a
            // THIRD instance of the PB13 quantizer defect, and the one PB5 itself missed. The runtime still
            // clamped at long.MaxValue (PB5's original 9.2×10¹⁸ clamp, never swept to this sibling), so
            // FUNCTION NUMVAL-F("1E+20") into a PIC 9(31) returned 9223372036 — ten orders of magnitude out,
            // with NO SIZE ERROR, where §15.69.4 r2 requires "an approximation of the numeric value represented
            // by argument-1". Found by FloatQuantizeHeadroomDriftTests, not by eye.
            case "NumvalF":
            {
                // ⛔ THE FLOAT FAMILY'S RECEIVERLESS/REAL ARM, SWEPT HERE AT LAST (fix-queue PB60 / RV-15.69.4-2 —
                // the sweep RenderFloat's own comment asked for). §15.69.4 r2 makes NUMVAL-F's NATIVE value an
                // APPROXIMATION, and this compiler's documented determination for an approximated returned value
                // (CONFORMANCE.md item 92) is the float family's: binary64 unless a FIXED-POINT arithmetic receiver
                // quantizes it. With no receiver there is no scale to quantize TO, and the ws-9 stand-in was not a
                // rendering choice but a wrong answer: `IF FUNCTION NUMVAL-F("5E+30") = FUNCTION NUMVAL-F("9E+30")`
                // was TRUE (both saturating to the Int128 sentinel), `DISPLAY` printed that sentinel, a COMP-2
                // receiver got 1.7E+29, and NUMVAL-F("1.5E-12") was 0 in every receiver-less channel. Every consumer
                // of a receiver-less numeric already has its Real arm (relation, text, subscript, argument).
                // A fixed arithmetic receiver keeps the EXACT Int128 parse below — the receiver's scale is known and
                // the PB13 cap makes any saturation visible to its capacity check — and so does a MOVE SENDER
                // (ReceiverContext.MoveSender: the receiver's scale is known and MOVE lands the parsed decimal
                // digit-for-digit; the binary64 route would land NUMVAL-F("1.5E-8") into V9(9) as 14 through
                // ToScaled's multiply-then-truncate, and lose every digit past the 17th of a 20-digit argument).
                if (num.Receiver.Real || (num.Receiver.Receiverless && !num.Receiver.MoveSender))
                    return new NumX(RuntimeApi.Intrinsic("NumvalFDouble", $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}"), 0, Real: true, Approximate: true);
                int ws = num.Receiver.FloatWorkingScale;
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ws}{CommaFlag}{DigitCapFlag}{CheckedFlag}"), ws);
            }
            case "TestNumvalF":                                                 // §15.95 — 0 / first-error position / LENGTH+1;
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,         //   the r1b digit-cap sub-note is MODE-dependent,
                    $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}"), 0);         //   exactly as its two TEST- siblings (PB60)

            case "IntegerOfBoolean":                                            // §15.45.4 r1 — the unsigned MSB-first value of the bit configuration
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, Str(ic.Args[0])), 0);

            default:
                return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (no numeric render recipe)"), 0);
        }
    }

    /// <summary>The §15.4.1 floating-math family: arguments as doubles (through the one scaled→double
    /// conversion), result quantized by the ONE FromDouble at <c>ws = max(Receiver.Scale, 9)</c> (the ≥9 float floor).</summary>
    /// <summary>The FLOATING-POINT body's name for an exact-family runtime method (PB2).</summary>
    /// <remarks>
    /// A CONVENTION, not a table: <c>XxxScaled</c> → <c>XxxReal</c>, anything else gains a <c>Real</c> suffix.
    /// The obvious alternative — a same-named <c>double</c> overload — does not compile: an integer literal
    /// converts implicitly to both <c>Int128</c> and <c>double</c>, so <c>FUNCTION MAX(5 7)</c> became a CS0121
    /// ambiguity and broke six corpus programs that never touched a float. <c>IntrinsicRealArgDriftTests</c>
    /// asserts the counterpart exists for every exact method reachable with a real argument.
    /// </remarks>
    /// <summary>
    /// The exact-family runtime methods that stay on their <b>exact</b> arm even with a floating-point argument,
    /// because <see cref="RenderFloat"/>'s tail wraps every result in <c>FromDouble(double, ws)</c> and their
    /// RESULT is not a value binary64 can carry. The argument run is numeric — so
    /// <see cref="IntrinsicArgumentRules.ArgumentRunIsAllNumeric"/> admits it — and the exclusion is about the
    /// CODOMAIN instead.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><c>Factorial</c> (fix-queue PB21) — §15.36's result cannot ride a double: 33! is ~8.7e36, which is
    ///         why the exact body returns <c>Int128</c>. Its arm consumes the argument through
    ///         <see cref="IntArg"/>, whose <c>(long)(double)</c> conversion handles a float operand correctly, so
    ///         the float case needs no separate body at all. Writing <c>FactorialReal</c> to satisfy the naming
    ///         convention would have meant returning a double and silently losing exactness past 2⁵³ — a function
    ///         answering differently because of how its ARGUMENT was stored, the shape-dependence defect PB13
    ///         closed.</item>
    /// </list>
    /// <para>⛔ IT IS A DECLARED SET AND NOT AN INLINE <c>!= "Factorial"</c> (kb/Work PB635). The literal sat
    /// inside the dispatch's boolean expression with its reason eight lines away and a drift test matching on
    /// that exact spelling, so a second member could only be added by copying the shape. Here a second member is
    /// one line with its own citation, and <c>IntrinsicRealArgDriftTests</c> reads the set rather than a
    /// sentence.</para>
    /// </remarks>
    internal static readonly FrozenSet<string> FloatLaneExempt = new[]
    {
        "Factorial",
    }.ToFrozenSet(StringComparer.Ordinal);

    internal static string RealMethod(string exact) =>
        exact.EndsWith("Scaled", StringComparison.Ordinal)
            ? string.Concat(exact.AsSpan(0, exact.Length - "Scaled".Length), "Real")
            : exact + "Real";

    /// <summary>The binary64 CALL this family computes in — the argument run only, with no container or receiver
    /// decision in it (kb/Work PB253: those belong to <see cref="RenderFloat"/> and
    /// <see cref="RenderFloatNative"/> respectively, and mixing them is what let an arm order go wrong).</summary>
    private string FloatBody(BoundIntrinsicCall ic, string method) => method switch
    {
        // RANDOM (§15.75.3): the no-argument form continues the current sequence; the seeded form restarts it.
        // ⛔ THE SEED TAKES THE WIDE INTAKE, because the argument is TOTAL (kb/Work PB636 — see IntArgWide):
        // §15.75.3 r2 constrains the SIGN alone and §15.75.4 r3 makes the distinct-sequence subset a FLOOR
        // ("from 0 through at least 32767"), not a domain, so §15.3 has no incorrect value to raise on.
        "Random" when ic.Args.Count == 0 => RuntimeApi.Intrinsic(method, ""),
        "Random" => RuntimeApi.Intrinsic(method, IntArgWide(ic, 0)),
        // COMBINED-DATETIME's binary64 carrier takes the compilation group's LEAP-SECOND state, exactly as its
        // two exact-carrier twins do (kb/Work PB620): §15.17.3 r2's bound is §7.3.17.4 GR4/GR5's, so a screen
        // that could not see the directive would admit 86 400.5 on one carrier and refuse it on another.
        "CombinedDatetimeReal" => RuntimeApi.Intrinsic(method, $"{Dbl(ic, 0)}, {Dbl(ic, 1)}{LeapSecondFlag}"),
        // A variadic body whose EAE CANCELS across its list takes an all-exact list UNNARROWED (kb/Work PB1565 —
        // see ExactListBodies): the SDIDI evaluation in the native intermediate rounding mode, narrowed once.
        // PRESENT-VALUE's rate leads unscreened, exactly as on RenderDec's arm: the SDIDI body decides §15.74.3 r2
        // on the exact rate itself (PresentValueDec), so a leading table(ALL) stays one CobolDec[] enumeration.
        "PresentValue" when !AnyRealArgument(ic) =>
            LeadThenTail(ic, method, $"{num.IntermediateMode}, ", "CobolDec", DecOf),
        _ when ExactListBodies.Contains(method) && !AnyRealArgument(ic) =>
            RuntimeApi.Intrinsic(method, $"{num.IntermediateMode}, {DecArgList(ic)}"),
        // PRESENT-VALUE (§15.74.2 `argument-1 { argument-2 } …`): the rate leads, the amounts are the params tail.
        // The lead is the DISCOUNT BASE 1 + rate, formed on the rate's exact carrier after the ONE screen
        // (kb/Work PB1000 — see PresentValueBase).
        "PresentValue" => LeadThenTail(ic, method, "", "double", DblOf, lead: a => PresentValueBase(ic, a)),
        // A table(ALL) argument enumerates at run time (ISO §15.3; kb/Work PB62) — the list becomes ONE array.
        _ => RuntimeApi.Intrinsic(method, ArgArray(ic, 0, "double", DblOf)
            ?? string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => i == 0 ? LeadArg(ic, method) : Dbl(ic, i)))),
    };

    /// <summary>
    /// The binary64-family bodies that take argument-1 on its EXACT carrier instead of its binary64 — a
    /// <c>CobolDec</c> overload in <c>CobolIntrinsics.Float.cs</c> — and which carriers each takes that way
    /// (kb/Work PB999).
    /// </summary>
    /// <remarks>
    /// <para>The §15.4.1 licence makes the RETURNED value an implementor-defined approximation; it does not make
    /// the ARGUMENT one. Each member is a body whose result for the narrowed substitute is not an approximation of
    /// its result for the real argument:</para>
    /// <list type="bullet">
    ///   <item><see cref="ExactIntake.Sdidi"/> — the substitute is wrong only past binary64's RANGE, which only an
    ///         SDIDI reaches (10^±6144, §8.8.1.5.2: a floating-point literal, a product past the Int128 window);
    ///         a scaled operand is inside it, where the body's bounded condition number keeps binary64's RELATIVE
    ///         error the ordinary approximation. <c>Sqrt</c> (the native arm; the standard modes are
    ///         <c>SqrtDec</c>'s) — §15.84.4 r4: the root of 10^−400 is the representable 10^−200.</item>
    ///   <item><see cref="ExactIntake.EveryExact"/> — the body is ILL-CONDITIONED somewhere in its domain (its
    ///         condition number |x·f′(x)/f(x)| is unbounded, or reaches hundreds), so even the RELATIVE error of a
    ///         scaled operand's binary64 is amplified into the result, and every exact operand, scaled or SDIDI, is
    ///         taken exactly (kb/Work PB1041). <c>Sin</c> / <c>Cos</c> / <c>Tan</c> (§15.82.4 r1 / §15.20.4 r1 /
    ///         §15.89.4 r1) at every multiple of π/2 and at magnitude — reduced to a quadrant and residue on the
    ///         exact carrier: SIN(3.14159265358979323) answered 1.22·10^−16 where the value is 8.46·10^−18,
    ///         sin(1.0E40) answered +0.6468 where sin(10^40) is −0.5696, and sin(10^400) computed sin(+∞) = NaN;
    ///         <c>Log</c> / <c>Log10</c> (§15.55.4 r1 / §15.56.4 r1) at 1 (LOG(1 + 10^−26) answered 0) and past the
    ///         range (log10(10^−400) is −400, not log10(+0.0) = −∞); <c>Acos</c> / <c>Asin</c> (§15.8.4 r1 /
    ///         §15.10.4 r1) at ±1 (ACOS(1 − 10^−26) answered 0, not 1.414·10^−13); <c>Exp</c> / <c>Exp10</c>
    ///         (§15.34.1 / §15.35.1 — condition number |x|: 150 ulps at EXP(700.12…)).</item>
    /// </list>
    /// <para>Not members, each because its condition number is bounded by a constant on the whole domain, so the
    /// substitute's result IS the approximation: ATAN (≤ 1), and SQRT for a scaled operand (½). A float operand IS
    /// its binary64 and keeps the double body. <c>WholeRangeBodiesDriftTests</c> pins the runtime overload each
    /// member needs and the values each arm owes.</para>
    /// <para>⛔ A domain member still passes the ONE argument-domain screen (§15.3 rule 14; kb/Work PB952): its
    /// operand is <c>CobolIntrinsics.DomainDecAdmitted</c>, <c>DomainDec</c>'s predicate returning the admitted
    /// carrier instead of its binary64, so the rule is decided in one place on either lane.</para>
    /// </remarks>
    internal static readonly FrozenDictionary<string, ExactIntake> WholeRangeBodies = new Dictionary<string, ExactIntake>
    {
        ["Sqrt"] = ExactIntake.Sdidi,
        ["Sin"] = ExactIntake.EveryExact, ["Cos"] = ExactIntake.EveryExact, ["Tan"] = ExactIntake.EveryExact,
        ["Log"] = ExactIntake.EveryExact, ["Log10"] = ExactIntake.EveryExact,
        ["Acos"] = ExactIntake.EveryExact, ["Asin"] = ExactIntake.EveryExact,
        ["Exp"] = ExactIntake.EveryExact, ["Exp10"] = ExactIntake.EveryExact,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// The VARIADIC binary64-family bodies that take an all-exact argument LIST unnarrowed — a runtime overload
    /// <c>(CobolRounding mode, …CobolDec…)</c> in <c>CobolIntrinsics.Float.cs</c> that evaluates the function's
    /// equivalent arithmetic expression on the SDIDI and narrows only the result (kb/Work PB1565): the list arm of
    /// <see cref="WholeRangeBodies"/>.
    /// </summary>
    /// <remarks>
    /// <para>Each member's EAE SUBTRACTS its arguments from one another, so narrowing each exact argument first is
    /// not an approximation of the returned value — its low digits cancel away: <c>Variance</c> (§15.98.4 r1, the
    /// deviations from FUNCTION MEAN: VARIANCE(100000000000000001 100000000000000002) answered 0, not 0.25),
    /// <c>StandardDeviation</c> (§15.86.4 r1, SQRT of that: 0, not 0.5), <c>PresentValue</c> (§15.74.4 r1, a Σ of
    /// signed amounts: PRESENT-VALUE(0 100000000000000001 −100000000000000000) answered 0, not 1).</para>
    /// <para>A list with a FLOATING argument keeps the binary64 body — its EAE is evaluated in binary64, the
    /// determination CONFORMANCE.md DOC-A.1-92 states for the exact family's floating arm. An SDIDI argument is
    /// already claimed by <c>RenderNum</c>'s native Dec arm before the float family is reached.
    /// <c>WholeRangeBodiesDriftTests</c> pins the overload each member needs and that EVERY variadic float-family
    /// catalog row is a member, so a new variadic float function cannot silently take the narrowing arm.</para>
    /// </remarks>
    internal static readonly FrozenSet<string> ExactListBodies = new[]
    {
        "Variance", "StandardDeviation", "PresentValue",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Which exact carriers a <see cref="WholeRangeBodies"/> member takes unnarrowed.</summary>
    internal enum ExactIntake
    {
        /// <summary>An SDIDI operand only — the narrowing is wrong only past binary64's range.</summary>
        Sdidi,
        /// <summary>A scaled operand too (lifted exactly by <c>CobolDec.From</c>) — the body is ill-conditioned
        /// somewhere, so the narrowing's relative error is amplified into the result.</summary>
        EveryExact,
    }

    /// <summary>Argument-1 of the default binary64-family arm: an operand a <see cref="WholeRangeBodies"/> member
    /// takes exactly stays (or is lifted exactly into) a <c>CobolDec</c>, screened by
    /// <see cref="RuntimeApi.DomainArgAdmitted"/> when the row has a domain; every other operand is
    /// <see cref="DomainArg"/>'s screened binary64.</summary>
    /// <remarks>INTAKE(EXACT) — a whole-range body's operand on an exact carrier, unnarrowed (kb/Work PB999); any
    /// other operand falls through to <see cref="DomainArg"/>, whose contract is APPROXIMATED.</remarks>
    private string LeadArg(BoundIntrinsicCall ic, string method)
    {
        NumX x = num.AsNum(ic.Args[0], num.Receiver);
        bool exact = WholeRangeBodies.TryGetValue(method, out var intake)
            && (x.Dec || intake is ExactIntake.EveryExact && !x.Real);
        return exact ? ExactArg(ic, x) : DomainArg(ic, x);
    }

    /// <summary>Argument-1 on an EXACT carrier as a <c>CobolDec</c> expression — an SDIDI as it is, a scaled or
    /// unsigned-wide operand lifted exactly (<c>CobolDec.From</c> holds any Int128 at any scale) — and, when the
    /// row has a §15.x.3 value domain, through the ONE screen's admitted form
    /// (<see cref="RuntimeApi.DomainArgAdmitted"/>: a <c>CobolDec?</c>, null = rejected). A float operand has no
    /// exact carrier beyond its binary64 and never reaches here.</summary>
    /// <remarks>INTAKE(EXACT) — the carrier unnarrowed; the body narrows only what it computes from it.</remarks>
    private string ExactArg(BoundIntrinsicCall ic, NumX x)
    {
        NumX dec = x.Dec ? x : new NumX(num.DecOperand(x), 0, Dec: true);
        return ic.Sig.Domain is IntrinsicDomain.None
            ? dec.Expr
            : RuntimeApi.DomainArgAdmitted(dec, ic.Sig.Domain, ic.Sig.Name, ic.Sig.DomainRule!);
    }

    /// <summary>PRESENT-VALUE's leading value: the discount base <c>1 + argument-1</c>
    /// (<c>CobolIntrinsics.PresentValueBase</c>), from the rate's exact carrier after the ONE §15.74.3 r2 screen
    /// (<see cref="ExactArg"/>), or from a float rate's own binary64 after <see cref="DomainArg"/>.</summary>
    /// <remarks>⛔ The base, not the rate (kb/Work PB1000): <c>1 + rate</c> cancels at the domain's bound, so a body
    /// forming it from the NARROWED rate turned the legal −0.99999999999999999999 into a base of 0 and answered ∞.
    /// INTAKE(EXACT) for an exact carrier; INTAKE(APPROXIMATED) for a float one, whose binary64 IS the argument.</remarks>
    private string PresentValueBase(BoundIntrinsicCall ic, BoundOperand a)
    {
        NumX x = num.AsNum(a, num.Receiver);
        return RuntimeApi.Intrinsic("PresentValueBase", x.Real ? DomainArg(ic, x) : ExactArg(ic, x));
    }

    /// <summary>Argument-1 of a binary64-family call, SCREENED against the row's §15.x.3 value domain on its exact
    /// carrier before it becomes the body's double (kb/Work PB952 — <see cref="RuntimeApi.DomainArg"/>); a row with
    /// no domain takes the plain <see cref="DblOf"/>. ⛔ The screen is the ONLY place such a rule is decided: the
    /// runtime bodies carry no second test on the double (a legal argument rounded onto a bound would trip it).</summary>
    /// <remarks>INTAKE(APPROXIMATED) — binary64 from the RAW carrier, after an EXACT domain test on that carrier.</remarks>
    private string DomainArg(BoundIntrinsicCall ic, BoundOperand a) => DomainArg(ic, num.AsNum(a, num.Receiver));

    private static string DomainArg(BoundIntrinsicCall ic, NumX x) =>
        ic.Sig.Domain is IntrinsicDomain.None
            ? NumericRenderer.Real(x)
            : RuntimeApi.DomainArg(x, ic.Sig.Domain, ic.Sig.Name, ic.Sig.DomainRule!);

    private NumX RenderFloat(BoundIntrinsicCall ic, string method)
    {
        string call = FloatBody(ic, method);
        // ⛔ THE ARITHMETIC MODE IS TESTED BEFORE THE RECEIVER SHAPE, AND THE COMMENT BELOW SAYS WHY EACH ARM IS
        // WHERE IT IS (kb/Work PB253). §15.4.1: "When standard-decimal arithmetic or standard-binary arithmetic is
        // in effect, the returned value for numeric and integer functions is contained in a temporary standard data
        // item in the intermediate form defined for the arithmetic mode in effect" — UNCONDITIONAL on whether the
        // function has an equivalent arithmetic expression AND on the shape of whatever consumes the value, and
        // §8.8.1.5.1 makes the mode "a method of evaluating an arithmetic expression, an arithmetic statement, the
        // SUM clause, AND CERTAIN INTEGER AND NUMERIC FUNCTIONS as specified in 15.4.1" — the function itself, not
        // only its use as an operand. The last-¶ exemption ("When a numeric or integer function does not have an
        // equivalent arithmetic expression, its returned value is implementor-defined unless otherwise specified in
        // the function definition") exempts the VALUE — the binary64 approximation this family computes — never the
        // CONTAINER. This is the ordering COBOLNET_NUMERIC_DESIGN.md D3 states for the whole engine ("the mode
        // branch runs BEFORE the D16 float branch") and that NumericRenderer.CombineCore, NumericRenderer.Power and
        // NumericRenderer.Landed already keep; this renderer was the one place that read the receiver first, so
        // under ARITHMETIC IS STANDARD-DECIMAL the SDIDI arm was UNREACHABLE for every receiver-less or
        // float-receiver reference and the raw binary64 escaped. Measured before the fix, with
        // A = 1.570796326794896619231321691639: `MOVE FUNCTION TAN(A)` landed 16331239353195368.96 (the binary64
        // ×10^scale artifact of CobolFloat.ToScaledUnchecked) while `COMPUTE R = FUNCTION TAN(A)` landed
        // 16331239353195370.00 — the SAME function, the SAME argument, TWO returned values in one run, which
        // §15.4.1 forbids outright ("the returned value is the same for all instances of a given function within a
        // single execution of the runtime element so long as the value and order of the arguments, the collating
        // sequence, and the locale are unchanged"). The text channel diverged the same way: `DISPLAY FUNCTION
        // SIN(0.00000000000000000001)` printed `1E-20`, CobolFloat.Display's binary64 E-notation, where the
        // SDIDI's own item-92 text (CobolDec.ToFunctionText) is `0.00000000000000000001`.
        // The residue this reaches is exactly the prose family with no §15.4.1 r1 equivalent arithmetic expression
        // and no RenderDec body — ACOS, ASIN, ATAN, COS, SIN, TAN, LOG, LOG10, RANDOM: everything else with
        // Float: true is already claimed above by RenderNum's standard-mode arm (E / PI / EXP / EXP10 by name,
        // SQRT / FACTORIAL / ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION by RenderDec's alwaysDec).
        //
        // ⛔ UNDER A STANDARD MODE THE RESULT CONVERTS IN, IT DOES NOT QUANTIZE (fix-queue PB56). The prose
        // family's returned value is an implementor-defined binary64 approximation in every mode (§15.4.1 last ¶),
        // but under STANDARD-DECIMAL that approximation is CONTAINED IN an SDIDI (§15.4.1) — CobolDec.FromDouble,
        // the §8.8.1.5.1 implementor-defined float→SDIDI conversion, defined here as the shortest round-trip
        // identity — and lands at the receiver ONCE, instead of truncating to unscaled at a receiver-derived
        // working scale first (the triage rows RV-15.8.4-1 / RV-15.10.4-1 / RV-15.11.4-1 / RV-15.35.4-1 measured
        // the double quantization). RealResult stays inside the conversion: it is the NaN → EC-ARGUMENT-FUNCTION
        // raise site, so the screen fires identically in every mode and every receiver shape.
        //
        // ⛔ AND THE ORDER IS STRUCTURAL, NOT POSITIONAL. The native arms live in <see cref="RenderFloatNative"/>,
        // which is not given the mode and cannot test it, so an arm added there CANNOT pre-empt the container rule
        // — the defect this shape closes was exactly an arm sitting one line too high. `num.StandardDecimal` is
        // read HERE and nowhere below (StandardModeReturnedValueContainerDriftTests proves the property over every
        // Float: true catalog row, so a new float function inherits it without anyone remembering to).
        return num.StandardDecimal
            ? new NumX(RuntimeApi.DecFromDouble(RuntimeApi.Intrinsic("RealResult", call)), 0, Dec: true)
            : RenderFloatNative(ic, call);
    }

    /// <summary>The §15.4.1 float family under NATIVE arithmetic — the receiver-shape arms, and ONLY those.
    /// <para>⛔ It is a separate method so the arithmetic mode is settled before any of this runs: the container
    /// question belongs to <see cref="RenderFloat"/>, this one answers the D16 quantization question. Deliberately
    /// mode-blind (kb/Work PB253).</para></summary>
    private NumX RenderFloatNative(BoundIntrinsicCall ic, string call)
    {
        var sig = ic.Sig;
        // ⛔ WHETHER THIS VALUE LANDS AT ALL, AND AT WHAT SCALE AND MODE, IS ONE DECISION TAKEN ONCE ON
        // ReceiverContext (kb/Work PB647, then PB653 — ReceiverContext.FloatLanding carries the derivation):
        // the FINAL TRANSFER into a fixed-point resultant lands at THAT identifier's scale with the statement's
        // ROUNDED mode (§14.7.4.1 — "truncation is relative to the size provided for the resultant identifier";
        // §14.7.4.3 rule 2 — no ROUNDED phrase IS ROUNDED MODE IS TRUNCATION), which is exactly the landing
        // `MOVE FUNCTION SQRT(3) TO S` performs (§14.6.8.2 rule 4), so §15.4.1's one-returned-value identity
        // holds across the two channels. EVERY OTHER SHAPE — a nested intermediate, a float receiver, a
        // receiver-less render — keeps the binary64, because there is no transfer here to quantize FOR.
        var landing = num.Receiver.FloatLanding(num.Outermost);
        // A float RECEIVER keeps the transcendental result in the binary64 pipeline (full precision — SQRT(2) into a
        // COMP-2 is 1.4142135623730951, not the scale-9 1.414213562); a fixed receiver quantizes through FromDouble
        // at the landing chosen just above. (D16 review finding.)
        //
        // ⛔ AND SO DOES A NESTED OPERAND — THE THIRD SHAPE, AND THE ONE THAT WAS MISSING (kb/Work PB653).
        // A returned value feeding a larger expression is not transferred anywhere yet, so quantizing it at a
        // ≥9 working scale merely truncates it one digit too early and every operation above it propagates the
        // loss: `COMPUTE R = FUNCTION SQRT(3) * 2` into PIC 9V9(9) gave 3.464101614 where a COMP-2 item holding
        // the IDENTICAL binary64 gave 3.464101615, and `FUNCTION SQRT(10) ** 2` gave 9.999999998 against that
        // arm's 10.000000000. §15.4.1 forbids the split — "the returned value is the same for all instances of a
        // given function within a single execution of the runtime element so long as the value and order of the
        // arguments, the collating sequence, and the locale are unchanged" — and the receiver-less RELATION
        // channel, which has always kept the binary64, already agreed with the COMP-2 item. Keeping it here makes
        // the four channels one value by CONSTRUCTION, and lets CombineCore's Real lane evaluate the expression
        // entirely in IEEE binary64 as numeric design D16 says it does. (Guard digits are NOT the alternative:
        // a landing cannot know how many multiplications sit above it, and two guarded scale-23 operands
        // multiply into a scale-46 product the Int128 carrier wraps — ReceiverContext.FloatLanding derives it.)
        //
        // ⛔ AND SO DOES A RECEIVER-LESS CONTEXT, UNDER NATIVE ARITHMETIC (fix-queue PB13, the half no
        // working-scale choice can reach — the standard-mode arm above now owns the other modes, which is the
        // scope this citation always had).
        // §15.4.1: "When native arithmetic is in effect, the characteristics and representation of the returned
        // value are defined by the implementor" — WiseOwl COBOL's determination is that the §15.4.1 float family's
        // returned value IS a binary64, and the quantization exists ONLY to land it in a fixed-point receiver,
        // whose scale is what defines the quantization. With no receiver there is no scale to quantize TO, and the
        // arbitrary ws = 9 stand-in was not merely a different rendering — it was WRONG, because FromDouble
        // saturates: `IF FUNCTION EXP10(30) = FUNCTION EXP10(31)` evaluated TRUE (both sides saturating to the
        // same Int128.MaxValue) and `DISPLAY FUNCTION EXP10(31)` printed 170141183460469231731687303715.884105727.
        // Every consumer of a receiver-less numeric already has a Real arm and is MORE correct on it: a relation
        // compares natively (§8.8.4.2.4 — "comparison proceeds by the rules of native arithmetic", the same arm a
        // COMP-2 operand takes), the text channel renders through the one CobolFloat.Display a float ITEM uses
        // (§14.9.11.4 GR1), and a MOVE source lands through CobolFloat.ToScaled AT THE RECEIVER'S SCALE — which is
        // the saturation-safe form, since the store's capacity check then sees the sentinel undivided.
        // This is what makes FUNCTION SQRT(2) and a COMP-2 item holding that value the SAME value everywhere,
        // which OperandText.NumericIntrinsicText already documented as required and the ws = 9 form silently broke.
        // ⛔ THE UNQUANTIZED ARMS KEEP THE RAISE SITE. FromDouble is where an out-of-domain NaN became the §15.3
        // EC-ARGUMENT-FUNCTION default (or the raise, under checking); leaving the value in binary64 skips it, so
        // `COMPUTE R = FUNCTION ACOS(2)` gave the default 0 while `IF FUNCTION ACOS(2) = 0` propagated a raw NaN
        // and compared FALSE. RealResult restores the screen without re-quantizing — a function's returned value
        // must not depend on the SHAPE of its receiver (§15.4), and under EC-ARGUMENT-FUNCTION checking
        // §14.6.13.1 requires the condition be raised at all. (Found by the Phase-B §15.55 refuter.)
        // ⛔ THE BINARY64 THAT STAYS IS AN APPROXIMATION THE ENGINE PRODUCED, NOT A FLOAT ITEM'S CONTENT (kb/Work
        // PB1641). It is the SAME value a COMP-2 item holding it would carry, so every receiver-less channel (a
        // relation, the text channel, a MOVE source) still reads one binary64; but as an OPERAND it is flagged
        // Approximate, and NumericRenderer.CombineCore — the one place the lane is chosen — lifts it to the SDIDI
        // (its shortest-round-trip decimal) beside fixed-point operands, so `13.2 + FUNCTION SQRT(16)` is 17.2 and not
        // the 17.1 a truncating store of 13.199999999999999 + 4.0 gives.
        if (!landing.Quantize)
            return new NumX(RuntimeApi.Intrinsic("RealResult", call), 0, Real: true, Approximate: true);
        // ⛔ A BOUNDED CODOMAIN CLAMPS THE QUANTIZED VALUE (fix-queue PB65 / RV-15.75.4-1): the catalog row
        // carries the §15.x.4 bound and the quantizer refuses to round out of it — RANDOM's [0,1) reached
        // exactly 1.000000000 in a 9V9(9) receiver, ASIN(1) exceeded its closed-but-irrational π/2. The
        // rounding itself stays (it recovers the SQRT(10) ** 2 binary64 artifact); only the exit is closed.
        // RenderFloat's standard-mode Dec arm needs no clamp: the un-quantized double never exits its codomain.
        // The quantizer's landing form past the carrier is the STATEMENT's (kb/Work PB77): saturate under ON SIZE
        // ERROR / EC-SIZE checking (the capacity check raises), the low-order digits for the no-phrase store.
        // ⛔ AND THE CLAMP IS NOW ONLY LIVE UNDER AN EXPLICIT ROUNDING MODE (kb/Work PB647): a TRUNCATING landing
        // can never leave a codomain the double is already inside, so the no-phrase COMPUTE and the MOVE agree by
        // construction; a ROUNDED one still needs the clamp, which is the case PB65 measured.
        if (sig.Codomain != IntrinsicCodomain.None)
            return new NumX(RuntimeApi.Intrinsic("FromDoubleBounded",
                $"{call}, {landing.Scale}, {RuntimeApi.RoundingText(landing.Mode)}, {RuntimeApi.CodomainConst(sig.Codomain)}{CheckedFlag}"), landing.Scale);
        return new NumX(RuntimeApi.Intrinsic("FromDouble", $"{call}, {landing.Scale}, {RuntimeApi.RoundingText(landing.Mode)}{CheckedFlag}"), landing.Scale);
    }

    // ── Argument rendering (the ONE NumericRenderer for every numeric-kind argument) ────────────────────────
    //
    // ⛔ THERE IS ONE ENTRY — `num.AsNum(operand, receiver)` — AND SEVERAL INTAKES OVER IT, WITH DIFFERENT VALUE
    // SEMANTICS. Every intake below therefore DECLARES its contract as `INTAKE(<class>)` in its doc comment, and
    // `IntrinsicArgumentIntakeContractDriftTests` fails if a member that calls `num.AsNum(` does not declare one.
    // The classes are exhaustive and mean exactly this — what the intake does to the argument's VALUE:
    //
    //   EXACT        the operand on its own carrier at its own scale; nothing is rescaled or converted.
    //   LIFTED       lifted to the SDIDI; the VALUE is preserved (a float converts in per §8.8.1.5.1).
    //   ALIGNED      rescaled UP to the argument list's common maximum scale; no digit lost, headroom spent.
    //   LANDED       landed into the exact Int128 lane at the RECEIVER's working scale — TRUNCATES past it.
    //   INTEGRAL     rescaled to scale 0 with truncation (the §15.3 integer positions).
    //   APPROXIMATED converted to binary64; exactness past 2^53 is surrendered.
    //   PREDICATE    rendered only to ask a question; the text is discarded, so no value semantics apply.
    //
    // ⛔ THE VOCABULARY EXISTS BECAUSE A ROW ABOUT "WHAT HAPPENS TO AN ARGUMENT" WAS VERIFIED FROM ONE OF THEM
    // (kb/Work PB251, row RV-15.4.1-2). The adjudication rested on "Arg/RawArg/DecArg is the ONE argument intake
    // for the numeric channel", and the file carried several with different semantics — so a claim true of
    // `RawArg` (it renders an argument without redefining its value) was recorded against `Arg`, which is the one
    // intake that CAN redefine it. Reading a class name off the member is now the whole check.

    /// <remarks>INTAKE(LANDED) — an SDIDI operand (and, under a standard mode, a float) is LANDED into the exact <c>Int128</c> lane at the receiver's working scale, truncating past it — the one intake that can change the argument's VALUE, which is why no function whose definition FIXES its value may consume its argument through it (kb/Work PB251).</remarks>
    private NumX Arg(BoundIntrinsicCall ic, int i) => Landed(num.AsNum(ic.Args[i], num.Receiver));

    /// <summary>
    /// ⛔ THE ONE SDIDI → EXACT-CARRIER LANDING FOR AN INTRINSIC ARGUMENT (fix-queue PB32/PB14).
    /// </summary>
    /// <remarks>
    /// <para><see cref="NumX"/> has THREE carriers — exact scaled <see cref="Int128"/>, the <c>CobolDec</c> SDIDI,
    /// and binary64 — and this renderer's arms were written for two. Under <c>ARITHMETIC IS STANDARD-DECIMAL</c>
    /// every arithmetic expression renders as a <c>Dec</c> carrier (<c>NumericRenderer.CombineCore</c> /
    /// <c>Power</c>), so a §15.3 type-10 arithmetic-expression argument — legal at 2014 and 2023 — was handed raw
    /// to a body expecting <c>Int128</c> and the user saw a Roslyn <c>CS1503</c> on conforming COBOL.</para>
    /// <para>⛔ IT IS LANDED HERE, AT <see cref="Arg"/>, AND NOT IN EACH ARM. Placing the arm in
    /// <c>NumericRenderer.Align</c> alone fixed only the variadic family that happens to route through it —
    /// measured: MAX / MIN / MOD / MEDIAN recovered while ABS, SIGN, INTEGER, FRACTION-PART and FACTORIAL still
    /// failed to compile, because those arms consume <c>Arg(...).Expr</c> (or <see cref="AsInt"/>) directly. Every
    /// numeric argument in this renderer originates HERE, so one landing covers all of them and the NEXT arm
    /// added is covered without anyone remembering to (feedback_change_the_dispatch_not_the_callers). Landing
    /// early also gives the SDIDI a compile-time <c>Scale</c>, so it participates in
    /// <see cref="AlignedArgsEx"/>'s common-scale maximum instead of contributing the placeholder 0 and dragging
    /// the whole argument list down to integer alignment.</para>
    /// <para>⚠ THE SCALE IS A CHOICE, AND IT IS AN IMPLEMENTOR'S CHOICE — NO CLAUSE PRESCRIBES IT. An SDIDI carries
    /// its exponent at RUN time, so there is no compile-time scale to preserve; the value lands through the same
    /// <c>WorkingScale(floor)</c> discipline the float family uses — the receiver's scale, never below
    /// <see cref="ReceiverContext.SdidiLandingScaleFloor"/>, capped at the receiver's <c>Int128</c> headroom by the
    /// PB13 argument. ⛔ THAT FLOOR IS NOT A NUMVAL RULE AND MUST NOT BE DESCRIBED AS ONE (kb/Work PB251): the
    /// sentence here used to read "never below the §15.67 fraction floor", and §15.67 prescribes no working scale
    /// at all — the family's value is fixed by §15.67.4 r1 and now renders on the SDIDI carrier in every mode.
    /// ⛔ THE §15.4.1 r1 FAMILY NO LONGER PASSES THROUGH HERE (fix-queue PB56): under a standard mode a
    /// Dec/float-bearing exact-family call routes to <see cref="RenderDec"/> BEFORE any landing, so this
    /// truncation now serves only the residual non-EAE consumers (the integer-argument intake via
    /// <c>AsInt</c>, and the arms with no Dec body) — where scale-0/format semantics make the landing the
    /// defined behavior rather than an approximation.</para>
    /// </remarks>
    /// <remarks>
    /// ⛔ <b>AND A FLOAT OPERAND LANDS THE SAME WAY UNDER A STANDARD ARITHMETIC MODE (fix-queue PB38).</b> This is
    /// where the ARITHMETIC MODE beats the float branch, which is the ordering `COBOLNET_NUMERIC_DESIGN.md` D3
    /// states in words ("the mode branch runs BEFORE the D16 float branch") and which
    /// <c>NumericRenderer.CombineCore</c>, <c>NumericRenderer.Power</c> and <c>ConditionRenderer</c> all obey —
    /// <c>RenderNum</c> was the ONE renderer that did not, so a single COMP-1/COMP-2 argument demoted the whole
    /// list to binary64 even under <c>ARITHMETIC IS STANDARD-DECIMAL</c>, where §15.4.1 r1 is unconditional (the
    /// returned value <i>shall equal</i> the equivalent arithmetic expression) and §8.8.1.5.2 r1 converts every
    /// fixed-point operand into an SDIDI EXACTLY. MEASURED before the fix, with three 18-digit items and a COMP-2
    /// pair: <c>FUNCTION MEDIAN(H1 H2 H3 F1 F1)</c> returned 100000000000000004.76 where the SDIDI-exact answer
    /// is 100000000000000001, and <c>FUNCTION MAX(H1 H2 H3 F1)</c> returned the same 100000000000000004.76
    /// against 100000000000000003 — the three distinct 18-digit operands all collapse to ONE binary64 (the ulp
    /// at 1e17 is 16) and compare EQUAL, so the §8.8.4.2.4 comparison the clause mandates never happens. The same
    /// operands without the float are exact in the same program.
    /// <para>The float converts through <see cref="NumericRenderer.DecOperand"/> — the compiler's own §8.8.1.5.1
    /// conversion, the one <c>CombineCore</c> and <c>Power</c> already use — and then lands by the identical
    /// route a <c>Dec</c> operand takes. So there is ONE landing here, not a second mechanism beside the first,
    /// and the exact Int128 family evaluates the EAE as §15.4.1 r1 requires.</para>
    /// </remarks>
    private NumX Landed(NumX x) => num.Landed(x, num.Receiver);   // the ONE landing (NumericRenderer.Landed — kb/Work PB84)

    /// <summary>
    /// The functions whose DEFINITION fixes the returned value in EVERY arithmetic mode, so §15.4.1's
    /// implementor latitude never reaches them and no working scale may be imposed (kb/Work PB251). Asked by
    /// <see cref="RenderNum"/> BEFORE the arithmetic-mode dispatch.
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
    /// value represented by argument-1". <see cref="RenderDec"/> holds their SDIDI arms (its <c>alwaysDec</c>
    /// list already named them), so this predicate only decides WHEN that lane is taken, never what it does.
    /// If it ever returned <c>null</c> here the switch's <c>default</c> stages LOUD — never a quiet fallback to
    /// a working-scale render.</para>
    /// </remarks>
    private static bool ValueFixedByDefinition(string runtimeMethod) => runtimeMethod is "Numval" or "NumvalC";

    // ── The STANDARD-DECIMAL body dispatch (fix-queue PB56) ──────────────────────────────────────────────────

    /// <summary>An argument as rendered, WITHOUT the unscaled landing — the SDIDI lane's input.</summary>
    /// <remarks>INTAKE(EXACT) — the operand on its OWN carrier at its OWN scale — nothing rescaled, converted or truncated.</remarks>
    private NumX RawArg(BoundIntrinsicCall ic, int i) => num.AsNum(ic.Args[i], num.Receiver);

    /// <summary>An argument lifted to a <c>CobolDec</c> expression from its RAW carrier: a Dec operand passes
    /// through, a float converts per §8.8.1.5.1, a fixed-point operand lifts exactly — never quantized.</summary>
    /// <remarks>INTAKE(LIFTED) — lifted to the SDIDI from the RAW carrier — exact for a fixed-point operand, §8.8.1.5.1 for a float; the value is preserved, the carrier is not.</remarks>
    private string DecArg(BoundIntrinsicCall ic, int i) => num.DecOperand(RawArg(ic, i));

    /// <remarks>INTAKE(LIFTED) — the variadic form of <see cref="DecArg"/> — same contract, per element.</remarks>
    private string DecArgList(BoundIntrinsicCall ic) =>
        ArgArray(ic, 0, "CobolDec", DecOf) ?? string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => DecArg(ic, i)));

    /// <summary>One operand lifted to a <c>CobolDec</c> from its RAW carrier — the per-operand form of <see cref="DecArg"/>
    /// (a table(ALL) element renders here inside its enumeration lambda).</summary>
    /// <remarks>INTAKE(LIFTED) — the per-operand form of <see cref="DecArg"/> — same contract.</remarks>
    private string DecOf(BoundOperand a) => num.DecOperand(num.AsNum(a, num.Receiver));

    /// <summary>One operand as a C# double from its RAW carrier — the per-operand form of <see cref="Dbl"/>.</summary>
    /// <remarks>INTAKE(APPROXIMATED) — binary64 from the RAW carrier — exactness past 2^53 is surrendered. §15.4.1's native latitude permits that for an equivalent-arithmetic-expression function; a standard mode and a value-fixing definition do not.</remarks>
    private string DblOf(BoundOperand a) => NumericRenderer.Real(num.AsNum(a, num.Receiver));

    // ── table(ALL) arguments — the enumeration seam (ISO §15.3; kb/Work PB62) ─────────────────────────────

    /// <summary>The C# expression enumerating a table(ALL) argument's elements as a <c>T[]</c>:
    /// <c>CobolTable.AllArgs</c> over the place's ranges — each a lambda over the index vector, so a nested
    /// dynamic-capacity table's capacity reads the OUTER occurrence's — with the element rendered by
    /// <paramref name="element"/> from the element operand (its subscripts are the index variable's slots).</summary>
    /// <param name="lead">When given, renders the FIRST enumerated element instead (<c>CobolTable.AllArgs</c>'s
    /// lead lambda) — the table(ALL) is the leading argument and its first element is argument-1.</param>
    private static string AllArgsExpr(TableAllPlace all, string csType, Func<BoundOperand, string> element,
        Func<BoundOperand, string>? lead = null)
    {
        string v = all.Indices.CsName;
        var counts = all.Counts.Select(c => $"{v} => (long)({AllCountExpr(c)})");
        var e = new BoundFieldOperand(all.Element);
        return RuntimeApi.TableAllArgs(csType, counts, $"{v} => {element(e)}", lead is null ? null : $"{v} => {lead(e)}");
    }

    /// <summary>One ALL level's range as a C# <c>long</c>-valued expression — the ONE occurrence-count renderer
    /// (<c>PlaceRenderer.OccurrenceCount</c>), shared with INITIALIZE's per-occurrence loop bound (kb/Work
    /// PB393). It was defined here until INITIALIZE became its second consumer.</summary>
    private static string AllCountExpr(AllCount c) => PlaceRenderer.OccurrenceCount(c);

    /// <summary>The argument list from position <paramref name="from"/> on as ONE <c>T[]</c> expression when a
    /// table(ALL) argument is among them (else null — the caller keeps its comma-list form, byte-identical to
    /// before): runs of written operands become array literals, each ALL an <see cref="AllArgsExpr"/>, joined
    /// in source order by <c>CobolTable.ArgConcat</c> — the ONE array a <c>params T[]</c> body binds to.</summary>
    /// <param name="lead">When given, renders the list's FIRST argument (the operand at <paramref name="from"/>, or
    /// the first element that operand's table(ALL) enumerates) — a leading argument with its own intake.</param>
    private string? ArgArray(BoundIntrinsicCall ic, int from, string csType, Func<BoundOperand, string> render,
        Func<BoundOperand, string>? lead = null)
    {
        if (!ic.Args.Skip(from).Any(a => a is BoundFieldOperand { Place: TableAllPlace })) return null;
        var parts = new List<string>();
        var run = new List<string>();
        void Flush() { if (run.Count > 0) { parts.Add($"new {csType}[] {{ {string.Join(", ", run)} }}"); run.Clear(); } }
        Func<BoundOperand, string>? firstLead = lead;   // consumed by the first argument only
        foreach (var a in ic.Args.Skip(from))
        {
            if (a is BoundFieldOperand { Place: TableAllPlace all }) { Flush(); parts.Add(AllArgsExpr(all, csType, render, firstLead)); }
            else run.Add((firstLead ?? render)(a));
            firstLead = null;
        }
        Flush();
        return parts.Count == 1 ? parts[0] : RuntimeApi.TableArgConcat(csType, parts);
    }

    /// <summary>A body with a LEADING positional argument and a <c>params</c> tail (PRESENT-VALUE's rate, then the
    /// amounts): the tail may enumerate; a table(ALL) in the LEADING position itself is legal too (§15.3 puts no
    /// position on the ALL — "as if each table element … were specified"), so then the flat list is bound once
    /// and split at run time.</summary>
    /// <param name="lead">The LEADING argument's own intake (PRESENT-VALUE's screened discount base), in place of
    /// <paramref name="render"/>. ⛔ Also when that argument is itself a table(ALL) (kb/Work PB1000): the lead then
    /// renders the enumeration's FIRST element — argument-1 (ISO §15.3 rule 14's left-to-right order) — from the
    /// element operand, inside the walk (<c>CobolTable.AllArgs</c>'s lead lambda), before it joins the list.
    /// Screening the list's <c>[0]</c> instead read a binary64 image: a legal rate −0.99999999999999999999 had
    /// already become −1.0 there and was refused under §15.74.3 r2.</param>
    private string LeadThenTail(BoundIntrinsicCall ic, string method, string prefix, string csType, Func<BoundOperand, string> render, string mid = "", Func<BoundOperand, string>? lead = null)
    {
        if (ic.Args[0] is BoundFieldOperand { Place: TableAllPlace })
        {
            string xs = NextWithVar();
            return RuntimeApi.With(ArgArray(ic, 0, csType, render, lead)!, xs, RuntimeApi.Intrinsic(method, $"{prefix}{xs}[0], {mid}{xs}[1..]"));
        }
        string tail = ArgArray(ic, 1, csType, render) ?? string.Join(", ", ic.Args.Skip(1).Select(render));
        return RuntimeApi.Intrinsic(method, $"{prefix}{(lead ?? render)(ic.Args[0])}, {mid}{tail}");
    }

    /// <summary>The number of arguments a call's list stands for when it is a compile-time fact — every table(ALL)
    /// argument with fixed ranges counted as its elements; null when an ALL ranges over a runtime count.</summary>
    private static long? StaticArgCount(BoundIntrinsicCall ic)
    {
        long n = 0;
        foreach (var a in ic.Args)
        {
            if (a is BoundFieldOperand { Place: TableAllPlace all }) { if (all.StaticCount is not { } k) return null; n += k; }
            else n++;
        }
        return n;
    }

    /// <summary>A fresh name for a <c>CobolTable.With</c> binding — nested bindings must not shadow.</summary>
    private string NextWithVar() => $"__xs{_withSerial++}";
    private int _withSerial;

    /// <summary>Does any argument arrive as a Dec (SDIDI) or float carrier, before any landing?</summary>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question; the rendered text is discarded, so no value semantics apply.</remarks>
    private bool AnyDecOrRealRaw(BoundIntrinsicCall ic) =>
        ic.Args.Any(a => { var x = num.AsNum(a, num.Receiver); return x.Dec || x.Real; });

    /// <summary>Can aligning this call's arguments to their common scale — and accumulating the aligned values —
    /// leave the <see cref="Int128"/> carrier? True only when EVERY argument is an exact scaled operand with a KNOWN
    /// digit bound (<see cref="NumX.Digits"/>) and the sum of the magnitudes the function adds, each bounded by its
    /// aligned digits, exceeds <see cref="Int128.MaxValue"/>. An argument whose bound is unknown answers false: nothing
    /// is provable, and the escape-checked native arm (PB252) keeps its documented behaviour for it.</summary>
    /// <remarks>
    /// <para>Which magnitudes are added is a property of the FUNCTION: SUM and MEAN add every argument (a table(ALL)
    /// argument, whose run-time count is unknown, is charged 9 more digits); RANGE subtracts two; MEDIAN and
    /// MIDRANGE add two and halve through ×10; MOD and REM align a pair and return one of them.</para>
    /// <para>The aligned value's own digits are an UPPER bound by construction: an argument's unscaled digits plus
    /// the scale gap it is shifted by.</para></remarks>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question — of the RAW operand: its carrier flags and its digit bound.</remarks>
    private bool AlignmentMayLeaveCarrier(BoundIntrinsicCall ic)
    {
        // ⛔ MOD / REM ARE NOT ROUTED. Their value is a DIFFERENCE of nearly equal magnitudes: MOD(9E30, 0.12345678) is
        // 0.1194939, which needs 39 digits of the dividend, and the SDIDI body keeps 34 — it would answer an
        // approximation SILENTLY where the native arm refuses LOUDLY (EC-SIZE-OVERFLOW at the carrier). The summing
        // family's value, in contrast, is as large as its arguments, so every digit a receiver can store (≤ 31) lies
        // inside the SDIDI's 34. (PB621 names SUM, RANGE, MEAN, MEDIAN and MIDRANGE only.)
        if (ic.Sig.RuntimeMethod is "ModScaled" or "RemScaled") return false;
        var xs = ic.Args.Select(a => num.AsNum(a, num.Receiver)).ToList();
        if (xs.Count == 0 || xs.Any(x => x.Dec || x.Real || x.Digits == 0)) return false;
        int s = xs.Max(x => x.Scale);
        // ⛔ THE BOUND IS ON THE VALUE, NOT ON THE WIDEST ARGUMENT PLUS A DIGIT COUNT. Argument i, aligned to the common
        // scale, is below 10^D_i (D_i its unscaled digits plus the scale gap — a table(ALL) argument stands for an
        // unknown number of elements, so it is charged nine more digits); the function's value is bounded by the sum of
        // the magnitudes it adds: every argument for SUM and MEAN, the two largest for RANGE, and those two times ten
        // for MEDIAN and MIDRANGE (added, then halved through ×10). `9(29)` beside `V9(9)` is 38 + 9 digits of two
        // values whose sum is 1.0000000010e38 — the Int128 carrier holds it, and the old "widest + digits of n"
        // allowance sent it to the SDIDI, which keeps 34 digits (kb/Work PB621's review follow-up).
        var terms = xs.Select((x, i) => Pow10Bound(x.Digits + (s - x.Scale)
            + (ic.Args[i] is BoundFieldOperand { Place: TableAllPlace } ? 9 : 0))).OrderByDescending(t => t).ToList();
        UInt128? bound = ic.Sig.RuntimeMethod switch
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

    /// <summary>Does any argument arrive on the SDIDI carrier (a native integer power — kb/Work PB69)?</summary>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question; the rendered text is discarded.</remarks>
    private bool AnyDecRaw(BoundIntrinsicCall ic) => ic.Args.Any(a => num.AsNum(a, num.Receiver).Dec);

    /// <summary>
    /// ⛔ THE FIRST HALF OF THE ALWAYS-SDIDI SET, AND THE ONLY HALF WITH A STRUCTURAL DEFINITION: the native-switch
    /// arms in <see cref="RenderNum"/> that CROSS-ALIGN their arguments to one common scale before evaluating.
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
    /// pattern so that <c>CrossAlignedArmsDriftTests</c> can hold it against the switch itself: the test reads
    /// <c>IntrinsicRenderer.cs</c>, collects the case labels of every arm whose body calls
    /// <see cref="AlignedArgs"/> / <see cref="AlignedArgsEx"/> or <c>NumericRenderer.Align(x, s)</c> against a
    /// COMMON scale, and fails if any of them is missing here. A new aligning arm is therefore routed
    /// automatically or the build goes red — it can no longer be forgotten.</para>
    /// <para>MAX / MIN / ORD-MAX / ORD-MIN are deliberately ABSENT: <see cref="RawArgPairs"/> aligns each argument
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
    /// The second half of the always-SDIDI set: functions whose standard-mode returned value a RULE fixes
    /// outright, so the argument carrier cannot matter. Unlike <see cref="CrossAlignedNativeArms"/> this half has
    /// no structural derivation — each member is here for its own cited reason and is listed one by one.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><c>Sqrt</c> — §15.84.4 r2 fixes the 34-digit correctly-rounded root; without this the early-out
    ///         returned null for a plain-operand call and SQRT fell to binary64 (kb/Work PB116). The arm's own
    ///         <c>when num.StandardDecimal</c> guard keeps a NATIVE call on the float lane.</item>
    ///   <item><c>Factorial</c> — §15.36.4 r1c on the SDIDI, where 34! is exact against the native lane's
    ///         documented 33 cap (kb/Work PB125); same <c>when</c> guard, same reason.</item>
    ///   <item><c>Annuity</c> / <c>PresentValue</c> / <c>Variance</c> / <c>StandardDeviation</c> — the four
    ///         INEXACT-EAE functions: their equivalent arithmetic expressions DIVIDE, so even an all-fixed-point
    ///         argument list must evaluate in SDIDI form. This is what removed their COBOLNET0899 stage.</item>
    ///   <item><c>Numval</c> / <c>NumvalC</c> / <c>NumvalF</c> — §15.67.4 r1 / §15.68.4 r1 / §15.69.4 r3 name the
    ///         returned value outright ("the numeric value represented by argument-1"; NUMVAL-F's r2/r3 pair
    ///         grants NATIVE arithmetic the approximation and STANDARD-DECIMAL none) and §15.4.1 places it in an
    ///         SDIDI. Their argument is a string, so the carrier question never arises; before this arm the
    ///         standard-mode value rode the native Int128 projection at the item-92 working scale
    ///         (fix-queue PB60, RV-15.67.4-1a). ⛔ For NUMVAL and NUMVAL-C the MODE is not part of the
    ///         question at all (kb/Work PB251): <see cref="ValueFixedByDefinition"/> routes them to
    ///         <see cref="RenderDec"/> BEFORE the arithmetic-mode dispatch, so that arm precedes this
    ///         set; they are listed here because a string argument makes <c>AnyDecOrRealRaw</c> false
    ///         and the early-out would otherwise return null under native. NUMVAL-F is standard-mode
    ///         only — §15.69.4 r2 grants IT, and only it, the native approximation.</item>
    /// </list>
    /// </remarks>
    private static readonly FrozenSet<string> StandardValueFixedByRule = new[]
    {
        "Sqrt", "Factorial",
        "Annuity", "PresentValue", "Variance", "StandardDeviation",
        "Numval", "NumvalC", "NumvalF",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Under a standard arithmetic mode, evaluate a §15.4.1 r1 function's equivalent arithmetic expression ON
    /// the SDIDI carrier (<c>CobolIntrinsics.*Dec</c>) and return the result as a Dec-valued <see cref="NumX"/>
    /// that lands at the receiver ONCE — the PB56 Dec-carrier body, replacing the interim per-argument
    /// unscaled landing whose fixed working scale truncated sub-microscale operands to zero.
    /// </summary>
    /// <remarks>
    /// <para>Routing, in ONE line: <c>CrossAlignedNativeArms ∪ StandardValueFixedByRule</c> route here for EVERY
    /// argument shape, and everything else routes here only when a RAW argument is Dec or float. The NON-aligning
    /// exact-EAE functions with an all-fixed-point list stay on the exact Int128 family, whose every EAE step is
    /// then digit-identical to the SDIDI evaluation (documented equivalence, <c>COBOLNET_NUMERIC_DESIGN.md</c> D3;
    /// the &gt;34-digit exact residue keeps MORE precision than per-op rounding would) — that equivalence is what
    /// ALIGNMENT breaks, which is exactly why the aligning arms are unconditional (see
    /// <see cref="CrossAlignedNativeArms"/>). Returns null for functions with no Dec body (the prose family
    /// converts its binary64 result in <see cref="RenderFloat"/>).</para>
    /// <para>Result carriers: SIGN / ORD-MAX / ORD-MIN return INTEGERS (scale-0 exact); everything else
    /// returns the SDIDI itself (<c>Dec: true</c>), the same channel the R18 constant arms and MEAN's
    /// existing SDIDI division already prove end-to-end.</para>
    /// </remarks>
    private NumX? RenderDec(BoundIntrinsicCall ic)
    {
        string m = ic.Sig.RuntimeMethod;
        // ⛔ TWO SETS, ONE DECISION (kb/Work PB252): CrossAlignedNativeArms is STRUCTURAL (the native arm
        // cross-aligns, so alignment can need more digits than the Int128 carrier holds) and
        // StandardValueFixedByRule is the cited-exception half. Between them they replace the inline
        // `alwaysDec` name chain, and ExactCarrierBoundaryDriftTests derives the first from this switch.
        // ⛔ AND THE NUMVAL PAIR REACHES HERE IN EVERY MODE (kb/Work PB251): RenderNum consults RenderDec for
        // Numval/NumvalC through ValueFixedByDefinition BEFORE the arithmetic-mode dispatch, because
        // §15.67.4 r1 / §15.68.4 r1 name the returned value with no mode qualification and §15.4.1's
        // implementor latitude reaches only a function whose definition does NOT otherwise specify it. That
        // arm precedes this set; the pair is listed below anyway because their argument is a STRING, so
        // AnyDecOrRealRaw is false and without the membership this early-out would return null under native.
        bool alwaysSdidi = CrossAlignedNativeArms.Contains(m) || StandardValueFixedByRule.Contains(m);
        if (!alwaysSdidi && !AnyDecOrRealRaw(ic)) return null;

        string mode = num.IntermediateMode;
        return m switch
        {
            // The NUMVAL family: the ONE positional scan's (sign, unscaled, frac[, exp]) lifted to the SDIDI exactly
            // at the parsed scale — no working scale, no receiver. ⛔ NUMVAL and NUMVAL-C reach these arms in EVERY
            // arithmetic mode (kb/Work PB251 — §15.67.4 r1 / §15.68.4 r1 fix the value with no mode qualification,
            // and §15.4.1's "unless otherwise specified in the function definition" is what makes that binding
            // under native too), so the digit cap is 31 or 34 by the MODE and is passed explicitly whenever it is
            // not the family's default — exactly as the TEST- twins receive it, so the scan's r1b sub-note-4
            // verdict and the value agree on the same number. NUMVAL-F alone stays standard-mode-only here: its
            // §15.69.4 r2 grants native arithmetic an approximation, which RenderNum's float arm delivers.
            "Numval" => Dec(RuntimeApi.Intrinsic("NumvalDec", $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}")),
            // StandardValueFixedByRule keeps this the path for EVERY NUMVAL-C under a standard arithmetic
            // mode, and RenderNum's ValueFixedByDefinition arm reaches it under NATIVE too (PB251) — the
            // LOCALE arm is needed HERE too or --arithmetic standard is silently wrong (indexing the absent
            // ic.Args[1] — no argument-2 is injected under LOCALE; PB64 T6).
            "NumvalC" when ic.LocaleWritten => Dec(RuntimeApi.Intrinsic("NumvalCLocaleDec",
                $"{Str(ic.Args[0])}, {LocaleTagArg(ic)}{AnycaseFlag(ic)}{DigitCapFlag}")),
            "NumvalC" => Dec(RuntimeApi.Intrinsic("NumvalCDec",
                $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{CommaFlag}{AnycaseFlag(ic)}{DigitCapFlag}{AnycaseClassification(ic)}")),
            "NumvalF" => Dec(RuntimeApi.Intrinsic("NumvalFDec", $"{mode}, {Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}")),
            // SQRT (§15.84.4 r2; kb/Work PB116): the one function whose standard-mode value is FIXED — the
            // 34-digit correctly-rounded root; without this arm it fell to RenderFloat's binary64 Math.Sqrt.
            // ⛔ Guarded to STANDARD-DECIMAL: RenderDec is also consulted under NATIVE when a float-typed
            // argument is present (AnyDecOrRealRaw), and an unguarded arm would flip native float SQRT onto
            // the Dec lane — a behavior change r4's approximation licence does not ask for.
            "Sqrt" when num.StandardDecimal => Dec(RuntimeApi.Intrinsic("SqrtDec", $"{mode}, {DecArg(ic, 0)}")),
            // FACTORIAL (§15.36.4 r1c; kb/Work PB125): the SDIDI product loop — FACTORIAL(34) is exact where
            // the native Int128 lane's 33 cap returned the §15.3 default 0 on a conforming argument.
            "Factorial" when num.StandardDecimal =>
                Dec(RuntimeApi.Intrinsic("FactorialDec", $"{mode}, {IntArg(ic, 0)}")),
            // COMBINED-DATETIME (§15.17.4 r1's EAE `argument-1 + (argument-2 / 100000)`; kb/Work PB620): the one
            // §15.4.1 r1 function PB56 left on the landing intake. Argument-2 carries no fraction-digit limit
            // (§15.17.3 r2 → §15.5.5's magnitude-only "standard numeric time form"), so a landing at
            // max(receiver scale, 6) truncated it and the function stopped equalling its own EAE; argument-1
            // keeps the NARROW intake because §15.5.2 genuinely bounds the integer date form.
            "CombinedDatetime" => Dec(RuntimeApi.DateFn("CombinedDatetimeDec",
                $"{mode}, {IntArg(ic, 0)}, {DecArg(ic, 1)}{LeapSecondFlag}")),
            "SignOf" => new NumX(RuntimeApi.Intrinsic("SignDec", DecArg(ic, 0)), 0),
            "AbsScaled" => Dec(RuntimeApi.Intrinsic("AbsDec", DecArg(ic, 0))),
            "Floor" => Dec(RuntimeApi.Intrinsic("FloorDec", DecArg(ic, 0))),
            "Truncate" => Dec(RuntimeApi.Intrinsic("TruncDec", DecArg(ic, 0))),
            "FractionPart" => Dec(RuntimeApi.Intrinsic("FractionPartDec", $"{mode}, {DecArg(ic, 0)}")),
            "ModScaled" => Dec(RuntimeApi.Intrinsic("ModDec", $"{mode}, {DecArg(ic, 0)}, {DecArg(ic, 1)}")),
            "RemScaled" => Dec(RuntimeApi.Intrinsic("RemDec", $"{mode}, {DecArg(ic, 0)}, {DecArg(ic, 1)}")),
            "MaxScaled" => Dec(RuntimeApi.Intrinsic("MaxDec", DecArgList(ic))),
            "MinScaled" => Dec(RuntimeApi.Intrinsic("MinDec", DecArgList(ic))),
            "OrdMax" => new NumX(RuntimeApi.Intrinsic("OrdMaxDec", DecArgList(ic)), 0),
            "OrdMin" => new NumX(RuntimeApi.Intrinsic("OrdMinDec", DecArgList(ic)), 0),
            "SumScaled" => Dec(RuntimeApi.Intrinsic("SumDec", $"{mode}, {DecArgList(ic)}")),
            "RangeScaled" => Dec(RuntimeApi.Intrinsic("RangeDec", $"{mode}, {DecArgList(ic)}")),
            "MeanScaled" => Dec(RuntimeApi.Intrinsic("MeanDec", $"{mode}, {DecArgList(ic)}")),
            "MedianScaled" => Dec(RuntimeApi.Intrinsic("MedianDec", $"{mode}, {DecArgList(ic)}")),
            "MidrangeScaled" => Dec(RuntimeApi.Intrinsic("MidrangeDec", $"{mode}, {DecArgList(ic)}")),
            "Variance" => Dec(RuntimeApi.Intrinsic("VarianceDec", $"{mode}, {DecArgList(ic)}")),
            "StandardDeviation" => Dec(RuntimeApi.Intrinsic("StdDevDec", $"{mode}, {DecArgList(ic)}")),
            "Annuity" => Dec(RuntimeApi.Intrinsic("AnnuityDec", $"{mode}, {DecArg(ic, 0)}, {IntArg(ic, 1)}")),
            "PresentValue" => Dec(LeadThenTail(ic, "PresentValueDec", $"{mode}, ", "CobolDec", DecOf)),
            _ => null,
        };

        static NumX Dec(string expr) => new(expr, 0, Dec: true);
    }

    /// <summary>Does any argument render as FLOATING (binary64) rather than as a scaled integer? (PB2.)</summary>
    /// <remarks>Asked of the ARGUMENTS, not the receiver: a float argument into a fixed-point receiver still has
    /// to be computed in binary64 and only then quantized, which is exactly what <see cref="RenderFloat"/>'s
    /// <c>FromDouble</c> tail does.</remarks>
    /// <remarks>⛔ ASKED OF THE <b>LANDED</b> OPERAND, NOT THE RAW ONE (fix-queue PB38). Under a standard
    /// arithmetic mode <see cref="Landed"/> converts a float in per §8.8.1.5.1, so it is no longer floating and
    /// this dispatch must not route the call to the binary64 body. Reading the RAW operand here would reinstate
    /// the exact defect the landing exists to remove, with the landing silently doing nothing — the two must ask
    /// the same question of the same value.</remarks>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question — of the LANDED operand, deliberately (see the remark above).</remarks>
    private bool AnyRealArgument(BoundIntrinsicCall ic) =>
        ic.Args.Any(a => Landed(num.AsNum(a, num.Receiver)).Real);

    /// <summary>Does EVERY argument render as floating? (An all-float selection list stays in the float lane —
    /// each argument's content is its double; a mixed list rides the SDIDI, kb/Work PB65 RV-15.59.4-1 D2.)</summary>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question — of the LANDED operand, as its sibling does.</remarks>
    private bool AllRealArguments(BoundIntrinsicCall ic) =>
        ic.Args.All(a => Landed(num.AsNum(a, num.Receiver)).Real);

    /// <summary>A numeric argument as a C# double (the float family's §15.4.1 carrier). ⛔ Converts the RAW
    /// operand (fix-queue PB56): routing through the <see cref="Landed"/> unscaled truncation first turned a
    /// sub-working-scale Dec operand to ZERO before the double conversion — SQRT(4e-18) probed as 0 where the
    /// approximation of 2e-9 is required — and <see cref="NumericRenderer.Real"/> carries its own exact arm
    /// for every carrier (a Dec operand converts via <c>ToDouble</c>, a float passes through).</summary>
    /// <remarks>INTAKE(APPROXIMATED) — binary64 from the RAW carrier — the indexed form of <see cref="DblOf"/>.</remarks>
    private string Dbl(BoundIntrinsicCall ic, int i) => NumericRenderer.Real(RawArg(ic, i));

    /// <summary>An integer-kind argument as a C# <c>long</c> (truncated to scale 0 when the operand carries a
    /// fraction — integer arguments "shall be integers", §15.3; a fractional value is the program's EC latitude).</summary>
    /// <remarks>INTAKE(INTEGRAL) — rescaled to scale 0 with TRUNCATION — the §15.3 integer positions, where the argument is required to be an integer already.
    /// ⛔ FOR A <b>PARTIAL</b> FUNCTION ONLY — see <see cref="AsInt"/>, and use
    /// <see cref="IntArgWide"/> when the argument is total.</remarks>
    private string IntArg(BoundIntrinsicCall ic, int i) => AsInt(RawArg(ic, i));   // an integer lands at scale 0 directly — never through a working scale that eats headroom (kb/Work PB69)

    /// <summary>The WIDE twin of <see cref="IntArg"/> on the NUMERIC channel — the intake for a §15 integer
    /// argument that is <b>TOTAL</b>. <see cref="AsIntWide"/> states the rule and names the drift test that
    /// holds the renderer's intake to the runtime body's declared carrier.</summary>
    /// <remarks>INTAKE(INTEGRAL) — the §15.3 type-6 argument class, on the Int128 carrier — the same scale-0 truncation <see cref="IntArg"/> applies, on a carrier wide enough for a TOTAL argument.</remarks>
    private string IntArgWide(BoundIntrinsicCall ic, int i) => AsIntWide(RawArg(ic, i));

    /// <summary>
    /// ⛔ THE ONE NARROWING, AND IT RAISES RATHER THAN WRAPS (fix-queue PB22). This emitted a bare
    /// <c>(long)(…)</c> over an <c>Int128</c>-typed expression, and <c>RoslynBackend</c> sets no
    /// <c>checkOverflow</c> — so the cast wrapped MODULO 2⁶⁴, silently, and BEFORE the function's own range
    /// guard could see the value. <c>FUNCTION INTEGER-OF-DAY(P * 100 + 62)</c> with
    /// <c>P PIC 9(18) VALUE 184467440737115466</c> is 2⁶⁴ + 1995046, so §15.5.2's correct 1601..9999 / 1..366
    /// check received 1995046 and returned a plausible 143951 — from an argument nineteen orders of magnitude
    /// away, with no EC-ARGUMENT-FUNCTION even under enabled checking.
    /// <para>Both entries (<see cref="IntArg"/> for the numeric channel, <see cref="ArgInt"/> for the string one)
    /// funnel here, so ONE change covers seven renderer arms over eleven functions — which is the point: a value
    /// the receiving body cannot represent is an incorrect argument (§15.3), and the place to say so is where
    /// the narrowing happens, not eleven times downstream.</para>
    /// <para>⛔ IT IS THE LANDING FOR A <b>PARTIAL</b> FUNCTION AND ONLY FOR ONE (kb/Work PB254). PB22 swept
    /// EVERY integer arm into it, including arms whose function is TOTAL — §15.90/§15.91's date validators and
    /// FIND-STRING's argument-3 — and for those the raise is manufactured: their argument rules constrain
    /// nothing but integer-ness, so §15.3 has no "incorrect value … according to the rules specified in the
    /// function definition" to key on. A total argument takes <see cref="AsIntWide"/>, which states the rule
    /// and names the drift test that keeps the two in step.</para>
    /// <para>The <c>Real</c> arm takes the double-typed twin by NAME rather than by overload: an integer literal
    /// converts implicitly to both <c>Int128</c> and <c>double</c>, and an overload pair would turn
    /// <c>FUNCTION FACTORIAL(5)</c> into a CS0121 ambiguity — the collision that broke six corpus programs when
    /// the <c>…Real</c> bodies were first written.</para></summary>
    private static string AsInt(NumX a) => IntegerIntake(a, wide: false);

    /// <summary>
    /// ⛔ THE ONE §15.3 TYPE-6 INTAKE, TOTAL OVER EVERY CARRIER A <see cref="NumX"/> CAN BE, and the bounded
    /// (<see cref="AsInt"/>) and total (<see cref="AsIntWide"/>) arms are this method's two widths — they were two
    /// hand-copied four-arm dispatches, and the copies had drifted: the wide one had an unsigned-wide arm the
    /// narrow one lacked (<c>USAGE COMP-5</c> over a 19+-digit unsigned PICTURE at INTEGER-OF-DATE / CHAR /
    /// BASE-CONVERT reached Roslyn as a CS1503 — kb/Work PB638) and ignored the scale of such an operand; neither
    /// asked the integrality question (kb/Work PB1526), which <see cref="RuntimeApi.IntegerArgOf"/> now asks on
    /// the exact value on every carrier.
    /// <list type="bullet">
    ///   <item>the unsigned-wide lane funnels through <see cref="NumericRenderer.DeU"/> FIRST — the R10 Widen
    ///         funnel, loud past the Int128 intermediate — and then rides the ordinary scaled arm, scale included;</item>
    ///   <item>the Dec arm is BEFORE the scale test (kb/Work R24 — ledger F44): a Dec NumX carries Scale 0 BY
    ///         CONVENTION (the PB14/PB32 lesson), so the scale test would pass the CobolDec expression through raw
    ///         and hand Roslyn the CS1503 this arm exists to close; the float arm is the binary64 carrier.</item>
    /// </list>
    /// <c>IntrinsicCarrierAgreementDriftTests.EveryIntegerIntake_IsTotalOverTheNumXCarriers</c> derives the carrier
    /// list from <see cref="NumX"/>'s own flags and proves both widths accept every one.</summary>
    internal static string IntegerIntake(NumX a, bool wide)
    {
        var (carrier, value) = NumericRenderer.IntakeCarrier(a);
        return RuntimeApi.IntegerArgOf(carrier, value.Expr, value.Scale, wide);
    }

    /// <summary>The variadic arguments aligned to their common scale (ISO §8.8.1 — alignment makes unscaled
    /// comparison/arithmetic equal value comparison/arithmetic), as a C# argument list + that scale.</summary>
    /// <remarks>INTAKE(ALIGNED) — every argument rescaled UP to the argument list's common maximum scale on the <c>Int128</c> carrier — no digit is lost, but the list's headroom is.</remarks>
    private (string ArgList, int Scale) AlignedArgs(BoundIntrinsicCall ic)
    {
        var (argList, s, _) = AlignedArgsEx(ic);
        return (argList, s);
    }

    /// <summary>As <see cref="AlignedArgs"/>, also reporting whether any argument rendered FLOATING (Real) —
    /// the MEAN standard-arithmetic branch keys on it (a float statistics argument keeps the pre-existing
    /// native rendering).</summary>
    /// <remarks>INTAKE(ALIGNED) — as <see cref="AlignedArgs"/> — same contract.</remarks>
    private (string ArgList, int Scale, bool AnyReal) AlignedArgsEx(BoundIntrinsicCall ic)
    {
        // A table(ALL) operand renders as its ELEMENT here (the index variable inside) — right for the scale and
        // the Real flag; the list itself enumerates through ArgArray (kb/Work PB62).
        var xs = ic.Args.Select(a => num.AsNum(a, num.Receiver)).ToList();
        int s = xs.Count == 0 ? 0 : xs.Max(x => x.Scale);
        string list = ArgArray(ic, 0, "Int128", a => NumericRenderer.Align(num.AsNum(a, num.Receiver), s))
                      ?? string.Join(", ", xs.Select(x => NumericRenderer.Align(x, s)));
        return (list, s, xs.Any(x => x.Real));
    }

    /// <summary>The variadic arguments UNALIGNED — parallel value/scale arrays for the selection family
    /// (fix-queue PB65). Each argument renders at its OWN scale (<c>Align(x, x.Scale)</c> is the identity for
    /// an exact operand and the standard carrier conversion for Real/Dec/U), so no widening ever happens on
    /// intake; the common scale is reported for the result's compile-time <see cref="NumX"/>.</summary>
    /// <remarks>INTAKE(EXACT) — each argument at its OWN scale with that scale carried beside it — the selection family's intake, where no common alignment may be imposed (kb/Work PB65).</remarks>
    private (string Vals, string Scales, int Scale) RawArgPairs(BoundIntrinsicCall ic)
    {
        var xs = ic.Args.Select(a => num.AsNum(a, num.Receiver)).ToList();
        int s = xs.Count == 0 ? 0 : xs.Max(x => x.Scale);
        // A table(ALL) argument enumerates (kb/Work PB62): its elements share one scale, so the scales array
        // enumerates the same ranges with a constant selector.
        string vals = ArgArray(ic, 0, "Int128", a => { var x = num.AsNum(a, num.Receiver); return NumericRenderer.Align(x, x.Scale); })
                      ?? $"new Int128[] {{ {string.Join(", ", xs.Select(x => NumericRenderer.Align(x, x.Scale)))} }}";
        string scales = ArgArray(ic, 0, "int", a => num.AsNum(a, num.Receiver).Scale.ToString())
                        ?? $"new int[] {{ {string.Join(", ", xs.Select(x => x.Scale))} }}";
        return (vals, scales, s);
    }

    /// <summary>The variadic string-argument list. <paramref name="admitNumeric"/> defaults FALSE and is passed
    /// true by exactly one caller (CONCAT — §15.18.3 r1 lists class numeric): four arms with three different
    /// §15.x.3 argument rules share this helper, so the admission is a parameter, never a global flip
    /// (§15.26.3 r1 / §15.66.3 r1 exclude class numeric outright — PB59).</summary>
    private string StrArgList(BoundIntrinsicCall ic, bool admitNumeric = false) =>
        ArgArray(ic, 0, "string", a => admitNumeric ? StrNum(a) : Str(a))   // a table(ALL) enumerates (kb/Work PB62)
        ?? string.Join(", ", ic.Args.Select(a => admitNumeric ? StrNum(a) : Str(a)));

    private string CommaFlag => ctx.Data.DecimalPointIsComma ? ", commaMode: true" : "";

    /// <summary>The ANYCASE named argument (§15.68.3 r4f / §15.37.4 r4 — the ONE <see cref="BoundIntrinsicCall.Anycase"/>
    /// flag), rendered only when present so the default-free call stays byte-stable.</summary>
    private static string AnycaseFlag(BoundIntrinsicCall ic) => ic.Anycase ? ", anycase: true" : "";

    /// <summary>⛔ THE CLASSIFICATION AN ANYCASE FOLD READS (§15.57.4 r3; kb/Work PB2631). ANYCASE is DEFINED as "the rules
    /// for the LOWER-CASE function without the LOCALE argument" (§15.37.4 r4, §15.87.4 r5, §15.68.3 r4f), and that
    /// function maps case through the module's CHARACTER CLASSIFICATION when it has one - so the matchers receive the
    /// same selector the UPPER-CASE / LOWER-CASE arms do (<see cref="ObjectComputerEmit.ClassificationExpr"/>, keyed on
    /// argument-1's class), only when the module HAS a classification and an ANYCASE comparison is in play, which keeps
    /// every other emission byte-stable.</summary>
    private string AnycaseClassification(BoundIntrinsicCall ic) =>
        ctx.Data.Classification is not null
        && (ic.Anycase || (ic.SubstituteModes?.Any(m => (m & 4) != 0) ?? false))
            ? $", classification: {ObjectComputerEmit.ClassificationExpr(ic.Args[0])}" : "";

    /// <summary>The §15.93.4/§15.94.4 r1b digit-cap named argument, DERIVED FROM THE MODE
    /// (<see cref="ArithmeticModes.NumvalDigitCap"/> — the one table, keyed on the mode the standard keys its
    /// three sub-notes on). Omitted when the cap is the runtime's own default, so the generated call stays
    /// byte-stable for every native compilation.
    ///
    /// <para>This was a two-state ternary on <c>num.StandardDecimal</c>, which gave STANDARD-BINARY the NATIVE
    /// cap through its else-branch. Unreachable today — the mode is declined at bind — but a lane that answers
    /// a question it was never asked is the half of a double defence that rots first.</para></summary>
    private string DigitCapFlag =>
        ArithmeticModes.NumvalDigitCap(ctx.Data.Options.Arithmetic) is var cap && cap != ArithmeticModes.DefaultDigitCap
            ? $", digitCap: {cap}" : "";

    /// <summary>The landing form past the Int128 carrier for a quantizer / exact-family parse (kb/Work PB77) — the
    /// ONE <c>NumericRenderer.CheckedFlag</c>, read from the receiver context this render runs under.</summary>
    private string CheckedFlag => num.CheckedFlag;

    /// <summary>The trailing weights argument for a PCS-flagged CHAR/ORD/CHAR-NATIONAL (hazard H5: the binder
    /// set <see cref="BoundIntrinsicCall.Collate"/>/<see cref="BoundIntrinsicCall.CollateNat"/> ONLY when the
    /// matching non-identity PCS exists — exactly when the program class emitted its <c>__COLLATE</c> /
    /// <c>__COLLATE_NAT</c> table; the two never coexist on one call — CHAR/alphanumeric-ORD read the
    /// alphanumeric sequence, CHAR-NATIONAL/national-ORD the national one, §15.15.4/§15.16.4/§15.70.4).</summary>
    private static string Collate(BoundIntrinsicCall ic) =>
        ic.Collate ? ", __COLLATE" : ic.CollateNat ? ", __COLLATE_NAT" : "";

    /// <summary>The LEADING weights argument for a PCS-flagged MAX/MIN/ORD-MAX/ORD-MIN string form — collate goes
    /// FIRST (a <c>params string[]</c> can take no trailing param), selecting the <c>MaxString(ushort[]|NationalCollation,
    /// params string[])</c> overload. The mirror of <see cref="Collate"/>'s trailing form for the single-arg CHAR/ORD.</summary>
    private static string CollatePrefix(BoundIntrinsicCall ic) =>
        // The MAX/MIN family takes the program's CobolCollation carrier, exactly as CHAR/ORD do (PB101 — the
        // ushort[] raw-table overload is gone; the carrier's Compare is the order-equivalent tail, PB59).
        ic.Collate ? "__COLLATE, " : ic.CollateNat ? "__COLLATE_NAT, " : "";

    // ── The STRING channel (instance — reached through OperandText.AsString with the per-unit renderer) ─────

    /// <summary>Render an alphanumeric-result intrinsic as a C# string expression. An INSTANCE channel (P7
    /// Step 12 — the context-free static twin is deleted): numeric-kind arguments render through the ONE
    /// <see cref="NumericRenderer"/> under <see cref="ReceiverContext.None"/> (a string-channel call has no
    /// numeric receiver), so division, float items, nested numeric intrinsics, and numeric-edited de-edits
    /// all render where the static channel stayed loud (H3 closed).</summary>
    /// <summary>
    /// Reference-modify the function RESULT when the reference carried one (ISO §8.4.3.3.3 SR2, fix-queue PB8),
    /// through the SAME <c>CobolString.RefMod</c> — and therefore the same §8.4.3.3.4 item-5c bounds check and
    /// EC-BOUND-REF-MOD raise — that a reference-modified data item uses. No second slicer exists.
    /// <para>⛔ THIS IS THE ONLY EMIT SITE THAT HAS TO HONOUR IT, and that is provable rather than surveyed: SR2
    /// admits a ref-mod only on an alphanumeric, boolean or national function, and every such result renders
    /// through this one method (reached from <c>OperandText.AsString</c> and from the nested-argument visitor).
    /// A numeric-result call cannot carry one — the binder rejects it with COBOLNET1629 — so the numeric and
    /// folded channels need no arm. Wrapping HERE rather than at each caller is what keeps that true.</para>
    /// </summary>
    public string RenderString(BoundIntrinsicCall ic)
    {
        string value = RenderStringValue(ic);
        return ic.RefMod is { } rm ? RuntimeApi.StrRefMod(value, rm) : value;
    }

    private string RenderStringValue(BoundIntrinsicCall ic)
    {
        var sig = ic.Sig;
        if (sig.Bind == IntrinsicBind.Deferred || sig.RuntimeMethod.Length == 0)
            return EmitText.LoudValue("string", $"FUNCTION {sig.Name} (catalogued, not yet implemented)");
        return sig.RuntimeMethod switch
        {
            // UPPER-CASE / LOWER-CASE (§15.97 / §15.57; kb/Work PB64 T5): with a LOCALE phrase, the named locale's LC_CTYPE
            // (r2); else, when this module has a CHARACTER CLASSIFICATION, the classification's LC_CTYPE for the
            // operand's class (r3 — __CLASSIFY, resolved at activation); else the implementor's correspondence (r4 —
            // the invariant map the function always used).
            "UpperCase" or "LowerCase" when ic.Locale.Tag is { } caseTag =>
                RuntimeApi.LocaleFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {EmitText.CsLiteral(caseTag)}"),
            "UpperCase" or "LowerCase" when ctx.Data.Classification is not null =>
                RuntimeApi.LocaleFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}{ObjectComputerEmit.ClassificationArg(ic.Args[0])}"),
            "UpperCase" or "LowerCase" or "Reverse" =>                         // §15.97/57/78 — length-preserving
                RuntimeApi.Intrinsic(sig.RuntimeMethod, Str(ic.Args[0])),
            "Char" =>                                                          // §15.15 — PCS-relative (H5 conditional weights)
                RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{ArgInt(ic.Args[0])}{Collate(ic)}"),
            "CharNational" =>                                                  // §15.16 — the NATIONAL PCS ordinal (native UTF-16 order,
                RuntimeApi.Intrinsic(sig.RuntimeMethod,                        //   or __COLLATE_NAT under a non-native ALPHABET … FOR NATIONAL)
                    $"{ArgInt(ic.Args[0])}{Collate(ic)}"),
            "CurrentDate" => RuntimeApi.DateFn(sig.RuntimeMethod, ""),         // §15.21 — the runtime clock
            // WHEN-COMPILED is the COMPILATION timestamp (§15.99.3 r2) — a constant in the generated source,
            // captured ONCE PER COMPILATION on ProgramEmitter (kb/Work PB120), not per process. (The legacy's
            // runtime-clock placeholder also passes IF142A's plausibility checks; the constant is the
            // spec-correct form — scout brief §4.4.)
            "WhenCompiled" => EmitText.CsLiteral(ctx.WhenCompiledStamp),
            "MaxString" or "MinString" =>                                      // §15.59/63 all-string form (PCS via CollatePrefix, CA23)
                RuntimeApi.Intrinsic(sig.RuntimeMethod, CollatePrefix(ic) + StrArgList(ic)),
            "Concat" =>                                                        // §15.18 — concatenate all argument images (2023);
                RuntimeApi.Intrinsic(sig.RuntimeMethod,                        //   r1 admits class NUMERIC → the admitting list (PB59)
                    StrArgList(ic, admitNumeric: true)),
            // BOOLEAN-OF-INTEGER (§15.13.4 r1) — a boolean-result function on the D-B1 '0'/'1' substrate:
            // rightmost position = low-order digit, left zero-fill/truncate to argument-2 positions.
            // Argument-1 crosses the WIDE bridge (Int128 — §15.13.3 r1 admits any positive integer, PB65);
            // argument-2 stays on the long bridge (§15.4 caps the returned length at CobolIntrinsics.ReturnedValueMaximum).
            "BooleanOfInteger" =>
                RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{ArgIntWide(ic.Args[0])}, {ArgInt(ic.Args[1])}"),
            // DISPLAY-OF (§15.26) / NATIONAL-OF (§15.66) — the national↔alphanumeric repertoire pair (2002);
            // the optional argument-2 is the one-character substitution string (§15.26.3 r2 / §15.66.3 r2).
            "DisplayOf" or "NationalOf" =>
                RuntimeApi.Intrinsic(sig.RuntimeMethod, StrArgList(ic)),
            "BaseConvert" =>                                                   // §15.12 — unsigned-integer base conversion (2023);
                RuntimeApi.Intrinsic(sig.RuntimeMethod,                        //   r1 admits an unsigned integer LITERAL argument-1 below
                    $"{StrNum(ic.Args[0])}, {ArgInt(ic.Args[1])}, {ArgInt(ic.Args[2])}"),   // base 11 (PB59); args 2/3 stay integers
            "Trim" =>                                                          // §15.96 — delete leading/trailing/both of the char set (default: space)
                ic.Args.Any(a => a is BoundFieldOperand { Place: TableAllPlace })   //   a table(ALL) argument-2 list enumerates (kb/Work PB62)
                    ? LeadThenTail(ic, sig.RuntimeMethod, "", "string", Str, mid: $"{ic.TrimMode}, ")
                    : RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ic.TrimMode}"
                        + string.Concat(ic.Args.Skip(1).Select(a => $", {Str(a)}"))),
            // STANDARD-COMPARE (§15.85) — the cultural-ordering comparison over the derived CLDR/UCA engine
            // (kb/Work PB101 T7). The ordering TABLE travels as the bind-time-resolved literal-9 (null ⇒ §15.85.3
            // r5's default table); the ordering LEVEL travels as 0 when argument-4 is omitted, which §15.85.4 r1
            // defines as "the highest level defined in the ordering table". Both are complete on the bound node,
            // so the backend never consults the SPECIAL-NAMES model.
            "StandardCompare" =>
                RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, "
                    + $"{(ic.OrderingTable is { } ot ? EmitText.CsLiteral(ot) : "null")}, "
                    + $"{(ic.Args.Count > 2 ? ArgInt(ic.Args[2]) : "null")}"),   // null = argument-4 unspecified (§15.85.4 r1); a specified 0 is §15.85.3 r6's violation
            // The LOCALE functions (§15.51–§15.54; kb/Work PB64 T4): the bound LocaleRef travels as the named locale's
            // L1-normalized tag (null ⇒ the locale current for the category at use — §14.6.6 r7/r8); the runtime
            // resolves availability (EC-LOCALE-MISSING) and content (EC-LOCALE-INVALID) at use.
            "Compare" when sig.Name == "LOCALE-COMPARE" =>
                RuntimeApi.LocaleFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {LocaleTagArg(ic)}"),
            "Date" when sig.Name == "LOCALE-DATE" =>
                RuntimeApi.LocaleFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {LocaleTagArg(ic)}"),
            "Time" when sig.Name == "LOCALE-TIME" =>
                RuntimeApi.LocaleFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {LocaleTagArg(ic)}"),
            "TimeFromSeconds" => RenderLocaleTimeFromSeconds(ic),               // §15.54 — standard numeric time form per t_fmt
            "Substitute" => RenderSubstitute(ic),                              // §15.87 — replace argument-2 pairs (2023)
            "Convert" =>                                                       // §15.19 — repertoire / hex / byte conversion (2023);
                RuntimeApi.Intrinsic(sig.RuntimeMethod,                        //   an ANY source takes the RAW STORAGE image (r7, PB59 5b)
                    $"{(ic.ConvertSource == 0 ? StorageArg(ic.Args[0]) : Str(ic.Args[0]))}, "
                    + $"{ic.ConvertSource}, {ic.ConvertDest}, {(ic.ConvertDestHex ? "true" : "false")}"),
            "ModuleName" =>                                                    // §15.65 — the runtime module call-name stack (2023)
                RuntimeApi.ModuleNameFn(ic.ModuleNameKind),
            "FormattedDate" =>                                                 // §15.39 — integer date per a date format (2014)
                RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ArgInt(ic.Args[1])}"),
            "FormattedTime" => RenderFormattedTime(ic),                        // §15.41
            "FormattedDatetime" => RenderFormattedDatetime(ic),               // §15.40
            "FormattedCurrentDate" =>                                          // §15.38 — the runtime clock per a combined format
                RuntimeApi.DateFn(sig.RuntimeMethod, Str(ic.Args[0])),
            // The last-exception interrogation functions (§15.28–15.33 — the EC model, incl. the national
            // twins EXCEPTION-FILE-N/EXCEPTION-LOCATION-N §15.29/§15.31): zero-argument reads of the runtime
            // register; the binder's EcNoteFunction flagged the group EC gate, so the generated source
            // carries the CobolNet.Runtime.Exceptions using.
            "EcStatus" => RuntimeApi.EcFn("Status"),                           // §15.33
            "EcLocation" => RuntimeApi.EcFn("Location"),                       // §15.30
            "EcLocationN" => RuntimeApi.EcFn("LocationN"),                     // §15.31 — the national twin
            "EcStatement" => RuntimeApi.EcFn("Statement"),                     // §15.32
            // §15.28.4 / §15.29.4: the no-argument form (r1) reads the last-exception register; the 2023
            // file-connector-argument form (r2) reads the NAMED connector — the SAME FileKeyExpr the file verbs use.
            "EcFile" => ic.FileArg is { } eff
                ? RuntimeApi.EcFn("File", EmitText.FileKeyExpr(eff))          // §15.28.4 r2 — the named connector's status
                : RuntimeApi.EcFn("File"),                                    // §15.28.4 r1 — the no-argument form
            "EcFileN" => ic.FileArg is { } effn
                ? RuntimeApi.EcFn("FileN", EmitText.FileKeyExpr(effn))        // §15.29.4 r2 — the national twin
                : RuntimeApi.EcFn("FileN"),                                   // §15.29.4 r1 — the no-argument form
            _ => EmitText.LoudValue("string", $"FUNCTION {sig.Name} in a string context"),
        };
    }

    /// <summary>FORMATTED-TIME (§15.41): seconds as (unscaled, scale) via <see cref="SecondsArg"/> — NOT the
    /// truncating ArgInt, so the fractional seconds survive — plus the optional offset-minutes (a3).</summary>
    private string RenderFormattedTime(BoundIntrinsicCall ic)
    {
        var (secExpr, secScale) = SecondsArg(ArgNum(ic.Args[1]));
        return RuntimeApi.DateFn(ic.Sig.RuntimeMethod, $"{Str(ic.Args[0])}, {secExpr}, {secScale}, "
             + $"{OffsetMinutesArg(ic, 2)}{LeapSecondFlag}");
    }

    /// <summary>The bound <see cref="BoundIntrinsicCall.Locale"/> as the runtime's <c>localeTag</c> argument: the named
    /// locale's L1-normalized tag literal, or <c>null</c> for the current-locale form.</summary>
    private static string LocaleTagArg(BoundIntrinsicCall ic) =>
        ic.Locale.Tag is { } tag ? EmitText.CsLiteral(tag) : "null";

    /// <summary>LOCALE-TIME-FROM-SECONDS (§15.54): the seconds argument through the ONE <see cref="SecondsArg"/> pair
    /// (fraction-preserving — the nanosecond note of Annex D.31.4.5), the locale tag, the LEAP-SECOND flag.</summary>
    private string RenderLocaleTimeFromSeconds(BoundIntrinsicCall ic)
    {
        var (secExpr, secScale) = SecondsArg(ArgNum(ic.Args[0]));
        return RuntimeApi.LocaleFn(ic.Sig.RuntimeMethod, $"{secExpr}, {secScale}, {LocaleTagArg(ic)}{LeapSecondFlag}");
    }

    /// <summary>FORMATTED-DATETIME (§15.40): integer date a2 + seconds a3 (via <see cref="SecondsArg"/>) + the
    /// optional offset-minutes a4.</summary>
    private string RenderFormattedDatetime(BoundIntrinsicCall ic)
    {
        var (secExpr, secScale) = SecondsArg(ArgNum(ic.Args[2]));
        return RuntimeApi.DateFn(ic.Sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ArgInt(ic.Args[1])}, "
             + $"{secExpr}, {secScale}, {OffsetMinutesArg(ic, 3)}{LeapSecondFlag}");
    }

    /// <summary>The UTC offset in minutes of FORMATTED-TIME (argument-3) and FORMATTED-DATETIME (argument-4). An
    /// omitted offset IS 0: §15.41.3 r6 / §15.40.3 r7 evaluate the function "as though 0 were specified", so the
    /// runtime gets one value on one path whether the program wrote it or not (kb/Work PB2629: a separate "has
    /// offset" flag skipped the UTC adjustment for the omitted form only, and the two forms disagreed on the leap
    /// second).</summary>
    private string OffsetMinutesArg(BoundIntrinsicCall ic, int index) =>
        ic.Args.Count > index ? ArgInt(ic.Args[index]) : "0";

    /// <summary>The <c>&gt;&gt;LEAP-SECOND ON</c> argument every §15.3 date/time function that reads a seconds subfield or
    /// a standard numeric time form takes (ISO §7.3.17 / §15.3.3.3 — kb/Work PB65): the compilation group's ONE
    /// directive fact (<c>DataBinder.LeapSecond</c>), passed as a trailing named argument only when ON, so every
    /// OFF emission is byte-identical to before (the CommaFlag discipline).</summary>
    private string LeapSecondFlag => ctx.Data.LeapSecond ? ", leapSecond: true" : "";

    /// <summary>The SECONDS argument of the formatted time family as an (unscaled, scale) pair, TOTAL over the
    /// four value carriers (kb/Work R24 — ledger F44/F46/F57; §15.41.4 r1 / §15.40.4 make the returned value a
    /// representation of the VALUE "contained in" the argument, fraction included):
    /// <list type="bullet">
    ///   <item>a FIXED-POINT operand passes through at its own scale with NO narrowing cast — the former
    ///         <c>(long)(expr)</c> silently WRAPPED a wide picture's unscaled form (a 9(5)V9(15) item holding
    ///         the in-range 45296.5 fabricated 02:20:03), and the runtime now takes <see cref="Int128"/>;</item>
    ///   <item>a FLOAT operand lands through the checked <c>CobolFloat.ToScaled</c> at scale 9 — the
    ///         FORMATTED-CURRENT-DATE nanosecond convention, and every reliable binary64 fraction digit under a
    ///         5-integer-digit bound — where the former cast truncated to WHOLE seconds;</item>
    ///   <item>an SDIDI operand (a legal §15.3 type-10 expression under a standard mode) lands exactly at scale
    ///         18, the documented §15.3.3.2 maximum fraction width (CONFORMANCE.md item 202) — it used to reach
    ///         the cast as a raw CS1503, the PB2 shape on the Dec axis;</item>
    ///   <item>an unsigned-wide operand narrows through the R10 Widen funnel (a valid time always fits).</item>
    /// </list></summary>
    private static (string Expr, int Scale) SecondsArg(NumX x)
    {
        x = NumericRenderer.DeU(x);
        // ⛔ BOTH LANDINGS ROUND TOWARD NEGATIVE INFINITY AND NEVER RAISE (kb/Work PB1379): the consumers screen the
        // landed value against §7.3.17.4's [0, 86 400) / [0, 86 401), and a landing that truncated a tiny negative to
        // zero (or raised a size error for a huge one) answered that screen before it could. CobolDate.SecondsOfReal
        // / SecondsOfDec preserve membership in the form; the fixed-point arm below is exact at its own scale.
        if (x.Real) return (RuntimeApi.SecondsLanding(x.Expr, dec: false), 9);
        if (x.Dec) return (RuntimeApi.SecondsLanding(x.Expr, dec: true), 18);
        return (x.Expr, x.Scale);
    }

    /// <summary>SUBSTITUTE (§15.87): the source (Args[0]) plus parallel from/to/mode arrays over the pair operands
    /// (Args[1..] taken two at a time; one <see cref="BoundIntrinsicCall.SubstituteModes"/> entry per pair).</summary>
    private string RenderSubstitute(BoundIntrinsicCall ic)
    {
        // The FLAT form (kb/Work PB81 — a table(ALL) among the pairs): every part after argument-1 is ONE string[]
        // (a written operand's singleton, or the enumeration), paired at run time by SubstituteFlat with the
        // per-part flags.
        if (ic.SubstituteFlat)
        {
            var parts = ic.Args.Skip(1).Select(a => a is BoundFieldOperand { Place: TableAllPlace all }
                ? AllArgsExpr(all, "string", Str)
                : $"new string[] {{ {Str(a)} }}");
            return RuntimeApi.Intrinsic("SubstituteFlat", $"{Str(ic.Args[0])}, "
                + $"new string[][] {{ {string.Join(", ", parts)} }}, "
                + $"new int[] {{ {string.Join(", ", ic.SubstituteModes ?? [])} }}{AnycaseClassification(ic)}");
        }
        var froms = new List<string>();
        var tos = new List<string>();
        for (int i = 1; i + 1 < ic.Args.Count; i += 2)
        {
            froms.Add(Str(ic.Args[i]));
            tos.Add(Str(ic.Args[i + 1]));
        }
        return RuntimeApi.Intrinsic(ic.Sig.RuntimeMethod, $"{Str(ic.Args[0])}, "
            + $"new string[] {{ {string.Join(", ", froms)} }}, "
            + $"new string[] {{ {string.Join(", ", tos)} }}, "
            + $"new int[] {{ {string.Join(", ", ic.SubstituteModes ?? [])} }}{AnycaseClassification(ic)}");
    }

    // ── String-channel argument rendering (P7 Step 12 — the ONE NumericRenderer under the default receiver) ──

    /// <summary>A numeric-kind argument inside the STRING channel, rendered by the ONE
    /// <see cref="NumericRenderer"/> under <see cref="ReceiverContext.None"/> — a string-channel render has no
    /// numeric receiver, exactly the Step-3 default-receiver convention (DESIGN-codegen-backend §2.5). The
    /// renderer's public entries save/restore the ambient receiver, so this re-entrant call cannot go stale.</summary>
    /// <remarks>INTAKE(EXACT) — the STRING channel's numeric intake, under <see cref="ReceiverContext.None"/> — a text context has no numeric receiver to derive a landing from.</remarks>
    private NumX ArgNum(BoundOperand op) => num.AsNum(op, ReceiverContext.None);

    /// <summary>An integer-kind argument inside the STRING channel (CHAR / BASE-CONVERT / the formatted
    /// date-time offsets) — the same (long) truncation the numeric channel's <see cref="IntArg"/> applies.</summary>
    /// <remarks>INTAKE(INTEGRAL) — the STRING channel's integer intake — the same scale-0 truncation <see cref="IntArg"/> applies.</remarks>
    private string ArgInt(BoundOperand op) => AsInt(ArgNum(op));

    /// <summary>The WIDE integer-argument bridge on the STRING channel — the Int128-carrier twin of
    /// <see cref="ArgInt"/>. <see cref="AsIntWide"/> carries the rule.</summary>
    /// <remarks>INTAKE(INTEGRAL) — the STRING channel's WIDE integer intake — scale-0 truncation on the <c>Int128</c> carrier.</remarks>
    private string ArgIntWide(BoundOperand op) => AsIntWide(ArgNum(op));

    /// <summary>
    /// ⛔ THE INTAKE FOR A <b>TOTAL</b> §15 INTEGER ARGUMENT — no narrowing, therefore no raise point.
    /// </summary>
    /// <remarks>
    /// <para>THE RULE, and it is a property of the ARGUMENT, never of the arm that happens to render it: a §15
    /// integer argument whose function definition places <b>no constraint on its VALUE</b> is total — the
    /// returned-value rule answers for every integer — so §15.3's closing paragraph has nothing to fire on
    /// ("the evaluation of an argument results in an incorrect value for that argument or for the returned
    /// value <i>according to the rules specified in the function definition</i>"), and putting
    /// <see cref="AsInt"/>'s §15.3 landing in front of it manufactures an exception condition the standard
    /// does not define. Members today:</para>
    /// <list type="bullet">
    /// <item>BOOLEAN-OF-INTEGER argument-1 — §15.13.3 r1 constrains the SIGN only; §15.13.4 r1's bit
    /// configuration is mathematical, so 2⁶³ and up are legal (fix-queue PB65 / RV-15.13.4-1 D1).</item>
    /// <item>TEST-DATE-YYYYMMDD / TEST-DAY-YYYYDDD argument-1 — §15.90.3 r1 / §15.91.3 r1 say only "shall be
    /// an integer" and r1a is a CATCH-ALL (kb/Work PB254 / RV-15.90.4-1 / RV-15.91.4-1).</item>
    /// <item>FIND-STRING argument-3 — §15.37.3 r3 places no value constraint; §15.37.4 r2/r3 answer for every
    /// integer (kb/Work PB254).</item>
    /// <item>RANDOM argument-1 — §15.75.3 r2 constrains the SIGN only ("it shall be zero or a positive
    /// integer") and §15.75.4 r3 makes the distinct-sequence subset a FLOOR ("shall include the values from 0
    /// through at least 32767"), not a domain, so a wider seed is legal and its sequence is defined
    /// (kb/Work PB636 — the narrow intake aborted the run unit on a 19-digit seed).</item>
    /// </list>
    /// <para>⚠ THE SET IS NOT MAINTAINED HERE. The runtime body's declared parameter carrier IS the totality
    /// claim — <c>Int128</c> for a total argument, <c>long</c> for one the argument rules bound — and
    /// <c>IntrinsicCarrierAgreementDriftTests.EveryTotalIntegerArgument_TakesTheWideIntake</c> re-derives the
    /// pairing from the runtime signatures and fails when an arm and its body disagree. Widening a body is
    /// therefore enough; forgetting the arm is red, not silent.</para>
    /// <para>Every legal fixed-point value rides Int128 exactly, so a scale-0 operand passes through RAW; the
    /// carrier arms mirror <see cref="AsInt"/> exactly (Dec BEFORE the scale test — a Dec NumX carries Scale 0
    /// by convention, the PB14/PB32 lesson; the unsigned-wide lane narrows through the R10
    /// <c>CobolNum.Widen</c> funnel, loud past the intermediate).</para>
    /// </remarks>
    private static string AsIntWide(NumX a) => IntegerIntake(a, wide: true);

    /// <summary>A string-kind argument (the §15.3 alphanumeric-argument shapes): literals, field display
    /// images, and nested alphanumeric intrinsics. A numeric-category operand in a string-argument position
    /// stays loud — §15's string functions take alphanumeric/national/boolean arguments (the named channel).
    /// ⛔ THE DEFAULT IS NON-ADMITTING AND MUST STAY SO: NUMVAL/NUMVAL-F carry open rows (AR-15.67.3-1 /
    /// AR-15.69.3-1) DEMANDING a compile-time rejection of a numeric literal — widening this default would
    /// turn their wrong-stage defect into a silently-wrong one.</summary>
    private string Str(BoundOperand op) => op.Accept(_strArg ??= new StrArgVisitor(this, admitNumeric: false));
    private StrArgVisitor? _strArg;

    /// <summary>CONVERT's ANY-source argument — the RAW STORAGE byte image (§15.19.3 r7: any usage, contents
    /// need not be valid for it — the storage BITS, never the display image; PB59 family 5b). A FIELD renders
    /// through the ONE storage channel (<see cref="OperandText.AsStorageImage"/>); a literal renders its
    /// declared storage (an N"…" literal its UTF-16BE bytes, a B"…" literal its packed bits, an alphanumeric
    /// literal its characters under the item-209 1-byte serialization). Every other shape (computed values,
    /// figuratives, ALL literals) HAS no storage — those keep the display-image channel they always rode, which
    /// for a numeric shape stays the deliberately-loud <see cref="Str"/> default.</summary>
    private string StorageArg(BoundOperand op) => op switch
    {
        // ⚠ transfer: true — CONVERT only READS the storage image (kb/Work PB1901, the Visit(BoundFieldOperand) twin):
        // a pointer-leaf strongly-typed group's image is its D-SLOT placeholder, the one image every reader gets.
        BoundFieldOperand f => OperandText.AsStorageImage(f.Place, transfer: true),
        BoundStringLiteral { Category: PicCategory.National } sl => RuntimeApi.NatBytes(EmitText.CsLiteral(sl.Value)),
        BoundStringLiteral { Category: PicCategory.Boolean } sl =>
            RuntimeApi.BitsPack(EmitText.CsLiteral(sl.Value), sl.Value.Length.ToString()),
        _ => Str(op),
    };

    /// <summary>The NUMERIC-ADMITTING string-argument entry (fix-queue PB59 / RV-15.12.4-1, RV-15.18.4-1) —
    /// for EXACTLY the functions whose §15.x.3 argument rules admit a numeric literal: BASECONVERT argument-1
    /// (§15.12.3 r1 — "a usage display or national data item or literal, and, if the base … is less than 11,
    /// … an unsigned integer data item or literal") and CONCAT (§15.18.3 r1 — class numeric is in its list).
    /// The literal renders through the ONE <c>OperandText.AsString</c> image (raw source text — correct for
    /// §15.18.4 r1; r3's value conditions are NOT the renderer's to enforce and stay recorded on the
    /// DeliberatelyUnscreened rows). Never a blanket route — the drift test pins the caller set.</summary>
    private string StrNum(BoundOperand op) => op.Accept(_strArgNum ??= new StrArgVisitor(this, admitNumeric: true));
    private StrArgVisitor? _strArgNum;

    /// <summary>The string-argument dispatch — an exhaustive generated-visitor implementation (a new
    /// <see cref="BoundOperand"/> leaf is a compile error), INSTANCE-bound so the nested-intrinsic arm
    /// re-enters the instance <see cref="RenderString"/>. ONE class, two cached flagged instances (the
    /// OperandText pattern): <paramref name="admitNumeric"/>'s ONLY difference is the
    /// <see cref="BoundNumericLiteral"/> arm — a second visitor CLASS would silently join the drift test's
    /// end-of-file body slice.</summary>
    private sealed class StrArgVisitor(IntrinsicRenderer owner, bool admitNumeric) : IBoundOperandVisitor<string>
    {
        private static string Loud(BoundOperand n) => EmitText.LoudValue("string", $"intrinsic string argument '{n.GetType().Name}'");
        public string Visit(BoundStringLiteral n) => EmitText.CsLiteral(n.Value);
        // An intrinsic function READS its argument's characters and never reads them back, so a strongly-typed group
        // holding a pointer/object leaf (§15.3 type 2: "treated as though they were of class and category
        // alphanumeric") takes the one-way transfer image, not the two-way Tier-C refusal (kb/Work PB1901).
        public string Visit(BoundFieldOperand n) =>
            OperandText.AsTransferString(n, owner.Num, "intrinsic function argument");
        // THE CURRENT RECORD (kb/Work PB339) is built only for a READ/RETURN INTO implicit MOVE, so no intrinsic
        // argument list can hold one today — but its image is alphanumeric and already written down once, so this
        // DELEGATES rather than going loud (the same delegation the field arm above makes; a loud arm here would
        // be the wrong-stage failure PB25 fixed for figuratives).
        public string Visit(BoundCurrentRecord n) => OperandText.AsString(n, owner.Num);
        public string Visit(BoundComputedOperand n) =>
            n.Expr is BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } nested
                ? owner.RenderString(nested)   // string-class results incl. national (§15.66) + boolean (§15.13 — the '0'/'1' substrate)
            // ⛔ A NUMERIC FUNCTION'S RETURNED VALUE AT A NUMERIC-ADMITTING POSITION (kb/Work PB1420). CONCAT §15.18.3 r1
            // lists class numeric and BASECONVERT §15.12.3 r1 a numeric argument-1 below base 11, and §8.4.3.2.3 SR12
            // exempts "the integer form of the ABS function" from the bar on integer functions at an unsigned-integer
            // position — so `FUNCTION CONCAT(FUNCTION ABS(3) "A")` is LEGAL and used to compile clean and abort at run
            // time on this arm (the wrong-stage family). Its text is the ONE numeric-function image,
            // OperandText.AsString's (DOC-A.1-92: the significant digits, no padding), exactly what a numeric LITERAL
            // argument renders beside it. The binder bars every other numeric/integer function here (SR11/SR12), so a
            // non-admitting position still reaches Loud only through a --permissive coercion.
            : admitNumeric && n.Expr is BoundIntrinsicCall { ResultCategory: PicCategory.Numeric }
                ? OperandText.AsString(n, owner.Num)
                : Loud(n);
        public string Visit(BoundOperandError n) => EmitText.LoudValue("string", n.Feature);
        // An address-identifier (kb/Work PB1021) is class pointer: the binder admits it only as a relation operand, which
        // ConditionRenderer's pointer arms render, and an INVOKE argument, which OoEmitter renders — never a
        // character or numeric value. A class-screened intrinsic position refuses it even under --permissive, whose
        // coercion is offered only for an operand that holds characters (IntrinsicBinder.ReportClass, kb/Work PB2079;
        // PB2074 pins UPPER-CASE (ADDRESS OF X)). Reaching this is a binder hole, so it is LOUD, never a guessed value.
        public string Visit(BoundAddressOperand n) => EmitText.LoudValue("string", "address-identifier as a character argument");
        // Admitted PER-FUNCTION (PB59): the raw source-text image via the ONE OperandText channel where the
        // function's §15.x.3 rule admits a numeric literal (see StrNum); Loud everywhere else — see Str's ⛔.
        public string Visit(BoundNumericLiteral n) => admitNumeric ? OperandText.AsString(n, owner.Num) : Loud(n);
        // ⛔ A FIGURATIVE CONSTANT IS A LEGAL INTRINSIC ARGUMENT, AND ITS IMAGE IS ALREADY WRITTEN DOWN ONCE
        // (fix-queue PB25). §8.3.3.6.3 SR1 admits a figurative constant "whenever 'literal' appears in a format",
        // and §8.4.3.2.3 SR8 makes a literal a valid argument-1 — so `FUNCTION LOWER-CASE(SPACE)` is legal source.
        // Both arms used to render EmitText.LoudValue, so it compiled CLEAN and aborted at RUN TIME with
        // "intrinsic string argument 'BoundFigurative'": the wrong-stage family, on legal input.
        // The image is §8.3.3.6.4 GR3's, for the case where "the length of the string is NOT specified in the
        // rules for the context" — which is exactly a bare function argument: (b) a figurative other than
        // ALL literal-1 is ONE character, (c) otherwise the length of literal-1. OperandText.AsString already
        // implements precisely that, PCS-aware (its BoundFigurative interception is collating-context sensitive,
        // which a local copy here could not be, since this visitor cannot reach the renderer's collating state).
        // So these delegate rather than re-deriving it — the same delegation the BoundFieldOperand arm above
        // already makes.
        public string Visit(BoundFigurative n) => OperandText.AsString(n, owner.Num);
        public string Visit(BoundAllLiteral n) => OperandText.AsString(n, owner.Num);
        public string Visit(BoundPredefinedNull n) => OperandText.AsString(n, owner.Num);   // throws — never an intrinsic argument
        // A boolean EXPRESSION argument (§8.4.3.2.3 SR8; kb/Work PB65): its '0'/'1' image through the ONE boolean
        // renderer — INTEGER-OF-BOOLEAN(BIT-A B-AND BIT-B) reads the combined bit string.
        public string Visit(BoundBoolOperand n) => BooleanRenderer.Render(n.Expr, owner.Num);
    }
}
