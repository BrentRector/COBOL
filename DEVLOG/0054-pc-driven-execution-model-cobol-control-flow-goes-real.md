## Entry 054 — 2026-03-15: PC-Driven Execution Model — COBOL Control Flow Goes Real

### The Problem

NC101A compiled and ran, but produced empty or placeholder output. The root cause
was architectural: the compiler treated each COBOL paragraph as an isolated method
called only from Main. But COBOL's execution model is **sequential fall-through** —
paragraphs execute in declaration order unless redirected by GO TO, PERFORM, or
STOP RUN. Our Main only called the first paragraph (OPEN-FILES) and returned.

### The Solution: Program Counter Dispatch

Redesigned the runtime model around a program counter (PC):

**Paragraph methods return `int` (next PC):**
- Fall-through: `return myIndex + 1`
- GO TO PARA-X: `return indexOf(PARA-X)`
- STOP RUN: `return -1`

**Main becomes a dispatch loop** using CIL `switch` opcode:
```
int pc = 0;
while (pc >= 0 && pc < N)
    pc = paragraphs[pc]();
```

This required changes across 3 files:
- **IrInstruction.cs**: Added `IrReturnConst(int)` and `IrParagraphDispatch`
- **Binder.cs**: Paragraph index tracking, PC-based GO TO/STOP RUN/fall-through,
  PERFORM THRU calls range of paragraphs sequentially
- **CilEmitter.cs**: `EmitParagraphDispatch` generates CIL switch table,
  `EmitPerform` pops int return value from paragraph calls

### IF Branching — Block-Structured Control Flow

The previous session's hung state had partially implemented IF branching. Completed it:

- `LowerIf` creates basic blocks: `if.then`, `if.else`, `if.join`
- Emits `IrBranchIfFalse(condVal, elseOrJoinBlock)` for conditional skip
- `IrJump(joinBlock)` at end of then/else for reconvergence
- `LowerCondition` handles: identifier vs identifier (IrPicCompare),
  identifier vs numeric literal (IrPicCompareLiteral), fallback (IrSetBool true)
- `EmitCompareResultToBool` handles all 6 relational operators (Equal=4 through
  GreaterOrEqual=9) using CIL ceq/clt/cgt with inversions

### String Comparison — IF P-OR-F EQUAL TO "FAIL*"

The NIST CCVS framework uses string comparisons extensively. Without string compare
support, `IF P-OR-F EQUAL TO "FAIL*"` fell back to always-true, causing FAIL-ROUTINE
to execute for every test and headers to repeat.

- **Runtime**: `StorageHelpers.CompareFieldToString(byte[], int, int, string)` —
  reads field as ASCII, TrimEnd both sides, string.Compare ordinal
- **IR**: `IrStringCompareLiteral` — like IrPicCompareLiteral but for strings
- **Binder**: `LowerCondition` checks `!leftLoc.Value.Pic.IsNumeric` and routes
  to string path when right-hand side is a string literal
- **Emitter**: `EmitStringCompareLiteral` calls CompareFieldToString, then
  `EmitCompareResultToBool` for the operator

### File Section Storage — The MOVE That Crossed Areas

`MOVE TEST-RESULTS TO PRINT-REC` copies working-storage data into the file record
buffer. This requires separate storage areas:

1. **DataSymbol.Area**: New property (`StorageAreaKind`) set by SemanticBuilder
   when visiting `workingStorageSection` vs `fileSection`
2. **ComputeStorageLayout**: Separate offset counters for WS and FS
3. **FD implicit REDEFINES**: Multiple 01-level records under the same FD share
   the same file record buffer (all start at offset 0, size = max of all records).
   Without this, PRINT-REC and DUMMY-RECORD had separate byte ranges and
   `MOVE TEST-RESULTS TO PRINT-REC` never reached the bytes that WRITE DUMMY-RECORD
   outputs.

### Figurative Constants in Expressions

`IF COMPUTED-A NOT EQUAL TO SPACE` was comparing against the literal string "SPACE"
instead of a single space character. Added figurative constant normalization in
`BindArithmeticExpr`: SPACE/SPACES → `" "`, ZERO/ZEROS/ZEROES → `0m`.

### Debug Line Stripping

NIST test programs use `Y` and `S` in column 7 for conditional/debugging lines.
The preprocessor only handled `D`/`d`. Added `S`/`s`/`Y`/`y` as debug indicators.
Without this fix, WRITE-LINE contained page-break logic that reprinted headers
whenever RECORD-COUNT exceeded 42.

### Results

NC101A now produces **147 lines of structured NIST output**:
- Headers printed once at top
- Test detail lines with paragraph names (MPY-TEST-F1-13 through F1-29-3)
- PASS/FAIL results per test
- Summary: 16 of 59 tests passed, 24 failed, 19 deleted
- `END OF TEST- NC101A` footer

The pass rate is not yet 100% — remaining issues include arithmetic precision for
edge cases, numeric MOVE formatting, ON SIZE ERROR handling, and MULTIPLY GIVING
form. But the **test framework itself is fully functional**: headers, test flow,
PASS/FAIL gating, PRINT-DETAIL, cross-area MOVE, and program termination all work.

### Debugging Journey

The session was a cascade of "fix one thing, reveal the next":
1. IF branching → revealed Main only calls first paragraph
2. PC dispatch model → revealed STOP RUN doesn't terminate
3. PC returns → revealed cross-area MOVE doesn't work
4. File section layout → revealed FD implicit REDEFINES missing
5. Record overlap → revealed string comparison not implemented
6. String compare → revealed figurative constants not normalized
7. SPACE fix → revealed Y-debug lines not stripped by preprocessor

Each fix was small and surgical, but finding the right fix required understanding
the full chain from COBOL source through preprocessing, parsing, binding, IR, CIL
emission, and runtime execution.

### AI Friction Points

- Session resumed from a hung state with partially-applied changes. Had to re-read
  all modified files to understand what was already done vs. what needed doing.
- Multiple tool call rejections due to file modification conflicts (linter or
  previous edits). Required re-reading files before each edit.
- Tendency to over-investigate before acting. The user repeatedly redirected toward
  concrete implementation instead of analysis.

---

---

