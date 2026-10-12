## Entry 050 — 2026-03-14: Storage Model Wired — MOVE Writes Real Bytes

Storage model is now end-to-end:
- ComputeStorageLayout assigns byte offsets to all DataSymbols
- ProgramState allocates space-filled byte arrays
- MOVE "literal" TO field → StorageHelpers.MoveStringToField → bytes written
- WRITE record → StorageHelpers.WriteRecordToFile → reads actual ProgramState bytes

Architecture refactored per user feedback:
- ProgramState: pure data holder (no methods)
- StorageHelpers: static helpers (MoveStringToField, MoveFieldToField, etc.)
- IrMoveStringToField: embeds string value directly, avoids stack ordering issues
- IrWriteRecordFromStorage: reads from StorageLocation

NC101A now writes 36 records from actual backing storage.
Records are space-filled (default) because MOVE identifier→identifier
isn't wired yet. Next: populate records with real field data.

---

