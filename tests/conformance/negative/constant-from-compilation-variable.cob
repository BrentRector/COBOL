*> reject-at: 2002 2014 2023
*> ISO 1989:2023 13.10.3 SR8 - "Compilation-variable-name-1 shall be a compilation-variable-name for which the
*> defined condition is currently true": no >>DEFINE MYVAR precedes the entry, so FROM MYVAR is refused
*> (kb/Work PB1368; this case used to pin the 0899 staging of the whole FROM form).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC09.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K CONSTANT FROM MYVAR.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
