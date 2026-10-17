*>--- conformance-case
*> id: pb242-delete-lock-below-2002
*> rules: [GR-14.9.10.4-7]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB242 - the 2002 golden pb242_delete_record_lock_release
      *> DELETEs through LOCK MODE IS MANUAL connectors (single and
      *> WITH LOCK ON MULTIPLE RECORDS) that share their file through
      *> the SHARING clause and lock records with READ ... WITH LOCK:
      *> COBOL-2002 introductions (ISO 12.4.5.9, 12.4.5.15), so below
      *> 2002 the entry is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB242GATE85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT D ASSIGN TO "pb242g.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS DK
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
               FILE STATUS IS DS.
       DATA DIVISION.
       FILE SECTION.
       FD D.
       01 D-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 DK PIC 99.
       01 DS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O D
           MOVE 1 TO DK
           READ D WITH LOCK
           DELETE D RECORD
           CLOSE D
           STOP RUN.
