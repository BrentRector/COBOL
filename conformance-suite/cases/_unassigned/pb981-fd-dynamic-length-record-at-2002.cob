*>--- conformance-case
*> id: pb981-fd-dynamic-length-record-at-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB981 - the DYNAMIC LENGTH clause is a COBOL-2014 addition (ISO 8.5.1.10 / 13.18.19), so
      *> below 2014 a dynamic-length FD record is refused by the edition gate (COBOLNET0900) - the
      *> out-of-line record area of determination D-FRA exists only where the clause does.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB981NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb981neg.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R2 PIC X(10).
       01 R3 PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           WRITE R3
           CLOSE F
           STOP RUN.
