## Entry 1052 — 2026-07-27 17:26 PDT — Spacing: a blank row between groups, and columns that stop at the group

The owner reviewed the sheet against the printed page and rejected the spacing: ACCEPT was missing the blank
rows before ON EXCEPTION and before END-ACCEPT, COMPUTE and the others had the same fault, and SELECT was
"badly formatted". All three are one root and one consequence.

**A blank row between groups.** The printed figure separates ACCEPT's `AT` group, its exception group and
`[ END-ACCEPT ]`, and the house worked example has always shown those blanks — the generator was collapsing
them. Where they go cannot be measured as a gap: **19.0 pt separates the exception group from `[ END-ACCEPT ]`
while 17.3 pt separates the two rows INSIDE that group.** That is the same trap that defeated gap-based band
detection one level up, and the answer is the same — ask the structure. Two adjacent rows belong together when
some delimiter spans both; otherwise they are different groups. One rule, no threshold.

**SELECT was badly formatted for a deeper reason: columns were packed across the WHOLE figure.** A file-control
entry stacks two dozen independent clauses, so a clause could be shoved about by a wider one three clauses
away. That is where `[ ORGANIZATION  IS  ]`, `RECORD  DELIMITER`, and `[ FILE STATUS IS data-name-4  ]` came
from. Vertical alignment carries meaning between rows the same delimiter spans and nowhere else, so the group
id now joins the column key and each group packs in its own space.

**That exposed the last one, and it retired a "won't fix".** Within a row, words were still being aligned
individually across rows — which aligns coincidences: `ERROR` on the ON SIZE ERROR row sits at x 130.6, `SIZE`
on the row below at 129.7, **0.9 apart, exactly the spread of a genuine alignment** (`FIRST`/`KEY`/`LAST`). I
had reported that shape as unfixable-by-tolerance and left `[ END-ACCEPT  ]` carrying a stray space. It was the
wrong frame: a row is a sequence of CELLS — runs of words between delimiters — and a cell's words flow with
single spaces because they are one phrase. Only cells align. `[ END-ACCEPT ]` came right on its own, which is
the tell that the earlier diagnosis was describing a symptom.

Also fixed a regression I introduced an hour earlier: mapping a delimiter's BOTTOM hook directionally (as I had
just done for the top) threw `RECORD` out of the ORGANIZATION clause's brace. The bottom hook's glyph is ~13 pt
tall and starts above its last enclosed row as often as below it — ORGANIZATION's begins 1.4 pt ABOVE `RECORD`
while ACCEPT's ends 9.4 pt below `COL` — so there is no direction to exploit and nearest-row was already right.
Only the top hook has a direction. And `scripts/render-spec-page.py` still pointed at the pre-move `specs/`
path, so it could not open the PDF at all; it globs `specs-private/` now.

475 figures over 339 pages, zero collisions, unchanged throughout.

**One known limitation, now stated as a row-model question rather than a defect.** Where a clause label is
vertically CENTRED beside a group, its centring offset becomes a row of its own and stretches a neighbouring
group by a row: ASSIGN's inner brace draws four rows where print has two. The same treatment is exactly what
gives ACCEPT's `AT` its own row — which the owner approved as faithful — so the two cases cannot be separated
without a two-dimensional row model. Recorded in §0 for an owner call rather than guessed at.

