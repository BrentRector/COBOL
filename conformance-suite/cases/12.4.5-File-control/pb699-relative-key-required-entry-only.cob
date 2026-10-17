*>--- conformance-case
*> id: pb699-relative-key-required-entry-only
*> rules: [SR-12.4.5.2-10]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.2 SR10 - "The RELATIVE clause shall be specified if the DYNAMIC or RANDOM
*> phrase of the ACCESS clause is specified." THE CITATION IS THE POINT: this rejection used to cite
*> "12.4.5.13 - required for random/dynamic access", and 12.4.5.13 has no syntax rules at all (they
*> are in 12.4.5.13.3, and none of the three requires the clause); the 12.4.5.1 Format 2 diagram
*> BRACKETS the RELATIVE KEY clause, so the format does not require it either. SR10 is the one
*> sentence that does. Entry-only: no verb names the file (kb/Work PB699).
IDENTIFICATION DIVISION.
PROGRAM-ID. P699LKR.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RLF ASSIGN TO "p699lkr.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS RANDOM.
DATA DIVISION.
FILE SECTION.
FD RLF.
01 RL-REC PIC X(8).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT RLF
    CLOSE RLF
    STOP RUN.
