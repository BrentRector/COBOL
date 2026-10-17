*>--- conformance-case
*> id: pb1026-external-dynamic-record-at-2002
*> rules: [GR-13.18.22.4-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1026 - the DYNAMIC LENGTH clause is a COBOL-2014 addition (ISO 8.5.1.10 / 13.18.19), so
      *> below 2014 a dynamic-length record of an EXTERNAL file is refused by the edition gate
      *> (COBOLNET0900) - the shared out-of-line record area exists only where the clause does.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1026NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "pb1026neg.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD EF IS EXTERNAL.
       01 R1 PIC X(10).
       01 R2 PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT EF
           WRITE R2
           CLOSE EF
           STOP RUN.
