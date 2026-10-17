*>--- conformance-case
*> id: a42-screen-from
*> rules: [SR-13.18.25.3-1, SR-13.18.25.3-2, SR-13.18.25.3-3,
*>    SR-13.18.25.3-4, SR-13.18.25.3-5, GR-13.18.25.4-1, FMT-13.18.25.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.25 - the FROM clause (item 14)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42FROM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 FROM WS-X.
       PROCEDURE DIVISION.
           STOP RUN.
