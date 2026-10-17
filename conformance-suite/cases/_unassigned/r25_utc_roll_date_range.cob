*>--- conformance-case
*> id: r25_utc_roll_date_range
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R25 - the 15.40.4 r2 UTC roll vs the integer-date-form range. Every argument is
      *> individually legal (the 15.40.3 r4/r5 screens pass), and the r2 adjustment then carries the
      *> DATE outside 1..3,067,671 (15.5.2). Before the fix the high end threw a raw CLR
      *> ArgumentOutOfRangeException out of Epoch.AddDays and the low end emitted year 1600 - and
      *> 15.3.1.3 requires the year "greater than 1600". 15.3 permits only EC-ARGUMENT-FUNCTION or
      *> the default; the roll now re-checks the day and raises. Time-only formats roll freely.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R25ROLL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R PIC X(16).
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
      *> The crash shape: max integer date + one westward day-roll. CAUGHT is the assertion.
           DISPLAY "1-ROLL-PAST-MAX".
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ"
               3067671 86399 -1439) TO W-R.
      *> The low-end mirror: day 1 + one eastward day-roll emitted year 1600 before the fix.
           DISPLAY "2-ROLL-BELOW-ONE".
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ"
               1 0 1439) TO W-R.
      *> One day inside each end: the same offsets roll INTO range and must keep emitting. No EC.
           DISPLAY "3-LEGAL-ROLL-FORWARD".
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ"
               3067670 86399 -1439) TO W-R.
           DISPLAY "  " W-R.
           DISPLAY "4-LEGAL-ROLL-BACKWARD".
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ"
               2 0 1439) TO W-R.
           DISPLAY "  " W-R.
      *> The boundary WITHOUT a roll: argument-4 omitted with a UTC format is 0 (15.40.3 r7). No EC.
           DISPLAY "5-MAX-NO-ROLL".
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ"
               3067671 0) TO W-R.
           DISPLAY "  " W-R.
           STOP RUN.
