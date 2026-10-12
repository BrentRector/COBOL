## Entry 012 — 2026-03-13: File I/O Code Generation — From Parse to Emit to Output

### What Changed

Implemented real CIL code generation for file I/O — the largest block of technical debt.
This required changes across 4 layers:

1. **Runtime** (CobolField.cs): Added `SetFromBytes(byte[])` and `CopyToBytes(byte[])` for
   record buffer ↔ field data transfer. (CobolProgram.cs): Added `FileReadNext`,
   `FileWrite`, `FileRewrite` helper methods that bridge CobolFileManager operations with
   CobolField byte operations.

2. **Semantic Analyzer**: Fixed `AnalyzeProgram` to build symbols from FILE SECTION and
   LINKAGE SECTION entries, not just WORKING-STORAGE. Without this, record fields declared
   under FD were unknown to the symbol table and the emitter couldn't create fields for them.

3. **CIL Emitter**:
   - Imports 12 new runtime types/methods (CobolFileManager, SequentialFileHandler, etc.)
   - `EmitFileManagerInit`: Creates `_fileManager` field, instantiates handler per SELECT
     entry, registers each handler, creates byte[] buffer fields per file
   - `EmitOpenStatement`: Calls `fm.Open(fileName, mode)`, stores FILE STATUS
   - `EmitCloseStatement`: Calls `fm.Close(fileName)`, stores FILE STATUS
   - `EmitReadStatement`: Calls `FileReadNext(fm, name, buf, recField)`, handles INTO
     clause, emits AT END / NOT AT END branching with status == "10" check
   - `EmitWriteStatement`: Handles FROM clause, calls `FileWrite`
   - `EmitRewriteStatement`: Same pattern as WRITE
   - `EmitDeleteStatement`: Calls `fm.Delete(fileName)`
   - `EmitGoToDependingStatement`: Emits CIL switch opcode (jump table) — evaluates
     expression, subtracts 1 for 0-based index, switches to paragraph call + ret

4. **Integration test**: `FileIO_WriteAndReadBack` — writes two records to a LINE
   SEQUENTIAL file, closes, reopens for INPUT, reads back, verifies first record content.
   This exercises OPEN OUTPUT, WRITE, CLOSE, OPEN INPUT, READ with AT END, DISPLAY.

### Current Score
- 28 fully implemented statements (was 20)
- 3 partial (REWRITE, DELETE need indexed file testing; CALL is a stub)
- 10 stubs with runtime warnings (down from 23 at the start of this session)
- 22 integration + 141 unit = 163 total tests, all passing

---

