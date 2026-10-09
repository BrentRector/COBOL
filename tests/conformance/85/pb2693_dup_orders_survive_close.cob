      *> kb/Work PB2693 (and PB701) - EVERY ALTERNATE KEY'S DUPLICATE ORDER
      *> SURVIVES THE CLOSE, even when no single order of the records could
      *> carry them all.
      *>
      *> 14.9.30.4 GR26: "records having the same duplicate value in an
      *> alternate record key that is the key of reference are made
      *> available in the same order ... in which they are released by
      *> execution of WRITE statements, or by execution of REWRITE
      *> statements that create such duplicate values".
      *> 14.9.35.4 GR24 a): an alternate key a REWRITE does
      *> not change keeps its order; b): a changed one makes the record "last
      *> within the set of duplicate records".
      *>
      *> Expected values: 01 and 02 are released in that order with R-A1 =
      *> "AA" and R-A2 = "PP". REWRITE 01 with R-A1 = "BB" (b): it leaves the
      *> AA set), then with R-A1 = "AA" again (b): it re-enters the AA set
      *> LAST); R-A2 never changes (a)). So under R-A1 the AA set is 02, 01
      *> and under R-A2 the PP set is 01, 02 - two orders no single sequence
      *> of the two records can hold. After CLOSE and OPEN INPUT each walk
      *> (START at the key, READ NEXT through the set) still yields them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2693DOC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2693doc.dat"
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
       01 WS-SET PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "01AAPP" TO R.
           WRITE R.
           MOVE "02AAPP" TO R.
           WRITE R.
           CLOSE F.
           OPEN I-O F.
           MOVE "01" TO R-KEY.
           READ F KEY IS R-KEY INVALID KEY DISPLAY "READ-INV" END-READ.
           MOVE "BB" TO R-A1.
           REWRITE R INVALID KEY DISPLAY "RW1-INV" END-REWRITE.
           MOVE "AA" TO R-A1.
           REWRITE R INVALID KEY DISPLAY "RW2-INV" END-REWRITE.
           DISPLAY "RW=" F-ST.
           CLOSE F.
           OPEN INPUT F.
           MOVE "AA" TO R-A1.
           START F KEY IS EQUAL TO R-A1
               INVALID KEY DISPLAY "START-A1-INV"
           END-START.
           MOVE "AA" TO WS-SET.
           DISPLAY "A1:" WITH NO ADVANCING.
           PERFORM WALK-A1.
           MOVE "PP" TO R-A2.
           START F KEY IS EQUAL TO R-A2
               INVALID KEY DISPLAY "START-A2-INV"
           END-START.
           MOVE "PP" TO WS-SET.
           DISPLAY "A2:" WITH NO ADVANCING.
           PERFORM WALK-A2.
           CLOSE F.
           STOP RUN.
       WALK-A1.
           READ F NEXT RECORD AT END MOVE SPACES TO R END-READ.
           PERFORM UNTIL R-A1 NOT = WS-SET
               DISPLAY " " R-KEY WITH NO ADVANCING
               READ F NEXT RECORD AT END MOVE SPACES TO R END-READ
           END-PERFORM.
           DISPLAY " ".
       WALK-A2.
           READ F NEXT RECORD AT END MOVE SPACES TO R END-READ.
           PERFORM UNTIL R-A2 NOT = WS-SET
               DISPLAY " " R-KEY WITH NO ADVANCING
               READ F NEXT RECORD AT END MOVE SPACES TO R END-READ
           END-PERFORM.
           DISPLAY " ".
