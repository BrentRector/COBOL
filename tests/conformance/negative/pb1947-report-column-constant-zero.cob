*> reject-at: 2002 2014 2023
*> kb/Work PB1947 - ISO 5.5 1): an integer-n "shall be unsigned and nonzero unless otherwise specified in
*>   the associated rules", and 13.10.4 GR1 makes COLUMN KZ "as if" COLUMN 0 were written.
*>   cite.py: OK  5.5 1)  (Integer operands)
*>   cite.py: OK  13.10.4 1)  (General rules)
*> No rule of the COLUMN clause permits zero (13.18.14.3), so a zero constant is COBOLNET2386 exactly as the
*> literal is. (A LINE PLUS operand, by contrast, "may be zero": 13.18.35.3 SR3.)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1947N2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB1947N2.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  KZ CONSTANT AS 0.
REPORT SECTION.
RD  R PAGE LIMIT IS 20 LINES.
01  D1 TYPE DE LINE PLUS 1.
    03  COLUMN KZ PIC X(2) VALUE "AB".
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
