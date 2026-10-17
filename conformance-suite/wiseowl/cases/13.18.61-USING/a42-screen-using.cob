*>--- conformance-case
*> id: a42-screen-using
*> rules: [SR-13.18.61.3-1, SR-13.18.61.3-2, SR-13.18.61.3-3, SR-13.18.61.3-4, GR-13.18.61.4-1, GR-13.18.61.4-2, FMT-13.18.61.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.61 - the USING clause (OMITTED from the printed A.4.2 list - see kb/Work PB260)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42USNG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 USING WS-X.
       PROCEDURE DIVISION.
           STOP RUN.
