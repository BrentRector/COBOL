*>--- conformance-case
*> id: file_sharing
*> rules: [GR-9.1.13.9-1, GR-14.9.27.4-22, GR-14.9.47.4-1,
*>    FMT-14.9.47.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FILESHARE.
      *> Phase 4d M2-FILE-1 demo: two RELATIVE connectors on one physical
      *> file exercise SHARING (61), LOCK MODE MANUAL, WITH LOCK (51),
      *> RETRY, IGNORING LOCK, and UNLOCK in one run unit (ISO 14.9.27/.30/.47).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "share.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS SEED-KEY
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "share.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS A-KEY
               FILE STATUS IS A-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-B ASSIGN TO "share.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS B-KEY
               FILE STATUS IS B-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD F-SEED.
       01 SEED-REC PIC X(5).
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 SEED-KEY PIC 9(4).
       01 A-KEY    PIC 9(4).
       01 B-KEY    PIC 9(4).
       01 SEED-ST  PIC XX.
       01 A-ST     PIC XX.
       01 B-ST     PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> Seed two records through an ordinary (non-sharing) connector.
           OPEN OUTPUT F-SEED.
           MOVE 1 TO SEED-KEY. MOVE "ALPHA" TO SEED-REC. WRITE SEED-REC.
           MOVE 2 TO SEED-KEY. MOVE "BRAVO" TO SEED-REC. WRITE SEED-REC.
           CLOSE F-SEED.
      *> Open the two sharing connectors (both ALL OTHER -> no conflict).
           OPEN I-O F-A. DISPLAY "OPEN-A=" A-ST.
           OPEN I-O F-B. DISPLAY "OPEN-B=" B-ST.
      *> A locks record 1.
           MOVE 1 TO A-KEY. READ F-A WITH LOCK. DISPLAY "READA=" A-ST.
      *> B's plain read of record 1 is blocked by A's lock -> 51.
           MOVE 1 TO B-KEY. READ F-B. DISPLAY "READB=" B-ST.
      *> B retries; A cannot release mid single-thread -> still 51.
           MOVE 1 TO B-KEY. READ F-B RETRY 2 TIMES. DISPLAY "RETRYB=" B-ST.
      *> B ignores the lock and reads ALPHA.
           MOVE 1 TO B-KEY. READ F-B IGNORING LOCK. DISPLAY "IGN=" B-REC.
      *> A unlocks; B's read now succeeds -> 00.
           UNLOCK F-A.
           MOVE 1 TO B-KEY. READ F-B. DISPLAY "AFTER=" B-ST.
      *> B reopens exclusive while A is still open -> 61.
           CLOSE F-B.
           OPEN I-O SHARING WITH NO OTHER F-B. DISPLAY "EXCL=" B-ST.
           CLOSE F-A.
           STOP RUN.
