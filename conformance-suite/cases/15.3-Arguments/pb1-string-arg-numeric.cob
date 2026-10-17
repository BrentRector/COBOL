*>--- conformance-case
*> id: pb1-string-arg-numeric
*> rules: [AR-15.3-2, AR-15.78.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 15.78.3 rule 1 - "Argument-1 shall be of class alphabetic, alphanumeric, or national". FUNCTION
*> REVERSE over a NUMERIC item was accepted and reversed the digit characters, so REVERSE(1234) gave
*> "4321". The mirror of the ABS case: one dead table (ArgKinds "s"), both directions unscreened (PB1).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB1STRARGNUM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC 9(4) VALUE 1234.
01 S PIC X(10).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION REVERSE(N) TO S.
    STOP RUN.
