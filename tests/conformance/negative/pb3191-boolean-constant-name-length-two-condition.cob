*> reject-at: 2002 2014 2023
*> A boolean constant-name is a boolean literal (13.10.4 GR1) and so a simple boolean condition (8.8.4.3), whose
*> syntax rule 1 allows only boolean items and literals of length 1 (8.8.4.3.3 SR1). B"10" is two positions long, so
*> `IF BK2` is refused by that rule (COBOLNET1511), as `IF B"10"` is (kb/Work PB3191).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG3191C.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 BK2 CONSTANT AS B"10".
PROCEDURE DIVISION.
MAIN.
    IF BK2 DISPLAY "K" END-IF.
    STOP RUN.
