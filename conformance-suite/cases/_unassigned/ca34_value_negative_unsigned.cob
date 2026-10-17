*>--- conformance-case
*> id: ca34_value_negative_unsigned
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> CA34 (CONFORMANCE-FIX-QUEUE): a signed (negative) numeric literal in a VALUE clause requires a signed numeric
*> or sign-bearing numeric-edited subject (ISO 13.18.63.3 SR3). PIC 99 is UNSIGNED (no S), so VALUE -5 is rejected
*> (COBOLNET1625). Pre-fix it silently seeded -5 into an unsigned native field. Edition-invariant (85/2002/2014/2023).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGCA34B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 U PIC 99 VALUE -5.
PROCEDURE DIVISION.
    DISPLAY U.
    STOP RUN.
