*>--- conformance-case
*> id: a42-ec-screen-raise
*> rules: [GR-13.18.14.4-18, GR-13.18.14.4-19, GR-13.18.35.4-14, GR-14.9.11.4-13, GR-14.9.11.4-19]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.29 - the EC-SCREEN family in the RAISE statement (item 10)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42ECR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       PROCEDURE DIVISION.
           RAISE EXCEPTION EC-SCREEN-ITEM-TRUNCATED.
           STOP RUN.
