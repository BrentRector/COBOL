*>--- conformance-case
*> id: pb699-relative-key-in-record-entry-only
*> rules: [SR-12.4.5.13.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.13.3 SR3 - "Data-name-1 shall not be defined in a record description entry
*> subordinate to the associated file-name." The relative key holds the record number the next I-O
*> statement is to use, so an item inside the record area would be overwritten by every READ.
*> Entry-only (kb/Work PB699); the citation is repaired here too - the old site said "12.4.5.13 SR3".
IDENTIFICATION DIVISION.
PROGRAM-ID. P699LKI.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RLF ASSIGN TO "p699lki.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS DYNAMIC
        RELATIVE KEY IS RL-RK.
DATA DIVISION.
FILE SECTION.
FD RLF.
01 RL-REC.
   05 RL-RK PIC 9(4).
   05 RL-FILLER PIC X(4).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT RLF
    CLOSE RLF
    STOP RUN.
