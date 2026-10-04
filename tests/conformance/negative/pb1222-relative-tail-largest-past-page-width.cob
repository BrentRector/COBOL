      *> reject-at: 2002 2014 2023
      *> kb/Work PB1222 - ISO 13.18.14.3 SR8 c): relative items at the end of a line "shall not cause the page width to be
      *> exceeded unless each of them is subject to a different PRESENT WHEN clause, in which case this rule applies only to the
      *> largest of them."   cite.py: OK  13.18.14.3 8) c)  (Syntax rules)
      *> The two items carry different clauses, so only the largest is judged: COLUMN PLUS 3 over twelve columns, alone, starts at
      *> column 3 and ends at 14, past the page width 12.
      *> (PRESENT WHEN is a COBOL 2002 clause, so the program is refused for this rule at 2002 and later.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1222RELATIVETAILLA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1222RELATIVETAILLA.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES 12 COLUMNS.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN PLUS 1 PIC X(6) VALUE "ABCDEF"
               PRESENT WHEN WS-ON = 1.
           03  COLUMN PLUS 3 PIC X(12) VALUE "VWXYZVWXYZVW"
               PRESENT WHEN WS-ON = 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
