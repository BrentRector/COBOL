      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1087 - a record-area SAME clause naming ONE file; the format prints file-name-1 { file-name-2 } ... so a second file is required.
      *>   cite.py --check 12.4.6.4.4 "A record-area format SAME clause specifies that two or more files referenced by file-name-1, file-name-2" -> OK
      *> COBOLNET2738 (same-clause-rule). The positive twin is conformance/2002/pb1087_same_clauses_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1087N0.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB1087N01.dat".
           SELECT F2 ASSIGN TO "PB1087N02.dat".
           SELECT F3 ASSIGN TO "PB1087N03.dat".
           SELECT S1 ASSIGN TO "PB1087N0s.dat".
           SELECT R1 ASSIGN TO "PB1087N0r.dat".
       I-O-CONTROL.
           SAME RECORD AREA FOR F1.
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
