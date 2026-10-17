*>--- conformance-case
*> id: pb23_week_date_beyond_integer_date_form
*> rules: [GR-15.3.3.6.1-3, AR-15.48.3-4, AR-15.48.3-5, RV-15.48.4-1, AR-15.79.3-5, RV-15.92.4-1, FMT-15.92.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.5.2 - "A value in integer date form ... shall be greater than zero
      *> and shall be less than or equal to the value of FUNCTION INTEGER-OF-DATE
      *> (99991231), which is 3,067,671."
      *>
      *> A WEEK DATE CAN SATISFY EVERY SUBFIELD RULE AND STILL HAVE NO INTEGER DATE
      *> FORM (fix-queue PB23). 15.3.1.7 requires only: year 1601-9999, week 01-52
      *> (01-53 in a long ISO year), day-of-week 1-7. But 9999-12-31 IS ISO
      *> 9999-W52-5, so 9999-W52-6 and 9999-W52-7 are 10000-01-01 and 10000-01-02.
      *> Both pass every subfield check; neither has an integer date form. The
      *> window is TWO days, not one.
      *>
      *> IT THREW A RAW CLR ArgumentOutOfRangeException OUT OF THREE INTRINSICS.
      *> ISOWeek.ToDateTime overflowed inside the SHARED analyzer, so
      *> TEST-FORMATTED-DATETIME and SECONDS-FROM-FORMATTED-TIME crashed too - not
      *> just INTEGER-OF-FORMATTED-DATE. 15.3 rule 14 permits only
      *> EC-ARGUMENT-FUNCTION or the implementor-defined result, never a CLR fault.
      *>
      *> THE THREE FUNCTIONS MUST NOT AGREE, AND THAT IS THE POINT:
      *> - INTEGER-OF-FORMATTED-DATE must raise. 15.48.4 r1 makes its result "the
      *>   integer date form equivalent", and past 15.5.2's ceiling there is none,
      *>   so rule 14's "incorrect value ... FOR THE RETURNED VALUE" arm applies.
      *> - TEST-FORMATTED-DATETIME must return ZERO. 15.92.1 tests validity
      *>   "according to the specified format"; 15.92.4 requires any non-zero answer
      *>   to be "the ordinal character position at which the first error ... was
      *>   detected" - and no CHARACTER here is in error. The identical '6' in
      *>   "2009W526" is valid, so there is no position to report.
      *> - SECONDS-FROM-FORMATTED-TIME must return its seconds: 15.79.4 reads the
      *>   TIME subfields, which do not depend on the date.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB23WEEKMAX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N  PIC 9(7) VALUE 0.
       01 W-WD PIC X(8).
       01 W-WD10 PIC X(10).
       01 W-WD23 PIC X(22).
       01 W-T  PIC 9(2) VALUE 0.
       01 W-S  PIC 9(5) VALUE 0.
       01 W-F  PIC 9(5).99 VALUE 0.
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
      *> The LAST representable week date: 9999-W52-5 = 9999-12-31 = 3,067,671,
      *> which is 15.5.2's stated maximum exactly. No EC.
           DISPLAY "1-W52-5-LAST-REPRESENTABLE".
           MOVE "9999W525" TO W-WD.
           MOVE FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYWwwD" W-WD)
               TO W-N.
           DISPLAY "  " W-N.
      *> The two days past it. Each raises; RESUME leaves W-N untouched, so the
      *> printed value is still the previous one - CAUGHT is the assertion.
           DISPLAY "2-W52-6-NO-INTEGER-DATE-FORM".
           MOVE "9999W526" TO W-WD.
           MOVE FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYWwwD" W-WD)
               TO W-N.
           DISPLAY "3-W52-7-NO-INTEGER-DATE-FORM".
           MOVE "9999W527" TO W-WD.
           MOVE FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYWwwD" W-WD)
               TO W-N.
      *> The extended week format reaches the same arithmetic - a second format,
      *> not a second defect, and it crashed identically before the fix.
           DISPLAY "4-EXTENDED-FORMAT-SAME".
           MOVE "9999-W52-7" TO W-WD10.
           MOVE FUNCTION INTEGER-OF-FORMATTED-DATE("YYYY-Www-D" W-WD10)
               TO W-N.
      *> TEST-FORMATTED-DATETIME says VALID for the very value that has no integer
      *> date form. A CAUGHT here would mean the wrong-stage reading won.
           DISPLAY "5-TEST-SAYS-VALID".
           MOVE FUNCTION TEST-FORMATTED-DATETIME("YYYYWwwD" "9999W526") TO W-T.
           DISPLAY "  " W-T.
      *> ...and still reports the right POSITION for data that is genuinely wrong:
      *> week 53 in a common ISO year, detectable at the second week digit.
           DISPLAY "6-TEST-STILL-CATCHES-BAD-WEEK".
           MOVE FUNCTION TEST-FORMATTED-DATETIME("YYYYWwwD" "9999W531") TO W-T.
           DISPLAY "  " W-T.
      *> The time subfields are unaffected by an unrepresentable date:
      *> 10*3600 + 11*60 + 12 = 36672.
           DISPLAY "7-SECONDS-UNAFFECTED".
           MOVE FUNCTION SECONDS-FROM-FORMATTED-TIME("YYYYWwwDThhmmss"
               "9999W527T101112") TO W-S.
           DISPLAY "  " W-S.
      *> INTEGER-OF-FORMATTED-DATE still raises for genuinely INVALID data - the
      *> other arm of the same function, so the new arm did not displace it.
           DISPLAY "8-INVALID-WEEK-STILL-RAISES".
           MOVE "9999W531" TO W-WD.
           MOVE FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYWwwD" W-WD)
               TO W-N.
      *> The calendar path's own maximum, as the control: same 3,067,671, no EC.
           DISPLAY "9-CALENDAR-MAX-CONTROL".
           MOVE "99991231" TO W-WD.
           MOVE FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD" W-WD)
               TO W-N.
           DISPLAY "  " W-N.
      *> THE STANDARD'S OWN WORKED EXAMPLES (Annex D.31.5.8 / D.31.5.9), which no
      *> test exercised: a COMBINED format must give the SAME answer as its date-
      *> only or time-only half, because 15.48.4's NOTE says the time portion "is
      *> validated ... but if valid does not impact the returned value", and
      *> 15.79.4's counterpart says the same of the date portion. Until now that
      *> NOTE was honoured by construction and verified by code reading only - and
      *> it is exactly the leg that faulted, since a purely TIME-side question was
      *> computing a date.
           DISPLAY "10-D31.5.8-DATE-ONLY".
           MOVE "19950215" TO W-WD.
           MOVE FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD" W-WD)
               TO W-N.
           DISPLAY "  " W-N.
           DISPLAY "11-D31.5.8-COMBINED-SAME".
           MOVE "19950215T05142781+0500" TO W-WD23.
           MOVE FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDDThhmmss.ss+hhmm"
               W-WD23) TO W-N.
           DISPLAY "  " W-N.
           DISPLAY "12-D31.5.9-TIME-ONLY".
           MOVE FUNCTION SECONDS-FROM-FORMATTED-TIME("hhmmss.ss+hhmm"
               "05142781+0500") TO W-F.
           DISPLAY "  " W-F.
           DISPLAY "13-D31.5.9-COMBINED-SAME".
           MOVE FUNCTION SECONDS-FROM-FORMATTED-TIME("YYYYMMDDThhmmss.ss+hhmm"
               "19950215T05142781+0500") TO W-F.
           DISPLAY "  " W-F.
           STOP RUN.
