## Entry 1057 — 2026-07-27 19:04 PDT — THE SWEEP IS APPLIED: 416 figures generated into the standard

**416 of 475 general formats in `specs/ISO_COBOL.md` are now generated from the printed page**, in the house
style, carrying their underlining. Word conservation over the whole document: **0 lost, 0 gained** — the
transcription says exactly what it said before, drawn from measurement instead of by hand. `--check` reports
every one of the 416 matching its generated form, and `verify_publishable.py` stays green.

The owner's instruction this round was **always correct transcription errors immediately**, and the first real
one was structural rather than textual: **the file-control entry had Format 3 shredded into SEVEN separate
blocks**, one per clause, each with its own figure note (`ASSIGN clause, SELECT Format 3`, `LOCK MODE clause,
SELECT Format 3`, …). One general format transcribed as seven figures. Format 4 likewise. Rather than hand-repair
that clause and the eleven others like it, the sweep now REASSEMBLES fragments — matching on words with the
notes excluded, and carrying every note through after the figure it describes. All six of Format 3's notes
survive under the single reassembled figure.

The one textual defect I chased turned out to be MINE again: `VALUE-EDITING` is printed letterspaced with its
hyphen set as a FIGURE DASH, and the transcription already had it right and said so in its own figure notes —
it was the GENERATOR splitting a COBOL word into three. Fixed there.

**Two apply-time bugs, both the same confusion, both caught by verification rather than by review.** After the
first apply, word conservation showed 151 words GAINED — impossible under a gate that requires words to match,
so something was being duplicated. It was: `BLOCKQUOTE` is `^\\s*>`, which matches `>>POP`, so a figure's own
compiler-directive line was collected as a "note" and re-emitted after the figure. Fixed, re-applied, still 97
gained: this time the transcription sets many figures INSIDE a blockquote, so the whole old figure was
collected as notes by prefix. The note collector needed the same fence-state logic the comparator already had.
Third apply: 0 and 0.

I would not have found either by reading the diff — 416 figures is too much to eyeball, which is exactly why
the conservation check exists. It is worth stating as a rule: **an invariant you can compute over the whole
artifact beats any amount of sampling.**

Twelve clauses (59 figures) remain, and they are now a genuine to-do list rather than tool noise: ten whose
printed and transcribed figure counts differ with no grouping that matches, and two word differences
(§8.7.5.1's ASCII-art corner, §13.2.1 carrying `DATA DIVISION` the printed figure does not).

