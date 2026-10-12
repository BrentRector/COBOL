## Entry 1035 — 2026-07-26 23:31 PDT — The choice-indicator bars, restored by MEASUREMENT rather than by eye

Batch 1 sub-batch 2. Eight exception-phrase figures had lost their §5.2.6.4 choice-indicator bars, which is the
difference between "at most one of ON SIZE ERROR / NOT ON SIZE ERROR" and "both, in either order". The
transcription forbade what every real COBOL program writes.

**The important thing is not the repair, it is that this stopped being a judgement call.** Until now, deciding
whether a printed figure carries the bars meant rendering the page and squinting at it — slow, and subjective at
exactly the point where being wrong silently corrupts the grammar. But the brackets and bars are VECTOR
RECTANGLES in the PDF content stream. They can be measured, and vector geometry has no encoding to obfuscate, so
this sidesteps the shifted-cmap text layer entirely. New tool: `scripts/spec/figure_geometry.py`.

A bracket stem carries short horizontal feet; a choice-indicator bar is a bare rule, drawn slightly taller. Page
632 is unambiguous: stems at x=76.29/309.72 h=31.98 with feet at both ends; bare bars at x=81.19/304.36 h=37.58.

**The measurement independently reproduced every one of eleven agent verifications** — including the non-obvious
ones. Two groups on p635, p645 and p722 each, matching "the defect hits BOTH figures"; one brace-group plus one
bracket-group on p607, exactly as described; and **zero** groups on p203, corroborating that finding's claim that
those braces carry no bars at all. Two independent methods agreeing this precisely is the strongest evidence this
effort has produced.

**It also found a site nobody had looked at — where the right answer was to leave it alone.** Page 66 prints a
bracket with bars and its markdown has none. It is Figure 1, the fixed-form reference-format ruler: those
verticals are tick marks, and the markdown already says the §5.2.6 meta-language does not apply. A sweep that
trusted the tool would have "repaired" a correct page. Recorded as a caveat in the tool's docstring.

**The class was enumerated before any edit.** 30 exception-phrase figure sites: 21 already correct, 9 defective.
The bars had degraded into three different wrong shapes — doubled corner glyphs, a bracket whose bars were simply
deleted, and nested plain brackets — so no single find-and-replace would have found them all. Eight are repaired;
p607 is a LaTeX diagram whose two verifiers DISAGREE about whether brackets wrap the LINE/COLUMN rows, so it is
held back for its own measurement rather than guessed at. After the repair: 29/30, p607 the only one outstanding.

**A near-miss worth recording.** The first draft attached figure notes by matching phrase text, and reported 13
notes for 8 repairs. `ON SIZE ERROR imperative-statement-1` also appears in the already-correct ADD, MULTIPLY and
SUBTRACT figures, so it would have bolted redundant notes onto healthy transcriptions. Caught only because the
count did not match the number of repairs. Notes are now bound to the positions actually edited, and the script
asserts it restored exactly 8.

Gates: `--check` 0 · anchors 1261 · catalog 3,790 · no degraded delimiter shape survives anywhere in the file.

Still open, tracked separately: the GRAMMAR. `CobolParserCore.g4` has not been touched, and eight statements just
turned out to permit a handler pair the figures previously forbade.

