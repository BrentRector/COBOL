## Entry 1041 — 2026-07-27 00:47 PDT — The first COMPILER bug the reconciliation has proven: ON is an optional word

Batch 1's exit criterion, and the point of the whole exercise. Every repair so far has been to the transcription.
This is the grammar having inherited one.

**The rule.** §5.2.2 makes an underlined uppercase word REQUIRED; §5.2.3 makes a non-underlined one an OPTIONAL
WORD that may be written or omitted with no change of meaning. In every arithmetic statement's printed general
format `SIZE`, `ERROR` and `NOT` are underlined and **`ON` is not**. So `SIZE ERROR` and `NOT SIZE ERROR` are
legal spellings of those phrases.

**Measured, not assumed** — `figure_extract.py` reads the underline rectangles per word: `ON` comes back plain on
p632 (COMPUTE), p644 (DIVIDE), p703 (MULTIPLY), p607 and p756 (ON EXCEPTION), while `SIZE`/`ERROR`/`NOT` on those
same pages come back underlined. Not a detection artifact.

**The grammar contradicted itself, which is the strongest evidence there is.** `callOnExceptionPhrase` already
read `ON? (EXCEPTION | OVERFLOW)`. `arithmeticOnSizeError`, `computeOnSizeError` and `mcsExceptionPhrases` all
required the bare token. One file, two answers to the same question — so this was never a judgement call about
the spec, it was an inconsistency anyone could have found.

**Repro, before the fix:** `ADD A TO B SIZE ERROR DISPLAY "OVERFLOW" END-ADD` →
`COBOL0001: no viable alternative at input 'SIZE'`. Legal COBOL rejected. After: compiles, and **runs**
correctly — both handlers fire, 50+60 and 50×99 each overflowing `PIC 9(2)`. Verified the values, not just that
it ran.

**Swept rather than patched at the site**, per the standing rule that every bug is a pattern: all three rules now
take `ON?`, and no non-comment rule requires a bare `ON` before `SIZE` or `EXCEPTION`.

Golden `tests/conformance/85/optional_on_size_error` pins `ON` as omittable across all five arithmetic
statements, proves each phrase still binds to its own role, and includes a MIXED case — `ON` omitted on one
phrase and written on the other, in the reversed order `phrase_order_arithmetic` established — so the two rules
are shown to COMPOSE rather than merely coexist. Filed under 85 because §5.2.3 is not version-gated.

Gates: solution build clean · Unit 577/577 · Conformance full run green · filtered arithmetic gate 44/44.

