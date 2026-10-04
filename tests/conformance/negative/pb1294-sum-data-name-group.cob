*> reject-at: 85 2002 2014 2023
*> kb/Work PB1294 - ISO 13.18.54.3 SR4: data-name-1 "shall be the name of a numeric data item in the report
*>   section". DETG is a report GROUP, not an elementary item, and carries no value of its own to add
*>   (13.18.54.4 GR6 adds a sum counter or the operand of a SOURCE or VALUE clause).
*>   cite.py: OK  13.18.54.3 4)  (Syntax rules)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1294N2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB1294N2.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  WS-A PIC 99 VALUE 0.
REPORT SECTION.
RD  R CONTROL IS FINAL.
01  DETG TYPE DE LINE PLUS 1.
    05  DA COLUMN 1 PIC 99 SOURCE WS-A.
01  CFF TYPE CF FINAL LINE PLUS 1.
    05  TOT COLUMN 1 PIC 999 SUM DETG.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
