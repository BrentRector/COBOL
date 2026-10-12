## Entry 102 — 2026-03-17: SEARCH / SEARCH ALL — Spec Compliance Win

Implemented SEARCH (linear) and SEARCH ALL (binary, currently lowered as linear pending KEY
ASCENDING/DESCENDING support). Grammar, bound model, binding, lowering, 13 tests.

**Grammar fix:** `searchAllWhenClause` changed from `WHEN relationalExpression` to
`WHEN condition imperativeStatement*`. The original grammar was a real hole — no imperative
statements and too-narrow condition syntax. Semantic restrictions (single WHEN, relational
equality, sorted table) enforced in the binder, not the parser. Consistent with IF/EVALUATE.

**Three pre-existing bugs surfaced and fixed:**

1. **Group OCCURS child step size** — `ResolveLocation` used the leaf element's size for
   subscript multipliers instead of the OCCURS group's element size. For `VAL PIC 9` inside
   `ROW OCCURS 3` (containing VAL + FLAG = 2 bytes), VAL(2) computed offset 1 instead of 2.
   Fix: introduced `stepSize` (OCCURS group element size) vs `leafSize` (leaf element size).

2. **Alphanumeric field-vs-field comparison** — `LowerCondition` always used `IrPicCompare`
   (numeric decode) for location-vs-location comparisons, even when both sides were PIC X.
   Added `IrStringCompare` IR instruction, `CompareFieldToField` runtime method, and
   category-based dispatch in the Binder.

3. **Multi-dimensional SEARCH index extraction** — `FindSubscriptOnTable` took the first
   subscript from `A(I, J)`, but for `SEARCH COL` the index should be J (COL's dimension),
   not I (ROW's dimension). Fixed by walking the OCCURS level chain and matching the SEARCH
   table to its positional subscript.

**AI win: caught non-standard COBOL in user-supplied tests.** User provided 2D/3D SEARCH tests
that expected SEARCH to iterate ALL dimensions simultaneously. Claude stopped implementation
and flagged this: "In standard COBOL, SEARCH only searches the innermost dimension — you nest
PERFORM loops for outer dimensions. Should I adjust these tests to conform to COBOL SEARCH
semantics, or do you want the non-standard behavior?" User confirmed: stick with the ISO spec.
Tests were rewritten to use PERFORM VARYING for outer dimensions, keeping the compiler
spec-compliant. This is exactly the right behavior — the AI pushed back on incorrect
assumptions instead of silently implementing non-standard semantics. The rule "implement from
the spec" applies to tests too, not just compiler code.

132 integration tests, 1 skip, all green.

---

## Retrospective — 2026-03-17: Systemic Pattern Analysis (Entries 086–101)

A retrospective scan across 15 entries reveals four recurring failure modes. All are variations
of the same root: bypassing the abstraction boundary. Codified here as architectural doctrine.

### Pattern 1: Rogue paths bypassing the canonical abstraction

Instances found across the log:
- EmitExpression bypassed IrLocation (Entry 101)
- LowerCondition bypassed ResolveExpressionLocation (Entry 101)
- BindPrimaryExpression bypassed BindIdentifierWithSubscripts (Entry 101)
- Arithmetic operand binding bypassed the identifier binder (Entry 101)
- FileRuntime bypassed CobolFileManager (Entry 094)
- ACCEPT FROM DATE initially bypassed lexer tokens (Entry 091)
- INSPECT initially bypassed region abstraction (Entry 095)
- GO TO DEPENDING initially bypassed subscript support (Entry 098)

The fix is always the same: create or extend the canonical abstraction. The abstractions that
now serve as canonical dispatch points:
- `IrLocation` — all data storage references
- `ResolveExpressionLocation` — all bound expression → location resolution
- `EmitLocationArgs` / `EmitLocationArgsWithPic` — all CIL location emission
- `BindIdentifierWithSubscripts` — all identifier binding from parse tree
- `CobolFileManager` — all file I/O operations

**Doctrine:** If a canonical abstraction exists, use it. If it doesn't, create it before
implementing the feature. Never route around it.

### Pattern 2: Fixing the instance instead of the pattern

"I keep treating bugs as isolated incidents instead of structural patterns."

Instances:
- Fixing one `IDENTIFIER().GetText()` instead of all 8 occurrences
- Fixing one `GetStorageLocation` bypass instead of auditing the emitter
- Fixing one ref-mod special case in LowerMove instead of unifying expression resolution
- Fixing one abbreviated relation case instead of rewriting the binder

**Doctrine:** Every bug is a pattern. Every pattern has multiple instances. When you find a
structural flaw, assume it exists elsewhere until proven otherwise. Stop, identify the pattern,
sweep the codebase, fix all instances, add regression tests. One pass.

### Pattern 3: Bolting instead of integrating

Adding a feature at the leaves instead of at the abstraction boundary:
- Reference modification initially bolted onto LowerMove as a type-check cascade (Entry 100)
- OCCURS initially wired into MOVE/DISPLAY only, not unified into IrLocation (Entry 099)
- ACCEPT FROM DATE initially bolted via string comparisons (Entry 091)
- File I/O: legacy FileRuntime bolted next to CobolFileManager (Entry 094)
- NEXT SENTENCE initially impossible because sentences weren't modeled (Entry 090)

**Doctrine:** If a feature touches multiple subsystems, integrate it at the abstraction
boundary, not at the leaves. The pre-change checklist (Entry 100) catches this:
1. Is there a single, canonical dispatch point? Extend it or create it.
2. Is the type logic centralized or smeared across call sites? If smeared, refactor first.
3. Am I modifying a leaf when the concept is more general? If yes, step back.

### Pattern 4: Missing the "single dispatch point"

When the answer to "is there a single dispatch point?" was "yes," the fix was trivial:
- `ResolveExpressionLocation` — one method, all data references
- `EmitLocationArgs` — one method, all CIL location emission
- `RewriteAbbreviatedRelations` — one pass, all abbreviated conditions
- `CobolFileManager` — one class, all file operations

When the answer was "no," creating one simplified everything downstream. The cost of creating
a dispatch point is always less than the cost of not having one.

**Doctrine:** Every concept in the compiler should have exactly one dispatch point. If you're
adding logic in multiple places for the same concept, you don't have a dispatch point yet.

---

