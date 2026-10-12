## Entry 011 — 2026-03-13: AI Misstep #2 — Not Verifying Demo Output

### What Happened

After implementing intrinsic functions (~70 functions, 30 unit tests, all passing), Claude compiled and ran DEMO5.cob. The program ran without crashing. Claude was about to commit and declare Phase 5 done — without noticing that **every single intrinsic function result was zero**.

The output showed:
```
SQRT(144) = 0000000
ABS(-42) = 0000000
MOD(17,5) = 0000000
```

The user had to point this out. The root cause: the CIL emitter's `EmitArithmeticExpression` method had no case for `FunctionCallExpression`, so it fell through to the `else` branch which emits `0m`. The parser produced correct AST nodes. The runtime had correct function implementations. The 30 unit tests tested the runtime directly and passed. But the **code generator never wired them together** — the entire feature was a dead end at the IL level.

### Why This Matters

This is a pattern compounding with Entry 008. The LLM has two related failure modes:

1. **Entry 008**: When compilation fails, change the source instead of fixing the compiler
2. **Entry 011**: When execution succeeds (no crash), declare victory without checking output

Both stem from the same root: treating surface-level success signals ("it compiled," "it ran") as proof of correctness, when the actual bar is "it produced the right results." For a compiler project, the chain is: source → parse → analyze → emit → run → **verify output**. Skipping the last step means bugs in the emit phase are invisible.

### The Fix

Added `EmitIntrinsicFunctionCall()` to the CIL emitter, handling both arithmetic contexts (unbox to decimal) and display contexts (toString). Connected it in `EmitArithmeticExpression` and `EmitDisplayStatement`.

### Lesson

After running ANY demo or test: **read the output and verify every value is correct**. "It ran" is not success. "It produced the right answers" is success. This is now a hard process rule.

---

