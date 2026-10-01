*> reject-at: 2002 2014 2023
*> 6.4.4.3: 'An inline comment may be written on any line of a compilation
*> group except on a line that contains a floating literal continuation
*> indicator.' (kb/Work PB1359, free form)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1359CMT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(12) VALUE "AB"- *> not here
   "CD".
PROCEDURE DIVISION.
DISPLAY X.
    STOP RUN.
