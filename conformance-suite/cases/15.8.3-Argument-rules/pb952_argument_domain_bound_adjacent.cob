*>--- conformance-case
*> id: pb952_argument_domain_bound_adjacent
*> rules: [AR-15.8.3-2, AR-15.10.3-2, AR-15.74.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 15.3 rule 14 - "If the evaluation of an argument results in an incorrect value for
      *> that argument ... the EC-ARGUMENT-FUNCTION exception condition is set to exist" - over the seven
      *> argument-1 VALUE domains the binary64 family's 15.x.3 rules state (kb/Work PB952):
      *>   15.8.3 r2 ACOS / 15.10.3 r2 ASIN  "greater than or equal to -1 and less than or equal to +1"
      *>   15.84.3 r2 SQRT                    "zero or positive"
      *>   15.55.3 r2 LOG / 15.56.3 r2 LOG10  "greater than zero"
      *>   15.9.3 r2 ANNUITY                  "greater than or equal to zero"
      *>   15.74.3 r2 PRESENT-VALUE           "greater than -1"
      *> THE WITNESS IS THE CLOSEST ARGUMENT OUTSIDE EACH BOUND, not a round number: a PIC S9V9(30) item
      *> holds 31 digits (13.18.40.3 SR14's maximum) EXACTLY, so +/-(1 + 10**-30) and -(10**-30) are the
      *> nearest values past +/-1 and 0 any fixed-point argument can take - and every one of them rounds ONTO
      *> the bound in binary64 (1 + 1E-30 is 1.0), which is how the screen used to miss them: ASIN answered
      *> pi/2 and raised nothing. The ACCEPT arm is the bound itself, or the nearest legal value inside it
      *> (PRESENT-VALUE's -0.999...9, which binary64 rounds to the ILLEGAL -1.0 - a screen on the double
      *> raised there). Checking is ON (so 15.3 fixes the observable: the condition EXISTS and the
      *> declarative runs); an accepted value prints its 15.x.4 approximation through a 1E-6 window, never
      *> this implementation's own digits.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB952DOMAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ABOVE-ONE   PIC S9V9(30) VALUE  1.000000000000000000000000000001.
       01 BELOW-M1    PIC S9V9(30) VALUE -1.000000000000000000000000000001.
       01 ONE         PIC S9V9(30) VALUE  1.
       01 MINUS-TINY  PIC S9V9(30) VALUE -0.000000000000000000000000000001.
       01 TINY        PIC S9V9(30) VALUE  0.000000000000000000000000000001.
       01 INSIDE-M1   PIC S9V9(30) VALUE -0.999999999999999999999999999999.
       01 AMT         PIC 9(3) VALUE 100.
       01 W-R         PIC S9(3)V9(6) VALUE 0.
       01 W-F         USAGE COMP-2.
       01 PT.
          05 PR       PIC S9V9(30) OCCURS 2 TIMES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  RAISED".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "ACOS-ABOVE-ONE".
           COMPUTE W-R = FUNCTION ACOS(ABOVE-ONE).
           DISPLAY "ACOS-BELOW-MINUS-ONE".
           COMPUTE W-R = FUNCTION ACOS(BELOW-M1).
           DISPLAY "ASIN-ABOVE-ONE".
           COMPUTE W-R = FUNCTION ASIN(ABOVE-ONE).
           DISPLAY "ASIN-BELOW-MINUS-ONE".
           COMPUTE W-R = FUNCTION ASIN(BELOW-M1).
      *>   the bound itself is admitted: arcsin(1) = pi/2 = 1.5707963...
           DISPLAY "ASIN-ONE".
           COMPUTE W-R = FUNCTION ASIN(ONE).
           IF W-R >= 1.570796 AND W-R <= 1.570797
               DISPLAY "  V=HALFPI" ELSE DISPLAY "  V=BAD" END-IF.
           DISPLAY "SQRT-MINUS-TINY".
           COMPUTE W-R = FUNCTION SQRT(MINUS-TINY).
           DISPLAY "LOG-ZERO".
           COMPUTE W-R = FUNCTION LOG(0).
      *>   ln(1E-30) = -30 ln 10 = -69.0775527...; log10(1E-30) = -30.
           DISPLAY "LOG-TINY".
           COMPUTE W-R = FUNCTION LOG(TINY).
           IF W-R >= -69.077553 AND W-R <= -69.077552
               DISPLAY "  V=LN" ELSE DISPLAY "  V=BAD" END-IF.
           DISPLAY "LOG10-MINUS-TINY".
           COMPUTE W-R = FUNCTION LOG10(MINUS-TINY).
           DISPLAY "LOG10-TINY".
           COMPUTE W-R = FUNCTION LOG10(TINY).
           IF W-R >= -30.000001 AND W-R <= -29.999999
               DISPLAY "  V=MINUS30" ELSE DISPLAY "  V=BAD" END-IF.
           DISPLAY "ANNUITY-MINUS-TINY".
           COMPUTE W-R = FUNCTION ANNUITY(MINUS-TINY 3).
           DISPLAY "PRESENT-VALUE-MINUS-ONE".
           COMPUTE W-F = FUNCTION PRESENT-VALUE(-1 AMT).
      *>   the nearest legal rate: no condition (the value is 100 / 1E-30 - kb/Work PB1000 forms the
      *>   discount base 1 + rate on the exact rate; the observable here is the silence).
           DISPLAY "PRESENT-VALUE-INSIDE".
           COMPUTE W-F = FUNCTION PRESENT-VALUE(INSIDE-M1 AMT).
      *>   The same two rates as a table(ALL) lead (kb/Work PB1000): argument-1 is the table's FIRST
      *>   implicit element (15.3 r14), screened on its exact value - the screen used to read the
      *>   enumerated binary64, where INSIDE-M1 is already -1.0, and refused it.
           MOVE -1 TO PR(1).
           MOVE AMT TO PR(2).
           DISPLAY "PRESENT-VALUE-ALL-MINUS-ONE".
           COMPUTE W-F = FUNCTION PRESENT-VALUE(PR(ALL)).
           MOVE INSIDE-M1 TO PR(1).
           DISPLAY "PRESENT-VALUE-ALL-INSIDE".
           COMPUTE W-F = FUNCTION PRESENT-VALUE(PR(ALL)).
           DISPLAY "DONE".
           STOP RUN.
