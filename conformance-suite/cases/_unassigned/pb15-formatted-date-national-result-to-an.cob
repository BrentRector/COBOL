*>--- conformance-case
*> id: pb15-formatted-date-national-result-to-an
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> PB15: FUNCTION FORMATTED-DATE's type follows ARGUMENT-1, and argument-1 is the FORMAT (15.39.1 result-type
*> table; 15.39.3 r1 admits "a national or alphanumeric literal"). So a NATIONAL format literal makes the
*> whole function national even though the value it renders is an integer date - and moving that result to an
*> alphanumeric receiver is invalid (14.9.25.3 SR10, Table 16).
*>
*> THIS IS THE ROW MOST EASILY READ THE WRONG WAY. The instinct is that a date function is "about" its date
*> argument, so its type should follow argument-2; the table says argument-1. All four of the FORMATTED-*
*> family work this way (15.38.1 / 15.39.1 / 15.40.1 / 15.41.1).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGP15F.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-DATE PIC 9(9) VALUE 100000.
01 W-X    PIC X(8).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION FORMATTED-DATE(N"YYYYMMDD" W-DATE) TO W-X.
    STOP RUN.
