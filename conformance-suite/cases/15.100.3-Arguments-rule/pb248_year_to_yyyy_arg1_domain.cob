*>--- conformance-case
*> id: pb248_year_to_yyyy_arg1_domain
*> rules: [AR-15.100.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.100.3 r1 - YEAR-TO-YYYY argument-1's VALUE domain, both arms. The row AR-15.100.3-1 was held
      *> at PARTIAL by two things: the integer-ness half (kb/Work PB248 - a floating-point argument-1 passed
      *> the 15.3 type-6 screen and was truncated) and the absence of any fixture pinning r1's REJECT edge.
      *> PB248 closed the first; this closes the second, so the row's evidence is complete on both halves.
      *>
      *> THE RULE, --check validated:
      *>   cite.py --check 15.100.3 "Argument-1 shall be a nonnegative integer that is less than 100"
      *>     -> OK 15.100.3 1). The domain is the closed interval [0, 99]: 0 is admitted ("nonnegative"),
      *>     99 is admitted ("less than 100"), 100 and every negative value are not.
      *>   cite.py --check 15.3 "the EC-ARGUMENT-FUNCTION exception condition is set to exist"
      *>     -> OK 15.3 14). Checking is TURNED ON because 15.3's closing sentence makes the RESULT
      *>     implementor-defined while checking is off - with it off this rule has no spec-fixed
      *>     observable at all, and a green test would prove nothing.
      *>
      *> THE ACCEPTED VALUES, DERIVED FROM 15.100.4 (not observed). With argument-2 = 50 and argument-3 =
      *> 2000, maximum-year = argument-2 + argument-3 = 2050 (r1), and FUNCTION MOD(2050, 100) = 50.
      *>   argument-1 = 0  : 50 >= 0  is TRUE  -> r2a: 0  + 100 * INTEGER(2050/100) = 0  + 100*20 = 2000
      *>   argument-1 = 99 : 50 >= 99 is FALSE -> r2b: 99 + 100 * (INTEGER(2050/100) - 1) = 99 + 1900 = 1999
      *>   argument-1 = 50 : 50 >= 50 is TRUE  -> r2a: 50 + 100*20 = 2050
      *> argument-3 is written explicitly so the window is FIXED and the expected values do not depend on
      *> the year at the time of execution (15.100.3 r5 / NOTE 2 - an omitted argument-3 slides).
      *>
      *> BOTH ARMS, AND BOTH ENDPOINTS. A closed-interval rule is falsified from two directions: by raising
      *> on 0 or 99 (over-rejecting legal source) and by staying silent at 100 or below zero. Lines 1-3 are
      *> the ACCEPT arm and 4-5 the REJECT arm; line 5 comes from a runtime data item so no constant fold
      *> can see the violation.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB248Y2YDOMAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> W-R is UNSIGNED so the accepted values display as plain digits; every value r2a/r2b can
      *> produce here is a positive four-digit year. W-A is signed - it carries the negative REJECT probe.
       01 W-R PIC 9(4) VALUE 0.
       01 W-A PIC S9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  RAISED".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> 1 - the LOWER endpoint, which r1 admits ("nonnegative").
           DISPLAY "1-AT-ZERO".
           COMPUTE W-R = FUNCTION YEAR-TO-YYYY(0, 50, 2000).
           DISPLAY "  V=" W-R.
      *> 2 - the UPPER endpoint, which r1 admits ("less than 100").
           DISPLAY "2-AT-99".
           COMPUTE W-R = FUNCTION YEAR-TO-YYYY(99, 50, 2000).
           DISPLAY "  V=" W-R.
      *> 3 - the interior, on r2a's side of the MOD comparison.
           DISPLAY "3-INTERIOR".
           COMPUTE W-R = FUNCTION YEAR-TO-YYYY(50, 50, 2000).
           DISPLAY "  V=" W-R.
      *> 4 - one above the upper bound, from a literal.
           DISPLAY "4-AT-100".
           COMPUTE W-R = FUNCTION YEAR-TO-YYYY(100, 50, 2000).
      *> 5 - below zero, from a runtime data item.
           DISPLAY "5-NEGATIVE".
           MOVE -1 TO W-A.
           COMPUTE W-R = FUNCTION YEAR-TO-YYYY(W-A, 50, 2000).
           DISPLAY "DONE".
           STOP RUN.
