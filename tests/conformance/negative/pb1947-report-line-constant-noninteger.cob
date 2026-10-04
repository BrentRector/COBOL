*> reject-at: 2002 2014 2023
*> kb/Work PB1947 - ISO 13.10.3 SR2: constant-name-1 may be used "anywhere that a format specifies a literal
*>   of the class and category of constant-name-1", and the LINE clause's integer-2 is an integer literal
*>   position (5.5 1)), so only an INTEGER constant-name may stand there. KA is alphanumeric.
*>   cite.py: OK  13.10.3 2)  (Syntax rules)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1947N3.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB1947N3.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  KA CONSTANT AS "AB".
REPORT SECTION.
RD  R PAGE LIMIT IS 20 LINES.
01  D1 TYPE DE LINE PLUS KA.
    03  COLUMN 1 PIC X(2) VALUE "AB".
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
