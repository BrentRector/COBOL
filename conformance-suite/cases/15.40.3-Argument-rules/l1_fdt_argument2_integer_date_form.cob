*>--- conformance-case
*> id: l1_fdt_argument2_integer_date_form
*> rules: [AR-15.40.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.3 r3 - "Argument-2 shall be a value in integer date
      *> form." §15.5.2 IS that form: "A value in integer date form is a
      *> positive integer that represents a number of days succeeding
      *> December 31, 1600, in the Gregorian calendar. It shall be greater
      *> than zero and shall be less than or equal to the value of FUNCTION
      *> INTEGER-OF-DATE (99991231), which is 3,067,671."
      *>
      *> So the admissible interval is [1, 3067671] and BOTH ends are legal
      *> values, not merely non-errors. Outside it §15.3 rule 14 applies -
      *> "If the evaluation of an argument results in an incorrect value for
      *> that argument ... the EC-ARGUMENT-FUNCTION exception condition is
      *> set to exist" - which is what the declarative below observes. The
      *> RESULT on a violating leg is NOT asserted: rule 14's last sentence
      *> makes it implementor-defined when checking is not enabled, so only
      *> the exception is a spec-derived expectation.
      *>
      *> A LOCAL combined format is used throughout so that r3's own screen
      *> is what is measured: the §15.40.4 r2 UTC roll can also carry a date
      *> out of the form, and that interaction is r25_utc_roll_date_range's.
      *>
      *> Derivation of the two legal values (§15.5.2's epoch, integer date
      *> 1 = 1601-01-01): 1 renders 16010101 and 3067671 renders 99991231,
      *> which is the clause's own stated maximum. §15.5.5: 45296 seconds
      *> past midnight is 12:34:56.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDT03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(40).
       01 S PIC 9(5) VALUE 45296.
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
      *> The FLOOR is a legal value, not a boundary to avoid.
           DISPLAY "1-FLOOR-ONE"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" 1 S) TO R
           DISPLAY "  " R
      *> The CEILING §15.5.2 names in words.
           DISPLAY "2-CEILING-3067671"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" 3067671 S)
               TO R
           DISPLAY "  " R
      *> "shall be GREATER THAN zero" - zero itself is not.
           DISPLAY "3-ZERO-IS-NOT-GREATER-THAN-ZERO"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" 0 S) TO R
      *> ...and neither is a negative value; the rule has a FLOOR as well as
      *> a ceiling, and a guard written only at the top would pass line 5.
           DISPLAY "4-NEGATIVE-IS-BELOW-THE-FLOOR"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" -1 S) TO R
      *> The first value past the ceiling - 9999-12-31 is the last date with
      *> an integer date form, so 3067672 denotes no date at all.
           DISPLAY "5-ONE-PAST-THE-CEILING"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" 3067672 S)
               TO R
           STOP RUN.
