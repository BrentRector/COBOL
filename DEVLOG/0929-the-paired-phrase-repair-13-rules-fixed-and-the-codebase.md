## Entry 929 — 2026-07-19 20:55 PDT — The paired-phrase repair: 13 rules fixed, and the codebase already contained the answer

Item 0 of the close-line. Before touching anything I re-derived each claimed defect from the *corrected* diagrams
and CLI-probed it — and that discipline paid, because **three of my own automated scan's hits were false
positives**:

- **`searchAtEndClause` — NOT a bug.** The corrected SEARCH figures (Formats 1 and 2) show only
  `[ AT END imperative-statement-1 ]` — a plain bracket, and **no `NOT AT END` at all**. Our grammar *accepts*
  one, so this is over-permissiveness, not a missing form. It sits beside an existing NIST/IBM `END`-without-`AT`
  leniency arm, so it is plausibly deliberate; left alone and recorded rather than "fixed" on a bad premise.
- **`exceptionPhrase` / `onExceptionPhrase` / `notOnExceptionPhrase` — DEAD.** Defined in `CobolParserCore.g4`,
  referenced by nothing. Deleted — and worth deleting rather than leaving, because `exceptionPhrase` modelled the
  pair as an *exclusive one-of-two*, i.e. it was a loaded gun: wiring it up would have reintroduced exactly the
  defect being repaired.
- **`readAtEnd` — a bug my scan MISSED**, because its body reads `AT? END` rather than the literal `AT END` my
  regex required. Found by widening the pattern. A reminder that a scan tuned to the examples you already know is
  a scan that finds only those.

**The codebase already contained the correct shape.** `returnAtEndPhrase` in `Core/CobolIO.g4` reads:
```antlr
returnAtEndPhrase
    : AT? END statementBlock (NOT AT? END statementBlock)?
    | NOT AT? END statementBlock (AT? END statementBlock)?     // ← the reversed arm every sibling lacked
    ;
```
with a comment citing *"ISO §14.9.34.3 SR4: the AT END and NOT AT END phrases may be written in REVERSED order."*
RETURN happens to be the one statement where the standard states the any-order rule **in prose** rather than
leaving it to the figure's choice-indicator bars — so it got implemented correctly, and the other twelve did not.
That is the whole defect in miniature: **the rule was only ever learned where the prose said it out loud.** The
fix is therefore not an invention but a generalization of an in-repo precedent — [[feedback_singular_pattern]]
applied to a rule that had one correct instance and twelve wrong ones.

**Repaired (13 rules):** `arithmeticOnSizeError` (⇒ ADD ×2, SUBTRACT ×2, MULTIPLY, DIVIDE) · `computeOnSizeError`
(×2) · `readAtEnd` · `readInvalidKey` · `writeAtEndOfPage` · `writeInvalidKey` · `rewriteInvalidKeyPhrase` ·
`deleteInvalidKeyPhrase` · `deleteFileOnException` · `startInvalidKeyPhrase` · `stringOnOverflow` ·
`unstringOnOverflow` · and CALL, which needed a new `callExceptionPhrases` **container** rule because its two
phrases were two independently-optional slots on `callStatement` (`callOnExceptionPhrase? callNotOnExceptionPhrase?`)
— syntactically order-fixed even though each was individually optional. Four of these (`readAtEnd`,
`writeAtEndOfPage`, `deleteFileOnException`, and CALL's container) also gained the previously-missing
negative-only form.

The CALL container change propagated to **three** consumers in one change set per [[feedback_no_transitional_hacks]]
— the greenfield `CallBinder`, the legacy `CallBinder`, and `VersionConformancePass`'s `VisitCallStatement`
(which reads `OVERFLOW()` off both phrases for the CallOnOverflowRemoved2023 gate). The build caught all three
immediately, which is the exhaustive-dispatch property working as designed.

**Verified by running, not by reading.** All three originally-proven rejections now compile; the forward-order
control still compiles; and semantics are pinned: a reversed-order `ADD` on a non-overflowing value takes the
`NOT` branch and on an overflowing value takes the `ON` branch — the written order does not change which
condition selects which phrase.

**Goldens** (`tests/conformance/85/`, edition-invariant since '85): `phrase_order_arithmetic` covers all five
arithmetic statements in reversed order with both the overflow and non-overflow case per statement (8 assertions);
`phrase_order_overflow` covers STRING and UNSTRING. One good catch while pinning the second: I expected
`UNS-NOT` and got `UNS-ON`. The compiler was right and my expectation was wrong — `UNSTRING "AB,CD,EFG" … INTO`
two receivers leaves `"EFG"` unexamined, which is precisely §14.9.48 GR15(b)'s overflow condition. Checked the
rule before pinning rather than after, per [[feedback_diff_is_a_bug]]; the comment now carries the citation.

Corpus runner green at 288. The comprehensive gate (full Conformance + unit + characterization + the FULL legacy
integration suite, then `scripts/guard.sh`) is running — mandatory here because the change is to shared `.g4`
consumed by both compilers.

