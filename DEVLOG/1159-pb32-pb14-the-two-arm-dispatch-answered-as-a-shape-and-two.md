## Entry 1159 — 2026-08-03 21:19 PDT — PB32 + PB14: the two-arm dispatch answered as a shape, and two of the three named instances did not survive measurement

**The queue said PB32 was the fifth instance of "a dispatch with two arms where only one is ever fixed", and told
me to answer the SHAPE rather than add a sixth individual fix. The shape was real. Two of its three cited examples
were not.** Measuring the entry before implementing it is the whole content of this session's first half, and it
changed what got built.

**WHAT THE ENTRY CLAIMED, AND WHAT MEASURING FOUND.** `RenderNum` routes on `AnyRealArgument(ic)`, so every
statistical/MOD function has an exact `Int128` body and a binary64 one; the entry named MOD's zero-divisor rule,
MEDIAN's even-count mean and MIDRANGE's ×5-at-scale-s+1 as three fixes applied to one arm only.

- **MOD's zero divisor — CONFIRMED, and worse than written.** `ModReal` was `b == 0 ? 0 : …`: a second,
  independent guard returning the §15.3 default while never SETTING EC-ARGUMENT-FUNCTION. Neither screen the
  renderer relies on could see it either — `RealResult` and `FromDouble` both test only for NaN, and 0.0 is
  finite. Under `>>TURN EC-ARGUMENT-FUNCTION CHECKING ON`, `DISPLAY FUNCTION MOD(A ** 2, Z)` printed 0 and
  execution CONTINUED past a condition Table 13 (§14.6.13.1.1) makes FATAL, while the very next statement — the
  same function through the exact body — correctly terminated the run unit. Two carriers side by side in one run.
  **REM is the identical defect**, and the refuter's sweep is exact: MOD and REM are the ONLY real bodies whose
  domain guard returns instead of raising; every other funnels through `TryIntegerArg`, which raises.
- **MEDIAN's even-count mean — REFUTED.** `MedianReal`'s `(s[m-1] + s[m]) / 2.0` is correct in binary64 and
  `MedianScaled`'s `(b + c) × 5` is correct at scale s+1. Both arms are right, each in its own carrier.
- **MIDRANGE's ×5 — a REAL defect, on the arm the entry did not accuse.** The ×5/×10 that makes the halving exact
  spends a decimal digit of `Int128` headroom that MAX/MIN/SUM/RANGE never spend, so MEDIAN and MIDRANGE wrap at
  ONE FIFTH their siblings' magnitude — on the EXACT path, with no float in sight. Measured with an `ON SIZE
  ERROR` phrase present and NOT taken: `P PIC S9(29)V99` = 99999999999999999999999999999.98 and `Q PIC SV9(9)` =
  0.000000002 gave MAX and MIN both exact while MIDRANGE returned **15971763307906153653662539256.81** against a
  true 49999999999999999999999999999.99 that FITS the receiver — and the compiler's OWN hand-written §15.62.4
  equivalent arithmetic expression produced the correct value in the same run.

**⛔ AND THE BIGGEST ONE WAS NOT IN PB32 AT ALL — IT WAS PB14, FILED SEPARATELY FOR TWO BATCHES.** `NumX` carries
THREE carriers — exact scaled `Int128`, the `CobolDec` SDIDI, binary64 — and `IntrinsicRenderer` was written for
two. Under `ARITHMETIC IS STANDARD-DECIMAL` every arithmetic expression renders as a `Dec`, so a §15.3 type-10
arithmetic-expression argument (legal at 2014/2023) reached `MaxScaled(params Int128[])` as a raw `CobolDec` and
the user saw `error CS1503: cannot convert from 'CobolNet.Runtime.CobolDec' to 'System.Int128'` on conforming
source — an internal failure escaping as a backend error, which is the PB2 shape on the Dec axis. PB14 and PB32's
second leg are **one defect recorded twice**; only measuring both showed it.

**THREE CHANGES, EACH AT A CHOKE POINT.**
1. **One raise site per RULE, not per carrier** — `CobolIntrinsics.ModZeroDivisor` / `RemZeroDivisor`. The `long`
   return converts implicitly to both `Int128` and `double`, so both carriers return the same §15.3 default with
   the same message and the same citation, and the rule cannot be corrected in one arm again.
2. **One SDIDI landing at `IntrinsicRenderer.Arg`**, plus the `Dec` arm `NumericRenderer.Align` was missing.
   ⚠ **Fixing `Align` alone would have looked like a full fix.** Measured after that edit: MAX / MIN / MOD /
   MEDIAN compiled again — and ABS, SIGN, INTEGER, FRACTION-PART and FACTORIAL still did not, because those arms
   consume `Arg(...).Expr` or `AsInt` and never reach `Align`. The four that recovered are exactly the ones a
   spot-check samples. Landing at `Arg` — the single origin of every numeric argument in the renderer — covers all
   nine and covers the next arm nobody has written yet. It also gives the SDIDI a compile-time scale, so it joins
   `AlignedArgsEx`'s common-scale maximum instead of contributing a placeholder 0.
   ⭐ `Align`'s own doc comment declared it **"TOTAL OVER THE CARRIER KINDS, and it has to be"** while handling two
   of three. The invariant was written down correctly and the code did not implement it.
3. **The exact carrier's escape boundary raises `EC-SIZE-OVERFLOW` instead of wrapping** (`ScaleForHalving`).
   This was not a new policy: `COBOLNET_NUMERIC_DESIGN.md`'s substrate paragraph already says the Int128 escape
   boundary is EC-SIZE-OVERFLOW. The code had simply never implemented it here.

**⛔ THE DESIGN DOC CARRIED A FALSE CLAIM, AND IT IS WHY THIS FAMILY LOOKED REVIEWED.** D3's intrinsics paragraph
read that the exact-Int128 family "already satisfies" §15.4.1 r1, unqualified. It is true only for an argument
list that is entirely fixed-point, and all three failures above sit outside that qualifier. Corrected in place
with the three measured refutations rather than quietly reworded — a design doc that overstates its own coverage
is what lets a review skip a clause.

**THE DRIFT TEST IS THE SHAPE, NOT THE INSTANCES.** `IntrinsicRealArgDriftTests` asserted the second body EXISTS;
nothing asserted the two AGREE. `IntrinsicCarrierAgreementDriftTests` now asserts both carriers raise on a zero
divisor and agree on the unchecked default — and its general arm fails on ANY real body answering a domain guard
with a bare literal, so the sixth instance is caught by shape rather than by name. It is self-tested against the
pre-fix `ModReal` text, because a guard nobody has seen fail is a guard nobody has tested.
⚠ **It failed on its own first run — against a COMMENT quoting the defective form** in the very fix that removed
it. Comments are now stripped before matching. A checker that cannot tell a defect from a description of one has
to be silenced, and a silenced guard is the dead lookup this project keeps rediscovering.

**WHAT IS STILL OPEN, AND IT IS BLOCKED, NOT FORGOTTEN.** The float body is still REACHED for a legal integer
argument, because `NumericRenderer.Power` returns `Real: true` in a receiver-less context: `FUNCTION MOD(A ** 2, B)`
is exact under `COMPUTE` and binary64 under `DISPLAY` or an `IF` subject — 930000007 versus **930000008**, and
`IF FUNCTION MOD(A ** 2, B) = 930000007` evaluates FALSE. That is a control-flow defect produced by a function's
value depending on its receiver's SHAPE, which §15.4 forbids and which PB13 closed for the float family only. Its
root cause is **PB18** — `**` has no exact arm for an integer exponent — so it closes when PB18 does, and PB18 is
waiting on the owner's native-`**` carrier decision. Re-verified unchanged after this wave landed, rather than
assumed. The other residue is **PB38** (new): a float argument still demotes the whole list to binary64 under a
standard mode, because `RenderNum` is the one renderer that does not test the arithmetic mode before its float
branch — `CombineCore`, `Power` and `ConditionRenderer` all do, which is the ordering D3 states in words.

**GATE.** Wave-local per the `gate` skill, all green: characterization 33/33 · Conformance corpus **419/419**
(both new goldens included) · Conformance Intrinsic 150/150 · Conformance Arithmetic 33/33 · Unit
Intrinsic+Numeric 193/193 · `IntrinsicCarrierAgreement` 8/8. Every one of the five original probes was re-run
after the landing; three flipped and the fourth (PB18's) is unchanged by design.

**Goldens:** `2023/pb32_dec_carrier_intrinsic_argument` (nine functions across all three argument routes),
`2023/pb32_exact_carrier_halving_boundary` (the boundary AND the ordinary magnitudes, because a guard that
over-fires trades a silent wrong answer for a loud one).

