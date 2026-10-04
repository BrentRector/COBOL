      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1295 - ISO 13.18.54.3 SR2: "The category of the subject of the entry shall be valid as the category of a receiving operand in a MOVE
      *> statement for a sending operand of the category numeric."   cite.py: OK  13.18.54.3 2)  (Syntax rules)
      *> 14.9.25.3 SR10, Table 16: a numeric operand does not move to an alphabetic item or to a boolean item.
      *> Here: an unprintable SUM entry (no COLUMN clause), whose counter still takes the entry's PICTURE (13.18.54.4 GR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W12GPB1295SUMENTRYUNPRINTABLEA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W12GPB1295SUMENTRYUNPRINTABLEA.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WK PIC 9 VALUE 1.
       01  TB.
           05  TE PIC 9 OCCURS 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE LINE PLUS 1.
           03  SC PIC A(4) SUM WK.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
