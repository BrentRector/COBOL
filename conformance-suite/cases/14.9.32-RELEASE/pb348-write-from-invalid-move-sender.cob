*>--- conformance-case
*> id: pb348-write-from-invalid-move-sender
*> rules: [SR-14.9.32.3-3, SR-14.9.51.3-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.51.3 syntax rule 6: "If record-name-1 is specified, identifier-1 or literal-1
*> shall be valid as a sending operand in a MOVE statement specifying record-name-1 as the receiving
*> operand."  The WRITE twin of the RELEASE case: a PIC 9(3) sender into a PIC A(8) record is
*> refused by 14.9.25.3 SR10 and Table 16 (COBOLNET0819), and 14.9.51.4 GR5 a) makes the FROM phrase
*> exactly that MOVE.
*> It is a SEPARATE fixture from its RELEASE sibling because the two verbs reached the same
*> inspection-free operand binder from different call sites, and the repo's most reproducible defect
*> shape is a dispatch with two arms of which only one is ever fixed.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB348N6.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-OUT ASSIGN TO "pb348n6.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-OUT.
01 F-REC PIC A(8).
WORKING-STORAGE SECTION.
01 WS-NUM PIC 9(3) VALUE 123.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F-OUT
    WRITE F-REC FROM WS-NUM
    CLOSE F-OUT
    STOP RUN.
