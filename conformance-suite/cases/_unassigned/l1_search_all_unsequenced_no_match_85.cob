      *>--- conformance-case
      *> id: l1_search_all_unsequenced_no_match_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR6 b) at COBOL-85 — the edition-invariant half.
      *> Same shape as the 2023 witness: the OCCURS clause declares
      *> ASCENDING KEY IS WS-K and the program stores 09, 07, 05, 03,
      *> 01, so GR5 a) is violated and GR6 governs; no occurrence holds
      *> 04, so GR6 b) is the branch, and execution proceeds as in
      *> GR1b — the AT END phrase is taken and control then reaches the
      *> end of the SEARCH statement.
      *> The exception-condition half of GR6b is not observable at this
      *> edition (the EC model and the TURN directive are COBOL-2002
      *> additions), so only the control-flow half is asserted here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRA685.
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
           DISPLAY "G6-AFTER".
           STOP RUN.
