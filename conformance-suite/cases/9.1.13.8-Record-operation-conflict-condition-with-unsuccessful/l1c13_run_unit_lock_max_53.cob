      *>--- conformance-case
      *> id: l1c13_run_unit_lock_max_53
      *> rules: [GR-9.1.13.8-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.1.13.8 3) — I-O status 53: the run unit's record-lock
      *> maximum (255, CONFORMANCE.md DOC-A.1-154) is reached.
      *> "I-O status = 53. The input-output statement is unsuccessful
      *> because the statement requested a record lock, but this run
      *> unit holds the maximum number of locks allowed by this
      *> implementation."
      *>   cite.py: OK  §9.1.13.8 3)  (Record operation conflict
      *>   condition with unsuccessful completion)
      *> §12.4.5.9.4 GR7 obliges the implementor to specify both the
      *> per-connector and the per-run-unit maximum:
      *>   cite.py: OK  §12.4.5.9.4 7)  (General rules)
      *> The documented choice (docs/CONFORMANCE.md DOC-A.1-154) is 255
      *> locks per run unit, counted over every file connector; the
      *> per-connector maximum (DOC-A.1-155) is 15.
      *>
      *> SHAPE.  One relative file of 256 records, eighteen connectors
      *> C01..C18 on it, all SHARING WITH ALL OTHER and LOCK MODE
      *> MANUAL WITH LOCK ON MULTIPLE RECORDS (the only mode in which a
      *> connector can hold more than one lock, §12.4.5.9.4 GR7).
      *> Connector i (1..17) locks the DISTINCT records (i-1)*15+1 ..
      *> i*15, so no read meets a record locked by another connector
      *> (which would be '51', item 1) and no connector passes its own
      *> ceiling of 15 (which would be '54', item 4).
      *>
      *> DERIVATION.
      *>   GRANTED=0255: 17 x 15 = 255 READ ... WITH LOCK statements,
      *>     each at most the 255th run-unit lock, so each completes
      *>     '00'; the program counts the 00s (PIC 9(4)).
      *>   C18-256=53  : C18 holds NO lock (so not '54'), record 256 is
      *>     locked by nobody (so not '51'), and the run unit already
      *>     holds 255 = the maximum -> item 3 -> '53'.
      *>   C18-AGAIN=53: the '53' READ obtained no lock, so a repeat
      *>     meets the same count -> '53' again.
      *>   AFTER-CLOSE=00: CLOSE C01 releases its 15 record locks
      *>     (§9.1.16: "all record locks established for a file are
      *>     released by the execution of an explicit or implicit CLOSE
      *>     statement"; cite.py: OK  §9.1.16), the run unit then holds
      *>     240, and
      *>     C18's READ WITH LOCK of record 256 succeeds -> '00'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C13A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEEDF ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT C01 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K01
               FILE STATUS IS S01
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C02 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K02
               FILE STATUS IS S02
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C03 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K03
               FILE STATUS IS S03
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C04 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K04
               FILE STATUS IS S04
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C05 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K05
               FILE STATUS IS S05
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C06 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K06
               FILE STATUS IS S06
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C07 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K07
               FILE STATUS IS S07
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C08 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K08
               FILE STATUS IS S08
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C09 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K09
               FILE STATUS IS S09
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C10 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K10
               FILE STATUS IS S10
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C11 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K11
               FILE STATUS IS S11
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C12 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K12
               FILE STATUS IS S12
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C13 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K13
               FILE STATUS IS S13
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C14 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K14
               FILE STATUS IS S14
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C15 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K15
               FILE STATUS IS S15
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C16 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K16
               FILE STATUS IS S16
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C17 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K17
               FILE STATUS IS S17
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT C18 ASSIGN TO "L1C13A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K18
               FILE STATUS IS S18
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
       DATA DIVISION.
       FILE SECTION.
       FD SEEDF.
       01 SEED-REC PIC X(4).
       FD C01.
       01 R01 PIC X(4).
       FD C02.
       01 R02 PIC X(4).
       FD C03.
       01 R03 PIC X(4).
       FD C04.
       01 R04 PIC X(4).
       FD C05.
       01 R05 PIC X(4).
       FD C06.
       01 R06 PIC X(4).
       FD C07.
       01 R07 PIC X(4).
       FD C08.
       01 R08 PIC X(4).
       FD C09.
       01 R09 PIC X(4).
       FD C10.
       01 R10 PIC X(4).
       FD C11.
       01 R11 PIC X(4).
       FD C12.
       01 R12 PIC X(4).
       FD C13.
       01 R13 PIC X(4).
       FD C14.
       01 R14 PIC X(4).
       FD C15.
       01 R15 PIC X(4).
       FD C16.
       01 R16 PIC X(4).
       FD C17.
       01 R17 PIC X(4).
       FD C18.
       01 R18 PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST0 PIC XX.
       01 N   PIC 9(4).
       01 J   PIC 9(4).
       01 GRANTED PIC 9(4) VALUE 0.
       01 K01 PIC 9(4).
       01 S01 PIC XX.
       01 K02 PIC 9(4).
       01 S02 PIC XX.
       01 K03 PIC 9(4).
       01 S03 PIC XX.
       01 K04 PIC 9(4).
       01 S04 PIC XX.
       01 K05 PIC 9(4).
       01 S05 PIC XX.
       01 K06 PIC 9(4).
       01 S06 PIC XX.
       01 K07 PIC 9(4).
       01 S07 PIC XX.
       01 K08 PIC 9(4).
       01 S08 PIC XX.
       01 K09 PIC 9(4).
       01 S09 PIC XX.
       01 K10 PIC 9(4).
       01 S10 PIC XX.
       01 K11 PIC 9(4).
       01 S11 PIC XX.
       01 K12 PIC 9(4).
       01 S12 PIC XX.
       01 K13 PIC 9(4).
       01 S13 PIC XX.
       01 K14 PIC 9(4).
       01 S14 PIC XX.
       01 K15 PIC 9(4).
       01 S15 PIC XX.
       01 K16 PIC 9(4).
       01 S16 PIC XX.
       01 K17 PIC 9(4).
       01 S17 PIC XX.
       01 K18 PIC 9(4).
       01 S18 PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT SEEDF.
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 256
               MOVE N TO SEED-REC
               WRITE SEED-REC
           END-PERFORM.
           CLOSE SEEDF.
           OPEN I-O C01 C02 C03 C04 C05 C06 C07 C08 C09.
           OPEN I-O C10 C11 C12 C13 C14 C15 C16 C17 C18.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K01 = (1 - 1) * 15 + J
               READ C01 WITH LOCK
               IF S01 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K02 = (2 - 1) * 15 + J
               READ C02 WITH LOCK
               IF S02 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K03 = (3 - 1) * 15 + J
               READ C03 WITH LOCK
               IF S03 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K04 = (4 - 1) * 15 + J
               READ C04 WITH LOCK
               IF S04 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K05 = (5 - 1) * 15 + J
               READ C05 WITH LOCK
               IF S05 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K06 = (6 - 1) * 15 + J
               READ C06 WITH LOCK
               IF S06 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K07 = (7 - 1) * 15 + J
               READ C07 WITH LOCK
               IF S07 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K08 = (8 - 1) * 15 + J
               READ C08 WITH LOCK
               IF S08 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K09 = (9 - 1) * 15 + J
               READ C09 WITH LOCK
               IF S09 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K10 = (10 - 1) * 15 + J
               READ C10 WITH LOCK
               IF S10 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K11 = (11 - 1) * 15 + J
               READ C11 WITH LOCK
               IF S11 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K12 = (12 - 1) * 15 + J
               READ C12 WITH LOCK
               IF S12 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K13 = (13 - 1) * 15 + J
               READ C13 WITH LOCK
               IF S13 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K14 = (14 - 1) * 15 + J
               READ C14 WITH LOCK
               IF S14 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K15 = (15 - 1) * 15 + J
               READ C15 WITH LOCK
               IF S15 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K16 = (16 - 1) * 15 + J
               READ C16 WITH LOCK
               IF S16 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 15
               COMPUTE K17 = (17 - 1) * 15 + J
               READ C17 WITH LOCK
               IF S17 = "00" ADD 1 TO GRANTED END-IF
           END-PERFORM.
           DISPLAY "GRANTED=" GRANTED.
           MOVE 256 TO K18.
           READ C18 WITH LOCK.
           DISPLAY "C18-256=" S18.
           READ C18 WITH LOCK.
           DISPLAY "C18-AGAIN=" S18.
           CLOSE C01.
           READ C18 WITH LOCK.
           DISPLAY "AFTER-CLOSE=" S18.
           STOP RUN.
