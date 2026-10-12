## Entry 175 — 2026-03-30: M003 Stage 2 — Leaf Emitters (Location + Expression)

### The extraction
Moved 9 methods to CilLocationEmitter (EmitLocationArgs, EmitLocationArgsWithPic,
EmitCachedLocationArgs, EmitElementAddress, EmitRefModAddress, EmitLoadBackingArray,
EmitLoadBackingArrayOrExternal, EmitLinkageLocationArgs, TryGetExternalField).

Moved 6 methods to CilExpressionEmitter (EmitIrExpression, EmitIrIntrinsicCall,
EmitFunctionCall, EmitLoadDecimal, EmitLoadPicDescriptor, EmitByteArrayLiteral).

### The SyncToContext discovery
First test run failed: `Value cannot be null (Parameter 'field')`. CilEmitter's local
`_programStateField` wasn't being synchronized to EmissionContext before VALUE clause
initialization called the location emitters. Root cause: CilEmitter writes to its own
fields during module setup, but extracted emitters read from `_ctx`.

Fix: Added `SyncToContext()` / `SyncFromContext()` methods that bridge CilEmitter's
local fields to EmissionContext. Called at key synchronization points — after
ProgramState setup, before method body emission, after each method body.

### The ArithmeticStatusLocal unification
Three NIST regressions (NC117A, NC172A, NC173A) — SIZE ERROR not triggering.
CilExpressionEmitter's `EmitLoadArithmeticStatusRef` created its own
ArithmeticStatusLocal, while CilEmitter's `EnsureArithmeticStatusLocal` created a
different one. Two separate locals in the same method = SIZE ERROR never set.

Fix: Made `_ctx.ArithmeticStatusLocal` the single source of truth. Both CilEmitter
and CilExpressionEmitter read/write the same field.

Tests: 689 unit + 287 integration + 95 NIST. All green after both fixes.

### Lesson
Extracting methods that share per-method mutable state is trickier than extracting
pure functions. The sync protocol is the price of incremental extraction — it
disappears in Stage 5 when forwarding wrappers are removed.

---

