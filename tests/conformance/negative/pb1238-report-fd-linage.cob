      *> reject-at: 85 2002 2014 2023
      *> A LINAGE CLAUSE ON A REPORT FILE'S DESCRIPTION ENTRY (kb/Work PB1238).
      *> ISO/IEC 1989:2023 §13.4.5.3 SR8: "Format 3 is the file description entry for a
      *> report file", and the rendered §13.4.5.2 Format 3 prints IS EXTERNAL, IS GLOBAL,
      *> BLOCK CONTAINS, the record-clause, CODE-SET and REPORT — no linage-clause (a
      *> report's page is described by its RD PAGE clause). Refused, COBOLNET2604.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1238RL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1238rl.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F LINAGE IS 20 LINES REPORT IS RP.
       REPORT SECTION.
       RD RP PAGE LIMIT IS 20 LINES.
       01 TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
