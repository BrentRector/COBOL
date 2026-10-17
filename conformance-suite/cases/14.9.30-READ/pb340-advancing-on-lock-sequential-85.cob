*>--- conformance-case
*> id: pb340-advancing-on-lock-sequential-85
*> rules: [GR-14.9.30.4-22]
*> source-format: free
*> portable: unverified
*>---
      *> The SEQUENTIAL-ORGANIZATION arm of the same COBOL-2002 gate
      *> (COBOLNET0900, construct record-lock-phrase-2002):
      *> VersionConformancePass fires on BoundRead.AdvancingOnLock. Two arms,
      *> one construct - the twin of pb340-advancing-on-lock-keyed-85, so a
      *> future edit that reaches only one of them is caught (kb/Work PB340).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB340N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb340n2.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F.
           READ F NEXT ADVANCING ON LOCK
               AT END CONTINUE
           END-READ.
           CLOSE F.
           STOP RUN.
