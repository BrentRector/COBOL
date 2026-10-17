*>--- conformance-case
*> id: pb255_test_formatted_datetime_bounds
*> rules: [RV-15.92.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB255 - FUNCTION TEST-FORMATTED-DATETIME, ISO 15.92.4 r1: "If no format problems or range
      *> problems occur during the evaluation of argument-2 according to the format in argument-1, the value
      *> returned is zero. Otherwise, the value returned is the ordinal character position at which the first
      *> error in argument-2 was detected." Three legs, and each one names the clause that fixes its RANGE.
      *>
      *> OFFH and OFFM - THE OFFSET SUBFIELD BOUNDS. The offset-hours and offset-minutes ranges are NOT
      *> 15.3.3.3's: that clause governs the COMMON TIME format by its own words ("the minutes subfield of the
      *> data (corresponding to mm in a common time format)"). The offset subfields are 15.3.3.6.2's - "The
      *> offset-hours subfield of the data associated with a basic or extended offset time format shall contain
      *> a value from 00 to 23 inclusive" / "... offset-minutes ... from 00 to 59 inclusive". The bounds happen
      *> to coincide with the common-time ones, which is exactly why one shared arm could carry an unverified
      *> justification unnoticed; these two legs are the measurement.
      *>   Format 'hhmmss+hhmm' (15.3.3.6.1: a plus sign, two lowercase h, two lowercase m) is 11 characters.
      *>   OFFH: "121500+2530" - offset-hours "25" at positions 8-9. The analyzer narrows per DIGIT: at '2'
      *>         the interval is [20,29], which straddles 23 and proves nothing; at '5' it is [25,25] and
      *>         25 > 23, so the first position at which the error can be determined is 9.
      *>   OFFM: "121500+0560" - offset-hours "05" is valid; offset-minutes "60" at positions 10-11. At '6'
      *>         the interval is [60,69] and 60 > 59, so the answer is 10 - the FIRST of the two digits.
      *>   OFF0: "121500+0530" - a wholly valid basic offset time: 0.
      *>
      *> SHRT - AN ARGUMENT-2 SHORTER THAN THE FORMAT, docs/CONFORMANCE.md D-TFD1. 15.92.3 places no size rule
      *> on argument-2 and the compiler applies none, so the shape is reachable; 15.92.4 admits only "the
      *> ordinal character position at which the first error in argument-2 was detected" and when the data runs
      *> out there is no character IN ERROR to point at. D-TFD1 answers LENGTH(argument-2) + 1, on two grounds:
      *> the rule says the position AT WHICH the error was detected (15.93.4 r1 b) says "the position of the
      *> first character in error" when it means that), and 15.93.4 r1 c) is the standard's own answer for the
      *> same shape in the sibling TEST- function - an argument that is valid but incomplete returns
      *> "(FUNCTION LENGTH (argument-1) + 1)".
      *>   SHRT: "2005121" against 'YYYYMMDD'. Year "2005" and month "12" consume six characters; the day
      *>         subfield takes '1' (interval [10,19] straddles [1,31], nothing provable) and then the data is
      *>         exhausted at the eighth character position, so the answer is 7 + 1 = 8.
      *>
      *> NOTE - the 15.92.4 NOTE's own examples are written in LOWERCASE ('yyyymmdd'), which 15.3.1.2 does not
      *> admit ("four uppercase 'Y' characters ...") and this compiler rejects at bind time; the formats here
      *> are spelled in uppercase, and docs/CONFORMANCE.md D-TFD2 records that substitution.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB255TFDBOUNDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D-OFFH PIC X(11) VALUE "121500+2530".
       01 D-OFFM PIC X(11) VALUE "121500+0560".
       01 D-OFF0 PIC X(11) VALUE "121500+0530".
       01 D-SHRT PIC X(7)  VALUE "2005121".
       01 W-R    PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W-R =
               FUNCTION TEST-FORMATTED-DATETIME("hhmmss+hhmm" D-OFFH)
           DISPLAY "OFFH=" W-R
           COMPUTE W-R =
               FUNCTION TEST-FORMATTED-DATETIME("hhmmss+hhmm" D-OFFM)
           DISPLAY "OFFM=" W-R
           COMPUTE W-R =
               FUNCTION TEST-FORMATTED-DATETIME("hhmmss+hhmm" D-OFF0)
           DISPLAY "OFF0=" W-R
           COMPUTE W-R =
               FUNCTION TEST-FORMATTED-DATETIME("YYYYMMDD" D-SHRT)
           DISPLAY "SHRT=" W-R
           STOP RUN.
