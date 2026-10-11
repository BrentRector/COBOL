## Entry 361 — M2 (WS-2002-FORMAT): `>>EVALUATE` / `>>WHEN` / `>>END-EVALUATE` — multi-branch conditional compilation

Completes the conditional-compilation trio (ISO §D.28 lists DEFINE + IF + EVALUATE) begun in 360. `>>EVALUATE`
(ISO §7.3.13) is the multi-branch / switch form, and it slots cleanly onto the machinery from 360.

Generalized the nesting frame from an IF-only record into a `Frame { Kind, ParentActive, Emitting, BranchTaken,
TruthForm, Subject }`, so the same stack carries both `>>IF…>>END-IF` and `>>EVALUATE…>>END-EVALUATE` levels and
they nest arbitrarily. `>>ELSE`/`>>END-IF` now act only when the top frame is an IF, and `>>WHEN`/`>>END-EVALUATE`
only when it is an EVALUATE — so a misplaced terminator can't corrupt the stack.

Both spec formats:
- **Format 1** `>>EVALUATE selection-subject` — each `>>WHEN object [THROUGH|THRU object3]` matches when the
  subject equals the object, or lies in the inclusive range (GR4). Alphanumeric equality is ordinal/length-exact
  (GR7). First matching WHEN's text is included; the rest (and `>>WHEN OTHER`) omitted.
- **Format 2** `>>EVALUATE TRUE` — each `>>WHEN constant-conditional-expression` is evaluated by the same
  recursive-descent evaluator as `>>IF` (so compound `AND`/`OR`/relations work in a WHEN).
- `>>WHEN OTHER` fires only if no prior WHEN matched (`BranchTaken`).

Refactored value resolution + relational comparison into shared statics (`ResolveToken`, `Relate`) used by both
the `>>IF` evaluator and the new `MatchWhen`, removing the duplicate that the first cut of `CondParser` carried.

Three integration tests: Format 1 string match selects the 2nd WHEN (`B`); Format 1 `THROUGH` ranges with no
match → `>>WHEN OTHER` (`HIGH`); Format 2 `>>EVALUATE TRUE` with a compound `N >= 10 AND N < 20` WHEN (`TEEN`).
The four 360 IF tests still pass (the frame generalization is behavior-preserving).

Still exact no-op on directive-free source → 364 NIST baselines untouched. Deferred: arithmetic/boolean
expression subjects/objects, and copybook-internal directives (pass runs before COPY).

Guard ALL GREEN: **1047 unit / 446 integration / 364 NIST**, 0 regressions.

