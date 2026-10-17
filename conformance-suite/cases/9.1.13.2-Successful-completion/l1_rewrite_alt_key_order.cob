      *>--- conformance-case
      *> id: l1_rewrite_alt_key_order
      *> rules: [GR-9.1.13.2-2, GR-14.9.30.4-26, GR-14.9.30.4-32, GR-14.9.35.4-24]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.35.4 GR24 - a REWRITE repositions the record in the
      *> duplicate sets of the alternate keys it CHANGED, and in no
      *> others.  Each alternate key's retrieval order is its own.
      *> GR24 a) "When the value of a specific alternate record key is
      *>   not changed, the order of retrieval when that key is the key
      *>   of reference remains unchanged."
      *> GR24 b) "When the value of a specific alternate record key is
      *>   changed, ... the record is logically positioned last within
      *>   the set of duplicate records where the alternate record key
      *>   value is equal to the same alternate key value in one or
      *>   more records in the file".
      *> 14.9.30.4 GR26 fixes what "order" means: duplicates under the
      *> key of reference come back in RELEASE order; GR32 makes the
      *> random read return "the first record in a sequence of
      *> duplicates that was released to the operating environment".
      *> 9.1.13.2 2 c) sets '02' only when "the record just written
      *> CREATED a duplicate key value" - an unchanged key that already
      *> duplicated created nothing.
      *> Statuses on the sequential walks are 14.9.30.4 GR27 a): '02'
      *> when the record that FOLLOWS duplicates the alternate key of
      *> reference.  A format-2 random READ is not a sequential access,
      *> so GR27 never applies to it - it reports '00'.
      *>
      *> Released: 01/AA/PP, 02/AA/PP, 03/BB/PP.
      *>   R-A1: AA(01,02) BB(03)      R-A2: PP(01,02,03)
      *> LEG A - the baseline walks and the random read.
      *> LEG B - REWRITE 01 changing R-A1 only (AA -> CC).  GR24 b)
      *>   moves it under R-A1: AA(02) BB(03) CC(01) -> 02,03,01.
      *>   GR24 a) leaves R-A2 alone: still 01,02,03 - one release
      *>   ordinal per RECORD reordered it to 02,03,01 as well.  The
      *>   REWRITE creates no duplicate (CC is unique; PP is unchanged
      *>   and duplicated before the statement ran), so 9.1.13.2 2 c)
      *>   gives '00'.
      *> LEG C - REWRITE 03 changing R-A1 BB -> AA.  That DOES create a
      *>   duplicate ('02') and GR24 b) puts 03 LAST in the AA set:
      *>   AA(02,03) CC(01) -> 02,03,01.  R-A2 still 01,02,03.
      *> LEG D - CLOSE and re-OPEN: both orders survive in the physical
      *>   file, which is written in an order that is simultaneously
      *>   every key's release order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RWA01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1rwa01.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-A1 WITH DUPLICATES
               ALTERNATE RECORD KEY IS R-A2 WITH DUPLICATES
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY PIC X(2).
          05 R-A1  PIC X(2).
          05 R-A2  PIC X(2).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       01 WS-TAG PIC X.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "01" TO R-KEY.
           MOVE "AA" TO R-A1.
           MOVE "PP" TO R-A2.
           WRITE R.
           MOVE "02" TO R-KEY.
           MOVE "AA" TO R-A1.
           MOVE "PP" TO R-A2.
           WRITE R.
           MOVE "03" TO R-KEY.
           MOVE "BB" TO R-A1.
           MOVE "PP" TO R-A2.
           WRITE R.
           CLOSE F.
           OPEN I-O F.
      *> LEG A - baseline.
           MOVE "A" TO WS-TAG.
           PERFORM WALK-A2.
           PERFORM WALK-A1.
           PERFORM RANDOM-A2.
      *> LEG B - change R-A1 of record 01 only.
           MOVE "01" TO R-KEY.
           READ F KEY IS R-KEY
               INVALID KEY DISPLAY "B-READ-INV"
           END-READ.
           MOVE "CC" TO R-A1.
           REWRITE R INVALID KEY DISPLAY "B-RW-INV" END-REWRITE.
           DISPLAY "BW=" F-ST.
           MOVE "B" TO WS-TAG.
           PERFORM WALK-A2.
           PERFORM WALK-A1.
           PERFORM RANDOM-A2.
      *> LEG C - change R-A1 of record 03 into the AA duplicate set.
           MOVE "03" TO R-KEY.
           READ F KEY IS R-KEY
               INVALID KEY DISPLAY "C-READ-INV"
           END-READ.
           MOVE "AA" TO R-A1.
           REWRITE R INVALID KEY DISPLAY "C-RW-INV" END-REWRITE.
           DISPLAY "CW=" F-ST.
           MOVE "C" TO WS-TAG.
           PERFORM WALK-A2.
           PERFORM WALK-A1.
           CLOSE F.
      *> LEG D - both orders survive the physical file.
           OPEN INPUT F.
           MOVE "D" TO WS-TAG.
           PERFORM WALK-A2.
           PERFORM WALK-A1.
           CLOSE F.
           STOP RUN.
       WALK-A2.
           MOVE "PP" TO R-A2.
           START F KEY IS NOT LESS R-A2
               INVALID KEY DISPLAY "W2-START-INV"
           END-START.
           READ F NEXT AT END DISPLAY "W2-ATEND-1" END-READ.
           DISPLAY WS-TAG "2a=" R-KEY " " F-ST.
           READ F NEXT AT END DISPLAY "W2-ATEND-2" END-READ.
           DISPLAY WS-TAG "2b=" R-KEY " " F-ST.
           READ F NEXT AT END DISPLAY "W2-ATEND-3" END-READ.
           DISPLAY WS-TAG "2c=" R-KEY " " F-ST.
       WALK-A1.
           MOVE "AA" TO R-A1.
           START F KEY IS NOT LESS R-A1
               INVALID KEY DISPLAY "W1-START-INV"
           END-START.
           READ F NEXT AT END DISPLAY "W1-ATEND-1" END-READ.
           DISPLAY WS-TAG "1a=" R-KEY " " F-ST.
           READ F NEXT AT END DISPLAY "W1-ATEND-2" END-READ.
           DISPLAY WS-TAG "1b=" R-KEY " " F-ST.
           READ F NEXT AT END DISPLAY "W1-ATEND-3" END-READ.
           DISPLAY WS-TAG "1c=" R-KEY " " F-ST.
       RANDOM-A2.
           MOVE "PP" TO R-A2.
           READ F KEY IS R-A2
               INVALID KEY DISPLAY "R2-INV"
           END-READ.
           DISPLAY WS-TAG "R=" R-KEY " " F-ST.
