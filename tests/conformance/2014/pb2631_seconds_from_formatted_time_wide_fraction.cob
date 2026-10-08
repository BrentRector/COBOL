      *> ISO 15.79.4 r1: SECONDS-FROM-FORMATTED-TIME returns ((H * 3600) + (M *
      *> 60) + S) for the time subfields of argument-2, and the fractional
      *> seconds are carried in the result's scale (the format's count of
      *> fractional-second digits, up to 18 - docs/CONFORMANCE.md item 202).
      *>
      *> kb/Work PB2631: the product of the whole seconds and 10 ** 18 was formed
      *> in a 64-bit long. 86 399 seconds at 18 fraction digits is
      *> 8.6399999999999999999E+22, past the 9.2E+18 a long holds, so the value
      *> wrapped silently for a format with 15 to 18 fraction digits.
      *> 23:59:59.999999999999999999 is 23 * 3600 + 59 * 60 + 59 = 86399 whole
      *> seconds and the 18-digit fraction 999999999999999999, so the value is
      *> 86399.999999999999999999. Fifteen digits: 86399.999999999999999.
      *> Controls: two fraction digits (12:34:56.50 is 45296.50) and none.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2631SFT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F18 PIC 9(5)V9(18).
       01 F15 PIC 9(5)V9(15).
       01 F2 PIC 9(5)V99.
       01 F0 PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION SECONDS-FROM-FORMATTED-TIME(
               "hh:mm:ss.ssssssssssssssssss",
               "23:59:59.999999999999999999") TO F18.
           DISPLAY "1-FRACTION-18=" F18.
           MOVE FUNCTION SECONDS-FROM-FORMATTED-TIME(
               "hh:mm:ss.sssssssssssssss",
               "23:59:59.999999999999999") TO F15.
           DISPLAY "2-FRACTION-15=" F15.
           MOVE FUNCTION SECONDS-FROM-FORMATTED-TIME(
               "hh:mm:ss.ss", "12:34:56.50") TO F2.
           DISPLAY "3-FRACTION-2=" F2.
           MOVE FUNCTION SECONDS-FROM-FORMATTED-TIME(
               "hh:mm:ss", "12:34:56") TO F0.
           DISPLAY "4-FRACTION-0=" F0.
           STOP RUN.
