## Entry 104 — 2026-03-17: Complete File I/O — DELETE, START, WRITE FROM, OPEN I-O

Closed all remaining file I/O gaps. The compiler now supports the full COBOL-85 file subsystem
across sequential, relative, and indexed organizations.

**WRITE FROM** — bound node already had `From` property but binding hardcoded it to null.
Fixed: `BindIdentifierWithSubscripts(fromCtx.identifier())`, lowering emits IrPicMove from
source to record before the IrWriteRecordFromStorage. One-line fix in binding, three lines in
lowering.

**DELETE** — full pipeline: BoundDeleteStatement, IrDeleteRecord IR instruction, LowerDelete
with INVALID KEY / NOT INVALID KEY branching (mirrors READ's AT END pattern),
EmitDeleteRecord calls FileRuntime.DeleteRecord. Runtime delegates to handler.Delete().

**START** — full pipeline: BoundStartStatement, IrStartFile IR instruction with key location
and condition, LowerStart with INVALID KEY branching, EmitStartFile pushes key area/offset/length
+ condition int, calls FileRuntime.StartFile. Fixed IndexedFileHandler.Start to not consume the
first matching record (was calling MoveNext in Start, then ReadNext called it again, skipping
the positioned record). Also fixed to enumerate ALL records from match point onward, not just
matching records.

**OPEN I-O** — added `I_O : 'I-O'` lexer token, added `I_O` to parser's `openMode` rule,
binder maps "I-O" to OpenMode.IO. All three handlers already supported InputOutput mode.

**Organization-aware file registration** — the entry point was creating SequentialFileHandler
for ALL files regardless of ORGANIZATION. Added `RegisterFileHandlerWithOrg` that dispatches
on organization string to create the correct handler. For INDEXED files, resolves RECORD KEY
to get key offset/length from storage layout.

**Record length from FD** — was defaulting to 132 for all files. Now computed from the FD
record's storage location length.

**Pre-existing runtime bugs fixed:**
- IndexedFileHandler.Start consumed first matching record, causing READ NEXT to skip it
- IndexedFileHandler.Start only enumerated condition-matching records, not all subsequent records

143 integration tests (3 new: WRITE FROM, DELETE indexed, START indexed), 1 skip, all green.

---

