*>--- conformance-case
*> id: l1-day-to-yyyyddd-arity4
*> rules: [FMT-15.25.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.25.2 general format — FUNCTION DAY-TO-YYYYDDD ( argument-1
      *> [ argument-2 [ argument-3 ] ] ). The nesting of the two brackets closes
      *> the count at three: argument-3 is the innermost optional item and nothing
      *> follows it, so a FOURTH argument is not a form this function has.
      *> Editions 2002 and later only — below 2002 the name itself is rejected
      *> (negative/date_window_below_2002 pins that), which would be a different
      *> diagnostic about a different rule.
      *> The accepting side is 2002/l1_day_to_yyyyddd_format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NEGDTY4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(7).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION DAY-TO-YYYYDDD(85365 10 1900 1)
           STOP RUN.
       END PROGRAM L1NEGDTY4.
