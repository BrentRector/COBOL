## Entry 955 — 2026-07-21 16:30 PDT — Wave D (cont.): FLAG flagging Incr 2 — the frontend-inline options b COMPILE-TIME-ARITHMETIC-EXPRESSIONS + c EVALUATE (emitted in ConditionalCompilationProcessor)

The two options with NO bound residue — the >>EVALUATE directive and compile-time arithmetic are consumed at the
conditional-compilation stage and never reach the bound tree, so they are flagged INLINE in
`ConditionalCompilationProcessor` (design D4's second collection site), not in `FlagConformancePass`.

**The in-scan FLAG state.** A `FlagScanState` (a per-option ON/OFF dict) is updated as the CC stage scans directive
lines in source order — mirroring the compile-time `FlagState` fold but built incrementally, because these
constructs are consumed here. A `>>FLAG-02`/`>>FLAG-14` line in an emitting branch applies to it (via the ONE
`FlagDirectiveLine` parser) AND still survives for the post-COPY `FlagDirectiveProcessor` (the bound-option state).
`DirectiveDiag` holds a reference to it and emits the flag WARNING with the SAME code/message shape as
`FlagConformancePass`, so the two collection sites are indistinguishable to the user.

* **c EVALUATE (§7.3.15.4 GR4 c; E.2 item 8):** the `Frame` now records the syntactic presence of a `>>WHEN` and a
  `>>WHEN OTHER` (independent of which branch emits) + the >>EVALUATE line + whether FLAG-14 EVALUATE was ON there;
  at `>>END-EVALUATE`, a directive carrying BOTH is flagged at the >>EVALUATE line.
* **b COMPILE-TIME-ARITHMETIC-EXPRESSIONS (§7.3.15.4 GR4 b; E.2 item 6):** `EvaluateOperandText` / `EvaluateCceText`
  (the fragment-parse choke points for every IF/EVALUATE/WHEN/DEFINE operand, incl. `MatchWhen`'s range operands)
  call `diag.FlagArithmetic` on the already-parsed fragment — flag when the operand contains a real `addOp`/`mulOp`
  (not a bare literal) and b is ON. Fires only in the EVALUATED context (the eval helpers run only in active
  branches) — where the arithmetic "could give a different result."

**Gate.** Build 0W/0E · CLI-probed both (c: WHEN+WHEN-OTHER flagged, WHEN-only not; b: `1 + 2` and `1 + 1 = 2`
flagged, sole `5` and `Y = 5` not) · 38 FlagDirectiveTests (4 new; note frontend-inline warnings carry a
`path(line,col)` prefix, unlike bound-option edition warnings) · characterization 33/33 byte-exact (zero-overhead:
no >>FLAG ⇒ no warning, no output change). **Detectors done: 10 of 19 — INCR 2 COMPLETE.** NEXT = Incr 3
(state-coupled: i REF-MOD-ZERO-LENGTH, d MOVE-TO-SAME-NAME, e RANGE-EXCEPTION-FOR-INDEX) → Incr 4 (new-analysis:
I-O-DECLARATIVE, I-O-STATUS-04/07, EC-PROGRAM-EXCEPTIONS).

