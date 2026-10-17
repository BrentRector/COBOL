      *>--- conformance-case
      *> id: l1_search_when_condition_forms
      *> rules: [GR-8.8.4.6.3-1, SR-14.9.37.3-6, FMT-8.8.4.6.2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.3 SR6 — "Condition-1 may be any conditional
      *> expression evaluated as specified in 8.8.4, Conditional
      *> expressions." SR6 is a PERMISSION, so its testable content is
      *> BREADTH: one Format-1 SEARCH per §8.8.4 sub-form over one
      *> table, each printing the occurrence the scan stopped on
      *> (§14.9.37.4 GR1a: the search index "remains set at the
      *> occurrence number that caused a WHEN condition to be
      *> satisfied"; GR4 makes the scan serial from the initial index).
      *> Table, 5 occurrences:  K = 1, 2, -3, 7, 9
      *>                        V = "abc","a1c","xyz","def","ghi"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRWCF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON
                           OFF STATUS IS SW1-OFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E OCCURS 5 TIMES INDEXED BY IX.
             10 WS-K PIC S9.
                88 WS-K-IS-SEVEN VALUE 7.
             10 WS-V PIC X(3).
       01 WS-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO WS-K (1).
           MOVE 2 TO WS-K (2).
           MOVE -3 TO WS-K (3).
           MOVE 7 TO WS-K (4).
           MOVE 9 TO WS-K (5).
           MOVE "abc" TO WS-V (1).
           MOVE "a1c" TO WS-V (2).
           MOVE "xyz" TO WS-V (3).
           MOVE "def" TO WS-V (4).
           MOVE "ghi" TO WS-V (5).
      *> §8.8.4.2 simple relation condition. Occurrence 4 holds 7.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R1=ATEND"
               WHEN WS-K(IX) = 7
                   SET WS-N TO IX
                   DISPLAY "R1=" WS-N
           END-SEARCH.
      *> §8.8.4.4 simple class condition. §8.8.4.4.4 3)b)2: ALPHABETIC
      *> is true only for letters and space, so "abc" IS alphabetic and
      *> "a1c" is not — NOT ALPHABETIC first holds at occurrence 2.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R2=ATEND"
               WHEN WS-V(IX) IS NOT ALPHABETIC
                   SET WS-N TO IX
                   DISPLAY "R2=" WS-N
           END-SEARCH.
      *> §8.8.4.5 simple condition-name condition. Occurrence 4.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R3=ATEND"
               WHEN WS-K-IS-SEVEN (IX)
                   SET WS-N TO IX
                   DISPLAY "R3=" WS-N
           END-SEARCH.
      *> §8.8.4.6 simple switch-status condition, switch OFF. §8.8.4.6.3
      *> makes it false at every occurrence, so the scan runs off the
      *> end (GR4) and the AT END phrase is taken (GR1b1).
           SET SW1 TO OFF.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R4=ATEND"
               WHEN SW1-ON
                   SET WS-N TO IX
                   DISPLAY "R4=" WS-N
           END-SEARCH.
      *> §8.8.4.6 again with the switch ON: true on the first
      *> evaluation, so the scan stops where it started, occurrence 1.
           SET SW1 TO ON.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R5=ATEND"
               WHEN SW1-ON
                   SET WS-N TO IX
                   DISPLAY "R5=" WS-N
           END-SEARCH.
      *> §8.8.4.7 simple sign condition (§8.8.4.7.4 1)b): true when the
      *> value is less than zero). Occurrence 3 holds -3.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R6=ATEND"
               WHEN WS-K(IX) IS NEGATIVE
                   SET WS-N TO IX
                   DISPLAY "R6=" WS-N
           END-SEARCH.
      *> §8.8.4.10 complex negated condition. NOT (K < 7) is first true
      *> where K is 7 — occurrence 4.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R7=ATEND"
               WHEN NOT (WS-K(IX) < 7)
                   SET WS-N TO IX
                   DISPLAY "R7=" WS-N
           END-SEARCH.
      *> §8.8.4.11 complex combined condition with explicit parentheses.
      *> Occurrence 4 is the only one with K > 0 and V = "def".
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R8=ATEND"
               WHEN (WS-K(IX) > 0) AND (WS-V(IX) = "def")
                   SET WS-N TO IX
                   DISPLAY "R8=" WS-N
           END-SEARCH.
      *> §8.8.4.11.3 precedence: "the order of precedence of logical
      *> operators is 'NOT', 'AND', 'EXCLUSIVE-OR' or 'XOR', 'OR'", so
      *> this reads (K = 2) OR ((K = 9) AND (V = "zzz")) and occurrence
      *> 2 matches. Under the wrong grouping,
      *> ((K = 2) OR (K = 9)) AND (V = "zzz"), nothing matches and the
      *> AT END phrase would be taken instead — the legs differ.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R9=ATEND"
               WHEN WS-K(IX) = 2 OR WS-K(IX) = 9 AND WS-V(IX) = "zzz"
                   SET WS-N TO IX
                   DISPLAY "R9=" WS-N
           END-SEARCH.
      *> §8.8.4.12 abbreviated combined relation condition, subject AND
      *> relational operator omitted (§8.8.4.12.1 item 2). §8.8.4.12.4:
      *> the last stated subject and operator are inserted, so
      *> "= 9 OR 7" is (K = 9) OR (K = 7), first true at occurrence 4.
      *> The implied insertion is load-bearing here: a compiler that
      *> DROPPED the abbreviated tail would evaluate (K = 9) alone and
      *> stop at occurrence 5, so this leg can fail.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R10=ATEND"
               WHEN WS-K(IX) = 9 OR 7
                   SET WS-N TO IX
                   DISPLAY "R10=" WS-N
           END-SEARCH.
      *> §8.8.4.12 again with the subject alone omitted (item 1):
      *> "> 0 AND > 8" is (K > 0) AND (K > 8), first true at occurrence
      *> 5, the only one holding a value above 8. A compiler that
      *> DROPPED the abbreviated tail would evaluate (K > 0) alone and
      *> stop at occurrence 1, so this leg can fail too.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "R11=ATEND"
               WHEN WS-K(IX) > 0 AND > 8
                   SET WS-N TO IX
                   DISPLAY "R11=" WS-N
           END-SEARCH.
           STOP RUN.
