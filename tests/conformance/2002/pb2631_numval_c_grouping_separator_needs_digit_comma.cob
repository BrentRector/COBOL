      *> ISO 15.68.3 r4d: under DECIMAL-POINT IS COMMA "the character comma
      *> shall be used in argument-1 to represent the decimal separator and the
      *> character period shall be used to represent the grouping separator",
      *> so r4a's "digit [ , digit ] ..." becomes "digit [ . digit ] ..." and
      *> the grouping separator still needs a digit after it (kb/Work PB2631;
      *> the period-mode twin is pb2631_numval_c_grouping_separator_needs_digit).
      *> 15.94.4 r1: 0 when conforming, else the position of the first
      *> character in error (r1b), else LENGTH + 1 (r1c).
      *>   '1.'      ends right after the separator: incomplete       -> 3
      *>   '1..2'    the second '.' (position 3) is in error          -> 3
      *>   '1.,5'    the ',' (position 3) is in error                 -> 3
      *>   '1.2.'    ends right after the second separator            -> 5
      *>   '1.234,5' conforming                                       -> 0
      *>   '1,5'     conforming (a decimal fraction)                  -> 0
      *>   '1,5.2'   a grouping separator after the decimal separator -> 4
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2631COM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X(8) VALUE SPACES.
       01 W-CUR PIC X VALUE "$".
       01 W-T PIC 9(3) VALUE 0.
       01 W-R PIC S9(5)V9 VALUE 99999.
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
           MOVE "1." TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "1-SEP-END=" W-T.
           MOVE "1..2" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "2-DOUBLED=" W-T.
           MOVE "1.,5" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "3-BEFORE-DEC=" W-T.
           MOVE "1.2." TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "4-SECOND-END=" W-T.
           MOVE "1.234,5" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "5-OK-GROUPED=" W-T.
           MOVE "1,5" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "6-OK-FRACTION=" W-T.
           MOVE "1,5.2" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "7-GROUP-AFTER-DEC=" W-T.
           MOVE "1.,5" TO W-A.
           COMPUTE W-R = FUNCTION NUMVAL-C(W-A, W-CUR).
           DISPLAY "8-VALUE-BEFORE-DEC=" W-R.
           MOVE "1..2" TO W-A.
           COMPUTE W-R = FUNCTION NUMVAL-C(W-A, W-CUR).
           DISPLAY "9-VALUE-DOUBLED=" W-R.
           MOVE "1.2.3,5" TO W-A.
           COMPUTE W-R = FUNCTION NUMVAL-C(W-A, W-CUR).
           DISPLAY "10-VALUE-OK=" W-R.
           STOP RUN.
