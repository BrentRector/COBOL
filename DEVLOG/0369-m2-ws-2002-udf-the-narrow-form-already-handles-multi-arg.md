## Entry 369 — M2 (WS-2002-UDF): the narrow form already handles multi-arg + alphanumeric — coverage + scoping

A quick assessment of how far slice 3's narrow form (368) actually reaches, before committing to the
higher-risk general inline form. It turns out the narrow whole-source `MOVE`/`COMPUTE` path already covers more
than 368 listed as follow-ups:
- **Multiple arguments** — `LowerUserFunctionCall` loops `func.Arguments`, so `COMPUTE WS-R =
  FUNCTION ADD3(WS-A, WS-B, WS-C)` works (→ 35).
- **Alphanumeric return** — the RETURNING mechanism writes the receiving item directly regardless of category, so
  `MOVE FUNCTION GREET(WS-NAME) TO WS-MSG` with `GREET` returning `PIC X(12)` works (→ `HI WORLD`).

Added `UserFunctionCall_MultiArg_And_Alphanumeric` to lock both in (test-only; the capabilities already worked).

**Scoping decision (transparency).** I investigated the remaining UDF gap — the GENERAL inline form (a
user-function call inside a larger expression / IF / DISPLAY / as a function argument). The implementation path is
now fully mapped: a Compilation pre-pass building FUNCTION-ID `SemanticModel`s (throwaway diagnostics) to register
each function's RETURNING `StorageLength` + `PicDescriptor`; a `BoundUserFunctionCall` + `IrUserFunctionCall`; and
an inline emit that mirrors `EmitCallProgram` (build `CobolDataPointer[]` args + a scratch `byte[]` as the trailing
RETURNING arg + `CobolProgramRegistry.Resolve` + `entry.Invoke(args)`) then `PicRuntime.DecodeNumeric(scratch, 0,
len, pic)` → push the decimal. That is ~150 lines of NOVEL inline CIL — genuinely risky to land cleanly at the
tail of a long session, so it is deferred to a dedicated focused iteration (recorded in memory
project_m2_progress) rather than rushed. The common UDF usage (assign a function result, any arity, numeric or
alphanumeric) already works.

Guard ALL GREEN: **1047 unit / 455 integration / 364 NIST**, 0 regressions.

