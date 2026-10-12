## Entry 1065 — 2026-07-28 01:20 PDT — A blind spot the sweep could not see, and what "superseded" was hiding

Went to re-key the four halted audits onto the clause hierarchy and found the premise wrong, in both directions.

**First, the reassuring half.** I had told the owner underlining "has no working gate". Measuring it: all 484
`<pre>` figures sit inside regions `sweep_figures.py --check` locates, as do 1,395 of the 1,401 lines carrying
`<u>`. The sweep regenerates each figure from the page and compares line-for-line, so underlining IS gated,
exactly. My earlier statement was too pessimistic and is corrected here.

**Then the part that matters.** Six `<u>` lines sit outside any swept region, so I checked them against the
page — and `figure_extract.extract()` said the words were NOT underlined. Rendering printed page 598 shows them
plainly underlined. The transcription was right and my measuring tool was wrong.

The cause: `is_underlined` accepts a rule whose top edge lies between 1.0 pt ABOVE and 4.0 pt below the word's
bottom edge. A word's bounding box includes DESCENDER space, so an underline drawn tight to the baseline of a
word with no descender sits *inside* the box. On that page it sits 1.8 pt above the bottom edge and was
rejected.

**This is the one failure mode `--check` structurally cannot catch.** The figure generator marks required words
from this same function. A word the function misses is missing from the generated figure AND from the
transcription — they agree with each other and both differ from the printed page. `--check` proves CONSISTENCY,
not correctness. Only measuring against the raw rectangles can find it, which is how this surfaced.

So I measured the offset distribution across the whole standard, and it is cleanly trimodal:

  +1.0 pt   340 samples   every general format             accepted, correctly
  −2.0 pt    14 samples   REJECTED — the real blind spot
  +5.0 pt    25 samples   a table's TOP BORDER under its caption — must STAY rejected

The −2.0 population is 11 pages, and **not one is a general format**: table column headers, the CLOSE
statement's prose sub-headings, two Annex D class labels, and the `true`/`false` terminals of the D.7–D.10
flowcharts. Widening the band to −2.5 changes **no figure at all** — all 484 still match — which is the
evidence that the normative content was never affected. That was the thing worth being sure of.

It did expose one real transcription defect. `**Account Class**` is marked `<u>` on printed page 1114 and its
sibling `CheckingAccount Class` on 1115 is not, though the page underlines both. Fixed. The table column
headers are not defects — Markdown renders a table header as a header — and the flowchart terminals are
recorded in the figures' own notes.

**A second thing the earlier fix missed.** The `line-height:1` repair keyed on `<pre>`, and four Annex D
illustrations are drawn from the same box-drawing glyphs inside ``` fences, which have no line-height either.
They rendered with a gap at every row boundary — the exact defect the owner reported — and the fix had walked
straight past them because they were never `<pre>` to begin with. The lint now keys on the GLYPHS rather than
on the tag, which is what actually needs the line-height.

**What this turned up about Annex D, which is now the largest open item in the transcription.** The general
formats are generated and exact; the fifteen Annex D illustrations were drawn by hand and five are not right:

  D.9, D.11, D.13    NOT DRAWN — the flowchart's labels in reading order, or a rough arrow sketch
  D.7                22 box walls in columns no rule reaches
  D.12                2 box walls in columns no rule reaches, and text overflowing its own border

The pattern in the undrawn three is odd and suggestive: each has a DRAWN sibling of the same family — D.9 next
to D.10, D.11 next to D.12, D.13 next to D.14 — so whoever drew these did the even-numbered ones and stopped.

Lint: clean on HEAD, **771 defects** on the pre-session revision. Gates: figures 484/484 · publishable at
47,801 lines · acknowledgment verbatim · 3,811 links, zero dangling.

