*>--- conformance-case
*> id: level88-boolean-value-alnum
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.63 SR4: an alphanumeric conditional variable takes alphanumeric literals - a boolean
*> literal (B"...") does not conform.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB08.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-A PIC X(2).
   88 W-ON VALUE B"01".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
