## Entry 1060 — 2026-07-27 20:28 PDT — THE SWEEP IS COMPLETE: every general format generated from the page

**483 of 484 general formats in `specs/ISO_COBOL.md` are generated from the printed page**; the one exception
is deliberate and counted — §12.3.6.2 carries Addendum correction C3 and must NOT be regenerated. `--check`
reports zero mismatches, zero word differences, zero count disagreements. `verify_publishable.py` green.

Driving the last eight clauses to zero split cleanly into two kinds, and the split is the interesting part.

**FOUR were my generator under-reporting figures.** A region that spans a heading-less page (§13.17.2's
`where screen-attribute-clauses is:`); a figure cut in two because `[ sentence ] … [ paragraph-name-1. …` was
read as PROSE, since `sentence` is a bare lower-case word — notation is the signal that settles it, and the
same blind spot was in the sweep's own test, hiding `identifier-1( leftmost-position : [ length ] )` and
`qualified-data-name-1 [ ( subscript … ) ]`; and a FALSE positive the other way, where the wrapped tail of a
prose table ("…or dynamic-capacity-table-format)") read as a one-line figure — its tell is the unbalanced
delimiter, since a real one-row figure is self-contained.

**FOUR were genuine transcription defects, all of one family: DUPLICATION AND RUN-IN.**

- Two RUN-IN FORMAT LABELS, exactly the defect predicted when this began: `SET { identifier-7 } … TO
  identifier-8 Format 10 (data-pointer-arithmetic):` and `qualified-data-name-with-subscripts-1 Format 3
  (reference-modification):`. The next format's label had been swallowed into the previous figure's last line,
  which silently merged two figures and lost a label.
- The SPECIAL-NAMES paragraph carried `feature-name-1 IS mnemonic-name-2` / `device-name-1 IS
  mnemonic-name-3` TWICE — once inside its figure and again as inline code after the notes.
- §10.6.1 carried the whole method-definition figure twice, character for character.

**The conservation check closed the loop.** 175 words lost, 4 gained, and every one traced: the duplicate
PICTURE figure and its incorrect note, the duplicated `DATA DIVISION.`, feature-name/device-name, and
method-definition figures, the prose bullet lists replaced by real figures — and the 4 gained are the C3 note
I reworded. Nothing moved that I did not move.

**What the exercise actually proved.** The point was never the box-drawing. It was that a figure can be
DERIVED rather than asserted, and therefore checked: eight transcription defects surfaced by a machine that
simply asked whether the words on the page and the words in the document were the same set. Two of them —
the run-in labels — had silently destroyed a Format boundary, and one (the contradictory PICTURE notes)
disagreed about whether `FOR` may be omitted, which is grammar.

