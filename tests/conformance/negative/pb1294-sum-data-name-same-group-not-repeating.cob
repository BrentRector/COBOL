*> reject-at: 85 2002 2014 2023
*> kb/Work PB1294 - ISO 13.18.54.3 SR4 b): "If data-name-1 is specified in the same report group description as the
*>   subject of the entry, data-name-1 shall be a repeating item ... and subject to at least one more level of
*>   repetition than the subject of the entry." DA is not a repeating item, and TOT is in the same report group.
*>   cite.py: OK  13.18.54.3 4) b)  (Syntax rules)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1294N5.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB1294N5.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  WS-A PIC 99 VALUE 0.
REPORT SECTION.
RD  R.
01  DET TYPE DE LINE PLUS 1.
    05  DA COLUMN 1 PIC 99 SOURCE WS-A.
    05  TOT COLUMN 5 PIC 999 SUM DA.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
