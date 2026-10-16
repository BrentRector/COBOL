## PB651: a §15.3 type-6 operand is taken as its integer before any lane is chosen

kb/Work PB651 asked for a probe of the `--permissive` lane, because PB248's strict screen (COBOLNET1627) had moved a
whole class of source, a floating-point operand at a §15.3 type-6 position, into a lane nothing probed. The probe is
now a drift test, and it found three wrong answers.

**The probe.** `IntrinsicIntegerPositionPermissiveDriftTests` (Unit) derives every type-6 operand position from the
spec-verified schemas in `IntrinsicArgumentRules.Verified` (33 positions over 23 functions) and fails on a position
with no fixture. One `--permissive` program calls each over a `FLOAT-LONG` holding the integer, one holding it plus
one half, and the integer literal. The compile must warn COBOLNET1627, the generated C# must compile, and both float
probes must answer what the literal answers (docs/CONFORMANCE.md DOC-A.1-124: the integer, or the truncated integer
with EC-ARGUMENT-FUNCTION checking off). `CobolNetTestBase` gained `RunAssembly`, so a test that compiles with its own
options can run the result.

**The re-probe on this build.** Every position compiled (PB635's CS0117 fix holds). Three functions disagreed with
their integer: `MOD(7.5 3)` gave 1.5 and `MOD(7 3.5)` gave 0, because the scaled arm aligned the fraction.
`COMBINED-DATETIME` over a float argument-1 rode the binary64 lane and added 143951.5. `ANNUITY(0.05 3.5)` computed
3.5 periods. The non-intrinsic sites were probed too: a float `PERFORM … TIMES` count is refused in both modes, and a
float subscript, reference-modifier bound and `CALL … BY VALUE` argument all answer the truncated integer.

**The fix.** `IntrinsicRenderer.RawArg` is now the one indexed argument entry. `Arg`, `Dbl`, `DecArg`, `IntArg`,
`LeadArg` and the lane predicates (through `Operands`) all read it. At a verified type-6 position
(`IntrinsicArgumentRules.IsIntegerPosition`), it lands a float, SDIDI or fractional fixed-point operand at scale 0
through the shared wide type-6 intake, which keeps the value or raises. A float past Int128 (±1.7e38) raises
EC-ARGUMENT-FUNCTION. The first cut clamped it to the carrier's extreme instead, and the branch review measured what
that did: `MOD(1.0E39, 7)` answered 1, the remainder of 2^127 − 1, where the operand's integer answers 5, and
`ANNUITY(0, 1.0E39)` answered 1/1.7e38, both silently. A clamp is right only where the function answers the same for
every integer past a bound: TEST-DATE-YYYYMMDD and TEST-DAY-YYYYDDD argument-1 (the r1a catch-all) and FIND-STRING
argument-3 (0 once every match is ignored). Those arms alone take `IntArgWideSaturating`, and
`IntrinsicCarrierAgreementDriftTests.OnlyAnArgumentWhoseAnswerIsConstantPastABound_Saturates` keeps the set closed.
The same review fix found that the intake-width guard's arm splitter took FIND-STRING's `"true" : "false"`
conditional for an arm head, so FIND-STRING's arm had never been examined. Heads are now anchored at the start of a
line, and the guard asserts which arms it judged.

An integer has no binary64-ness, so a float at a type-6 position no longer enters the binary64 lane. The following
code became unreachable and was deleted:

- `IntrinsicRenderer.FloatLaneExempt` (FACTORIAL's by-name exemption);
- the binary64 bodies of the integer-only functions: the date family, the TEST- validators, the windowing trio and
  `ModReal`;
- the runtime helpers `TryIntegerArg` and `TryTotalIntegerArg`.

Removing shipped members is an incompatible change, so the runtime major goes from 4 to 5 and the surface hash is
re-pinned (`RuntimeAbiPinDriftTests`). `IntrinsicRealArgDriftTests` now derives which exact arms owe a `…Real` twin
from the verified type-6 positions, and its new `NoRealBody_ServesAFunctionWhoseEveryPositionIsType6` keeps the
deleted bodies from coming back. The floors of `IntrinsicArgumentIntakeContractDriftTests` (8 intakes) and the
MOD/REM raise scan (5 sites) were lowered to match. `FloatIntegerArgumentPermissiveTests` gained
`Pb651FractionalFloat_AtATypeSixPosition_UnderPermissive_IsTruncatedOnEveryLane`, which checks the values from the
equivalent arithmetic expressions (§15.64.4, §15.17.4, §15.9.4): MOD 1, COMBINED-DATETIME 143951.03600 and ANNUITY
0.367208564. The intrinsics and runtime design docs are updated, and DOC-A.1-124 lists the drift test as a witness.

**Not fixed: the registrar's extension.** The multi-receiver MOVE of a NUMERIC function goes through the §15.4
temporary (`SendingValueTemp.FunctionValuePic`, scale 9), and the problem is wider than the note says. Under strict
rules, `MOVE FUNCTION PI TO Q1 Q2` (PIC 9V9(15)) stores 3.141592653000000, while a single receiver stores
3.141592653589793. Under `--permissive`, `NUMVAL("3.7")` reaches a character receiver as `3.700000000`. The fix needs
a temporary that keeps the value on the carrier the renderer chooses, and that is a redesign beyond this slice. It is
reported as a lead.

No inventory row is re-verdicted (`closes_rows: []`, with its reason). No diagnostic codes were used.

**Review round 1.** R1-1: `IsIntegerPosition` had been inserted between `ArgumentRunIsAllNumeric` and its doc
block, so it carried two summaries while `ArgumentRunIsAllNumeric` had none; it was moved. The same displaced-doc shape
appears at about forty other sites across the tree. Those sites predate this branch and are reported as a lead for a
tree-wide sweep with a drift test. R1-2 is the clamp described above. The finisher's gate:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261015T222413Z-d30b2a). semgrep verify
PASS, no count rose.

**Finisher (the landing's Linux gate, RED on 348b0926c).** All three reds were this branch's own; main had touched
none of the files involved since the branch's base, and the leg-1 gate had run none of the cases. (1) The type-6
intake landed an SDIDI operand on `Int128`, on the premise that "an SDIDI holds 34 digits". It holds 34
SIGNIFICANT digits at any exponent, so MOD(10 ** 40, 3) (`2014/pb2630_integer_family_beyond_int128`) and
MOD(A ** 3, B) of a 15-digit A (`2023/pb69_native_power_past_the_carrier`) became fatal EC-SIZE-OVERFLOW where the
SDIDI body answers exactly. `RawArg` now keeps an SDIDI operand on the SDIDI as its integer
(`CobolIntrinsics.IntegerArgUnboundedDec`: the same integrality test, then `CobolDec.TruncateToInteger`). (2)
`2023/pb56_dec_carrier_intrinsics` computed MOD(7e-9, 3e-9) = 1e-9. That breaks §15.64.3 r1 ("Argument-1 and
argument-2 shall be integers"), and under DOC-A.1-124 the answer is the truncated integers, which is what the branch
now gives. The line now takes the integral SDIDI values 7 and 3, built from the same 1e-9 operand (MO=1.000000000).
That still witnesses PB56's no-quantization property, because a scale-6 landing makes both 0. (3)
`ExactCarrierBoundaryDriftTests.TheLiftPredicate_IsAskedNeverRestatedByTheIntrinsicRenderer` matched the call
spelling `num.LiftRounds(`, and the branch passes the predicate as a method group. The test now accepts either
form. (4) The 14 emitted-C# snapshots were re-recorded for runtime 5.0.0. The diff is exactly the two version lines
per snapshot, the third recurrence of kb/Work PB3144.
