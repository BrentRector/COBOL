# The COBOL standard, as a searchable transcription

`ISO_COBOL.md` is a transcription of **ISO/IEC 1989:2023(E)** — *Information technology — Programming languages,
their environments and system software interfaces — Programming language COBOL* (third edition, 2023-01).
© ISO/IEC 2023.

It is reproduced under the permission in the standard's own Introduction (page 28), which grants reproduction
"in whole or in part … for any other purpose" and asks that the acknowledgment paragraphs accompany it. **They
are carried in the file's Preface**, which is the first thing in the document.

**The standard is authoritative.** Where this transcription and ISO/IEC 1989:2023 differ, the published standard
governs. ISO sells the original; this is a working transcription, not a substitute for it.

## What is here, and what is not

| | |
|---|---|
| `ISO_COBOL.md` | the transcription — 1,261 pages, every clause, general format and rule |
| the source PDF | **not here.** It is licensed per copy and stays in a private submodule (`specs-private/`) |

Tools that MEASURE the printed page — underlining, bracket and choice-indicator geometry — need the PDF and will
say so if it is absent. Everything that works on the transcription alone runs without it.

## Corrections

`ISO_COBOL.md` is the working copy of the standard for the project that keeps it (kb/Work R66), and **correctness
rules over faithfulness to a typographical error**. It is faithful to the printed standard except where the standard
itself is defective, and there the defect is corrected in place, in the change set that finds it: a character the
typesetting dropped (the reserved-word list on printed page 206 prints `EMD-START` where the rest of the standard says
`END-START`; five sites print `valu62'` for `value '62'`), a placeholder printed without its hyphen, a cross-reference
to a sub-item that does not exist, two rules that contradict each other. A transcription slip (lost indentation, a
wrong label delimiter, a look-alike character) is repaired to the printed form and is not a departure.

**Every departure from the printed text is listed in the Addendum at the end of the document, together with the
printed form and the printed page, so that any correction can be reversed** if it later proves mistaken. Each is also
flagged in place with a `> ⚠ **CORRECTED — see the Addendum (Cn).**` note that quotes the printed form verbatim:
`scripts/spec/cite.py --check` accepts a quotation of either form, and the rule catalog never carries the note. A
correction to a whole class of characters (C14, the look-alike hyphens and minus signs) is flagged once, in the
Preface. Defects that are doubtful rather than clear are transcribed AS PRINTED and listed in the Addendum too.

`scripts/spec/verify_publishable.py` enforces this: it fails if a correction is flagged in the text but missing
from the Addendum, listed in the Addendum but referenced nowhere, flagged without quoting the printed form and naming
the printed page, or listed without its printed page. It runs in every gate and in CI.

## A note on page numbers

Citations in the transcription and its Addendum use the standard's own **printed folios**, so they can be
checked against a copy of ISO/IEC 1989:2023. The internal anchors (`#page-N`) use the PDF's sequential page,
which runs 30 ahead — printed page 206 is PDF page 236 — because the front matter occupies 30 pages. Clause
references (14.9.41.2) are unambiguous either way.

## Provenance

The transcription was produced by OCR of page images and has since been checked against the PDF by measurement
rather than by eye — underlines and delimiters are vector rectangles in the source, so a word's
required-or-optional status follows from geometry, not judgement. Whole-standard sweeps to date: choice
indicators 30/30 correct; underlining 0 defects across 2,215 measured tokens on 694 pages; figure words 1
discrepancy across 15,625 tokens on 820 pages, and that one is the standard's own typo (Addendum C3).
