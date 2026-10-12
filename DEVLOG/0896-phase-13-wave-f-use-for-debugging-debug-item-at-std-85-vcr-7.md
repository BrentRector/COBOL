## Entry 896 — 2026-07-18 17:26 PDT — PHASE-13 Wave F — USE FOR DEBUGGING + DEBUG-ITEM at --std 85 (VCR 7.17) — parallel worktree + adversarial review → integrated

Landed the X3.23-1985 debug module at `--std 85` (deleted 2002, absent ISO-2023 — the 1985 standard is the authority):
the USE FOR DEBUGGING ON procedure-name / ALL PROCEDURES trigger leg + the DEBUG-ITEM special register. A REMOVAL
gate (85 accepts + models; >=2002 rejects, COBOLNET0902). **This wave is the first end-to-end proof of the
parallel-worktree + adversarial-review execution model** ([[feedback_execution_model_tiered_parallel]]).

**Process (the model working).** Delegated to a worktree agent (isolation:'worktree', run in parallel with the main
session's EC-BOUND-REF-MOD wave — file-disjoint). The agent implemented + self-gated (its own conformance 3645). A
5-agent adversarial review workflow (5 lenses → per-blocker verification) then found **2 CONFIRMED BLOCKERS + 4
should-fixes** the agent's green battery had NOT caught: (1) DEBUG-LINE reported the SUBJECT procedure's line for every
cause, but X3.23-1985 (DB101A:611-614) requires the CAUSING statement's line; (2) `DebugRegisterPlace` carried a raw
C# expression string (`__dbgItem.Image` …) — a backend-neutrality leak. Sent the agent back with precise spec-first
fixes; it fixed all 4, re-gated (conformance 3647 in-worktree); I RE-VERIFIED both blockers against the primary
sources myself (DB101A:611-617 confirms the PERFORM-line requirement; the neutralized `DebugRegisterPlace(DataItem,
DebugRegisterMember enum)` confines the C# text to `PlaceRenderer.DebugRead`) before squash-integrating. **Two spec
violations were caught before they reached the mainline** — the safety the pattern exists for.

**As-built.** Runtime `DebugItem` register (fixed-width string members; DEBUG-SUB-1/2/3 = S9(4) SIGN LEADING SEPARATE
per MOVE GR6a §14.9.25.4, width 5 / group 86) → the backend-NEUTRAL `DebugRegisterPlace`. DEBUG-CONTENTS taxonomy
(START PROGRAM / FALL THROUGH / PERFORM LOOP / SPACES) + DEBUG-LINE causing-statement, both threaded through every
transfer of control (fall-through, GO TO, GO TO DEPENDING, altered GO TO, per-PERFORM `__dbgFirst`, EXIT PARAGRAPH,
NEXT SENTENCE) via a defaulted `SourceLine` on the transfer bound nodes. Two-switch model: WITH DEBUGGING MODE
(compile-time, gates scaffolding — zero-scaffolding invariant, characterization 33 byte-exact) + `RunUnit.DebugMode`
(object-time, default ON). Loud-staged **COBOLNET1571** (ERROR, not the scout's WARNING — the anti-silent-wrong-answer
call the review confirmed sound): data-name/file/cd subjects + SORT/MERGE-procedure overlap. Docs: constructs.json
`use-for-debugging-removed-2002` + ConstructRegistry regenerated (0902 message current), COBOLNET_VALIDATION_DESIGN
row-7.17, DIAGNOSTICS.md; the dead 0899 debug-register descriptor retired → DebugSubFacilityStaged (1571). Diag-band
reconciliation: main used 1570 (free) for none, Wave F took 1571; Wave G CLASS A will use 1570/1572.

NO grammar change (legacy + NIST unaffected). Battery on the integrated tree (main + Wave F squashed): conformance
**3648** · characterization 33 · Wave F/drift 39 · unit 311. Worktree history `d8284660` (impl) + `1779763a` (review
fixes), squashed to one commit on `phase-13-m4-2023`.

