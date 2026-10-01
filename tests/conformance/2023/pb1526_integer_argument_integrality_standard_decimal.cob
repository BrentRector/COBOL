      *> kb/Work PB1526 - the SDIDI carrier of the same rule.  Under ARITHMETIC IS STANDARD-DECIMAL an
      *>   arithmetic-expression argument is an SDIDI intermediate (8.8.1.5), and 15.3 type 6 still asks
      *>   whether its VALUE is an integer: "An arithmetic expression that will always result in an
      *>   integer value or an integer data item shall be specified" and a value the argument rules do
      *>   not admit sets EC-ARGUMENT-FUNCTION (15.3's closing paragraph), which the declarative
      *>   observes.  The returned value on a CAUGHT leg is the implementor's and is not asserted.
      *>   Derivations: X = 5: X / 2 + 1 = 3.5 is not an integer; X = 4: 4 / 2 + 1 = 3 is, FACTORIAL(3) = 6
      *>   (15.36.4: 3 x 2 x 1); the BOUNDED (FACTORIAL) and the TOTAL (TEST-DATE-YYYYMMDD) intake both
      *>   ask - X / 2 + 20240228 with X = 5 is 20240230.5 (not an integer), with X = 4 is 20240230, an
      *>   integer that is not a date (day 30 of February: verdict 3).
      *>   cite.py --check 15.3 "An arithmetic expression that will always result in an integer value
      *>     or an integer data item shall be specified" -> OK 15.3 6)
      *>   cite.py --check 15.3 "the EC-ARGUMENT-FUNCTION exception condition is set to exist" -> OK 15.3
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1526SDI.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC 9 VALUE 5.
       01 R  PIC 9(5).
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
           DISPLAY "1-BOUNDED-FRACTION-X5"
           COMPUTE R = FUNCTION FACTORIAL(X / 2 + 1)
           DISPLAY "2-TOTAL-FRACTION-X5"
           COMPUTE R = FUNCTION TEST-DATE-YYYYMMDD(X / 2 + 20240228)
           MOVE 4 TO X
           DISPLAY "3-BOUNDED-INTEGRAL-X4"
           COMPUTE R = FUNCTION FACTORIAL(X / 2 + 1)
           DISPLAY "  R=" R
           DISPLAY "4-TOTAL-INTEGRAL-X4"
           COMPUTE R = FUNCTION TEST-DATE-YYYYMMDD(X / 2 + 20240228)
           DISPLAY "  R=" R
           STOP RUN.
