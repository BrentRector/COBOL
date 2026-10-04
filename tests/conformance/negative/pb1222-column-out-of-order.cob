      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1222 - ISO 13.18.14.3 SR7: "Within a given report line, any two or more absolute items defined using column
      *> numbers that are not in increasing numerical order shall be subject to a different PRESENT WHEN clause."
      *> cite.py: OK  13.18.14.3 7)  (Syntax rules)
      *> COLUMN 20 then COLUMN 10, neither subject to a PRESENT WHEN clause. The report printed B at 10 and A at 20.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1222COLUMNOUTOFORD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1222COLUMNOUTOFORD.rpt".
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
           03  COLUMN 20 PIC X VALUE "A".
           03  COLUMN 10 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
