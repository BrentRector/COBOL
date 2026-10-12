## Entry 1049 — 2026-07-27 15:10 PDT — Figure style settled by rendering; the generator works, band detection does not

Closing out the figure-rendering thread, and the process lesson is the durable part.

**The style is settled** — `spec-reconciliation/FIGURE-STYLE.md`. Six rules: `<pre>` not a fence (a fence renders
`<u>` literally, so it structurally cannot carry §5.2.2/§5.2.3) · BOX DRAWING only · square brackets, curved
braces with their point · `│` bars one space clear of any delimiter · minimum three rows per group ·
`line-height: 1`.

**Every one was settled by rendering candidates and looking. FOUR of the six the owner found**, from screenshots,
after I had reasoned my way to the wrong answer:

- I proposed Miscellaneous Technical glyphs as "faithful to ISO". Measured afterwards: **not one Windows
  monospace font contains U+23A1–U+23AD**, so the browser substitutes a proportional face per glyph and the
  columns drift — and the substitutes are not even mutually consistent, so a single figure skews internally.
- I then set `line-height: 1.55` while "fixing" the font, which breaks box-drawing tiling: the strokes only join
  at exactly one em. Every choice indicator had a gap at every row and I had introduced it.
- `╭│` reads as one compound mark; the owner asked for a space, and `╭ │` reads as the two distinct pieces of
  notation they are.
- The owner suggested turning two-row groups into three. **That turned out to be the FAITHFUL layout, not a
  workaround** — ACCEPT Format 3 is SEVEN rows in print, not the five I had hand-built. The standard gives each
  operand its own row with the phrase label centred between, which is exactly what supplies each delimiter with
  a middle piece. My flattening was the error, and the style rule and the printed layout are the same thing.

**The generator works from an explicit band.** `render_figure.py` builds a figure entirely from measurement and
asserts that stripping the `<u>` tags reproduces the laid-out text — the property that makes a 254-figure sweep
safe rather than 254 visual checks. Five layout bugs were fixed along the way, all invisible except in output: a
global x-scale that ballooned every gap; a row tolerance of 5.0 pt merging rows 4.9 pt apart; stems reserving a
column on their first row only, so text packed through them; the brace filling every middle row with its point
and becoming a comb; and page-wide handedness drawing a closing bracket as an opening one.

**Band detection does not work, and I stopped rather than keep trying.** Three attempts: it treats headings and
`Format N (…)` labels as figures, splits one figure across bands, and merges a trailing `[ END-xxx ]` into an
adjacent exception group where the bracket draws through the text. The cause is structural — row spacing WITHIN a
format varies more than the spacing BETWEEN formats, so no gap threshold separates them. Committed marked
EXPERIMENTAL with its failures recorded, rather than hand-picking bands to produce a test sheet that would have
looked like validation while testing nothing about scaling.

**The lesson worth carrying:** a figure style cannot be reviewed as source, only as output. Render before
sweeping, not after.

