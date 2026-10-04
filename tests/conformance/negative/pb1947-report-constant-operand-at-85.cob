*> reject-at: 85
*> kb/Work PB1947 - ISO 13.10.3 SR2: a constant-name may stand wherever a format specifies a literal,
*>   cite.py: OK  13.10.3 2)  (Syntax rules)
*> but the constant entry itself is COBOL-2002 (13.10), so below it the program is refused at the entry
*> (COBOLNET0900) and LINE PLUS KL has no constant to name. The positive twin is
*> conformance/2002/pb1947_report_constant_operands.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1947N1.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "PB1947N1.TXT".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS R.
WORKING-STORAGE SECTION.
01  KL CONSTANT AS 2.
REPORT SECTION.
RD  R PAGE LIMIT IS 20 LINES.
01  D1 TYPE DE LINE PLUS KL.
    03  COLUMN 1 PIC X(2) VALUE "AB".
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
