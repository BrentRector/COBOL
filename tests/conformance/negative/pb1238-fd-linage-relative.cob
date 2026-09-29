      *> reject-at: 85 2002 2014 2023
      *> A LINAGE CLAUSE ON A RELATIVE FILE'S DESCRIPTION ENTRY (kb/Work PB1238, PB865).
      *> ISO/IEC 1989:2023 §13.4.5.3 SR7: "Format 2 is the file description entry for a
      *> relative file or an indexed file", and the rendered §13.4.5.2 Format 2 prints only
      *> IS EXTERNAL, IS GLOBAL, BLOCK CONTAINS and the record-clause — no linage-clause.
      *> Until PB1238 the clause bound a logical page no relative connector ever drives,
      *> so LINAGE-COUNTER never moved; now COBOLNET2604 refuses it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1238NL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1238nl.dat" ORGANIZATION RELATIVE.
       DATA DIVISION.
       FILE SECTION.
       FD F LINAGE IS 20 LINES.
       01 R PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
