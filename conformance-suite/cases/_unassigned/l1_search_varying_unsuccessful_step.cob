      *>--- conformance-case
      *> id: l1_search_varying_unsuccessful_step
      *> rules: []
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR3 b) 1 and b) 2 on the branch the successful
      *> legs cannot reach: how many times identifier-2 is incremented
      *> when the scan ends UNSUCCESSFULLY by exhaustion.
      *> GR4 fixes the count: "If none of the conditions is satisfied,
      *> the search index is incremented by one occurrence number. The
      *> process is then repeated using the new index setting UNLESS the
      *> new value for the search index corresponds to a table element
      *> outside the permissible range" — the increment happens first
      *> and the NEW value is what is tested. Over a 5-occurrence table
      *> scanned from occurrence 1 with no match there are therefore
      *> five failed evaluations and five increments, the fifth
      *> producing occurrence 6, which ends the search.
      *> Leg A (GR3b2, integer item): 20 + 5 = 25.
      *> Leg B (GR3b1, index data item): occurrence 1 + 5 = 6, which is
      *> inside the range §13.18.38.4 GR2 requires an index to accept
      *> for a 5-element table ((1 - 5) through (2 * 5)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRVUS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 5 TIMES INDEXED BY IX1.
       01 WS-N PIC 9(2) VALUE 0.
       01 WS-D USAGE IS INDEX.
       01 WS-B PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO WS-E (1).
           MOVE 2 TO WS-E (2).
           MOVE 3 TO WS-E (3).
           MOVE 4 TO WS-E (4).
           MOVE 5 TO WS-E (5).
           MOVE 20 TO WS-N.
           SET IX1 TO 1.
           SEARCH WS-E VARYING WS-N
               AT END DISPLAY "UA=ATEND"
               WHEN WS-E(IX1) = 8
                   CONTINUE
           END-SEARCH.
           DISPLAY "UA-N=" WS-N.
           SET IX1 TO 1.
           SET WS-D TO IX1.
           SET IX1 TO 1.
           SEARCH WS-E VARYING WS-D
               AT END DISPLAY "UB=ATEND"
               WHEN WS-E(IX1) = 8
                   CONTINUE
           END-SEARCH.
           SET IX1 TO WS-D.
           SET WS-B TO IX1.
           DISPLAY "UB-D=" WS-B.
           STOP RUN.
