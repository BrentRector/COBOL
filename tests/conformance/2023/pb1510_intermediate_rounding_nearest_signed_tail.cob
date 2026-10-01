      *> kb/Work PB1510 (row GR-11.9.11.2-3) - the NEAREST modes of the same defect.  11.9.11.2 GR3 a)
      *>   (NEAREST-AWAY-FROM-ZERO, the default) and c) (NEAREST-EVEN): an intermediate that cannot be
      *>   represented exactly in SDIDI form (34 significant digits) is the NEAREST representable value.
      *>   Derivations (exact decimal arithmetic, Python decimal at precision 34, ROUND_HALF_UP over the
      *>   exact value; the NEAREST-EVEN column is the sibling program's):
      *>   - 1 - 1.0E-50 = 0.99999...9 whose 35th and later digits are all 9: the nearest 34-digit value
      *>     is 1 (rounded UP).  Minus 1 is 0, times 1.0E+34 is 0 - printed +0.
      *>   - 1.0E-50 - 1 mirrors it: -1, plus 1 is 0 - printed +0.
      *>   - 1 - (5.0E-35 + 1.0E-60): the exact value is 0.9999999999999999999999999999999999 followed by
      *>     4999...9 (digit 35 is a 4, BELOW the tie), so the nearest is the 34 nines = 1 - 1.0E-34 (NOT
      *>     1): minus 1 is -1.0E-34, times 1.0E+34 is -1.  The defect read the dropped tail as excess
      *>     and answered +0.
      *>   - 1 - 5.0E-35 is the exact tie (digit 35 is 5, nothing follows): NEAREST-AWAY goes to the value
      *>     farther from zero, 1 (11.9.11.2 3) b)).  -1 + 1 = 0 - printed +0.
      *>   The default mode is exercised here (no INTERMEDIATE ROUNDING phrase = NEAREST-AWAY-FROM-ZERO,
      *>   11.9.11.2 3) a)); the NEAREST-EVEN sibling is pb1510_intermediate_rounding_nearest_even_tie.
      *>   cite.py --check 11.9.11.2 "If the INTERMEDIATE ROUNDING clause is not specified, the
      *>     NEAREST-AWAY-FROM-ZERO phrase is implied" -> OK 11.9.11.2 3) a)
      *>   cite.py --check 11.9.11.2 "the value is rounded to the nearest value that can be
      *>     represented in that format" -> OK 11.9.11.2 3) b)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1510NAF.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9 SIGN LEADING SEPARATE.
       01 T PIC S9 SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = ((1 - 1.0E-50) - 1) * 1.0E+34
           DISPLAY "SUB-FAR=" R
           COMPUTE R = ((1.0E-50 - 1) + 1) * 1.0E+34
           DISPLAY "ADD-FAR=" R
           COMPUTE R = ((1 - (5.0E-35 + 1.0E-60)) - 1) * 1.0E+34
           DISPLAY "BELOW-TIE=" R
           COMPUTE R = ((1 - 5.0E-35) - 1) * 1.0E+34
           DISPLAY "TIE=" R
      *> (1 + 2.5E-33) is the exact tie between ...002 and ...003 at the 34th digit: NEAREST-AWAY-
      *> FROM-ZERO delivers the one farther from zero, ...003 - minus 1 is 3.0E-33, times 1.0E+33 is 3
      *> (the NEAREST-EVEN sibling delivers ...002 and 2: the two modes are told apart here).
           COMPUTE T = ((1 + 2.5E-33) - 1) * 1.0E+33
           DISPLAY "TIE-AWAY-DIFFERS=" T
      *> (1.000000000000000000000000000000003 - 5.0000001E-34) is BELOW that tie (exact value
      *> 1.0000000000000000000000000000000024999999): the nearest is ...002, so the result is 2 -
      *> the opposite-sign tail the defect read as excess.
           COMPUTE T = ((1.000000000000000000000000000000003E+0
               - 5.0000001E-34) - 1) * 1.0E+33
           DISPLAY "BELOW-TIE-2=" T
           STOP RUN.
