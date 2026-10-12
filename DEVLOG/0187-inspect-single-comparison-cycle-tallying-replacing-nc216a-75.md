## Entry 187 — 2026-05-28: INSPECT single comparison cycle (TALLYING/REPLACING) — NC216A 7→5 FAIL*

**Problem (NC216A INS-TEST-F3-19, F3-38, F1-27):** INSPECT lowered each TALLYING/REPLACING
operand to an independent full-string pass. The spec (ISO 6.17.3 GR 8) requires all
operands of a phrase to share ONE left-to-right comparison cycle: at each position the
operands are tried in source order, the first match tallies/replaces and advances past the
matched characters, then the cycle restarts. Independent passes double-count and mis-handle
operand precedence — e.g. `TALLYING c1 FOR ALL "A" c2 FOR LEADING "AH"` must leave c2 at 0
because the leading 'A' is consumed by the earlier ALL operand before LEADING is ever tried.

**Fix (runtime + IR + lowering + emitter):**
- `InspectRuntime.TallyingPass`/`ReplacingPass` implement the single cycle with per-operand
  region eligibility (BEFORE/AFTER) and LEADING/FIRST run-termination state. TALLYING returns
  per-operand counts; the emitter adds each into its counter (`AddCountToField`).
- New grouped IR `IrInspectTallying`/`IrInspectReplacing` (with `IrInspectTallyOp`/`...ReplaceOp`)
  replace the per-operand `IrInspectTally`/`IrInspectReplace`. `StringLowerer` groups all
  operands of one INSPECT; `CilStringEmitter` marshals parallel `int[]`/`string[]` arrays
  (same pattern as the UNSTRING delimiter arrays) and calls the single runtime entry point.
- CONVERTING unchanged. Decomposition unit tests updated for the renamed emit methods.

Validated regression-free: NC124A (170), NC126A, NC243A, NC244A all still MATCH. NC216A
INS-TEST-F3-19.05 and F3-38 now pass (7→5 FAIL*). The remaining failures (F3-19.01/.03/.04,
F1-27) are blocked by a separate grammar ambiguity in multi-counter TALLYING — `inspectCountPhrase`
greedily consumes the next counter's data-name as a pattern — and F1-23.02 by signed-DISPLAY
overpunch. Those are tracked separately (grammar change pending review).

---

