## Entry 111 — 2026-03-19: NC107A 0 Failures, NC112A 100%, NC124A 100% — REDEFINES Families, PIC Editing, Doubled-Quote Un-escaping

The second half of the NIST autonomous session. Started at NC107A 166/177, NC112A 31/32,
NC124A 158/169. Ended with all three at effective 100% (zero test failures).

### SIZE ERROR detection gap (NC112A 32/32)

`SUBTRACT ... FROM 100 GIVING DNAME-1 ON SIZE ERROR` — the SIZE ERROR never fired because
`EmitComputeStore` called `MoveNumericLiteral` which doesn't check overflow. Consolidated:
removed the redundant `ComputeAndStore` method and routed through `MoveAccumulatedToField` —
the single "store decimal with overflow detection" path now shared by ALL arithmetic operations
(ADD/SUB/MUL/DIV accumulator, COMPUTE, GIVING). Non-arithmetic paths (MOVE, VALUE init,
STRING/UNSTRING) correctly skip overflow. One path, one truth.

### PIC editing zero-suppression (NC124A 169/169)

Five distinct PIC formatting bugs in `FormatByEditPattern`:

1. **Floating symbol digit count**: `effectiveDigitCount = trueDigitCount - 1` when floating —
   one position is always reserved for the symbol itself. Fixed `PIC $$99` value 1234 → `$234`.

2. **Full-field zero suppression**: when entire integer part is floating AND value==0 AND no
   fixed `9` anywhere, blank the field. Space-fill: all spaces, skip floating placement.
   Asterisk-fill: all `*` but preserve `.` as decimal point.

3. **allIntegerSuppressed guard**: `case '9'` sets `allIntegerSuppressed = false` — fixed `9`
   in the integer part blocks full-field blanking. Without this guard, `PIC +9.99` value 0
   was incorrectly blanked to spaces.

4. **Skip floating placement after blanking**: when the entire field was blanked to spaces
   (fullFieldBlanked && !asteriskFill), don't run the floating symbol placement pass — it
   would re-insert `+`, `-`, or `$` into an all-spaces field.

5. **PIC P trailing scaling**: `FormatByEditPattern` wasn't dividing by `10^TrailingScaleDigits`
   before formatting. `EncodeDisplay` did this correctly; the numeric-edited path was missing
   the same scaling. `PIC ZZZPP` value 900 → now correctly shows `  9` instead of `900`.

### Doubled-quote un-escaping (CONTIN-TEST-9)

The preprocessor was correct all along — 322 quotes in the output = 160 literal characters.
The actual bug: `text[1..^1]` stripped outer quotes from ANTLR STRINGLIT tokens but never
converted `""` pairs to single `"` characters. A 160-character string of quotes became 320
characters internally. Added `.Replace(q+q, q)` in all three extraction sites:
BoundTreeBuilder.BindNonNumericLiteral, SemanticBuilder VALUE clause, ParseConditionLiteralValue.

The preprocessor continuation state machine (ScanLiteralState + pendingQuote tracking) was
a valuable addition even though the bug was downstream — it ensures correct continuation
handling for any future doubled-quote scenarios.

### REDEFINES family max-extent (RDF-TEST-9/10)

The hardest bug. Three attempts:

**Attempt 1** (failed): Compute group REDEFINES size from children, use that as
StorageLocation.Length. Caused NC171A regression — DIVIDE INTO B C D failed because my
grammar unification accidentally changed `divideIntoOperand` and `multiplyByOperand` from
`target+` (multiple targets) to single operands. Also caused RDF-TEST-11 regression because
`MOVE REDEF13 TO REDEF12` (overlapping source/dest) used the 120-byte REDEF12 size instead
of the 46-byte original overlap.

**Attempt 2** (failed): Retroactive expansion — compute layout normally, then add extra bytes
to working storage for oversized REDEFINES. This was architecturally wrong: REDEF13 was already
placed at offset 46 (original's end), not offset 120 (family max). Expanding the total size
doesn't fix the offset placement.

**Attempt 3** (success): `RedefinesFamily` tracker during layout. The main `ComputeLayout` loop
over 01-level items maintains a `currentFamily` that tracks the base offset and max extent. Each
REDEFINES group registers with its OWN declared size but updates the family's max end. When the
next non-REDEFINES 01-level item arrives, `currentFamily.NextSiblingOffset` determines where it
starts. REDEF13 now starts at offset 120 (after REDEF12's 120-byte extent), not offset 46.

Key insight from user: **separate storage extent from declared length**. Each group keeps its
own declared size for MOVE semantics. The family max extent determines only where the NEXT
sibling starts. This is why RDF-TEST-11 works: `MOVE REDEF13 TO REDEF12` uses each group's
declared length (120 bytes each), and since REDEF13 now starts at offset 120 (not 46), there's
no overlap corruption.

### Grammar regressions found and fixed

1. `multiplyByOperand` accidentally changed from `+` (multiple targets) to singular — broke
   `MULTIPLY A BY B ROUNDED C D` (COBOL Format 1 with multiple BY targets).
2. `divideIntoOperand` same issue — broke `DIVIDE A INTO B C D`.
   Both restored to `arithmeticTarget+` (multiple targets) and `arithmeticTarget+ | literal`.

### Session scorecard

| Test | Start | End | Key fixes |
|------|-------|-----|-----------|
| NC107A | 166/177 (6 fail) | 172/177 (0 fail) | Continuation, REDEFINES family, doubled-quote |
| NC112A | 31/32 | 32/32 (100%) | SIZE ERROR in GIVING form |
| NC124A | 158/169 | 169/169 (100%) | PIC editing: suppression, floating, scaling |

Total: 119 unit + 164 integration + 10 NIST golden-file (964 kernel). All green.

---

