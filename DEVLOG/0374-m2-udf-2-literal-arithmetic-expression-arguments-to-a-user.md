## Entry 374 — M2-UDF-2: literal / arithmetic-expression arguments to a user function

Completes the UDF correctness chapter (with M2-UDF-1, 372). `FUNCTION FOO(5)` and `FOO(A + 1)` silently yielded 0
— the inline-form lowering bailed when an argument was not a storage location, so it fell through to the intrinsic
path. Now a non-location argument is **encoded into the parameter's format** and passed BY CONTENT.

- The Compilation pre-pass additionally records each FUNCTION-ID unit's **USING-parameter signatures**
  (`SemanticModel.UserFunctionParameterSignatures` = ordered list of each parameter's storage length + PIC) —
  additive, leaving the M2-UDF-1 `UserFunctionSignatures` untouched.
- `IrUserFunctionArg` now carries **either** a location (BY CONTENT) **or** a computed value + the target
  parameter's length/PIC. `ExpressionLowerer` builds the right kind per argument (location → as before; otherwise
  lower the value and attach the parameter signature; if no signature, fall through).
- The inline emit, for a value argument, computes the decimal and calls
  `CobolProgramRegistry.EncodeFunctionArg(value, len, pic)` (→ `PicRuntime.EncodeNumeric` into a fresh buffer →
  BY-CONTENT pointer); location arguments emit exactly as before.

Verified `COMPUTE WS-R = FUNCTION DOUBLER(5)` → 10 and `FUNCTION DOUBLER(WS-A + 1)` (WS-A=4) → 10, with M2-UDF-1
(`DOUBLER(WS-X)+1` = 43) still correct. Tests: `UserFunctionCall_LiteralAndArithmeticArguments` + conformance
`tests/conformance/2002/udf_value_args`.

Plan M2-UDF-2 ticked ☑ — **the numeric-UDF correctness chapter is closed.** Guard ALL GREEN: **1047 unit / 465
integration (+2) / 364 NIST**, 0 regressions.

