## Entry 010 — 2026-03-13: Massive Oversight — 23 Statement Types With No Code Generation

### The Scale of the Problem

A full audit of the CIL emitter revealed that **23 out of 40 parseable statement types**
emit `nop` — they parse correctly, compile without error, and produce programs that silently
skip the statement at runtime. This isn't a handful of edge cases. It's the majority of the
language.

The NOP stubs span every major feature area:
- **Core statements**: ACCEPT, INITIALIZE, CALL, STRING, UNSTRING, INSPECT, GO TO DEPENDING
- **File I/O (7 statements)**: OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START
- **Sorting**: SORT
- **Table handling**: SEARCH
- **Archaic**: ALTER
- **Report Writer**: INITIATE, GENERATE, TERMINATE
- **OO COBOL**: INVOKE
- **Exception handling**: RAISE, RESUME

This happened because the parser was built feature-by-feature across 6 phases, and each
phase added parsing without always adding the corresponding code generation. The CIL emitter
grew a `case` for each new AST type with `_il!.Emit(OpCodes.Nop)` as a placeholder, and
many were never revisited.

### Why This Is Worse Than Entry 009

Entry 009 documented 4 statements (MULTIPLY, DIVIDE, SET, EVALUATE) that parsed without
emission — caught and fixed in the same session. This audit reveals the problem was
**systemic from Phase 3 onward**. The test suite verified that programs compiled and ran,
but the programs weren't doing what the COBOL source said. Any COBOL program using CALL,
STRING, INSPECT, or file I/O would compile "successfully" and produce silently wrong results.

### The Fix

Implementing real code generation for all 23 NOP stubs. Every fix includes a runtime
method (if needed) and an output-verifying integration test.

---

