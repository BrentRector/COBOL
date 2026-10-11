## Entry 906 — 2026-07-19 12:06 PDT — PHASE-13 comprehensive plan-vs-spec review — 28 confirmed findings (1 critical), the ledger persisted

**What ran:** an owner-directed comprehensive review of the rearchitecture plan + implementation at HEAD `38c4a669`
against `specs/ISO_COBOL.md` — a 16-finder Workflow across three axes (A: spec-coverage omissions per spec area ·
B: plan-vs-implementation claim verification · C: citation drift in landed P13 code), every finding adversarially
verified by two independent lenses (spec re-read + code re-read) with a tiebreak judge. 184 agents, 85 completed
(~7.7M tokens); a session-limit outage killed 99 agents mid-run.

**Result — persisted as `docs/rearchitecture/PHASE-13-plan-vs-spec-review.md` (LIVE ledger, per-item dispositions):**
- **28 CONFIRMED** (1 critical · 14 major · 13 minor; 26 unique). The critical: **COBOLNET1573 is shipped with two
  meanings** — the `>>REF-MOD-ZERO-LENGTH` malformed-operand error (frontend BARE string literal, Entry 897) vs the
  Wave E external-file-status-consistency error (catalog descriptor, Entry 905). Root cause: the frontend emit
  bypasses `DiagnosticCatalog`, so Wave E's catalog-only next-free scan couldn't see the earlier claim, and the
  registry drift tests scan only the catalog. Other confirmed majors: `>>DEFINE AS PARAMETER`/compile-time-expression
  silent wrong values (§7.3.11 GR4/GR6) · `>>SOURCE FORMAT` first-directive-wins (§7.3.24.3 GR1) · CC-directives-in-
  COPY never processed (§7.2.1 Step 1/2 ordering) · RW SUPPRESS has no grammar rule (hard parse error, untracked) ·
  CONFORMANCE.md false present-tense claims ('04' + the four-facility recognize-and-name) · VCR ledger 16+ stale
  `todo` anchors (the generated Gating-status index omits ALL of P13) · VCR 16 checks TYPE presence not STRENGTH
  (§13.16.3 SR13 ¶2) · VCR 86 SR6 mis-derivation (VALUE 0 vs VALUE ZERO diverge below 2023) · '04' cites §14.9.35
  (REWRITE) instead of §14.9.30 (READ) GR14 · main-program GOBACK RAISING terminates loudly instead of normally ·
  runtime-negative ref-mod length collides with the omitted-length −1 sentinel (EC-BOUND-REF-MOD can never raise for
  negatives) · stale next-free=1575 in the banner (the same stale-ledger pattern that caused the 1573 collision).
- **37 UNVERIFIED** — plausible finder reports whose verify agents died on the limit (OO surface gaps, EC seam gaps,
  intrinsics national/PHYSICAL legs, OPTIONS INITIALIZE dropped in codegen, audit re-tally 53/16/1/1, …). Routed to
  Wave I for the verify pass. **3 finders never ran** (A3 §9/§12 sweep, A4 §13 clause sweep, C4 Wave C re-derivation)
  — those areas are unreviewed; Wave I re-runs them.
- **Recurring theme (the P10–P13 lesson again):** 6 confirmed wrong-§/wrong-annex citations shipped in
  otherwise-correct code; every one was caught by re-reading the spec text, none by any test.

**Routing (the ledger §6):** FIX NOW (1573 renumber + doc/citation remediation) · a spec-derive review-fix wave
(VCR 16 strength leg, VCR 86 re-derivation, GOBACK RAISING, ref-mod negative-length, EXCEPTION-FILE r2a legs) ·
RW SUPPRESS → the grammar batch · directive fixes → Wave D · everything unverified → Wave I. DOC_INDEX row added
(review-findings ledger family).

