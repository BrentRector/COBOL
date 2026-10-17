      *>--- conformance-case
      *> id: l1c25_rewrite_record_lock_release
      *> rules: [GR-14.9.35.4-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.35.4 GR12 — REWRITE record-lock release/set rules
      *> (single locking a) 1. and a) 2., multiple locking b), c)).
      *> "12) If record locks are in effect, the following actions take
      *> place at the beginning or at the successful completion of the
      *> execution of the REWRITE statement:
      *>  a) If single record locking is specified for the rewrite file
      *>  connector: 1. If that file connector holds a record lock on
      *>  the record to be logically replaced, that lock is released at
      *>  completion unless the WITH LOCK phrase is specified.
      *>  2. If that
      *>  file connector holds a record lock on a record other than the
      *>  one to be logically replaced, that lock is released at the
      *>  beginning.
      *>  b) If multiple record locking is specified for the rewrite
      *>  file connector, and a record lock is associated with the
      *>  record to be logically replaced, that record lock is released
      *>  at completion only when the WITH NO LOCK phrase is specified
      *>  and the record to be logically replaced was already locked by
      *>  that file connector.
      *>  c) If the WITH LOCK phrase is specified, the record lock
      *>  associated with the record to be replaced is set at
      *>  completion."
      *> cite.py: OK  §14.9.35.4 12)  (General rules) - lead-in, a) 1.,
      *>   a) 2., b) and c) each checked separately
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
      *> cite.py: OK  §9.1.16   (Record locking) - "all record locks
      *>   established for a file are released by the execution of an
      *>   explicit or implicit CLOSE statement for the file."
      *> Probe: observer connector Q (LOCK MODE MANUAL, never locks)
      *> READs a record randomly: 51 = locked by another connector,
      *> 00 = not locked. Every sharing connector states SHARING WITH
      *> ALL OTHER explicitly (no implementor default is relied on).
      *> P = single locking (LOCK MODE MANUAL), M = multiple locking.
      *> Derivation of the expected lines:
      *>  S1: P READ 1 WITH LOCK sets P's lock on 1 -> Q1=51. P REWRITE
      *>      1 (no phrase) -> 00; a) 1. releases it at completion ->
      *>      Q1=00 and Q sees the new content.
      *>  S2: P READ 1 WITH LOCK; P REWRITE 1 WITH LOCK -> a) 1. does
      *>      not release, c) sets the lock -> Q1=51.
      *>  S3: P still locks 1; P REWRITE 2 (no phrase) -> a) 2. releases
      *>      the lock on 1 at the beginning -> Q1=00; MANUAL mode sets
      *>      no lock without a LOCK phrase -> Q2=00.
      *>  S4: P READ 3 WITH LOCK -> Q3=51. P REWRITE 9 (no such slot)
      *>      -> invalid key, 23 (GR21). The lock on 3 is on a record
      *>      other than the one to be replaced, released "at the
      *>      beginning" (a) 2.; also §12.4.5.9.4 GR6: ANY I-O statement
      *>      releases it) -> Q3=00 although the REWRITE failed.
      *>  S5: M READ 1 WITH LOCK, READ 2 WITH LOCK (both kept, GR7).
      *>      M REWRITE 1 (no phrase) -> b): released only with WITH NO
      *>      LOCK -> Q1=51; nothing releases the lock on 2 -> Q2=51.
      *>  S6: M REWRITE 2 WITH NO LOCK -> b) releases it -> Q2=00;
      *>      the lock on 1 stays -> Q1=51.
      *>  S7: M REWRITE 3 WITH LOCK (3 not locked before) -> c) sets it
      *>      -> Q3=51.
      *>  S8: CLOSE M releases all its locks (§9.1.16) -> Q1=00, Q3=00.
      *>  Final contents: 1 = M's rewrite, 2 = M's, 3 = M's, 4 intact.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C25G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S-FILE ASSIGN TO "L1C25G.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S-ST
               SHARING WITH NO OTHER.
           SELECT P-FILE ASSIGN TO "L1C25G.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS P-KEY
               FILE STATUS IS P-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT M-FILE ASSIGN TO "L1C25G.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS M-KEY
               FILE STATUS IS M-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT Q-FILE ASSIGN TO "L1C25G.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS Q-KEY
               FILE STATUS IS Q-ST
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
       WORKING-STORAGE SECTION.
       01 S-ST  PIC XX.
       01 P-ST  PIC XX.
       01 M-ST  PIC XX.
       01 Q-ST  PIC XX.
       01 P-KEY PIC 9(4).
       01 M-KEY PIC 9(4).
       01 Q-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT S-FILE.
           MOVE "AAAAAAAAAA" TO S-REC. WRITE S-REC.
           MOVE "BBBBBBBBBB" TO S-REC. WRITE S-REC.
           MOVE "CCCCCCCCCC" TO S-REC. WRITE S-REC.
           MOVE "DDDDDDDDDD" TO S-REC. WRITE S-REC.
           CLOSE S-FILE.
           OPEN I-O P-FILE.
           OPEN INPUT Q-FILE.
           DISPLAY "OPEN P=" P-ST " Q=" Q-ST.
      *> S1: single locking, no phrase: released at completion.
           MOVE 1 TO P-KEY.
           READ P-FILE WITH LOCK.
           DISPLAY "S1 READ=" P-ST.
           MOVE 1 TO Q-KEY. READ Q-FILE.
           DISPLAY "S1 Q1=" Q-ST.
           REWRITE P-REC FROM "P1-REWRITE".
           DISPLAY "S1 REW=" P-ST.
           MOVE 1 TO Q-KEY. READ Q-FILE.
           DISPLAY "S1 Q1=" Q-ST " " Q-REC.
      *> S2: single locking, WITH LOCK: kept.
           MOVE 1 TO P-KEY.
           READ P-FILE WITH LOCK.
           REWRITE P-REC FROM "P1-LOCKED " WITH LOCK.
           DISPLAY "S2 REW=" P-ST.
           MOVE 1 TO Q-KEY. READ Q-FILE.
           DISPLAY "S2 Q1=" Q-ST.
      *> S3: single locking, lock on another record released.
           MOVE 2 TO P-KEY.
           REWRITE P-REC FROM "P2-REWRITE".
           DISPLAY "S3 REW=" P-ST.
           MOVE 1 TO Q-KEY. READ Q-FILE.
           DISPLAY "S3 Q1=" Q-ST " " Q-REC.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S3 Q2=" Q-ST " " Q-REC.
      *> S4: released at the beginning even when the REWRITE fails.
           MOVE 3 TO P-KEY.
           READ P-FILE WITH LOCK.
           MOVE 3 TO Q-KEY. READ Q-FILE.
           DISPLAY "S4 Q3=" Q-ST.
           MOVE 9 TO P-KEY.
           REWRITE P-REC FROM "P9-REWRITE"
               INVALID KEY DISPLAY "S4 INVALID KEY"
           END-REWRITE.
           DISPLAY "S4 REW=" P-ST.
           MOVE 3 TO Q-KEY. READ Q-FILE.
           DISPLAY "S4 Q3=" Q-ST " " Q-REC.
           CLOSE P-FILE.
      *> S5: multiple locking, no phrase: kept.
           OPEN I-O M-FILE.
           MOVE 1 TO M-KEY. READ M-FILE WITH LOCK.
           DISPLAY "S5 READ1=" M-ST.
           MOVE 2 TO M-KEY. READ M-FILE WITH LOCK.
           DISPLAY "S5 READ2=" M-ST.
           MOVE 1 TO M-KEY.
           REWRITE M-REC FROM "M1-REWRITE".
           DISPLAY "S5 REW=" M-ST.
           MOVE 1 TO Q-KEY. READ Q-FILE.
           DISPLAY "S5 Q1=" Q-ST.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S5 Q2=" Q-ST.
      *> S6: multiple locking, WITH NO LOCK: released.
           MOVE 2 TO M-KEY.
           REWRITE M-REC FROM "M2-NOLOCK " WITH NO LOCK.
           DISPLAY "S6 REW=" M-ST.
           MOVE 2 TO Q-KEY. READ Q-FILE.
           DISPLAY "S6 Q2=" Q-ST " " Q-REC.
           MOVE 1 TO Q-KEY. READ Q-FILE.
           DISPLAY "S6 Q1=" Q-ST.
      *> S7: WITH LOCK sets a lock on a record not locked before.
           MOVE 3 TO M-KEY.
           REWRITE M-REC FROM "M3-LOCKED " WITH LOCK.
           DISPLAY "S7 REW=" M-ST.
           MOVE 3 TO Q-KEY. READ Q-FILE.
           DISPLAY "S7 Q3=" Q-ST.
      *> S8: CLOSE releases every lock.
           CLOSE M-FILE.
           MOVE 1 TO Q-KEY. READ Q-FILE.
           DISPLAY "S8 Q1=" Q-ST " " Q-REC.
           MOVE 3 TO Q-KEY. READ Q-FILE.
           DISPLAY "S8 Q3=" Q-ST " " Q-REC.
           MOVE 4 TO Q-KEY. READ Q-FILE.
           DISPLAY "S8 Q4=" Q-ST " " Q-REC.
           CLOSE Q-FILE.
           STOP RUN.
