## Entry 067 — 2026-03-15: ON SIZE ERROR Bound + Stubbed Lowering

Added ON SIZE ERROR / NOT ON SIZE ERROR support to MULTIPLY:

**Bound nodes:**
- BoundMultiplyStatement: added `OnSizeError` and `NotOnSizeError` (IReadOnlyList<BoundStatement>)
- BoundAddStatement: same additions (ready for ADD SIZE ERROR later)

**BoundTreeBuilder:**
- BindMultiply now calls `ctx.multiplyOnSizeError()` and extracts both
  `imperativeStatement` blocks into OnSizeError/NotOnSizeError lists

**Binder lowering:**
- LowerMultiply now returns IrBasicBlock (like LowerIf) for block continuation
- When OnSizeError/NotOnSizeError present: creates conditional blocks
  (size.error, not.size.error, size.done) with IrBranchIfFalse
- **Stubbed**: size error flag always false (NOT ON SIZE ERROR path always taken)
- Real ArithmeticStatus detection deferred — requires threading ref parameter
  through CIL emission

**Result:** NC101A: **54/90 pass** (was 51/89). Three more tests pass from NOT ON
SIZE ERROR clauses executing. Test count rose from 89→90 as more sub-tests parse.

Footer still shows "NO TEST(S) FAILED" despite failures — counter bug separate.

---

---

