*>--- conformance-case
*> id: pb732-value-data-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 — arm: a DEFINED data-name written in a VALUE clause. No general format of the VALUE
*> clause admits an identifier (ISO 13.18.63.2, every operand position is literal-n; verified against the
*> printed figures), so a data-name there is as invalid as a typo, and the diagnostic says which it is.
*> Pre-fix the item silently initialized to the string "DNAME" - the data item's SPELLING, not its value.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB732H.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 DNAME PIC X(5) VALUE "HELLO".
01 A PIC X(5) VALUE DNAME.
PROCEDURE DIVISION.
    DISPLAY A.
    STOP RUN.
