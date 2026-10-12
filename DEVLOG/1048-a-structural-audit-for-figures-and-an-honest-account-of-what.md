## Entry 1048 — 2026-07-27 13:35 PDT — A structural audit for figures, and an honest account of what it proves

The owner believes many syntax diagrams are structurally wrong and asked for the audit that would settle it.
Built `scripts/spec/audit_figure_structure.py`, comparing printed delimiter structure against the transcription's
across every general format.

**First, the representation premise he had been given elsewhere was false:** that Markdown cannot draw multi-line
brackets or vertical bars. It can, and this document already does, with the Unicode bracket and brace extension
glyphs (U+23A1–U+23AD). START on page 784 draws a five-row bracket, a three-row brace and a pair of choice
indicators, and it matches the printed page. Three of the four semantics he was worried about are already
preserved; only underlining cannot live inside a fence, and it is carried in the figure note.

I argued against both alternatives he was offered. Combining low line (`F̲I̲R̲S̲T̲`) breaks `grep FIRST` and would
disable every tool built this week. SVG un-does the decode — it makes the standard un-greppable again and adds a
second source of truth that can drift from the text.

**Three false starts building the audit, all the same shape as every other checker bug this week — a measure that
looks equivalent to the thing being measured and is not:** counting `|` CHARACTERS rather than bar COLUMNS (a
printed indicator is one continuous rule; the transcription redraws it per row, so every correct two-row group
scored as a mismatch); counting delimiters anywhere on the page (reserved-word TABLES are all-uppercase, so the
lower-case-word test cannot separate them from figures, and their grid rules are identical to bars — 122 of 260
pages "disagreed" while it was auditing the word lists); and counting only the extension-glyph convention, which
scored page 393 as five missing braces with nothing missing.

**Result, stated as what it is: 161 pages print a delimited general format; 46 show a count mismatch, 9 use the
ambiguous repeated-delimiter style. The 46 is an UPPER BOUND**, still contaminated by layout differences — a
shortlist for inspection, not a count of broken figures. Written into the tool's docstring so it cannot be quoted
out of context later.

**So the owner's question is not yet answered, and I would rather say that than hand him a flattering number.**
What is established: structure is the one class no sweep had touched, a tractable shortlist exists, and precision
requires comparing per-figure geometry rather than per-page totals. That is the next piece of work.

**A real defect found while answering his follow-up.** He asked why the bars are not drawn with a connecting
character so they appear continuous. They should be — `│` (U+2502) joins vertically where `|` does not, and it
also disambiguates a choice indicator from a Markdown table pipe, which is precisely what made the bar counting
fragile. Measuring it found the file already uses **both**: 210 fenced rows with `|`, 81 with `│`. Two characters
doing one job, in the reference document. A house-style sweep is owed.

