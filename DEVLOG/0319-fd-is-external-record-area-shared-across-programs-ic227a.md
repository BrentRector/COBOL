## Entry 319 — FD ... IS EXTERNAL record area shared across programs → IC227A baselined (NIST 356→357)

Third parallel-implementation fix. **IC227A** (3 FAIL*) — the record area of an `FD … IS EXTERNAL` file is not
shared across the separately-compiled programs of a run unit; only `01 … EXTERNAL` items in WORKING-STORAGE
were. The 3 failures were exactly the cross-program record-AREA tests (a subprogram MOVE to EXTERNAL-FILE-RECORD
not seen by the main; a READ into the subprogram's own record area not seen by the main). ISO §8.6.3/§8.6.4/
§13.18.22: an EXTERNAL file's records and subordinate items attain the external attribute — one representation
per run unit. `SemanticBuilder.VisitFileDescriptionEntry` read only the GLOBAL alternative of
`fileGlobalExternalClause`; `EmitExternalStorageInitialization` registered only `Area==WorkingStorage`. Fix (6
files): add `FileSymbol.IsExternal`; `SemanticBuilder` reads the EXTERNAL alternative and propagates it onto the
FD's level-01 record `DataSymbol.IsExternal` (the externalized key is the record NAME, §13.18.22);
`SymbolValidator` no longer flags a FileSection level-01 EXTERNAL record; `EmitExternalStorageInitialization`
also registers EXTERNAL `Area==FileSection` 01s; **a `StorageAreaKind Area` discriminator was added to the
`_externalRanges`/`ExternalRanges` tuple (CilEmitter + EmissionContext) and to the `CilLocationEmitter` redirect
gates** — REQUIRED, else a FileSection range at offset N would wrongly redirect a WORKING-STORAGE reference at N
and corrupt IC226A. IC227A → 019 OF 023, 0 FAIL*, baselined (after IC226A); **IC226A (EXTERNAL WS) stays MATCH**,
confirming the Area discriminator. Guard ALL GREEN: **1040 unit / 347 integration / 357 NIST** (IC 20→21), 0
regressions.

