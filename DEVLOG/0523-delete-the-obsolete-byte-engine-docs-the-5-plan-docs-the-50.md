## Entry 523 — 2026-06-09 18:28 PDT — DELETE the obsolete byte-engine docs (the 5 plan docs + the ~50 CobolSharp* architecture/guide docs)

Owner directive (after the Entry-522 banner pass): "Delete them" — both the 5 obsolete plan docs AND the CobolSharp*
byte-engine architecture/guide set. Done via `git rm` (55 files): `PROJECT_PLAN.md`, `docs/MASTER_PLAN.md`,
`docs/DATA_MODEL_ARCHITECTURE.md`, `docs/DATA_MODEL_REVIEW.md`, `docs/RECORD_STRUCT_STORAGE_DESIGN.md`, and all 50
`docs/CobolSharp*.md` / `docs/Cobolsharp*.md` (the byte-engine ExecutionContext/StorageBlock/CIL-backend/numeric-engine/
runtime/debugger/optimizer/packaging/cookbook/handbook/… docs). `docs/` went 123 → 69.

**Preserved (verified):** the greenfield design corpus `docs/COBOLNET_*` (14 docs), the version-correctness docs
(`VERSION_CHANGE_REFERENCE.md`, `VERSION_TEST_MATRIX_DESIGN.md`), the M2/M3/M4 feature catalog
(`ISO2023_CONFORMANCE_PLAN.md`, bannered), `DOC_INDEX.md`, `DEVLOG.md`, `resume-prompt.md`, `CLAUDE.md`, `README.md`,
and the ISO spec submodule. `DOC_INDEX.md` cleaned (55 table rows dropped via a path-existence filter; the top banner +
"How to use" + the dangling `MASTER_PLAN §2` mentions rewritten to "deleted"). Dangling refs fixed in the LIVE docs
(`CLAUDE.md`, `MEMORY.md`, `docs/COBOLNET_ARCHITECTURE.md`). Doc-only; greenfield build/suites unaffected.

NOTE: a residual set of OLDER byte-engine-era docs that are NOT CobolSharp*-named survives (e.g. `AUDIT_REPORT.md`,
`docs/BATCH5-*`, `docs/M407/M411-*`, `docs/TERMINAL-*`, `docs/SEMANTIC-ANALYSIS-ARCHITECTURE.md`, `docs/BINDER-DESIGN.md`,
`docs/IL-BYTECODE-GENERATION-DESIGN.md`, `docs/OO_IMPLEMENTATION_DESIGN.md`, `docs/REPORT_WRITER_*`, the
binder/boundtree/cilemitter/ir decomposition docs, the SPEC_FIX/COMPLIANCE/MULTIVERSION plans) — these were outside the
stated delete scope and carry dangling refs to the now-deleted plan docs; flagged for an owner decision on a further sweep.

