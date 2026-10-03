      *> reject-at: 2002 2014 2023
      *> kb/Work PB1220. ISO/IEC 1989:2023 13.18.14.3 SR9: "If LEFT, CENTER, or RIGHT is specified, all the
      *> operands shall be absolute." (cite.py OK). COLUMN RIGHT PLUS 5 writes the alignment word with a
      *> relative operand, which SR9 forbids (COBOLNET2711).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1220NR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1220-rel.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       REPORT SECTION.
       RD R-1.
       01 DET-A TYPE DE.
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X VALUE "A".
             03 COLUMN RIGHT PLUS 5 PIC XXX VALUE "RRR".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
