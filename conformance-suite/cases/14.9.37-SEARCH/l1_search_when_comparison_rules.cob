      *>--- conformance-case
      *> id: l1_search_when_comparison_rules
      *> rules: [GR-14.9.37.4-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR2 — "The comparison associated with each WHEN
      *> phrase is executed in accordance with the rules specified for
      *> conditional expressions." GR2 is an ALL FORMATS rule, so both
      *> formats are exercised, and the legs are chosen so that only a
      *> §8.8.4-conformant comparison lands on the printed occurrence.
      *> N1  §8.8.4.2.4 — numeric comparison is algebraic "regardless of
      *>     the manner in which their usage is described" and the
      *>     literal's digit count "is not significant": a BINARY
      *>     S9(4) holding 0001 equals the literal 1 (occurrence 2).
      *> A1  §8.8.4.2.7 2) — unequal-length alphanumeric comparison
      *>     extends the shorter operand on the right with spaces, so
      *>     X(5) "ZZ   " equals the 2-character literal (occurrence 2).
      *> K1  the Format-2 WHEN, relation form (occurrence 3).
      *> K2  the Format-2 WHEN, condition-name form (occurrence 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRCMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T1.
          05 WS-E OCCURS 4 TIMES INDEXED BY IX.
             10 WS-NB PIC S9(4) USAGE BINARY.
             10 WS-A PIC X(5).
       01 WS-T2.
          05 WS-KE OCCURS 5 TIMES
             ASCENDING KEY IS WS-K INDEXED BY KX.
             10 WS-K PIC 9(2).
                88 WS-K-IS-FIVE VALUE 5.
       01 WS-R PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 7 TO WS-NB (1).
           MOVE 1 TO WS-NB (2).
           MOVE 42 TO WS-NB (3).
           MOVE 3 TO WS-NB (4).
           MOVE "AB" TO WS-A (1).
           MOVE "ZZ" TO WS-A (2).
           MOVE "QQ" TO WS-A (3).
           MOVE "MM" TO WS-A (4).
           MOVE 1 TO WS-K (1).
           MOVE 3 TO WS-K (2).
           MOVE 5 TO WS-K (3).
           MOVE 7 TO WS-K (4).
           MOVE 9 TO WS-K (5).
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "N1=ATEND"
               WHEN WS-NB(IX) = 1
                   SET WS-R TO IX
                   DISPLAY "N1=" WS-R
           END-SEARCH.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "A1=ATEND"
               WHEN WS-A(IX) = "ZZ"
                   SET WS-R TO IX
                   DISPLAY "A1=" WS-R
           END-SEARCH.
           SET KX TO 1.
           SEARCH ALL WS-KE
               AT END DISPLAY "K1=ATEND"
               WHEN WS-K(KX) = 5
                   SET WS-R TO KX
                   DISPLAY "K1=" WS-R
           END-SEARCH.
           SET KX TO 1.
           SEARCH ALL WS-KE
               AT END DISPLAY "K2=ATEND"
               WHEN WS-K-IS-FIVE (KX)
                   SET WS-R TO KX
                   DISPLAY "K2=" WS-R
           END-SEARCH.
           STOP RUN.
