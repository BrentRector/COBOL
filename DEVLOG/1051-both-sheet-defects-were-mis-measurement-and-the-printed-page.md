## Entry 1051 — 2026-07-27 17:11 PDT — Both sheet defects were mis-measurement, and the printed page settled them

The owner did not accept "two known defects" and asked what they actually were — then produced the printed
FLOAT-DECIMAL clause (folio 275) as the reference. That page turned out to be the fastest way to settle both,
because it is a KNOWN-GOOD image of the shape I was getting wrong elsewhere.

**Defect 1 — a choice-indicator bar adopting its neighbour's foot.** Folio 503 draws
`[ | encoding-phrase / endianness-phrase | ]` with four stems: brackets at x 239.93 and 344.51, bars at 244.60
and 339.60 (the taller pair, exactly as `figure_geometry`'s own docstring says). The bracket's foot runs
[239.93 … 243.43] and the bar begins at 244.60 — **1.17 pt** past the foot's far end. The foot test accepted a
rule touching EITHER end of a stem, so the bar claimed that foot, was classified a bracket, and drew a closing
corner in the middle of the group. A foot is anchored at its OWN stem and extends AWAY from it; testing that
instead fixed it. Blast radius, measured by diffing every stem in the standard before and after: **4 stems, all
bracket→bar, all the same defect** — two on folio 503, one on folio 631, and one in Figure 1's column ruler,
which carries no general format and produces no band at all.

**Defect 2 — rule 5 was enforced for half the delimiters.** The spacer that grows a two-alternative group to
three rows only looked at vector stems. Braces are ALWAYS glyph-drawn, so every two-alternative brace came out
two rows tall with nowhere to put its point — and the point is the single mark separating §5.2.6.3 "exactly
one" from §5.2.6.2 "at most one". The owner's screenshot shows it plainly on `where encoding-phrase is:`. Fixed
by counting both families; the point then has to be placed in the span's INTERIOR, because with only two rows
of text the measured middle piece maps onto the top or bottom row, where the hook wins.

Fixing those exposed a third, in the same family: a delimiter's hooks were snapped to the NEAREST row, so the
function-identifier's parentheses sat a row high, wrapped around an operand belonging to the brace beside them.
A hook brackets its content — top piece to the first row at or below, bottom piece to the last row at or above.
With that and a tightened top tolerance (the two populations are 0.4 pt and 3.8 pt, so 2.0 separates them
cleanly), folio 127 now renders bracket-paren-bracket exactly as the transcription draws it.

Verified against the printed page rather than against my own expectations: folios 275, 276 and 127 all match.
Sweep unchanged at 475 figures on 339 pages, zero collisions.

**What I got wrong in reporting.** I offered both as "defects for the owner to rule on" when they were bugs I
had not finished diagnosing — and one of them I had not even localised (I called it "the nested FLOAT-DECIMAL
brackets" without having measured which stem was wrong). A defect worth a decision has to be one where the
measurement is settled and only the choice is open. The genuinely open one is the stray space in
`[ END-ACCEPT  ]`, and it stays open for a stated reason: `]` at x 160.8 aligns with `NUMBER` at 161.4, tighter
than real alignments like FIRST/KEY/LAST at 0.9 apart, so no tolerance distinguishes coincidence from column.

