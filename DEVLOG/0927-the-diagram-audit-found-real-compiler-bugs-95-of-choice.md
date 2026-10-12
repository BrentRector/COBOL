## Entry 927 — 2026-07-19 20:05 PDT — The diagram audit found REAL COMPILER BUGS: 95% of choice indicators lost, and we reject legal COBOL because of it

The code-block half of the diagram audit finished, and it converted a documentation-hygiene task into a genuine
bug hunt. **The compiler rejects source the standard permits, and the defect traces directly to the transcription.**

**THE CENSUS.** 49 fenced code-block diagrams; **19 carry choice indicators in the printed standard; the markdown
preserved them in exactly ONE** (PICTURE Format 1 — the one I had already fixed by hand). **18 of 19 lost — a 95%
loss rate**, 17 of them decision-changing. That single surviving instance is why a corpus-wide grep for `|` in
these blocks returned a count of 1, which is what tipped me off. Fourteen of the losses are the positive/negative
conditional-phrase pair (`ON SIZE ERROR`/`NOT`, `ON EXCEPTION`/`NOT`) — precisely the case where a markdown-only
reader concludes the standard forbids writing both branches. The agents also confirmed genuine NEGATIVES (13
diagrams where bars really are absent and must NOT be added: RECEIVE `CONTINUE AFTER`, EVALUATE subjects, OPEN,
INSPECT tallying/replacing, CLOSE, INVOKE, TRIM, …), so this is a measured census, not bar-spraying.

**THE BUGS — proven by CLI probe against the built compiler, not by reading.** Ten paired-phrase grammar rules
exist, and they split into two defect classes:

*Class 1 — wrong ORDER rejected (all ten rules).* Every one is shaped `X … (NOT X …)? | NOT X …`, which admits
`ON`-then-`NOT` and each alone, but **not** `NOT`-then-`ON`. §5.2.6.4 is explicit that choice-indicator
alternatives "may be specified in **any order**." Proven:
```
ADD 1 TO A  NOT ON SIZE ERROR DISPLAY "OK"  ON SIZE ERROR DISPLAY "SZ"  END-ADD.
→ error COBOL0001: no viable alternative at input 'ON'
```
The forward order compiles clean. Same for COMPUTE. Via `arithmeticOnSizeError`/`computeOnSizeError` this reaches
**ADD (both forms), SUBTRACT (both), MULTIPLY, DIVIDE, COMPUTE (both)** — every arithmetic statement — plus the
six I/O rules (`readInvalidKey`, `writeInvalidKey`, `rewriteInvalidKeyPhrase`, `deleteInvalidKeyPhrase`,
`startInvalidKeyPhrase`).

*Class 2 — the NOT-alone form MISSING ENTIRELY (three rules).* `notOnExceptionPhrase` (`CobolParserCore.g4:1148`),
`searchAtEndClause` (`CobolControlFlow.g4:137`), and `deleteFileOnException` (`CobolIO.g4:442`) have no `| NOT …`
arm at all, so the negative-only form cannot be written. Proven:
```
DELETE FILE F  NOT ON EXCEPTION DISPLAY "GONE"  END-DELETE.
→ error COBOL0001: no viable alternative at input 'NOT'
```
Control: the same shape on ADD (which *does* have the arm) compiles — so the probe method is sound.

**Why this matters beyond the fix.** These are not obscure corners; `ON SIZE ERROR` on arithmetic and
`INVALID KEY` on I/O are everyday COBOL. A real program written in the legal-but-unusual order would be rejected
by our compiler with a parse error blaming the wrong token. And nothing in the markdown's *rule text* hints at it
— §14.9.1 and friends never restate phrase ordering, because the ordering lives entirely in the FIGURE. Only the
printed page carries it. This is the strongest possible evidence for the standing rule adopted in 926: when a
general-format diagram is load-bearing, render the PDF.

Also worth recording: this class of bug is invisible to the whole existing battery. 3699 conformance tests, 313
unit, 33 characterization, 167 legacy — all green at HEAD, none writes `NOT ON SIZE ERROR` before
`ON SIZE ERROR`, because the corpus was written against the same defective understanding. **A test suite derived
from a defective spec cannot detect the defect.** The negative-corpus discipline does not help either, since this
is legal source we wrongly reject rather than illegal source we wrongly accept.

Fix plan: repair the 10 rules to the each-at-most-once/any-order shape (with a duplicate-phrase diagnostic, since
"only once" is also normative), land the 31 corrected code-block diagrams, and add conformance goldens covering
the reversed order and the negative-only form for every affected statement. The prose-figure pass (195 figures)
is still running; its apply list lands with the same change set. Evidence:
`docs/rearchitecture/evidence/PHASE-13-spec-codeblock-repair.json` (incl. a 14-item compiler cross-check list —
the remaining items still to verify are DISPLAY `AT LINE/COLUMN` ordering, PROGRAM-ID `COMMON`+`INITIAL`,
INSPECT `AFTER`+`BEFORE INITIAL`, SORT `FOR ALPHANUMERIC`+`FOR NATIONAL`, INITIALIZE multi-category, and the
screen-section SIGN clause optionality). OPEN multi-mode was probed and is CORRECT.

