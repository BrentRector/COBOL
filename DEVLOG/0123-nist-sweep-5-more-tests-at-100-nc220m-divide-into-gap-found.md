## Entry 123 — 2026-03-20: NIST Sweep — 5 More Tests at 100%, NC220M DIVIDE INTO Gap Found

### Sweep results

Compiled 24 unvalidated A-tests and 12 M-tests. 5 new tests at 100% without any code changes:
- NC206A (53/53), NC210A (85/85), NC239A (8/8), NC248A (11/11), NC253A (61/61)

NC220M compiles and runs but has 5 DIVIDE INTO REMAINDER failures. Root cause: the REMAINDER
computation only works for DIVIDE GIVING (which uses an accumulator). For `DIVIDE A INTO B
REMAINDER R` (non-GIVING), the Binder's REMAINDER path checks `div.Receiver != null` and
skips when Receiver is null. Non-GIVING DIVIDE INTO stores the quotient directly into the
target field — there's no accumulator to feed into the REMAINDER calculation.

### Compilation failure patterns across remaining tests

| Pattern | Tests | Blocker |
|---------|-------|---------|
| PERFORM VARYING (inline) | NC231A, NC232A, NC234A, NC236A | Grammar: `performVarying` only in out-of-line PERFORM |
| ASCENDING KEY in OCCURS | NC238A, NC233A, NC237A, NC247A | Not yet supported |
| INDEXED BY parsing | NC244A, NC133A | Grammar issue with INDEXED BY |
| Subscript under-specification | NC245A, NC138A, NC139A | Partial subscripts |
| PIC trailing period | NC125A | Ambiguous sentence terminator |
| VALUE THRU | NC252A, NC201A, NC250A | VALUE THROUGH not recognized |
| Reserved word conflicts | NC211A, NC215A, NC219A, NC254A | STATUS, PROGRAM |
| OCCURS > 3 levels | NC243A | COBOL-85 limit |

### Numbers

- 37+ NIST tests at 100%
- 119 unit, 182 integration (1 skip), guard ALL GREEN

---

