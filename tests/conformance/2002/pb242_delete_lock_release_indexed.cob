      *> kb/Work PB242 - ISO §14.9.10.4 GR7 on an INDEXED file, under
      *> LOCK MODE IS AUTOMATIC, through BOTH ways a DELETE identifies
      *> its record: the prime record key (random/dynamic access, GR3)
      *> and the last record read (sequential access, GR2). The lock
      *> a READ set must be found and released under the SAME record
      *> identity the DELETE computes; the relative twin is
      *> pb242_delete_record_lock_release (single AND multiple locking).
      *> cite.py: OK  §14.9.10.4 7) a) 1.  (General rules) - "A lock
      *>   held by that file connector on the deleted record is
      *>   released at the completion of the successful execution of
      *>   the DELETE RECORD statement."
      *> cite.py: OK  §14.9.10.4 7) a) 2.  (General rules) - "A lock
      *>   held by that file connector on another record is released
      *>   at the beginning of the execution of the DELETE RECORD
      *>   statement."
      *> cite.py: OK  §14.9.10.4 3)  (General rules) - indexed, random
      *>   or dynamic: "the record identified by the content of the
      *>   prime record key data item"
      *> cite.py: OK  §14.9.10.4 2)  (General rules) - sequential
      *>   access: the last I-O statement "shall have been a
      *>   successfully executed READ statement"; that record goes
      *> cite.py: OK  §12.4.5.9.4 4)  (General rules) - AUTOMATIC:
      *>   "Records are locked when any READ statement is executed."
      *> cite.py: OK  §12.4.5.9.4 5)  (General rules) - MANUAL (the
      *>   observer Q): "Records locks are obtained only when the LOCK
      *>   phrase is explicitly specified on an I-O statement."
      *> cite.py: OK  §14.9.30.4 9)  (General rules) - a READ of "the
      *>   record identified for access is locked by another file
      *>   connector" -> record operation conflict condition (51,
      *>   §9.1.13.8 1))
      *> cite.py: OK  §14.9.51.4 36)  (General rules) - a WRITE of a
      *>   prime key no record has (the deleted one's) inserts it
      *> Derivation of the expected lines (Q never locks; 51 = locked
      *> by another connector, 00 = free):
      *>  S1: P READ K001 (AUTOMATIC locks it) -> Q1=51. P DELETE K001
      *>      -> 00, a) 1. releases at completion: Q WRITE K001 -> 00,
      *>      Q READ K001 -> 00 with Q's record.
      *>  S2: P READ K002 -> Q2=51. P DELETE K003 -> 00; a) 2. had
      *>      released the lock on K002 at the beginning -> Q2=00.
      *>  S3: CLOSE P. W (sequential access) READs its first record
      *>      K001 (locked) -> Q1=51. W DELETE (the record just read,
      *>      GR2) -> 00, a) 1. -> Q WRITE K001 -> 00, READ -> 00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB242X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P-FILE ASSIGN TO "PB242X.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS P-K
               FILE STATUS IS P-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC.
           SELECT W-FILE ASSIGN TO "PB242X.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS W-K
               FILE STATUS IS W-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC.
           SELECT Q-FILE ASSIGN TO "PB242X.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS Q-K
               FILE STATUS IS Q-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD P-FILE.
       01 P-REC.
          05 P-K PIC X(4).
          05 P-D PIC X(6).
       FD W-FILE.
       01 W-REC.
          05 W-K PIC X(4).
          05 W-D PIC X(6).
       FD Q-FILE.
       01 Q-REC.
          05 Q-K PIC X(4).
          05 Q-D PIC X(6).
       WORKING-STORAGE SECTION.
       01 P-ST  PIC XX.
       01 W-ST  PIC XX.
       01 Q-ST  PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT P-FILE.
           MOVE "K001AAAAAA" TO P-REC. WRITE P-REC.
           MOVE "K002BBBBBB" TO P-REC. WRITE P-REC.
           MOVE "K003CCCCCC" TO P-REC. WRITE P-REC.
           CLOSE P-FILE.
           OPEN I-O P-FILE.
           OPEN I-O Q-FILE.
           DISPLAY "OPEN P=" P-ST " Q=" Q-ST.
      *> S1: the deleted record's own lock, by prime record key.
           MOVE "K001" TO P-K. READ P-FILE.
           DISPLAY "S1 READ=" P-ST.
           MOVE "K001" TO Q-K. READ Q-FILE.
           DISPLAY "S1 Q1=" Q-ST.
           DELETE P-FILE RECORD.
           DISPLAY "S1 DEL=" P-ST.
           MOVE "K001QQQQQQ" TO Q-REC. WRITE Q-REC.
           DISPLAY "S1 QWRITE=" Q-ST.
           MOVE "K001" TO Q-K. READ Q-FILE.
           DISPLAY "S1 Q1=" Q-ST " " Q-REC.
      *> S2: a lock on another record goes at the beginning.
           MOVE "K002" TO P-K. READ P-FILE.
           MOVE "K002" TO Q-K. READ Q-FILE.
           DISPLAY "S2 Q2=" Q-ST.
           MOVE "K003" TO P-K. DELETE P-FILE RECORD.
           DISPLAY "S2 DEL=" P-ST.
           MOVE "K002" TO Q-K. READ Q-FILE.
           DISPLAY "S2 Q2=" Q-ST " " Q-REC.
           CLOSE P-FILE.
      *> S3: sequential access - the record last read is deleted.
           OPEN I-O W-FILE.
           READ W-FILE.
           DISPLAY "S3 READ=" W-ST " " W-REC.
           MOVE "K001" TO Q-K. READ Q-FILE.
           DISPLAY "S3 Q1=" Q-ST.
           DELETE W-FILE RECORD.
           DISPLAY "S3 DEL=" W-ST.
           MOVE "K001ZZZZZZ" TO Q-REC. WRITE Q-REC.
           DISPLAY "S3 QWRITE=" Q-ST.
           MOVE "K001" TO Q-K. READ Q-FILE.
           DISPLAY "S3 Q1=" Q-ST " " Q-REC.
           CLOSE W-FILE Q-FILE.
           STOP RUN.
