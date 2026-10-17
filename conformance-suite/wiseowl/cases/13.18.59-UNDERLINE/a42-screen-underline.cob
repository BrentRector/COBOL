*>--- conformance-case
*> id: a42-screen-underline
*> rules: [GR-13.18.59.3-1, GR-13.18.59.3-2, FMT-13.18.59.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.59 - the UNDERLINE clause (item 27)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42UND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 UNDERLINE.
       PROCEDURE DIVISION.
           STOP RUN.
