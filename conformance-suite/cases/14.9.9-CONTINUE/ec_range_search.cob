*>--- conformance-case
*> id: ec_range_search
*> rules: [SR-14.9.9.3-1, SR-14.9.9.3-2, GR-14.9.37.4-4, GR-14.9.37.4-9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> EC-RANGE-SEARCH-INDEX / EC-RANGE-SEARCH-NO-MATCH (ISO §14.9.37.4 GR4/GR6/GR9; Table 13 Nonfatal). A serial
      *> SEARCH whose INITIAL index is < 1 or > the highest permissible occurrence sets SEARCH-INDEX (GR4 — this also
      *> fixes a latent bug: the initial index < 1 now goes to AT END/unsuccessful, not a phantom occurrence 0). A scan
      *> that advances past the last occurrence with no WHEN satisfied sets NO-MATCH (GR6; SEARCH ALL GR9). A SUCCESSFUL
      *> search sets neither. Observed via FUNCTION EXCEPTION-STATUS under >>TURN EC-RANGE CHECKING ON (the level-2
      *> parent enables both level-3 names). The AT END phrase is taken on the unsuccessful path (GR1b).
      >>TURN EC-RANGE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-RNG-SR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 3 TIMES INDEXED BY IX.
       01 WS-KT.
          05 WS-KE OCCURS 3 TIMES ASCENDING KEY IS WS-K INDEXED BY KX.
             10 WS-K PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 5 TO WS-E (1).
           MOVE 6 TO WS-E (2).
           MOVE 7 TO WS-E (3).
           MOVE 1 TO WS-K (1).
           MOVE 3 TO WS-K (2).
           MOVE 5 TO WS-K (3).
      *> successful serial search: WHEN body runs, no EC set.
           SET IX TO 1.
           SEARCH WS-E
               WHEN WS-E (IX) = 6 DISPLAY "FOUND-AT-2"
           END-SEARCH.
           DISPLAY "AFTER-MATCH[" FUNCTION EXCEPTION-STATUS "]".
      *> serial, initial index 0 (< 1): SEARCH-INDEX + AT END.
           SET IX TO 0.
           SEARCH WS-E AT END DISPLAY "ATEND-LOW"
               WHEN WS-E (IX) = 9 CONTINUE
           END-SEARCH.
           DISPLAY "LOW[" FUNCTION EXCEPTION-STATUS "]".
      *> serial, initial index 4 (> max 3): SEARCH-INDEX + AT END.
           SET IX TO 4.
           SEARCH WS-E AT END DISPLAY "ATEND-HIGH"
               WHEN WS-E (IX) = 5 CONTINUE
           END-SEARCH.
           DISPLAY "HIGH[" FUNCTION EXCEPTION-STATUS "]".
      *> serial, valid start, no match: advance off end -> NO-MATCH + AT END.
           SET IX TO 1.
           SEARCH WS-E AT END DISPLAY "ATEND-NM"
               WHEN WS-E (IX) = 9 CONTINUE
           END-SEARCH.
           DISPLAY "NM[" FUNCTION EXCEPTION-STATUS "]".
      *> SEARCH ALL, no key match: NO-MATCH (GR9) + AT END.
           SEARCH ALL WS-KE AT END DISPLAY "ATEND-ALL"
               WHEN WS-K (KX) = 9 CONTINUE
           END-SEARCH.
           DISPLAY "ALL-NM[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
