## Entry 072 — 2026-03-15: ArithmeticStatus Refactor — Statement-Level Sticky Status

### The Problem

Multi-target MULTIPLY with ON SIZE ERROR:
```cobol
MULTIPLY A BY B C D ON SIZE ERROR ...
```
If target B overflows but D doesn't, ON SIZE ERROR should still fire (spec: "if any target
overflows"). The old design called `EmitInitArithmeticStatus` inside each arithmetic emitter,
so each target reset the status — only the last target's overflow was preserved. This caused
3 sub-tests (F1-18/20/22 .06) to be missing entirely from output.

### The Fix — Production-Quality Refactor

**Old design**: Each arithmetic CIL emitter (EmitPicMultiply, EmitPicAdd, etc.) called
`EmitInitArithmeticStatus` internally. Emitter controlled status lifecycle.

**New design**: One ArithmeticStatus per statement, binder-driven.
- **Binder** emits `IrInitArithmeticStatus` once before all operations in a statement.
- **Runtime helpers** never clear status — they only set `SizeError = true` (sticky).
- **Emitter** is dumb and uniform: `IrInitArithmeticStatus` → initobj, arithmetic ops just
  pass `ref status`, `IrLoadSizeError` reads the accumulated result.

No accumulator locals, no OR operations, no special multi-target logic. The status naturally
accumulates across all targets because nobody clears it between calls.

### AI Process Failure — Attempted Backward-Compatible Hack

First instinct was to add a second "accumulator" local variable and OR flags together after
each target. User corrected: back-compatibility is irrelevant in a from-scratch compiler.
The right fix is to refactor the architecture to the cleanest long-term shape.

### Result

**NC101A: 93/93 test results appear** (was 90/93). F1-18/20/22 .06 sub-tests now pass.
Only remaining: F1-6 (COMP REDEFINES issue) + footer display bug.

---

