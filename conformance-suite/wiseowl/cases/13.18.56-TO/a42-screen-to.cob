*>--- conformance-case
*> id: a42-screen-to
*> rules: [SR-13.18.56.3-1, SR-13.18.56.3-2, SR-13.18.56.3-3,
*>    GR-13.18.56.4-1, GR-13.18.56.4-2, FMT-13.18.56.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.56 - the TO clause (item 26)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42TO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 TO WS-X.
       PROCEDURE DIVISION.
           STOP RUN.
