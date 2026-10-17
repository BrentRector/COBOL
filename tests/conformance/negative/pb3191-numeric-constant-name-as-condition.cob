*> reject-at: 2002 2014 2023
*> A constant-name is the literal it substitutes (13.10.4 GR1; 13.10.3 SR1: a single numeric literal operand is a
*> literal), so `IF K` is `IF 0`: a numeric literal is none of the simple conditions of 8.8.4.2.1. The refusal names
*> the constant-name as that literal (COBOLNET2318) - it used to say COBOLNET1639 "'K' is not defined" (kb/Work PB3191).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG3191A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K CONSTANT AS 0.
PROCEDURE DIVISION.
MAIN.
    IF K DISPLAY "K" END-IF.
    STOP RUN.
