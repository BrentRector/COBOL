*> reject-at: 85 2002 2014 2023
*> ISO 14.9.48.3 SR8 - "the data item referenced by identifier-1 is the sending operand": UNSTRING's identifier-1 is
*> a data item, and a symbolic-character "defines a figurative constant" (12.3.7.4 GR11 a)), a literal. COBOLNET1651,
*> the refusal a constant-name there already draws (kb/Work PB1182), asked of the ONE literal-alias resolution
*> (kb/Work PB1544).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG1544E.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS SA IS 66.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R1 PIC X(4).
PROCEDURE DIVISION.
MAIN.
    UNSTRING SA DELIMITED BY SPACE INTO R1.
    STOP RUN.
