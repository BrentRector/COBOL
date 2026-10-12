## Entry 1129 - 2026-07-30 08:15 PDT - PB7: every zero-argument intrinsic was unreachable in the keyword-omitted form, and it compiled clean

**BATCH 3 MERGED — 56 rules over 12 intrinsics (COS…EXCEPTION-LOCATION-N), GAP 3810 → 3800, 181 rules
adjudicated.** 24 agents, zero errors. And it produced the worst failure mode the review has found.

**⛔ PB7 — SILENT COMPILE-THEN-CRASH.**

```cobol
REPOSITORY. FUNCTION ALL INTRINSIC.
    MOVE CURRENT-DATE TO WS-CD          *> compiles with zero diagnostics
```
then at run time: `NotImplementedCobolFeatureException: reference 'CURRENT-DATE'`. `PI` and `E` failed
identically — the entire zero-argument family.

§15.21.2's general format is `FUNCTION CURRENT-DATE` with NO parentheses, so with the keyword omitted
(§12.3.8.1 + §8.4.3.2.3 SR2) the reference is a **bare name — ZERO suffixes, not one**.
`KeywordOmittedFunction` opened `if (suffixes.Length != 1 …) return null;`, so it fell through to a data
reference, resolved to nothing, and landed in the runtime's not-implemented stage. The standard writes the form
itself at §D.14.3.6. Fixed narrowly: a bare name becomes a function reference only when the catalog says the
function admits zero arguments, so a declared item still wins and no other bare word is re-routed.

**⚠ AND IT WAS FOUND BY THE REFUTER, NOT THE ADJUDICATOR** — the third time in this review. The adjudicator had
reported the function's OTHER problems; the refuter went looking for what a bare reference does and found a class
of failure invisible to output-sampling, because the program compiles.

**TWO AUTHORING ERRORS OF MY OWN, both caught by running rather than by reasoning.**

⛔ **My golden's arithmetic was wrong.** I bounded π with `R < 3.1416` on a `PIC S9V9(4)` receiver — which stores
π as *exactly* 3.1416, so the correct value failed my own test and the corpus went red. This is the SECOND time
today I derived a value from the spec and forgot to derive how the receiver would STORE it (the first was
`000050.00` where a signed DISPLAY item renders `0000500{`). The fixture now carries 6 decimals, inclusive
bounds, and the reason.

⛔ **My validator crashed on untrusted input.** An agent emitted `editions` as a JSON list; `record_verdicts.py`
threw `AttributeError` from inside `validate`. A validator that CRASHES on malformed input tells the author
nothing about what to fix AND hides every other violation in the file — this batch had 33. It now type-checks
each adjudicated field before touching it and reports all 33 at once, with the corrected form inline
(`— write it as "85,2002,2014,2023"`). A batch file is written by an agent or by hand; it is untrusted by
construction, and the writer's own robustness is part of the mechanism, not incidental to it.

**PB8 OPENED AND DELIBERATELY NOT STARTED.** Reference-modifying a function result is a PARSE ERROR:
`MOVE FUNCTION CURRENT-DATE (1:4) TO WS-YR` and `FUNCTION UPPER-CASE("abc") (1:2)` both give
`COBOL0001: no viable alternative at input '('`. §8.4.3.3.3 SR2 (validated verbatim) explicitly contemplates a
reference-modified function-identifier and constrains only WHICH functions qualify; both of these do. So we
reject legal COBOL.
Root cause located, in the LEXER: `OnDefaultLParen` pushes SUBSCRIPT mode — which is what captures a ref-mod —
only when `PreviousTokenCouldBeDataName() && !PreviousIsFunctionName()`. The two shapes miss it for DIFFERENT
reasons: after a zero-argument function NAME the `PreviousIsFunctionName()` guard suppresses it; after a call's
closing `)` the previous token is not a data name at all. The lexer's own comment says the decision "is frozen at
lex time and cannot be repaired later", so the fix is a lexer-mode change plus a grammar tail — the riskiest
category here, and not something to start in the same pass that landed a blocker.

**GATES.** Conformance **4150/4150**, zero skipped, nothing red. Unit **963/963**. Corpus 396/396.

