## Entry 1063 — 2026-07-27 22:23 PDT — Two rendering defects the gates could not see, and the table Table 10 never was

The owner read the transcription and reported two things no verifier had caught: *"The formatting is basically
unintelligible"* and *"the line drawing characters do not connect. there are gaps"*. Both were real, both had
been true for a long time, and neither was visible to any gate — because every gate reads the file as TEXT and
the reader reads it as a RENDERED PAGE. That is the whole lesson of this entry: fidelity checks that never
render cannot see what the document looks like.

**Defect 1 — runaway emphasis.** A bare `*` in prose or a table cell opens Markdown emphasis. The document has
137 of them: `| * | comment indicator |` in the reference-format tables, `(operand-1 * operand-1)` in the
arithmetic-expression rules, `'*'` throughout the zero-suppression discussion. Each one italicised everything
that followed until the next stray asterisk closed it, which also suppressed block parsing — so headings
rendered as literal `######`, blockquotes as literal `>`, and paragraphs ran together. Ten lines in the file
already wrote `\*`, so the convention existed; it had simply never been applied systematically.

The fix escaped the 137 literal asterisks, skipping `<pre>` blocks, fenced blocks and inline code, where they
are already literal. It then went wrong twice, and both mistakes are worth recording because they are the same
mistake in opposite directions:

- The escaping pass could not tell `*Information technology*` (a standard's title, italic on the printed page)
  from `A * B`, so it flattened 82 deliberate italics across the normative references and the bibliography.
- The pass that RESTORED those italics rebuilt each line by escaping every asterisk outside a single-`*` span,
  which swept up the `**` of three bold runs.

What caught both was not inspection but an INVARIANT: escaping is supposed to change only backslashes, so
de-escaping every asterisk in the new file must reproduce HEAD exactly. 1306 asterisk-bearing lines, zero lost,
zero gained. Neither error would have been found by reading the diff — it is 986 lines long.

**Defect 2 — the box-drawing gaps.** FIGURE-STYLE.md rule 8 has said since the style settled that
`line-height: 1` is a hard constraint: a box-drawing glyph is drawn spanning the full em box precisely so
consecutive `│` meet, and at a previewer's ordinary 1.45 default every row boundary opens a gap, turning each
brace into a dotted column. The rule was written down and then left to the environment to honour — and a
Markdown file has no stylesheet, so "the environment" means whatever previewer the reader happens to open. The
constraint now travels with the document: all 484 figures open with `<pre style="line-height:1">`, emitted from
one constant (`render_figure.PRE_OPEN`) that `sweep_figures.py` reuses for both `--apply` and `--check`, so the
document and the generator cannot drift. `sweep_figures.py --check` is clean on all 484.

**Table 10 was never one table.** Chasing the last known-mangled table turned up a defect class instead. The
standard's long tables run off the bottom of a page and restart on the next under a repeated caption and a
repeated column header. The transcription preserved those page breaks — so Table 10's 24×24 picture-symbol
precedence matrix was two tables separated by a horizontal rule, and its second half's header row had swallowed
the first data row (`| Other symbols | | 9 | x | x | …` is a group label, a row label and a row of marks on one
line). Table 13 was FIVE tables. Table 12 was three, Table 21 nine, Table 1 two, and Table 6 was two with the
caption repeated verbatim and no "(Continued)" marker at all.

Table 10's matrix was rebuilt from the printed geometry rather than from reading order, because every cell in it
is positional: an 'x' means nothing except through the column it sits under, and reading order gives the marks
but not the empty cells, so the marks silently shift left. Each mark was snapped to a column centre measured off
the header, with a hard failure if any mark landed more than 9 pt from a column. The reconstruction is checked
three ways — the matrix must be square (24 rows against 24 columns), the row symbols must be the same list in
the same order as the column symbols, and the mark count must equal an independent count taken straight off the
page without snapping. 163 = 163. The fifteen rows the transcription already had turned out to be correct
cell-for-cell, which is the reason to measure rather than assume: the half that looked fine WAS fine, and the
half that looked broken was broken exactly where it looked broken.

18 joints merged in all, 108 lines of repeated caption and header removed. A repeated header row left in place
is not cosmetic — it sits in the body as a data row reading `Exception-name | Cat | Description`. All 124
Markdown tables now have zero ragged rows, which is the check that proves the merges landed cleanly.

Two smaller things fell out. The six captions written as `## Table N — …` headings were exactly the tables that
broke across pages; they are captions, not sections, and the other 34 were already bold, so they were
normalised. And the bibliography's entry [6] read `including I*SO 1989:1985/Amd 1:1992 …` — a transposed
asterisk that the printed page settles immediately.

Gates: `sweep_figures --check` clean (484), `verify_publishable` PUBLISHABLE (47,946 lines), 
`verify_acknowledgment` verbatim in both positions, 3,735 internal links with zero dangling.

**What this says about the verification set.** Every gate here passed before these defects were fixed and after,
which means none of them measures what the owner actually looked at. The audits check text against the PDF; they
do not check that the Markdown, rendered, is legible. The two defects were found by a human opening the file.
That gap is now closed by `scripts/spec/lint_rendering.py`: unbalanced emphasis outside code, `<pre>` without
the line-height, a column header repeating inside a table body, ragged rows, caption-as-heading, unbalanced tags,
dangling links. No PDF needed. It is clean on the current file and reports **536 defects** on the revision before
this commit — 37 emphasis, 484 line-height, 9 ragged, 6 caption — which is the evidence that it can fail. It also
found one more repeat the caption-keyed merger could not see, and one false positive worth recording: the
multi-column reserved-word lists are LAYOUT tables with an empty header row, so their blank spacer rows matched
it trivially. Only a header with content can be repeated.

