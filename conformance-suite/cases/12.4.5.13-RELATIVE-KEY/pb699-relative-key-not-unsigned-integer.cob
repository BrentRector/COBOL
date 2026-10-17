*>--- conformance-case
*> id: pb699-relative-key-not-unsigned-integer
*> rules: [SR-12.4.5.13.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.13.3 SR2 - "Data-name-1 shall reference an unsigned integer data item whose
*> description does not contain the picture symbol 'P'." A SIGNED item is not an unsigned one, and a
*> relative record number is "an integer value greater than zero" (12.4.5.10.3 GR4). Entry-only, and
*> the citation is repaired from the old site's "12.4.5.13 SR2" (kb/Work PB699).
IDENTIFICATION DIVISION.
PROGRAM-ID. P699LKN.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RLF ASSIGN TO "p699lkn.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS DYNAMIC
        RELATIVE KEY IS WS-RK.
DATA DIVISION.
FILE SECTION.
FD RLF.
01 RL-REC PIC X(8).
WORKING-STORAGE SECTION.
01 WS-RK PIC S9(4).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT RLF
    CLOSE RLF
    STOP RUN.
