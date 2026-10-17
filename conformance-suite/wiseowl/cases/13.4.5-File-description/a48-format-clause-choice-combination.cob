*>--- conformance-case
*> id: a48-format-clause-choice-combination
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
*> ISO 1989:2023 Annex A.4.8 item 1); the shape half is 13.18.24.2's general format, RENDERED from the
*> printed standard (PDF p403) rather than read off the OCR. BIT / CHARACTER / NUMERIC sit inside CHOICE
*> INDICATORS - the pair of vertical bars just inside the braces - so 5.2.6.4 makes them ONE OR MORE, each
*> at most once, IN ANY ORDER. `FORMAT BIT CHARACTER NUMERIC DATA` is therefore LEGAL SOURCE, not a syntax
*> error, and this witness exists so the recognizer can never be narrowed back to a one-of-three choice:
*> a falsely-restrictive diagram reading is exactly the transcription defect class rule 1 warns about, and
*> a recognizer that rejected this spelling would refuse it with the WRONG diagnostic (a parse error) while
*> still reading green on the single-alternative witness.
IDENTIFICATION DIVISION.
PROGRAM-ID. A48FMTC9AL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-OUT ASSIGN TO "a48fmtc9al.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-OUT
    RECORD IS VARYING IN SIZE FROM 1 TO 80 CHARACTERS
    FORMAT BIT CHARACTER NUMERIC DATA.
01 F-REC PIC X(80).
WORKING-STORAGE SECTION.
01 WS-REC PIC X(80) VALUE "HELLO".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F-OUT.
    WRITE F-REC FROM WS-REC.
    CLOSE F-OUT.
    STOP RUN.
