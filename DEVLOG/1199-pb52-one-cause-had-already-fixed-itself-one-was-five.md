## Entry 1199 — 2026-08-07 07:15 PDT — PB52: one cause had already fixed itself, one was five functions rather than the one named, and one was two clauses that differ by a single word

**RE-MEASURED BEFORE IT WAS WORKED, and the table had moved under the note.** PB52 listed three rows measured on
2026-08-05. On 2026-08-07:

| source | 08-05 | 08-07 |
|---|---|---|
| `FUNCTION SQRT(SPACE)` | run-time `figurative 'S' in a numeric context` | ⛔ same |
| `FUNCTION MAX(SPACE 5)` | run-time, same message | ✅ **already fixed** — bind, citing §15.59.3 r2 |
| `FUNCTION BYTE-LENGTH(5)` | run-time `a numeric literal is not a valid argument` | ⛔ same |

**Cause 2 closed itself.** PB31 landed the per-position/cross-argument SCHEMA this very note argued for — "three
instances now, which is the argument for the schema PB12 identified as its design half" — and
`CrossArgRule.AllSameClass` carries §15.59.3 r2. The argument was right and the work was already done. Only
re-measuring shows that; re-reading the note would have built it twice.

⛔ **CAUSE 1 WAS FIVE FUNCTIONS, NOT THE ONE THE NOTE NAMED.** `IntrinsicArgumentRules.Verified` is deliberately
fail-open — a function is screened only once its §15.x rule has been READ AND CITED, because asserting the
catalog's unaudited `ArgKinds` column rejected 12 legal corpus programs (PB1). SQRT was absent; so were **SIGN,
COS, SIN and TAN**, each carrying the identical rule, each clause read individually (§15.84.3 / §15.81.3 /
§15.20.3 / §15.82.3 / §15.89.3 r1).

⚠ **AND I READ THE WRONG CLAUSE FIRST: SQRT IS §15.84, NOT §15.81 — §15.81 IS SIGN.** A first pass read
§15.81.3's "Argument-1 shall be of class numeric", validated it clean, and took it for SQRT's. The text is real;
the clause belongs to another function (`feedback_a_real_clause_can_answer_a_different_question`). All five were
re-extracted BY CLAUSE ANCHOR afterwards, so the function-to-clause mapping is mechanical rather than recalled.

⚠ **A FIXTURE NAME IMPLIED THE COVERAGE THAT WAS MISSING.** `pb1-numeric-arg-trig-family` screens ACOS, ASIN and
ATAN — the three INVERSE functions — and its comment says exactly that. Its NAME does not: a reader asking "is
the trig family screened?" reads yes, while COS/SIN/TAN sat unscreened. Nothing was claimed falsely; a filename
did the implying, which no assertion can catch.

⛔ **CAUSE 3 IS TWO CLAUSES THAT DIFFER IN ONE WORD.** Both §15.14.3 r1 and §15.50.3 r1 admit "a data item of any
class or category", so only a LITERAL argument is restricted — a screen on the operand's CLASS would reject the
numeric ITEMS both permit, which is PB1's direction again. And the admissible literal sets are NOT the same:

```
§15.50.3 r1  LENGTH       an alphanumeric, national, or BOOLEAN literal …
§15.14.3 r1  BYTE-LENGTH  an alphanumeric or national literal …            ← no boolean
```

`FUNCTION LENGTH(B"101")` is legal and `FUNCTION BYTE-LENGTH(B"101")` is not, so ONE shared check would have got
one of them wrong. Both now emit a cited bind diagnostic through one `InadmissibleArgument` helper taking the
admissible set per clause. Verified at all four corners, including `LENGTH(N"AB")` = 2 character positions
against `BYTE-LENGTH(N"AB")` = 4 bytes (D-N1).

⚠ **TWO THINGS DELIBERATELY UNCHANGED.** The other `BoundExprError`s in those folds stay: a ref-modified, ODO,
ANY LENGTH or DYNAMIC LENGTH argument has a RUNTIME length a compile-time fold genuinely cannot produce — a
staged gap, correctly loud where it is found, and a different family from an inadmissible literal. And the new
helper does NOT take the `--permissive` downgrade the class screen applies: that warning reads "accepted … with
the existing coercion", and here there is no coercion — the fold has no value to produce, so a warning would be
followed by the identical run-time abort. Permissive is the migration mode for constructs an EDITION removed.

**GATE.** Conformance `~Corpus|~Negative|~Intrinsic|~VersionMatrix` **2646/2646** zero skipped · Unit
**4028/4028** zero skipped. Golden `pb52_intrinsic_argument_stage` (11 assertions, weighted to the LEGAL side
because that is what constrains the fix) · negative `pb52-intrinsic-argument-stage`.

