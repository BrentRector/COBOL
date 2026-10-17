*>--- conformance-case
*> id: pb692-access-random-organization-sequential
*> rules: [SR-12.4.5.5.2-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.5.2 syntax rule 2: "The DYNAMIC and RANDOM phrases shall not be
*> specified for a sequential file." F's ORGANIZATION clause says SEQUENTIAL, so the RANDOM
*> phrase is closed out of this entry at every edition -- the general format says the same
*> structurally (Format 3, the sequential file control entry, admits only
*> [ ACCESS MODE IS SEQUENTIAL ]). No VERSION_CHANGE_REFERENCE row touches 12.4.5.5, and the
*> 1985 sequential I-O file control entry likewise offered no ACCESS MODE but SEQUENTIAL, so
*> all four editions reject. COBOLNET1858 (kb/Work PB692).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB692N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb692n1.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS RANDOM.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F.
           CLOSE F.
           STOP RUN.
