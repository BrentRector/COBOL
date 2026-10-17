*>--- conformance-case
*> id: value-boolean-mismatch
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.63 SR10: the VALUE of a boolean item shall be a boolean literal (B"...") or ZERO -
*> a quoted alphanumeric literal does not conform.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB06.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-B PIC 1(4) VALUE "0101".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
