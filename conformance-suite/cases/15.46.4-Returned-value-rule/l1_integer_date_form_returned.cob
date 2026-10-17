*>--- conformance-case
*> id: l1_integer_date_form_returned
*> rules: [RV-15.46.4-1, RV-15.47.4-1, FMT-15.46.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.46.4 r1 / §15.47.4 r1 — INTEGER-OF-DATE and INTEGER-OF-DAY each
      *> return a value IN INTEGER DATE FORM, and §15.46.2's one-argument format.
      *>
      *> Both rules are one sentence — "The returned value is in integer date
      *> form." — so the whole of their content is §15.5.2, which defines that
      *> form: "a positive integer that represents a number of days succeeding
      *> December 31, 1600, in the Gregorian calendar", on "a starting date of
      *> Monday, January 1, 1601", and "It shall be greater than zero and shall be
      *> less than or equal to the value of FUNCTION INTEGER-OF-DATE (99991231),
      *> which is 3,067,671." The calendar is §15.5.1's ("as described in
      *> ISO 8601-1:2019, 4.2.1"), whose Note gives the leap rule.
      *>
      *> ⛔ THE MAXIMUM IS NOT A MEASUREMENT. §15.5.2 writes the reference
      *> FUNCTION INTEGER-OF-DATE (99991231) and its value 3,067,671 into the
      *> standard's own text, so DATE-MAX below is the one line of this file whose
      *> expected value is quoted verbatim rather than derived. DATE-MIN is the
      *> other end of the same sentence: 1601-01-01 is the starting date, one day
      *> succeeding December 31, 1600, hence 1.
      *>
      *> §15.46.3 r1 gives argument-1 as "(YYYY * 10,000) + (MM * 100) + DD" and
      *> §15.47.3 r1 as "(YYYY * 1000) + DDD", so THE SAME CALENDAR DAY IS WRITTEN
      *> TWO WAYS and must produce the SAME integer — there is one integer date
      *> form, not one per function. Every pair below is that assertion, and it is
      *> the axis a single-function golden cannot see: 1604-02-29 is 1604060,
      *> 1700-02-28 is 1700059, 1700-03-01 is 1700060, 2000-02-29 is 2000060,
      *> 1995-02-15 is 1995046, 9999-12-31 is 9999365 (9999 is not divisible by
      *> four, so its ordinal year ends at 365 — the §15.47.3 r1b "valid for the
      *> year specified" edge).
      *>
      *> THE LEAP BRANCHES, each derived by counting days from 1601-01-01:
      *>   16040229 / 1604060  -> 1155     (÷4, not a century)
      *>   20000229 / 2000060  -> 145791   (a century ÷ 400 IS a leap year)
      *>   17000228 / 1700059  -> 36218    (a century not ÷ 400 is NOT, so...)
      *>   17000301 / 1700060  -> 36219    (...these two are CONSECUTIVE)
      *>   19950215 / 1995046  -> 143951   (a mid value, both subfields non-trivial)
      *>
      *> §15.46.2's format is closed in the rejecting direction too, both halves of
      *> it: negative/l1-integer-of-date-arity2 (a second argument, a form the
      *> function does not have) and negative/l1-integer-of-date-no-function-word
      *> (the underlined required word omitted without §8.4.3.2.3 SR2's REPOSITORY
      *> permission). The permission's own accepting arm is
      *> 2014/l1_repository_bare_intrinsic_names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IDF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N7 PIC 9(7).
       PROCEDURE DIVISION.
       MAIN.
      *> §15.5.2's two endpoints, through the standard-date form.
           COMPUTE N7 = FUNCTION INTEGER-OF-DATE(16010101)
           DISPLAY "DATE-MIN=" N7
           COMPUTE N7 = FUNCTION INTEGER-OF-DATE(99991231)
           DISPLAY "DATE-MAX=" N7
      *> ...and through the Julian form, which must agree.
           COMPUTE N7 = FUNCTION INTEGER-OF-DAY(1601001)
           DISPLAY "DAY-MIN=" N7
           COMPUTE N7 = FUNCTION INTEGER-OF-DAY(9999365)
           DISPLAY "DAY-MAX=" N7
      *> §15.5.1 Note branch 1 — 1604, divisible by four, not a century.
           COMPUTE N7 = FUNCTION INTEGER-OF-DATE(16040229)
           DISPLAY "DATE-L4=" N7
           COMPUTE N7 = FUNCTION INTEGER-OF-DAY(1604060)
           DISPLAY "DAY-L4=" N7
      *> branch 3 — 2000, a century divisible by four hundred.
           COMPUTE N7 = FUNCTION INTEGER-OF-DATE(20000229)
           DISPLAY "DATE-L400=" N7
           COMPUTE N7 = FUNCTION INTEGER-OF-DAY(2000060)
           DISPLAY "DAY-L400=" N7
      *> branch 2 — 1700, a century NOT divisible by four hundred: the two dates
      *> either side of the absent February 29 are one apart, not two.
           COMPUTE N7 = FUNCTION INTEGER-OF-DATE(17000228)
           DISPLAY "DATE-C100A=" N7
           COMPUTE N7 = FUNCTION INTEGER-OF-DATE(17000301)
           DISPLAY "DATE-C100B=" N7
           COMPUTE N7 = FUNCTION INTEGER-OF-DAY(1700059)
           DISPLAY "DAY-C100A=" N7
           COMPUTE N7 = FUNCTION INTEGER-OF-DAY(1700060)
           DISPLAY "DAY-C100B=" N7
      *> a mid value in both forms.
           COMPUTE N7 = FUNCTION INTEGER-OF-DATE(19950215)
           DISPLAY "DATE-MID=" N7
           COMPUTE N7 = FUNCTION INTEGER-OF-DAY(1995046)
           DISPLAY "DAY-MID=" N7
           STOP RUN.
       END PROGRAM L1IDF01.
