      *> ISO 8.8.1.2 r6a: "If the value of an expression to be raised to a power
      *> is zero, the exponent shall have a value greater than zero." Any
      *> positive exponent is therefore legal for a zero base, and nothing at all
      *> bounds the exponent of a base of 1 or -1. 8.8.1.3 leaves the technique
      *> to the implementor but not whether the evaluation finishes.
      *>
      *> kb/Work PB2632: native integer exponentiation multiplied once per unit
      *> of the exponent when the base was 0, 1 or -1 (the carrier-overflow exit
      *> only fires for a larger base), so COMPUTE R = B ** N with N holding
      *> 999999999999999999 was 10 ** 18 Int128 multiplications and never
      *> finished. The values are fixed by arithmetic:
      *>   1 ** N                = 1 for every N
      *>   0 ** N, N > 0         = 0
      *>   (-1) ** N             = 1 for an even N, -1 for an odd N
      *>   2 ** 10, (-3) ** 3    = 1024, -27 (the exact loop is unchanged)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2632POW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B-ONE PIC S9 VALUE 1.
       01 B-ZERO PIC S9 VALUE 0.
       01 B-MINUS PIC S9 VALUE -1.
       01 B-TWO PIC S9 VALUE 2.
       01 B-M3 PIC S9 VALUE -3.
       01 N-EVEN PIC 9(18) VALUE 999999999999999998.
       01 N-ODD PIC 9(18) VALUE 999999999999999999.
       01 N-TEN PIC 99 VALUE 10.
       01 N-THREE PIC 99 VALUE 3.
       01 R PIC S9(5).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = B-ONE ** N-ODD.
           DISPLAY "1-ONE-ODD=" R.
           COMPUTE R = B-ONE ** N-EVEN.
           DISPLAY "2-ONE-EVEN=" R.
           COMPUTE R = B-ZERO ** N-ODD.
           DISPLAY "3-ZERO-ODD=" R.
           COMPUTE R = B-ZERO ** N-EVEN.
           DISPLAY "4-ZERO-EVEN=" R.
           COMPUTE R = B-MINUS ** N-ODD.
           DISPLAY "5-MINUS-ODD=" R.
           COMPUTE R = B-MINUS ** N-EVEN.
           DISPLAY "6-MINUS-EVEN=" R.
           COMPUTE R = B-TWO ** N-TEN.
           DISPLAY "7-TWO-TEN=" R.
           COMPUTE R = B-M3 ** N-THREE.
           DISPLAY "8-MINUS-THREE-THREE=" R.
           STOP RUN.
