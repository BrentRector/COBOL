*>--- conformance-case
*> id: l1_year_to_yyyy_defaults_and_sum_window
*> rules: [AR-15.100.3-3, AR-15.100.3-6, RV-15.100.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.100.3 r3, §15.100.3 r6 and §15.100.4 r1 — YEAR-TO-YYYY's omitted-argument-2 default, the
      *> window its two arguments' SUM has to fall in, and the maximum-year that sum IS.
      *>   r3  "If argument-2 is omitted, the function shall be evaluated as though 50 were specified for
      *>       argument-2."
      *>   r6  "The sum of the values of argument-2 and argument-3 shall be less than 10000 and greater
      *>       than 1699."
      *>   §15.100.4 r1  "Maximum-year is calculated as follows: (argument-2 + argument-3)"
      *> (cite.py --check 15.100.3 on r3 and r6, and --check 15.100.4 "Maximum-year is calculated as
      *> follows" -> OK at rules 3, 6 and 1.)
      *>
      *> ⛔ r3 GOVERNS EXACTLY ONE REFERENCE SHAPE. The §15.100.2 general format is
      *> FUNCTION YEAR-TO-YYYY ( argument-1 [ argument-2 [ argument-3 ] ] ) with argument-3 NESTED inside
      *> argument-2's brackets, so an omitted argument-2 implies an omitted argument-3 and the only
      *> reference r3 governs is the ONE-ARGUMENT one. Before this fixture the only witness for r3 called
      *> the C# runtime API directly (CobolDateWindowingTests) — it never compiled a one-argument COBOL
      *> reference, so it could not see the renderer's lane dispatch at all.
      *>
      *> THE SLIDING PROBES EVALUATE THE STANDARD'S OWN EXPRESSIONS AND COMPARE. r5 defines the omitted
      *> argument-3 as FUNCTION NUMVAL (FUNCTION CURRENT-DATE (1:4)) — written out below, so the expected
      *> value is derived at run time from the rules rather than pinned to a calendar year that would flip
      *> this golden every January. maximum-year is r1's (argument-2 + argument-3) with r3's 50, and the
      *> returned value is §15.100.4 r2's two-armed equivalent arithmetic expression, transcribed verbatim:
      *>       r2a  argument-1 + 100 * (FUNCTION INTEGER (maximum-year / 100))     when MOD >= argument-1
      *>       r2b  argument-1 + 100 * (FUNCTION INTEGER (maximum-year / 100) - 1) otherwise
      *> Four argument-1 values — 0, 30, 76, 99 — straddle the r2a/r2b split whatever MOD(maximum-year, 100)
      *> is in the year of the run, so no single wrong default reproduces all four answers. Each probe also
      *> asserts the one-argument form against the TWO-argument form with 50 WRITTEN, which is r3 itself:
      *> that equality fails for every default other than 50, and the derived-expression check is what stops
      *> the pair from agreeing because both are wrong.
      *>
      *> THE r6 PROBES ARE MATCHED PAIRS ONE YEAR APART ON EACH BOUND, ON YEAR-TO-YYYY ITSELF. No fixture
      *> pinned either boundary on any function before this: pb65_date_windowing writes
      *> DAY-TO-YYYYDDD(85365, 9000, 1995), whose sum of 10995 is far outside the window and so cannot tell
      *> a bound at 10000 from one at 20000. Argument-3 is 1650 or 9999 in every probe — both satisfy r4
      *> ("an integer greater than 1600 and less than 10000"), so a raised exception here is attributable to
      *> r6 and to nothing else.
      *>   SUM1700   (30 50 1650): maximum-year 1700, MOD 0 < 30 -> r2b -> 30 + 100*(17-1) = 1630. LEGAL:
      *>             1700 is "greater than 1699".
      *>   SUM1699   (30 49 1650): maximum-year 1699 is NOT greater than 1699 -> EC-ARGUMENT-FUNCTION.
      *>   SUM9999   (30 0 9999): maximum-year 9999, MOD 99 >= 30 -> r2a -> 30 + 100*99 = 9930. LEGAL:
      *>             9999 is "less than 10000".
      *>   SUM10000  (30 1 9999): maximum-year 10000 is NOT less than 10000 -> EC-ARGUMENT-FUNCTION.
      *>   Checking is ON, so §15.3 item 14's exception is the observable rather than its implementor-
      *>   defined result; RESUME AT NEXT STATEMENT abandons the failed COMPUTE, and the receiver still
      *>   holding the previous probe's value corroborates that nothing was stored.
      *>   NOTE1     (98 -15 2008) is §15.100.4 NOTE 1's own second example: maximum-year 1993 — which only
      *>             comes out if r1's sum is SIGNED, with no abs and no clamp — MOD 93 < 98 -> r2b -> 1898.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1Y2YWIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EY   PIC 9(4).
       01 MX   PIC 9(4).
       01 A1   PIC 9(2).
       01 EXPV PIC S9(9).
       01 R    PIC S9(9).
       01 R2   PIC S9(9).
       01 TAG  PIC X(3).
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
           COMPUTE EY = FUNCTION NUMVAL(FUNCTION CURRENT-DATE(1:4))
           COMPUTE MX = 50 + EY
           MOVE "S00" TO TAG
           MOVE 0 TO A1
           PERFORM SLIDE-P
           MOVE "S30" TO TAG
           MOVE 30 TO A1
           PERFORM SLIDE-P
           MOVE "S76" TO TAG
           MOVE 76 TO A1
           PERFORM SLIDE-P
           MOVE "S99" TO TAG
           MOVE 99 TO A1
           PERFORM SLIDE-P
           COMPUTE R = FUNCTION YEAR-TO-YYYY(30 50 1650)
           IF R = 1630
               DISPLAY "SUM1700 OK" ELSE DISPLAY "SUM1700 BAD " R END-IF
           DISPLAY "SUM1699-LOW"
           COMPUTE R = FUNCTION YEAR-TO-YYYY(30 49 1650)
           IF R = 1630
               DISPLAY "  UNTOUCHED" ELSE DISPLAY "  STORED " R END-IF
           COMPUTE R = FUNCTION YEAR-TO-YYYY(30 0 9999)
           IF R = 9930
               DISPLAY "SUM9999 OK" ELSE DISPLAY "SUM9999 BAD " R END-IF
           DISPLAY "SUM10000-HIGH"
           COMPUTE R = FUNCTION YEAR-TO-YYYY(30 1 9999)
           IF R = 9930
               DISPLAY "  UNTOUCHED" ELSE DISPLAY "  STORED " R END-IF
           COMPUTE R = FUNCTION YEAR-TO-YYYY(98 -15 2008)
           IF R = 1898
               DISPLAY "NOTE1 OK" ELSE DISPLAY "NOTE1 BAD " R END-IF
           STOP RUN.
       SLIDE-P.
           IF FUNCTION MOD(MX 100) >= A1
               COMPUTE EXPV = A1 + 100 * FUNCTION INTEGER(MX / 100)
           ELSE
               COMPUTE EXPV = A1 + 100 * (FUNCTION INTEGER(MX / 100) - 1)
           END-IF
           COMPUTE R = FUNCTION YEAR-TO-YYYY(A1)
           COMPUTE R2 = FUNCTION YEAR-TO-YYYY(A1 50)
           IF R = EXPV AND R2 = EXPV
               DISPLAY TAG " OK"
           ELSE
               DISPLAY TAG " BAD " R " " R2 " " EXPV
           END-IF.
       END PROGRAM L1Y2YWIN.
