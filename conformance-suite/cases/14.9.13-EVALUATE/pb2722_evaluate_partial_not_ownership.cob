*>--- conformance-case
*> id: pb2722_evaluate_partial_not_ownership
*> rules: [GR-14.9.13.4-4, FMT-14.9.13.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2722 - the 2002 legs of conformance-suite/cases/14.9.13-EVALUATE/pb2722_evaluate_object_not_ownership: a NOT that
      *> begins partial-expression-1 is THAT expression's own, and NOT boolean-expression-2 is the bracketed NOT
      *> ISO 1989:2023 14.9.13.2 prints ("The bracketed `NOT` applies only to the first five alternatives, exactly as
      *> stacked"). 14.9.13.3 SR5: "A selection object is a partial-expression if the leftmost portion of the
      *> selection object is a relational operator, a class condition without the identifier, a sign condition
      *> without the identifier" - `NOT >` is a relational operator (8.7.5), `NOT NUMERIC` and `NOT POSITIVE` are
      *> the class / sign condition's own IS [NOT]. SR8 then reads the object as the condition that results from
      *> preceding it by the subject, and 8.8.4.12.4 GR1 fills in each abbreviated relation's subject.
      *> The grammar used to hoist ONE optional NOT over every object, so it negated the WHOLE partial expression:
      *> P1 and P5 selected the wrong arm.
      *>   cite.py --check 14.9.13.3 "A selection object is a partial-expression if the leftmost portion of the
      *>     selection object is a relational operator, a class condition without the identifier" -> OK 5)
      *> EXPECTED VALUES (X = 3, T = "ABC", N = -2; BA = 1100, BC = 1010, BX = 1000), from the spec:
      *>   P1 WHEN NOT > 5 AND < 2   X NOT > 5 AND X < 2 = true AND false   -> P1 OTHER (the hoist gave P1 MATCH)
      *>   P2 WHEN NOT > 5           X NOT > 5 = true                       -> P2 MATCH
      *>   P3 WHEN NOT NUMERIC       "ABC" IS NOT NUMERIC = true            -> P3 MATCH
      *>   P4 WHEN NOT POSITIVE      -2 IS NOT POSITIVE = true              -> P4 MATCH
      *>   P5 WHEN NOT < 5 OR = 3    X NOT < 5 OR X = 3 = false OR true     -> P5 MATCH (the hoist gave P5 OTHER)
      *>   B1 WHEN NOT BA B-AND BC   GR4 a) 6. BX NOT = 1000 = false        -> B1 OTHER
      *>   B2 WHEN NOT BA B-OR BC    GR4 a) 6. BX NOT = 1110 = true         -> B2 MATCH
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2722PN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 3.
       01 T PIC X(3) VALUE "ABC".
       01 N PIC S9 VALUE -2.
       01 BA PIC 1(4) VALUE B"1100".
       01 BC PIC 1(4) VALUE B"1010".
       01 BX PIC 1(4) VALUE B"1000".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE X
              WHEN NOT > 5 AND < 2 DISPLAY "P1 MATCH"
              WHEN OTHER DISPLAY "P1 OTHER"
           END-EVALUATE
           EVALUATE X
              WHEN NOT > 5 DISPLAY "P2 MATCH"
              WHEN OTHER DISPLAY "P2 OTHER"
           END-EVALUATE
           EVALUATE T
              WHEN NOT NUMERIC DISPLAY "P3 MATCH"
              WHEN OTHER DISPLAY "P3 OTHER"
           END-EVALUATE
           EVALUATE N
              WHEN NOT POSITIVE DISPLAY "P4 MATCH"
              WHEN OTHER DISPLAY "P4 OTHER"
           END-EVALUATE
           EVALUATE X
              WHEN NOT < 5 OR = 3 DISPLAY "P5 MATCH"
              WHEN OTHER DISPLAY "P5 OTHER"
           END-EVALUATE
           EVALUATE BX
              WHEN NOT BA B-AND BC DISPLAY "B1 MATCH"
              WHEN OTHER DISPLAY "B1 OTHER"
           END-EVALUATE
           EVALUATE BX
              WHEN NOT BA B-OR BC DISPLAY "B2 MATCH"
              WHEN OTHER DISPLAY "B2 OTHER"
           END-EVALUATE
           STOP RUN.
