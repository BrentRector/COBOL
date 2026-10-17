*>--- conformance-case
*> id: a48-format-clause-no-optional-word
*> rules: [SR-13.4.5.3-6, GR-13.18.13.4-5, SR-13.18.24.3-1,
*>    SR-13.18.24.3-2, SR-13.18.24.3-3, GR-13.18.24.4-1,
*>    GR-13.18.24.4-2, GR-13.18.24.4-3, GR-13.18.24.4-4,
*>    GR-13.18.24.4-5, GR-13.18.24.4-6, GR-13.18.24.4-7,
*>    GR-13.18.24.4-8, GR-13.18.24.4-9, GR-13.18.24.4-10,
*>    GR-13.18.24.4-11, GR-13.18.24.4-12, SR-14.9.27.3-9,
*>    FMT-13.18.24.2, DOC-A.1-84, DOC-A.1-85, DOC-A.1-173]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 Annex A.4.8 item 1). The third FORMAT spelling: `DATA` is NOT underlined in 13.18.24.2's
*> printed general format, so it is an OPTIONAL WORD and `FORMAT NUMERIC` alone is legal source. The pair
*> with a48-format-clause-declined (which writes DATA) is what keeps the recognizer from making the word
*> required - the underlining decides required-vs-optional, and getting it backwards rejects legal COBOL
*> with a parse error instead of the named A.4.8 refusal.
IDENTIFICATION DIVISION.
PROGRAM-ID. A48FMTN9AL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-OUT ASSIGN TO "a48fmtn9al.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-OUT
    RECORD IS VARYING IN SIZE FROM 1 TO 80 CHARACTERS
    FORMAT NUMERIC.
01 F-REC PIC X(80).
WORKING-STORAGE SECTION.
01 WS-REC PIC X(80) VALUE "HELLO".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F-OUT.
    WRITE F-REC FROM WS-REC.
    CLOSE F-OUT.
    STOP RUN.
