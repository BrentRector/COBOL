*>--- conformance-case
*> id: pb1-numeric-arg-family
*> rules: [AR-15.3-10, AR-15.9.3-1, AR-15.74.3-1, AR-15.75.3-1,
*>    AR-15.76.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 15.74.3 r1 (PRESENT-VALUE), 15.75.3 r1 (RANDOM) and 15.76.3 r1 (RANGE) each require class NUMERIC.
*> All three are screened from IntrinsicArgumentRules.Verified but had no fixture of their own, so the rows
*> stood as CONFORMS-but-untested. One fixture covers the family: an alphanumeric operand in each.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB1NUMFAMILY.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(3) VALUE "100".
01 R PIC S9(6)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION PRESENT-VALUE(A 100).
    COMPUTE R = FUNCTION RANDOM(A).
    COMPUTE R = FUNCTION RANGE(A 5).
    STOP RUN.
