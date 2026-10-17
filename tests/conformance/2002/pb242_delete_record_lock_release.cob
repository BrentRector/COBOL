      *> kb/Work PB242 - ISO §14.9.10.4 GR7, the DELETE RECORD
      *> record-lock release rules, observed end to end through a
      *> second connector (the statement's single- AND multiple-
      *> locking arms, on success and on failure).
      *> "7) If record locks are in effect and the file is not subject
      *> to an active APPLY COMMIT clause, the following actions take
      *> place:
      *>  a) If single record locking is specified for the file
      *>  connector associated with file-name-1:
      *>  1. A lock held by that file connector on the deleted record
      *>  is released at the completion of the successful execution of
      *>  the DELETE RECORD statement.
      *>  2. A lock held by that file connector on another record is
      *>  released at the beginning of the execution of the DELETE
      *>  RECORD statement.
      *>  b) If multiple record locking is specified for the file
      *>  connector associated with file-name-1, all locks held on the
      *>  deleted record are released at the completion of the
      *>  successful execution of the DELETE RECORD statement."
      *> cite.py: OK  §14.9.10.4 7)  (General rules) - lead-in, a) 1.,
      *>   a) 2. and b) each checked separately
      *> cite.py: OK  §14.9.10.4 4)  (General rules) - relative,
      *>   random: the record "identified by the content of the
      *>   relative key data item"; not present -> invalid key
      *> cite.py: OK  §14.9.10.4 5)  (General rules) - "the identified
      *>   record has been logically removed from the physical file
      *>   and can no longer be accessed"
      *> cite.py: OK  §14.9.10.4 6) a)  (General rules) - conflict:
      *>   "The record is not logically removed, and may be accessed."
      *> cite.py: OK  §12.4.5.9.4 5)  (General rules) - MANUAL: "Records
      *>   locks are obtained only when the LOCK phrase is explicitly
      *>   specified on an I-O statement."
      *> cite.py: OK  §12.4.5.9.4 6)  (General rules) - single locking
      *>   is implied by LOCK MODE without LOCK ON; "Execution of any
      *>   I-O statement except START releases any previously locked
      *>   record in that file for that file connector."
      *> cite.py: OK  §12.4.5.9.4 7)  (General rules) - MULTIPLE: "a
      *>   file connector is permitted to have more than one record of
      *>   a file locked"
      *> cite.py: OK  §14.9.30.4 9)  (General rules) - a READ of "the
      *>   record identified for access is locked by another file
      *>   connector" -> record operation conflict condition
      *> cite.py: OK  §9.1.13.8 1)  (Record operation conflict condition
      *>   with unsuccessful completion) - "I-O status = 51."
      *> cite.py: OK  §14.9.51.4 32)  (General rules) - a relative I-O
      *>   random WRITE inserts the record at the relative key's number
      *> cite.py: OK  §9.1.16   (Record locking) - "all record locks
      *>   established for a file are released by the execution of an
      *>   explicit or implicit CLOSE statement for the file."
      *> APPLY COMMIT is declined (COBOLNET1709), so GR7's antecedent
      *> reduces to "record locks are in effect" - here, MANUAL mode
      *> with an explicit WITH LOCK.
      *> Probe: observer connector Q (LOCK MODE MANUAL, never locks)
      *> READs a record randomly: 51 = locked by another connector,
      *> 00 = not locked, 23 = no such record. Q also WRITEs a deleted
      *> slot back, so that a lock left on the DELETED record's id
      *> would show as 51 on Q's next READ of it. R holds a lock for
      *> the conflict case. Every sharing connector states SHARING
      *> WITH ALL OTHER explicitly (no implementor default is relied
      *> on). P = single locking (LOCK MODE MANUAL), M = multiple.
      *> Derivation of the expected lines:
      *>  S1: P READ 1 WITH LOCK -> Q1=51. P DELETE 1 -> 00; a) 1.
      *>      releases P's lock on the deleted record at completion.
      *>      Q WRITE 1 -> 00 (slot free, GR5) and Q READ 1 -> 00 with
      *>      Q's record: no lock is left on record 1.
      *>  S2: P READ 2 WITH LOCK -> Q2=51. P DELETE 3 (not locked) ->
      *>      00. a) 2. released the lock on 2 -> Q2=00; Q3=23 (GR5).
      *>  S3: P READ 2 WITH LOCK -> Q2=51. P DELETE 9 (no such slot) ->
      *>      invalid key, 23 (GR4). The lock on 2 is on another record,
      *>      released "at the beginning" (a) 2.), so although the
      *>      DELETE failed -> Q2=00.
      *>  S4: P READ 2 WITH LOCK; R READ 4 WITH LOCK. P DELETE 4 ->
      *>      51 (GR6, no RETRY phrase). a) 2. ran first, at the
      *>      beginning -> Q2=00; R still locks 4 -> Q4=51. CLOSE R
      *>      (§9.1.16) -> Q4=00 DDDDDDDDDD: GR6 a) left it present.
      *>  S5: M READ 2 WITH LOCK, READ 4 WITH LOCK (both kept,
      *>      §12.4.5.9.4 GR7) -> Q2=51, Q4=51. M DELETE 4 -> 00. b)
      *>      frees the deleted record's lock only: the lock on 2
      *>      survives -> Q2=51; Q WRITE 4 -> 00, Q READ 4 -> 00 with
      *>      Q's record.
      *>  S6: M READ 5 WITH LOCK -> Q5=51. M DELETE 9 -> 23 (GR4).
      *>      Multiple locking releases nothing at the beginning and b)
      *>      needs a successful DELETE -> Q2=51, Q5=51.
      *>  S7: CLOSE M releases all its locks (§9.1.16) -> Q2=00,
      *>      Q5=00. Final contents: 1 = Q's, 2 = BBBB, 3 deleted,
      *>      4 = Q's, 5 = EEEE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB242D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S-FILE ASSIGN TO "PB242D.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S-ST
               SHARING WITH NO OTHER.
           SELECT P-FILE ASSIGN TO "PB242D.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS P-KEY
               FILE STATUS IS P-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT M-FILE ASSIGN TO "PB242D.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS M-KEY
               FILE STATUS IS M-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT Q-FILE ASSIGN TO "PB242D.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS Q-KEY
               FILE STATUS IS Q-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT R-FILE ASSIGN TO "PB242D.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS R-KEY
               FILE STATUS IS R-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD S-FILE.
       01 S-REC PIC X(10).
       FD P-FILE.
       01 P-REC PIC X(10).
       FD M-FILE.
       01 M-REC PIC X(10).
       FD Q-FILE.
       01 Q-REC PIC X(10).
       FD R-FILE.
       01 R-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 S-ST  PIC XX.
       01 P-ST  PIC XX.
       01 M-ST  PIC XX.
       01 Q-ST  PIC XX.
       01 R-ST  PIC XX.
       01 P-KEY PIC 9(4).
       01 M-KEY PIC 9(4).
       01 Q-KEY PIC 9(4).
       01 R-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT S-FILE.
           MOVE "AAAAAAAAAA" TO S-REC. WRITE S-REC.
           MOVE "BBBBBBBBBB" TO S-REC. WRITE S-REC.
           MOVE "CCCCCCCCCC" TO S-REC. WRITE S-REC.
           MOVE "DDDDDDDDDD" TO S-REC. WRITE S-REC.
           MOVE "EEEEEEEEEE" TO S-REC. WRITE S-REC.
           CLOSE S-FILE.
           OPEN I-O P-FILE.
           OPEN I-O Q-FILE.
           OPEN I-O R-FILE.
           DISPLAY "OPEN P=" P-ST " Q=" Q-ST " R=" R-ST.
      *> S1: single locking, the deleted record's own lock is
      *> released at completion.
           MOVE 1 TO P-KEY.
           READ P-FILE WITH LOCK.
           DISPLAY "S1 READ=" P-ST.
           MOVE 1 TO Q-KEY. READ Q-FILE.
           DISPLAY "S1 Q1=" Q-ST.
           DELETE P-FILE RECORD.
           DISPLAY "S1 DEL=" P-ST.
           MOVE 1 TO Q-KEY.
           WRITE Q-REC FROM "Q1-REWRITE".
           DISPLAY "S1 QWRITE1=" Q-ST.
           MOVE 1 TO Q-KEY. READ Q-FILE.
           DISPLAY "S1 Q1=" Q-ST " " Q-REC.
      *> S2: single locking, a lock on another record is released.
           MOVE 2 TO P-KEY.
           READ P-FILE WITH LOCK.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S2 Q2=" Q-ST.
           MOVE 3 TO P-KEY.
           DELETE P-FILE RECORD.
           DISPLAY "S2 DEL=" P-ST.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S2 Q2=" Q-ST " " Q-REC.
           MOVE 3 TO Q-KEY. READ Q-FILE.
           DISPLAY "S2 Q3=" Q-ST.
      *> S3: released at the beginning although the DELETE fails.
           MOVE 2 TO P-KEY.
           READ P-FILE WITH LOCK.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S3 Q2=" Q-ST.
           MOVE 9 TO P-KEY.
           DELETE P-FILE RECORD
               INVALID KEY DISPLAY "S3 INVALID KEY"
           END-DELETE.
           DISPLAY "S3 DEL=" P-ST.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S3 Q2=" Q-ST " " Q-REC.
      *> S4: released at the beginning, ahead of the GR6 conflict.
           MOVE 2 TO P-KEY.
           READ P-FILE WITH LOCK.
           MOVE 4 TO R-KEY.
           READ R-FILE WITH LOCK.
           DISPLAY "S4 RREAD=" R-ST.
           MOVE 4 TO P-KEY.
           DELETE P-FILE RECORD.
           DISPLAY "S4 DEL=" P-ST.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S4 Q2=" Q-ST " " Q-REC.
           MOVE 4 TO Q-KEY. READ Q-FILE.
           DISPLAY "S4 Q4=" Q-ST.
           CLOSE R-FILE.
           MOVE 4 TO Q-KEY. READ Q-FILE.
           DISPLAY "S4 Q4=" Q-ST " " Q-REC.
           CLOSE P-FILE.
      *> S5: multiple locking, only the deleted record's lock goes.
           OPEN I-O M-FILE.
           MOVE 2 TO M-KEY. READ M-FILE WITH LOCK.
           DISPLAY "S5 READ2=" M-ST.
           MOVE 4 TO M-KEY. READ M-FILE WITH LOCK.
           DISPLAY "S5 READ4=" M-ST.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S5 Q2=" Q-ST.
           MOVE 4 TO Q-KEY. READ Q-FILE.
           DISPLAY "S5 Q4=" Q-ST.
           MOVE 4 TO M-KEY.
           DELETE M-FILE RECORD.
           DISPLAY "S5 DEL=" M-ST.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S5 Q2=" Q-ST.
           MOVE 4 TO Q-KEY.
           WRITE Q-REC FROM "Q4-REWRITE".
           DISPLAY "S5 QWRITE4=" Q-ST.
           MOVE 4 TO Q-KEY. READ Q-FILE.
           DISPLAY "S5 Q4=" Q-ST " " Q-REC.
      *> S6: multiple locking, a failed DELETE releases nothing.
           MOVE 5 TO M-KEY. READ M-FILE WITH LOCK.
           MOVE 5 TO Q-KEY. READ Q-FILE.
           DISPLAY "S6 Q5=" Q-ST.
           MOVE 9 TO M-KEY.
           DELETE M-FILE RECORD
               INVALID KEY DISPLAY "S6 INVALID KEY"
           END-DELETE.
           DISPLAY "S6 DEL=" M-ST.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S6 Q2=" Q-ST.
           MOVE 5 TO Q-KEY. READ Q-FILE.
           DISPLAY "S6 Q5=" Q-ST.
      *> S7: CLOSE releases every lock.
           CLOSE M-FILE.
           MOVE 1 TO Q-KEY. READ Q-FILE.
           DISPLAY "S7 Q1=" Q-ST " " Q-REC.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S7 Q2=" Q-ST " " Q-REC.
           MOVE 3 TO Q-KEY. READ Q-FILE.
           DISPLAY "S7 Q3=" Q-ST.
           MOVE 4 TO Q-KEY. READ Q-FILE.
           DISPLAY "S7 Q4=" Q-ST " " Q-REC.
           MOVE 5 TO Q-KEY. READ Q-FILE.
           DISPLAY "S7 Q5=" Q-ST " " Q-REC.
           CLOSE Q-FILE.
           STOP RUN.
