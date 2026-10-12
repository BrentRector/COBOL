## Entry 113 — 2026-03-19: 24 NIST Tests at 100% — INDEX Items, INSPECT Patterns, "Every Bug Is a Pattern" Failure

### The pattern I should have swept

When I fixed SUBTRACT GIVING's minuend reconstruction to preserve subscripts (changing
`new BoundIdentifierExpression(targets[0].Target.Symbol, ...)` to `targets[0].Target`),
I fixed the same bug in DIVIDE GIVING but **missed ADD GIVING**. This is a direct violation
of the "every bug is a pattern" rule: the identical anti-pattern (reconstructing a
BoundIdentifierExpression from just the Symbol, dropping subscripts) existed in three places.
I fixed two and left one latent.

The user forced me to write subscripted GIVING conformance tests for ALL arithmetic operations.
The ADD test (`ADD WS-A TO NUM(2) GIVING WS-R`) immediately caught the bug: expected 210,
got 010. The TO operand `NUM(2)` was being silently discarded when GIVING was present —
`targets.Clear()` removed it without preserving its value as an addend.

**Lesson**: when the same structural pattern appears in N places, fix all N in the same commit.
Don't fix 2 of 3 and move on. The test suite should enforce this by covering ALL instances.

### The ADD GIVING bug (deeper than SUBTRACT)

SUBTRACT GIVING's bug was about subscript loss. ADD GIVING's bug was about operand loss:
- `ADD A TO B GIVING C` → C = A + B. The TO item `B` is a SOURCE (addend), not a TARGET.
- The binder cleared the targets list (which contained B) without moving B to the operands list.
- Result: C = A (only the addOperandList was accumulated, not the TO operands).
- This bug was INVISIBLE in all existing tests because they used `ADD A B GIVING C` (no TO),
  or `ADD A TO B` (no GIVING).

### INDEX items from INDEXED BY (NC122A, NC123A)

INDEX names declared via `INDEXED BY idx-name` in OCCURS clauses were never added to the symbol
table. `SET INDEX1 TO 4` compiled but stored to nowhere. `TABLE1-REC(INDEX1)` evaluated the
subscript as 0 (unresolved identifier → literal "INDEX1" → numeric 0 → offset = -elementSize).

Fix: SemanticBuilder now declares INDEX names as level-77 PIC S9(9) COMP DataSymbols with
resolved PicDescriptor. NC122A went from crash to 12/24. NC123A went from crash to 34/34 (100%).

### INSPECT data-reference patterns (NC115A 31/31)

INSPECT patterns that are data references (field names) were being passed as the field NAME
instead of the field VALUE. `ExtractInspectChar` returned `"SPACE-XN-1-1"` (the identifier text)
instead of `" "` (the space character stored in the field).

Refactored to `InspectPatternValue` (literal OR data-ref). Data-ref patterns are materialized at
runtime via `ReadFieldAsRawString` (no TrimEnd — trailing spaces are significant for INSPECT).
Compile-time resolution stays for BEFORE/AFTER delimiters and CONVERTING (more efficient, values
are constants with VALUE clauses). NC115A went from 13/31 to 31/31 (100%).

### Conformance test suite expansion

Added 5 subscripted-operand GIVING tests covering every arithmetic statement:
- `Subtract_FromSubscripted_GivingIdentifier`
- `Add_ToSubscripted_GivingIdentifier` ← caught the ADD bug immediately
- `Multiply_BySubscripted_GivingIdentifier`
- `Divide_IntoSubscripted_GivingIdentifier`
- `Compute_WithSubscriptedOperand`

These are regression guardrails against the "reconstruct from Symbol, lose subscripts" pattern.

### Session scorecard

| Test | Start | End | Key fix |
|------|-------|-----|---------|
| NC115A | 13/31 | 31/31 (100%) | INSPECT data-ref patterns |
| NC122A | crash | 12/24 | INDEX items declared |
| NC123A | crash | 34/34 (100%) | INDEX + SUBTRACT GIVING subscript |
| ADD GIVING | latent bug | fixed | TO operands preserved as addends |

24 NIST tests at 100%, 169 integration tests, 10 golden-file regressions — all green.

---

