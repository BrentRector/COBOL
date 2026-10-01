*> reject-at: 2002 2014 2023
*> 6.4.2: 'The first nonblank character on the continuation line shall be a
*> quotation symbol matching the quotation symbol used in the opening
*> delimiter' - free form (kb/Work PB1359, PB1492).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1359FNQ.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(12) VALUE "AB"-
   CD".
PROCEDURE DIVISION.
DISPLAY X.
    STOP RUN.
