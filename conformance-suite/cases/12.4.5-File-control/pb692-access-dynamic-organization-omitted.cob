*>--- conformance-case
*> id: pb692-access-dynamic-organization-omitted
*> rules: [SR-12.4.5.5.2-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.5.2 syntax rule 2 with the ORGANIZATION clause OMITTED -- the shape a
*> screen keyed on a written clause would miss. 12.4.5.10.3 general rule 6: "When the
*> ORGANIZATION clause is not specified, sequential organization with the RECORD SEQUENTIAL
*> phrase is implied", so F IS a sequential file and DYNAMIC is closed out of it even though
*> nothing in the entry says SEQUENTIAL. COBOLNET1858 at every edition (kb/Work PB692).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB692N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb692n3.dat"
               ACCESS MODE IS DYNAMIC.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F.
           CLOSE F.
           STOP RUN.
