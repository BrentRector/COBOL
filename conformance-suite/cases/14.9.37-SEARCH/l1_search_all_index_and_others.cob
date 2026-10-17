      *>--- conformance-case
      *> id: l1_search_all_index_and_others
      *> rules: [GR-14.9.37.4-8]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR8 — "The search index is the index referenced
      *> by the first (or only) index-name specified in the INDEXED
      *> phrase in the OCCURS clause associated with identifier-1. Any
      *> other indexes associated with identifier-1 remain unchanged by
      *> the search operation."
      *> WS-KE is INDEXED BY KX KY with ASCENDING KEY IS WS-K holding
      *> 01, 03, 05, 07, 09 — GR5 a) satisfied, GR5 b) vacuous. KX is
      *> pre-set to 5 and KY to 2, and the WHEN names 07, which exactly
      *> one occurrence holds.
      *>   G8-KX=4  KX is the search index. Exactly one setting
      *>            satisfies the WHEN, so GR7 does not apply and GR9's
      *>            success clause routes to GR1a, which leaves the
      *>            search index at the satisfying occurrence — 4. The
      *>            pre-set 5 is not the answer, which also shows GR9's
      *>            "The initial setting of the search index is
      *>            ignored" being honoured.
      *>   G8-KY=2  KY is another index of identifier-1 and Format 2
      *>            has no VARYING phrase that could reach it, so it
      *>            still holds the 2 it was set to.
      *> §14.9.37.3 SR8 is respected: data-name-1 is subscripted by the
      *> FIRST index-name associated with identifier-1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRA8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-KE OCCURS 5 TIMES
             ASCENDING KEY IS WS-K INDEXED BY KX KY.
             10 WS-K PIC 9(2).
       01 WS-A PIC 9 VALUE 0.
       01 WS-B PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO WS-K (1).
           MOVE 3 TO WS-K (2).
           MOVE 5 TO WS-K (3).
           MOVE 7 TO WS-K (4).
           MOVE 9 TO WS-K (5).
           SET KX TO 5.
           SET KY TO 2.
           SEARCH ALL WS-KE
               AT END DISPLAY "G8=ATEND"
               WHEN WS-K(KX) = 7
                   SET WS-A TO KX
                   DISPLAY "G8=FOUND"
           END-SEARCH.
           SET WS-B TO KY.
           DISPLAY "G8-KX=" WS-A.
           DISPLAY "G8-KY=" WS-B.
           STOP RUN.
