## Entry 190 — 2026-05-28: EVALUATE consecutive WHENs share one imperative — NC225A 100% (95/95 NC suite)

**Bug (NC225A EVA-TEST-GF-35-1):** `EVALUATE TRUE  WHEN a  WHEN b  WHEN c  MOVE "A"...
WHEN OTHER ...` — the grammar `evaluateWhenClause : WHEN evaluateWhenGroup ... statementBlock*`
gave each WHEN its own (empty) body, so a match on the first two WHENs executed nothing.
ISO 14.8.4: one or more consecutive WHEN phrases share the following imperative (they are OR'd).

**Fix (grammar, approved + binder):**
- Grammar: `evaluateWhenClause : evaluateWhenPhrase+ statementBlock* | WHEN OTHER statementBlock*`
  with `evaluateWhenPhrase : WHEN evaluateWhenGroup (ALSO evaluateWhenGroup)*`.
- Binder (`ControlFlowBinder.BindEvaluate`): bind the clause's shared imperative once, then
  emit one `BoundEvaluateWhen` match arm per phrase over that shared body. The existing
  EVALUATE lowering tests the arms in order, so the first matching phrase runs the body and
  exits — exactly the OR semantics, no bound-node or lowerer change required.

NC225A: 1→0 FAIL*, now 63/63. Guard green — other EVALUATE tests (NC132A, NC210A, NC211A,
NC254A) still MATCH.

**Milestone: all 95 NC-series NIST nucleus tests pass at 100% with clean baselines** (was
89/95 at the start of this session; the six closed here were NC201A, NC250A, NC237A, NC247A,
NC216A, NC225A).

---

