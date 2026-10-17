*>--- conformance-case
*> id: pb236-release-record-not-sd
*> rules: [SR-14.9.32.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB236 - ISO 14.9.32.3 SR1: "Record-name-1 shall be the name of a logical record in a sort-merge
*> file description entry and it may be qualified." F-REC is a record of an FD.
*> THE RELEASE IS BEHIND A GO TO ON PURPOSE. That is the shape the defect was measured on: with the statement
*> on a path the flow skips, the run-time loud never fired either, so this program COMPILED AND RAN TO NORMAL
*> COMPLETION with no message at any stage - illegal source shipped in silence. ISO 4.2.2 paragraph 2 makes
*> the compile-time mechanism mandatory for "the general formats and the explicit syntax rules", and a
*> compile-time verdict does not care whether the statement is reached.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236RELNOSD.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-OUT ASSIGN TO "pb236relnosd.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-OUT.
01 F-REC PIC X(20).
WORKING-STORAGE SECTION.
01 WS-REC PIC X(20) VALUE "HELLO".
PROCEDURE DIVISION.
MAIN.
    GO TO SKIPPER.
    RELEASE F-REC FROM WS-REC.
SKIPPER.
    STOP RUN.
