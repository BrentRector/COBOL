## PB3147: COBOL-85's areas A and B are enforced at --std 85 — the reference format records where each fixed-form line's text begins, one pre-bind pass asks every token

**Re-probe (w1078f, base c29af1704).** At `--std 85` a paragraph header written in column 12 (`E:/COBOL-coord/scratch/w1078f/probe/p1.cob`)
compiled and ran, and so did a program with every area-A item in area B and statements, a continued PIC clause and a USE
statement in area A (`q1.cob`). The compiler had no reference-format placement check at all; PB2720 had removed the last
line-position parse predicate and left the 1985 half open.

**Rule.** ISO 2023 has no areas — §6.3.1 "The program-text area begins in character position 8 and terminates with the
character position immediately to the left of margin R", §6.3.5 continues an entry "in the program-text area of a
subsequent line" (both `cite.py --check` OK). The X3.23-1985 rule is DERIVED, not quoted (VCR Table 7 row 7.35): an
area-A item (division, section or paragraph header, FD/SD/RD, level-number 01 or 77, DECLARATIVES, END DECLARATIVES, an
end marker) begins its line in area A (positions 8–11); every other line's first text begins in area B; a level-number
other than 01/77 may begin in either. Evidence: GnuCOBOL 3.2 `cobol85.conf` `areacheck: yes`, `cobol2002.conf` `no`,
and its `-std=cobol85` refusals of the probe; every area-A item of the 459 NIST CCVS-85 programs begins in area A (NC113M
uses positions 8–11, OBNC1M's comment-entry is a whole program in area B). ⚠ DETERMINATION: the area-B arm covers every
line's first text and the identification / environment paragraph headers are area-A items — wider than GnuCOBOL's
statement-only check.

**Fix shape.** One fact, recorded where it is born and asked where the grammar is known. `FixedFormLine` is the ONE
definition of area A (`AreaAWidth`, `ProgramTextStart`, `BeginsInAreaA`; the comment-entry reader `FirstAreaAWord` now
uses it and `ReferenceFormatProcessor.AreaAWidth` is deleted). `ReferenceFormatMap.PlacementAt` keeps, per fixed-form
physical line, the physical column where its program text begins and its area (the physical column through the new
`PhysicalLine.ColumnOf`, which `TextSite.Col` now shares). `FixedFormAreas` composes the converged `SourceLineMap` with
every text's map (`Frontend.Areas` → `EditionContext.Areas`), so a token is placed where the user wrote it through COPY,
REPLACE and tabs. `Validation/AreaPlacementPass` runs pre-bind beside `LevelNumberPass` (a front-end error would have
hidden every bind-time edition gate of an 85 negative) and asks every terminal against the heads it collects: rows
`fixed-form-area-a-item-outside-area-a-2002` (COBOLNET3734) and `fixed-form-text-in-area-a-2002` (COBOLNET3735), both
introductions of 2002, so the pass is silent from 2002 on. Design: DESIGN-frontend-grammar.md §3.6.0.

**Goldens and tests.** `2002/pb3147_area_free_placement` (every item where COBOL-85 forbade it; A / 7 / M / B by §14.4.3
order), negatives `pb3147-area-a-item-in-area-b-85` (COBOLNET3734) and `pb3147-statement-in-area-a-85` (COBOLNET3735),
unit `AreaPlacementPassTests` (seven cases: column-12 header, mid-line header, statement in area A, continued clause vs
indented level-number, tab-indented text accepted, a header in library text reported at the library text, every area-A
item in area A accepted). `85/pb2739_indented_division_header_remarks` — a PROCEDURE DIVISION header in column 12 — moved
to `2002/`, the edition where that placement is legal; the NIST corpus and the other 85 goldens compile as before.

**Rows.** None closed: the inventory's §6.3 rows are 2023 rules and already CONFORMS (closes_rows_reason in the note).
Residual: the 1985 "area A of a continuation line" rule is not screened (GnuCOBOL accepts it; unverifiable without the
1985 text), recorded in the note.
