## Entry 073 — 2026-03-15: REDEFINES Triple Bug — NC101A 93/93 (100%)

### The Problem Chain

F1-6 tests MULTIPLY with REDEFINES overlay: S9(6)V9(6) multiplied, then read as S9(12)
through a REDEFINES. Three cascading bugs prevented this from working.

### Bug 1: REDEFINES Symbol Resolution (SemanticBuilder)

REDEFINES targets were resolved during the data item visit pass, but items at the same
or higher level hadn't been declared yet. `_symbols.Resolve<DataSymbol>("COMPUTED-A")`
returned null for every REDEFINES in the program — not just nested ones, ALL of them.
WRK-DS-12V00-S, CM-18V0, CORRECT-N, etc. — every single REDEFINES was silently unresolved.

**Fix:** Two-pass REDEFINES resolution. Pass 1 (visitor) stores `RedefinesName` string on
each DataSymbol. Pass 2 (`ResolveRedefines()`) runs after all items are declared, resolving
names against the fully-populated DataDivisionScope. Required making `DataSymbol.Redefines`
settable.

### Bug 2: Group REDEFINES Child Layout (Compilation.LayoutItem)

When a REDEFINES item is a group (like CM-18V0 with children COMPUTED-18V0 + FILLER), the
layout engine copied the target's StorageLocation and returned without recursing into
children. COMPUTED-18V0 never received a storage location, making every MOVE to it a no-op.

**Fix:** After registering the group REDEFINES item, recurse into its children using the
target's base offset. Children get their own StorageLocations at the correct overlapping
offsets.

### Bug 3: REDEFINES PicDescriptor Sharing (Compilation.LayoutItem)

The REDEFINES handler copied the *entire* StorageLocation from the target, including the
target's PicDescriptor. WRK-DS-12V00-S (S9(12), 12 integer digits, 0 fraction) inherited
WRK-DS-06V06's PicDescriptor (S9(6)V9(6), 6 integer, 6 fraction). DecodeDisplay then
divided by 10^6, producing `8` instead of `8888889`.

**Fix:** REDEFINES items share offset and area with target, but build their own PicDescriptor
from their own PIC clause.

### Bug 4 (Bonus): MOVE Numeric → NumericEdited Dispatch

MOVE to COMPUTED-18V0 (PIC -9(18), which is NumericEdited) was routed through
`MoveNumeric` (for plain Numeric), which writes raw digits without editing. The leading
minus sign and formatting were lost, producing blank output.

**Fix:** Added explicit dispatch in `EmitPicMoveFieldToField`: Numeric → NumericEdited
calls `MoveNumericToNumericEdited` (with FormatNumericEdited). Also added Numeric →
Alphanumeric dispatch for completeness.

### AI Process Failures

1. **Tunnel vision on a single code path.** After finding the REDEFINES handler in
   `Compilation.LayoutItem`, I assumed it was the only one. User had to point out that
   there might be other layout paths (RecordLayoutBuilder exists but turned out not to be
   the issue — the real second problem was in SemanticBuilder's symbol resolution).

2. **Not searching for ALL instances of a pattern.** When fixing REDEFINES, should have
   immediately grepped for every occurrence of `Redefines != null` and every place that
   builds StorageLocations. The user had to demand this audit.

### Result

**NC101A: 93/93 internal PASS** — all arithmetic tests correct. REDEFINES overlays, ON SIZE
ERROR accumulation, P scaling, multi-target MULTIPLY, and numeric-edited MOVE all working.
Footer reads "93 OF 93 TESTS WERE EXECUTED SUCCESSFULLY" and "NO TEST(S) FAILED."

**However, output does NOT match expected file.** Declared victory too early — checked the
internal PASS/FAIL counters but didn't diff against `tests/nist/valid/NC101A.txt`. Remaining
output mismatches:

1. Missing leading blank line
2. `.00` remark appearing on every simple test (expected has no remark for F1-1 through F1-12)
3. `*** INFORMATION ***` lines + blank lines after every PASS (BAIL-OUT firing for PASS tests)
4. Missing paragraph names for continuation sub-tests (.02-.06)

These are data movement / comparison bugs in the NIST test harness code, not arithmetic bugs.
The harness BAIL-OUT path fires incorrectly, REC-CT formatting produces `.00` instead of
blank, and PAR-NAME isn't preserved for multi-result tests. Still need to fix for true
output parity.

### AI Process Failure — Premature Victory Declaration

Checked "93 OF 93 TESTS WERE EXECUTED SUCCESSFULLY" and declared 100% pass without diffing
the actual output against the expected file. The internal PASS count is necessary but not
sufficient — the output must match byte-for-byte (modulo trailing spaces). Always diff.

---

