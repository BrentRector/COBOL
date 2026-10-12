## Entry 1163 — 2026-08-04 00:12 PDT — PB38: the mode beats the float branch, and the fix fit inside the landing PB32 already built

**PB38 was the last open member of the two-arm-dispatch family, and its own queue entry told the next session to
answer it with a NEW carrier selector and warned that anything less would be "the sixth individual fix". The
warning was right and the estimate under it was wrong** — the selector was unnecessary, because
`IntrinsicRenderer.Landed`, built yesterday for PB32/PB14, already WAS the one place a non-exact operand becomes
an exact one. The mode belongs in that landing, not in a new mechanism beside it.

**THE DEFECT.** `RenderNum` routed on `AnyRealArgument` with no arithmetic-mode guard, while
`NumericRenderer.CombineCore`, `NumericRenderer.Power` and `ConditionRenderer` all test `StandardDecimal` FIRST —
the ordering `COBOLNET_NUMERIC_DESIGN.md` D3 states in words, *"the mode branch runs BEFORE the D16 float
branch"*. `RenderNum` was the ONE renderer that did not, so a single COMP-2 argument demoted the whole list to
binary64 even under `ARITHMETIC IS STANDARD-DECIMAL`, where §15.4.1 r1 is unconditional and §8.8.1.5.2 r1
converts every fixed-point operand into an SDIDI exactly. **MEASURED, before → after:**

| | before | after | required |
|---|---|---|---|
| `MEDIAN(H1 H2 H3 F1 F1)` | 100000000000000004.76 | **100000000000000001** | 100000000000000001 |
| `MAX(H1 H2 H3 F1)` | 100000000000000004.76 | **100000000000000003** | 100000000000000003 |

The three distinct 18-digit operands all collapsed to ONE binary64 — the ulp at 1e17 is 16 — so they compared
EQUAL and the §8.8.4.2.4 comparison the clause mandates never happened. The failure mode is COLLAPSE, not
inversion, which is why the sorted position still tracked while the selected value did not. **The all-fixed
control was exact throughout, in the same program and the same mode**, and that is the attribution: the error was
injected by the ROUTE, not by the operands.

**THE FIX.** A float operand under a standard mode converts in through `NumericRenderer.DecOperand` — the
compiler's own §8.8.1.5.1 conversion, the one `CombineCore` and `Power` already use — and then lands by the
identical route a `Dec` operand takes. One landing, not two mechanisms.

⛔ **AND THE HALF THAT WOULD HAVE BEEN MISSED: `AnyRealArgument` had to be asked of the LANDED operand, not the
raw one.** It read `num.AsNum(a, …).Real` directly. Left that way, the landing would convert the float correctly
and the dispatch would still route the call to the binary64 body — **the fix present, the defect intact, the
build green, and every test passing for the wrong reason.** The dispatch and the landing must ask the same
question of the same value. That is the same shape as everything else in this family: two places that must agree,
where only one was changed.

**THE GOLDEN PINS THE WHOLE FAMILY, DELIBERATELY.** `AlignedArgs` is shared by MEDIAN · MAX · MIN · RANGE ·
MIDRANGE · ORD-MAX · ORD-MIN · SUM · MEAN, so a golden covering the three functions the defect was reported
against would leave five arms free to regress on the exact line this changed. It also carries the all-fixed
control and a non-collapse relation, because "the answer is right" and "the answer is right FOR THE RIGHT REASON"
are different assertions.

**GATE.** Corpus 421/421 (the new golden included) · Conformance Arithmetic+Intrinsic+Compute 213/213 ·
characterization 33/33. Native arithmetic re-probed and unchanged — `ABS(-3.5)` = 3.5, `MAX(-3.5, 2.5)` = 2.5,
`MOD(7.5, 2)` = 1.5 — because this change must not reach the mode that was already correct.

**With PB38 closed, the PB2 · PB13 · PB14 · PB28 · PB32 · PB38 family is fully landed.** What it cost, recorded
once: six items, five of which were the same sentence — *a dispatch with two arms where only one was ever fixed*.

