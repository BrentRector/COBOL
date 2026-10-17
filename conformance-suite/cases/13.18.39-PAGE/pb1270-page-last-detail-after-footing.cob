*>--- conformance-case
*> id: pb1270-page-last-detail-after-footing
*> rules: [SR-13.18.39.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1270 - ISO 13.18.39.3 SR6: the integers "shall be in ascending order, with equality allowed", and the
      *> order is HEADING, FIRST DETAIL, LAST CONTROL HEADING, LAST DETAIL, FOOTING, then the page limit (integer-3 through
      *> integer-7, then integer-1).   cite.py: OK  13.18.39.3 6)  (Syntax rules)
      *> LAST DETAIL 9 is greater than FOOTING 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1270PAGELASTDETAIL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1270PAGELASTDETAIL.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES LAST DETAIL 9 FOOTING 7.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
