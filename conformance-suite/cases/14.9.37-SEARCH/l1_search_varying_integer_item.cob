      *>--- conformance-case
      *> id: l1_search_varying_integer_item
      *> rules: [GR-14.9.37.4-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR3 b) 2 — "If identifier-2 references an
      *> integer data item, that data item is incremented by the value
      *> one at the same time as the search index is incremented."
      *> The amount is the LITERAL one, not the search index's value or
      *> delta-in-anything-else, so the item's final value is its
      *> starting value plus the number of unsuccessful passes GR4 made.
      *> Leg A: N starts at 10, search index at 1, match at occurrence
      *>        4 = three passes -> 13.
      *> Leg B: N starts at 10, search index at 3, match at occurrence
      *>        4 = one pass -> 11.
      *> Leg A also separates GR3b2 from an assignment: were the item
      *> set from the index it would read 04, and were it incremented by
      *> the index it would read 16.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRVIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 5 TIMES INDEXED BY IX1.
       01 WS-N PIC 9(2) VALUE 0.
       01 WS-A PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO WS-E (1).
           MOVE 2 TO WS-E (2).
           MOVE 3 TO WS-E (3).
           MOVE 4 TO WS-E (4).
           MOVE 5 TO WS-E (5).
           MOVE 10 TO WS-N.
           SET IX1 TO 1.
           SEARCH WS-E VARYING WS-N
               AT END DISPLAY "L32A=ATEND"
               WHEN WS-E(IX1) = 4
                   SET WS-A TO IX1
           END-SEARCH.
           DISPLAY "L32A IX=" WS-A " N=" WS-N.
           MOVE 10 TO WS-N.
           SET IX1 TO 3.
           SEARCH WS-E VARYING WS-N
               AT END DISPLAY "L32B=ATEND"
               WHEN WS-E(IX1) = 4
                   SET WS-A TO IX1
           END-SEARCH.
           DISPLAY "L32B IX=" WS-A " N=" WS-N.
           STOP RUN.
