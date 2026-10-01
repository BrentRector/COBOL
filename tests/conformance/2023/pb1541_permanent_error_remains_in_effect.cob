      *> ISO 1989:2023 §9.1.13.1 (Permanent error condition) and Annex A.1 item 105 (DOC-A.1-105, technique CLOSE).
      *> §9.1.13.1: "Permanent error. The input-output statement was unsuccessfully executed as the result of an
      *> error that precluded further processing of the file. ... The permanent error condition remains in effect
      *> for all subsequent input-output operations on the file unless an implementor-defined technique is invoked
      *> to correct the permanent error condition." The technique this processor provides (Annex A.1 item 105, an
      *> optional item documented in docs/CONFORMANCE.md DOC-A.1-105) is CLOSE: a completed CLOSE ends the
      *> condition, so the next OPEN starts clean. Every other statement on the connector is unsuccessful, touches
      *> nothing, and sets the SAME I-O status again.
      *> The error is produced by §14.9.51.4 GR29 b): "If the relative key data item contains a value that is less
      *> than 1 ..., the execution of the WRITE statement is unsuccessful, and the I-O status for the write file
      *> connector is set to '34'" - a '3x' value, i.e. a permanent error (§9.1.13.6).
      *>
      *> WHY EACH LEG CAN FAIL: every statement below is one that would SUCCEED on a healthy connector - the WRITE
      *> of record 2 ('00' + a record), the READ of record 1 ('00'), the REWRITE and DELETE of the record just read,
      *> the START, the UNLOCK, the OPEN's own already-open answer would be '41' - so each '34' is the condition
      *> being in effect and nothing else. The reopened reads then prove that NOTHING was done while it was in
      *> effect: record 1 still holds "A" (the stuck REWRITE and DELETE changed nothing) and record 2 does not exist
      *> (the stuck WRITE stored nothing). The CLOSE answering '00' and the OPEN I-O + WRITE of record 3 answering
      *> '00' prove the technique ends it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1541PE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1541pe.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R PIC X.
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4).
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE 1 TO RK.
           MOVE "A" TO R.
           WRITE R.
           DISPLAY "WRITE1 " FS.
           CLOSE F.
           OPEN I-O F.
           DISPLAY "OPEN " FS.
      *> The permanent error.
           MOVE 0 TO RK.
           MOVE "B" TO R.
           WRITE R.
           DISPLAY "WRITE0 " FS.
      *> Everything after it, each a statement that would succeed on a healthy connector.
           MOVE 2 TO RK.
           MOVE "C" TO R.
           WRITE R.
           DISPLAY "WRITE2 " FS.
           MOVE 1 TO RK.
           READ F.
           DISPLAY "READ " FS.
           READ F NEXT.
           DISPLAY "READNEXT " FS.
           REWRITE R.
           DISPLAY "REWRITE " FS.
           DELETE F.
           DISPLAY "DELETE " FS.
           START F KEY IS EQUAL TO RK.
           DISPLAY "START " FS.
           UNLOCK F.
           DISPLAY "UNLOCK " FS.
           OPEN I-O F.
           DISPLAY "REOPEN " FS.
      *> The correction technique.
           CLOSE F.
           DISPLAY "CLOSE " FS.
           OPEN INPUT F.
           DISPLAY "OPENIN " FS.
           MOVE 1 TO RK.
           READ F.
           DISPLAY "READ1 " FS " " R.
           MOVE 2 TO RK.
           READ F.
           DISPLAY "READ2 " FS.
           CLOSE F.
           OPEN I-O F.
           MOVE 3 TO RK.
           MOVE "D" TO R.
           WRITE R.
           DISPLAY "WRITE3 " FS.
           CLOSE F.
           DISPLAY "CLOSE " FS.
           STOP RUN.
