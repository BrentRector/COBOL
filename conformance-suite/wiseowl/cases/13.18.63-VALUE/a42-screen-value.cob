*>--- conformance-case
*> id: a42-screen-value
*> rules: [SR-13.18.63.3-15, GR-13.18.63.4-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63 - the VALUE clause of a screen description entry
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42VAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 VALUE "HI".
       PROCEDURE DIVISION.
           STOP RUN.
