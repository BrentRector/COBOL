## Entry 139 — 2026-03-24: NIST Blocker Fixes — Validation, CIL, RENAMES, Grammar

Systematic pass through NIST test blockers. Multiple root causes identified and fixed:

**OCCURS validation too strict:**
- Raised subscript/OCCURS depth limit from 3 to 7 (NIST exercises up to 7 levels; COBOL-85 says 3 but
  implementations may support more). Diagnostics COBOL0407/0408 now fire at >7 instead of >3.
- Removed CBL1104 "group item as OCCURS key" — COBOL-85 actually allows group keys. Updated unit test
  to verify group keys are accepted.

**ALL ZEROS figurative constant parsing:**
- `ALL ZEROS` was stored as raw text "ALLZEROS" because SemanticBuilder didn't strip the ALL prefix
  from `fig.GetText()`. Fixed to strip ALL prefix before matching ZERO/SPACE/HIGH-VALUE/etc. Also
  handles `ALL "X"` (literal repeat) correctly.

**CIL Decimal op_Explicit ambiguity (NC252A):**
- Power operator (`**`) had unused `var toDouble = typeof(decimal).GetMethod("op_Explicit"...)` that
  caused Mono.Cecil ambiguity error at assembly generation time (multiple op_Explicit overloads with
  same parameter type, different return types). The actual code path used `ToDouble` correctly — removed
  the dead variable.

**RENAMES category inheritance (NC252A):**
- Single-field RENAMES (`66 X RENAMES Y`) was always treated as alphanumeric group-like byte range.
  Fixed: when RENAMES covers exactly one elementary field with no THRU, inherit the source field's
  PIC and ResolvedType. This allows `ADD 3500 TO RENAME-12` when RENAME-12 aliases a numeric field.

**ZERO in arithmetic context (NC250A):**
- Attempted to add ZERO to `numericLiteralCore` and `primaryExpression` grammar rules so `ZERO - X`
  parses as arithmetic. Both approaches caused exponential ALL(*) backtracking because ZERO conflicts
  with `signCondition`'s `IS ZERO` terminal. **Reverted.** This requires a deeper grammar restructuring
  — possibly separating `signCondition` from `primaryCondition` to eliminate the ZERO ambiguity.
  Filed as known gap.

**Abbreviated conditions grammar (prior uncommitted work):**
- Grammar rules `abbreviatedRelation` and `abbreviatedAndChain` added for COBOL-85 §6.3.4.2.
- `BoundAbbreviatedExpression` node + `RewriteAbbreviatedRelations` rewrite pass fills in elided
  left operands and operators from context.
- `BindLogicalOr`/`BindLogicalAnd` rewritten to iterate children generically (not just typed arrays)
  to handle mixed full/abbreviated alternatives.

**NC233A reaches 100%** — added to guard suite (now 31 NIST tests).

**Remaining blockers categorized:**
- NC211A/NC254A: condition-name conditions (`IF switch-condition`) — not abbreviated conditions
- NC247A: OCCURS DEPENDING ON runtime truncation — SEARCH/comparison don't respect active ODO count
- NC215A/NC219A: collating sequence (ALPHABET clause) not applied to comparisons
- NC250A: ZERO-in-arithmetic grammar backtracking
- NC220M/NC237A: runtime infinite loops (undiagnosed)

