      *> kb/Work PB1526 - 15.3 type 6: "An arithmetic expression that will always result in an
      *>   integer value or an integer data item shall be specified."  The compile-time screen can only
      *>   refuse an argument that is PROVABLY not always integral (X / 2 + 1 is not provable, so it
      *>   compiles); the VALUE is the rule's real test.  15.3's closing paragraph makes a value the
      *>   function's argument rules do not admit an incorrect value for that argument and sets
      *>   EC-ARGUMENT-FUNCTION, which checking turns into the condition the declarative observes.
      *>   "An integer" is the exact value having no nonzero digit right of the decimal point
      *>   (docs/CONFORMANCE.md DOC-A.1-124) - 3.5 and 2.5 are not integers, 3.0 and 2.0 are.  The
      *>   intake used to truncate the fraction silently: FACTORIAL(X / 2 + 1) with X = 5 answered
      *>   FACTORIAL(3) and raised nothing.
      *>   The returned value on a CAUGHT leg is implementor-defined (15.3's last sentence) and is
      *>   deliberately not asserted.
      *>   Derivations: X = 5: X / 2 + 1 = 3.5 and X * 0.5 = 2.5 are not integers; X = 4: 4 / 2 + 1 =
      *>   3 and 4 * 0.5 = 2 are, FACTORIAL(3) = 6 and FACTORIAL(2) = 2; CHAR(4 / 2 + 64) = CHAR(66),
      *>   the character at ordinal position 66, which is "A" in the ASCII collating sequence.
      *>   cite.py --check 15.3 "An arithmetic expression that will always result in an integer
      *>     value or an integer data item shall be specified" -> OK 15.3 6)
      *>   cite.py --check 15.3 "the EC-ARGUMENT-FUNCTION exception condition is set to exist" -> OK 15.3
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1526IAV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC 9 VALUE 5.
       01 F  COMP-2 VALUE 2.5.
       01 R  PIC 9(5).
       01 C  PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  CAUGHT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "1-QUOTIENT-PLUS-ONE-X5"
           COMPUTE R = FUNCTION FACTORIAL(X / 2 + 1)
           DISPLAY "2-PRODUCT-BY-HALF-X5"
           COMPUTE R = FUNCTION FACTORIAL(X * 0.5)
           DISPLAY "3-CHAR-X5"
           MOVE FUNCTION CHAR(X / 2 + 64) TO C
      *> A binary64 operand and a TOTAL (wide-intake) function ask the same question of the value:
      *> F * 1 = 2.5 is not an integer; TEST-DATE-YYYYMMDD's argument-1 "shall be an integer"
      *> (15.90.3 r1) although its verdicts are total over every integer.
           DISPLAY "3A-COMP-2-EXPRESSION-FRACTION"
           COMPUTE R = FUNCTION FACTORIAL(F * 1)
           DISPLAY "3B-TOTAL-FUNCTION-FRACTION-X5"
           COMPUTE R = FUNCTION TEST-DATE-YYYYMMDD(X / 2 + 20240228)
           MOVE 4 TO X
           MOVE 3.0 TO F
           DISPLAY "4-QUOTIENT-PLUS-ONE-X4"
           COMPUTE R = FUNCTION FACTORIAL(X / 2 + 1)
           DISPLAY "  R=" R
           DISPLAY "5-PRODUCT-BY-HALF-X4"
           COMPUTE R = FUNCTION FACTORIAL(X * 0.5)
           DISPLAY "  R=" R
           DISPLAY "6-CHAR-X4"
           MOVE FUNCTION CHAR(X / 2 + 64) TO C
           DISPLAY "  C=" C
      *> F = 3.0 is an integer (COMP-2 2.0-style exactness): FACTORIAL(3) = 6.  X = 4 makes
      *> X / 2 + 20240228 = 20240230, an integer that is not a date (day 30 of February): the
      *> TEST-DATE verdict for an invalid day is 3 (15.90.4, the day is the third subfield).
           DISPLAY "7-COMP-2-EXPRESSION-INTEGRAL"
           COMPUTE R = FUNCTION FACTORIAL(F * 1)
           DISPLAY "  R=" R
           DISPLAY "8-TOTAL-FUNCTION-INTEGRAL-X4"
           COMPUTE R = FUNCTION TEST-DATE-YYYYMMDD(X / 2 + 20240228)
           DISPLAY "  R=" R
           STOP RUN.
