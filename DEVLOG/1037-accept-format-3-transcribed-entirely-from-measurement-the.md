## Entry 1037 — 2026-07-26 23:22 PDT — ACCEPT Format 3, transcribed entirely from measurement; the class closes at 30/30

The ninth exception-phrase figure, and the one I deliberately held back: it is a LaTeX diagram, and its two
independent verifiers **contradicted each other** about the AT phrase. One said there were no brackets around the
individual LINE / COLUMN rows; the other gave `[ AT {| [LINE NUMBER ...] / [{COLUMN|COL} NUMBER ...] |} ]`, with
brackets. Picking one by eye would have written a fabricated structure into the reference document.

**Measurement settles it.** In the AT band there are exactly two bracket stems — the outer pair at x=92.16 and
x=348.01 — plus the separate ON EXCEPTION pair below. None around either row. The first verifier was right. Just
inside the big brace sits a pair of bare rules with no feet, x=122.98 and x=337.97: choice indicators.

**The transcription was wrong in both directions at once.** A brace containing two BRACKETED rows reads as
"exactly one of (optional LINE) or (optional COLUMN)" — forbidding `AT LINE NUMBER 5 COLUMN NUMBER 10`, the
ordinary form, while permitting an empty `AT` with nothing after it.

**Underlining is now read off the page rather than asserted.** New tool `figure_extract.py` matches underline
rectangles to individual words. Measured: `LINE`, `COLUMN`, `COL`, `EXCEPTION`, `NOT`, `END-ACCEPT` underlined;
`AT`, `NUMBER`, `ON` not. Two more defects fall out — `COL` was left un-underlined although the page underlines
it (it is the required abbreviation of COLUMN, not a user-defined word), and the ON EXCEPTION rows had lost their
underlining entirely. `AT` being un-underlined also means `ACCEPT screen-name-1 LINE NUMBER 5` is legal.

This is the first figure transcribed **entirely** from measurement: words from the decoded text layer,
delimiters and underlining from the vector geometry. That is the workflow the grammar audit will run on.

**A mistake caught in review, not by a gate.** My replacement first carried `<u>` tags INSIDE the code fence,
where they render literally. Every other fenced figure in the file states underlining in its note instead. Fixed,
and now asserted: zero `<u>` inside any fence.

The exception-phrase class is **30/30 correct — closed.** Gates: `--check` 0 · anchors 1261 · catalog 3,790.

