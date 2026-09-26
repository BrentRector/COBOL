      *> reject-at: 2002
      *> kb/Work PB1196 -- THE EDITION EDGE of the 2014 positive
      *> tests/conformance/2014/pb1196_float_resultant_rounded_mode.
      *> FLOAT-SHORT is a COBOL-2002 usage, and at 2002 a floating-point
      *> resultant already takes 14.7.4.3 rule 2's implied TRUNCATION and
      *> a bare ROUNDED; the ROUNDED MODE IS phrase that selects any other
      *> of the eight modes is a COBOL-2014 introduction (14.7.4.2), so at
      *> --std 2002 the phrase alone is refused, COBOLNET0803.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1196FLTNEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1  USAGE FLOAT-SHORT.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE F1 ROUNDED MODE IS TOWARD-GREATER = 0.1.
           STOP RUN.
