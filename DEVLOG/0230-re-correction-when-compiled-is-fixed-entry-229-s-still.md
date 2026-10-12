## Entry 230 — Re-correction: WHEN-COMPILED IS fixed (Entry 229's "still broken" was a test-harness error)

Entry 229 claimed WHEN-COMPILED was still returning the runtime clock. That conclusion was WRONG —
it came from a broken verification, not broken code. The earlier "two runs differ" tests had been
run against a STALE DLL (compiled before the rebuild finished) and/or with a `cd`/relative-path race
that made the program fail to launch while the file comparison trivially "matched". 

Re-verified properly — clean rebuild, absolute paths, fully sequential, same DLL run twice 3s apart:
both runs print the identical `2026053011422607-0700`. WHEN-COMPILED is BAKED at compile time and
STABLE across runs. The fix (commit 00713a9, `CilExpressionEmitter.EmitIrIntrinsicCall` special-case)
is effective on the MOVE path too: `EmitFunctionCall` builds the IrIntrinsicCall and delegates to
`EmitIrIntrinsicCall`, which leaves the baked constant on the stack before `MoveStringToField`.

Corrected status of the spec-conformance sweep:
- **#2 WHEN-COMPILED — FIXED & VERIFIED** (00713a9; cross-run stable).
- **#1 ON SIZE ERROR — VERIFIED-WORKS** (never broken).
- **#3 LENGTH of OCCURS DEPENDING ON group — OPEN BUG** (returns max layout; `ExpressionBinder`
  folds LENGTH to a compile-time constant with no ODO branch). Still genuinely open.

Process lesson (third time this session a verification mislead me): for run-twice stability checks,
use absolute paths, a clean rebuild, and sequential calls — never trust a pass/fail when the run
itself errored. docs/spec-gaps.md updated to this corrected status.

