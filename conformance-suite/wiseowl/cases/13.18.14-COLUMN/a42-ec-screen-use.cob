*>--- conformance-case
*> id: a42-ec-screen-use
*> rules: [GR-13.18.14.4-18, GR-13.18.14.4-19, GR-13.18.35.4-14, GR-14.9.11.4-13, GR-14.9.11.4-19]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.49 - the EC-SCREEN family in a USE statement declarative (item 10).
      *> This line first read "14.6.4", which is Item identification; cite.py --check caught it.
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42ECU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-SCREEN-STARTING-COLUMN.
       D-P.
           CONTINUE.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           STOP RUN.
