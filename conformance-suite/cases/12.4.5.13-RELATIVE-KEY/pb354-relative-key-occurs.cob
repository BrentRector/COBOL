*>--- conformance-case
*> id: pb354-relative-key-occurs
*> rules: [SR-12.4.5.13.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.13.3 SR1 - "Data-name-1 shall not be subject to any OCCURS clauses." The
*> RELATIVE KEY clause has exactly three syntax rules; SR2 (unsigned integer without 'P') and SR3
*> (not defined within a record of the file) were implemented and SR1 was not - the shape where the
*> missing member of a rule set is hardest to see, because the others look thorough.
IDENTIFICATION DIVISION.
PROGRAM-ID. P354RKOC.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RLF ASSIGN TO "p354rkoc.dat"
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
    READ RLF NEXT AT END CONTINUE END-READ
    STOP RUN.
