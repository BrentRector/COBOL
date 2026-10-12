## Entry 178 — 2026-03-30: M003 Stage 5 — Cleanup (4,083 → 1,299 lines)

### The final cleanup
Rewrote EmitInstruction's 65 case arms to call `_ctx.*` emitters directly (no more
forwarding wrappers). Deleted ~60 forwarding wrappers. Updated all remaining CilEmitter
methods that called wrappers (EmitValueClauseInitialization, EmitCallProgram) to use
`_ctx.Location.*` / `_ctx.Expression.*` directly.

Removed `_cobolDataPointerCtor` field (now only on EmissionContext). Cleaned up
SyncToContext/SyncFromContext.

### What remains in CilEmitter (1,299 lines)
- Constructor + EmissionContext wiring
- EmitAssembly (2 overloads) — public API
- EmitModule — 7-step orchestration pipeline
- SyncToContext / SyncFromContext — field bridge
- Module setup: SeedPrimitiveTypes, DefineType, DefineGlobal, GetTypeRef,
  DefineMethodSignature, CreateEntryMethodSignature, EmitAlternateEntryMethod
- Program state: EmitProgramState + 8 sub-methods
- Method body: EmitMethodBody, EmitInstruction (dispatch-only)
- Paragraph dispatch: EmitParagraphDispatchInline, EmitParagraphDispatch
- CALL: EmitCall, EmitCallProgram, EmitCheckCallException
- Runtime: EmitRuntimeCall
- Tiny inline cases: IrSetBool, IrBinaryLogical, IrLoadSizeError,
  IrInitAccumulator, IrAccumulateField/Literal, IrComputeIntoAccumulator, IrCancelProgram

### M003 final scorecard
| Metric | Before | After |
|--------|--------|-------|
| CilEmitter.cs | 4,083 lines | 1,299 lines (-68%) |
| Emission classes | 0 | 11 (10 emitters + EmissionContext) |
| God class methods | 87 | 25 (orchestration only) |
| Unit tests | 674 | 765 |
| Integration tests | 287 | 287 |
| NIST guard | 95 | 95 |

M003 closed. Three P0 items (M001, M002, M003) done. Next open P0: M004
(BoundTreeBuilder decomposition).

---

