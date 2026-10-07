*> reject-at: 2002 2014 2023
*> ISO 1989:2023 13.10.3 SR6 - none of the literals in arithmetic-expression-1 of a constant entry
*> shall be a figurative constant; 7.3.6.2 SR1b - every operand shall be a fixed-point numeric literal
*> (kb/Work PB1229; COBOLNET1547). The figurative ZERO next to an arithmetic operator, as ZERO, ZEROS or
*> ZEROES, and inside parentheses, is no numeric literal.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC11.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K1 CONSTANT AS ZERO + 1.
01 K2 CONSTANT AS ZEROES * 3.
01 K3 CONSTANT AS 2 - (ZEROS).
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
