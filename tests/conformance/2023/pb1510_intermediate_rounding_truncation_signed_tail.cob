      *> kb/Work PB1510 (row GR-11.9.11.2-3) - 11.9.11.2 GR3 e): "If the TRUNCATION phrase is specified
      *>   and an intermediate data item cannot be represented exactly in SDIDI form, the value shall be
      *>   the nearest value in that format that is nearer to zero than the intermediate value."
      *>   An SDIDI holds 34 significant digits.  Derivations (exact decimal arithmetic, checked with
      *>   Python decimal at precision 34 / ROUND_DOWN):
      *>   - 1 - 1.0E-50 is 0.99999...9 (fifty nines) - not
      *>     representable; the nearest 34-digit value nearer to zero is 0.9999999999999999999999999999999999
      *>     (thirty-four nines) = 1 - 1.0E-34.  Minus 1 that is -1.0E-34 exactly; times 1.0E+34 is -1.
      *>   - (1.0E-50 - 1) is -0.999...9 (34 nines, toward zero); plus 1 is +1.0E-34; times 1.0E+34 is +1.
      *>   - the control (the exponent gap fits the scratch): 1 - 1.0E-36 is 0.99999...9 (36 nines)
      *>     truncated to 34 nines; minus 1 is -1.0E-34, times 1.0E+34 is -1.
      *>   The defect folded the discarded tail of the opposite-signed operand into an UNSIGNED sticky bit
      *>   that the rounding read as excess in the result's direction, so the first two answered +0.
      *>   cite.py --check 11.9.11.2 "If the TRUNCATION phrase is specified and an intermediate data
      *>     item cannot be represented exactly in SDIDI form" -> OK 11.9.11.2 3)  (printed rule 3) e)))
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1510TRC.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL
           INTERMEDIATE ROUNDING IS TRUNCATION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9 SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = ((1 - 1.0E-50) - 1) * 1.0E+34
           DISPLAY "SUB-FAR=" R
           COMPUTE R = ((1.0E-50 - 1) + 1) * 1.0E+34
           DISPLAY "ADD-FAR=" R
           COMPUTE R = ((1 - 1.0E-36) - 1) * 1.0E+34
           DISPLAY "CONTROL=" R
           STOP RUN.
