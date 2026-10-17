*>--- conformance-case
*> id: pb1246-fd-first-entry-not-level-1
*> rules: [GR-13.18.33.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1246 - ISO 13.11.1 / 13.18.33.4 GR1 (cite.py OK on both): the first entry of a record description shall have level-number 1.
      *> The FD arm: the record area's first entry is level 05.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1246FDFIRSTNOTONE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "W13PPB1246FDFIRSTNOTONE.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       05  R PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
