      *> kb/Work PB1900 (train 1039b review) - a FLOATING-POINT numeric-edited resultant is not a fixed-point final
      *>   transfer: its working scale is only the mask's hint, and the value normalizes into the mask.
      *>   14.6.8.4 1): "the exponent and significand of the value are adjusted such that the most significant digit
      *>   of the significand is not zero"; 2) aligns and truncates per 13.18.40.  So no arm may round the value at the
      *>   hint scale before the store: not the outermost product past the Int128 carrier (A*A, 41 digits), not the
      *>   settlement of a nested exact product, not the outermost quotient, not the MAX selection.  Each expected
      *>   image is derived from the rules: the value normalized, the six-digit significand of -9.9(5)E+99 truncated,
      *>   a positive value showing a space for the minus sign.
      *>   - A = 10**20: A*A = 10**40                        -> " 1.00000E+40" (was 0: wrapped)
      *>   - A*A - A = 10**40 - 10**20, the nested exact product settled once
      *>                                                     -> " 9.99999E+39" (was 9.99999E+29: wrapped)
      *>   - 1 / 3000000 = 3.333...E-07                       -> " 3.33333E-07" (was 3.30000E-07)
      *>   - MAX(D, E) with D = 1.234E-07, E = 1E-10          -> " 1.23400E-07" (was 1.20000E-07)
      *>   - a fixed-point resultant still rounds at its own scale: 1 / 3 into PIC 9V9(5) -> 0.33333
      *>   cite.py --check 14.6.8.4 "the exponent and significand of the value are adjusted such that the most
      *>     significant digit of the significand is not zero" -> OK 14.6.8.4 1)
      *>   cite.py --check 8.8.1.3 "Native arithmetic is an implementor-defined method of evaluating an arithmetic
      *>     expression" -> OK 8.8.1.3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1900FE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC 9(21) VALUE 100000000000000000000.
       01 D  PIC V9(10) VALUE 0.0000001234.
       01 E  PIC V9(10) VALUE 0.0000000001.
       01 FE PIC -9.9(5)E+99.
       01 R  PIC 9V9(5).
       PROCEDURE DIVISION.
           COMPUTE FE = A * A
           DISPLAY "A*A      [" FE "]".
           COMPUTE FE = A * A - A
           DISPLAY "A*A-A    [" FE "]".
           COMPUTE FE = 1 / 3000000
           DISPLAY "1/3E6    [" FE "]".
           COMPUTE FE = FUNCTION MAX(D, E)
           DISPLAY "MAX(D,E) [" FE "]".
           COMPUTE R = 1 / 3
           DISPLAY "1/3->R   [" R "]".
           STOP RUN.
