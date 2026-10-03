      *> kb/Work PB1464. ISO 1989:2023 8.8.2: "a boolean expression enclosed in parentheses" is a boolean
      *> expression, and 8.8.4.2.1 says "The inclusion in parentheses of simple conditions does not change the
      *> simple condition truth value" - so `(BW)` is the simple boolean condition BW (8.8.4.3) and, written as
      *> an operand of a relation, the boolean operand of the boolean relation of 8.8.4.2.2 Format 2.
      *>   cite.py --check 8.8.4.2.1 "The inclusion in parentheses of simple conditions does not change the simple
      *>     condition truth value" -> OK
      *> BW = 1 and BZ = 0, both one boolean position (8.8.4.3.3 SR1):
      *>   (BW) T1 | NOT (BZ) T2 | ((BW)) T3 | NOT (BW) false F4 | (BW) AND NOT (BZ) T5
      *>   (BW) = BZ is 1 = 0, false F6 | (BW) = (BW) T7 | BZ = (BZ) T8 | (BW) = B"1" T9
      *>   EVALUATE (BW) WHEN BZ: 1 against 0, no match, so WHEN OTHER: EV-O
      *> Before this change (BW) drew COBOLNET2318 as a condition and COBOLNET0844 "not a numeric operand" as a
      *> relation operand, in every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1464BOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BW PIC 1 VALUE B"1".
       01 BZ PIC 1 VALUE B"0".
       PROCEDURE DIVISION.
       MAIN.
           IF (BW) DISPLAY "T1" ELSE DISPLAY "F1".
           IF NOT (BZ) DISPLAY "T2" ELSE DISPLAY "F2".
           IF ((BW)) DISPLAY "T3" ELSE DISPLAY "F3".
           IF NOT (BW) DISPLAY "T4" ELSE DISPLAY "F4".
           IF (BW) AND NOT (BZ) DISPLAY "T5" ELSE DISPLAY "F5".
           IF (BW) = BZ DISPLAY "T6" ELSE DISPLAY "F6".
           IF (BW) = (BW) DISPLAY "T7" ELSE DISPLAY "F7".
           IF BZ = (BZ) DISPLAY "T8" ELSE DISPLAY "F8".
           IF (BW) = B"1" DISPLAY "T9" ELSE DISPLAY "F9".
           EVALUATE (BW)
              WHEN BZ DISPLAY "EV-Z"
              WHEN OTHER DISPLAY "EV-O"
           END-EVALUATE.
           STOP RUN.
