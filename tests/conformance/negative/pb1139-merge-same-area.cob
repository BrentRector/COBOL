      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1139 - TWO FILE-NAMES OF ONE MERGE IN THE SAME SAME AREA CLAUSE ARE REFUSED.
      *>   cite.py --check 14.9.24.3 "No pair of file-names in a MERGE statement may be specified in the same SAME
      *>     AREA, SAME SORT AREA, or SAME SORT-MERGE AREA clause." -> OK §14.9.24.3 11)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139I.tmp".
           SELECT SW2 ASSIGN TO "PB1139Is2.tmp".
           SELECT F1 ASSIGN TO "PB1139I1.dat".
           SELECT F2 ASSIGN TO "PB1139I2.dat".
           SELECT F3 ASSIGN TO "PB1139I3.dat".
       I-O-CONTROL.
           SAME AREA FOR F1 F2.
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
           MERGE SW ASCENDING KEY SK USING F1 F2 GIVING F3
           STOP RUN.
