*>--- conformance-case
*> id: pb445_search_all_format2_legal
*> rules: [SR-14.9.37.3-7, SR-14.9.37.3-8, SR-14.9.37.3-9,
*>    SR-14.9.37.3-10, SR-14.9.37.3-11, SR-14.9.37.3-13]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB445 - the LEGAL Format-2 SEARCH ALL shapes (ISO 1989:2023 14.9.37.2 Format 2 and
      *> 14.9.37.3 SR7-SR13). The companion to the fourteen pb445-* negatives: every operand form the
      *> printed general format admits, so the SR7-SR13 screens cannot be tightened into rejecting
      *> conforming source without this program going red.
      *>
      *> A. literal-1            WHEN K1 (IX1) = 05
      *> B. identifier-3         WHEN K1 (IX1) = BASE           (BASE is neither a key nor IX1-subscripted)
      *> C. arithmetic-expr-1    WHEN K1 (IX1) = BASE + 4
      *> D. a QUALIFIED key      WHEN K1 OF E1 (IX1) = 09       (SR8 is about the item, not the spelling)
      *> E. the AND repetition   WHEN KA (IX2) = 03 AND KB (IX2) = 02
      *> F. condition-name-1     WHEN KA-IS-3 (IX2) AND KB (IX2) = 01
      *>                         (single-valued, on a key, and SR11 satisfied because the condition-name's
      *>                          own data-name KA is the key that precedes KB)
      *> G. the AT END path      WHEN K1 (IX1) = 04             (no such key value)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLLEGAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1 OCCURS 5 TIMES
               ASCENDING KEY IS K1
               INDEXED BY IX1.
             10 K1 PIC 9(2).
             10 V1 PIC X(3).
       01 T2.
          05 E2 OCCURS 4 TIMES
               ASCENDING KEY IS KA KB
               INDEXED BY IX2.
             10 KA PIC 9(2).
                88 KA-IS-3 VALUE 03.
             10 KB PIC 9(2).
             10 V2 PIC X(3).
       01 R    PIC 9.
       01 BASE PIC 9(2) VALUE 03.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "01aaa" TO E1 (1).
           MOVE "03bbb" TO E1 (2).
           MOVE "05ccc" TO E1 (3).
           MOVE "07ddd" TO E1 (4).
           MOVE "09eee" TO E1 (5).
           MOVE "0101pqr" TO E2 (1).
           MOVE "0301stu" TO E2 (2).
           MOVE "0302vwx" TO E2 (3).
           MOVE "0501yza" TO E2 (4).
           SEARCH ALL E1
               AT END DISPLAY "A NONE"
               WHEN K1 (IX1) = 05
                   SET R TO IX1
                   DISPLAY "A " R " " V1 (IX1)
           END-SEARCH.
           SEARCH ALL E1
               AT END DISPLAY "B NONE"
               WHEN K1 (IX1) = BASE
                   SET R TO IX1
                   DISPLAY "B " R " " V1 (IX1)
           END-SEARCH.
           SEARCH ALL E1
               AT END DISPLAY "C NONE"
               WHEN K1 (IX1) = BASE + 4
                   SET R TO IX1
                   DISPLAY "C " R " " V1 (IX1)
           END-SEARCH.
           SEARCH ALL E1
               AT END DISPLAY "D NONE"
               WHEN K1 OF E1 (IX1) = 09
                   SET R TO IX1
                   DISPLAY "D " R " " V1 (IX1)
           END-SEARCH.
           SEARCH ALL E2
               AT END DISPLAY "E NONE"
               WHEN KA (IX2) = 03 AND KB (IX2) = 02
                   SET R TO IX2
                   DISPLAY "E " R " " V2 (IX2)
           END-SEARCH.
           SEARCH ALL E2
               AT END DISPLAY "F NONE"
               WHEN KA-IS-3 (IX2) AND KB (IX2) = 01
                   SET R TO IX2
                   DISPLAY "F " R " " V2 (IX2)
           END-SEARCH.
           SEARCH ALL E1
               AT END DISPLAY "G NONE"
               WHEN K1 (IX1) = 04
                   DISPLAY "G HIT"
           END-SEARCH.
           STOP RUN.
