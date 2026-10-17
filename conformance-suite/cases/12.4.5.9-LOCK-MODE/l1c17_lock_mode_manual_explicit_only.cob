      *>--- conformance-case
      *> id: l1c17_lock_mode_manual_explicit_only
      *> rules: [GR-12.4.5.9.4-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.9.4 GR5 — MANUAL: locks only on explicit LOCK
      *> "If the MANUAL phrase is specified, the lock mode is manual.
      *>  Records locks are obtained only when the LOCK phrase is
      *>  explicitly specified on an I-O statement."
      *> cite.py --check 12.4.5.9.4 "Records locks are obtained only
      *>   when the LOCK phrase is explicitly specified on an I-O
      *>   statement." -> OK §12.4.5.9.4 5)
      *> cite.py --check 14.9.51.4 "If record locks have an effect for
      *>   the write file connector and the WITH LOCK phrase is
      *>   specified or implied, the record lock associated with the
      *>   record written is set when the execution of the WRITE
      *>   statement is successful." -> OK §14.9.51.4 11)
      *> cite.py --check 9.1.16 "While locked by a given file
      *>   connector, a record is not accessible to another file
      *>   connector" -> OK §9.1.16
      *> A and B share one relative file, both SHARING WITH ALL OTHER
      *> ("Record locks are in effect", §9.1.15 3)) and both LOCK MODE
      *> IS MANUAL.  B performs each I-O statement twice: once without
      *> and once with the LOCK phrase.  A then reads the record B
      *> just touched; a lock held by B makes that READ fail '51'
      *> (§9.1.16, §9.1.13.8), no lock lets it succeed '00'.
      *> Both halves of GR5 are exercised, for READ and for WRITE:
      *>   "only when" -> an unphrased READ / WRITE sets no lock;
      *>   "explicitly specified" -> WITH LOCK does set one.
      *> Single record locking (GR6, no MULTIPLE): each B statement
      *> releases B's previous lock, so each A probe sees only the lock
      *> of the statement just before it.
      *>
      *> DERIVED OUTPUT:
      *>   B-READ1=00 R001   plain READ under MANUAL
      *>   A-READ1=00 R001   -> no lock was obtained (GR5 "only when")
      *>   B-READ3=00 R003   READ ... WITH LOCK
      *>   A-READ3=51        -> lock obtained, A refused
      *>   B-WRITE5=00       plain WRITE of new record 5
      *>   A-READ5=00 R005   -> no lock was obtained
      *>   B-WRITE6=00       WRITE ... WITH LOCK of new record 6
      *>   A-READ6=51        -> lock obtained (WRITE GR11), A refused
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C17B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "L1C17B.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT FA ASSIGN TO "L1C17B.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K-A
               FILE STATUS IS ST-A
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT FB ASSIGN TO "L1C17B.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K-B
               FILE STATUS IS ST-B
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 S-REC PIC X(4).
       FD FA.
       01 A-REC PIC X(4).
       FD FB.
       01 B-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST-S PIC XX.
       01 ST-A PIC XX.
       01 ST-B PIC XX.
       01 K-A  PIC 9(4).
       01 K-B  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FS.
           MOVE "R001" TO S-REC.
           WRITE S-REC.
           MOVE "R002" TO S-REC.
           WRITE S-REC.
           MOVE "R003" TO S-REC.
           WRITE S-REC.
           CLOSE FS.
           OPEN I-O FA.
           OPEN I-O FB.
      *> READ without the LOCK phrase: no lock.
           MOVE 1 TO K-B.
           READ FB.
           DISPLAY "B-READ1=" ST-B " " B-REC.
           MOVE 1 TO K-A.
           READ FA.
           DISPLAY "A-READ1=" ST-A " " A-REC.
      *> READ with the LOCK phrase: lock.
           MOVE 3 TO K-B.
           READ FB WITH LOCK.
           DISPLAY "B-READ3=" ST-B " " B-REC.
           MOVE 3 TO K-A.
           READ FA.
           DISPLAY "A-READ3=" ST-A.
      *> WRITE without the LOCK phrase: no lock.
           MOVE 5 TO K-B.
           MOVE "R005" TO B-REC.
           WRITE B-REC.
           DISPLAY "B-WRITE5=" ST-B.
           MOVE 5 TO K-A.
           READ FA.
           DISPLAY "A-READ5=" ST-A " " A-REC.
      *> WRITE with the LOCK phrase: lock.
           MOVE 6 TO K-B.
           MOVE "R006" TO B-REC.
           WRITE B-REC WITH LOCK.
           DISPLAY "B-WRITE6=" ST-B.
           MOVE 6 TO K-A.
           READ FA.
           DISPLAY "A-READ6=" ST-A.
           CLOSE FB.
           CLOSE FA.
           STOP RUN.
