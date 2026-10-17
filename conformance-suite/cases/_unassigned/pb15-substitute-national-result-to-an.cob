*>--- conformance-case
*> id: pb15-substitute-national-result-to-an
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> PB15: FUNCTION SUBSTITUTE over a NATIONAL argument-1 returns a NATIONAL result (ISO 15.87.1 result-type
*> table), so moving that result to an alphanumeric receiver is invalid (14.9.25.3 SR10, Table 16).
*> SUBSTITUTE is the SECOND bespoke bind path (the [ANYCASE][FIRST|LAST] argument-2/argument-3 pairs) that
*> built its own node with a hardcoded alphanumeric category - the same arm TRIM had. Both are fixed by the
*> ONE IntrinsicResultType.Resolve call, not by two more special cases.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGP15S.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N1 PIC N(6) VALUE N"ABCDEF".
01 W-X  PIC X(6).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION SUBSTITUTE(W-N1 N"A" N"Z") TO W-X.
    STOP RUN.
