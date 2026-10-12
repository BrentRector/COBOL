## Entry 928 — 2026-07-19 20:25 PDT — All 226 spec diagrams reproduced from the PDF; the transcription is now trustworthy for syntax

The prose-figure pass finished, and both halves of the audit are now applied (`specs` submodule `763a521`).

**The combined census.**

| | prose figures | code blocks | total |
|---|---|---|---|
| diagrams checked | 195 | 49 | 244 |
| defective | 83 | 31 | 114 |
| DECISION-CHANGING | 28 | 20 | 48 |
| printed figures with choice indicators | 27 | 19 | 46 |
| …where the transcription dropped the bars | 25 (93%) | 18 (95%) | 43 (93%) |

Of the two prose survivors, one drew the bars but *described* them as an exclusive one-of-three choice — so the
correct "one or more, any order" semantics survived in **1 of 27 figures (4%)**. The rollup's finding is the one
that matters: the loss "always fails in the same direction — toward **falsely restrictive** syntax (legal source
made to look illegal)." That is exactly the bug class proved in 927, and it means the defect is *uniform*: our
compiler's failures from this cause will all be wrongful rejections, never wrongful acceptances. That is at least
a benign direction for a compiler — you find out loudly — but it is invisible to a corpus authored from the same
source.

**Beyond the bars**, 28 further prose figures had structural defects: braces rendered as brackets and vice versa
(required-vs-optional inverted), invented outer brackets, ellipses bound to the wrong delimiter pair, and — worst
— whole trailing phrases simply truncated. Two of those truncations land directly on live work: **STRING's entire
`ON OVERFLOW` / `NOT ON OVERFLOW` group with its `END-STRING`**, and **PERFORM Format 3's `WHEN OTHER` / `FINALLY`
/ `END-PERFORM` lines**. The latter is the construct the C5 research packet was designed from — which retro-
explains a chunk of that packet's confusion, including its inverted GO TO / RESUME reading. C5 must be re-derived
against the corrected figure before any of it is implemented.

**Applied:** 226 replacements (195 figures + 31 code blocks), zero skipped. Diagrams carrying choice-indicator
bars go from **1 → 46**, matching the 27+19 census exactly — a good independent check that the apply landed
correctly rather than smearing. Every reproduction now carries a `> **Figure notes**` block naming the underlined
words and, where bars exist, stating plainly what they permit, so a future reader skimming prose cannot lose the
structure again. Both passes also confirmed genuine NEGATIVES (RECEIVE `CONTINUE AFTER`, EVALUATE subjects, OPEN,
INSPECT tallying/replacing, CLOSE, INVOKE, TRIM, …) where bars are truly absent and must not be added — the census
is measured, not sprayed.

A mechanical note worth keeping: the first apply attempt was wrong. I applied the 195 figure replacements, then
went to apply the 31 code-block fixes — whose line numbers had been computed against the *original* file and were
now all shifted. Caught it before committing, reverted, merged both lists into ONE descending-by-line pass. The
lesson generalizes: **two independently-computed apply-lists over the same file must be merged and sorted
together, never applied in sequence.**

Still open from this thread: the six unverified compiler cross-check items (DISPLAY `AT LINE/COLUMN` ordering,
PROGRAM-ID `COMMON`+`INITIAL`, INSPECT `AFTER`+`BEFORE INITIAL`, SORT `FOR ALPHANUMERIC`+`FOR NATIONAL`,
INITIALIZE multi-category, screen SIGN optionality), the ten paired-phrase grammar rules to repair, and the
`>>TURN` owner question from 925.

