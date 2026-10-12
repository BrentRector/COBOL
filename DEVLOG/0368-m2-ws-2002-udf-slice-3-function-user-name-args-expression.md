## Entry 368 — M2 (WS-2002-UDF, slice 3): `FUNCTION user-name(args)` expression invocation (ISO §8.4.3 / §15)

The finale of the UDF trio: actually *calling* a user-defined function with the `FUNCTION user-name(args)` syntax,
returning its value. Slice 2's FUNCTION-ID unit compiles to a callable program with a working RETURNING path
(365), so the invocation is fundamentally `CALL "user-name" USING args RETURNING <target>`.

Investigation (DEVLOG-367 session, recorded in memory) found the general inline-expression form (a new IR node
+ a novel inline emit: scratch buffer + CALL + decode-and-push) was the risky path. This commit lands the
**lower-risk narrower form** that reuses the proven `IrCallProgram` emit and needs **no cross-unit type
registry** — because the RETURNING target IS the assignment's receiving item, the callee writes it directly.

Pieces:
- `Compilation`: a pre-pass collects the names of all FUNCTION-ID units in the compilation group into a set
  (order-independent — a caller may precede the function in source), stored on each unit's `SemanticModel`
  (`UserFunctionNames`). NOTE: caller + function must be in the **same compilation group** (one source/tree) for
  the caller's binder to see the function — separate-file user functions need a prototype/external repository
  (a later slice).
- `LoweringContext.IsUserFunction(name)` + `LowerUserFunctionCall(func, destLoc, block)`: the latter builds
  `IrCallProgram(name, USING args BY CONTENT, RETURNING destLoc)` (args BY CONTENT — function arguments are
  values, ISO §8.4.3); returns false (no-op) if any argument isn't a resolvable location (arithmetic-expression
  args deferred).
- `ArithmeticLowerer.LowerCompute` and `DataMovementLowerer` (MOVE): when the *whole* source expression is a
  user-function call, route to `LowerUserFunctionCall(…, destLoc)` instead of the intrinsic path.

**Exact no-op for existing code**: a program with no FUNCTION-ID unit has an empty `UserFunctionNames`, so
`IsUserFunction` is always false and intrinsic `FUNCTION` calls in MOVE/COMPUTE lower exactly as before — the
364 NIST baselines are untouched.

**Verified END-TO-END** (compile + run): one source with a `FUNCTION-ID. DOUBLER.` unit (L-R = L-X * 2) and a
caller doing `COMPUTE WS-R = FUNCTION DOUBLER(WS-X)` and `MOVE FUNCTION DOUBLER(WS-X) TO WS-R` → `C=0042` / `M=0042`.
Test `UserFunctionCall_InComputeAndMove_Invokes` (cobol2002).

Scope (documented follow-ups): a user-function call inside a *larger* expression / IF / DISPLAY / as a function
argument (the general inline-result form with a scratch temp typed from the callee's RETURNING), alphanumeric and
multi-argument returns, separate-compilation user functions (prototypes), and binding `REPOSITORY FUNCTION`
specifiers. **The core UDF feature — define a function and call it — now works.**

Guard ALL GREEN: **1047 unit / 454 integration / 364 NIST**, 0 regressions.

