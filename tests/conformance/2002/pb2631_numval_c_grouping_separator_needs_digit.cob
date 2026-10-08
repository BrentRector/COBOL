      *> ISO 15.68.3 r4a: argument-1 of NUMVAL-C has the format
      *> "digit [ , digit ] ... [ . [ digit ] ] | . digit", where "digit is a
      *> string of one or more of the digits 0 through 9" - so a grouping
      *> separator is admitted only as ", digit". A separator no digit follows
      *> ('1,'), a doubled one ('1,,2'), one ahead of the decimal separator
      *> ('1,.5') and one ahead of a space ('1, 2') are not conforming.
      *> 15.94.4 r1: TEST-NUMVAL-C returns 0 for a conforming argument,
      *> otherwise the position of the first character in error (r1b), or
      *> LENGTH + 1 when no specific character is in error (r1c). 15.3 item 14:
      *> NUMVAL-C of a non-conforming argument sets EC-ARGUMENT-FUNCTION.
      *>
      *> kb/Work PB2631: the one scan (NvScan) skipped a separator without
      *> requiring the digit, so TEST-NUMVAL-C('1,.5') returned 0 and
      *> NUMVAL-C('1,.5') returned 1.5 and NUMVAL-C('1,,2') returned 12, both
      *> without the exception. Expected values below are derived from the
      *> rules, one character position at a time:
      *>   '1,'     ends right after the separator: valid but incomplete -> 3
      *>   '1,,2'   the second ',' (position 3) is the character in error -> 3
      *>   '1,.5'   the '.' (position 3) is the character in error        -> 3
      *>   '1, 2'   the space (position 3) is the character in error      -> 3
      *>   ',1'     no digit precedes the separator, position 1           -> 1
      *>   '1,2,'   ends right after the second separator                 -> 5
      *>   '1,2,3'  conforming (groups of any length)                     -> 0
      *>   '1,2.5'  conforming                                            -> 0
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2631GRP.
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
           MOVE "1," TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "1-COMMA-END=" W-T.
           MOVE "1,,2" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "2-DOUBLED=" W-T.
           MOVE "1,.5" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "3-BEFORE-DEC=" W-T.
           MOVE "1, 2" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "4-BEFORE-SPACE=" W-T.
           MOVE ",1" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "5-LEADING=" W-T.
           MOVE "1,2," TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "6-SECOND-END=" W-T.
           MOVE "1,2,3" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "7-OK-GROUPS=" W-T.
           MOVE "$1,2.5" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "8-OK-CURRENCY=" W-T.
           MOVE "1,5-" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "9-OK-TRAIL-SIGN=" W-T.
           MOVE "1,-" TO W-A.
           COMPUTE W-T = FUNCTION TEST-NUMVAL-C(W-A, W-CUR).
           DISPLAY "10-SEP-THEN-SIGN=" W-T.
      *> The value twin rides the same scan: the non-conforming forms raise
      *> EC-ARGUMENT-FUNCTION, so the declarative runs and the statement is
      *> abandoned (W-R keeps 99999.0).
           MOVE "1,.5" TO W-A.
           COMPUTE W-R = FUNCTION NUMVAL-C(W-A, W-CUR).
           DISPLAY "11-VALUE-BEFORE-DEC=" W-R.
           MOVE "1,,2" TO W-A.
           COMPUTE W-R = FUNCTION NUMVAL-C(W-A, W-CUR).
           DISPLAY "12-VALUE-DOUBLED=" W-R.
           MOVE "1,2,3.5" TO W-A.
           COMPUTE W-R = FUNCTION NUMVAL-C(W-A, W-CUR).
           DISPLAY "13-VALUE-OK=" W-R.
           STOP RUN.
