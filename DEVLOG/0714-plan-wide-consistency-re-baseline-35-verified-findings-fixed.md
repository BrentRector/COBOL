## Entry 714 — 2026-07-09 13:05 PDT — Plan-wide consistency re-baseline: 35 verified findings fixed across 19 docs; local-model experiment closed

**Trigger.** The owner directed a full re-evaluation of the rearchitecture planning corpus (Fable 5, ultracode) and
then "completely update all necessary sections so future sessions follow up-to-date and accurate guidance." A
42-agent review workflow (`wf_d74af639`: six dimensions — master-plan self-consistency, pipeline-description
consistency, phase-doc alignment, ordering/ownership, docs-vs-reality, omissions — every finding adversarially
verified refute-by-default) confirmed **35 findings**; a 34-agent fix workflow (`wf_f01623da`: one editor + one
checker per document, all under a shared decision canon, in an isolated `main` worktree) applied them.

**The headline defects fixed.** (1) OWNERSHIP was contradictory: the plan's §4 driver note assigned the binder-Check
relocation + edition-agnostic binder to P06 (and bind/emit to P07) while the banner/design assigned all of it to P03 —
resolved: **P03 delivers the ENTIRE version-conformance pipeline residue-first as Steps 12–15** (12: Batch C — RETRY
across SIX grammar sites incl. the previously-missed `deleteFileStatement:425`, + the boolean family, + the deferred
code-side staleness sub-items; 13: delete `ReservedWordEditionHints`, vendor JSON/XML COBOL0313 → `CobolErrorStrategy`;
14: the skeleton — `VersionConformancePass`, ALL **88** compiler-embedded Check sites funneled [not the understated
"~24"; incl. EditionValidator's own + one emitter-side site in `CSharpEmitter.Call.cs`], EditionValidator absorbed+
deleted, `.Syntax` back-ref, bind/emit split, CheckOnly/EditionHarness/INV-1 legs re-pointed; 15: close with 3 new
exit criteria); P04 = superset completion + the construct-id annotation convention + D10; P06/P07 = preserve-and-harden
(named pass in the manifest, zero-Check exit criterion; no gating in emitters). (2) PHASE-03's own STATUS said "RESUME
AT: Step 11 (phase close)" — a fresh session following the §0 protocol would have closed the phase without building
the pipeline core; rewritten to current truth + Steps 12–15. (3) Stale mechanisms presented as design: SSOT §2
({isXXXX()}? + binder gating), COBOLNET_PIPELINE_DESIGN (5-phase, no pass), DESIGN-frontend-grammar D1/D3/§3.2
(forward gate-stamping), DESIGN-edition-framework + module-topology + VALIDATION_DESIGN (ReservedWordEditionHints
"PERMANENT" / EditionValidator as the vehicle), PHASE-13 Wave C (mandated NEW parse predicates + binder Checks for M4 —
now: constructs.json row + superset rule/annotation + a pass rule + per-edition fixtures, never predicates/binder
Checks), PHASE-14 (validator refs), PHASE-04 (predicate-preservation disclaimers), DATA_MODEL/OO deep-dives
(EditionGateHints recipes → the real bind-time Checks). (4) `retryPhraseAhead()` was under-specified — canonical spec
now: complete retry tail AND `openFileSpec+` must stay satisfiable (so `OPEN INPUT RETRY FOREVER.` stays two file
names at 85; ambiguous `RETRY FOREVER F.` resolves by edition; fail-safe). (5) Design §5 re-sequenced natively to the
residue-first order (Stages 1→2→3), batch note = landed (DEVLOG 709–713) vs remaining. (6) Count/staleness sweep:
battery counts now live ONLY in status banners (principles state composition), CLAUDE.md's "EXECUTION NOT STARTED"
fixed, resume-prompt's 9→10 design count + stale battery figure fixed, DOC_INDEX validation/pipeline rows fixed.

**Local-model experiment CLOSED.** The 3-day aider/Ollama experiment produced one analysis file (branch
`local-model-wip`, commit e34d4ab6); the audit found it directionally right but untrustworthy in detail (omitted the
`deleteFileStatement` site, fabricated comment text in its grammar quotes, wrong line anchors) — its corrected
superset is now IN PHASE-03 Step 12, so the branch is deleted unmerged and the handoff kit (HANDOFF-LOCAL-MODEL.md,
LOCAL-MODEL-JOURNAL.md) retired per its own §7. Docs-only change set; battery untouched (3112 · 227 · 32 · guard ALL
GREEN at head).

