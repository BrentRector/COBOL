      *> kb/Work PB1510 (row GR-11.9.11.2-3) - NEAREST-EVEN, 11.9.11.2 GR3 c): "the intermediate data
      *>   item shall be rounded to the nearest value that can be represented exactly in SDIDI form.  If
      *>   two such values are equally near, the value in which the rightmost digit of the significand is
      *>   even shall be delivered."  Derivations (exact decimal arithmetic, Python decimal at precision
      *>   34, ROUND_HALF_EVEN over the exact value):
      *>   - 1 - 1.0E-50 rounds UP to 1 (the dropped digits are all 9s, far above half): -1 + 1 = 0.
      *>   - 1 - (5.0E-35 + 1.0E-60) is below the tie (digit 35 is 4): the nearest value is the 34 nines;
      *>     minus 1 is -1.0E-34, times 1.0E+34 is -1.  The defect read the dropped tail as excess and
      *>     answered +0.
      *>   - 1 - 5.0E-35 is the EXACT tie between 0.999...9 (34 nines, odd last digit) and 1.000...0 (even
      *>     last digit): the even neighbour is 1, so -1 + 1 = 0.
      *>   - (1 + 2.5E-33) is the exact tie between ...002 (even last digit) and ...003 (odd) at the 34th
      *>     digit: NEAREST-EVEN delivers ...002, so minus 1 is 2.0E-33 and times 1.0E+33 is 2 (NEAREST-AWAY
      *>     would deliver ...003 and 3 - the sibling program's TIE-EVEN-DIFFERS leg).
      *>   - (1.000000000000000000000000000000003 - 5.0000001E-34) lies BELOW the tie (the exact value is
      *>     1.0000000000000000000000000000000024999999): the nearest is ...002 in every NEAREST mode, so the
      *>     result is 2.  The subtracted operand is shifted down and its tail is NEGATIVE against a positive
      *>     sum - the sign case the defect got wrong (read as excess, it made the value a tie or above).
      *>   - 1234567890123456789012345678901 - 7.0E-30 (31 integer digits): the exact value
      *>     1234567890123456789012345678900.999999999999999999999999999993 has more than 34 digits and its
      *>     35th digit is a 9, so the nearest 34-digit value is 1234567890123456789012345678900.999 + one
      *>     unit = the integer 1234567890123456789012345678901 itself, which fits PIC S9(31).  Printed
      *>     with its sign.
      *>   cite.py --check 11.9.11.2 "the value in which the rightmost digit of the significand is even
      *>     shall be delivered" -> OK 11.9.11.2 3)  (printed rule 3) c)))
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1510NEV.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL
           INTERMEDIATE ROUNDING IS NEAREST-EVEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC S9 SIGN LEADING SEPARATE.
       01 W  PIC S9(31) SIGN LEADING SEPARATE.
       01 T  PIC S9 SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = ((1 - 1.0E-50) - 1) * 1.0E+34
           DISPLAY "SUB-FAR=" R
           COMPUTE R = ((1 - (5.0E-35 + 1.0E-60)) - 1) * 1.0E+34
           DISPLAY "BELOW-TIE=" R
           COMPUTE R = ((1 - 5.0E-35) - 1) * 1.0E+34
           DISPLAY "TIE=" R
           COMPUTE T = ((1 + 2.5E-33) - 1) * 1.0E+33
           DISPLAY "TIE-EVEN-DIFFERS=" T
           COMPUTE T = ((1.000000000000000000000000000000003E+0
               - 5.0000001E-34) - 1) * 1.0E+33
           DISPLAY "BELOW-TIE-2=" T
           COMPUTE W = 1234567890123456789012345678901 - 7.0E-30
           DISPLAY "WIDE=" W
           STOP RUN.
