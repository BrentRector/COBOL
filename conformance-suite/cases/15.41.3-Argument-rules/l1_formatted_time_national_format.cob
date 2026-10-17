      *>--- conformance-case
      *> id: l1_formatted_time_national_format
      *> rules: [AR-15.41.3-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.41.3 1) — FORMATTED-TIME argument-1, national literal
      *> "Argument-1 shall be a national or alphanumeric literal": the
      *> NATIONAL half, which no golden in the corpus exercised, and
      *> the alphanumeric half beside it as the control.
      *> 8.3.3.5.1 - "National literals are of
      *> the class and category national" - makes N"hh:mm:ss" exactly
      *> the literal the rule's first word admits, and 15.41.1's type
      *> table makes the FUNCTION type follow argument-1's type
      *> (Alphanumeric -> Alphanumeric, National -> National). A
      *> national format literal therefore returns a NATIONAL value,
      *> received in PIC N and rendered through 15.26 DISPLAY-OF.
      *>
      *> THE VALUES, DERIVED FROM THE RULE TEXT.
      *> 15.5.5 - "A value in standard numeric time form is a numeric
      *> value representing seconds past midnight", so 45296 is
      *> 12*3600 + 34*60 + 56 = 12:34:56.
      *> 15.3.3.1 - the EXTENDED common time format with integer
      *> seconds is hh : mm : ss and "the two colon characters appear
      *> in the data", so "hh:mm:ss" renders 12:34:56 in eight
      *> characters; the BASIC form "contains six characters" with no
      *> separators, so "hhmmss" renders 123456.
      *> 15.3.3.5 - a UTC format is a common time format "followed by
      *> a single uppercase 'Z' character"; 15.41.4 2) makes the time
      *> portion reflect "the adjustment of the value in argument-2 by
      *> the offset in argument-3", and 15.3.3.6.1 1) fixes the
      *> direction: a plus sign means the common time portion "is
      *> adjusted downward by the offset values to represent UTC".
      *> 12:34:56 - 02:00 = 10:34:56, so "10:34:56Z", nine characters.
      *>
      *> THE ALPHANUMERIC CONTROL PRINTS THE SAME CHARACTERS. 15.41.4
      *> 1) makes the returned value "a representation of the standard
      *> numeric time contained in argument-2 according to the format
      *> in argument-1" - the two admitted argument types differ in
      *> the returned TYPE (15.41.1) and never in the representation,
      *> so a divergence between the NAT- and ANU- pairs below is a
      *> defect in one of the two channels, not a property of either.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FT01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NA6 PIC N(6).
       01 NA8 PIC N(8).
       01 NA9 PIC N(9).
       01 AL6 PIC X(6).
       01 AL8 PIC X(8).
       01 AL9 PIC X(9).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-TIME(N"hh:mm:ss" 45296) TO NA8
           DISPLAY "NAT-EXT=[" FUNCTION DISPLAY-OF(NA8) "]"
           MOVE FUNCTION FORMATTED-TIME(N"hhmmss" 45296) TO NA6
           DISPLAY "NAT-BAS=[" FUNCTION DISPLAY-OF(NA6) "]"
           MOVE FUNCTION FORMATTED-TIME(N"hh:mm:ssZ" 45296 120) TO NA9
           DISPLAY "NAT-UTC=[" FUNCTION DISPLAY-OF(NA9) "]"
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ss" 45296) TO AL8
           DISPLAY "ANU-EXT=[" AL8 "]"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" 45296) TO AL6
           DISPLAY "ANU-BAS=[" AL6 "]"
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ssZ" 45296 120) TO AL9
           DISPLAY "ANU-UTC=[" AL9 "]"
           STOP RUN.
       END PROGRAM L1FT01.
