*> reject-at: 85 2002 2014 2023
*> ISO 8.8.1.1 names numeric data items, numeric literals and the figurative constant ZERO as the operands of an
*> arithmetic expression; a symbolic-character "defines a figurative constant" (12.3.7.4 GR11 a)) other than ZERO, the
*> only one 8.3.3.6.3 SR1 a) admits where a literal is restricted to numeric. COBOLNET0844, as for a non-numeric
*> constant-name (kb/Work PB1544: it was "not defined").
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG1544D.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS SA IS 66.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC 9(4) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    COMPUTE N = SA + 1.
    STOP RUN.
