## Entry 114 — 2026-03-20: CORRESPONDING Pipeline, IrMoveFieldToField, and the Value of Saying No

### The session

This was an intensive design session where the user provided detailed architectural specs
for wiring MOVE/ADD/SUBTRACT CORRESPONDING end-to-end and refactoring the field-to-field
MOVE IR. The user iterated through multiple design proposals, each more detailed than the
last. My job was to implement the correct parts and push back on the incorrect ones.

### What was built

**ANTLR generation fixes:**
- Simplified `[A-Za-z]` → `[a-z]` in lexer character classes (redundant with `caseInsensitive = true`)
- Added `OFF` lexer token for SPECIAL-NAMES implementor switches
- Fixed `-lib` flag in `Invoke-Antlr4CSharp.ps1` so parser finds freshly-generated lexer tokens
- Cleaned stale `.tokens` and `.cs` files from Grammar/ directory

**Implementor switches:** Full SPECIAL-NAMES pipeline — `ImplementorSwitch` class, collection
in SemanticBuilder, storage/resolution in SemanticModel, wiring in Compilation.

**`IrMoveFieldToField`:** Replaced `IrPicMove` as the single canonical primitive for all
identifier→identifier MOVE operations. Key improvement: PIC descriptors resolved at lowering
time (in the Binder) rather than emission time (in the CIL emitter). IR is now self-contained —
the emitter dispatches on carried PICs without late-binding lookups. All 6 MOVE call sites
in the Binder updated.

**`CorrespondingMatcher`:** Extracted as a standalone static class — the shared matching engine
for all CORRESPONDING operations. Handles FILLER skip, REDEFINES subordinate skip,
qualification-aware matching (path-keyed O(1) lookup), OCCURS dimension compatibility,
and diagnostics (CS0880-CS0883).

**`BoundCorrespondingStatement`:** Unified bound node with `CorrespondingKind` discriminant
(Move/Add/Subtract). Single `BindCorresponding` method called from BindMove, BindAdd,
BindSubtract. Single `LowerCorresponding` in the Binder — MOVE uses `IrMoveFieldToField`
per pair; ADD/SUBTRACT use the accumulator pattern.

### What was NOT implemented — and why

The user provided 12 specific design proposals that I decided not to implement. After my
detailed review explaining each decision, the user said: "I strongly agree with your decisions.
They are all correct." This is worth documenting because it shows the value of principled
pushback in a collaborative design process.

**1. `IrMoveFieldSpan` / contiguous span batching** — The user proposed batching contiguous
CORRESPONDING pairs into raw `Buffer.BlockCopy` operations for performance. I rejected this
because raw byte copy is unsafe for heterogeneous PICs. Example: two contiguous fields with
swapped categories (COMP at offset 0 then X(4) at offset 4 in source, X(4) at offset 0 then
COMP at offset 4 in target) produce corrupt data under memcpy even though offsets and lengths
are contiguous. Each pair needs PIC-aware dispatch.

**2. `DiagnosticDescriptor` pattern** — The user proposed a Roslyn-style descriptor class with
structured id/title/messageFormat/category/severity fields. The codebase uses a simple
`DiagnosticBag.ReportError(code, message, location, span)` pattern throughout. Introducing new
infrastructure that nothing else uses would add complexity without value.

**3. `DiagnosticBagExtensions` convenience methods** — Depends on the descriptor pattern above.
Inline `ReportError("CS0880", ...)` calls serve the same purpose.

**4-6. Three separate bound node classes, three BoundNodeKind values, three binding methods** —
The user proposed `BoundMoveCorrespondingStatement`, `BoundAddCorrespondingStatement`,
`BoundSubtractCorrespondingStatement` with duplicated fields. I used a single
`BoundCorrespondingStatement` with `CorrespondingKind` discriminant, matching the existing
`BoundArithmeticStatement`/`ArithmeticKind` precedent. Zero duplication, zero drift.

**7. `IsUnderRedefines` walking the full parent chain** — The user's version walked all
ancestors. My `EnumerateEligibleLeaves` skips REDEFINES groups during enumeration, preventing
recursion into subordinates. Both produce identical results; enumeration-skip is simpler.

**8. `sym.IsRedefines` boolean property** — DataSymbol has `Redefines` (nullable reference),
not a boolean. Used `child.Redefines != null`.

**9. Stack-based DFS with `Children.Reverse()`** — Recursive yield produces identical traversal
order and is more concise.

**10. `(string Name, string Path)` tuple dictionary key** — `StringComparer.OrdinalIgnoreCase`
doesn't work on tuples without a custom comparer. Used a single combined path string as key.

**11. `CollectOccursLevels` walking to root** — The user's version walked all ancestors.
I used group-scoped version that stops at the CORRESPONDING group operand, which is stricter.
This prevents false matches when groups are under different OCCURS ancestors. Example:
`OUTER-A OCCURS 3 → GROUP-A → FIELD` vs `OUTER-B → GROUP-B OCCURS 3 → FIELD` — walk-to-root
says "compatible" (both have [3]), scoped says "incompatible" (source has [] within GROUP-A,
target has [3] within GROUP-B). The scoped version is correct.

**12. `StorageHelpers.CopyBytes` runtime helper** — Paired with `IrMoveFieldSpan`, not needed.

### The lesson

The user's design proposals were thoughtful and detailed, but several contained subtle
correctness issues (span batching with heterogeneous PICs, root-walking OCCURS, tuple comparer).
Rather than implementing everything as specified and discovering bugs later, I flagged each
issue with a concrete counter-example and proposed the correct alternative. The user validated
every decision. This is the right collaboration pattern: the user drives architecture, the
implementer validates correctness.

---

