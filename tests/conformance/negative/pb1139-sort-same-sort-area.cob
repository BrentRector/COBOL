      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1139 - TWO FILE-NAMES OF ONE SORT IN THE SAME SAME SORT AREA CLAUSE ARE REFUSED.
      *>   cite.py --check 14.9.40.3 "No pair of file-names in the same SORT statement may be specified in the same
      *>     SAME SORT AREA or SAME SORT-MERGE AREA clause." -> OK §14.9.40.3 10)
      *> SW (file-name-1) and F1 (a USING file) are one pair of the statement's file-names. The SAME clauses were
      *> parsed and discarded, so nothing could read them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139G.tmp".
           SELECT SW2 ASSIGN TO "PB1139Gs2.tmp".
           SELECT F1 ASSIGN TO "PB1139G1.dat".
           SELECT F2 ASSIGN TO "PB1139G2.dat".
           SELECT F3 ASSIGN TO "PB1139G3.dat".
       I-O-CONTROL.
           SAME SORT AREA FOR SW F1.
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
           SORT SW ASCENDING KEY SK USING F1 GIVING F2
           STOP RUN.
