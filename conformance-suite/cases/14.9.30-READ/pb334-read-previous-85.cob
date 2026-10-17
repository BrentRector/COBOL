*>--- conformance-case
*> id: pb334-read-previous-85
*> rules: [GR-14.9.30.4-21]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB334 — READ ... PREVIOUS is a COBOL-2002 introduction (constructs.json
      *> `read-previous-2002`, COBOLNET0900). The gate keys on the bound read's KIND, and the
      *> sequential-organization node had no kind: this program compiled CLEAN at --std 85 while the
      *> identical statement on an INDEXED file was correctly rejected — one rule, two dispatch arms,
      *> one of them fixed. The behaviour leg is 2002|2014|2023/pb334_read_previous_sequential.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB334N85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb334n85.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SQF.
       01  SQ-REC        PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-ST         PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT SQF
           READ SQF PREVIOUS RECORD
               AT END CONTINUE
           END-READ
           CLOSE SQF
           STOP RUN.
