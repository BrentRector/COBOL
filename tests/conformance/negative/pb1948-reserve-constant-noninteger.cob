*> reject-at: 2002 2014 2023
*> kb/Work PB1948 - ISO 13.10.3 SR2: "If constant-name-1 is an integer, it may also be used to specify
*>   repetition in a picture character-string" and, in its first sentence, a constant-name stands where a
*>   format specifies a literal OF THE CLASS AND CATEGORY of constant-name-1: RESERVE integer-1 AREAS
*>   (12.4.5.14) specifies an integer, so an alphanumeric constant ("ABC") is not the literal it names.
*>   cite.py: OK  13.10.3 2)  (Syntax rules)
*>   cite.py: OK  12.4.5.14  (RESERVE clause)
*> COBOLNET1547 - the constant shall be an INTEGER constant-name. The positive twin is
*> conformance/2002/pb1948_integer_n_constants.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1948N2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F1 ASSIGN TO "PB1948N2.TXT" RESERVE KS AREAS.
DATA DIVISION.
FILE SECTION.
FD  F1.
01  F1R PIC X(5).
WORKING-STORAGE SECTION.
01  KS CONSTANT AS "ABC".
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
