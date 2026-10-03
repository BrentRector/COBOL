      *> kb/Work PB1391, the positive side. ISO 1989:2023 8.8.4.12.3 SR1 forbids abbreviating AFTER a boolean
      *> relation; it does not make a boolean relation illegal, and a boolean item written as the next simple
      *> condition is a SIMPLE BOOLEAN CONDITION (8.8.4.3.4 GR1: true if the result of the expression is 1), not an
      *> abbreviated object. B1 = 0, B2 = 1, B3 = 1, each one boolean position (8.8.4.3.3 SR1).
      *>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
      *>   cite.py --check 8.8.4.3.4 "Boolean-expression-1 evaluates true if the result of the expression is 1"
      *>     -> OK 1)
      *>   B1 = B2 OR B1 <> B3   both relations stated in full: false OR true               -> T1
      *>   B1 = B2 AND B3        false AND (B3 is true)                                      -> F2
      *>   B1 = B1 AND B2        true AND (B2 as a condition = true); the abbreviation
      *>                         reading B1 = B2 would be false                              -> T3
      *>   B1 = B1 AND B1        true AND (B1 as a condition = false); the abbreviation
      *>                         reading B1 = B1 would be true                               -> F4
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1391POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"0".
       01 B2 PIC 1 VALUE B"1".
       01 B3 PIC 1 VALUE B"1".
       PROCEDURE DIVISION.
       MAIN.
           IF B1 = B2 OR B1 <> B3 DISPLAY "T1" ELSE DISPLAY "F1".
           IF B1 = B2 AND B3 DISPLAY "T2" ELSE DISPLAY "F2".
           IF B1 = B1 AND B2 DISPLAY "T3" ELSE DISPLAY "F3".
           IF B1 = B1 AND B1 DISPLAY "T4" ELSE DISPLAY "F4".
           STOP RUN.
