## Entry 1137 - 2026-07-30 22:19 PDT - PB13: the blocker reproduces, PB5 closed on a false sentence, and one finding does not hold

Worked the one BLOCKER of the §15.32–15.44 batch. The rule I keep relearning applied again: **run the finding's
own repro, not your summary of it.** My first attempt used `SQRT(1e20)` and `ABS(1e11)`, saw no saturation, and I
was one step from recording the blocker as a false positive. The findings' repros reach the clamp; mine did not.

**Both reproduce, both silent:**

```
01 R PIC 9(31).  COMPUTE R = FUNCTION EXP(70) …   -> NO SIZE ERROR
                                                     R = 0170141183460469231731687303715
   §15.34.4 r1 + §15.4.1 require ≈2.5154386709191670×10³⁰ — wrong by a factor of fifteen.

IF FUNCTION EXP10(30) = FUNCTION EXP10(31)        -> TRUE
   two values a factor of TEN apart compare equal, with no receiver in the statement at all.
```

**PB5 closed on a false sentence, and the sentence is why this survived.** `FromDouble`'s doc said the clamp was
"past the 10¹⁸ any PICTURE can describe, so unreachable from a declarable receiver". It is wrong twice: a PICTURE
reaches 31 digit positions (CA33 caps it there), so `PIC 9(31)` holds 10³⁰; and the clamp fires at the FUNCTION's
quantization point, before any store, so a relation operand under `ReceiverContext.None` saturates with no
receiver at all. I corrected the comment in place rather than only the queue, because that sentence is what a
future reader trusts — exactly as PB5's author did. **A closing argument in a doc comment is load-bearing;
when it turns out to be wrong, the comment is the first thing to fix.**

**Scoped rather than attempted, and this time BEFORE writing code.** Widening the clamp constant cannot work: at
`ws = 9` the intermediate needs (receiver integer digits + 9) decimal digits and `Int128` supplies about 38, so a
31-digit receiver is two digits short whatever constant is chosen. The fix is two-sided — the emitter must choose
the working scale against the receiver's CAPACITY (`ReceiverContext` carries no digit count, which is the actual
work), and the runtime must stop silently saturating for the receiver-less case, which no emitter-side choice can
reach. That is a change to the float→fixed pipeline, where §0 already carries an open architectural finding on the
same seam. PB10 taught me this morning what it costs to size a change after starting it instead of before.

**Pinned so it cannot quietly rot:** `tests/conformance/2023/pb13_float_quantize_headroom.cob` is registered under
the manifest's `pending` list. It asserts the CORRECT values, and I verified it FAILS on both cases today — a
pending golden that already passed would be worthless. It becomes a passing spec-derived golden the moment the fix
lands, instead of the repro living only in prose.

**And one finding does not hold, which is worth as much as the ones that do.** The cluster claimed "INTEGER's
returned value depends on its receiver". It does not: `FUNCTION INTEGER(-1.5)` returns −2 into scale-0, scale-4
and scale-9 receivers alike. The related §0 remark about `COS(1)` differing between a 9- and a 17-decimal receiver
is ordinary receiver rounding of ONE value, which no rule forbids — storing into a wider receiver keeping more
digits is what every implementation does. Recorded as unproven rather than carried forward as fact. The refute
stage downgrades adjudications; nothing was downgrading the DEFECT list until it was run.

