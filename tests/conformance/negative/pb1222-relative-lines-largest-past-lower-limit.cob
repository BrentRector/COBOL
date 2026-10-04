      *> reject-at: 2002 2014 2023
      *> kb/Work PB1222 - ISO 13.18.35.3 SR6 d): the relative lines at the end of a group that holds an unconditional absolute
      *> line "shall not cause the report group's lower limit to be exceeded unless each of them is subject to a different PRESENT
      *> WHEN clause, in which case this rule applies only to the vertically largest of them."
      *> cite.py: OK  13.18.35.3 6) d)  (Syntax rules)
      *> LAST DETAIL is 10 (13.18.57.4 GR8 e). The tail lines carry different clauses, so only the largest is judged: LINE PLUS 2
      *> after LINE 9, alone, is line 11, past the lower limit.
      *> (PRESENT WHEN is a COBOL 2002 clause, so the program is refused for this rule at 2002 and later.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1222RELATIVELINESL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1222RELATIVELINESL.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES HEADING 2
           FIRST DETAIL 5 LAST DETAIL 10 FOOTING 12.
       01  D1 TYPE DE.
           03  LINE 9.
               05  COLUMN 1 PIC X VALUE "A".
           03  LINE PLUS 1 PRESENT WHEN WS-ON = 1.
               05  COLUMN 1 PIC X VALUE "B".
           03  LINE PLUS 2 PRESENT WHEN WS-ON = 2.
               05  COLUMN 1 PIC X VALUE "C".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
