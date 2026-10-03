      *> reject-at: 2023
      *> kb/Work PB1139 - A MERGE IN A FILE SORT INPUT PROCEDURE IS A COBOL-2023 REMOVAL.
      *>   cite.py --check 14.9.24.3 "A MERGE statement may appear anywhere in the procedure division except in
      *>     imperative-statement-1 of an exception-checking PERFORM statement, or in an output procedure of another
      *>     MERGE statement, or an input or output procedure of a file format SORT statement, or in a declarative
      *>     procedure." -> OK §14.9.24.3 1)
      *> Annex E.2 item 20: the prior standard allowed it, 2023 prohibits it (COBOLNET1572). The 2014 acceptance is
      *> pinned by 2014/pb1139_merge_in_sort_procedure_allowed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139K.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139K.tmp".
           SELECT SW2 ASSIGN TO "PB1139Ks2.tmp".
           SELECT F1 ASSIGN TO "PB1139K1.dat".
           SELECT F2 ASSIGN TO "PB1139K2.dat".
           SELECT F3 ASSIGN TO "PB1139K3.dat".
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
