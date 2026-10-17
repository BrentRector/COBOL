      *>--- conformance-case
      *> id: pb157_compute_sdidi_sole
      *> rules: [GR-14.9.8.4-1]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB157SD.
      *> kb/Work PB157 - 14.9.8.4 GR1a under ARITHMETIC IS
      *> STANDARD-DECIMAL: a SOLE fixed-point data item or literal as
      *> arithmetic-expression-1 evaluates to its EXACT algebraic value
      *> - "rounding, truncation, and decimal point alignment
      *> specifications do not apply to the production of that exact
      *> algebraic value" - so 2.25 must reach the ROUNDED store intact
      *> and round to 2.3 (14.7.4's default NEAREST-AWAY-FROM-ZERO). A
      *> production-side alignment to the receiver's scale would answer
      *> 2.2 for both legs.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9V99 VALUE 2.25.
       01 X PIC 9V9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE X ROUNDED = W
           DISPLAY "X1=" X
           COMPUTE X ROUNDED = 2.25
           DISPLAY "X2=" X
           STOP RUN.
