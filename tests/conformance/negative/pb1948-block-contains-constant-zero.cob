*> reject-at: 2002 2014 2023
*> kb/Work PB1948 - ISO 5.5 1): an integer-n "shall be unsigned and nonzero unless otherwise specified in
*>   the associated rules", and 13.10.4 GR1 makes BLOCK CONTAINS KZ RECORDS "as if" BLOCK CONTAINS 0 RECORDS
*>   were written (13.10.3 SR2 lets the constant stand there).
*>   cite.py: OK  5.5 1)  (Integer operands)
*>   cite.py: OK  13.10.4 1)  (General rules)
*> No rule of the BLOCK CONTAINS clause (13.18.10.3) permits zero, so a zero constant is COBOLNET2386 exactly
*> as the literal is. The positive twin is conformance/2002/pb1948_integer_n_constants.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1948N1.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F1 ASSIGN TO "PB1948N1.TXT".
DATA DIVISION.
FILE SECTION.
FD  F1 BLOCK CONTAINS KZ RECORDS.
01  F1R PIC X(5).
WORKING-STORAGE SECTION.
01  KZ CONSTANT AS 0.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
