      *> kb/Work PB1379 - the SDIDI carrier of the floor.  Under ARITHMETIC IS STANDARD-DECIMAL a seconds
      *>   argument written as an arithmetic expression is an SDIDI intermediate, landed at 18 fraction
      *>   digits (15.3.3.2).  7.3.17.4 GR5 (LEAP-SECOND OFF, implied): "a standard numeric time form
      *>   value shall be greater than or equal to zero and less than 86,400".  The landing truncated
      *>   toward zero, so Z - 1.0E-30 (below zero by far less than a unit of the 18th digit) became 0
      *>   and was formatted as midnight; it now lands negative and takes the out-of-range path
      *>   (EC-ARGUMENT-FUNCTION, observed by the declarative; the returned value on a CAUGHT leg is the
      *>   implementor's and is not asserted).  Z is zero, so Z + 3661.5 is 01:01:01.5 and Z + 86399 is
      *>   23:59:59, both in the form (3661 = 1*3600 + 1*60 + 1; 86399 = 23*3600 + 59*60 + 59).
      *>   cite.py --check 7.3.17.4 "a standard numeric time form value shall be greater than or equal
      *>     to zero and less than 86,400" -> OK 7.3.17.4 5)
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1379SDI.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z  PIC 9 VALUE 0.
       01 R  PIC X(40).
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
           DISPLAY "1-BELOW-ZERO-BY-A-TINY-AMOUNT"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" Z - 1.0E-30) TO R
           DISPLAY "2-FRACTIONAL-VALUE-IN-THE-FORM"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" Z + 3661.5) TO R
           DISPLAY "  " R
           DISPLAY "3-LAST-SECOND-OF-THE-DAY"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" Z + 86399) TO R
           DISPLAY "  " R
           DISPLAY "4-86400-IS-OUT-UNDER-OFF"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" Z + 86400) TO R
           STOP RUN.
