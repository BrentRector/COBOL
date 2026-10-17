*>--- conformance-case
*> id: pb334-read-not-invalid-key-sequential-org
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB334 — the INVALID KEY bracket belongs to READ FORMAT 2 (14.9.30.2). On a file with
      *> SEQUENTIAL organization every READ is a FORMAT 1 read: 12.4.5.5.2 SR2 bars ACCESS RANDOM and
      *> DYNAMIC there, 14.9.30.3 SR8 then implies the NEXT phrase, and 14.9.30.4 GR19 makes an implied
      *> NEXT a sequential read. So the phrase is not conforming source, and the sequential arm neither
      *> reported it nor bound it: the block below was COMPILED AWAY -- the READ succeeded and neither
      *> imperative ran, although 14.9.30.4 GR13 c) transfers control to the NOT INVALID KEY imperative
      *> on a successful read. Under --permissive the phrase is a WARNING and the block does run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB334NIK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb334nik.dat"
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
           READ SQF NEXT RECORD
               NOT INVALID KEY DISPLAY "NIK"
           END-READ
           CLOSE SQF
           STOP RUN.
