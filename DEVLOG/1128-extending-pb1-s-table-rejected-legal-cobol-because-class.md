## Entry 1128 - 2026-07-30 06:30 PDT - Extending PB1's table rejected legal COBOL, because "class numeric" and "an integer" are not the same rule

**THE INTENDED WORK.** Batch 2 adjudicated twelve functions, so PB1's `Verified` table could grow and convert its
34 mapped DIVERGES rows into actual enforcement. Eight functions added; four deliberately NOT, each with its
reason recorded in a new `DeliberatelyUnscreened` table so the omission cannot be mistaken for an oversight:
BYTE-LENGTH (§15.14.3 r1 admits ANY class), BASECONVERT (constrains USAGE, not class), CONCAT (its r2/r3 are
cross-argument USAGE rules a class screen would only pretend to enforce), CONVERT (the admissible argument depends
on a format KEYWORD). **GAP 3814 → 3810**, four rows closed and seven honestly PARTIAL.

**⛔ AND THE FULL CONFORMANCE RUN CAME BACK RED — TWO CASES, BOTH LEGAL COBOL.**

```
FUNCTION CHAR(WS-ED)      with  01 WS-ED PIC Z9.
error intrinsic-argument-class: FUNCTION CHAR argument-1 is of class alphanumeric; §15.3 requires class numeric
```

A corpus golden (`func_expr_arg`) and a `conformance-test` named `StringChannel_NumericEditedArg_DeEdits` both
pinned that as CONFORMING — and both were written with `AssertSpec`, i.e. derived from the standard by an earlier
session. **My screen contradicted a reading this compiler had already made and tested.**

**THE ERROR WAS IN READING THE SPEC, NOT IN THE CODE.** I mapped the catalog's `'n'` and `'i'` codes to one
screen, "class numeric". They stand for DIFFERENT RULES:

| rule | its words | numeric-edited operand |
|---|---|---|
| ABS §15.7.3 r1 | "shall be of **class** numeric" | EXCLUDED — §8.5.2.1 Table 2 puts numeric-edited under class ALPHANUMERIC |
| CHAR §15.15.3 r1 | "shall be an **integer**" → §15.3 type 6 | ADMITTED — type 6 accepts "an arithmetic expression that will always result in an integer value", and a numeric-edited item DE-EDITS to a defined numeric value |

DA6 had already settled the second half and I walked straight past it: its §8.8.1.1 screen matches only
`EditMask: null`, so it deliberately admits a numeric-edited operand as arithmetic. The evidence that my screen
was wrong was sitting in the codebase, in a comment, written by the wave immediately before this one.

**THE FIX MODELS THE DISTINCTION THE STANDARD DRAWS**, rather than adding a tolerance to make a test pass.
Numeric-edited is now its own member: it still counts as ALPHANUMERIC wherever a rule names a class — so the
`pb1-numeric-arg-numeric-edited` fixture, which pins `FUNCTION ABS(PIC ZZ9.99)` as rejected, stands untouched —
while a rule naming §15.3's INTEGER type admits it. Verified together: `ABS` over `PIC ZZ9.99` still rejects;
`ORD(CHAR(WS-ED))` over `PIC Z9` prints `034`.

**⚠ THIS IS THE SECOND TIME TODAY THAT ONE CODE, ONE SCREEN WAS TOO COARSE.** The first was the `ArgKinds` hint
column itself — unaudited, and screening from it rejected twelve corpus programs. This is the same mistake one
level in: a per-function CODE can only be as precise as the rules it stands for, and §15's argument rules are not
one shape. Where the previous lesson was "re-derive a dead table before enforcing it", this one is "a code that
covers two rules enforces neither correctly".

⚖ Worth noting what the golden could NOT do. `pb1-numeric-arg-numeric-edited` asserts the ABS reading and passed
throughout — it was never in tension with the defect, because the defect was in a DIFFERENT rule shape that no
fixture covered. A golden proves the case it encodes and says nothing about the case beside it.

**GATES.** Conformance **4149/4149**, zero skipped, nothing red. Unit **963/963**. Negative corpus 151/151 with
the new `pb1-numeric-arg-trig-family` fixture (ACOS/ASIN/ATAN over an alphanumeric operand).

⚠ **Two of the rows closed here did not exist yesterday.** `AR-15.10.3-1` (ASIN) and `AR-15.13.3-1/-2`
(BOOLEAN-OF-INTEGER) were recovered by the extractor fix — I found the denominator was short precisely because I
went to cite ASIN's argument rule for this table and the catalog had none.

