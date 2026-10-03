      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1139 - A FILE SORT IN A DECLARATIVE PROCEDURE IS REFUSED.
      *>   cite.py --check 14.9.40.3 "A SORT statement shall not appear in imperative-statement-1 of an
      *>     exception-checking PERFORM statement, in an input or output procedure, or in a declarative procedure."
      *>     -> OK §14.9.40.3 3)
      *> No binder asked whether the bind position was inside a declarative, so the statement compiled clean at
      *> every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139A.tmp".
           SELECT SW2 ASSIGN TO "PB1139As2.tmp".
           SELECT F1 ASSIGN TO "PB1139A1.dat".
           SELECT F2 ASSIGN TO "PB1139A2.dat".
           SELECT F3 ASSIGN TO "PB1139A3.dat".
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
           SORT SW ASCENDING KEY SK USING F1 GIVING F2.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           STOP RUN.
