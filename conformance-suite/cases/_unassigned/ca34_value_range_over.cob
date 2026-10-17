*>--- conformance-case
*> id: ca34_value_range_over
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> CA34 (CONFORMANCE-FIX-QUEUE): a numeric VALUE literal must be a permissible value in the range the PICTURE
*> indicates, representable WITHOUT truncation of leading or trailing nonzero digits (ISO 13.18.63.3 SR2). VALUE
*> 12345 needs 5 integer digit positions but PIC 99 has 2 -> the leading 1,2,3 would be truncated -> rejected
*> (COBOLNET1625). Pre-fix it silently seeded 12345 into the native long. Edition-invariant (85/2002/2014/2023).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGCA34A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC 99 VALUE 12345.
PROCEDURE DIVISION.
    DISPLAY A.
    STOP RUN.
