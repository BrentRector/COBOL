*>--- conformance-case
*> id: l1_rewrite_alt_key_suppress
*> rules: [GR-14.9.35.4-24]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.35.4 GR24 - the two SUPPRESS WHEN sub-rules of REWRITE's
      *> alternate-key repositioning, and its two closing sentences.  The
      *> a)/b) halves are pinned by l1_rewrite_alt_key_order; this program
      *> exercises the halves that only a SUPPRESS WHEN key can reach.
      *> GR24, second SUPPRESS WHEN sub-rule: "If alternate record key
      *>   suppression is specified for this alternate record key and the
      *>   value of this alternate record key is now equal to its key
      *>   suppression value: 1. the access path to the record using this
      *>   alternate record key shall no longer be provided, and 2. the
      *>   record shall be logically repositioned so that it will not be
      *>   found when accessed using this alternate record key."
      *> GR24, first SUPPRESS WHEN sub-rule: "If the SUPPRESS WHEN phrase
      *>   is specified ... and the value of the alternate record key is
      *>   no longer equal to the literal specified in that phrase: 1. an
      *>   access path to this record using this key of reference shall be
      *>   provided, and 2. the record shall be logically positioned so
      *>   that it will be found when accessed using the alternate record
      *>   key."  GR24 b) fixes WHERE: "logically positioned last within
      *>   the set of duplicate records".
      *> GR24, closing sentences: "Any number of records may have the same
      *>   alternate key value equal to its key suppression value without
      *>   requiring the DUPLICATES phrase to be specified for that key.
      *>   Key entries that are suppressed shall not cause a duplicate key
      *>   condition to exist."
      *> Supporting: 12.4.5.6.4 GR6 (a suppressed key provides no access
      *>   path); 14.9.30.4 c) ("any record identified as being suppressed
      *>   ... is not considered to exist"); 14.9.30.4 GR26 (duplicates come
      *>   back in release order) and GR27 a) ('02' when the record that
      *>   FOLLOWS duplicates the alternate key of reference); 9.1.13.2
      *>   2 c) ('02' when the record just written CREATED a duplicate key
      *>   value for an alternate key that allows duplicates).
      *> SUPPRESS WHEN is a COBOL-2023 addition (Annex E.3.3 item 42).
      *>
      *> Released 01/AA/P1, 02/BB/P2, 03/AA/P3 - release ordinals 1,2,3.
      *>   R-A1 (DUPLICATES, SUPPRESS WHEN "XX"): AA(01,03) BB(02)
      *>   R-A2 (no DUPLICATES, SUPPRESS WHEN "ZZ"): P1(01) P2(02) P3(03)
      *> LEG A - the baseline walks.  A1: 01,03,02 - 01 reports '02'
      *>   because 03 follows it with the same AA.  A2: 01,02,03 all '00'.
      *> LEG B - REWRITE 01 setting R-A1 to the suppression value "XX".
      *>   The key is suppressed, so it is skipped for uniqueness and
      *>   creates nothing: '00'.  Its A1 access path is withdrawn, so the
      *>   A1 walk is 03,02 and 01 is gone; 03 no longer has an AA
      *>   follower, so it reports '00'.
      *> LEG C - REWRITE 01 setting R-A1 back to "AA".  The key stops being
      *>   suppressed: the access path is restored and GR24 b) puts the
      *>   record LAST in the AA duplicate set - so the walk is 03,01,02,
      *>   the reverse of leg A's 01,03 within AA.  The REWRITE CREATED a
      *>   duplicate under a DUPLICATES key, so 9.1.13.2 2 c) gives '02'.
      *> LEG D - R-A2 has NO DUPLICATES phrase.  REWRITE 02 and then 03
      *>   both to R-A2 = "ZZ", the suppression value.  Two records now
      *>   share that value under a key with no DUPLICATES: GR24's closing
      *>   sentences make both REWRITEs '00', not the '22' a real duplicate
      *>   would raise, and the A2 walk finds only 01.
      *> LEG E - CLOSE and re-OPEN: both the restored A1 position and the
      *>   two withdrawn A2 paths survive the physical file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RWSUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1rwsup.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-A1 WITH DUPLICATES
                   SUPPRESS WHEN "XX"
               ALTERNATE RECORD KEY IS R-A2
                   SUPPRESS WHEN "ZZ"
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
       01 WS-EOF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "01" TO R-KEY.
           MOVE "AA" TO R-A1.
           MOVE "P1" TO R-A2.
           WRITE R.
           MOVE "02" TO R-KEY.
           MOVE "BB" TO R-A1.
           MOVE "P2" TO R-A2.
           WRITE R.
           MOVE "03" TO R-KEY.
           MOVE "AA" TO R-A1.
           MOVE "P3" TO R-A2.
           WRITE R.
           CLOSE F.
           OPEN I-O F.
      *> LEG A - baseline.
           MOVE "A" TO WS-TAG.
           PERFORM WALK-A1.
           PERFORM WALK-A2.
      *> LEG B - R-A1 of record 01 ENTERS suppression.
           MOVE "01" TO R-KEY.
           READ F KEY IS R-KEY
               INVALID KEY DISPLAY "B-READ-INV"
           END-READ.
           MOVE "XX" TO R-A1.
           REWRITE R INVALID KEY DISPLAY "B-RW-INV" END-REWRITE.
           DISPLAY "BW=" F-ST.
           MOVE "B" TO WS-TAG.
           PERFORM WALK-A1.
      *> LEG C - R-A1 of record 01 LEAVES suppression, back into AA.
           MOVE "01" TO R-KEY.
           READ F KEY IS R-KEY
               INVALID KEY DISPLAY "C-READ-INV"
           END-READ.
           MOVE "AA" TO R-A1.
           REWRITE R INVALID KEY DISPLAY "C-RW-INV" END-REWRITE.
           DISPLAY "CW=" F-ST.
           MOVE "C" TO WS-TAG.
           PERFORM WALK-A1.
      *> LEG D - two records take the suppression value of a key that has
      *> no DUPLICATES phrase.
           MOVE "02" TO R-KEY.
           READ F KEY IS R-KEY
               INVALID KEY DISPLAY "D1-READ-INV"
           END-READ.
           MOVE "ZZ" TO R-A2.
           REWRITE R INVALID KEY DISPLAY "D1-RW-INV" END-REWRITE.
           DISPLAY "DW1=" F-ST.
           MOVE "03" TO R-KEY.
           READ F KEY IS R-KEY
               INVALID KEY DISPLAY "D2-READ-INV"
           END-READ.
           MOVE "ZZ" TO R-A2.
           REWRITE R INVALID KEY DISPLAY "D2-RW-INV" END-REWRITE.
           DISPLAY "DW2=" F-ST.
           MOVE "D" TO WS-TAG.
           PERFORM WALK-A2.
           CLOSE F.
      *> LEG E - both survive the physical file.
           OPEN INPUT F.
           MOVE "E" TO WS-TAG.
           PERFORM WALK-A1.
           PERFORM WALK-A2.
           CLOSE F.
           STOP RUN.
       WALK-A1.
           MOVE 0 TO WS-EOF.
           MOVE LOW-VALUES TO R-A1.
           START F KEY IS NOT LESS R-A1
               INVALID KEY DISPLAY "W1-START-INV"
           END-START.
           PERFORM UNTIL WS-EOF = 1
               READ F NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END
                       DISPLAY WS-TAG "1=" R-KEY " " R-A1 " " F-ST
               END-READ
           END-PERFORM.
       WALK-A2.
           MOVE 0 TO WS-EOF.
           MOVE LOW-VALUES TO R-A2.
           START F KEY IS NOT LESS R-A2
               INVALID KEY DISPLAY "W2-START-INV"
           END-START.
           PERFORM UNTIL WS-EOF = 1
               READ F NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END
                       DISPLAY WS-TAG "2=" R-KEY " " R-A2 " " F-ST
               END-READ
           END-PERFORM.
