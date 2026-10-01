*> reject-at: 2002 2014 2023
*> ISO 1989:2023 13.18.2.3 SR1: the PICTURE character-string of an ANY LENGTH entry shall be ONE instance
*> of the picture symbol 'N', 'X', or '1'. 13.18.40.3 SR6: a parenthesized integer may be a constant-name and
*> "indicates the number of consecutive occurrences of the symbol", so PIC X(TWO-CHARS) with TWO-CHARS = 2 is
*> TWO instances of X and violates SR1 (COBOLNET1542). The single-instance test reads the RESOLVED count, so a
*> count of 1 in any spelling is accepted (pb1210_any_length_count_one_spellings) and a count of 2 never is.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1210NEGMAIN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 TWO-CHARS CONSTANT GLOBAL AS 2.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1210NEGSUB.
DATA DIVISION.
LINKAGE SECTION.
01 L PIC X(TWO-CHARS) ANY LENGTH.
PROCEDURE DIVISION USING L.
M.
    EXIT PROGRAM.
END PROGRAM PB1210NEGSUB.
END PROGRAM PB1210NEGMAIN.
