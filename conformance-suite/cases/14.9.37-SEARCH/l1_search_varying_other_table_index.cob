      *>--- conformance-case
      *> id: l1_search_varying_other_table_index
      *> rules: [GR-14.9.37.4-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR3 c) 2 — "If index-name-1 is not one of the
      *> indexes specified in the INDEXED phrase in the OCCURS clause
      *> associated with identifier-1, the search index is the same as
      *> in General rule 3a. The index referenced by index-name-1 is
      *> incremented by one occurrence number at the same time as the
      *> search index is incremented." Plus GR3's closing sentence,
      *> "All other indexes associated with identifier-1 are unchanged".
      *> WS-E is INDEXED BY IX1 IX2 and holds 1..5; WS-F is a different
      *> table INDEXED BY JX. IX1 starts at 1, IX2 at 5, JX at 3, and
      *> the statement is SEARCH WS-E VARYING JX.
      *>   IX1 = 4  — the search index is still GR3a's first index-name
      *>              and GR1a leaves it at the matching occurrence.
      *>   JX  = 6  — three unsuccessful passes, three occurrence
      *>              numbers: 3 + 3. §13.18.38.4 GR2 guarantees an
      *>              index accepts at least (1 - 5) through (2 * 5),
      *>              so 6 is inside the required range of a 5-element
      *>              table's index.
      *>   IX2 = 5  — an index of identifier-1 that was not indicated.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRVOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 5 TIMES INDEXED BY IX1 IX2.
       01 WS-U.
          05 WS-F PIC 9 OCCURS 5 TIMES INDEXED BY JX.
       01 WS-A PIC 9 VALUE 0.
       01 WS-B PIC 9 VALUE 0.
       01 WS-C PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO WS-E (1).
           MOVE 2 TO WS-E (2).
           MOVE 3 TO WS-E (3).
           MOVE 4 TO WS-E (4).
           MOVE 5 TO WS-E (5).
           MOVE 0 TO WS-F (1).
           MOVE 0 TO WS-F (2).
           MOVE 0 TO WS-F (3).
           MOVE 0 TO WS-F (4).
           MOVE 0 TO WS-F (5).
           SET IX1 TO 1.
           SET IX2 TO 5.
           SET JX TO 3.
           SEARCH WS-E VARYING JX
               AT END DISPLAY "L42=ATEND"
               WHEN WS-E(IX1) = 4
                   SET WS-A TO IX1
                   DISPLAY "L42=FOUND"
           END-SEARCH.
           SET WS-B TO JX.
           SET WS-C TO IX2.
           DISPLAY "L42-IX1=" WS-A.
           DISPLAY "L42-JX=" WS-B.
           DISPLAY "L42-IX2=" WS-C.
           STOP RUN.
