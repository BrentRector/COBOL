      *>--- conformance-case
      *> id: l1_search_varying_same_table_index
      *> rules: [GR-14.9.37.4-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR3 c) 1 — "If index-name-1 is specified in the
      *> INDEXED BY phrase in the OCCURS clause associated with
      *> identifier-1, the index referenced by index-name-1 IS the
      *> search index." Not an extra varied index: the search index
      *> itself, which displaces the GR3a default.
      *> The table is INDEXED BY IX1 IX2 and holds 1..5. IX1 (the GR3a
      *> default) is pre-set to 5 and IX2 to 2, and the statement is
      *> SEARCH WS-E VARYING IX2. Three consequences follow and all
      *> three are printed:
      *>   the scan STARTS from IX2's value (GR4 reads the search
      *>   index's initial setting: 2, not IX1's 5);
      *>   it ENDS at the matching occurrence (GR1a: IX2 = 4);
      *>   IX1 is UNCHANGED (GR3's closing sentence: 5).
      *> Had IX1 remained the search index the scan would have begun at
      *> occurrence 5 and run off the end, taking AT END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRVSM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 5 TIMES INDEXED BY IX1 IX2.
       01 WS-A PIC 9 VALUE 0.
       01 WS-B PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO WS-E (1).
           MOVE 2 TO WS-E (2).
           MOVE 3 TO WS-E (3).
           MOVE 4 TO WS-E (4).
           MOVE 5 TO WS-E (5).
           SET IX1 TO 5.
           SET IX2 TO 2.
           SEARCH WS-E VARYING IX2
               AT END DISPLAY "L41=ATEND"
               WHEN WS-E(IX2) = 4
                   SET WS-A TO IX2
                   DISPLAY "L41=FOUND"
           END-SEARCH.
           SET WS-B TO IX1.
           DISPLAY "L41-IX2=" WS-A.
           DISPLAY "L41-IX1=" WS-B.
           STOP RUN.
