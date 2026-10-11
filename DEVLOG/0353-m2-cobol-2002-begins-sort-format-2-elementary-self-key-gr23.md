## Entry 353 — M2 (COBOL-2002) begins: SORT Format-2 elementary self-key + GR23 omitted-KEY

M1 ('85) complete; the WS-SPEC drive moves to M2 (COBOL-2002). First M2 fix — a real defect in the
already-shipped Format-2 table-SORT code (Format-2 = sort an OCCURS table in place, introduced in 2002).

`FileIoLowerer.BuildKeySpecField` computed a key's length from its StorageLocation.Length. For an elementary
OCCURS item sorted on ITSELF (`SORT WS-N ON ASCENDING KEY WS-N`), the key IS the table, whose Length spans the
whole table (ElementSize × MaxOccurs) — so the runtime decoded each per-entry buffer with an out-of-range
offset+length and threw. `BuildKeySpecField` is shared by the file and table SORT/MERGE paths, so the fix adds an
optional `entrySize` parameter clamped only on the table path (`BuildTableKeysSpec` passes
`tableItem.ElementSize`; the file path passes 0 = no clamp). A subordinate key already has element-size length,
so the clamp is a no-op there. Secondary: `FileIoBinder.BindTableSort` GR23 — with the KEY data-name omitted and
no inherent OCCURS KEY, the table item (data-name-2) is itself the key (ISO §14.9.40.4 GR23). Bug-fix to existing
functionality, no dialect gate (the cobol85-rejection of Format-2 SORT is a separate flagging item).

Tests (`SpecFixTests`): `SortTable_ElementarySelfKey_SortsInPlace` (ascending 300,100,500,200,400 → 100..500),
`SortTable_OmittedKey_UsesTableItemDescending` (omitted KEY, descending → 050,020,010).
Guard ALL GREEN: **1047 unit / 431 integration (+2) / 364 NIST**, 0 regressions.

