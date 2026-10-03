      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1139 - A MERGE IN A DECLARATIVE PROCEDURE IS REFUSED.
      *>   cite.py --check 14.9.24.3 "A MERGE statement may appear anywhere in the procedure division except in
      *>     imperative-statement-1 of an exception-checking PERFORM statement, or in an output procedure of another
      *>     MERGE statement, or an input or output procedure of a file format SORT statement, or in a declarative
      *>     procedure." -> OK §14.9.24.3 1)
      *> The declarative half of the rule holds at every edition (Annex E.2 item 20 changed only the SORT/MERGE
      *> procedure half, at 2023).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139B.tmp".
           SELECT SW2 ASSIGN TO "PB1139Bs2.tmp".
           SELECT F1 ASSIGN TO "PB1139B1.dat".
           SELECT F2 ASSIGN TO "PB1139B2.dat".
           SELECT F3 ASSIGN TO "PB1139B3.dat".
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
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F3.
       D1P.
           MERGE SW ASCENDING KEY SK USING F1 F2 GIVING F3.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           STOP RUN.
