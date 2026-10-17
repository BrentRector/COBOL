*>--- conformance-case
*> id: pb352-start-length-non-indexed
*> rules: [SR-14.9.41.3-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.41.3 SR8 - "If the LENGTH phrase is specified, file-name-1 shall reference a file
*> with indexed organization." Two spellings of the same violation, and until kb/Work PB352 only ONE of
*> them was reachable: the relative arm reported SR8, while on a SEQUENTIAL file the check sat behind an
*> early return, so `START SQF KEY IS = SQ-REC WITH LENGTH 2` violated SR8 AND SR2 and reported NEITHER.
*> The organization-independent syntax rules are now screened before either arm bails, so this program
*> draws SR8 for both files (and SR2 for the sequential one, which is the point: every rule a statement
*> violates gets reported).
IDENTIFICATION DIVISION.
PROGRAM-ID. P352LEN8.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "p352len8s.dat"
        ORGANIZATION IS SEQUENTIAL
        ACCESS MODE IS SEQUENTIAL.
    SELECT RLF ASSIGN TO "p352len8r.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS DYNAMIC
        RELATIVE KEY IS WS-RK.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 SQ-REC PIC X(5).
FD RLF.
01 RL-REC PIC X(8).
WORKING-STORAGE SECTION.
01 WS-RK PIC 9(4).
PROCEDURE DIVISION.
MAIN.
    START SQF KEY IS = SQ-REC WITH LENGTH 2 END-START
    START RLF KEY IS = WS-RK WITH LENGTH 2 END-START
    STOP RUN.
