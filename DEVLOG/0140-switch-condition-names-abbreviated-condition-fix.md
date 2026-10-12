## Entry 140 — 2026-03-24: Switch Condition-Names + Abbreviated Condition Fix

**Switch-status conditions implemented (NC254A → 100%):**
Condition-name conditions defined via `ON STATUS IS` / `OFF STATUS IS` in SPECIAL-NAMES now fully
work. Added `BoundSwitchConditionExpression` → `IrTestSwitch` → CIL emission calling
`SwitchRuntime.GetSwitchState()`. Switch state is configurable via environment variables
(`COBOL_SWITCH_1=ON`). NC254A passes all tests with switch-1 ON.

**Abbreviated condition bare-left-operand fix:**
The `RewriteAbbreviatedRelations` pass wasn't expanding bare operands that appeared as the LEFT
child of AND/OR nodes in abbreviated condition chains. For example, `IF A = B OR C AND D`
produced `C` as an unexpanded `BoundIdentifierExpression`. Fixed by checking if the left operand
remains bare after recursive rewrite and expanding it using inherited relational context.

**Careful regression lesson:** First attempt expanded bare operands globally (at the top of
`RewriteAbbrev`), which broke `IF A < B AND B < C` by turning `B` (left operand of `B < C`)
into `A < B`. The fix must only apply in the AND/OR handler where bare operands are at the
condition level, not inside relational expressions.

**Guard: 32 tests** (NC254A added).

---

