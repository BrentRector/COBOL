*>--- conformance-case
*> id: pb2848-lock-clause-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2848 - the 2002 golden pb2848_lock_ceiling_after_record_rules
      *> writes WITH LOCK on a LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE
      *> RECORDS connector sharing its file through the SHARING clause,
      *> COBOL-2002 introductions (ISO 12.4.5.9, 12.4.5.15), so below
      *> 2002 the entry is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2848GATE85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT Q ASSIGN TO "pb2848g.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS QK
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
               FILE STATUS IS QS.
       DATA DIVISION.
       FILE SECTION.
       FD Q.
       01 Q-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 QK PIC 99.
       01 QS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT Q
           MOVE 1 TO QK
           WRITE Q-REC WITH LOCK
           CLOSE Q
           STOP RUN.
