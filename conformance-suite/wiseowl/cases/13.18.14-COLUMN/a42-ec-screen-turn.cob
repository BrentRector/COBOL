*>--- conformance-case
*> id: a42-ec-screen-turn
*> rules: [GR-13.18.14.4-18, GR-13.18.14.4-19, GR-13.18.35.4-14, GR-14.9.11.4-13, GR-14.9.11.4-19]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 7.3.25 - the EC-SCREEN family in the TURN compiler directive (item 10);
      *> 14.6.13.1.1 is the exception-name hierarchy the directive selects from.
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       >>TURN EC-SCREEN-FIELD-OVERLAP CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42ECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       PROCEDURE DIVISION.
           STOP RUN.
