## Entry 302 — Architecture assessment (NO rewrite) + P0 validation-integrity hardening (NIST 350→347 honest)

The owner reframed the goal: a **production-quality, commercial-level COBOL-85 compiler, extensible to later
ISO specs**, explicitly putting "a complete refactor or rewrite" on the table. Rather than assume either
answer, ran two parallel evidence-based agent workflows over the real codebase: a **10-dimension architecture
audit** (each auditor read actual code, cited file:line, gave a per-layer verdict) and an **11-test
root-cause diagnosis** of every remaining in-scope NIST failure. Full synthesis written to
`docs/ARCHITECTURE_ASSESSMENT.md`.

**Verdict: DO NOT REWRITE.** Across 10 independent layer audits: **8× targeted-refactor, 2×
incremental-improvement, 0× rewrite.** The architecture is a sound, textbook layered compiler; the debts are
localized and fixable in place; and the 350 NIST + 1347 tests encode hard-won correctness a rewrite would
endanger for no structural gain (Spolsky). The real gap is not the design — it is that the compiler was driven
by NIST (which only feeds VALID programs), so it is strong at *accepting valid input* and under-invests in
(T1) diagnostics/robustness on *invalid* input, (T2) validation integrity, (T3) structural self-verification
of generated code, with (T4) a few latent correctness bugs masked by NIST's data, (T5) some per-feature
structural tax, and (T6) in-memory file engines. The "later-spec" extensibility is currently scaffolding
(orphaned 2002/2014/2023 grammars not in the build) — the seams exist, the implementation above 85 does not.

**P0 acted on immediately — the audit proved the validation harness was lying.** The guard greped per-paragraph
`FAIL*` detail lines but never parsed the AUTHORITATIVE CCVS footer total, so a test that fails-to-execute
("000 OF 001 EXECUTED" + "001 TEST(S) FAILED") with zero FAIL* detail passed as MATCH. A full baseline sweep
found three false-greens in the headline "350":
- **SQ212A** (0-byte baseline — introduced THIS session in the SQ batch): it does not produce a report, it
  **crashes** — `ArgumentException: Source array was not long enough` in `FileRuntime.WriteRecordVariable`
  (variable-length sequential WRITE of a shorter-than-max record). My determinism check passed it vacuously
  (empty == empty). A genuine compiler bug, now tracked as remaining work, not a pass.
- **IX108A** (pre-existing): footer `000 OF 001 EXECUTED / 001 TEST(S) FAILED` — a real failure that slipped
  in at DEVLOG 277 because the body had no FAIL* detail line.
- **NC303M** (pre-existing): 0-byte baseline for an obsolete-feature *flagging* module that emits no report at
  all (its sibling NC302M/NC401M produce small non-empty outputs); the empty baseline matched vacuously.

Hardened `scripts/guard.sh` so this cannot recur: (1) the per-test check now also parses the footer
`NNN TEST(S) FAILED` total → REGRESSION on any nonzero ("NO TEST(S) FAILED" is treated as zero); (2) the
final baseline-integrity loop now rejects any **0-byte** baseline AND any baseline whose footer reports a
nonzero failed total, in addition to the existing FAIL*-detail rejection. Removed the 3 false-greens from
`NIST_TESTS` and deleted their baselines. **Inspection/flag modules with "000 OF nnn EXECUTED / nnn TEST(S)
REQUIRE INSPECTION" (SM106A, SQ101M, SQ207M–210M, IX111A) are legitimate** (0-executed is their correct CCVS
output, "NO TEST(S) FAILED") and were verified to still pass the hardened checks.

Guard ALL GREEN at the **honest 347** (93 NC + 42 IF + 15 SM + 18 IC + 82 SQ + 26 RL + 39 IX + 29 ST + 3
OBSQ); 1000 unit / 347 integration unchanged. The −3 is not a regression — it is the headline becoming
truthful. SQ212A/IX108A and the deferred RL/IC gaps are now the diagnosed feature-completion backlog
(docs/ARCHITECTURE_ASSESSMENT.md). Next: P1 (diagnose-on-invalid-input) and the high-confidence feature fixes.

