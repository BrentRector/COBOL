*>--- conformance-case
*> id: formatted_time_seconds_channel
*> rules: [RV-15.41.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R24 (ledger F44/F46/F57) - the FORMATTED-* seconds argument carries its VALUE across
      *> all four carriers (fixed / wide / float / SDIDI). Derived from ISO/IEC 1989:2023:
      *>   15.41.4 r1 / 15.40.4 - the returned value represents "the standard numeric time contained in
      *>                argument-2 [argument-3]" - the VALUE, fraction included; a float argument is class
      *>                numeric (8.5.2.12 item 2) and legal (15.41.3 r3: Num2).
      *>   15.3.3.2   - a BASIC format's decimal separator "does not appear in the data"; the fraction
      *>                digits render at the format's 's' widths (truncated at the width).
      *>   7.3.17 r5  - standard numeric time form is [0, 86400) under LEAP-SECOND OFF; the VALUE is what
      *>                is range-checked, so a wide-PICTURE argument (9(5)V9(15)) holding 45296.5 is IN
      *>                range - before R24 its unscaled form wrapped through a (long) cast and FABRICATED
      *>                02:20:03 with no exception.
      *>   15.4.1 r1  - under STANDARD-DECIMAL an arithmetic-expression seconds argument (SDIDI carrier)
      *>                is legal (15.3 type 10) and must compile - it drew a raw CS1503 before R24.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R24SECCH.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FL  USAGE FLOAT-LONG VALUE 45296.5.
       01 WID PIC 9(5)V9(15) VALUE 45296.5.
       01 SA  PIC 9(7) VALUE 45000.
       01 SB  PIC 9(3) VALUE 296.
       01 R   PIC X(24).
       PROCEDURE DIVISION.
      *> F46/F57-A: the float fraction survives (basic format: no separator in the data, 15.3.3.2).
           MOVE FUNCTION FORMATTED-TIME("hhmmss.ss", FL) TO R.
           DISPLAY "FLT=[" R "]".
      *> F57-B: a wide unscaled argument passes by VALUE - 45296.5 is 12:34:56 (integer part).
           MOVE FUNCTION FORMATTED-TIME("hhmmss", WID) TO R.
           DISPLAY "WID=[" R "]".
      *> F44: an SDIDI (expression) seconds argument compiles and computes under STANDARD-DECIMAL.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss", 153569, SA + SB) TO R.
           DISPLAY "DEC=[" R "]".
           STOP RUN.
