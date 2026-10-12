## Entry 919 — 2026-07-19 18:00 PDT — The §11 ANALYSIS BACKLOG recorded (10 missing analyses, owner-directed) + integrated into the work plan

The owner asked which analyses are useful but missing, then directed a durable record integrated into the
plan. **New plan §11** — the register of ten known analytical blind spots, each with why-it-matters, its
output, a SCHEDULED HOME, and a PENDING status (flipping a row is a plan edit; silent drops forbidden):
A1 per-edition authority sufficiency (can 85/2002/2014 conformance be derived from the sole in-repo 2023
spec? may overturn council decision #1) · A2 the SR-enforcement census · A3 the numeric-semantics depth audit
(§8.8.1 end-to-end) · A4 the external differential corpus (GnuCOBOL + the NIST module-coverage map) · A5 the
runtime isolation/hosting-contract audit (the full statics census + the written contract) · A6 the
performance benchmark + generated-code cost model · A7 mutation-testing of test-suite strength · A8 the
interpretations-register extraction · A9 the developer-experience analysis · A10 the effort model.

**Work-plan integration:** A1/A2 became **P14 Step 0a/0b** (companions to the traceability inventory —
0a stamps every pre-2023 row with its derivation authority; 0b deepens rows to SR granularity, feeding the
negative corpus). A6 became **P14 Step 12** (the perf gate: benchmark suite + cost model, CI-trended,
escalating architecture surprises BEFORE the legacy comparison dies). **P15 gained a ⛔ preconditions gate:**
the oracle deletion shall not start until A3/A4/A5/A6 are complete (each is cheap only while the legacy
comparison exists); A8 executes inside P15's conformance-doc step. A5 executes WITH the review-ledger §24
tier-7 fix unit. A10 is due at the next §0 update. §0 and the §4 P14 row point at the backlog.

