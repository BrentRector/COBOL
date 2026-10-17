*>--- conformance-case
*> id: pb742-relative-key-on-sequential-file
*> rules: [SR-12.4.5.2-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.2 SR9 sentence 1 - "Format 2 shall be specified only for a relative file."
*> SR8's twin one format over: the RELATIVE KEY clause appears only in the 12.4.5.1 Format 2
*> (relative) entry, so writing it on a sequential file specifies Format 2 for a file the rule does
*> not admit. It is in the same change set as SR8 deliberately: a rule set with one member enforced
*> is exactly the shape in which the missing member hides (kb/Work PB742).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB742SEQRL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RLF ASSIGN TO "pb742seqrl.dat"
        ORGANIZATION IS SEQUENTIAL
        RELATIVE KEY IS WS-RK.
DATA DIVISION.
FILE SECTION.
FD RLF.
01 RL-REC PIC X(10).
WORKING-STORAGE SECTION.
01 WS-RK PIC 9(4).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT RLF
    CLOSE RLF
    STOP RUN.
