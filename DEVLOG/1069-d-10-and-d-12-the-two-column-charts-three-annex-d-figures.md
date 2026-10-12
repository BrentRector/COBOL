## Entry 1069 — 2026-07-28 03:05 PDT — D.10 and D.12, the two-column charts; three Annex D figures left

Both remaining flowcharts are drawn. Each needed a second COLUMN, which is what had kept them out of the two
generators: neither is a chain of boxes down one axis.

**D.12** (TEST BEFORE, two conditions) — condition-2's True branch does not rejoin the main column; it runs a
stack of two boxes down its own column, which then loops back to condition-1, while condition-2's False path
runs the body and loops back to condition-2 itself. Two independent loops at different depths plus a column of
its own. It was also the last RAGGED figure — its text used to overflow its own border.

**D.10** ((condition-1 OR NOT condition-2) AND condition-3 AND condition-4) — the parenthesised OR is decided
in the left column, and either way of satisfying it hands over to a second column holding the two ANDed
conditions. The left column's spine then continues BELOW the hand-over as the `false` collector that the right
column's exits fall back to, so the columns are not independent.

That last point produced the one interesting bug. The left spine is drawn last, but by then the right column
has already placed `├` junctions on it — and `vert` refused them, correctly, since it exists to catch a line
drawn through something. Passing through is a genuinely different operation from drawing through, so it is a
separate method (`spine`) rather than a relaxation of `vert`. Weakening the guard would have removed the check
that has now caught six would-be-invisible defects across these figures.

**⚠ A TEST QUIETLY STOPPED TESTING, and it is worth recording because nothing announced it.** The truth-chart
generator's `--verify` compared its output against the hand-drawn D.8 — a real check, which is how the
`str.center` bug was caught. Once `--apply` ran, D.8 in the file IS the generator's output, so the same command
now compares the generator with itself and prints a confident ✓ for a tautology. It still has value as a DRIFT
check, but it is no longer validation, and the code now says so where someone would otherwise trust it. The
validation that mattered is recorded in entry 1068 and its commit: 32 of 33 rows, the difference being a
correction to a terminal the original had left a column askew.

Annex D is now six of nine: D.7, D.9, D.10, D.11, D.12, D.13, D.14 drawn, D.8 already correct, D.5 rendered as
tables. **Three remain, and none is a flowchart** — D.1 the SEARCH decision chart, D.3 compilation-group and
run-unit structure, D.6 an example page layout. Each is a different shape and wants its own layout rather than
another parameter on an existing generator.

Lint red at 3, deliberately. Gates: sweep 484/484 · publishable at 47,105 lines · acknowledgment verbatim ·
3,811 links, zero dangling.

