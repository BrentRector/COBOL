*> reject-at: 85
*> kb/Work PB1948 - ISO 13.10.3 SR2: a constant-name may stand wherever a format specifies a literal,
*>   cite.py: OK  13.10.3 2)  (Syntax rules)
*> but the constant entry itself is COBOL-2002 (13.10), so below it the program is refused at the entry
*> (COBOLNET0900) and RESERVE KR AREAS has no constant to name. The positive twin is
*> conformance/2002/pb1948_integer_n_constants.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1948N3.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F1 ASSIGN TO "PB1948N3.TXT" RESERVE KR AREAS.
DATA DIVISION.
FILE SECTION.
FD  F1.
01  F1R PIC X(5).
WORKING-STORAGE SECTION.
01  KR CONSTANT AS 2.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
