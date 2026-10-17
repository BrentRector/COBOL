*>--- conformance-case
*> id: a42-screen-secure
*> rules: [GR-13.18.50.3-1, GR-13.18.50.3-2, GR-13.18.50.3-3,
*>    GR-13.18.50.3-4, FMT-13.18.50.2, DOC-A.1-172]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.50 - the SECURE clause (item 23)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42SECU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 SECURE.
       PROCEDURE DIVISION.
           STOP RUN.
