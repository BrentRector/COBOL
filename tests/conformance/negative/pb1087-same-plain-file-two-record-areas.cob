      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1087 - the plain file F1 in TWO record-area format clauses.
      *>   cite.py --check 12.4.6.4.3 "A given file-name that represents a file other than a report file or a sort or merge file may be specified in one file-area format" -> OK
      *> COBOLNET2738 (same-clause-rule). The positive twin is conformance/2002/pb1087_same_clauses_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1087N8.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB1087N81.dat".
           SELECT F2 ASSIGN TO "PB1087N82.dat".
           SELECT F3 ASSIGN TO "PB1087N83.dat".
           SELECT S1 ASSIGN TO "PB1087N8s.dat".
           SELECT R1 ASSIGN TO "PB1087N8r.dat".
       I-O-CONTROL.
           SAME RECORD AREA FOR F1 F2.
           SAME RECORD AREA FOR F1 F3.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(6).
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
