*>--- conformance-case
*> id: national-of-wrong-category
*> rules: [AR-15.3-9, AR-15.66.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 15.66.3 rule 1: FUNCTION NATIONAL-OF argument-1 shall be of class alphabetic or alphanumeric —
*> a national argument is the inverse direction (FUNCTION DISPLAY-OF, 15.26). Also exercises the
*> 15.66.3 rule 2 arm indirectly: argument-2 shall be category national. COBOLNET1546 (P10 wave).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNOF1P10N.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC N(3) VALUE N"ABC".
01 W-R PIC N(3).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION NATIONAL-OF(W-N) TO W-R.
    STOP RUN.
