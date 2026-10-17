*>--- conformance-case
*> id: l1_search_all_duplicate_keys
*> rules: [GR-14.9.37.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.37.4 GR7 — "If both conditions specified in General
      *> rule 5 are satisfied and there is more than one setting of the
      *> search index for which all conditions in the WHEN phrase are
      *> satisfied, the search operation is successful. The final
      *> setting of the search index is equal to one of them, but it is
      *> undefined which one."
      *> GR5 a) holds: the contents of WS-K are 01, 03, 03, 03, 09,
      *> which is the ascending order the ASCENDING phrase declares
      *> (§13.18.38.4 GR3). Duplicates have to be compatible with §14.9.37.4 GR5a,
      *> because GR7's own premise — more than one satisfying setting
      *> WHILE GR5 is satisfied — can arise no other way. GR5 b) holds
      *> vacuously: WS-KE is not subordinate to any OCCURS clause.
      *> Three settings (2, 3 and 4) satisfy WHEN WS-K = 03, so GR7 is
      *> the governing rule. Only what GR7 fixes is asserted:
      *>   G7=FOUND      the search IS successful (GR1a, not AT END);
      *>   G7-KEY-OK     the final setting satisfies the WHEN phrase;
      *>   G7-IX-IN-SET  it is one of the settings that do (2..4).
      *> WHICH of the three is undefined and is never printed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRA7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-KE OCCURS 5 TIMES
             ASCENDING KEY IS WS-K INDEXED BY KX.
             10 WS-K PIC 9(2).
       01 WS-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO WS-K (1).
           MOVE 3 TO WS-K (2).
           MOVE 3 TO WS-K (3).
           MOVE 3 TO WS-K (4).
           MOVE 9 TO WS-K (5).
           SET KX TO 1.
           SEARCH ALL WS-KE
               AT END DISPLAY "G7=ATEND"
               WHEN WS-K(KX) = 3
                   SET WS-N TO KX
                   DISPLAY "G7=FOUND"
           END-SEARCH.
           IF WS-K(KX) = 3
               DISPLAY "G7-KEY-OK"
           ELSE
               DISPLAY "G7-KEY-BAD"
           END-IF.
           IF WS-N > 1 AND WS-N < 5
               DISPLAY "G7-IX-IN-SET"
           ELSE
               DISPLAY "G7-IX-OUT"
           END-IF.
           STOP RUN.
