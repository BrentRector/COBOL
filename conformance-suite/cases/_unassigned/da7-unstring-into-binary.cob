*>--- conformance-case
*> id: da7-unstring-into-binary
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.48.3 SR4 - the UNSTRING receiver shall be usage display with category alphabetic/
*> alphanumeric/numeric, or usage national with category national/numeric. DA7: previously a
*> RUN-TIME throw. A GROUP receiver stays legal. Edition-invariant.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGDA7UNSTRINGINTOB.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 C PIC S9(4) COMP VALUE 0.
01 X PIC X(5) VALUE "abcde".
PROCEDURE DIVISION.
MAIN.
    UNSTRING X INTO C.
    STOP RUN.
