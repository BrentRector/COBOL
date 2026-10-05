      *> reject-at: 2002 2014 2023
      *> ISO §12.4.5.9.2 general format — ON is a REQUIRED word of the LOCK MODE clause (kb/Work PB755):
      *>   LOCK MODE IS { MANUAL | AUTOMATIC } [ [WITH] LOCK ON [MULTIPLE] { RECORD | RECORDS } ]
      *>   cite.py --check 12.4.5.9.2 "LOCK ON" -> OK §12.4.5.9.2 (figure notes: `LOCK` (both occurrences),
      *>     `MANUAL`, `AUTOMATIC`, `ON`, `MULTIPLE`, `RECORD`, and `RECORDS` are underlined)
      *>   cite.py --check 5.2.2 "They are shown in uppercase and underlined in general formats" -> OK §5.2.2
      *> Measured on printed page 355 / folio 325: ON's box 311.09-325.35 carries a rule at 312.52-324.43
      *> (83.5% cover). Only the outer bracket makes the whole phrase omittable and only the inner one
      *> makes MULTIPLE omittable; neither touches ON, so `WITH LOCK MULTIPLE RECORDS` is a syntax error at
      *> every edition that has the clause (below 2002 the clause itself draws COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB755N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb755n.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F-K
               LOCK MODE IS AUTOMATIC WITH LOCK MULTIPLE RECORDS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-R.
          05 F-K PIC X(4).
          05 F-D PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "OK".
           STOP RUN.
