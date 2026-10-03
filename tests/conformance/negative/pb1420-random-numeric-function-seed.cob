      *> reject-at: 85 2002 2014 2023
      *> ISO 8.4.3.2.3 SR11: "A numeric function shall not be specified where an integer operand is required, even
      *> though a particular reference of the numeric function might yield an integer value" (cite.py --check
      *> 8.4.3.2.3 OK, rule 11). RANDOM's argument is typed Int in 15.6 Table 21 ("| RANDOM | Int1 | Num |") and 15.75.3
      *> r2 requires "zero or a positive integer" (cite.py --check 15.75.3 OK), so a numeric function such as SQRT -
      *> even SQRT(4), whose value is the integer 2 - is barred from the seed position.
      *> kb/Work PB1420: the Verified row typed the seed class numeric only ('n'), so the integer-operand screen never
      *> ran on it; FACTORIAL, whose rule is the same, was screened. The Table 21 drift test now derives every integer
      *> position from the standard's table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1420NEG3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9V9(6).
       PROCEDURE DIVISION.
           COMPUTE X = FUNCTION RANDOM(FUNCTION SQRT(4))
           STOP RUN.
