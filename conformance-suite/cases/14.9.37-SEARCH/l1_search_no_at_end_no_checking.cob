      *>--- conformance-case
      *> id: l1_search_no_at_end_no_checking
      *> rules: [GR-14.9.37.4-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR1 b) 3 — "If the AT END phrase is not
      *> specified and neither exception condition was raised because
      *> the checking for those exception conditions was not enabled,
      *> control is transferred to the end of the SEARCH statement."
      *> §14.6.13.1.1: "By default, checking is not enabled for any
      *> exception condition", and this program contains no TURN
      *> directive and no declaratives, so no applicable exception
      *> processing statement exists and GR1b2 cannot apply either.
      *> Both unsuccessful shapes GR4 defines are run with no AT END
      *> phrase, and each is followed by a DISPLAY that can only be
      *> reached if control was transferred to the end of the SEARCH:
      *>   A1 — a valid initial index whose scan exhausts the table
      *>        (the EC-RANGE-SEARCH-NO-MATCH shape);
      *>   A2 — an initial index of zero, which GR4 calls unsuccessful
      *>        before any comparison (the EC-RANGE-SEARCH-INDEX
      *>        shape). §13.18.38.4 GR2 guarantees an index accepts at
      *>        least occurrence (1 - 5), so SET IX TO 0 is legal here.
      *> The L23-EC line pins the OTHER conjunct of GR1b3's premise,
      *> that neither condition was RAISED. §14.6.13.1.1: "if checking
      *> for an exception that occurs is not enabled, no exception
      *> condition is raised", so the last exception status still
      *> indicates that no exception condition exists and §15.33.3
      *> rule 1 returns 31 alphanumeric spaces. Without that line a
      *> compiler that wrongly enabled EC-RANGE checking by default
      *> would print exactly the same output: both conditions are
      *> nonfatal (Table 13) and no declarative exists, so control
      *> would still reach the end of the SEARCH — green for a reason
      *> that is not GR1b3.
      *> The final line proves the run unit continued past both.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRNC23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 5 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO WS-E (1).
           MOVE 2 TO WS-E (2).
           MOVE 3 TO WS-E (3).
           MOVE 4 TO WS-E (4).
           MOVE 5 TO WS-E (5).
           SET IX TO 1.
           SEARCH WS-E
               WHEN WS-E(IX) = 8
                   DISPLAY "A1=FOUND"
           END-SEARCH.
           DISPLAY "A1-AFTER".
           SET IX TO 0.
           SEARCH WS-E
               WHEN WS-E(IX) = 1
                   DISPLAY "A2=FOUND"
           END-SEARCH.
           DISPLAY "A2-AFTER".
           DISPLAY "L23-EC[" FUNCTION EXCEPTION-STATUS "]".
           DISPLAY "L23-DONE".
           STOP RUN.
