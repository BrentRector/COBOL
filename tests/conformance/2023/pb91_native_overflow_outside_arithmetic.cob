      *> PB91 - a NATIVE (Int128-carrier) intermediate that overflows, or a zero divisor, in a NON-arithmetic context
      *> under >>TURN EC-SIZE checking is the size error condition 14.7.5's no-phrase rule 3 names ("if an arithmetic
      *> operation on an intermediate data item ... would cause the new nonzero value to be farther from zero ... than
      *> is allowed for the intermediate data item"), disposed per 14.6.13.1.3 exactly like PB75's decimal carrier: a
      *> USE declarative runs and RESUME AT NEXT STATEMENT continues after the offending statement (#5); an enclosing
      *> exception-checking PERFORM's WHEN handler runs (#4); an arithmetic statement's own ON SIZE ERROR still takes
      *> precedence (#1). Before PB91 the checked kernels were selected only by the arithmetic statement's phrase, so
      *> `IF A * A > 5` wrapped silently (item 179 (1) documents the wrap for the checking-OFF case only) and a
      *> zero divisor in a condition returned 0. A = 10^20 - 1: A * A is ~10^40, past the Int128 carrier's 1.7 x 10^38.
      *> kb/Work PB1143: a product of two ITEMS whose digit bounds are known (here 20 + 20 > 38) no longer
      *> raises - it is formed exactly on the SDIDI, whose range is 10^6144, and the condition below
      *> (R7) is simply TRUE.  The checked Int128 kernel this golden pins is the one a product of operands whose
      *> bound is UNKNOWN still takes (an intrinsic's value), so the overflowing products below are of
      *> FUNCTION ABS(A) - 10^20 - 1 as an intrinsic result - and the SDIDI's own range is the true boundary.
       >>TURN EC-SIZE-OVERFLOW EC-SIZE-ZERO-DIVIDE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB91NOVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(20) VALUE 99999999999999999999.
       01 Z PIC 9 VALUE 0.
       01 R PIC 9(5) VALUE 7.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW EC-SIZE-ZERO-DIVIDE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "R1".
           IF FUNCTION ABS(A) * FUNCTION ABS(A) > 5
               DISPLAY "GT" ELSE DISPLAY "LE" END-IF.
           DISPLAY "R2".
           IF 5 / Z > 1 DISPLAY "GT2" ELSE DISPLAY "LE2" END-IF.
           DISPLAY "R3".
           DISPLAY "V=" FUNCTION ABS(FUNCTION ABS(A) * FUNCTION ABS(A)).
           DISPLAY "R4".
           PERFORM
               IF FUNCTION ABS(A) * FUNCTION ABS(A) + 1 > 5
                   DISPLAY "GT3" END-IF
           WHEN EC-SIZE-OVERFLOW
               DISPLAY "WHEN=" FUNCTION EXCEPTION-STATUS
               RESUME AT NEXT STATEMENT
           END-PERFORM.
           DISPLAY "R5".
           COMPUTE R = FUNCTION ABS(A) * FUNCTION ABS(A)
               ON SIZE ERROR DISPLAY "PHRASE=" FUNCTION EXCEPTION-STATUS
               NOT ON SIZE ERROR DISPLAY "NOSIZE"
           END-COMPUTE.
           DISPLAY "R6 R=" R.
           IF A - 1 > 5 DISPLAY "STILL-FINE" END-IF.
           DISPLAY "R7".
           IF A * A > 5 DISPLAY "EXACT-GT" ELSE DISPLAY "EXACT-LE" END-IF.
           STOP RUN.
