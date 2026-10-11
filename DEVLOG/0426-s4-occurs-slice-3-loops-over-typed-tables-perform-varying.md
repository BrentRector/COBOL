## Entry 426 — S4: OCCURS slice 3 — loops over typed tables (PERFORM VARYING works; SEARCH safely stays byte)

Verified and locked in the loop-over-typed-table behavior — no codegen needed, it falls out of slice 1's generalized
primitives:
- **PERFORM VARYING over a typed table works** byte-identically: the loop variable is a typed `long`, and `COMPUTE
  T(I) = …` / `ADD T(I) TO …` inside the loop go through the typed element location. Probe + flip test
  (`PerformVaryingOverTypedTable_ByteIdentical`) confirm `T(I)` writes/reads across all 4 elements identically.
- **SEARCH / INDEXED BY tables safely stay byte**: a `SEARCH T` statement references the table as a *whole* operand,
  so the §9.3 whole-table demotion fires and the table is byte-backed — SEARCH keeps working via the byte engine,
  byte-identical with the flag on or off (flip test `SearchOverIndexedTable_StaysByteBacked_ByteIdentical` confirms,
  and reflection shows no `_TA_` field). Typed SEARCH-on-array is a later enhancement; for now the migration never
  breaks SEARCH.

Test-only commit (the capability already emerged from the slice-1/2 substrate). 22 flip tests, all flag-on≡flag-off.
Guard **ALL GREEN: 1196 unit / 504 integration / 364 NIST**. OCCURS is now well-covered (char + numeric tables;
subscripted + PERFORM-VARYING access; SEARCH safely byte). NEXT: nested groups (group-of-subgroups → nested record
structs), then Stage-4 pointers/OO.

