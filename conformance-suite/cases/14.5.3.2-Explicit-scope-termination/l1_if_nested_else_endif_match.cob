      *>--- conformance-case
      *> id: l1_if_nested_else_endif_match
      *> rules: [GR-14.5.3.2-1, GR-14.5.3.3-2, SR-14.9.19.3-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.19.3 SR2 a) — "any ELSE encountered is matched with
      *> the nearest preceding IF that either has not been already
      *> matched with an ELSE or has not been implicitly or explicitly
      *> terminated" — and SR2 b) — "any END-IF encountered is matched
      *> with the nearest preceding IF that has not been implicitly or
      *> explicitly terminated" — plus the NOTE "A nested IF statement
      *> is terminated by terminal separator period of the containing
      *> IF statement".
      *> Each shape below is chosen so the TWO candidate matchings give
      *> DIFFERENT output; that is what makes a golden decide the rule
      *> instead of merely running through it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IFN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9 VALUE 0.
       01 W-B PIC 9 VALUE 0.
       01 W-C PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
      *> S1 — the dangling ELSE. No END-IF at either level; the two IFs
      *> are terminated by the sentence's period (the NOTE). Outer TRUE,
      *> inner FALSE. SR2 a) binds the ELSE to the INNER IF, so the
      *> inner else-arm runs. Bound to the outer IF (true) instead,
      *> NOTHING would print, so the two readings are distinguishable.
           MOVE 1 TO W-A.
           MOVE 0 TO W-B.
           IF W-A = 1
               IF W-B = 1
                   DISPLAY "S1-INNER-THEN"
               ELSE
                   DISPLAY "S1-INNER-ELSE".
      *> S2 — the inner IF is EXPLICITLY TERMINATED by an END-IF before
      *> the ELSE appears. SR2 b) matched that END-IF to the inner IF;
      *> SR2 a) may then no longer match the ELSE to it, so the ELSE
      *> binds to the OUTER IF, which is TRUE: the outer then-arm tail
      *> runs and the outer else-arm does not.
           MOVE 1 TO W-A.
           MOVE 0 TO W-B.
           IF W-A = 1
               IF W-B = 1
                   DISPLAY "S2-INNER-THEN"
               END-IF
               DISPLAY "S2-OUTER-TAIL"
           ELSE
               DISPLAY "S2-OUTER-ELSE"
           END-IF.
      *> S3 — the "has not been ALREADY MATCHED with an ELSE" half of
      *> SR2 a), which S2 alone does not reach: the inner IF has taken
      *> an ELSE and has been terminated, so the SECOND ELSE binds to
      *> the outer IF. The outer condition is FALSE, so only the outer
      *> else-arm runs and the inner IF is never entered.
           MOVE 0 TO W-A.
           MOVE 1 TO W-B.
           IF W-A = 1
               IF W-B = 1
                   DISPLAY "S3-IN-T"
               ELSE
                   DISPLAY "S3-IN-E"
               END-IF
           ELSE
               DISPLAY "S3-OUT-E"
           END-IF.
      *> S4 — three levels, each with its own ELSE and END-IF: every
      *> ELSE and every END-IF must bind to its own level. L3 is FALSE
      *> (its else-arm runs), L2 and L1 are TRUE (theirs must not).
           MOVE 1 TO W-A.
           MOVE 1 TO W-B.
           MOVE 0 TO W-C.
           IF W-A = 1
               IF W-B = 1
                   IF W-C = 1
                       DISPLAY "S4-L3-T"
                   ELSE
                       DISPLAY "S4-L3-E"
                   END-IF
                   DISPLAY "S4-L2-TAIL"
               ELSE
                   DISPLAY "S4-L2-E"
               END-IF
           ELSE
               DISPLAY "S4-L1-E"
           END-IF.
           DISPLAY "DONE".
           STOP RUN.
