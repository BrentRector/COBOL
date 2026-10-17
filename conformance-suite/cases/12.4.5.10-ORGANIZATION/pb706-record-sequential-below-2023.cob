*>--- conformance-case
*> id: pb706-record-sequential-below-2023
*> rules: [FMT-12.4.5.10.2]
*> source-format: free
*> portable: unverified
*>---
      *> ORGANIZATION IS RECORD SEQUENTIAL is the written-out RECORD arm of
      *> the ISO 12.4.5.10.2 { LINE | RECORD } SEQUENTIAL inner choice,
      *> which arrived with line sequential organization in COBOL-2023
      *> (the Foreword names "Line Sequential file organization"; 9.1.7.2
      *> is what splits sequential files into record and line types).
      *> Before 2023 the format printed a lone SEQUENTIAL, so the word
      *> RECORD there is not a pre-2023 spelling: COBOLNET0900 names the
      *> edition.  The bare SEQUENTIAL spelling stays legal at every
      *> edition (kb/Work PB706).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB706NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb706n.dat"
               ORGANIZATION IS RECORD SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
