*>--- conformance-case
*> id: a42-screen-bell
*> rules: [GR-13.18.6.3-1, GR-13.18.6.3-2, FMT-13.18.6.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.6 - the BELL clause (item 4)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42BELL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 BELL.
       PROCEDURE DIVISION.
           STOP RUN.
