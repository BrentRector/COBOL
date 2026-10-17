*>--- conformance-case
*> id: a42-screen-foreground-color
*> rules: [SR-13.18.23.3-1, SR-13.18.23.3-2, GR-13.18.23.4-1, GR-13.18.23.4-2, GR-13.18.23.4-3, GR-13.18.23.4-4, FMT-13.18.23.2, DOC-A.1-27, DOC-A.1-83]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.23 - the FOREGROUND-COLOR clause (item 13)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42FGC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 FOREGROUND-COLOR IS 3.
       PROCEDURE DIVISION.
           STOP RUN.
