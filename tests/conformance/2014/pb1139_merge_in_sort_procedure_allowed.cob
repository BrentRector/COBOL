      *> kb/Work PB1139 - THE EDITION SPLIT OF MERGE'S SORT/MERGE-PROCEDURE BAN: COBOL-2014 STILL ALLOWS IT (compile-only).
      *>   cite.py --check E.2 "A MERGE statement is now prohibited in an output procedure of another MERGE statement"
      *>     -> the §14.9.24.3 SR1 text is new at 2023 (Annex E.2 item 20: "now prohibited"); the prior editions
      *>     allowed a MERGE there, so the same statement that negative/pb1139-merge-in-sort-procedure refuses at 2023
      *>     compiles at 2014. A FILE SORT in the same place is refused at EVERY edition (its rule, §14.9.40.3 SR3,
      *>     "SORT already disallowed it"): negative/pb1139-sort-in-sort-procedure.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139M14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1139m14.tmp".
           SELECT SW2 ASSIGN TO "pb1139m14s.tmp".
           SELECT F1 ASSIGN TO "pb1139m141.dat".
           SELECT F2 ASSIGN TO "pb1139m142.dat".
           SELECT F3 ASSIGN TO "pb1139m143.dat".
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
           MERGE SW2 ASCENDING KEY SK2 USING F1 F2 GIVING F3.
