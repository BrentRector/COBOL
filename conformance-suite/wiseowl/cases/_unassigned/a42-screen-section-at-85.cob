*>--- conformance-case
*> id: a42-screen-section-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.9 - the SCREEN SECTION at --std 85, where SCREEN is not even a reserved word (8.9 reserves it from 2002): the module does not exist in COBOL-85, so the refusal is the same one and not a silent accept
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42S85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 PIC X FROM WS-X.
       PROCEDURE DIVISION.
           STOP RUN.
