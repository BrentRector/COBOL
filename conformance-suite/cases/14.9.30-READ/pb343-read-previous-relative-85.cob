*>--- conformance-case
*> id: pb343-read-previous-relative-85
*> rules: [GR-14.9.30.4-21]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB343 — READ ... PREVIOUS is a COBOL-2002 introduction (constructs.json
      *> `read-previous-2002`, COBOLNET0900).  The gate is ONE arm over IBoundRead, so it
      *> shall fire on the RELATIVE organization exactly as it does on the sequential one
      *> (negative/pb334-read-previous-85) and the indexed one
      *> (unit:EditionGateDiagnosticTests.ReadPrevious_At85_Registry0900_Not0860).  The
      *> behaviour leg is 2002|2014|2023/pb343_read_previous_relative.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB343N85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb343n85.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-K
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  RLF.
       01  RL-REC        PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-ST         PIC XX.
       01  WS-K          PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT RLF
           READ RLF PREVIOUS RECORD
               AT END CONTINUE
           END-READ
           CLOSE RLF
           STOP RUN.
