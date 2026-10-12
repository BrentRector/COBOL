## Entry 372 — M2-UDF-1: general inline user-function invocation (fixes the silent-0 correctness bug)

Plan Wave 1 item 1. The audit found a **correctness defect**: a `FUNCTION user-name(args)` call anywhere other
than the whole source of a MOVE/COMPUTE silently evaluated to **0** (it fell through to the intrinsic dispatch,
`IntrinsicFunctions.Call("DOUBLER",…)` → 0). Reproduced: `COMPUTE WS-R = FUNCTION DOUBLER(WS-X) + 1` gave `0001`.

Fix — the general inline form, with the risky work kept OUT of inline CIL:
- **Runtime helper** `CobolProgramRegistry.InvokeNumericFunction(name, usingArgs, returnLen, returnPic)` (clean
  C#): appends a scratch `byte[returnLen]` as the trailing RETURNING arg, `Resolve`s + `Invoke`s the function
  (which writes its result through that pointer), then `PicRuntime.DecodeNumeric`s the buffer → decimal.
- **Compilation pre-pass** builds each FUNCTION-ID unit's signature (its RETURNING item's storage length + PIC,
  via a throwaway-diagnostics `BuildSemanticModel` + `ComputeLayout` + `GetStorageLocation`) → order-independent
  registry `SemanticModel.UserFunctionSignatures`.
- New IR expression node **`IrUserFunctionCall`**; `ExpressionLowerer` routes a user-function call (known
  signature + all arguments resolvable to storage locations) to it instead of the intrinsic path.
- **Inline emit** (`CilExpressionEmitter.EmitIrExpression`) only builds the `CobolDataPointer[]` USING args (BY
  CONTENT) and calls the helper → leaves a decimal on the stack like any other numeric expression.

The whole-source MOVE/COMPUTE form is unchanged (intercepted earlier in the statement lowerers); intrinsic
`FUNCTION` calls are unaffected (the new branch is gated on `IsUserFunction` + a known signature). Verified
`COMPUTE WS-R = FUNCTION DOUBLER(WS-X) + 1` → `EXPR=0043`. Tests: `UserFunctionCall_InsideLargerExpression` +
conformance `tests/conformance/2002/udf_inline_expression` (the per-feature conformance rule).

**Remaining (M2-UDF-2, deferred):** literal / arithmetic-expression arguments — `FUNCTION DOUBLER(5)` /
`DOUBLER(A+1)` still yield 0 because a non-location argument can't yet be materialized + encoded per the callee's
parameter PIC (needs parameter-type signatures, cross-unit). The routing correctly falls through for those today.

Plan: M2-UDF-1 ticked ☑ in `docs/ISO2023_CONFORMANCE_PLAN.md`. Guard ALL GREEN: **1047 unit / 461 integration
(+2) / 364 NIST**, 0 regressions.

