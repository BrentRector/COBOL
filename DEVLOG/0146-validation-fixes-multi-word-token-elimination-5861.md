## Entry 146 — 2026-03-25: Validation Fixes + Multi-Word Token Elimination (58→61)

Three quick validation fixes unblocked 3 NIST tests:

**CBL2605 DIVIDE REMAINDER too strict** (NC203A, NC251A): Rejected numeric-edited REMAINDER
targets. COBOL-85 §6.4.5 allows both numeric and numeric-edited. Also removed the "integer
only" restriction — REMAINDER can have decimal places. One-line fix in `IsValidRemainderTarget`.

**CBL0901 MOVE NumericEdited→Numeric rejected** (NC222A): `MoveLegalPairs` was missing the
`(NumericEdited, Numeric)` pair. COBOL-85 §14.9.24 allows this — the runtime de-edits the
source. Added the pair and wired `MoveNumericEditedToNumeric` in `LoweringTable`.

**Multi-word lexer token elimination (production-quality refactor):** Removed all 5 multi-word
lexer tokens (`NEXT_SENTENCE`, `BY_REFERENCE`, `BY_VALUE`, `BY_CONTENT`, `BLANK_WHEN_ZERO`)
and replaced with individual token sequences in parser rules. This fixes any COBOL statement
that wraps across line breaks after preprocessing — the lexer can now match `NEXT` on one line
and `SENTENCE` on the next. Added `SENTENCE` as standalone lexer token. `BLANK WHEN? ZERO` now
accepts the optional `WHEN` per COBOL-85 spec.

**Why this matters**: Every multi-word lexer token was a latent bug — any could break when
a COBOL source line wrap happened to split the multi-word construct. The refactor eliminates
the entire class of bugs, not just the one that NC208A exposed.

Guard: 61 tests (NC203A, NC222A, NC251A added).

---

