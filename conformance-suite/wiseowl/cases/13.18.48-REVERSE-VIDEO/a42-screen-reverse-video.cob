*>--- conformance-case
*> id: a42-screen-reverse-video
*> rules: [GR-13.18.48.3-1, GR-13.18.48.3-2, FMT-13.18.48.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.48 - the REVERSE-VIDEO clause (item 19)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42REV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 REVERSE-VIDEO.
       PROCEDURE DIVISION.
           STOP RUN.
