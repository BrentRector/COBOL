*>--- conformance-case
*> id: segment-limit
*> rules: []
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGSL01.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
OBJECT-COMPUTER. GENERIC-BOX
    SEGMENT-LIMIT IS 20.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
