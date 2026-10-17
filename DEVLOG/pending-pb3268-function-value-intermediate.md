## PB3268 - the intermediate that hands a numeric function's value to several receivers is described for the value, not as the returned-value item

**What was wrong.** `MOVE FUNCTION PI TO Q1 Q2` (`PIC 9V9(15)`) stored 3141592653000000 in both receivers while
`MOVE FUNCTION PI TO Q3` stored 3141592653589793. ISO §14.9.25.4 GR1 writes a function-identifier's single evaluation as
`MOVE a TO temp / MOVE temp TO b / MOVE temp TO c`, "where 'temp' is an intermediate result item provided by the
implementor", and §15.4.1 requires the returned value to be "the same for all instances of a given function". The note
(filed from PB651's lead L1) named the field and the symptom; the re-probe on this build showed one root with four
symptoms: the same intermediate gave a COMP-2 receiver beside another 3.141592653, kept 21 integer digits (a
25-integer-digit NUMVAL stored 0000567890123456789012345 in both `PIC 9(25)` receivers) and compared an EVALUATE subject
at nine places (`EVALUATE FUNCTION PI WHEN 3.141592653 WHEN 3.14159265358979` took the first WHEN).

**Root cause.** `SendingValueTemp` used ONE description, `FunctionValuePic` (S9(21)V9(9)), for two different items.
DOC-A.1-92 fixes the §15.4 returned-value ITEM at that description for the questions asked OF the item (`FUNCTION LENGTH`,
IN-ARITHMETIC-RANGE), and that stays. The GR1 `temp` is another item, whose only job is to give every receiver what
`MOVE a TO that receiver` gives.

**Fix shape.** `SendingValueTemp.IntermediatePic` describes the intermediate for the value and its use
(`SendingValueTemp.ValueUse`, passed by `MoveBinder` as `MovedTo(targets)` and by `OoBinder` as `Text`). An INTEGER function
is unchanged (S9(30) scale 0). The §15.4.1 float family under NATIVE arithmetic (`IntrinsicSig.Float` with a non-decimal
mode: the renderer's own dispatch, `IntrinsicRenderer.RenderFloat`, read at bind time) and any function whose receivers
are all floating-point items are held on a COMP-2 item, so the store into the intermediate quantizes nothing and each
receiver's own float landing quantizes once at its scale. Every other function is held in the WIDEST receiver's fixed-point
description (scale and integer digits from `PicInfo.ReceiverScale` / `DigitPositions`; a floating-point receiver asking
21 + 17; a character receiver the returned-value item's 21 + 9), capped at the 38 digits of the `Int128` carrier with the
integer digits kept first. A value read as an operand (an EVALUATE subject, a function argument `ArgumentOrder` freezes) is
held in 21 integer + 17 fraction digits; the returned-value item's S9(21)V9(9) remains only for text in an INVOKE character
formal. The 38 is now `PicInfo.ScaledCarrierDigits`, which `ReceiverContext.IntermediateDigits` reads. Two stale claims in
files read on the way were corrected: `FunctionValuePic`'s summary said the function-bearing subscript temp shared its
shape (PB1890 gave that temp its own saturated 38-digit item) and `COBOLNET_DATA_MODEL_DESIGN.md` still described the
subscript temp as S9(21)V9(9).

**Differential.** A generated program (30 functions, native and STANDARD-DECIMAL, 13 receiver kinds including COMP-1/COMP-2,
COMP-3, COMP-5, numeric-edited, 9(25), V9(20) and S9(30)V9) compares `MOVE f TO R1 R2` with `MOVE f TO R3`: BAD=0 in both
modes after the fix. Reference-modified character receivers (`--permissive`) and RANDOM (one draw) agree too.

**Goldens.** `85/pb3268_function_value_one_evaluation`, `2002/pb3268_function_value_pi_and_wide_integers`,
`2023/pb3268_function_value_float_receivers`, `2023/pb3268_function_value_standard_decimal`, each derived from the rule and
the true decimal expansions of PI, E and SQRT(2); no negative, since the change rejects no source. The drift test
`FunctionValueIntermediateDriftTests` holds the carrier over every `Float: true` catalog row (native: `double`; decimal
engine: not `double`) and that only the three readers of the returned-value item ask for `FunctionValuePic`; it reuses
`StandardModeReturnedValueContainerDriftTests`' fixture table, now internal.

**Rows.** None claimed (the note had `inventory_rows: []`); `closes_rows_reason` says why. DOC-A.1-92's determination
text in `docs/CONFORMANCE.md` is updated with the intermediate's description and the residual.

**Residual, named in DOC-A.1-92.** An exact-family value needing more digits than the `Int128` carrier (NUMVAL text of more
than 17 fraction digits as an EVALUATE subject; a MOVE whose receivers' integer plus fraction digits exceed 38) is held at
the carrier's digits where the un-hoisted reference keeps the SDIDI's. Closing it needs an SDIDI-carried item.

**Self-review.** Architecture: one description per item (the returned-value item and the intermediate are two names now),
no C# text in a bound node. Full code: the float-class predicate mirrors `RenderFloat`; the sibling arms (EVALUATE, frozen
argument, INVOKE text) were each probed. Performance: bind-time only. Duplication: the 38-digit bound is one constant. The
gate's first run was RED on two unit drift tests (`PlaceDenotedItemDriftTests` wanted `Place.DenotedItem` for the slice
question; `HostWitnessTraitDriftTests` wanted the sweep trait on the new class); both fixed.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run:
Conformance 5,720 · Unit 446 · Characterization 31 ... Conformance 6,123/11,843 · Unit 33,333/33,779 · Characterization 5/36
cases ran` (run `20261016T194103Z-db34e6`).

**Review round 1.** The branch review found three defects in the first cut, all answered in the same branch.
(1) The bind-time test "is this function's value a binary64" read the function alone (`IntrinsicSig.Float`), while the
renderer chooses the lane from the function and its argument carriers. `MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO Q1 Q2`
(`PIC 9V9(25)`) stored 00155555555555555551083823 against the single-receiver 00155555555555555555555555 (a floating-point
literal is an SDIDI operand, so the call leaves the float lane; the COMP-2 intermediate rounded it), and `MOVE FUNCTION
ABS(F9) TO FA N1` (F9 a COMP-2 holding 1E+30) stored 19884624838656 in the COMP-2 (a binary64 argument puts any
numeric-argument function on the float lane; the fixed intermediate wrapped). A 14-function by 8-receiver-set differential
found the same two directions across SUM, MEAN, RANGE, MIDRANGE, ANNUITY, PRESENT-VALUE, STANDARD-DEVIATION and a nested
function's binary64 value. The fix is `FunctionValueLane`: the renderer's function-level order stated once
(`ReturnsBinary64`), argument carriers read by a visitor over the generated bound-tree visitor interfaces (so a bound node
added tomorrow is a compile error there as in `NumericRenderer`), following `CombineCore` and `Power` for the arithmetic
around a float; `NumericLiteral.TryGetIntegerValue` is the one reader of "is this literal an integer value" now shared with
`NumericRenderer`'s exact-power arm. The list it restates - the functions with an SDIDI body under native arithmetic - is
pinned to `RenderDec`'s switch by `FunctionValueLaneDriftTests`; the golden `2023/pb3268_function_value_lane_matrix` is the
whole prediction against the renderer's actual answer: 353 cells of function x argument shape x receiver set, each a
multi-receiver MOVE compared with the single-receiver one (BAD=0000), plus the VARIANCE and ANNUITY readbacks (7/450 and
0.1331/0.331 truncated at 25 places) and the 1E+30 and 1E-20 COMP-2 cases. (2) A floating-point receiver beside a
fixed-point one was sized 21 integer + 17 fraction digits and so cost the fixed receiver fraction digits (`PIC 9V9(20)`
beside a COMP-2 kept 17); the binary64 half is closed by (1), and for an exact or SDIDI value the float receiver now takes
what the fixed-point receivers leave of the 38 digits, 12 integer digits at least. What stays open is the carrier's limit:
an exact value outside 1E-9..1E+12 reaching a COMP-2 beside a fixed item (`MOVE FUNCTION NUMVAL("0.00000000000000000001")
TO F1 N1` stores 0 in F1) needs an SDIDI-carried intermediate item, the same redesign as the values past the `Int128`
carrier. (3) The COMP-2 description cited §13.18.60.4 GR15 (FLOAT-BINARY-64); the usage is FLOAT-LONG, GR13 (`cite.py
--check` run on both).

Review round 2 (three findings against the round-1 `FunctionValueLane`). (1) NUMVAL / NUMVAL-C are SDIDI-carried in every
mode (`RenderNum`'s first arm), so a NUMVAL nested in VARIANCE / ANNUITY / STANDARD-DEVIATION puts that call on the SDIDI;
the bind-time prediction classified the nested call by its string argument and handed the outer function a COMP-2
intermediate (`VARIANCE(NUMVAL("0.1") NUMVAL("0.2") NUMVAL("0.4")) TO Q1 Q2` stored `...551083823` where the single MOVE
stored `...555555`). The value-fixed-by-definition set moved into `FunctionValueLane` and the renderer's arm asks it there.
The sibling the reviewer named reproduced: a nested SUM / RANGE / MEAN / MEDIAN / MIDRANGE whose aligned arguments may leave
the `Int128` carrier is SDIDI-carried (PB621), so `AlignmentMayLeaveCarrier` became one reader in `FunctionValueLane` that
the renderer (from its `NumX` operands) and the prediction (from the bound tree: a field's digit bound, a literal's) both
ask. (2) A binary64 or fractional operand at a §15.3 type-6 position is already its integer when `RawArg` hands it to the
lane predicates, so `AtPosition` does the same in the prediction (`COMBINED-DATETIME` of a COMP-2 date, permissive only).
(3) The `Selections` citation said "the content of" for ORD-MAX / ORD-MIN (§15.71.4 / §15.72.4 return an ordinal number)
and for MEDIAN (§15.61.1; §15.61.4 r2 makes an even count the arithmetic mean); corrected in the code, the renderer
comment and the intrinsics design doc. Goldens: `2023/pb3268_function_value_nested_lane` (derived from the equivalent
arithmetic expressions with exact decimals: 7/450, 0.1/(1-1.1^-3), sqrt(7/450)); the permissive witness is a unit test,
since strict conformance rejects the float at a type-6 position. Residual: a computed expression as the direct argument of
a nested wide cross-aligning call has a digit bound only the renderer's arithmetic knows.

**Review round 3 (w1108b finisher).** One finding. R3-1: `FunctionValueLane.Of` returned an exact value for every
INTEGER-typed call BEFORE its SDIDI arms, but `IntrinsicRenderer.RenderNum` has no integer early-out: INTEGER(1.5E0)
computes `FloorDec` on the SDIDI, a SUM over PIC 9(31) elements takes the PB621 arm, and INTEGER of a COMP-2 item takes
`FloorReal`. So a nested integer call with an SDIDI-carried argument made the outer float-family call compute on the SDIDI
while the prediction said binary64: `MOVE FUNCTION ANNUITY(FUNCTION INTEGER(1.5E0) 3) TO Q1 Q2` (PIC 9V9(25)) stored
1.1428571428571427937015414 where the single MOVE stores 1.1428571428571428571428571 (8/7), and `ANNUITY(SUM(T(ALL)) 3)`
stored 0.3333333333333333148296162 against 0.3333333333333333333333333 (reproduced on this build). Fixed at the root: the
early-out now covers only a non-numeric result (§15.2 item 5 fixes an integer function's TYPE, never its carrier), and an
SDIDI arm answers the carrier `RenderDec`'s arm really returns: the SDIDI, or an exact integer for SIGN / ORD-MAX / ORD-MIN
(`FunctionValueLane.NativeSdidiBodiesWithExactResult`, derived from the renderer's switch by
`FunctionValueLaneDriftTests.NativeSdidiBodiesWithExactResult_AreTheRenderDecArmsNotBuiltByDec`). The sibling sweep
(`E:\COBOL-coord\scratch\w1108\w1108b\intsweep.cbl`: INTEGER of a COMP-2, INTEGER-PART, SIGN, ORD-MAX, SUM / VARIANCE /
MEAN / STANDARD-DEVIATION over a nested integer call) found every nested shape now equal, and one more defect in the same
intermediate: an INTEGER function's intermediate was the returned-value item's S9(30), so `MOVE FUNCTION INTEGER(N31) TO R1
R2` (N31, R PIC S9(31)) dropped the leading digit of a 31-digit value the single MOVE stores whole. The intermediate of an
integer value is now the carrier's 38 digits at scale 0 (`SendingValueTemp.IntegerIntermediatePic`; the 30-digit item stays
what `FUNCTION LENGTH` measures and the text image a character formal reads). Witness:
`2023/pb3268_function_value_integer_nested` (INTEGER and INTEGER-PART of 1.5E0 and a wide integer SUM nested in ANNUITY,
8/7 and 1/3 truncated to 25 places, and the 31-digit INTEGER and SUM stored whole; prints BAD=0000).

**Review round 4 (w1108b finisher).** Two findings on the round-3 head `75b34dce4`. R1-1: `SendingValueTemp.IntermediatePic`
answered an INTEGER function before it asked whether the value is a binary64 or every receiver is floating-point, so a
binary64 or SDIDI integer of 1E38 or more was stored into the 38-digit scale-0 `IntegerIntermediatePic` and reached COMP-2
receivers cut: `MOVE FUNCTION INTEGER(F) TO C1 C2` (F COMP-2 1.5E50) stored 9.99906754673275E+37 where `MOVE ... TO C3`
stores 1.5E+50, INTEGER-PART likewise, and `MOVE FUNCTION INTEGER(1.5E40) TO C1 C2` stored 0 (probe
`E:\COBOL-coord\scratch\w1108\w1108b\r1big.cbl`). §15.2 item 5 fixes the type, never the magnitude (OK §15.2 5)), so the
binary64 / all-float question now comes first for every function and the integer item answers only what is left; the text
use keeps the returned-value item's 30-digit image (DOC-A.1-92). EVALUATE / IF over INTEGER(F) and a fixed-point receiver
beside a float one agreed before and after (`r1evl.cbl`). Witness `2023/pb3268_function_value_integer_float` (INTEGER and
INTEGER-PART of a FLOAT-LONG 1.5E50, INTEGER(1.5E40), INTEGER(-2.5E0) beside a fixed receiver; BAD=0003 on the round-3
build, BAD=0000 now). R1-2: the lane (`CobolNet.Binding.Procedure`, compiler.binding) imported `CobolNet.CodeGen.Emit`
(`IntrinsicRenderer.CrossAlignedNativeArms`, `EmitText.UnscaledLit`) and the renderer imported the lane
(`ValueFixedByDefinition`, `AlignmentMayLeaveCarrier`, `AlignedOperand`): a namespace cycle that DESIGN-architecture-review.md
§8.1 allows in neither direction. The shared rules moved to the layer both may reach: `IntrinsicCarrierRules`
(`CobolNet.Binding.Bound`, compiler.bound) holds the cross-aligned arms, the value-fixed-by-definition pair and the carrier
escape bound, and `NumericLiteral.TryUnscaledParts` / `UnscaledDigitBound` (frontend.common) hold the literal
decomposition and digit bound that `EmitText.UnscaledLit`, the CALL argument lane and the lane prediction read. Every caller
changed, no forwarder. `FunctionValueLaneDriftTests.BindingProcedure_AndCodeGenEmit_DoNotReachEachOther` holds every file of
both namespaces to it.
