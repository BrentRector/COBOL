*>--- conformance-case
*> id: pb699-relative-key-occurs-entry-only
*> rules: [SR-12.4.5.13.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.13.3 SR1 - "Data-name-1 shall not be subject to any OCCURS clauses."
*> Entry-only twin of pb354-relative-key-occurs: OPEN/CLOSE and nothing else (kb/Work PB699).
IDENTIFICATION DIVISION.
PROGRAM-ID. P699LKO.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RLF ASSIGN TO "p699lko.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS DYNAMIC
        RELATIVE KEY IS WS-RK.
DATA DIVISION.
FILE SECTION.
FD RLF.
01 RL-REC PIC X(8).
WORKING-STORAGE SECTION.
01 WS-RKT.
   05 WS-RK PIC 9(4) OCCURS 3 TIMES.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT RLF
    CLOSE RLF
    STOP RUN.
