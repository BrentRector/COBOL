*> reject-at: 85 2002 2014 2023
*> kb/Work PB1294 - ISO 13.18.54.3 SR4 e): "Any chain of reference shall terminate at an entry that does not contain
*>   a SUM clause referring to a data-name-1 defined in the report section." XA sums YB of report RB, and YB sums XA
*>   of report RA: the chain never terminates. (SR4 g) lifts the group-type restriction across the two reports, so
*>   nothing else is wrong with either clause.)
*>   cite.py: OK  13.18.54.3 4)  (Syntax rules)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1294N8.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRTA ASSIGN TO "PB1294N8A.TXT".
    SELECT PRTB ASSIGN TO "PB1294N8B.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRTA REPORT IS RA.
FD  PRTB REPORT IS RB.
WORKING-STORAGE SECTION.
01  WS-A PIC 99 VALUE 0.
REPORT SECTION.
RD  RA CONTROL IS FINAL.
01  DA TYPE DE LINE PLUS 1.
    05  COLUMN 1 PIC 99 SOURCE WS-A.
01  CFA TYPE CF FINAL LINE PLUS 1.
    05  XA COLUMN 1 PIC 999 SUM YB OF RB.
RD  RB CONTROL IS FINAL.
01  DB TYPE DE LINE PLUS 1.
    05  COLUMN 1 PIC 99 SOURCE WS-A.
01  CFB TYPE CF FINAL LINE PLUS 1.
    05  YB COLUMN 1 PIC 999 SUM XA OF RA.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
