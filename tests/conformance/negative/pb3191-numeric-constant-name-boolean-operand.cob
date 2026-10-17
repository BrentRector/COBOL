*> reject-at: 2002 2014 2023
*> A boolean expression is a boolean item, a boolean literal, ZERO or ALL boolean-literal (8.8.2). A NUMERIC
*> constant-name is the numeric literal it substitutes (13.10.4 GR1), which that list does not contain: COBOLNET1511
*> names it as that literal - it used to be COBOLNET1639 "'N' is not defined" (kb/Work PB3191).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG3191B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N CONSTANT AS 5.
01 B1 PIC 1 USAGE BIT VALUE B"1".
PROCEDURE DIVISION.
MAIN.
    IF B1 B-AND N DISPLAY "N" END-IF.
    STOP RUN.
