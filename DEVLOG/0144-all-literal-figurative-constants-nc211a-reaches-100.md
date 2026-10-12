## Entry 144 — 2026-03-24: ALL Literal Figurative Constants — NC211A Reaches 100%

**The final two NC211A failures were `ALL "ABC"` figurative constants.** `VALUE ALL "ABC"` for
`PIC X(6)` should produce `ABCABC` (pattern repeated to fill) but produced `ABC   ` (pattern
once, space-padded).

### Root cause: ALL literal stored but never expanded

The `SemanticBuilder` correctly parsed `ALL "ABC"` and stored the literal `"ABC"` as
`initialValue`. But it set `_deferredFigurativeInit = null` — no figurative fill mechanism
was triggered. The comment on line 464 said "the runtime fills by repeating it" but that was
aspirational: no code existed to do the repetition.

For figurative constants like `ALL ZEROS` or `ALL SPACES`, a `FigurativeKind` enum drives
field-filling at initialization. But `ALL "literal"` has no `FigurativeKind` — it's a
literal-specific pattern, not a single-character fill.

### Fix: expand at layout time

Added `AllLiteralPattern` property to `DataSymbol`. When `StorageLayoutComputer.RegisterValue`
processes a field with `AllLiteralPattern`, it repeats the pattern to fill `ElementSize` using
a `StringBuilder`. The expanded string is registered as the initial value — no new IR
instructions or runtime support needed.

This is the correct architectural position: the expansion happens when the field's physical
size is known (layout phase), not during parsing (where size is unknown) or at runtime
(where it would add overhead to every program startup).

### Result

NC211A: **51/51 — 100%**. Added to guard suite (33 tests). The figurative constant fix also
benefits any other NIST test using `ALL literal`.

---

