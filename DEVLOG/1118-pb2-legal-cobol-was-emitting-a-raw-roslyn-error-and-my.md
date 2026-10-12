## Entry 1118 - 2026-07-29 21:35 PDT - PB2: legal COBOL was emitting a raw Roslyn error, and my elegant fix broke six programs that had nothing to do with it

**THE DEFECT, verified by hand before any code was read.** A floating-point argument is legal for every
exact-family intrinsic — §15.7.3 r1 and its siblings require class NUMERIC, §8.5.2.12 item 2 makes a COMP-1/COMP-2
item category numeric, and §8.5.2.1 Table 2 makes that class numeric. `FUNCTION ABS(<COMP-2>)` produced:

```
error: backend compilation failed (generated C# at F1.g.cs):
  (43,69): error CS1503: Argument 1: cannot convert from 'double' to 'System.Int128'
```

**Ten of the eleven functions I probed did that.** Not a wrong answer — an INTERNAL failure escaping as a
diagnostic, phrased in generated C# the user never asked to see. The queue entry had described three possible
symptoms ("no value, a CS1503, or a silent requantization"); for the exact family it is reliably the second.

**ROOT CAUSE, one line.** `RenderNum` opened with `if (sig.Float) return RenderFloat(ic);` — dispatch by the
FUNCTION's family, never by the ARGUMENT's type. The exact family then rendered every argument through
`Align()`, which for a Real operand returns the double expression unchanged, straight into an `Int128` parameter.
The machinery to notice was already there and unused: `NumX` has carried a `Real` flag all along, and
`AlignedArgsEx` already computed `anyReal` for MEAN's benefit alone.

**WHY A DOUBLE BODY IS CONFORMING AND NOT A SHORTCUT.** Under native arithmetic — the default — §15.4.1 makes the
returned value's characteristics implementor-defined, and each of these functions is defined by an equivalent
arithmetic expression over its own operands. Once an argument arrives as binary64 the EAE *is* a binary64
evaluation. Nothing exact is surrendered, because there was nothing exact left to keep.

**⛔ AND THEN THE ELEGANT VERSION BROKE SIX PROGRAMS THAT NEVER TOUCHED A FLOAT.**

The first implementation gave the real bodies the SAME names as the exact ones. The reasoning was sound as far as
it went: `Int128` has no implicit conversion from `double`, so an exact call could never silently bind to a float
body — and then `RenderFloat`, which already renders every argument through `Dbl()` and calls
`sig.RuntimeMethod` by name, would route everything with a one-line change and no second naming scheme. I wrote
that argument into the file's doc comment as a virtue.

It does not compile. An integer **literal** converts implicitly to BOTH `Int128` and `double`, so
`FUNCTION MAX(5 7)` emitted `MaxScaled(5, 7)` and C# answered:

```
CS0121: The call is ambiguous between 'MaxScaled(params Int128[])' and 'MaxScaled(params double[])'
```

Six previously-green corpus programs went red — `da2_function_as_text`, `arith_standard`, `udf_nested_args`,
`udf_keyword_omitted`, `func_expr_arg`, `cobol_words_intrinsic` — none of which involves a floating-point
argument at all. **The blast radius of an ambiguous overload is every call site, not the ones you were thinking
about.** The bodies now carry a `…Real` name by a CONVENTION rather than a table (`XxxScaled` → `XxxReal`, else a
`Real` suffix — one string transform), and `NoRealBody_SharesAnExactMethodName` asserts the trap cannot come back.

**THE DRIFT TEST THEN FOUND A GAP MY PROBE MISSED.** I had tested sixteen functions by hand;
`EveryExactArm_HasItsRealCounterpart` reported a seventeenth — **COMBINED-DATETIME**, whose argument-2 §15.6
types `Num2` and which may therefore legitimately be a float. Confirmed: it failed with `CS0117` for the missing
body. §15.17.4 r1 gives the returned value as `argument-1 + (argument-2 / 100000)`, and the exact twin turns out
to encode precisely that as a scale shift (`date × 10^(scale+5) + secUnscaled`, read at scale+5), so the two
agree by construction. Verified: `COMBINED-DATETIME(20260729, 3661.5)` = `20260729.036615`.

⚠ **And that test needed correcting twice before it was honest.** Its first form demanded a real body for every
arm, including the date/format and integer-argument ones that can never receive a float. Its second scoped by the
catalog's `ArgKinds` column — the very column PB1 established is UNAUDITED. That is acceptable *here* and would
not be in the compiler, for a reason worth stating: this use only SCOPES A TEST, so a wrong hint leaves a gap in
what is asserted; PB1's use would have REJECTED SOURCE, where a wrong hint turns legal COBOL away. Same column,
opposite blast radius. It also needed the zero-arity exclusion, since SECONDS-PAST-MIDNIGHT takes no arguments
and its empty `ArgKinds` reads as "numeric" through the empty-means-`'n'` default.

**THE GOLDEN IS BUILT AROUND THE PAIRS THAT DISCRIMINATE.** A wrong body agrees with a right one on positive
operands, so `pb2_float_argument_exact_family` pins the four cases where they diverge: MOD(−7,3) = **2** (floored,
sign of argument-2, §15.64.4) against REM(−7,3) = **−1** (truncated, sign of argument-1, §15.77.4); and
INTEGER(−3.5) = **−4** (§15.44, "greatest integer not greater than") against INTEGER-PART(−3.5) = **−3** (§15.49,
truncated). Seventeen expected values, every one derived from the spec. It matched on the first run.

**WHAT DID NOT LAND, recorded rather than waved through.** `RV-15.75.4-1` RANDOM keeps its PARTIAL: its defect is
the fixed-point RECEIVER path (`FromDouble(call, ws)` re-rounding a value §15.75.4 r1 already places in `[0,1)`),
not the argument path. The standard-arithmetic legs (PI's `RV-15.73.3-2`/`-3`, PRESENT-VALUE's `RV-15.74.4-1`)
are a different seam. And the EC-ARGUMENT-FUNCTION **value** rules stay open by design: §15.3 makes an incorrect
argument VALUE a run-time condition, and this change is compile-time routing — conflating the two would be DA7's
wrong-stage defect in reverse.

**GATES.** Conformance **4142/4142**, zero skipped, nothing red. Unit **963/963**. Characterization **33/33**.
GAP **3779 → 3774**.

