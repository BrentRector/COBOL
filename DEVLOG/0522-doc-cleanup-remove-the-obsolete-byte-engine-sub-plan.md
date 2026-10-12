## Entry 522 — 2026-06-09 18:10 PDT — Doc cleanup: remove the obsolete byte-engine sub-plan references (they mislead)

Owner directive: "Remove all references to a historical byte-engine sub-plan. They are obsolete and can mislead." A
read-only classification workflow confirmed the hazard is real — one of its own agents was MISLED, reading
`docs/MASTER_PLAN.md` (DEVLOG 456) + the DATA_MODEL/RECORD_STRUCT docs as "current-greenfield, keep entirely",
not realizing the greenfield PIVOT (DEVLOG 457) POST-dates MASTER_PLAN and supersedes them. Executed (owner authority
over the agents' "keep"):
- **resume-prompt.md**: cut the entire "HISTORICAL byte-engine kickoff" (§0–§9: the old Mono.Cecil/data-model-migration/
  OO-on-byte mission) — 456 → 231 lines; the live top (PIVOT, STATE, version-correctness framework, two-track RESUME
  AT, NON-NEGOTIABLE PROCESS RULES) is preserved.
- **CLAUDE.md**: removed the "read MASTER_PLAN/PROJECT_PLAN first" directives and the obsolete "Session Resume Context"
  (data-model-migration) block + the stale DEVLOG-485/483 snapshots — 195 → 65 lines; TOP-LEVEL PLAN now points to
  `resume-prompt.md` + `docs/COBOLNET_DESIGN.md`; STATE → DEVLOG 520; mission = full ISO-2023 + all prior editions.
- **Obsolete plan docs** (`PROJECT_PLAN.md`, `docs/MASTER_PLAN.md`, `docs/DATA_MODEL_ARCHITECTURE.md`,
  `docs/RECORD_STRUCT_STORAGE_DESIGN.md`, `docs/DATA_MODEL_REVIEW.md`): prepended a prominent ⛔ OBSOLETE/SUPERSEDED
  banner (kept the files as DEVLOG-article historical record rather than deleting — say the word to delete outright).
- **`docs/ISO2023_CONFORMANCE_PLAN.md`**: bannered PARTIALLY-SUPERSEDED — its data-model-migration framing is obsolete;
  keep it ONLY for the M2/M3/M4 feature catalog.
- **`docs/DOC_INDEX.md`**: "Start from" → resume-prompt + COBOLNET_DESIGN; the 5 plan-doc rows marked OBSOLETE; the
  conformance-plan row → LEDGER (catalog-only); the "stack of record" note corrected (greenfield = Roslyn, not the
  Mono.Cecil byte engine).
- **MEMORY.md** (the per-session memory index): removed the 3 misleading byte-engine-plan entries (MASTER_PLAN-as-SSOT,
  data-model-migration #1 priority, conformance-plan-as-migration) → one accurate greenfield-plan pointer; fixed the
  dangling "7-stage plan below" + the north-star MASTER_PLAN reference.
Doc-only; greenfield suites unaffected. (Folds in the Entry-521 resume-doc refresh + the RECEIVE-grep-artifact
correction, below.)

