## Entry 147 — 2026-03-25: LABEL RECORDS + MOVE Alphanumeric→Numeric (61→63)

**LABEL RECORDS STANDARD clause** (NC104A, NC105A): FD clause `LABEL RECORD(S) IS/ARE
STANDARD | OMITTED | data-name` — obsolete COBOL-85 clause, semantically inert. Added
`labelRecordsClause` parser rule + `LABEL`, `RECORDS`, `OMITTED` lexer tokens. NC104A
passes 141/141. NC105A passes 129/132 (3 deleted tests, 0 failures).

**MOVE Alphanumeric→Numeric/NumericEdited** (NC104A, NC105A): The COBOL-85 MOVE table
(§14.9.24) permits alphanumeric as source for numeric and numeric-edited targets. Our
`MoveLegalPairs` was missing these. Added both pairs + `MoveAlphanumericToNumeric` and
`MoveAlphanumericToNumericEdited` in `LoweringTable`. Runtime methods already existed.

**Subscript ambiguity identified** (NC134A, NC138A, NC139A): `ANIMAL (+8  +1  +3)` — the
parser treats `+8 +1 +3` as `8 + 1 + 3` (arithmetic) instead of three signed-literal
subscripts. Fundamental grammar ambiguity: `+` as both unary and binary operator. Deferred
for separate fix — needs grammar-level resolution.

Guard: 63 tests.

---

