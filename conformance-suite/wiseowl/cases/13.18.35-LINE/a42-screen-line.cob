*>--- conformance-case
*> id: a42-screen-line
*> rules: [SR-13.18.35.3-11, SR-13.18.35.3-12, SR-13.18.35.3-13,
*>    GR-13.18.35.4-10, GR-13.18.35.4-11, GR-13.18.35.4-12,
*>    GR-13.18.35.4-13, GR-13.18.35.4-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.35 - the LINE clause format 2
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42LINE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 LINE NUMBER IS 4.
       PROCEDURE DIVISION.
           STOP RUN.
