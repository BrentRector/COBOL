*>--- conformance-case
*> id: max_min_leftmost_tie
*> rules: [RV-15.59.4-2, FMT-15.59.2, FMT-15.63.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.59 MAX / §15.63 MIN — the leftmost-tie returned-value rule and the { argument-1 } …
      *> repetition both general formats declare (§15.59.2, §15.63.2).
      *> §15.59.4 r1: comparisons follow §8.8.4.2 simple relation conditions, so alphanumeric operands
      *> of unequal size compare as if the shorter were space-padded — "AB " and "AB" are EQUAL.
      *> §15.59.4 r2: "the content of the argument-1 returned is the leftmost argument-1 having that
      *> value." §15.59.4 r3: "the size of the returned value is the same as the size of the selected
      *> argument-1." So MAX("AB " "AB") selects the 3-char leftmost, MAX("AB" "AB ") the 2-char one,
      *> and a tie AWAY from first position (MAX("A" "B " "B")) selects position 2 (size 2).
      *> FUNCTION LENGTH proves each selected size; the integer legs prove the … repetition with three
      *> arguments through both functions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAXMINTIE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L   PIC 9.
       01 I2  PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH(FUNCTION MAX("AB " "AB")) TO L
           DISPLAY "L1=" L
           MOVE FUNCTION LENGTH(FUNCTION MAX("AB" "AB ")) TO L
           DISPLAY "L2=" L
           MOVE FUNCTION LENGTH(FUNCTION MAX("A" "B " "B")) TO L
           DISPLAY "L3=" L
           DISPLAY "C1=[" FUNCTION MAX("AB " "AB") "]"
           MOVE FUNCTION MAX(11 3 7) TO I2
           DISPLAY "MX=" I2
           MOVE FUNCTION MIN(11 3 7) TO I2
           DISPLAY "MN=" I2
           STOP RUN.
