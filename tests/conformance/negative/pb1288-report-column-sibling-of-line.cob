      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1224 + PB1288 - ISO 13.15.3 SR9: "Every elementary entry with a COLUMN clause but no LINE clause shall be
      *> subordinate to an entry with a LINE clause."   cite.py: OK  13.15.3 9)  (Syntax rules)
      *> (13.18.14.3 SR3 is the same rule from the COLUMN clause: "The entry shall also contain a LINE clause or shall be
      *> subordinate to an entry containing a LINE clause.")
      *> The second 03 entry is a SIBLING of the 03 LINE entry, subordinate only to the 01, which has no LINE clause. The binder
      *> asked whether a line was open (the last one opened, never cleared when the walk left its subtree), so the entry compiled
      *> and printed B on A's line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1288REPORTCOLUMNSI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1288REPORTCOLUMNSI.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES.
       01  D1 TYPE DE.
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X VALUE "A".
           03  COLUMN 10 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
