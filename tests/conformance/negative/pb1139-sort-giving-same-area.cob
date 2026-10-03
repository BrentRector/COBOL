      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1139 - TWO GIVING FILES OF ONE SORT IN THE SAME SAME AREA CLAUSE ARE REFUSED.
      *>   cite.py --check 14.9.40.3 "File-names associated with the GIVING phrase shall not be specified in the same
      *>     SAME AREA clause." -> OK §14.9.40.3 10)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139H.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139H.tmp".
           SELECT SW2 ASSIGN TO "PB1139Hs2.tmp".
           SELECT F1 ASSIGN TO "PB1139H1.dat".
           SELECT F2 ASSIGN TO "PB1139H2.dat".
           SELECT F3 ASSIGN TO "PB1139H3.dat".
       I-O-CONTROL.
           SAME AREA FOR F2 F3.
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
           SORT SW ASCENDING KEY SK USING F1 GIVING F2 F3
           STOP RUN.
