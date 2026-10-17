*>--- conformance-case
*> id: level88-alnum-value-boolean
*> rules: [SR-13.18.63.3-10, SR-13.18.63.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.63 SR24 -> SR10: a boolean conditional variable takes boolean literals or ZERO - a plain
*> alphanumeric literal does not conform.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB09.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-B PIC 1(2).
   88 W-ON VALUE "01".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
