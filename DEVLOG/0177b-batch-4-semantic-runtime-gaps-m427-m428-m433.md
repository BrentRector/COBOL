## Entry 177 — 2026-03-31: Batch 4 — Semantic/Runtime Gaps (M427, M428, M433)

Three items closed:

**M428 (SYMBOLIC CHARACTERS N:N validation)**: Added count-equality check in
SemanticBuilder for symbolicCharacterEntry. When name count != ordinal count, emits
diagnostic per ISO §12.3.7 rule 16c. 3 integration tests (valid N:N, valid single,
count mismatch).

**M427 (SORT Format 2 table sort)**: Full binder→lowerer→emitter pipeline for in-place
table sort. FileIoBinder.BindSort now detects data-item targets (not files) and creates
BoundTableSortStatement. IrTableSort IR instruction carries storage location, entry size,
count, and keys spec. SortRuntime.SortTable extracts OCCURS entries to temp array, sorts
with StableSort (same LINQ OrderBy/ThenBy as file sort), copies back. CilFileIoEmitter
emits call to SortRuntime.SortTable. 2 integration tests (ascending, descending).

**M433 (Lexer keyword shadowing)**: Audit confirmed all 15 screen tokens + COLUMN are in
both cobolWord and _dataNameTokens. No NIST tests use these as data names. Already fully
mitigated in Batch 3. Closed.

**Deferred items**: M432 (multi-char currency, COBOL-2002+, P3), M429/M430/M431 (screen
I/O runtime + CRT STATUS + CURSOR, P2/P3) — grammar/semantics complete, runtime pending.
These are Batch 5 candidates.

Regression: 922 unit + 334 integration + 95 NIST = 0 failures.

---

