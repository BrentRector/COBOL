*> reject-at: 2002 2014 2023
*> kb/Work PB1948 - ISO 13.18.34.3 SR3: "Integer-2 shall not be greater than integer-1." LINAGE IS KL LINES WITH
*>   FOOTING AT KF names the constants for integer-1 and integer-2 (13.10.3 SR2; 13.10.4 GR1: "as if" the
*>   literals were written), so KF = 7 against KL = 5 violates the rule exactly as LINAGE IS 5 LINES WITH
*>   FOOTING AT 7 does.
*>   cite.py: OK  13.18.34.3 3)  (Syntax rules)
*>   cite.py: OK  13.10.3 2)  (Syntax rules)
*> The positive twin is conformance/2002/pb1948_integer_n_constants.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1948N5.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F1 ASSIGN TO "PB1948N5.TXT".
DATA DIVISION.
FILE SECTION.
FD  F1 LINAGE IS KL LINES WITH FOOTING AT KF.
01  F1R PIC X(5).
WORKING-STORAGE SECTION.
01  KL CONSTANT AS 5.
01  KF CONSTANT AS 7.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
