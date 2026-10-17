      *>--- conformance-case
      *> id: l1_formatted_time_seconds_range_ec
      *> rules: [AR-15.41.3-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.41.3 3) — argument-2 outside standard numeric time form
      *> RAISES EC-ARGUMENT-FUNCTION, at BOTH ends of the interval.
      *>
      *> "Argument-2 shall be a value in standard numeric time form."
      *> 15.5.5 defines that form as "a numeric value representing
      *> seconds past midnight", and 7.3.17.4 5) fixes its range:
      *> "When OFF is specified or implied, a standard numeric time
      *> form value shall be greater than or equal to zero and less
      *> than 86,400." No LEAP-SECOND directive is written here, so
      *> 7.3.17.4 1) implies the OFF phrase and that is the range in
      *> force.
      *>
      *> WHAT THE STANDARD ITSELF REQUIRES IS THE EXCEPTION, NOT A
      *> VALUE. 15.3 14): "If the evaluation of an argument results in
      *> an incorrect value for that argument ... according to the
      *> rules specified in the function definition and no exception
      *> condition was raised during item identification or expression
      *> evaluation, the EC-ARGUMENT-FUNCTION exception condition is
      *> set to exist"; the same rule leaves the RESULT to the
      *> implementor, and only "if ... checking for EC-ARGUMENT-
      *> FUNCTION is not enabled". Every existing witness of this rule
      *> (pb11_datetime_format_grammar's SEC-OVER,
      *> pb65_leap_second_off's T5) runs with checking OFF and so pins
      *> that implementor-defined result, never the exception the
      *> standard mandates. This one turns checking ON and asserts the
      *> exception - and the LOW end of the interval had no witness of
      *> any kind.
      *>
      *>   1  0 is the lowest legal value: 00:00:00, so "000000"
      *>      under the 15.3.3.1 basic six-character format. No EC.
      *>   2  86399 is the highest: 23*3600 + 59*60 + 59, so
      *>      "235959". No EC.
      *>   3  -1 is not "greater than or equal to zero", so it is not
      *>      a value in standard numeric time form and 15.3 14)
      *>      applies.
      *>   4  86400 is not "less than 86,400", likewise.
      *>   5  RESUME AT NEXT STATEMENT abandons the failed MOVE, so
      *>      the receiver still holds probe 2's value - the returned
      *>      value never reached it. CAUGHT is the assertion; the
      *>      receiver is the corroboration that nothing was stored.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FT03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P6 PIC X(6) VALUE SPACES.
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
           DISPLAY "1-LOW-BOUND-LEGAL".
           MOVE FUNCTION FORMATTED-TIME("hhmmss" 0) TO P6.
           DISPLAY "  [" P6 "]".
           DISPLAY "2-HIGH-BOUND-LEGAL".
           MOVE FUNCTION FORMATTED-TIME("hhmmss" 86399) TO P6.
           DISPLAY "  [" P6 "]".
           DISPLAY "3-BELOW-ZERO".
           MOVE FUNCTION FORMATTED-TIME("hhmmss" -1) TO P6.
           DISPLAY "4-AT-86400".
           MOVE FUNCTION FORMATTED-TIME("hhmmss" 86400) TO P6.
           DISPLAY "5-RECEIVER-UNTOUCHED".
           DISPLAY "  [" P6 "]".
           STOP RUN.
