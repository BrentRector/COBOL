## Entry 1055 — 2026-07-27 18:37 PDT — The sweep, keyed on the clause hierarchy; pages are not a thing in Markdown

Built `scripts/spec/sweep_figures.py` to address the sweep-readiness blockers. I started it keyed on the PAGE —
"the nth figure of page N" — and the owner stopped that: **pages are a layout artifact, and the goal is to
remove page numbering from the transcription entirely, with references becoming intra-document links.** Re-keyed
on the CLAUSE HIERARCHY: a figure's identity is its clause number and Format number (14.9.8.2 Format 2), which
is what the printed page and the transcription actually agree on. `render_figure.find_figures` now returns the
clause and label beside each band, and `figure_key` resolves a continuation page's clause by walking back.

That was the right correction and not a small one: the page-keyed version could not even express a figure that
straddles a page break, and it is exactly the kind of coupling that makes the eventual page-number removal
expensive. Recorded as a standing goal in memory.

**THE GATE: words unchanged, notation replaced.** A figure is rewritten only where the generated form carries
the same WORDS as the text it replaces. Notation may change freely — that is the point of the sweep — but a
word appearing or vanishing means the target is wrong, and the figure is reported rather than written.

Getting the comparator honest took four rounds, every one of them MY bug rather than a real difference:
`<u>` tags counted as words (229 false mismatches); fence markers and back-ticks likewise; a blockquote-prefix
strip that ate one `>` from `>>COBOL-WORDS`; and a blockquote test applied to the UNESCAPED line, so the
transcription's `\\>\\>compiler-instruction` read as a blockquote and every compiler-directive format was
discarded as prose. Each looked like a transcription defect until measured.

Where it stands: **437 figures keyed · 258 replaceable · 179 needing attention** (88 clauses whose printed and
transcribed figure counts differ with no Format labels to pair by, 69 ambiguous spans, 22 word differences).
Several of the 22 are REAL defects the sweep surfaced — §8.4.3.1.2 runs the next format's label into the figure
line (`qualified-data-name-with-subscripts-1 Format 3 (reference-modification):`), and running headers sit
INSIDE figure regions. Both are page furniture, which the owner's directive retires anyway.

Not applied. A half-converted document — some `<pre>`, some fenced — is a worse state than either end, so the
sweep waits until the 179 are resolved. `audit_figure_structure.py` now reads `<pre>` as well as fences and
counts the house box-drawing glyphs, so a swept document cannot report a false all-clear; its docstring records
that `sweep_figures.py --check` supersedes it, being an exact regenerate-and-diff rather than a count shortlist.

Also landed: the repetition ellipsis is normalised to U+2026 BEFORE layout (the standard sets it three ways),
so the width change costs nothing and the transcription stays internally consistent.

