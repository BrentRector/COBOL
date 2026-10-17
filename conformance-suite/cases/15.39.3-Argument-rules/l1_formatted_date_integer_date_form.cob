*>--- conformance-case
*> id: l1_formatted_date_integer_date_form
*> rules: [AR-15.39.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.39.3 r3 — "Argument-2 shall be a value in integer date form": the FULL interval, both ends and
      *> both directions of the violation, for FORMATTED-DATE.
      *>
      *> §15.5.2 is what "integer date form" MEANS, and it states an interval, not a ceiling: "A value in integer
      *> date form is a positive integer that represents a number of days succeeding December 31, 1600, in the
      *> Gregorian calendar. It shall be greater than zero and shall be less than or equal to the value of
      *> FUNCTION INTEGER-OF-DATE (99991231), which is 3,067,671." The same clause fixes the epoch — "a starting
      *> date of Monday, January 1, 1601 … integer date 1 was a Monday" — so integer date 1 IS 1601-01-01 and
      *> 3,067,671 IS 9999-12-31, which §15.3.1.2's basic calendar date format renders as 16010101 and 99991231
      *> (§15.39.4 r1: "a representation of the date contained in argument-2 according to the format in
      *> argument-1").
      *>
      *> Outside that interval the argument is an incorrect value for the argument, so ISO §15.3 applies: "If the
      *> evaluation of an argument results in an incorrect value for that argument … the EC-ARGUMENT-FUNCTION
      *> exception condition is set to exist." With checking enabled the declarative fires and RESUME AT NEXT
      *> STATEMENT leaves the receiver untouched, so CAUGHT is the assertion and step 7 proves the receiver was
      *> never written by any of the four raising calls. §15.3's OTHER arm — "if … checking … is not enabled, the
      *> implementor defines the result of the function reference" — is why the exception, and not a default
      *> value, is what this file asserts: a default result is implementor-defined and cannot be derived here.
      *>
      *> THE IN-RANGE ENDPOINTS ARE NOT DECORATION. A guard that raised on everything would satisfy steps 3-6 and
      *> be wrong; steps 1 and 2 are what make the interval an interval.
      *>
      *> STEP 6 IS THE NARROWING ARM, and it is the site this inventory row names (IntrinsicRenderer#AsInt):
      *> 184,467,440,737,096,955 × 100 + 67 = 18,446,744,073,709,695,567 = 2^64 + 143,951. A cast that wrapped
      *> modulo 2^64 would hand the function 143,951 — a perfectly plausible integer date (1995-02-15) — and print
      *> 19950215 from an argument nineteen orders of magnitude away, with the §15.5.2 guard never reached. A value
      *> the receiving body cannot represent is an incorrect argument under §15.3, so it raises there instead.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDIDF1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R   PIC X(8) VALUE SPACES.
       01 P18   PIC 9(18) VALUE 184467440737096955.
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
      *> §15.5.2's lower endpoint: "greater than zero", and integer date 1 is the epoch itself.
           DISPLAY "1-LOW-ENDPOINT-1".
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD" 1) TO W-R.
           DISPLAY "  " W-R.
      *> §15.5.2's upper endpoint: INTEGER-OF-DATE(99991231) = 3,067,671.
           DISPLAY "2-HIGH-ENDPOINT-3067671".
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD" 3067671) TO W-R.
           DISPLAY "  " W-R.
      *> Zero is not "greater than zero".
           DISPLAY "3-ZERO-NOT-IN-INTEGER-DATE-FORM".
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD" 0) TO W-R.
      *> One past the stated ceiling.
           DISPLAY "4-PAST-CEILING-3067672".
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD" 3067672) TO W-R.
      *> Negative: "a positive integer" excludes it, and the low end is a bound, not a sign test.
           DISPLAY "5-NEGATIVE".
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD" -5) TO W-R.
      *> 2^64 + 143,951 — the narrowing arm; a wrapping cast would print 19950215 at step 7.
           DISPLAY "6-BEYOND-THE-CARRIER".
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD" P18 * 100 + 67)
               TO W-R.
      *> The receiver still holds step 2's value: no raising call stored anything.
           DISPLAY "7-RECEIVER-UNTOUCHED".
           DISPLAY "  " W-R.
           STOP RUN.
       END PROGRAM L1FDIDF1.
