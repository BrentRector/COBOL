*>--- conformance-case
*> id: a42-screen-section
*> rules: [GR-9.2.3-1, GR-9.2.3-2, GR-9.2.3-3, GR-9.2.3-4, SR-13.9.3-1,
*>    GR-13.9.4-1, SR-13.17.3-1, SR-13.17.3-2, SR-13.17.3-3,
*>    SR-13.17.3-4, SR-13.17.3-5, SR-13.17.3-6, SR-13.17.3-7,
*>    SR-13.17.3-8, SR-13.17.3-9, SR-13.17.3-10, GR-13.17.4-1,
*>    GR-13.17.4-2, SR-13.18.33.3-6, GR-14.6.2.3.2-L2.4, FMT-13.9.2,
*>    FMT-13.17.2, DOC-A.1-199]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.9 - the SCREEN SECTION header (A.4.2 items 8 and 22)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42SEC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
       PROCEDURE DIVISION.
           STOP RUN.
