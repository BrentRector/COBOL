*>--- conformance-case
*> id: pb340-advancing-on-lock-keyed-85
*> rules: [GR-14.9.30.4-22]
*> source-format: free
*> portable: unverified
*>---
      *> The record-lock phrases of the READ statement are a COBOL-2002
      *> introduction, so READ ... ADVANCING ON LOCK is rejected at --std 85
      *> (COBOLNET0900, construct record-lock-phrase-2002).
      *> This is the KEYED arm of that gate: VersionConformancePass fires on
      *> BoundKeyedRead.AdvancingOnLock. The gate has always existed but nothing
      *> exercised it - the version-matrix row for the family writes
      *> READ ... WITH LOCK - and while the phrase was DROPPED at emission
      *> (kb/Work PB340) a green edition gate was the only sign of support the
      *> construct had.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB340N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb340n1.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS K.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(5).
       WORKING-STORAGE SECTION.
       01 K PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F.
           READ F NEXT ADVANCING ON LOCK
               AT END CONTINUE
           END-READ.
           CLOSE F.
           STOP RUN.
