## Entry 985 — 2026-07-22 14:09 PDT — §24 fix-queue: EC-seam batch — the comprehensive pre-merge gate is ALL GREEN

Closed the EC-seam batch (F10 · V3 · V4) with the full comprehensive gate. **Greenfield Conformance 3848/3848** (the
3842 session-start baseline + exactly the 6 new EC goldens, 0 regressions) · **NIST 353 MATCH / 0 regressions** ·
**legacy Unit 1203/1203** · **legacy Integration 684/685 (1 skip) / 0 fail** · **characterization 33/33 byte-identical**.

Two process points worth recording (feedback_transparency):
1. **The GreenfieldOnly miss (caught by the guard, not the greenfield suite).** The first guard run flagged `int_rc=1`
   — 6 legacy-Integration failures that were EXACTLY the 6 new goldens. Each exercises a `>>TURN EC-… CHECKING ON`
   directive over a 2023-only construct the frozen legacy compiler cannot parse, and I'd added them to the shared
   `tests/conformance/` corpus WITHOUT the matching `GreenfieldOnly` exclusion the legacy `ConformanceTests` runner
   needs (the feedback_legacy_suite_on_shared_corpus discipline). Adding the six `("2023", …)` exclusions restored the
   legacy Integration suite to green (the greenfield CorpusRunner already byte-compares them). This is exactly the
   gap the guard exists to catch past the greenfield suite — and, per the owner's framing, those 6 were never a
   behaviour regression: the legacy oracle correctly declined to compile net-new 2023 syntax, and the fix was
   test-harness bookkeeping, not a code change.
2. **A NIST guard flake, verified serially (DEVLOG 964 discipline).** The confirmation guard re-run reported
   `NIST 352 MATCH / 1 REGRESSION` — `RL203A: COMPILE FAILED`. The only change since the clean first run was the
   test-only `ConformanceTests.cs` exclusion edit, which cannot touch the NIST leg (a separate greenfield-compiler
   path). A `COMPILE FAILED` (not a behaviour DIFF) under the JOBS=32 parallel guard is the known parallel-cold-start
   false-red. Verified serially: the greenfield `NistDifferentialTests` for RL203A passes 4/4, and the full Conformance
   NIST differential (3848/3848) already covered it. Confirmed flake, not a regression — do NOT re-baseline anything.

**⭐ THE EC-SEAM BATCH IS COMPLETE AND MERGE-READY.** ⏭ Next §24 batch: the ref-mod-externality residue (V45-sentinel /
V45-externality), then misc-semantics (V47/V5/V46/V24), minors, the doc-slice sweep, and the owner-decision items.

