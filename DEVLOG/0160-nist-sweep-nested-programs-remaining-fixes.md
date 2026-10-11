## Entry 160 — 2026-03-28: NIST Sweep — Nested Programs + Remaining Fixes

**Full NIST sweep:** 64/95 at 100% (up from 60). Dispatched 4 agents for the 28 remaining
non-Report-Writer tests. Agent permission issues: sub-agents couldn't run `dotnet run` (custom
executable) — only standard `dotnet build`/`dotnet test` are auto-approved for sub-agents.
Two agents made significant progress before hitting the block.

**What landed:**
- Nested program support: grammar rules, multi-program compilation pipeline, CilEmitter
  multi-type emission. NC113M+ tests can now compile nested programs.
- ODO improvements: additional NIST integration tests for NC245A/NC246A/NC247A/NC220M
- CURRENCY SIGN: `SIGN` keyword now optional (`CURRENCY "<"` parses)
- 4 new valid output files: NC114M, NC134A, NC139A, NC235A
- Guard expanded from 60 to 64 tests

**Honest assessment of the spec audit methodology:**
The 8-agent audit (Entry 153) checked feature *presence* — "is X parsed? is it lowered? is it
emitted?" It did NOT validate grammar *completeness* — every optional keyword, every syntax
variant. Only NIST tests (real COBOL programs) can expose grammar edge cases. The audit was
thorough for architecture and algorithms but shallow on syntax variants. This is an inherent
limitation of code-level auditing vs. conformance testing.

**Remaining NIST blockers (28 tests):**
- 7 nested programs (need GLOBAL scope chain + deeper testing)
- 5 ODO/runtime (hangs, truncation)
- 4 partial pass (DISPLAY format, collating sequence, INITIALIZE)
- 12 individual parse/compiler bugs (custom currency PIC, double period, subscripts, etc.)
- All require iterative compile-fix-test cycles that sub-agents couldn't do

**Results:** 421 unit + 274 integration + 64 NIST guard = ALL GREEN.

---

