*>--- conformance-case
*> id: a413-write-file-format-1
*> rules: [SR-13.4.5.3-3, SR-14.9.51.3-7, SR-14.9.51.3-8,
*>    SR-14.9.51.3-9, SR-14.9.51.3-10, GR-14.9.51.4-7, GR-14.9.51.4-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 Annex A.4.13 item 2) "WRITE FILE file-name-1 (14.9.51)" - the FILE phrase of the WRITE
*> statement, an OPTIONAL language element (4.2.7) for which this implementation claims NO support
*> (docs/CONFORMANCE.md section 5, row A.4.13). Refused by name with COBOLNET1706.
*> This is 14.9.51.2 FORMAT 1 (sequential organization; 14.9.51.3 rule 2 requires format 1 there).
*> WHAT THIS WITNESS ACTUALLY PINS: until 2026-09-02 this exact program COMPILED CLEAN and RAN, because
*> the binder carried a live file-name arm that wrote the whole record area through FileModel.AreaRecord -
*> undeclared support for an unclaimed module, and not even the standard's own model (14.9.51.4 GR8 derives
*> the implicit record from the DESCRIPTION OF identifier-1, not from the largest record's view). So the
*> `.err` code is not decoration: run this fixture against the pre-fix compiler and the runner's
*> Assert.False(ok) is what reds. The section-5 note that called it "no surface - a parse error today" was
*> false on both arms.
IDENTIFICATION DIVISION.
PROGRAM-ID. A413WF19AL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-OUT ASSIGN TO "a413wf19al.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-OUT.
01 F-REC PIC X(20).
WORKING-STORAGE SECTION.
01 WS-REC PIC X(20) VALUE "HELLO".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F-OUT.
    WRITE FILE F-OUT FROM WS-REC.
    CLOSE F-OUT.
    STOP RUN.
