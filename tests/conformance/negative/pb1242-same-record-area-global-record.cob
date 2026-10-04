      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1242 - F1's record description entry carries GLOBAL.
      *>   cite.py --check 13.18.27.3 "If the SAME RECORD AREA clause is specified for several files, the record description" -> OK
      *> COBOLNET2738 (same-clause-rule). The positive twin is conformance/2002/pb1087_same_clauses_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1242N13.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB1242N131.dat".
           SELECT F2 ASSIGN TO "PB1242N132.dat".
           SELECT F3 ASSIGN TO "PB1242N133.dat".
           SELECT S1 ASSIGN TO "PB1242N13s.dat".
           SELECT R1 ASSIGN TO "PB1242N13r.dat".
       I-O-CONTROL.
           SAME RECORD AREA FOR F1 F2.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(6) GLOBAL.
       FD F2.
       01 F2-REC PIC X(6).
       FD F3.
       01 F3-REC PIC X(6).
       SD S1.
       01 S1-REC PIC X(6).
       FD R1 REPORT IS RPT.
       REPORT SECTION.
       RD RPT.
       01 TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY "RAN".
           STOP RUN.
