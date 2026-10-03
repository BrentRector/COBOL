      *> kb/Work PB1464. ISO 1989:2023 8.8.4.2.1: "The inclusion in parentheses of simple conditions does not change
      *> the simple condition truth value." A condition-name (8.8.4.2.7 rule 2) and a switch-status condition-name
      *> (8.8.4.6) are simple conditions, so `(X1)` and `(S1-OFF)` are those conditions and mean what X1 and S1-OFF
      *> mean. The grammar reads a parenthesized bare word as a parenthesized ARITHMETIC operand, and the bare-operand
      *> classifier did not look through the parentheses, so every one of these drew COBOLNET2318 in every edition.
      *>   cite.py --check 8.8.4.2.1 "The inclusion in parentheses of simple conditions does not change the simple
      *>     condition truth value" -> OK
      *> X = 1 and X1 is true for 1; the switch is OFF by default (its OFF STATUS name is true, its ON STATUS name false).
      *>   (X1) T1 | NOT (X1) false F2 | ((X1)) T3 | (S1-OFF) T4 | NOT (S1-ON) T5
      *>   (X1) AND X = 1 T6 | X = 1 AND (X1) T7 | X = 2 OR (X1) T8 | EVALUATE TRUE WHEN (X1) T9
      *>   PERFORM UNTIL (X1) from X = 0 runs the body once (it makes X = 1): N=1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1464POS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS S1-ON OFF STATUS IS S1-OFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
          88 X1 VALUE 1.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           IF (X1) DISPLAY "T1" ELSE DISPLAY "F1".
           IF NOT (X1) DISPLAY "T2" ELSE DISPLAY "F2".
           IF ((X1)) DISPLAY "T3" ELSE DISPLAY "F3".
           IF (S1-OFF) DISPLAY "T4" ELSE DISPLAY "F4".
           IF NOT (S1-ON) DISPLAY "T5" ELSE DISPLAY "F5".
           IF (X1) AND X = 1 DISPLAY "T6" ELSE DISPLAY "F6".
           IF X = 1 AND (X1) DISPLAY "T7" ELSE DISPLAY "F7".
           IF X = 2 OR (X1) DISPLAY "T8" ELSE DISPLAY "F8".
           EVALUATE TRUE
              WHEN (X1) DISPLAY "T9"
              WHEN OTHER DISPLAY "F9"
           END-EVALUATE.
           MOVE 0 TO X.
           PERFORM UNTIL (X1)
              ADD 1 TO X
              ADD 1 TO N
           END-PERFORM.
           DISPLAY "N=" N.
           STOP RUN.
