*>--- conformance-case
*> id: display-of-wrong-category
*> rules: [AR-15.26.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 15.26.3 rule 1: FUNCTION DISPLAY-OF argument-1 shall be of class NATIONAL — an alphanumeric
*> argument is the inverse direction (FUNCTION NATIONAL-OF, 15.66). COBOLNET1546 (P10 national wave).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGDOF1P10N.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-X PIC X(3) VALUE "ABC".
01 W-R PIC X(3).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION DISPLAY-OF(W-X) TO W-R.
    STOP RUN.
