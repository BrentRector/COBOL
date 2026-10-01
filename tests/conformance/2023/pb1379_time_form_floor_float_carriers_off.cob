      *> kb/Work PB1379 (rows GR-7.3.17.4-5, and GR-7.3.17.4-4 in the sibling program) - 7.3.17.4 GR5: with
      *>   LEAP-SECOND OFF (the implied default) "a standard numeric time form value shall be greater
      *>   than or equal to zero and less than 86,400".  The formatted-time family screened the LANDED
      *>   seconds, and the float landing TRUNCATED toward zero first: a COMP-2 / COMP-1 argument of
      *>   -1.0E-10 became 0 and was formatted as midnight with no condition, while the same value in a
      *>   fixed-point item (and COMBINED-DATETIME's own binary64 body) was refused.  15.3 rule: a value
      *>   outside the form is an incorrect argument and sets EC-ARGUMENT-FUNCTION, which the
      *>   declarative observes; the returned value on a CAUGHT leg is the implementor's and is not
      *>   asserted.  Zero is IN the form ("greater than or equal to zero"), as is a fractional value.
      *>   Derivations: 0 seconds = 00:00:00; 3661.5 = 1*3600 + 1*60 + 1.5 = 01:01:01(.5); the format
      *>   "hhmmss" shows whole seconds.
      *>   cite.py --check 7.3.17.4 "a standard numeric time form value shall be greater than or equal
      *>     to zero and less than 86,400" -> OK 7.3.17.4 5)
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1379FLO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SECS2 COMP-2 VALUE -1.0E-10.
       01 SECS1 COMP-1 VALUE -1.0E-10.
       01 NS   PIC S9V9(12) VALUE -0.0000000001.
       01 ZRO  COMP-2 VALUE 0.
       01 FRC  COMP-2 VALUE 3661.5.
       01 D    PIC 9(7) VALUE 143951.
       01 R    PIC X(40).
       01 N    PIC 9(7)V9(5).
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
           DISPLAY "1-FORMATTED-TIME-COMP-2-BELOW-ZERO"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" SECS2) TO R
           DISPLAY "2-FORMATTED-TIME-COMP-1-BELOW-ZERO"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" SECS1) TO R
           DISPLAY "3-FORMATTED-TIME-FIXED-BELOW-ZERO"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" NS) TO R
           DISPLAY "4-FORMATTED-DATETIME-COMP-2-BELOW-ZERO"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D SECS2)
               TO R
           DISPLAY "5-LOCALE-TIME-FROM-SECONDS-COMP-2-BELOW-ZERO"
           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS(SECS2) TO R
           DISPLAY "6-COMBINED-DATETIME-COMP-2-BELOW-ZERO"
           COMPUTE N = FUNCTION COMBINED-DATETIME(1 SECS2)
           DISPLAY "7-ZERO-IS-IN-THE-FORM"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" ZRO) TO R
           DISPLAY "  " R
           DISPLAY "8-A-FRACTIONAL-FLOAT-IS-IN-THE-FORM"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" FRC) TO R
           DISPLAY "  " R
           STOP RUN.
