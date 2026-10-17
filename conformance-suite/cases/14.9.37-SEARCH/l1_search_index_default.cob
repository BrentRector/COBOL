      *>--- conformance-case
      *> id: l1_search_index_default
      *> rules: [GR-14.9.37.4-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR3 a) — "If the VARYING phrase is not
      *> specified, the search index is the index referenced by the
      *> first (or only) index-name specified in the INDEXED phrase in
      *> the OCCURS clause associated with identifier-1", together with
      *> GR3's closing sentence: "Only the data item and indexes
      *> indicated are varied by the search operation. All other indexes
      *> associated with identifier-1 are unchanged by the search
      *> operation."
      *> The table is INDEXED BY IX1 IX2 IX3 and holds 1..5. IX1 starts
      *> at 1, IX2 at 5, IX3 at 3, and no VARYING phrase is written.
      *> GR3a makes IX1 the search index; GR4 scans serially from 1 and
      *> occurrence 4 satisfies the WHEN, so GR1a leaves IX1 at 4. IX2
      *> and IX3 are untouched. The legs are distinguishable: had IX2
      *> been the search index the scan would have started at
      *> occurrence 5 (value 5, not 4) and run off the end to AT END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRIDX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 5 TIMES INDEXED BY IX1 IX2 IX3.
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
           SET IX1 TO 1.
           SET IX2 TO 5.
           SET IX3 TO 3.
           SEARCH WS-E
               AT END DISPLAY "G3=ATEND"
               WHEN WS-E(IX1) = 4
                   SET WS-A TO IX1
                   DISPLAY "G3=FOUND"
           END-SEARCH.
           SET WS-B TO IX2.
           SET WS-C TO IX3.
           DISPLAY "G3-IX1=" WS-A.
           DISPLAY "G3-IX2=" WS-B.
           DISPLAY "G3-IX3=" WS-C.
           STOP RUN.
