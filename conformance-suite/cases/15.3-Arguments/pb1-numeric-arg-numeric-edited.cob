*>--- conformance-case
*> id: pb1-numeric-arg-numeric-edited
*> rules: [AR-15.3-10, AR-15.7.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.5.2.1 Table 2 - NUMERIC-EDITED sits under class ALPHANUMERIC when its usage is display. So a
*> PIC ZZ9.99 item is NOT of class numeric however numeric it looks, and 15.7.3 rule 1 excludes it from
*> FUNCTION ABS. This is the row of Table 2 most easily read the other way, and reading it the other way
*> would silently admit exactly the operand the rule excludes - hence its own fixture.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB1NUMARGEDITED.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 E PIC ZZ9.99 VALUE 12.34.
01 R PIC S9(6)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION ABS(E).
    STOP RUN.
