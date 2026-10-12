## Entry 321 — WRITE was the only I/O statement not emitting its INVALID KEY / NOT INVALID KEY blocks → IX108A baselined (NIST 359→360)

**IX108A** (`000 OF 001`, `001 TEST(S) FAILED`, no detail line) — the last diagnosed file-I/O fail, and NOT the
RL205A NOT-INVALID *binding* bug (that fix bound the blocks correctly but they were never lowered). Root cause:
**`FileIoLowerer.LowerWrite` never emitted the invalid-key branch.** READ, REWRITE, DELETE and START each emit
`IrCheckFileInvalidKey` + `LowerConditionalBranch(InvalidKey, NotInvalidKey, …)` after the operation — WRITE
alone did not, so a WRITE's bound INVALID KEY / NOT INVALID KEY imperatives were silently DROPPED compiler-wide
(ISO §14.9.51 GR11 / §9.1.14). IX108A's first test is `WRITE IX-FS2R1 NOT INVALID (MOVE 1 TO SWITCH-NOT-INVALID)`
then `IF SWITCH-NOT-INVALID = 1 … ELSE PERFORM FAIL / GO TO CCVS-EXIT`; the WRITE succeeded but its NOT-INVALID
block never ran → the switch stayed 0 → `PERFORM FAIL` (counted, no `PRINT-DETAIL`) → `GO TO CCVS-EXIT` at the
very first test → `000 OF 001`. Fix: add the missing invalid-key branch to `LowerWrite` (mutually exclusive with
the LINAGE AT END-OF-PAGE branch). IX108A → **032 OF 032**, 0 FAIL*, baselined (after IX107A).

**The fix also un-skipped latent tests in a baseline I'd set THIS session (transparency).** It is a broad change
(every `WRITE … INVALID KEY`/`NOT INVALID KEY` now emits its block), and the full guard flagged exactly one
existing baseline — **RL111A** (DEVLOG 315), whose report grew from `022 OF 022` to **`024 OF 024`** with two new
PASSes (`REL-TEST-040-1`/`050-1`, both `WRITE … NOT INVALID` tests that the dropped-block bug had silently
skipped). The new output is strictly more correct (2 genuine PASSes, footer reconciles, 0 FAIL*), so RL111A's
baseline was regenerated — the prior one had captured incomplete output (the same bug, non-fatal there because
those two tests were not gating). No other baseline changed. Guard ALL GREEN: **1040 unit / 347 integration /
360 NIST** (IX 39→40), 0 regressions. **⇒ The entire DEVLOG-311 diagnosed file-I/O/IC backlog is now COMPLETE.**

