      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1222 - ISO 13.18.14.3 SR10 b): "All the occurrences of integer-1 shall be in increasing order of magnitude."
      *> cite.py: OK  13.18.14.3 10) b)  (Syntax rules)
      *> A multiple COLUMN clause (more than one operand, SR10) with the integers 20 and 10. SR10 has no PRESENT WHEN excuse.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1222MULTIPLECOLUMN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1222MULTIPLECOLUMN.rpt".
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
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 20 10 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
