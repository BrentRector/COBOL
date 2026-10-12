## Entry 298 — Index-name of a LINKAGE/FILE table must live in WORKING-STORAGE → IC106A/IC207A (NIST 317→319)

The two IC crashes from Entry 297 (IC106A, IC207A — both rc=139 "segfault") shared one root cause, and it
was a real compiler bug, not a CALL-mechanism gap. The "segfault" was actually an unhandled
`NullReferenceException` in `PicRuntime.EncodeCompBinary` (the process exits 139): the callee did
`SET <index-name> TO <index-data-item>` and the **destination index-name resolved to a null storage area**.

Minimal repro (works standalone, crashes only across a CALL): a callee with
`01 L-GRP … 02 L-ELT PIC X OCCURS 10 INDEXED BY L-IX` in its **LINKAGE SECTION** + `SET L-IX TO L-IDN`.
`SET <index> TO <index>` in plain WORKING-STORAGE already worked — the crash needed the table (hence its
index-name) in LINKAGE.

Root cause in `SemanticBuilder` (the INDEXED-BY index-name declaration): the synthesized 77-level S9(9)
COMP index-name symbol was tagged `indexSym.Area = _currentArea` — i.e. it inherited whatever section the
OCCURS table was in. For a LINKAGE table that made the index-name a LINKAGE item, but an index-name is
**never a USING parameter**, so `StorageLayoutComputer`'s linkage pass laid it out yet nothing ever bound
its area to a caller pointer → the resolved `area` byte[] was null → NRE the moment it was written. (A
FILE-SECTION table's index-name would likewise have been pointed at the shared record buffer.)

Fix (one line + rationale comment): an index-name is compiler-allocated implementation storage owned by
**this** program (ISO §8.5.1.2), distinct from any data item and not passable — so it always needs real
backing storage. Allocate it in `StorageAreaKind.WorkingStorage` unconditionally, regardless of where the
associated OCCURS table is declared. Index-names of WORKING-STORAGE tables (the overwhelming majority,
already `WorkingStorage`) are unaffected; only LINKAGE/FILE/LOCAL-table index-names move — and they could
never have worked before (null area). The layout computer already buckets purely by `data.Area` and
index-names resolve as independent by-name symbols, so the change is self-contained.

IC106A 14/14 (SEPARATE INDEXES / INDEX DATA ITEM / SUBPROGRAM INDEX / TABLE REFERENCES / REDEFINED ITEM —
the "SEPARATE INDEXES" pass directly confirms the index-name now has its own storage, distinct from the
caller's index) and IC207A 11/11 (variable-length OCCURS-DEPENDING table + condition-names in a LINKAGE
param). Both non-vacuous: a no-op transfer would mis-index the LINKAGE table and fail. Baselined both
(deterministic, 0 FAIL\*). Guard ALL GREEN: 1000 unit / 347 integration / **319 NIST**, 0 regressions —
confirming no existing index-name (working-storage) test moved.

