      *> kb/Work PB1412 - the boolean-expression forms of an EVALUATE selection subject and selection object.
      *> ISO 1989:2023 14.9.13.2 prints boolean-expression-1 as a selection subject and [ NOT ] boolean-expression-2 as a
      *> selection object; 14.9.13.3 SR10 Table 15 has a Boolean-expression column and row; 14.9.13.4 GR3 d) "Any
      *> selection subject in which boolean-expression-1 is specified is assigned a boolean value according to the
      *> rules for evaluating boolean expressions" and GR4 a) 6. lowers a boolean-expression-2 pair to
      *> "selection-subject [NOT] = selection-object". The boolean operators are a COBOL-2002 introduction, so the
      *> introducing edition of this program is 2002 (the shift operators, a 2023 introduction, are in the 2023 golden
      *> pb1412_evaluate_boolean_expression_subject).
      *>   cite.py --check 14.9.13.4 "Any selection subject in which boolean-expression-1 is specified is assigned a
      *>     boolean value according to the rules for evaluating boolean expressions." -> OK 3) d)
      *>   cite.py --check 14.9.13.4 "If the selection object is identifier-2, literal-2, arithmetic-expression-2, or
      *>     boolean-expression-2, the pair is considered to be a conditional expression of the following form" -> OK 4) a) 6.
      *>   cite.py --check 14.9.13.3 "If the selection subject is other than TRUE or FALSE and the selection object is a
      *>     boolean expression that results in one boolean character, the selection object is treated as a boolean
      *>     expression and therefore boolean-expression-2." -> OK 6) c)
      *> A = 1100, C = 1010, E = 1000, F1 = 1, F0 = 0. A B-AND C = 1000, A B-OR C = 1110, A B-XOR C = 0110, B-NOT A = 0011.
      *>   O1  E = A B-AND C (1000)                      -> match      -> O1-Y
      *>   O2  E = NOT (A B-OR C) i.e. E <> 1110         -> match      -> O2-Y
      *>   B   A B-AND C against A B-OR C (1110) then A B-AND C (1000) -> second arm -> B2-Y
      *>   C   A B-AND C against 0000, 1111, 1000        -> third arm  -> C3-Y
      *>   D1  ALSO: 0110 = B"0110", F1 = B"1", TRUE = (F1 = B"1")     -> D1-Y
      *>   D2  ALSO: F1 = B"1", B-NOT A = B"0011"        -> D2-Y
      *>   E   SR6 a): TRUE subject, one-character object F1 B-AND F0 (0) is condition-2, false; F1 B-OR F0 (1) true
      *>         -> E2-Y
      *>   F   SR6 b): one-character subject F1 B-AND F0 (0) against TRUE / FALSE objects -> F2-F
      *>   G   SR6 b): F1 B-OR F0 (1) -> G1-T
      *>   H   SR6 d): one-character subject F1 B-OR F0 (1) against boolean literals B"0", B"1" -> H2-1
      *>   I   partial-expression "= B"1000"" spliced onto the subject A B-AND C (1000)         -> I1-Y
      *>   J   ANY pairs with a boolean-expression subject                                        -> J1-Y
      *>   K   a parenthesized boolean literal as subject and object                              -> K1-Y
      *>   L   a bare subject beside a later operator-bearing one                                 -> L1-Y
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1412EVO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 1(4) VALUE B"1100".
       01 C PIC 1(4) VALUE B"1010".
       01 E PIC 1(4) VALUE B"1000".
       01 F1 PIC 1 VALUE B"1".
       01 F0 PIC 1 VALUE B"0".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE E
              WHEN A B-AND C DISPLAY "O1-Y"
              WHEN OTHER DISPLAY "O1-N"
           END-EVALUATE.
           EVALUATE E
              WHEN NOT A B-OR C DISPLAY "O2-Y"
              WHEN OTHER DISPLAY "O2-N"
           END-EVALUATE.
           EVALUATE A B-AND C
              WHEN A B-OR C DISPLAY "B1-Y"
              WHEN A B-AND C DISPLAY "B2-Y"
              WHEN OTHER DISPLAY "B3-O"
           END-EVALUATE.
           EVALUATE A B-AND C
              WHEN B"0000" DISPLAY "C1-Y"
              WHEN B"1111" DISPLAY "C2-Y"
              WHEN B"1000" DISPLAY "C3-Y"
              WHEN OTHER DISPLAY "C4-O"
           END-EVALUATE.
           EVALUATE A B-XOR C ALSO F1 ALSO TRUE
              WHEN B"0110" ALSO B"1" ALSO F1 = B"1" DISPLAY "D1-Y"
              WHEN OTHER DISPLAY "D1-O"
           END-EVALUATE.
           EVALUATE F1 ALSO B-NOT A
              WHEN B"1" ALSO B"0011" DISPLAY "D2-Y"
              WHEN OTHER DISPLAY "D2-O"
           END-EVALUATE.
           EVALUATE TRUE
              WHEN F1 B-AND F0 DISPLAY "E1-Y"
              WHEN F1 B-OR F0 DISPLAY "E2-Y"
              WHEN OTHER DISPLAY "E3-O"
           END-EVALUATE.
           EVALUATE F1 B-AND F0
              WHEN TRUE DISPLAY "F1-T"
              WHEN FALSE DISPLAY "F2-F"
           END-EVALUATE.
           EVALUATE F1 B-OR F0
              WHEN TRUE DISPLAY "G1-T"
              WHEN FALSE DISPLAY "G2-F"
           END-EVALUATE.
           EVALUATE F1 B-OR F0
              WHEN B"0" DISPLAY "H1-0"
              WHEN B"1" DISPLAY "H2-1"
           END-EVALUATE.
           EVALUATE A B-AND C
              WHEN = B"1000" DISPLAY "I1-Y"
              WHEN OTHER DISPLAY "I1-O"
           END-EVALUATE.
           EVALUATE A B-AND C ALSO A
              WHEN ANY ALSO ANY DISPLAY "J1-Y"
           END-EVALUATE.
           EVALUATE (B"1")
              WHEN (B"1") DISPLAY "K1-Y"
              WHEN OTHER DISPLAY "K1-O"
           END-EVALUATE.
           EVALUATE A ALSO A B-AND C
              WHEN B"1100" ALSO B"1000" DISPLAY "L1-Y"
              WHEN OTHER DISPLAY "L1-O"
           END-EVALUATE.
           STOP RUN.
