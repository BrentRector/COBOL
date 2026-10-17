*>--- conformance-case
*> id: pb742-record-key-no-organization-clause
*> rules: [SR-12.4.5.2-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.2 SR8 sentence 1, reached through 12.4.5.10.3 GR6 - "When the ORGANIZATION
*> clause is not specified, sequential organization with the RECORD SEQUENTIAL phrase is implied."
*> ⛔ THIS IS THE SHAPE THAT MATTERS. It is not a contrived spelling: an entry written without an
*> ORGANIZATION clause at all is record sequential by GR6, so a RECORD KEY clause on it specifies
*> Format 1 for a file that is not indexed. The diagnostic must NAME the omission - a message that
*> only said "this file is sequential" would name something the source does not contain (PB742).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB742NOORG.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "pb742noorg.dat"
        RECORD KEY IS SQ-KEY.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 SQ-REC.
   05 SQ-KEY PIC X(5).
   05 SQ-DATA PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    CLOSE SQF
    STOP RUN.
