*>--- conformance-case
*> id: pb66_float_edited_algebraic_and_checking
*> rules: [GR-14.6.13.2-4, GR-14.9.25.4-5, GR-14.9.25.4-6, RV-15.43.4-1,
*>    RV-15.58.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB66 - HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC of a floating-point numeric-edited argument (15.43.4 r1 /
      *> 15.58.4 r1: the value farthest from zero the data description permits - an all-nines significand at the
      *> maximum exponent, negative for LOWEST of a signed picture; LOWEST of an unsigned picture is zero, 15.58.4
      *> r2), and the two exception conditions the form raises under checking: EC-DATA-OVERFLOW (14.9.25.4 GR6
      *> item 4a - the value is farther from zero than the picture permits; a fatal exception, here handled by a
      *> USE declarative + RESUME, the receiver unchanged) and EC-DATA-INCOMPATIBLE (14.6.13.2 rule 4 - a
      *> de-editing MOVE from content that is not a possible result of editing into the picture; the content is
      *> planted through a REDEFINES). Rule 4 reaches the FIXED-POINT numeric-edited sender too: under checking the
      *> de-edit verifies the image is an editing result (Format of the de-edited value reproduces it - Format is the
      *> one editor), so " 12.50" / a BLANK WHEN ZERO all-spaces zero pass and "AB.CDE" raises.
       >>TURN EC-DATA-OVERFLOW EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB66FA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC -9.9(5)E+99.
       01 E3 PIC 9.99E+9.
       01 U1 PIC 9.9(3)E+99.
       01 G.
          05 E4 PIC 9.99E+9.
       01 XR REDEFINES G PIC X(7).
       01 G2.
          05 NE2 PIC ZZ9.99.
       01 XR2 REDEFINES G2 PIC X(6).
       01 NB PIC ZZ9.99 BLANK WHEN ZERO.
       01 N2 PIC S9(5)V9(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-OVERFLOW EC-DATA-INCOMPATIBLE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(E1) TO E1
           DISPLAY "HIGHEST(E1) [" E1 "]".
           MOVE FUNCTION LOWEST-ALGEBRAIC(E1) TO E1
           DISPLAY "LOWEST(E1)  [" E1 "]".
           MOVE FUNCTION HIGHEST-ALGEBRAIC(E3) TO E3
           DISPLAY "HIGHEST(E3) [" E3 "]".
           MOVE FUNCTION LOWEST-ALGEBRAIC(E3) TO E3
           DISPLAY "LOWEST(E3)  [" E3 "]".
           MOVE FUNCTION LOWEST-ALGEBRAIC(U1) TO U1
           DISPLAY "LOWEST(U1)  [" U1 "]".
           MOVE 999 TO E3
           MOVE 1.0E+10 TO E3
           DISPLAY "after overflow [" E3 "] (unchanged)".
           MOVE 1.0E-12 TO E3
           DISPLAY "after underflow [" E3 "] (zero, no exception)".
           MOVE "1.23E+X" TO XR
           MOVE E4 TO N2
           DISPLAY "after incompatible [" N2 "] (unchanged)".
           MOVE 12.5 TO NE2
           MOVE NE2 TO N2
           DISPLAY "fixed de-edit [" N2 "] from [" NE2 "]".
           MOVE 0 TO NB
           MOVE NB TO N2
           DISPLAY "blank-when-zero de-edit [" N2 "] from [" NB "]".
           MOVE "AB.CDE" TO XR2
           MOVE NE2 TO N2
           DISPLAY "after fixed incompatible [" N2 "] (unchanged)".
           STOP RUN.
