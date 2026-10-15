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
through the type-6 intake. A float goes through the new `CobolIntrinsics.IntegerArgTotalReal`, which saturates past
Int128 rather than raising, so the body's own intake still decides range. That extends PB254's total-argument
determination to every position.

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
