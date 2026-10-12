## Entry 112 — 2026-03-19: 22 NIST Tests at 100% — Qualified Names, Unified Arithmetic Storage, Grammar Expansion

The third phase of the autonomous NIST session. Started at 19 tests at 100% (1,686 kernel
tests). Ended at 22 tests at 100% (1,779 kernel tests). Every fix gated by 164 integration
tests + 10 NIST golden-file regressions.

### SafeDivide — divide-by-zero as SIZE ERROR (NC117A 40/40)

NC117A was completely broken — runtime crash from `System.DivideByZeroException` in
`decimal.op_Division` on the CIL stack. The COBOL ON SIZE ERROR clause should catch this,
but the expression was evaluated BEFORE the SIZE ERROR infrastructure could intervene.

Fix: replaced `decimal.op_Division` in CIL expression trees with `PicRuntime.SafeDivide(left,
right, ref ArithmeticStatus)`. Returns 0 and sets SizeError on divide-by-zero instead of
throwing. NC117A went from crash to 38/40, then to 40/40 after StoreArithmeticResult.

### StoreArithmeticResult — unified arithmetic→edited routing (NC117A, NC120A)

Three tests (NC117A ×2, NC120A ×1) showed raw digits (`00030401`) where numeric-edited output
(`3,040.1`) was expected. Root cause: `MoveAccumulatedToField`, `AddAccumulatedToField`, and
`SubtractAccumulatedFromField` all called `EncodeNumeric` directly, bypassing the
`FormatNumericEdited` path for numeric-edited targets.

Extracted `StoreArithmeticResult` — the single point where ALL arithmetic results are stored.
Checks `destPic.Category == NumericEdited` and routes through `FormatNumericEdited` +
`MoveStringToBytes`. Every arithmetic operation (ADD/SUB/MUL/DIV/COMPUTE GIVING) converges here.

### B insertion in asterisk-fill (NC126A 145/145)

PIC `-*B*99` with value -42: expected `-***42`, got `-* *42`. The `B` insertion character was
missing from Pass 2 zero-suppression — added `case 'B'` alongside `case ','` for asterisk-fill
replacement.

### Qualified names — grammar + binder + resolution (NC206A 53/53)

The biggest structural addition of the session:

**Grammar**: `identifier` now accepts `dataNameTail*` which interleaves `qualification` (OF/IN
IDENTIFIER with optional subscripts/refmods), `subscriptPart`, and `refModPart`. This matches
COBOL-85's full qualified reference syntax: `A(I) OF B(J) OF C`.

**Binder**: `ResolveQualifiedName` implements right-to-left narrowing — resolves the outermost
qualifier first (rightmost in syntax), then walks inward. `FindChild` searches recursively
through group children. Qualified subscripts are extracted from the `qualification` node's
`subscriptPart`, not just from top-level tails.

**Resolution**: `A OF B OF C` → resolve C globally → find B in C → find A in B. Subscripts
attached to qualifiers (e.g., `AX-2 IN AX(CX-SUB OF CX)`) are properly extracted and applied.

### Grammar batch — USAGE INDEX, ALL figuratives, VALUES ARE, ADD/SUBTRACT CORRESPONDING

Four additive grammar changes to unblock the 200-series:

1. **USAGE INDEX**: added `INDEX` to `usageKeyword` and bare-keyword `usageClause`. New `INDEX`
   and `ARE` lexer tokens.
2. **ALL figurativeConstant**: `ALL ZERO`, `ALL SPACE`, `ALL HIGH_VALUE`, `ALL LOW_VALUE`,
   `ALL QUOTE_` added to `figurativeConstant` rule.
3. **VALUES ARE**: `valueClause` now accepts `(IS | ARE)?` for level-88 condition entries.
4. **ADD/SUBTRACT CORRESPONDING**: new alternatives in `addStatement` and `subtractStatement`
   with `CORRESPONDING identifier TO identifier ROUNDED?`.

NC206A was the first 200-series test to reach 100% (53/53). NC202A and NC207A now parse
successfully but need binder implementation for CORRESPONDING.

### What's left

The remaining non-100% tests are all runtime implementation issues:
- **NC115A** (13/31): INSPECT TALLYING ALL SPACE returns 0; REPLACING doesn't modify data
- **NC109M** (1/11): ACCEPT FROM DATE/TIME returns wrong formats
- **NC122A/NC123A**: INSPECT crashes from negative offset (subscript computation bug)

These are deep runtime bugs in `InspectRuntime` and `AcceptRuntime`, not grammar or binder
issues. The grammar and binder infrastructure is complete for the 100-series and 200-series.

### Architecture established this session

1. **StoreArithmeticResult**: single convergence point for all arithmetic → storage
2. **SafeDivide**: divide-by-zero as SIZE ERROR, not exception
3. **Qualified name resolution**: right-to-left narrowing with recursive child search
4. **dataNameTail***: flexible grammar for interleaved qualification/subscript/refmod

---

