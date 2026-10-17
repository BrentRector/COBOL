      *>--- conformance-case
      *> id: pb145_round_trunc
      *> rules: [GR-8.8.1.5.2-1, GR-8.8.1.5.2-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB145RT.
      *> kb/Work PB145 - 8.8.1.5.2 r1/r3 + 11.9.11.2 rule 3: the
      *> INTERMEDIATE ROUNDING mode decides (a) the 35-digit exact-tie
      *> reduction (r3b away vs r3c to-even, both kept-digit parities),
      *> (b) the 36-digit literal's r1 conversion (and NUMVAL-F, the
      *> second inexact producer), (c) the gradual-underflow landing on
      *> the 10**-6176 quantum. Values derived per leg in the header of
      *> each expectation.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL
           INTERMEDIATE ROUNDING IS TRUNCATION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9.
       01 M PIC 999.
       01 Q PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = 2.000000000000000000000000000000005E+33 / 2
               - 1.0E+33
           DISPLAY "TIE=" R
           COMPUTE R = 2.000000000000000000000000000000003E+33 / 2
               - 1.0E+33
           DISPLAY "TIE2=" R
           COMPUTE M = 1.23456789012345678901234567890123455E0
               * 1.0E+33 - 1.234567890123456789012345678901E+33
           DISPLAY "L36=" M
           COMPUTE Q = (1.5E-6000 / 1.0E+176) * 1.0E+6144 * 1.0E+32
           DISPLAY "SUB=" Q
           STOP RUN.
