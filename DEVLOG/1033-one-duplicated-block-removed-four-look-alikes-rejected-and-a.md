## Entry 1033 — 2026-07-26 22:31 PDT — One duplicated block removed, four look-alikes rejected, and a heuristic that would have deleted three real rules

The duplicated-block batch. One real artifact, four false positives, and the interesting part is the false
positives.

**The artifact.** The body of printed page 546 was emitted twice — once stranded at the end of the page-575 slice
behind a stray horizontal rule and a stray running-header H1, and again in its proper place under the page-576
anchor. Four SECTION anchors were duplicated, and because the stray copy came first in file order, every
intra-document link and heading-derived TOC entry resolved to the copy under the WRONG page anchor. A grep-based
citation gets two hits for one rule with no way to tell which is canonical — exactly how `spec-lookup` reads this
file. 21 non-blank lines removed; the anchored copy survives.

**⛔ The heuristic was wrong.** I detected candidates as "the same text repeated within 15 lines with a page anchor
between." Five regions matched. Only one was a defect:

- **p275** — items c) and d) belong to *rule 4* before the break and *rule 5* after. The standard states the same
  conditions for BY REFERENCE and for BY VALUE parameters, so identical wording under different rules is correct.
- **p328** — Table 6's caption repeats because the table CONTINUES across the page break; the rows differ either
  side.
- **p647** — a) and b) belong to *rule 5* (FORMATS 1–3) and *rule 6* (FORMATS 4 AND 5).
- **INSPECT examples** — two worked examples legitimately sharing a code-block header and a table header.

Four false positives out of five candidates. Adjacency cannot distinguish a transcription artifact from a standard
that repeats itself, and standards repeat themselves constantly — that is what makes them precise.

**Had I trusted it and swept all five, I would have deleted three passages of distinct normative text** — two
General Rules of a DIVIDE format among them — **and every gate would still have been green.** `--check` passes,
the anchor count is untouched, the catalog count barely moves. Deleting a whole rule breaks none of the invariants
I have been leaning on all day. That is worth writing down: the gates prove I did not corrupt the STRUCTURE; they
say nothing about whether I deleted the right lines.

**The invariant that actually works** is duplicated SECTION ANCHORS plus stray structural markers — things unique
by construction. Verified after the repair: zero section anchors and zero page anchors appear more than once in
the entire file. That is checkable. Adjacency is a guess.

The repair is scripted with assertions rather than hand-edited: it locates boundaries by CONTENT (the file already
shifted once today under the heading repair, and will shift again), asserts both stray markers are present,
asserts exactly one of each duplicated anchor inside the stray region, refuses to run if that region contains a
page anchor, and re-checks anchor uniqueness after writing.

Gates: `--check` 0 · anchors 1261 · catalog 3,790. **~186 findings remain.**

