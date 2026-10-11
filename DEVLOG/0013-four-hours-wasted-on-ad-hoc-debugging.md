## Entry 013 — 2026-03-14: Four Hours Wasted on Ad-Hoc Debugging

### The Failure

Spent approximately four hours trying to fix a parser infinite loop that prevents
compilation of NIST test programs. The approach was wrong from the start:

1. Launched 391 NIST programs in parallel — overwhelmed the system
2. Switched to sequential with timeouts — still wrong approach
3. Added "safety advance" workarounds instead of fixing root cause
4. Guessed at what paragraph headers look like instead of reading the spec
5. Added Console.Error traces, then file-based traces, then flushed traces —
   chasing the symptom through 10+ edit-build-run cycles
6. Never identified the actual bug despite narrowing it to the IF statement's
   interaction with period-terminated scope closing

### Root Causes Identified But Not Fixed

The parser has a fundamental design flaw: `ParseStatements` doesn't correctly implement
COBOL's sentence/scope termination model from the spec (§14.5). Specifically:

- A period terminates the current sentence and closes ALL open scopes
- `ParseStatements` was consuming periods and continuing, which causes nested
  statement parsers (IF, PERFORM, etc.) to never terminate when period-terminated
- The fix attempts (returning at period, adding period as terminator) caused
  other loops to break because the paragraph-level loop expects to consume periods

### What Should Have Been Done

1. Read the spec grammar for sentences, statements, and scope termination (§14.5)
2. Design the scope model correctly from the start
3. Implement it once, test it against NIST
4. Never add "safety advance" workarounds

### Process Failures (cumulative this session)
- Entry 009: Parsing without code generation (4 statements)
- Entry 010: 23 NOP stubs across all phases
- Entry 011: EmitRuntimeWarning is not code generation; test workaround
- Entry 012: File I/O implementation (actually a success)
- Entry 013: Four hours of ad-hoc debugging without progress

---

