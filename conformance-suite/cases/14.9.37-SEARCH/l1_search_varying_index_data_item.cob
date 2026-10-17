      *>--- conformance-case
      *> id: l1_search_varying_index_data_item
      *> rules: [GR-14.9.37.4-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR3 b) 1 — "If identifier-2 references an index
      *> data item, that data item is incremented by the same amount as,
      *> and at the same time as, the search index."
      *> GR4 increments the search index by ONE OCCURRENCE NUMBER per
      *> unsuccessful pass, so "the same amount" is one occurrence
      *> number per pass. §13.18.60.4 GR10 (USAGE) fixes the only
      *> portable reading of an index data item's content: it "contains
      *> a value that shall correspond to an occurrence number", the
      *> stored representation being implementor-defined — so the item
      *> is seeded and read back through an index-name (§14.9.39.4 GR2
      *> b) and c)), never displayed directly.
      *> Leg A: index data item at occurrence 2, search index at 1,
      *>        match at occurrence 4 = THREE passes -> 2 + 3 = 5.
      *> Leg B: index data item at occurrence 1, search index at 3,
      *>        match at occurrence 4 = ONE pass -> 1 + 1 = 2.
      *> The two legs differ in the DELTA, not the absolute value, which
      *> is what "the same amount" requires and what distinguishes it
      *> from an assignment of the search index.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRVID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 5 TIMES INDEXED BY IX1.
       01 WS-D USAGE IS INDEX.
       01 WS-A PIC 9 VALUE 0.
       01 WS-B PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO WS-E (1).
           MOVE 2 TO WS-E (2).
           MOVE 3 TO WS-E (3).
           MOVE 4 TO WS-E (4).
           MOVE 5 TO WS-E (5).
           SET IX1 TO 2.
           SET WS-D TO IX1.
           SET IX1 TO 1.
           SEARCH WS-E VARYING WS-D
               AT END DISPLAY "L31A=ATEND"
               WHEN WS-E(IX1) = 4
                   SET WS-A TO IX1
           END-SEARCH.
           SET IX1 TO WS-D.
           SET WS-B TO IX1.
           DISPLAY "L31A IX=" WS-A " D=" WS-B.
           SET IX1 TO 1.
           SET WS-D TO IX1.
           SET IX1 TO 3.
           SEARCH WS-E VARYING WS-D
               AT END DISPLAY "L31B=ATEND"
               WHEN WS-E(IX1) = 4
                   SET WS-A TO IX1
           END-SEARCH.
           SET IX1 TO WS-D.
           SET WS-B TO IX1.
           DISPLAY "L31B IX=" WS-A " D=" WS-B.
           STOP RUN.
