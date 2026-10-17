*>--- conformance-case
*> id: a42-screen-column
*> rules: [SR-13.18.14.3-11, SR-13.18.14.3-12, SR-13.18.14.3-13, GR-13.18.14.4-13, GR-13.18.14.4-14, GR-13.18.14.4-15, GR-13.18.14.4-16, GR-13.18.14.4-17, GR-13.18.14.4-18, GR-13.18.14.4-19, DOC-A.1-199]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.14 - the COLUMN clause format 2 (item 7)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42COL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 COLUMN NUMBER IS 5.
       PROCEDURE DIVISION.
           STOP RUN.
