*>--- conformance-case
*> id: pb732-value-88-word
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 — arm: Format 3 (condition-name) VALUE, SINGLETON operand. ISO 13.18.63.2 Format 3
*> writes literal-2, so the level-88 operand is the same literal position as Format 1's literal-1
*> (13.18.63.3 SR27 reads it as "the value of literal-2"). An undefined word there is COBOLNET1639
*> (8.4.2.1). Pre-fix, on an ALPHANUMERIC conditional variable it compiled clean and compared against
*> the seven-character string "NOSUCHW".
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB732C.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X VALUE "B".
   88 A-IS VALUE NOSUCHW.
PROCEDURE DIVISION.
    IF A-IS DISPLAY "Y" ELSE DISPLAY "N" END-IF.
    STOP RUN.
