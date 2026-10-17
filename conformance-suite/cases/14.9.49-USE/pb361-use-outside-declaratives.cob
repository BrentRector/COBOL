*>--- conformance-case
*> id: pb361-use-outside-declaratives
*> rules: [SR-14.9.49.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.49.3 SR1: "A USE statement, when present, shall immediately follow a section header in
*> the declaratives portion of the procedure division and shall appear in a sentence by itself." This
*> program has no DECLARATIVES; its USE statement is in an ordinary paragraph (kb/Work PB361). It used
*> to compile clean and abort the run unit as a "not implemented" feature when reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB361UOD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb361uod.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       M1.
           DISPLAY "M1".
           STOP RUN.
       M2.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
