## Entry 1191a — 2026-08-06 13:46 PDT — PB46 (INVOKE half): BY CONTENT takes arithmetic-expression-1, and the first cut silently destroyed the identifier path my own comment had warned about

> ⚙ **RECONSTRUCTED 2026-08-07 from the primary sources — the commit message (`dd45ddfd`), its `kb/Work/` note and its diff — not written at the time.** The commit landed without a DEVLOG entry; the owner's call was to reconstruct where it can be done accurately. Every fact below is carried from those sources; nothing is recalled. **Lettered rather than renumbered** because entries 1192–1198 are already referenced by pushed commit messages, which cannot be edited.

ISO §14.9.23.2's BY CONTENT branch admits `arithmetic-expression-1 | boolean-expression-1 | identifier-5 |
literal-2` (read off the RENDERED general format). `invokeArgument` admitted `(dataReference | literal)`, **and
the failure was NOT the parse error the queue entry predicted**: `INVOKE O "M" USING BY CONTENT N + 1` matched
`N` as a dataReference and started a SECOND argument at `+ 1`, so it reported `COBOLNET0828: 2 USING argument(s)
for 1 formal parameter(s)` — an arity diagnostic about a rule the program does not violate, pointing the reader
at the method signature. **A silent mis-parse is worse than a rejection.**

**FOUR LAYERS, because a computed operand had no representation:** the grammar alternative; a `ContentExpr` slot
on `BoundInvokeArg` (which carried only Place / NumericLiteral / StringLiteral); an `OoBindInvokeArg` arm
applying §14.8.2.3.3 rule 2a — an expression is transferred "according to the rules of the COMPUTE statement", so
the formal shall be category numeric; and an `OoEmitter` arm rendering it through the SAME `NumStore` rescale the
identifier CONTENT arm uses.

⛔ **THE FIRST CUT CAUSED TWO REGRESSIONS, NEITHER A COMPILE ERROR, AND MY OWN COMMENT HAD WARNED ABOUT IT.** I
wrote `(nonNumericLiteral | arithmeticExpression)` with a note citing `feedback_grammar_precedence`, then walked
into it: `arithmeticExpression` SUBSUMES `dataReference`, so `BY CONTENT A` began matching the expression arm and
lost the §14.9.23.3 SR9/SR10 object-data rules, the §14.8.2.3.2 conformance check and the ref-mod handling —
while `BY CONTENT "XY"` stopped binding at all, because the proven literal arm keys on `literal`, not
`nonNumericLiteral`. Caught by probing ALL TEN argument shapes rather than only the one being fixed
(`feedback_probe_the_shape_the_subject_hides`).
**THE REPAIR:** keep `literal` first, drop `dataReference` from the alternation entirely, and recover the
identifier case IN THE BINDER from a sole-dataReference expression — the reduction `ConditionBinder` and
`IntrinsicBinder` already use, now SHARED rather than copied a third time (`feedback_one_rule_one_place`). The
grammar cannot express "a reference, unless it is part of an expression" without the ambiguity that caused the
original defect.

**VERIFIED, ten shapes:** `N + 1`→6 · `N * 2 - 3`→7 · `N`→5 · `42`→42 · `"XY"` · `A`→"ABCD" · BY REFERENCE ·
bare identifier · bare literal — all correct; and an expression against an ALPHANUMERIC formal now rejects with
the §14.8.2.3.3 citation (§14.9.25.3 Table 16 admits a numeric sender to an alphanumeric receiver only for an
INTEGER sender, which an expression cannot guarantee at compile time).

⚠ **WHAT REMAINS OPEN, neither for convenience.** (a) `boolean-expression-1` under BY CONTENT: the format admits
it, but it is a SEPARATE VALUE CHANNEL (`BoundBoolExpr`, D-B1) needing the `{boolExprAhead()}?` predicate plus
boolean marshaling — unguarded it is ambiguous with the arithmetic arm, because `booleanExpression`'s leaf is
`valueOperand`. (b) The CALL half stays BLOCKED on the program-prototype registry (P13): §14.9.4.2 Format 2 is
the program-prototype CALL and REPOSITORY has no `PROGRAM program-prototype-name` entry, so no parseable source
selects it, while Format 1's BY CONTENT is `{ identifier-2 }` only — widening `callByContent` would trade a
rejection of legal source for an acceptance of illegal source.

⚖ **ON THE EXTERNAL-ORACLE BAR:** GnuCOBOL cannot serve as the oracle for this construct — it does not implement
OO COBOL classes/methods, so INVOKE is absent from its corpus. The differential ran as a regression net (0 flips)
but **corroborates nothing here**; the spec's rendered general format is the authority, and saying so is better
than implying a survey that was not possible.

**GATE.** `=== BATTERY: ALL GREEN ===` on ONE whole run — FULL greenfield Conformance **4221/4221** zero skipped ·
Unit **3986/3986** · characterization **33/33** · guard-fast ALL GREEN with NIST 353 MATCH / 0 REGRESSION and
NIST AUDIT CLEAN · GnuCOBOL differential 1323 cases, **0 PER-CASE FLIPS**. Golden
`pb46_invoke_by_content_expression` (8 assertions incl. the two regression controls) · negative
`pb46-invoke-by-content-expression-alphanumeric-formal`.

