      *>--- conformance-case
      *> id: l1_search_all_unsequenced_no_match
      *> rules: [GR-14.9.37.4-6]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR6 b) — "If no such setting of the search index
      *> exists, the final setting of the search index is undefined, the
      *> EC-RANGE-SEARCH-NO-MATCH exception condition is set to exist,
      *> and execution proceeds as in General rule 1b."
      *> GR6 applies only "If any condition specified in General rule 5
      *> is not satisfied", so this program VIOLATES GR5 a) on purpose:
      *> the OCCURS clause declares ASCENDING KEY IS WS-K and the
      *> program stores 09, 07, 05, 03, 01 — descending. That is the
      *> premise a SEARCH ALL over correctly-ordered keys cannot reach,
      *> and it is what separates GR6 from GR9.
      *> GR6 a) is deliberately NOT the branch under test: it offers the
      *> implementation two observably different outcomes and says "It
      *> is undefined which of these alternatives occurs", so no
      *> deterministic expectation exists for it. GR6 b) has exactly one
      *> outcome, and it is reached here because no occurrence holds 04.
      *> Expected: AT END is taken (GR1b1) and the last exception status
      *> names the condition GR6b sets. The final setting of the search
      *> index is undefined and is therefore never displayed.
       >>TURN EC-RANGE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRA6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-KE OCCURS 5 TIMES
             ASCENDING KEY IS WS-K INDEXED BY KX.
             10 WS-K PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 9 TO WS-K (1).
           MOVE 7 TO WS-K (2).
           MOVE 5 TO WS-K (3).
           MOVE 3 TO WS-K (4).
           MOVE 1 TO WS-K (5).
           SEARCH ALL WS-KE
               AT END DISPLAY "G6=ATEND"
               WHEN WS-K(KX) = 4
                   DISPLAY "G6=FOUND"
           END-SEARCH.
           IF FUNCTION EXCEPTION-STATUS = "EC-RANGE-SEARCH-NO-MATCH"
               DISPLAY "G6-EC=NO-MATCH"
           ELSE
               DISPLAY "G6-EC=OTHER"
           END-IF.
           DISPLAY "G6-AFTER".
           STOP RUN.
