      *> reject-at: 2002 2014 2023
      *> kb/Work PB1220. ISO/IEC 1989:2023 13.18.14.4 GR6 c): "If RIGHT is specified, integer-1 is the
      *> rightmost column of the printable item. The leftmost column of the printable item is integer-1 -
      *> printable-size + 1." (cite.py OK). COLUMN RIGHT 2 over PIC XXX puts the leftmost column at
      *> 2 - 3 + 1 = 0, before the first column of a line (COBOLNET2712; the standard states no outcome,
      *> docs/CONFORMANCE.md section 3 "COLUMN alignment").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1220NL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1220-left.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       REPORT SECTION.
       RD R-1.
       01 DET-A TYPE DE.
          02 LINE PLUS 1.
             03 COLUMN RIGHT 2 PIC XXX VALUE "RRR".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
