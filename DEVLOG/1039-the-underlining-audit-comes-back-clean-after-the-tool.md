## Entry 1039 — 2026-07-26 23:58 PDT — The underlining audit comes back clean, after the tool accused two correct pages

Ran the underlining sweep over the whole standard — the class I said would be the next big one, because every
normative defect so far has been notation rather than prose. **694 pages carry general-format underlining, 2,215
underlined tokens measured. UNMARKED 0. OVERMARKED 0.** At page-set level the transcription is clean.

That is a negative result and it is worth as much as a positive one: it redirects the remaining effort away from
a sweep that would have found nothing.

**⛔ But the tool's first run was wrong, and wrong in the dangerous direction.** It reported two OVERMARKED
defects — `IN` on p322, `TO` on p673 — claiming the markdown underlines words the page does not. I checked the
raw rectangles before touching anything, and both words ARE underlined: their rules are 8.40 pt and 8.87 pt wide.
`figure_extract.py` was discarding horizontal rules under 9 pt as bracket feet, so it silently mis-read **every
two-letter required word in the standard**.

The tool was accusing a CORRECT transcription. Acting on it would have stripped real underlining *out* of the
reference document — the precise failure this whole effort exists to prevent, arriving from the direction I was
not watching. Everything so far has been about the transcription being wrong; this was the checker being wrong.

Width cannot separate a bracket foot from an underline. Position can, and the coverage and gap tests already did
that work — the width floor was redundant *and* harmful.

**Every claim I had already written into the spec from this tool was re-checked** against the corrected detector
— p607, p606, p632, p322, p649, p653, p661. **Contradicted: zero.** The bug under-detected, and none of the words
I had asserted on were two-letter cases. Verified rather than assumed, because the alternative was leaving wrong
statements in the reference.

**Two limits, both found by looking rather than reasoned about.** Tables: a row border is geometrically identical
to an underline, so every cell in a bordered row reads as underlined — p628's footnote markers `c`, `c,f`,
`a,c,f` are the clearest case, and most MIXED hits on table-heavy pages are this artifact. And MIXED compares
sets, so it can say a page is inconsistent but never which occurrence the transcription got wrong: it is a queue
for inspection, never a finding.

Restricting to FIGURE lines is what made the signal usable — in a general format every lower-case term is
hyphenated (`identifier-1`, `rounded-phrase`) while prose is full of plain words. That cut MIXED from 57 to 7 on
the statements range.

