## Entry 1053 — 2026-07-27 18:02 PDT — A blank row between SIBLINGS, and two wrong theories before the right one

Owner: `BIT` / `COMPUTATIONAL` / `COMP` / `DISPLAY` in the USAGE clause need space between them, and the four
operands of the relation condition likewise — "this will likely generalize." It does, but I got the rule wrong
twice first, and both wrong versions looked plausible on the page I happened to be checking.

**Attempt 1 — blank between every adjacent pair.** Rejected before rendering: it would double ACCEPT Format 3
to fourteen rows, and the owner established its SEVEN rows as the faithful printed layout.

**Attempt 2 — blank between every pair except where one row is a brace's POINT row.** This looked right on
ACCEPT (`LINE NUMBER` sits on the point row, correctly separating `identifier-3` from `integer-1`) and on
USAGE. The relation condition killed it: THREE braces stand side by side there, and the MIDDLE brace's point
row falls on the row holding `literal-1`. So the left brace's four operands came out tight while its
neighbour's twelve were spaced — inside one figure. The printed page never does that.

**The rule is SIBLINGHOOD.** Two rows open up when they carry content at the same nesting depth, i.e. when they
are alternatives of one delimiter. `identifier-3` and `LINE NUMBER` are not siblings — the label sits one level
further out — so ACCEPT keeps its seven rows, while `BIT`/`COMPUTATIONAL` and `identifier-1`/`literal-1` are
siblings and open up. Depth is the count of delimiters to a word's left on its row, the measure the cell split
already uses.

**And one more correction inside that.** Counting depth per row does not work: a sibling enclosure that starts
or stops between the two rows shifts the count. In the relation condition the operand braces cover only rows
4-8 of the figure, so `IS <>` measured depth 3 and the row directly below it depth 1, and two plain siblings
failed to pair. Depth has to be counted against the delimiters spanning BOTH rows of the pair.

I also broke the file mid-session with a bad scripted splice — I anchored a replacement between two markers
whose order I had assumed, and the later marker occurred FIRST, so the block was duplicated rather than
replaced. It still parsed and still ran, and the stale copy won; the render simply did not change, which sent
me looking for a logic bug that was not there. Restored from the commit and re-applied as a single replacement.
Lesson: assert the marker order, or assert the replacement count — `str.replace` with `count == 1` verified.

475 figures over 339 pages, zero collisions, throughout.

