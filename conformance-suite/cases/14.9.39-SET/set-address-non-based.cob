*>--- conformance-case
*> id: set-address-non-based
*> rules: [SR-14.9.39.3-18, GR-14.9.39.4-13]
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPT03.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC X(5).
01 P USAGE POINTER.
PROCEDURE DIVISION.
MAIN.
    SET ADDRESS OF W TO P.
    STOP RUN.
