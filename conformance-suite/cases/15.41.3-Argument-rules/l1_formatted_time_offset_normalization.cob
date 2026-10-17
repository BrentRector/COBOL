      *>--- conformance-case
      *> id: l1_formatted_time_offset_normalization
      *> rules: [AR-15.41.3-6, RV-15.41.4-2, RV-15.41.4-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.41.4 2)/3) — the UTC adjustment across MIDNIGHT
      *> in both directions, and the DIRECT reflection at both ends of
      *> standard numeric time form.
      *>
      *> THE SIGN CONVENTION IS THE STANDARD'S, NOT AN ASSUMPTION.
      *> 15.3.3.6.1 1) - a plus sign means the common time portion of
      *> the data "is adjusted downward by the offset values to
      *> represent UTC"; 2) - a minus sign means it "is adjusted
      *> upward". 15.41.3 4) makes argument-3 "an integer
      *> representation of offset from UTC expressed in minutes", so a
      *> positive argument-3 subtracts and a negative one adds.
      *>
      *> WHY A CROSSING MUST NORMALIZE. 15.3.3.3 confines the hours
      *> subfield of the data to "a value from 00 to 23 inclusive" and
      *> the minutes subfield to 00..59, so the only representation of
      *> an adjustment that leaves the day which the format can hold
      *> at all is the congruent time of day.
      *>   R1  3600 = 01:00:00 with +120: 01:00:00 - 02:00 is one hour
      *>       before midnight, i.e. 23:00:00        -> "23:00:00Z"
      *>   R2  82800 = 23:00:00 with -120: 23:00:00 + 02:00 is one
      *>       hour after midnight, i.e. 01:00:00    -> "01:00:00Z"
      *>   R3  45296 = 12:34:56 with +120            -> "10:34:56Z"
      *>       the mid-range control: the adjustment applies and stays
      *>       inside the day, so R1/R2 cannot be read as a lost offset
      *>
      *> 15.41.4 3) - with an OFFSET format "the value in argument-2
      *> is reflected directly in the time portion of the returned
      *> value, and the offset in argument-3 is reflected directly in
      *> the offset portion". No adjustment at all. The two ENDS of
      *> the interval are where a leaked UTC adjustment would have to
      *> wrap and so become unmistakable. 15.3.3.6.1 gives the offset
      *> subformat of a basic offset time format as "five characters:
      *> a plus sign; two lowercase 'h' characters ... and two
      *> lowercase 'm' characters", so 300 minutes renders +0500 and
      *> -300 renders -0500.
      *>   R4  86399 = 23:59:59 with -300  -> "235959-0500"
      *>   R5  0     = 00:00:00 with +300  -> "000000+0500"
      *>   R6  18867.812479168304 s with +300. 18867 = 05:14:27 by
      *>       15.5.5, and 15.3.3.2 says the decimal separator "does
      *>       not appear in the data associated with a basic common
      *>       time format with fractional seconds representation", so
      *>       the two fraction digits (.8124... -> 81) abut the
      *>       seconds: "05142781+0500". That is also the answer Annex
      *>       D.31.5.6 states for these exact three arguments; the
      *>       annex is INFORMATIVE, so the expectation is derived
      *>       from the normative rules and D.31.5.6 is corroboration
      *>       - but it is the one datetime worked example of the
      *>       standard's own that nothing ran FORWARD (pb23 runs
      *>       D.31.5.8 and D.31.5.9, both of them inverses).
      *>       WARNING: D.31.5.6 also asserts that for a UTC or offset
      *>       format "the third parameter is required (although it
      *>       may be zero)". That contradicts the NORMATIVE 15.41.3
      *>       6), which says an omitted argument-3 "shall be
      *>       evaluated as though 0 were specified". The normative
      *>       rule governs, and R7/R8 pin it.
      *>
      *> 15.41.3 6) - "If argument-3 is omitted and the time portion
      *> of the format in argument-1 is a UTC format or an offset
      *> format, the function shall be evaluated as though 0 were
      *> specified", so R7 and R8 shall be character-identical; a
      *> guard present on only one of the two paths separates them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FT02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WSEC PIC 9(5)V9(12) VALUE 18867.812479168304.
       01 P9   PIC X(9).
       01 P11  PIC X(11).
       01 P13  PIC X(13).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ssZ" 3600 120) TO P9
           DISPLAY "R1-UTC-UNDER=[" P9 "]"
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ssZ" 82800 -120) TO P9
           DISPLAY "R2-UTC-OVER=[" P9 "]"
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ssZ" 45296 120) TO P9
           DISPLAY "R3-UTC-MID=[" P9 "]"
           MOVE FUNCTION FORMATTED-TIME("hhmmss+hhmm" 86399 -300)
               TO P11
           DISPLAY "R4-OFF-TOP=[" P11 "]"
           MOVE FUNCTION FORMATTED-TIME("hhmmss+hhmm" 0 300)
               TO P11
           DISPLAY "R5-OFF-BOT=[" P11 "]"
           MOVE FUNCTION FORMATTED-TIME("hhmmss.ss+hhmm" WSEC 300)
               TO P13
           DISPLAY "R6-D31-5-6=[" P13 "]"
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ssZ" 45296) TO P9
           DISPLAY "R7-OMIT=[" P9 "]"
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ssZ" 45296 0) TO P9
           DISPLAY "R8-ZERO=[" P9 "]"
           STOP RUN.
       END PROGRAM L1FT02.
