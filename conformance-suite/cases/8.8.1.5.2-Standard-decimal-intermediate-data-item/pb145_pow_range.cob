*>--- conformance-case
*> id: pb145_pow_range
*> rules: [GR-8.8.1.5.2-2, GR-8.8.1.5.4-2, GR-8.8.1.5.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      >>TURN EC-SIZE-UNDERFLOW EC-SIZE-OVERFLOW CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB145PR.
      *> kb/Work PB145 - CobolDec.Pow's dispositions, each derived from
      *> 8.8.1.5.4 r2/r3 + 8.8.1.5.2 r2: a near-unit base past the loop
      *> bound COMPUTES (1.00001**1000000 = e^9.99995 ~ 22025.36 - the old
      *> escape raised a spurious size error); (-1) to an even exponent
      *> past the long range is +1 in BOTH exponent signs (the old code
      *> took the parity of the SATURATED long - always odd); an SDIDI
      *> base outside binary64 does not collapse (1.0E-400**0.5 = 1.0E-200
      *> exactly; 2.0E+400**0.5 ~ 1.4142E+200); and the two out-of-range
      *> directions carry their OWN names - 0.5**600000 is TOO SMALL
      *> (EC-SIZE-UNDERFLOW, was OVERFLOW), 2**600000 too large.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC S9 VALUE -1.
       01 N PIC 9(21) VALUE 100000000000000000000.
       01 NN PIC S9(21) VALUE -100000000000000000000.
       01 R1 PIC 9(5).9(2).
       01 R2 PIC S9 SIGN LEADING SEPARATE.
       01 T1 PIC 9.9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R1 = 1.00001 ** 1000000
           DISPLAY "NEARONE=" R1
           COMPUTE R2 = B ** N
           DISPLAY "EVENPOS=" R2
           COMPUTE R2 = B ** NN
           DISPLAY "EVENNEG=" R2
           COMPUTE T1 = (1.0E-400 ** 0.5) * 1.0E+200
           DISPLAY "TINY=" T1
           COMPUTE T1 = (2.0E+400 ** 0.5) / 1.0E+200
           DISPLAY "HUGE=" T1
           COMPUTE R2 = 0.5 ** 600000
               ON SIZE ERROR DISPLAY "UNDER=" FUNCTION EXCEPTION-STATUS
           END-COMPUTE
           COMPUTE R2 = 2 ** 600000
               ON SIZE ERROR DISPLAY "OVER=" FUNCTION EXCEPTION-STATUS
           END-COMPUTE
      *> ...and a NEGATIVE out-of-range exponent takes its name from r3's own expression, not from where
      *> the final value lands (kb/Work PB266).  r3 makes 2 ** -600000 the evaluation of
      *> 1 / (2 ** 600000), and THAT inner power is the operation whose value leaves the range - upward.
      *> 8.8.1.5.2 r2 names the condition of the operation that meets it, and 8.8.1.5.2 requires the
      *> exception conditions encountered to be those the mandated evaluation encounters.  Before the r3
      *> hoist these two lines answered the other way round: the sign rode inside the exp argument, so
      *> 2 ** -600000 said UNDERFLOW and 0.5 ** -600000 said OVERFLOW.
           COMPUTE R2 = 2 ** -600000
               ON SIZE ERROR DISPLAY "R3OVER=" FUNCTION EXCEPTION-STATUS
           END-COMPUTE
           COMPUTE R2 = 0.5 ** -600000
               ON SIZE ERROR DISPLAY "R3UNDR=" FUNCTION EXCEPTION-STATUS
           END-COMPUTE
      *> An exponent past the LONG range is the written exponent, never the clamped loop-bound probe
      *> (kb/Work PB267).  1.0E+25 x ln(1.0000000000000001) = 1E+9 and x ln(0.9999999999999999) = -1E+9,
      *> both far outside the decimal128 exponent range - so these are r2's two names.  With the clamped
      *> probe the development computed b ** 9223372036854775807 instead: 3.68E+400 (a SIZE ERROR by
      *> receiver capacity, under a DIFFERENT name) and 2.71E-401 (a SILENT zero, no condition at all).
           COMPUTE R2 = 1.0000000000000001 ** 1.0E+25
               ON SIZE ERROR DISPLAY "SATOVR=" FUNCTION EXCEPTION-STATUS
           END-COMPUTE
           COMPUTE R2 = 0.9999999999999999 ** 1.0E+25
               ON SIZE ERROR DISPLAY "SATUND=" FUNCTION EXCEPTION-STATUS
           END-COMPUTE
           STOP RUN.
