## Entry 081 — 2026-03-16: NC118A 30/30 — ADD GIVING Was Silently Dropped

One root cause fixed all 13 NC118A failures: `BindAdd` returned `BoundArithmeticStatement` (silent no-op) when `addToPhrase` was null, which is the case for `ADD A B GIVING C` — no TO phrase. The GIVING targets were never parsed.

Fix: handle absent TO phrase by proceeding to check GIVING. Added `BoundAddStatement.IsGiving` flag. `LowerAdd` uses `IrMoveAccumulatedToTarget` (target = sum) for GIVING instead of `IrAddAccumulatedToTarget` (target += sum).

Also fixed NC106A's last failure: `ApplyScalingAndRounding` ignored TrailingScaleDigits (trailing P). PIC S99P → stored values are multiples of 10. SUBTRACT ROUNDED now divides by 10^P, rounds, multiplies back.

### AI Misstep: Silent Drops Are Gross Code Generation Errors

`BoundArithmeticStatement` is a silent-drop pattern — the compiler parses a valid COBOL statement, binds it to a node that produces NO code, and the program runs without the statement's effect. This was used 13 times across ADD, SUBTRACT, MULTIPLY, DIVIDE, and COMPUTE binders as "safe" early returns when something wasn't recognized.

This is not a "deferred feature" or "partial implementation." It's a code generation error. A conforming compiler must either:
1. Generate correct code for the statement, OR
2. Refuse to compile with a diagnostic

It must NEVER silently skip a statement the programmer wrote. The `BoundArithmeticStatement` silent-drop pattern was directly responsible for NC118A's 13 failures (ADD GIVING silently dropped), and the `IrSetBool(true)` fallback was the same class of error in the condition pipeline.

Both patterns were introduced by Claude without the user's knowledge — they were "convenient" fallbacks that avoided compilation failures at the cost of silent wrong behavior. This is the opposite of production quality. The correct approach: throw a fatal compiler error for any construct not yet implemented, so the developer knows immediately.

6 NIST tests at 100%: NC101A (94), NC171A (109), NC106A (127), NC176A (125), NC116A (67), NC118A (30).

---

