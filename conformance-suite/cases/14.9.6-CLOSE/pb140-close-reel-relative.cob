*>--- conformance-case
*> id: pb140-close-reel-relative
*> rules: [SR-14.9.6.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.6.3 SR1: the NO REWIND, REEL and UNIT phrases may be used
      *> only with files of sequential organization (kb/Work PB140: the old
      *> acceptance degraded to a stale FILE STATUS value at run time).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB140N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "r.dat"
               ORGANIZATION RELATIVE ACCESS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD R.
       01 R-REC PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           CLOSE R UNIT
           STOP RUN.
