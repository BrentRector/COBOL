*>--- conformance-case
*> id: pb732-value-88-thru-word
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 — arm: Format 3 (condition-name) VALUE with a THROUGH range. literal-3 (the HIGH
*> operand) travels a separate code path (valueClauseRange) from the singleton, so it gets its own
*> witness. ISO 13.18.63.2 Format 3 writes `literal-2 [{THROUGH|THRU} literal-3]` and 13.18.63.3 SR26 a)
*> reads both as values ("the value of literal-2 shall be less than the value of literal-3"), so an
*> undefined word at either end is COBOLNET1639 (8.4.2.1). Pre-fix this compiled clean and emitted a
*> comparison against the string "NOSUCHW".
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB732D.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X VALUE "B".
   88 A-IN VALUE "A" THRU NOSUCHW.
PROCEDURE DIVISION.
    IF A-IN DISPLAY "Y" ELSE DISPLAY "N" END-IF.
    STOP RUN.
