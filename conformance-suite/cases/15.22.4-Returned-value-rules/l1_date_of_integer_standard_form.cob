*>--- conformance-case
*> id: l1_date_of_integer_standard_form
*> rules: [RV-15.22.4-1, RV-15.22.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.22.4 r1/r2 — DATE-OF-INTEGER's returned value IS the standard
      *> date equivalent of argument-1, in the form (YYYYMMDD).
      *>
      *> DERIVED FROM THE RULE TEXT ALONE, never from what the compiler prints.
      *> r1: "The returned value represents the standard date equivalent of the
      *> integer specified in argument-1." r2: "The returned value is in the form
      *> (YYYYMMDD) where YYYY represents a year in the Gregorian calendar; MM
      *> represents the month of that year; and DD represents the day of that
      *> month."
      *> The equivalence is fixed by §15.5.2 — "A value in integer date form is a
      *> positive integer that represents a number of days succeeding December 31,
      *> 1600, in the Gregorian calendar" on "a starting date of Monday, January 1,
      *> 1601" — over the calendar §15.5.1 names ("the Gregorian calendar as
      *> described in ISO 8601-1:2019, 4.2.1"), whose Note gives the leap rule:
      *> divisible by four, EXCEPT a century, EXCEPT a century divisible by 400.
      *>
      *> ⛔ THE ENDPOINTS ARE THE STANDARD'S OWN. §15.5.2: the value "shall be
      *> greater than zero and shall be less than or equal to the value of FUNCTION
      *> INTEGER-OF-DATE (99991231), which is 3,067,671." So 1 is 1601-01-01 (the
      *> starting date) and 3,067,671 is 9999-12-31 — two values the spec states
      *> rather than two values a run reported.
      *>
      *> THE THREE LEAP BRANCHES, chosen so no two agree by accident:
      *>   1155     = 1,154 days after 1601-01-01 -> 1604-02-29 (÷4, not a century)
      *>   145791   -> 2000-02-29                 (a century ÷ 400: IS a leap year)
      *>   36218/36219 -> 1700-02-28 and 1700-03-01, CONSECUTIVE integers, which is
      *>                  how "1700 has no February 29" is visible in the answer
      *>                  rather than merely absent from it (÷100, not ÷400).
      *>   143951   -> 1995-02-15, a mid value whose MM and DD are both non-zero
      *>                  and non-degenerate.
      *> r2's THREE COMPONENTS are then read back out of the mid value by INVERTING
      *> §15.46.3 r1's composition of this very form: "Argument-1 shall be an
      *> integer of the form YYYYMMDD, whose value is obtained from the calculation
      *> (YYYY * 10,000) + (MM * 100) + DD", the three fields §15.5.3 names. Its
      *> inverse is exactly YYYY = INTEGER(D8 / 10000),
      *> MM = INTEGER(MOD(D8, 10000) / 100), DD = MOD(D8, 100).
      *> ⚠ THE ANCHOR IS NOT §15.23.4 r1, though that clause writes the same two
      *> spellings. Its argument-1 is DATE-TO-YYYYMMDD's, which §15.23.1 defines as
      *> the SIX-digit form YYmmdd and §15.23.3 r1 caps below 1000000 — there
      *> INTEGER(argument-1/10000) yields a TWO-digit YY, not a four-digit year, so
      *> it answers a different question about a different value. (A real clause
      *> that answers a different question is the citation failure this file was
      *> corrected for; the composition above is the one that decomposes eight
      *> digits.)
      *> A golden that only printed 19950215 would pin the DIGITS without pinning
      *> that they are a year, a month and a day in that order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DOI01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D8  PIC 9(8).
       01 YY4 PIC 9(4).
       01 MM2 PIC 9(2).
       01 DD2 PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
      *> §15.5.2's two endpoints, stated by the standard itself.
           COMPUTE D8 = FUNCTION DATE-OF-INTEGER(1)
           DISPLAY "E-MIN=" D8
           COMPUTE D8 = FUNCTION DATE-OF-INTEGER(3067671)
           DISPLAY "E-MAX=" D8
      *> §15.5.1 Note, branch 1: 1604 is divisible by four and is not a century.
           COMPUTE D8 = FUNCTION DATE-OF-INTEGER(1155)
           DISPLAY "LEAP4=" D8
      *> branch 3: 2000 is a century divisible by four hundred — a leap year.
           COMPUTE D8 = FUNCTION DATE-OF-INTEGER(145791)
           DISPLAY "LEAP400=" D8
      *> branch 2: 1700 is a century NOT divisible by four hundred, so these two
      *> consecutive integer dates straddle a February 29 that does not exist.
           COMPUTE D8 = FUNCTION DATE-OF-INTEGER(36218)
           DISPLAY "C100A=" D8
           COMPUTE D8 = FUNCTION DATE-OF-INTEGER(36219)
           DISPLAY "C100B=" D8
      *> a mid value, then r2's YYYY / MM / DD read back out of it.
           COMPUTE D8 = FUNCTION DATE-OF-INTEGER(143951)
           DISPLAY "MID=" D8
           COMPUTE YY4 = FUNCTION INTEGER(D8 / 10000)
           COMPUTE MM2 =
               FUNCTION INTEGER(FUNCTION MOD(D8, 10000) / 100)
           COMPUTE DD2 = FUNCTION MOD(D8, 100)
           DISPLAY "MID-YYYY=" YY4
           DISPLAY "MID-MM=" MM2
           DISPLAY "MID-DD=" DD2
           STOP RUN.
       END PROGRAM L1DOI01.
