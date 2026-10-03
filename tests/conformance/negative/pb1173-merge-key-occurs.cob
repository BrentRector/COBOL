      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1173 — A MERGE KEY UNDER OCCURS IS REFUSED BY §14.9.24.3 SR4 b) AND f).
      *>   cite.py --check 14.9.24.3 "Key data names shall not be subject to any OCCURS clauses." ->
      *>     OK §14.9.24.3 4) b)
      *> The MERGE twin of negative/pb1173-sort-file-key-occurs, through the same key-admissibility predicate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173MC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1173mc.tmp".
           SELECT MI1 ASSIGN TO "pb1173mc1.dat".
           SELECT MI2 ASSIGN TO "pb1173mc2.dat".
           SELECT MO1 ASSIGN TO "pb1173mco.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K1 PIC X.
          05 KOCC PIC X OCCURS 2.
       FD MI1.
       01 M1R PIC X(5).
       FD MI2.
       01 M2R PIC X(5).
       FD MO1.
       01 MOR PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           MERGE SW ASCENDING KEY KOCC USING MI1 MI2 GIVING MO1
           STOP RUN.
