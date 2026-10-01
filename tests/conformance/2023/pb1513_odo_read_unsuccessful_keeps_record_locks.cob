      *> kb/Work PB1513 (train review finding F2) - an OCCURS DEPENDING ON READ made unsuccessful with '34'
      *> (section 9.1.13.6 item 4 b) gives back only the record lock THAT READ took. Under MANUAL + MULTIPLE record
      *> locking, section 14.9.30.4 GR11 b): "no record locks are released, except when the NO LOCK phrase is
      *> specified and the record accessed was already locked by that file connector. In this case, that record lock
      *> is released at the completion of the successful execution of the READ statement", and GR9 makes a record
      *> locked by another file connector a record operation conflict ('51') for every READ of it. The lock actions
      *> belong to a SUCCESSFUL READ, so an unsuccessful one must leave every lock as it found it.
      *> A '34' is a permanent error that "remains in effect for all subsequent input-output operations on the
      *> file" (section 9.1.13.1, DOC-A.1-105: CLOSE ends it), so each scenario is its own OPEN of F, and F2 - a
      *> different file connector on the same physical file - is the observer.
      *>
      *> EXPECTED, DERIVED:
      *>  A1  F READ record 1 WITH LOCK, count 3 (fits)      -> '00'; F holds record 1 (GR11 d).
      *>  A2  F2 READ record 1 WITH LOCK                     -> '51' (GR9).
      *>  A3  F READ record 1, count 50 (record too long)    -> '34'. The lock is an EARLIER READ's, not this one's.
      *>  A4  F2 READ record 1 WITH LOCK                     -> '51' still: F still holds it (the finding: '00').
      *>  B1  fresh OPEN; F READ record 2 WITH LOCK, count 50 -> '34'. The READ is unsuccessful, so GR11 d)'s lock
      *>      (only for a "successfully accessed record") is not set, and none is left behind.
      *>  B2  F2 READ record 2 WITH LOCK                     -> '00': nothing holds record 2.
      *>  C1  fresh OPEN; F READ record 1 WITH LOCK, count 3  -> '00'; F holds record 1.
      *>  C2  F READ record 1 WITH NO LOCK, count 50          -> '34'. GR11 b)'s release is for the SUCCESSFUL
      *>      execution, so F's lock on record 1 is NOT released.
      *>  C3  F2 READ record 1 WITH LOCK                     -> '51'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1513LK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1513lk.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
               SHARING WITH ALL OTHER
               FILE STATUS IS FS.
           SELECT F2 ASSIGN TO "pb1513lk.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK2
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
               SHARING WITH ALL OTHER
               FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R.
           05 H PIC XX.
           05 T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON WS-N.
       FD  F2.
       01  R2 PIC X(12).
       WORKING-STORAGE SECTION.
       01  FS  PIC XX.
       01  FS2 PIC XX.
       01  RK  PIC 9.
       01  RK2 PIC 9.
       01  WS-N PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F2.
           MOVE 1 TO RK2.
           MOVE "ABCDEFGHIJKL" TO R2.
           WRITE R2.
           MOVE 2 TO RK2.
           MOVE "MNOPQRSTUVWX" TO R2.
           WRITE R2.
           CLOSE F2.
           OPEN I-O F2.
           OPEN I-O F.
           MOVE 3 TO WS-N.
           MOVE 1 TO RK.
           READ F WITH LOCK.
           DISPLAY "A1 " FS.
           MOVE 1 TO RK2.
           READ F2 WITH LOCK.
           DISPLAY "A2 " FS2.
           MOVE 50 TO WS-N.
           MOVE 1 TO RK.
           READ F.
           DISPLAY "A3 " FS.
           MOVE 1 TO RK2.
           READ F2 WITH LOCK.
           DISPLAY "A4 " FS2.
           CLOSE F.
           OPEN I-O F.
           MOVE 50 TO WS-N.
           MOVE 2 TO RK.
           READ F WITH LOCK.
           DISPLAY "B1 " FS.
           MOVE 2 TO RK2.
           READ F2 WITH LOCK.
           DISPLAY "B2 " FS2.
           CLOSE F.
           OPEN I-O F.
           MOVE 3 TO WS-N.
           MOVE 1 TO RK.
           READ F WITH LOCK.
           DISPLAY "C1 " FS.
           MOVE 50 TO WS-N.
           MOVE 1 TO RK.
           READ F WITH NO LOCK.
           DISPLAY "C2 " FS.
           MOVE 1 TO RK2.
           READ F2 WITH LOCK.
           DISPLAY "C3 " FS2.
           CLOSE F2.
           CLOSE F.
           STOP RUN.
