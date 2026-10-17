*>--- conformance-case
*> id: l1_fdt_argument3_time_form
*> rules: [GR-7.3.17.4-1, AR-15.40.3-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.3 r4 - "Argument-3 shall be a value in standard numeric
      *> time form." §15.5.5 IS that form: "A value in standard numeric time
      *> form is a numeric value representing seconds past midnight. If the
      *> LEAP-SECOND directive with the OFF phrase is in effect, the value
      *> shall be greater than or equal to zero and less than 86,400."
      *> §7.3.17.4 GR5 says the same, and OFF is the implied default, so this
      *> unit - which specifies no LEAP-SECOND directive - is the OFF case.
      *> The ON case is pinned by pb65_leap_second_on, whose T8 line runs
      *> FORMATTED-DATETIME at 86400 and gets 235960.
      *>
      *> The admissible interval is therefore [0, 86400): 0 and 86399 are
      *> legal VALUES, 86400 is the first illegal one and a negative value is
      *> below the floor. Outside the interval §15.3 rule 14 sets
      *> EC-ARGUMENT-FUNCTION, which the declarative observes; the returned
      *> value on those legs is implementor-defined by rule 14's last
      *> sentence and is deliberately not asserted.
      *>
      *> "A VALUE ... representing seconds past midnight" is NOT restricted
      *> to integers: §15.3.3.2 gives the common time formats a fractional
      *> seconds representation, so line 3 carries 18867.812479168304 (the
      *> value Annex D.31.5.4/D.31.5.7 uses) and renders it at the format's
      *> own fraction width.
      *>
      *> Derivations: §15.5.2 makes 143951 = 1995-02-15. 0 seconds past
      *> midnight is 00:00:00 and 86399 is 23:59:59 (86399 = 23*3600 +
      *> 59*60 + 59). 18867.812479168304 is 05:14:27.812479168304 (18867 =
      *> 5*3600 + 14*60 + 27); the format 'hhmmss.ss' has TWO fraction
      *> digits and is BASIC, and §15.3.3.2 says "the decimal separator does
      *> not appear in the data associated with a basic common time format
      *> with fractional seconds representation" - so 05142781, not
      *> 051427.81.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDT04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC X(40).
       01 D  PIC 9(7) VALUE 143951.
       01 SF PIC 9(5)V9(12) VALUE 18867.812479168304.
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
      *> "greater than or equal to zero" - zero is IN the form.
           DISPLAY "1-FLOOR-ZERO"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D 0) TO R
           DISPLAY "  " R
      *> The last value below 86,400.
           DISPLAY "2-CEILING-86399"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D 86399)
               TO R
           DISPLAY "  " R
      *> A FRACTIONAL value is a value in standard numeric time form.
           DISPLAY "3-A-FRACTIONAL-VALUE-IS-A-VALUE"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss.ss" D SF)
               TO R
           DISPLAY "  " R
      *> "less than 86,400" - 86400 is the FIRST illegal value under OFF.
           DISPLAY "4-86400-IS-THE-FIRST-ILLEGAL-VALUE"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D 86400)
               TO R
      *> The form has a FLOOR as well as a ceiling.
           DISPLAY "5-BELOW-THE-FLOOR"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D -1) TO R
           STOP RUN.
