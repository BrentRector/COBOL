## Entry 344 — WS-SPEC #5: cross-program GLOBAL FD I-O for indexed/relative (Layer-1 + Layer-2)

Backlog #5 (M1/'85). A contained program reading a containing program's FD … IS GLOBAL file. Diagnosed by
running both an INDEXED and a RELATIVE test:

- **INDEXED already worked.** The prime RECORD KEY lives in the global record, which InheritGlobalItems
  already inherits, and CBL0702 (file-not-open) is a *Warning* — non-fatal — so the contained keyed READ
  compiled and the runtime connector (name-keyed shared _manager, container's OPEN) was found. Kept the test
  as a regression guard.
- **RELATIVE failed.** `CBL3128: Undefined data-name 'RL-KEY'` in the contained program: the RELATIVE KEY is
  a separate WORKING-STORAGE item, not subordinate to the global record, so the FD's GLOBAL clause does not
  make it a global name — and the contained program's keyed I/O needs it.

Two layers:
- **Layer-2 (functional fix).** `Compilation.InheritGlobalItems` now also inherits the global file's RELATIVE
  KEY DataSymbol + StorageLocation (tagged with the owner program's id so it reads the container's bytes); and
  `CollectInheritedGlobalNames` yields the relative-key name so CBL3128's whitelist accepts it. ISO §9.1.5(2):
  contained programs share the global file connector; the relative-record number must come from the shared key.
- **Layer-1 (soundness).** `FileStateValidator.CheckFileOpen` now takes the FileSymbol and early-returns for a
  GLOBAL file: a per-program forward walk can't see that a containing program opened it, so CBL0702 is unsound
  for globals (the runtime status code still surfaces a genuinely-unopened file). Removes the false warning.

Tests: `NestedProgram_ReadsContainingGlobalIndexedFile_SharesConnector`,
`NestedProgram_ReadsContainingGlobalRelativeFile_SharesConnectorAndKey`.
Guard ALL GREEN: **1047 unit / 422 integration (+2) / 364 NIST**, 0 regressions.

