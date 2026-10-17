*>--- conformance-case
*> id: pb515-value-on-index-item
*> rules: [SR-13.16.3-10]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 SR10 - "The VALUE clause shall not be specified for data items of class index,
*> message-tag, object, or pointer" - and 8.5.2.1 Table 2 files category index (USAGE INDEX) under class
*> index. 13.18.63.3 SR9 names four usages and not INDEX, and the screen was written from SR9 alone, so this
*> compiled clean at every edition, and the note measured the seeded value live at its pin (kb/Work PB515).
*> The inherited spelling (01 G USAGE INDEX. 05 A VALUE 1.) is the same class by 13.18.60.4 GR1 and draws the
*> same diagnostic - ValueClauseScreenTests pins it. (COBOLNET2168)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB515IDXVAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
77 I USAGE INDEX VALUE 7.
PROCEDURE DIVISION.
MAIN.
    DISPLAY "OK"
    STOP RUN.
