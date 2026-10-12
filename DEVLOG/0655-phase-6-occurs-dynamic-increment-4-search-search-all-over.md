## Entry 655 — 2026-07-07 02:41 PDT — Phase 6: OCCURS DYNAMIC increment 4 — SEARCH/SEARCH ALL over current capacity + INITIALIZE (the GR10 category-default correction)

Increment 4 of **OCCURS DYNAMIC** (ISO/IEC 1989:2023 §14.9.37 / §14.9.20 GR10; data-model **D9**): a dynamic table
can be SEARCHed (bounded over its current capacity) and INITIALIZEd. Greenfield-only.

**SEARCH / SEARCH ALL.** The table guard in both binders accepts `IsTable` (fixed OR dynamic — a dynamic table's
`Occurs` is null, so the old `Occurs is not null` skipped it and `table.Occurs!.Value` would NPE; now `Occurs ?? 0`).
`OdoModel.SearchBound` gains a dynamic branch returning `{tablePath}.Capacity` (checked before the mutually-exclusive
ODO branch) — a RUN-TIME bound threaded through the existing `BoundSearch.DependCount`, which `EmitSearch` already
prefers over the compile-time count, so the scan bounds over the current capacity (AT END fires exactly at Capacity,
never running past). `BoundSearch` gains a `DynTable` field (the table path); `EmitSearch` is refactored to
`EmitSearchScan` so a dynamic table can bracket the scan `{tbl}.EnterSearch(); try { <scan> } finally {
{tbl}.ExitSearch(); }` — the try/finally is required because the WHEN/AT-END arms `goto __searchEnd` OUT of the scan,
so ExitSearch must run on every exit path. This realizes §14.9.39 GR31: a SET Format 14 on that same table WHILE
searching raises EC-FLOW-SEARCH (fatal).

**INITIALIZE — a spec-checked correction.** The first cut wired INITIALIZE of a dynamic table to
`CobolDynTable.InitializeAll` (which re-seeds with the VALUE-inclusive grow-seed). Reading §14.9.20 **GR10** (spec
line 28023) proved that WRONG: "all the elements of the table up to current capacity … are initialized … the current
capacity is left unchanged" — where "initialized" is the INITIALIZE statement's OWN action (the CATEGORY DEFAULTS —
SPACES/ZEROES — or REPLACING / the VALUE-phrase sender), NOT the OCCURS grow-seed. So a VALUE element must go to
ZEROES/SPACES on a plain INITIALIZE, not back to its VALUE. Reimplemented as an `InitializeDynLoop(var,
{tablePath}.Capacity, body)` — a RUN-TIME-bounded loop (the sibling of the fixed-count `InitializeLoop`) whose body is
the element's normal INITIALIZE store expansion over a new `InitializeDynCursor` that yields a `DynTablePlace` (writes
through `RefReceiving`, within the 1‥Capacity bound so no growth). A group CONTAINING a dynamic table (the other GR10
case) is a variable-length group → staged LOUD (deferred to inc 5's §14.6.9 1527 family). `CobolDynTable.InitializeAll`
stays in the runtime (unused by the INITIALIZE statement now).

**Verified**: `dyn_search` (`FOUND VALUE=030 POS=03` at occ 3 via `SET WS-POS TO IDX`; `NF-99` — AT END fires because
the scan bounds over Capacity 5, not a compile-time max), EC-FLOW-SEARCH confirmed manually (SET CAP inside a WHEN of
the same table's SEARCH → fatal), `dyn_initialize` (a group element with VALUE `XYZ`/`55` → `A1/A2=[   ][00]` after
INITIALIZE — category defaults over both current occurrences, NOT the VALUE). The `EmitSearch` refactor + the
`IsTable` SEARCH guard do NOT regress fixed-table SEARCH (full greenfield conformance green). Greenfield battery: 213
unit + conformance green.

