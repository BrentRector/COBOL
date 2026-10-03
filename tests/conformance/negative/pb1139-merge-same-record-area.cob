      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1139 - A USING FILE AND A GIVING FILE OF ONE MERGE IN ONE SAME RECORD AREA CLAUSE ARE REFUSED.
      *>   cite.py --check 14.9.24.3 "The only file-names in a MERGE statement that may be specified in the same SAME
      *>     RECORD AREA clause are those associated with the GIVING phrase." -> OK §14.9.24.3 11)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139J.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139J.tmp".
           SELECT SW2 ASSIGN TO "PB1139Js2.tmp".
           SELECT F1 ASSIGN TO "PB1139J1.dat".
           SELECT F2 ASSIGN TO "PB1139J2.dat".
           SELECT F3 ASSIGN TO "PB1139J3.dat".
       I-O-CONTROL.
           SAME RECORD AREA FOR F1 F3.
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
