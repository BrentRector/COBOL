      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1139 - A FILE SORT IN THE INPUT PROCEDURE OF ANOTHER FILE SORT IS REFUSED.
      *>   cite.py --check 14.9.40.3 "A SORT statement shall not appear in imperative-statement-1 of an
      *>     exception-checking PERFORM statement, in an input or output procedure, or in a declarative procedure."
      *>     -> OK §14.9.40.3 3)
      *> No check asked the procedure ranges about a SORT (only MERGE and COMMIT/ROLLBACK, and only from 2023), so the
      *> statement compiled clean; SR3 holds at every edition ("SORT already disallowed it", Annex E.2 item 20).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139C.tmp".
           SELECT SW2 ASSIGN TO "PB1139Cs2.tmp".
           SELECT F1 ASSIGN TO "PB1139C1.dat".
           SELECT F2 ASSIGN TO "PB1139C2.dat".
           SELECT F3 ASSIGN TO "PB1139C3.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC X(4).
       SD SW2.
       01 SR2.
          05 SK2 PIC X(4).
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       FD F3.
       01 R3 PIC X(4).
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           SORT SW ASCENDING KEY SK
               INPUT PROCEDURE IS LOADIT
               GIVING F3
           STOP RUN.
       LOADIT SECTION.
       L1.
           SORT SW2 ASCENDING KEY SK2 USING F1 GIVING F2.
