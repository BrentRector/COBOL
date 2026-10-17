*>--- conformance-case
*> id: a48-format-clause-declined
*> rules: [SR-13.4.5.3-6, GR-13.18.13.4-5, SR-13.18.24.3-1, SR-13.18.24.3-2, SR-13.18.24.3-3, GR-13.18.24.4-1, GR-13.18.24.4-2, GR-13.18.24.4-3, GR-13.18.24.4-4, GR-13.18.24.4-5, GR-13.18.24.4-6, GR-13.18.24.4-7, GR-13.18.24.4-8, GR-13.18.24.4-9, GR-13.18.24.4-10, GR-13.18.24.4-11, GR-13.18.24.4-12, SR-14.9.27.3-9, FMT-13.18.24.2, DOC-A.1-84, DOC-A.1-85, DOC-A.1-173]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 Annex A.4.8 item 1) "FORMAT clause (13.18.24)" - an OPTIONAL language element (4.2.7) for
*> which this implementation claims NO support (docs/CONFORMANCE.md section 5, row A.4.8). A.4.1: an
*> implementation shall accept the syntax for an optional element ONLY when support for it is claimed - so
*> the clause is RECOGNIZED (13.18.24.2 general format) in order to be REFUSED BY NAME with COBOLNET1705,
*> never accepted inert: 13.18.24.4 GR1 makes the clause change the on-medium representation, so an inert
*> compile would write the WRONG BYTES rather than merely omit a facility.
*> This witness is the minimal legal single-alternative spelling WITH the optional word DATA, on the
*> variable-length record 13.4.5.3 rule 6 requires of an FD carrying FORMAT.
IDENTIFICATION DIVISION.
PROGRAM-ID. A48FMTD9AL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-OUT ASSIGN TO "a48fmtd9al.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-OUT
    RECORD IS VARYING IN SIZE FROM 1 TO 80 CHARACTERS
    FORMAT CHARACTER DATA.
01 F-REC PIC X(80).
WORKING-STORAGE SECTION.
01 WS-REC PIC X(80) VALUE "HELLO".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F-OUT.
    WRITE F-REC FROM WS-REC.
    CLOSE F-OUT.
    STOP RUN.
