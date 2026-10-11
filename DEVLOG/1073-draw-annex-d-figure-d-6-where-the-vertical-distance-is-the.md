## Entry 1073 — 2026-07-28 12:17 PDT — Draw Annex D Figure D.6, where the vertical DISTANCE is the content

The last undrawn Annex D illustration. `lint_rendering.py` had been red at exactly 1 for it; it is now clean,
and the transcription has nothing outstanding.

**What was there.** A three-column Markdown table, and it was faithful — all 150 words present, in order. What
a table cannot carry is the thing Figure D.6 is *about*. It is a picture of a printed page: one vertical rule
for the edge of the form, the four form positions named down its left, the six PAGE clause phrases down its
right, and the report content beside them. A table gives every row the same height, so both of the standard's
meaningful voids collapsed to nothing — the gap mid-body that says "and further body groups follow", and the
gap between PAGE LIMIT and the last line the sheet can physically print, which is the entire reason the
logical and the physical bottom of form are drawn as two different lines. The words survived; the figure did
not.

**Drawn from the measured page.** Rows are placed at `round((y - 103.9) / 8.7)` — the printed y of every line,
at the page's own pitch — which is what reproduces both voids without anyone deciding how big they should be.
The `<…>` content lines are centred on an axis taken as a fraction of the content column (the printed page
sets them by tab; their ten centres average 25.4 characters into the column, and land back at the same 36%
across it). Columns are the one thing that could NOT be taken from the page: the printed form labels wrap
("Physical Bottom of" / "Form") to fit a narrow column, this draws them on one line, so the columns are sized
from their content instead. Said so in the docstring rather than implying the whole geometry was measured.

**Two defects that generalise, and neither is about this figure.**

- **A `<pre>` is raw HTML, so `<blank>` is a TAG.** D.6 is the first figure whose own notation is
  angle-bracketed — `<blank>`, `<Detail lines>`, `<Control Heading lines>`. Written literally, every
  sanitizing renderer drops the unknown tag and the words with it: fourteen lines would have rendered EMPTY
  while the file still contained the characters and every text-level audit stayed green. Exactly the
  silent-rendering class `lint_rendering.py` exists for, and it did not cover it. The canvas keeps the real
  characters (so the geometry and the collision guard measure what the standard prints) and `escape()`
  converts on the way out. New lint check **SWALLOWED**, proved by pointing the lint at an unescaped copy:
  14 findings, and 0 on the file as written. No other `<pre>` in the standard has an unescaped `<` — the 40
  that looked like candidates are all `>>` compiler directives.
- **The lint named the wrong file under its own verdict.** It takes a positional path so it can be run
  against an older revision — the mechanism that proves it can fail — and then printed the *default* path in
  the summary line. A gate that reports a filename it did not read is a gate you cannot trust the verdict of.

**Verified, not assumed.** Word conservation against the printed page is exact both ways: 150 words, empty in
both directions, PDF page 1162 (folio 1132) plus the header line the standard sets on 1161. Rendered it in a
headless browser and looked at it — the entities resolve, and the rule tiles continuously under
`line-height:1`. Re-applying the generator leaves D.3 byte-identical, so it is idempotent.

**Gates:** `lint_rendering.py` CLEAN (was 1) · `sweep_figures.py --check` clean, 484/484 ·
`verify_publishable.py` green at 47,172 lines · `verify_acknowledgment.py` verbatim. No compiler code
touched, so no test battery applies.

