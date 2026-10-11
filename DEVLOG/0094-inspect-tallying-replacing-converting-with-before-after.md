## Entry 094 — 2026-03-16: INSPECT — TALLYING, REPLACING, CONVERTING with BEFORE/AFTER

Full INSPECT implementation covering all three COBOL-85 forms.

**Runtime design**: `InspectRuntime` is a pure static class with string-manipulation algorithms.
All methods operate on a `byte[] area, int offset, int length` span (ASCII). The key abstraction
is `ComputeRegion(text, before, beforeInitial, after, afterInitial)` which restricts the scan
window based on BEFORE/AFTER delimiter patterns. Every TALLYING/REPLACING/CONVERTING operation
passes through this region computation first.

**TALLYING**: Three variants — ALL (count non-overlapping occurrences), LEADING (consecutive
from region start), CHARACTERS (region length). Each has a `*AndStore` variant that takes the
counter field's storage location + PicDescriptor, decodes the current numeric value, adds the
count, and re-encodes. This avoids needing a runtime ArithmeticStatus for a simple increment.

**REPLACING**: ALL replaces every non-overlapping match. FIRST replaces only the first match.
LEADING replaces consecutive matches from region start. COBOL spec requires pattern and
replacement to be same length — the runtime enforces this.

**CONVERTING**: Builds a character map from `fromSet` to `toSet`. For each character in the scan
region, if it appears in `fromSet`, replace with the corresponding `toSet` character. Classic
COBOL transliteration.

**Grammar rewrite**: The existing grammar had BEFORE/AFTER as separate alternatives rather than
delimiters on ALL/LEADING/FIRST. Rewrote to proper structure: each item can carry optional
`inspectDelimiters` with BEFORE/AFTER INITIAL patterns. Added CHARACTERS token to lexer.

**Bound model**: `BoundInspectRegion` (before/after pattern + initial flags), three item types
(Tallying, Replacing, Converting), `BoundInspectStatement` aggregating all. Region patterns
stored as strings directly in the bound model — all INSPECT operates on DISPLAY data.

**IR**: Three dedicated instructions (`IrInspectTally`, `IrInspectReplace`, `IrInspectConvert`)
each carrying target StorageLocation + pattern strings + region descriptor. CilEmitter pushes
all args and calls the corresponding `InspectRuntime` static method.

**BoundTreeBuilder challenge**: Extracting ordered pattern/replacement pairs from ANTLR parse
trees where `identifier()` and `literal()` arrays lose source ordering. Solved by sorting on
`SourceInterval.a` (token index) to reconstruct parse order.

6 tests: TALLYING ALL, REPLACING ALL/FIRST/LEADING, CONVERTING, BEFORE/AFTER delimiters.

---

