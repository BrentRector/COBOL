## Entry 1156 — 2026-08-03 16:18 PDT — §5b step 5: PB18 re-verified end to end, and deliberately NOT started — the recipe does not answer the question the fix turns on

Step 5 is "drain the fix queue to zero once, while it is 12 items". PB18 is the best-scoped of them, so it went
first — and §0's standing caution ("a queue entry's summary is a CLAIM — three were wrong last session") got the
treatment it asks for BEFORE any code was touched.

**EVERY CLAIM IN THE ENTRY HOLDS.** That is worth recording precisely because the last three did not:

- Both citations validate: `cite.py --check 8.8.1.2 "exponentiation"` and `--check 8.8.1.3 "implementor"`.
- The repro is exact. `COMPUTE R = 10 ** 30` into `PIC 9(31)` prints **1000000000000000071935427891953**
  against the exact **1000000000000000000000000000000** — off by 71,935,427,891,953 and entirely plausible on
  sight, which is the family §0 names ("the worst defects return a plausible answer, not a broken one").
- The named code is real: `NumericRenderer.Power` (`NumericRenderer.cs:417`) emits
  `FromDouble(System.Math.Pow(…), ws)` for fixed-point operands **regardless of whether the exponent is an
  integer**.
- The "shape to copy" is real, and I doubted it wrongly on first read. `CobolDec.cs:120` is a bare
  `FromDouble(Math.Pow(...))`, which looked like the same defect one register over — but it is the NON-integer
  fallback, correctly guarded by the integer test above it, and `CobolDec.Pow` really does binary
  square-and-multiply over exact SDIDI at lines 107–116. Reading the guard, not the call, settled it.

**⛔ AND THE RECIPE STILL DOES NOT ANSWER THE QUESTION THE FIX TURNS ON.** "Repeated exact `Int128`
multiplication for an integer exponent" is straightforward only for a scale-0 base. A base of scale *s* raised
to *n* produces scale *s·n*: `10 ** 30` is 31 digits and sits comfortably inside Int128, but `1.5 ** 30` needs
scale 30 on a value of ~191751 — roughly 36 significant digits before the receiver's own capacity enters — and
a scale-2 base at a modest exponent leaves Int128 entirely. So the fix must decide what happens when the EXACT
result does not fit: raise EC-SIZE-EXPONENTIATION (which is `CobolDec.Pow`'s own precedent — it throws
`CobolSizeError` past its loop bound), or fall back to the documented double approximation for that case alone.
§8.8.1.3 licenses either; D3's "exact Int128 fixed-point engine" is what makes the choice a documentation
question rather than a free one.

**That is a numeric-DESIGN decision, not a code edit, and it belongs in `docs/COBOLNET_NUMERIC_DESIGN.md`
before the emitter changes** (CLAUDE.md rules 2 and 5). Starting it as "a small change" and discovering the
scale explosion halfway is how a MINOR item becomes an un-gated half-fix in the numeric engine — the one
subsystem where a plausible wrong answer is hardest to see. So it is deliberately NOT started, the verification
is written into the queue entry, and the next session begins from measured ground instead of an estimate.

⚠ This is the same discipline the rest of the day produced by accident: **measure the step before executing it.**
Three of §5b's four premises fell to that today; here the premise held and the RECIPE was the part that did not.

