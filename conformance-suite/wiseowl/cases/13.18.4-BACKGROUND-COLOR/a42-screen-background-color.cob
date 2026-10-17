*>--- conformance-case
*> id: a42-screen-background-color
*> rules: [SR-13.18.4.3-1, SR-13.18.4.3-2, GR-13.18.4.4-1,
*>    GR-13.18.4.4-2, GR-13.18.4.4-3, GR-13.18.4.4-4, FMT-13.18.4.2,
*>    DOC-A.1-11, DOC-A.1-27]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.4 - the BACKGROUND-COLOR clause (item 3)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42BGC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 BACKGROUND-COLOR IS 2.
       PROCEDURE DIVISION.
           STOP RUN.
