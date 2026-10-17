      *>--- conformance-case
      *> id: pb2722_evaluate_object_not_ownership
      *> rules: [GR-14.9.13.4-4, FMT-14.9.13.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2722 - WHOSE NOT IS IT? ISO 1989:2023 14.9.13.2 prints the bracketed NOT on five selection-object
      *> alternatives only; the figure note: "The bracketed `NOT` applies only to the first five alternatives, exactly
      *> as stacked" (identifier-2, literal-2, arithmetic-expression-2, boolean-expression-2, range-expression).
      *> condition-2 owns its own leading NOT - a negated condition, 8.8.4.10.1 "A condition is negated by use of the
      *> logical operator 'NOT'" - and 8.8.4.11.3 binds it tighter than AND / OR: "The order of precedence of
      *> logical operators is 'NOT', 'AND', 'EXCLUSIVE-OR' or 'XOR', 'OR'".
      *> The grammar used to hoist ONE optional NOT over every object, so a condition-2's leading NOT negated the
      *> WHOLE condition: L5, L7 and L9 selected the wrong arm. The other legs pin the five-alternative NOT.
      *>   cite.py --check 14.9.13.4 "selection-subject [NOT] = selection-object" -> OK 4) a) 6.
      *>   cite.py --check 8.8.4.11.3 "The order of precedence of logical operators is 'NOT', 'AND'" -> OK
      *> EXPECTED VALUES (X = 3, A = 1, B = 2; X-THREE is X's level-88 for 3), from the spec, never from a run:
      *>   L1 WHEN NOT 3          GR4 a) 6. X NOT = 3            false -> L1 OTHER
      *>   L2 WHEN NOT A          GR4 a) 6. X NOT = A (3 vs 1)   true  -> L2 NOT-A
      *>   L3 WHEN NOT A + B      GR4 a) 6. X NOT = 3            false -> L3 OTHER
      *>   L4 WHEN NOT 1 THRU 2   GR4 a) 5. X < 1 OR X > 2       true  -> L4 NOT-RANGE
      *>   L5 TRUE / WHEN NOT A > B AND A > B   (NOT 1>2) AND 1>2 = true AND false = false
      *>                                                         -> L5 OTHER   (the hoist gave L5 MATCH)
      *>   L6 TRUE / WHEN NOT A > B             NOT 1>2 = true   -> L6 MATCH
      *>   L7 FALSE / WHEN NOT A > B OR A < B   (NOT 1>2) OR 1<2 = true; a FALSE subject matches only a false
      *>      condition (GR4 a) 3.)                              -> L7 OTHER   (the hoist gave L7 MATCH)
      *>   L8 TRUE / WHEN NOT X-THREE           NOT true = false -> L8 OTHER
      *>   L9 X ALSO TRUE / WHEN NOT 4 ALSO NOT A < B AND A > B
      *>      X NOT = 4 is true; (NOT 1<2) AND 1>2 = false AND false = false
      *>                                                         -> L9 OTHER   (the hoist gave L9 MATCH)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2722NO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 3.
          88 X-THREE VALUE 3.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE X
              WHEN NOT 3 DISPLAY "L1 NOT-3"
              WHEN OTHER DISPLAY "L1 OTHER"
           END-EVALUATE
           EVALUATE X
              WHEN NOT A DISPLAY "L2 NOT-A"
              WHEN OTHER DISPLAY "L2 OTHER"
           END-EVALUATE
           EVALUATE X
              WHEN NOT A + B DISPLAY "L3 NOT-SUM"
              WHEN OTHER DISPLAY "L3 OTHER"
           END-EVALUATE
           EVALUATE X
              WHEN NOT 1 THRU 2 DISPLAY "L4 NOT-RANGE"
              WHEN OTHER DISPLAY "L4 OTHER"
           END-EVALUATE
           EVALUATE TRUE
              WHEN NOT A > B AND A > B DISPLAY "L5 MATCH"
              WHEN OTHER DISPLAY "L5 OTHER"
           END-EVALUATE
           EVALUATE TRUE
              WHEN NOT A > B DISPLAY "L6 MATCH"
              WHEN OTHER DISPLAY "L6 OTHER"
           END-EVALUATE
           EVALUATE FALSE
              WHEN NOT A > B OR A < B DISPLAY "L7 MATCH"
              WHEN OTHER DISPLAY "L7 OTHER"
           END-EVALUATE
           EVALUATE TRUE
              WHEN NOT X-THREE DISPLAY "L8 MATCH"
              WHEN OTHER DISPLAY "L8 OTHER"
           END-EVALUATE
           EVALUATE X ALSO TRUE
              WHEN NOT 4 ALSO NOT A < B AND A > B DISPLAY "L9 MATCH"
              WHEN OTHER DISPLAY "L9 OTHER"
           END-EVALUATE
           STOP RUN.
