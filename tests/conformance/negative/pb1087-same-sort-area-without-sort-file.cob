      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1087 - a SAME SORT AREA clause over two plain files.
      *>   cite.py --check 12.4.6.4.3 "At least one file-name specified in a sort-merge-area format SAME clause shall represent a sort or merge file" -> OK
      *> COBOLNET2738 (same-clause-rule). The positive twin is conformance/2002/pb1087_same_clauses_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1087N9.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB1087N91.dat".
           SELECT F2 ASSIGN TO "PB1087N92.dat".
           SELECT F3 ASSIGN TO "PB1087N93.dat".
           SELECT S1 ASSIGN TO "PB1087N9s.dat".
           SELECT R1 ASSIGN TO "PB1087N9r.dat".
       I-O-CONTROL.
           SAME SORT AREA FOR F1 F2.
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
