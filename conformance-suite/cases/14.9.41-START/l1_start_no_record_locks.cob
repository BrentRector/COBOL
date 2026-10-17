      *>--- conformance-case
      *> id: l1_start_no_record_locks
      *> rules: [GR-14.9.41.4-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.41.4 GR3 — "The execution of the START statement
      *> does not detect, acquire, or release record locks."
      *> Three prohibitions, one line of evidence each. Two file
      *> connectors, FA and FB, share one physical indexed file with
      *> SHARING WITH ALL OTHER and LOCK MODE IS AUTOMATIC, so
      *> §12.4.5.9.4 GR4 applies — "If the AUTOMATIC phrase is
      *> specified, the lock mode is automatic. Records are locked when
      *> any READ statement is executed."
      *>   AREAD   FA reads BBB200 -> '00', and by GR4 now holds its
      *>           lock.
      *>   BLOCKED FB reads the same record -> '51' (§9.1.13.8 item 1,
      *>           "an attempt to access a record that is currently
      *>           locked by another file connector"). This line makes
      *>           the three that follow meaningful: without it a
      *>           uniform '00' would prove nothing.
      *>   ASTART  FA issues a START. It succeeds, '00'.
      *>   STILL   ...does NOT RELEASE: FB's read of BBB200 is STILL
      *>           '51'. §12.4.5.9.4 GR6 states the same exemption from
      *>           the other side — "Execution of any I-O statement
      *>           except START releases any previously locked record
      *>           in that file for that file connector" — so had this
      *>           been a READ instead, FB would now see '00'.
      *>   NOTHELD ...does NOT ACQUIRE: CCC300, the record FA's START
      *>           positioned at, is NOT locked, so FB reads it '00'.
      *>   BSTART  ...does NOT DETECT: FB starts on BBB200, the record
      *>           FA holds locked, and gets '00' — a START never
      *>           consults the lock, where FB's READ of the very same
      *>           record was refused.
      *> The three records are seeded through a third connector that is
      *> closed before FA and FB open, so Table 19 (§14.9.27.4 GR4) has
      *> only the two sharing-with-all-other connectors to arbitrate,
      *> and both opens are "Normal open".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1STR03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "l1str03.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS S-KEY
               FILE STATUS IS ST-S.
           SELECT FA ASSIGN TO "l1str03.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS A-KEY
               FILE STATUS IS ST-A
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC.
           SELECT FB ASSIGN TO "l1str03.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS B-KEY
               FILE STATUS IS ST-B
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 S-REC.
          05 S-KEY PIC X(6).
          05 S-VAL PIC X(4).
       FD FA.
       01 A-REC.
          05 A-KEY PIC X(6).
          05 A-VAL PIC X(4).
       FD FB.
       01 B-REC.
          05 B-KEY PIC X(6).
          05 B-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST-S PIC XX.
       01 ST-A PIC XX.
       01 ST-B PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FS
           MOVE "AAA100" TO S-KEY
           MOVE "VAAA" TO S-VAL
           WRITE S-REC
           MOVE "BBB200" TO S-KEY
           MOVE "VBBB" TO S-VAL
           WRITE S-REC
           MOVE "CCC300" TO S-KEY
           MOVE "VCCC" TO S-VAL
           WRITE S-REC
           CLOSE FS
           OPEN I-O FA
           DISPLAY "OPENA=" ST-A
           OPEN I-O FB
           DISPLAY "OPENB=" ST-B
      *> FA locks BBB200 (LOCK MODE AUTOMATIC).
           MOVE "BBB200" TO A-KEY
           READ FA INVALID KEY CONTINUE END-READ
           DISPLAY "AREAD=" ST-A
           MOVE "BBB200" TO B-KEY
           READ FB INVALID KEY CONTINUE END-READ
           DISPLAY "BLOCKED=" ST-B
      *> The START under test.
           MOVE "BBB200" TO A-KEY
           START FA KEY IS > A-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "ASTART=" ST-A
      *> ...did not RELEASE FA's lock.
           MOVE "BBB200" TO B-KEY
           READ FB INVALID KEY CONTINUE END-READ
           DISPLAY "STILL=" ST-B
      *> ...did not ACQUIRE a lock on the record it positioned at.
           MOVE "CCC300" TO B-KEY
           READ FB INVALID KEY CONTINUE END-READ
           DISPLAY "NOTHELD=" ST-B
      *> ...does not DETECT a lock another connector holds.
           MOVE "BBB200" TO B-KEY
           START FB KEY IS EQUAL TO B-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "BSTART=" ST-B
           CLOSE FA
           CLOSE FB
           STOP RUN.
