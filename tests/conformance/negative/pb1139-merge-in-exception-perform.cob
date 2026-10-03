      *> reject-at: 2023
      *> kb/Work PB1139 - A MERGE IN IMPERATIVE-STATEMENT-1 OF AN EXCEPTION-CHECKING PERFORM IS REFUSED.
      *>   cite.py --check 14.9.24.3 "A MERGE statement may appear anywhere in the procedure division except in
      *>     imperative-statement-1 of an exception-checking PERFORM statement, or in an output procedure of another
      *>     MERGE statement, or an input or output procedure of a file format SORT statement, or in a declarative
      *>     procedure." -> OK §14.9.24.3 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139L.tmp".
           SELECT SW2 ASSIGN TO "PB1139Ls2.tmp".
           SELECT F1 ASSIGN TO "PB1139L1.dat".
           SELECT F2 ASSIGN TO "PB1139L2.dat".
           SELECT F3 ASSIGN TO "PB1139L3.dat".
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
           PERFORM
               MERGE SW ASCENDING KEY SK USING F1 F2 GIVING F3
           WHEN EC-ALL
               CONTINUE
           END-PERFORM
           STOP RUN.
