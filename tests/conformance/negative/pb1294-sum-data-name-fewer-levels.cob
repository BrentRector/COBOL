*> reject-at: 2002 2014 2023
*> kb/Work PB1294 - ISO 13.18.54.3 SR4 c): in a DIFFERENT report group description data-name-1 "either shall not
*>   reference a repeating item or shall reference a repeating item that is subject to at least the same number of
*>   levels of repetition as the subject of the entry." CELL has one level (OCCURS 3) and COLT two (the enclosing
*>   OCCURS 2 and its own OCCURS 3).
*>   cite.py: OK  13.18.54.3 4) c)  (Syntax rules)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1294N7.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB1294N7.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  WS-A PIC 99 VALUE 0.
01  WS-B PIC 99 VALUE 0.
01  WS-C PIC 99 VALUE 0.
REPORT SECTION.
RD  R CONTROL IS FINAL.
01  DET TYPE DE LINE PLUS 1.
    05  CELL COLUMN 1 PIC 99 OCCURS 3 TIMES STEP 3 SOURCE WS-A WS-B WS-C.
01  CFF TYPE CF FINAL.
    03  LINE PLUS 1 OCCURS 2 TIMES STEP 1.
        05  COLT COLUMN 1 PIC 999 OCCURS 3 TIMES STEP 4 SUM CELL.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
