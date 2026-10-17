*>--- conformance-case
*> id: pb586-report-literal-out-of-range
*> rules: [SR-13.18.63.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR2 (ALL FORMATS) reaches format 4, the report section, as it reaches format 1:
*> "all literals in the VALUE clause shall be numeric and shall be permissible values within the range
*> indicated by the PICTURE clause". MEASURED BEFORE: compiled clean and PRINTED 45 - the literal silently
*> truncated to the printable item's two digit positions (kb/Work PB586's sibling sweep: the report arm
*> reached no part of the VALUE literal funnel but SR6's edition gate). (COBOLNET1625)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB586RPTRNG.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "pb586rptrng.txt".
DATA DIVISION.
FILE SECTION.
FD  PRT REPORT IS RP.
REPORT SECTION.
RD  RP PAGE LIMIT IS 10 LINES.
01  DET TYPE DE.
    02  LINE PLUS 1.
        03  COLUMN 1 PIC 9(2) VALUE 12345.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT PRT.
    INITIATE RP.
    GENERATE DET.
    TERMINATE RP.
    CLOSE PRT.
    STOP RUN.
