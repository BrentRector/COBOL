## Entry 163 — 2026-03-28: Semantic Fixes — 77→82 NIST Tests

**5 more NIST tests pass** via semantic/compiler fixes:

1. **NC108M** — Allow string VALUE for numeric-edited items (VALUE is the edited display form)
2. **NC110M** — Guard now captures stdout for DISPLAY-only tests
3. **NC209A** — REDEFINES resolution: scan backwards for same-level sibling instead of global
   scope lookup that picked wrong same-named item across record boundaries
4. **NC214M/NC219A** — ALPHABET THRU detection: check for THRU keyword presence instead of
   `lits.Length >= 2` heuristic that misinterpreted ALSO values as THRU endpoints

**Other fixes:**
- ArithmeticExpression vs ArithmeticExpression comparison: new IrDecimalCompare/IrDecimalCompareLiteral
  IR instructions with CIL `decimal.CompareTo` emission
- PERFORM VARYING subscripted index: removed incorrect validation (COBOL-85 allows it)
- Guard stdout capture: third comparison path for DISPLAY-only tests

**Remaining (13 NC tests):**
- Parse: NC125A (PIC period), NC205A (continuation), NC216A (INSPECT), NC250A (ZERO arith), NC302M (AUTHOR)
- Semantic: NC208A (qualified paragraphs), NC225A (EVALUATE class conditions)
- Runtime: NC201A (PERFORM VARYING hang), NC220M, NC237A (known)
- Codegen: NC252A (RENAMES THRU)
- Flagging: NC303M, NC401M (no output expected)

---

