## Entry 1038 — 2026-07-26 23:41 PDT — The last eight LaTeX diagrams, and two defects only measurement could find

The transcription rendered 514 general formats as fenced ASCII figures and **eight as LaTeX**. That inconsistency
was not cosmetic. The ASCII figures carry `Figure notes` recording which words are underlined and what the
delimiters mean; the LaTeX ones carried none — so the notation that §5.2.2/§5.2.3 and §5.2.6 attach meaning to
was being held in a form nothing checks. ACCEPT Format 3 was one of the eight, and it had lost its choice
indicators *and* mis-stated underlining in two places.

**Every underline here was read off the page rather than carried across from the LaTeX**, and two normative
defects fell out that no sweep agent had reported:

- **p606, ACCEPT Format 2** — `YYYYMMDD` and `YYYYDDD` ARE underlined on the printed page. The LaTeX left them
  plain, which per §5.2.3 makes them optional words that may be omitted.
- **p653, EXIT Format 2** — in `LAST EXCEPTION`, `LAST` is underlined and `EXCEPTION` is NOT. The LaTeX
  underlined both. The identical phrase on p661 had it RIGHT, so the two contradicted each other and the
  measurement settles which. Recorded AS PRINTED and flagged rather than harmonised away — an un-underlined
  `EXCEPTION` is hard to read as a true optional word and may be a defect in ISO's own typesetting.

**A content loss that every gate would have missed.** The p606 source line carried the NEXT format's label —
`Format 3 (screen):` — appended after the LaTeX. Replacing the whole line silently deleted it, leaving page 607's
figure with no heading at all. `--check` stayed 0, anchors stayed 1261, the catalog stayed 3,790, because a
format label is not a rule, an anchor, or a heading. I caught it by reading the result, which is the only reason
it is not still missing. Restored; the script re-emits it; a re-run is proven to reproduce the file byte-for-byte.

That is the lesson worth keeping from today: the gates verify structure, and structure is not content. Line-level
replacements have to be read, not just counted.

Gates: `--check` 0 · anchors 1261 · catalog 3,790 · zero LaTeX general formats · zero `<u>` inside code fences.

