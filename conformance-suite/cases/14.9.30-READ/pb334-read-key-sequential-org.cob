*>--- conformance-case
*> id: pb334-read-key-sequential-org
*> rules: [SR-14.9.30.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB334 — ISO 14.9.30.3 SR10: "The KEY phrase may be specified only if ORGANIZATION IS
      *> INDEXED is specified in the file control entry for file-name-1." The rule was enforced on the
      *> KEYED binder arm only, which reaches RELATIVE files, so the diagnostic LOOKED present; the
      *> arm every SEQUENTIAL and LINE SEQUENTIAL file takes never called readKey() at all and the
      *> phrase was parsed and dropped in silence. Edition-invariant: 14.9.30.3 carries no edition
      *> marker on this clause and Annex E lists no change to it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB334NKY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb334nky.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SQF.
       01  SQ-REC.
           05 SQ-K       PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-ST         PIC XX.
       01  WS-K          PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT SQF
           MOVE "AAA" TO WS-K
           READ SQF KEY IS WS-K
           END-READ
           CLOSE SQF
           STOP RUN.
