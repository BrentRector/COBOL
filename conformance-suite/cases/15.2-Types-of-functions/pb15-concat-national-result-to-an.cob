*>--- conformance-case
*> id: pb15-concat-national-result-to-an
*> rules: [GR-15.2-3, RV-15.18.4-2]
*> source-format: free
*> portable: unverified
*>---
*> PB15: FUNCTION CONCAT with a NATIONAL argument-1 returns a NATIONAL result, so moving it to an
*> alphanumeric receiver is invalid (14.9.25.3 SR10, Table 16).
*>
*> CONCAT'S 15.18.1 TABLE IS THE RICHEST IN 15 - it keys on CLASS AND USAGE, because a boolean or numeric
*> argument of usage NATIONAL yields a national function while the same class of usage DISPLAY yields an
*> alphanumeric one. 15.18.4 r2 states the governing rule in prose: "If argument-1 is of class or usage
*> national, the function will return a national value." That prose is also the authority for the last row of
*> the table, whose Function-type cell is EMPTY in the transcription.
*> 15.18.3 r2 makes the argument list uniform in usage, so argument-1 decides for the whole call.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGP15C.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N1 PIC N(4) VALUE N"ABCD".
01 W-N2 PIC N(4) VALUE N"EFGH".
01 W-X  PIC X(8).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION CONCAT(W-N1 W-N2) TO W-X.
    STOP RUN.
