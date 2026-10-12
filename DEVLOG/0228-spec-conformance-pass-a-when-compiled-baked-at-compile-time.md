## Entry 228 — Spec-conformance pass (A): WHEN-COMPILED baked at compile time; ON SIZE ERROR verified correct

Part of a spec-correctness sweep (the failure mode "compiles + runs + wrong output, no diagnostic").

**#1 ON SIZE ERROR — investigated, NO bug (no change).** An initial read suggested arithmetic
SIZE ERROR was unhandled, but that was based on DEAD code (`CobolProgram.DivideInto`/`DivideGiving`,
which silently no-op on /0 and have no IR caller). The LIVE path is fully correct: `ArithmeticLowerer`
emits `IrInitArithmeticStatus` + `LowerSizeError` (→ `IrLoadSizeError` + branch to ON/NOT-ON blocks)
for ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE, and `PicRuntime` sets `ArithmeticStatus.SizeError` via
`WouldOverflow`/`SafeDivide` on every op. Verified empirically (/e/tmp/repro/SE*.cob): DIVIDE-by-0,
COMPUTE/ADD/ADD-GIVING overflow all fire ON SIZE ERROR. A suspected SUBTRACT case (SUBTRACT 99 FROM
unsigned 9(2)=10) correctly did NOT fire: ISO arithmetic rules store the ABSOLUTE VALUE into an
unsigned receiver (|−89|=89 ≤ 99 → no size error; stored 89). Confirmed the stored value (89 unsigned,
−89 signed). So the original report was a wrong test expectation; ON SIZE ERROR is spec-correct.

**#2 FUNCTION WHEN-COMPILED — FIXED.** It returned the execution-time clock (`IntrinsicFunctions.
WhenCompiled()` called at runtime), so two runs of the same compiled program differed (proven:
…11593700 vs …11593800). Per ISO it must return the time the program was COMPILED. Fix: bake it as a
constant at emit time. `CilExpressionEmitter.EmitIrIntrinsicCall` now special-cases "WHEN-COMPILED"
and emits `Ldstr <WhenCompiledTimestamp>` (a static captured once at compiler-process start via the
runtime formatter, so the 21-char `yyyyMMddHHmmsscc±hhmm` form matches exactly and is identical for
every use in a compilation) instead of the runtime Call. Verified: the same compiled DLL now returns
an identical value across runs. Test: `IntrinsicWhenCompiledTests` (well-formed 21-char timestamp,
date == compile date).

Guard ALL GREEN: 1000 unit, 343 integration (+1 unrelated skip), 148 NIST baselines 0 FAIL*.
Remaining from the sweep: #3 LENGTH of an OCCURS DEPENDING ON group (uses max layout, not the current
DEPENDING-ON length) — to verify/fix next. Full spec-gap audit doc (task B) to follow.

