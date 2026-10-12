## Entry 1050 — 2026-07-27 16:55 PDT — Band detection keyed to the clause structure; then an invariant found six more bugs

Yesterday's entry ended with band detection broken and the cause diagnosed but unfixed. The fix is small and the
consequences were not.

**The structure was already in the document.** A bold numbered heading opens and closes every region; only a
region headed `General format(s)` carries figures; inside it a `Format N (label):` line separates one format
from the next. That is three rules and it replaces every threshold. Geometry never could have worked — row
spacing WITHIN one general format varies more than the spacing BETWEEN two of them (ACCEPT Format 3 steps its
operand rows 4.9 pt apart and its phrase groups 24 pt; COMPUTE's two formats sit 31 pt apart), so no gap
separates them. Font size looks like a second signal and is not: figure text is 9.7 pt on folio 754 and 10.7 pt
— identical to the prose beside it — on folios 602 and 577. The standard sets each figure to fit.

Whole standard: **475 figures on 339 pages.** START is one figure again instead of three; COMPUTE splits into
its two Formats with the trailing "where rounded-phrase is described in…" note excluded.

**Then I added an invariant, and it turned a one-fix session into a seven-fix one.** A delimiter must never be
drawn onto a character. The motivating case was `[ END-START ]` rendering as `|N]-START` — still figure-shaped,
still plausible, completely wrong. Making that abort the run exposed, one page at a time:

- `covers()` gave a stem the row BELOW its group (`y1 + 6` slack where an INSET was wanted). Measured, an
  enclosed row's top sits 13.7–15.9 pt above its stem's bottom and the next row's top 1.9–3.6 pt below it —
  two clean populations, so the threshold was never delicate, only backwards.
- `cluster()` grouped by single-link CHAINING. Folio 577's left edge runs 89.5 · 90.8 · 92.2 · 95.9 · 96.4 ·
  97.7 with every step under 4 pt, so the chain swallowed a bracket, its bar, `AT` and `[ END-ACCEPT ]` into
  one column. Bounding a group by its WIDTH, and clustering each KIND of column separately, fixed it.
- Braces reserved only the rows their pieces sat on, so text packed through the rows between.
- **A whole delimiter family was being drawn as letters.** Enumerating every non-ASCII glyph inside every
  figure found `æçè`/`ö÷ø` — Adobe Symbol's extensible PARENTHESIS pieces, COBOL's own `(` `)` set full
  height. The function-identifier format nests bracket-paren-bracket, exactly as the transcription draws it.
  `¼` was the repetition ellipsis. Unmapped glyphs now abort rather than render.
- Handedness was inferred from a midpoint over nearby stems. A bucket holding ONE stem always produced a
  closing bracket, because a stem is never left of its own midpoint — the RESERVE clause. The feet already
  state it: they turn inward. `figure_geometry` measured them and threw the direction away; it now returns it.
- A brace's point sat at the arithmetic centre of its span rather than on the row where its middle piece was
  measured. ASSIGN's inner brace is four rows with its point on the second.
- Glyph pieces were grouped by column alone, so two braces in one column but different clauses merged into one
  tall brace — the file-control entry's LOCK MODE brace drew up through ACCESS MODE and FILE STATUS. The
  pieces carry their own role, so the cut at each top hook is exact and needs no threshold.

**Every one of those was the same mistake:** inferring something the printed page already states — a hand, a
centre, a grouping, a glyph's identity. The file's own docstring says take positions from measurement; these
were the places where it quietly did not.

Two defects remain, both for the owner's eye on the test sheet (`--sheet`, 21 figures over 12 pages): the
nested FLOAT-DECIMAL brackets of the USAGE clause, and a stray space in `[ END-ACCEPT  ]` — that one from a
COINCIDENTAL alignment (`]` at x 160.8, `NUMBER` at 161.4) that sits closer than genuine alignments do
(FIRST/KEY/LAST span 0.9), so no tolerance can separate them and I did not hand-tune it away.

I also spent the first part of this session on CA30 before being redirected — spec derived and confirmed
(§14.9.39.3 SR10d, and §11.4.4/§11.8.4 GR2 make "implements" transitive), no code written. Recorded so the next
session does not re-derive it.

