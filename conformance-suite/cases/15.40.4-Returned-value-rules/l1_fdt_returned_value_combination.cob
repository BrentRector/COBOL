*>--- conformance-case
*> id: l1_fdt_returned_value_combination
*> rules: [RV-15.40.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.4 r1 - "The returned value is a representation of the
      *> date contained in argument-2 combined with the time contained in
      *> argument-3 according to the format in argument-1."
      *>
      *> THE RULE HAS THREE MOVING PARTS AND THE FORMAT DRIVES ALL OF THEM,
      *> so ONE date and ONE seconds value are run through every combined
      *> format shape §15.3.3.7 admits: "A basic combined date and time
      *> format consists of a basic date format followed by an uppercase 'T'
      *> character followed by a basic time format ... An extended combined
      *> date and time format consists of an extended date format, followed
      *> by an uppercase 'T' character, followed by an extended time format."
      *> §15.3.1.1's six date formats supply both halves of the date column -
      *> calendar, ordinal and week - which no existing FORMATTED-DATETIME
      *> golden exercised beyond the calendar pair.
      *>
      *> Derivations, all from the rule text.
      *>   §15.5.2 - integer date 1 is 1601-01-01, so 143951 is 143950 days
      *>     later: 1995-02-15. §15.3.1.2 renders that as 19950215.
      *>   §15.3.1.5 - the day-of-year subfield: February 15 is day 31+15 =
      *>     046 of 1995 (a common year), so 1995046 / 1995-046.
      *>   §15.3.1.7 - "The first week of a given year is the week that
      *>     includes January 4 of that year." January 4 1995 is a
      *>     Wednesday, so week 01 runs Monday January 2 to Sunday January 8.
      *>     February 15 is 44 days after January 2 = 6 whole weeks + 2 days,
      *>     hence week 07, and it is a Wednesday, which §15.3.1.7 numbers 3
      *>     ("1 through 7 inclusive, representing Monday through Sunday").
      *>     So 1995W073 / 1995-W07-3.
      *>     ANNEX D.31.5.2's table prints 1995W063 for this same date.
      *>     That row contradicts §15.3.1.7's own arithmetic and the annex
      *>     is informative; the normative rule is what is asserted here.
      *>     D.31.5.2 is NOT used as an oracle for any value in this file.
      *>   §15.5.5 - 45296 seconds past midnight is 12:34:56 (12*3600 +
      *>     34*60 + 56). Every shape leg below carries that one time and
      *>     that one date, so the legs differ in the FORMAT and in nothing
      *>     else - and the four shapes the 2014 twin also renders
      *>     (l1_fdt_rules_2014 ORD-B / WK-B / ORD-E / WK-E) assert the
      *>     identical characters, which makes the pair a cross-check.
      *>
      *> WHY THE SHAPE LEGS CARRY AN INTEGER SECONDS VALUE. Nothing in
      *> §15.3, §15.5.5 or §15.40.4 says how precision in EXCESS of the
      *> format's own seconds subfield is disposed of; truncation and
      *> rounding are both admissible under that text, so a fractional
      *> argument rendered into an integer-seconds format would pin an
      *> implementation-defined value rather than a derived one. Every
      *> fractional value used below is therefore EXACT at its own width.
      *>   §15.3.3.2 - the fraction renders at the format's own 's' width,
      *>     and "the decimal separator does not appear in the data
      *>     associated with a BASIC common time format with fractional
      *>     seconds representation" while "the two colon characters and
      *>     the decimal separator appear" in the EXTENDED one. SF =
      *>     18867.812479168304 is 05:14:27.812479168304 (§15.5.5); at TWO
      *>     fraction digits truncation and rounding agree on 81, so
      *>     FRAC2-B and FRAC2-E are determined by the text alone. At FOUR
      *>     digits they do not agree (8124 vs 8125), so the four-digit leg
      *>     uses SF4 = 18867.8124, which carries no excess precision and
      *>     renders 0514278124 from §15.3.3.2 alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDT06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R   PIC X(40).
       01 D   PIC 9(7) VALUE 143951.
       01 S   PIC 9(5) VALUE 45296.
       01 SF  PIC 9(5)V9(12) VALUE 18867.812479168304.
       01 SF4 PIC 9(5)V9(4) VALUE 18867.8124.
       PROCEDURE DIVISION.
       MAIN.
      *> §15.3.1.2 basic calendar + §15.3.3.1 basic integer-seconds time.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D S) TO R
           DISPLAY "CAL-B=" R
      *> §15.3.1.4 basic ordinal date.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYDDDThhmmss" D S) TO R
           DISPLAY "ORD-B=" R
      *> §15.3.1.6 basic week date - the 'W' appears in the data.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYWwwDThhmmss" D S) TO R
           DISPLAY "WK-B=" R
      *> The extended half of each pair; §15.3.3.1 puts the two colons in the
      *> data and §15.3.1.2/§15.3.1.4/§15.3.1.6 the hyphens.
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss" D S)
               TO R
           DISPLAY "CAL-E=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-DDDThh:mm:ss" D S)
               TO R
           DISPLAY "ORD-E=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-Www-DThh:mm:ss" D S)
               TO R
           DISPLAY "WK-E=" R
      *> §15.3.3.2 fractional seconds: the width is the format's, and the
      *> basic form's decimal separator is absent from the data.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss.ss" D SF)
               TO R
           DISPLAY "FRAC2-B=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss.ssss"
               D SF4) TO R
           DISPLAY "FRAC4-B=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss.ss"
               D SF) TO R
           DISPLAY "FRAC2-E=" R
           STOP RUN.
