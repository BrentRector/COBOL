*>--- conformance-case
*> id: a42-screen-occurs
*> rules: [SR-13.18.38.3-11, SR-13.18.38.3-12, SR-13.18.38.3-13, SR-13.18.38.3-14, SR-13.18.38.3-15, GR-13.18.38.4-5, GR-13.18.38.4-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.38 - the OCCURS clause of a screen description entry
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42OCC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 OCCURS 3 TIMES.
       PROCEDURE DIVISION.
           STOP RUN.
