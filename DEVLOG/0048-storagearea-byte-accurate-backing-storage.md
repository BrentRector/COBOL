## Entry 048 — 2026-03-14: StorageArea — Byte-Accurate Backing Storage

StorageArea: byte array per 01-level record, space-filled by default (COBOL convention).
Field access via offset + size spans. MoveString/ReadString for alphanumeric data.

ProgramState: dictionary of named StorageAreas for a running program. GetOrCreate allocates
on first access. Static helpers MoveStringToField/MoveFieldToField for emitter to use.

This is the last piece needed to wire MOVE → real bytes → WRITE → real output. The storage
model matches the RecordLayoutBuilder's byte offsets exactly.

Next: wire MOVE to call ProgramState.MoveStringToField / PicRuntime.MoveNumeric on the
StorageArea bytes, then WRITE to output those bytes as the record.

---

