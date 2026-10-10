      *> kb/Work PB2694 - a COMPUTE with SEVERAL receivers whose
      *>   expression is a QUOTIENT forms that quotient for EACH
      *>   receiver's own final transfer, never once at the widest
      *>   receiver's scale plus guard digits.
      *>   14.7.7 rule 4 a): the statement has ONE initial evaluation
      *>   (the operands); the arithmetic value is then stored into
      *>   each resultant, rounded by THAT resultant's phrase
      *>   (14.7.4.3 rules 2-8). So each receiver sees the exact
      *>   quotient, whichever other receivers share the statement:
      *>   1  FE ROUNDED, X = 1 / HUGE (HUGE = 3E24)
      *>        FE: 3.333.. -> significand +3.33E-25 (14.6.8.4 rule 1
      *>        adjusts the exponent so the leading digit is nonzero)
      *>        X : 0.000.. truncated -> 000
      *>   2  the same quotient NESTED (1 / HUGE + 0): +3.33E-25
      *>   3  no ROUNDED anywhere (truncation, 14.7.4.3 rule 2):
      *>        FE +3.33E-25, X 000
      *>   4  X NEAREST-EVEN, Y ROUNDED = 1 / 8 = 0.125 exactly:
      *>        X is the even neighbour 0.12 (rule 5), Y the one
      *>        farther from zero 0.13 (rule 4)
      *>   5  X NEAREST-EVEN, Y ROUNDED = NUM / DEN where the quotient
      *>        is 0.125 + 1.25E-25: ABOVE the tie, so both round
      *>        up -> 0.13 / 0.13 (a quotient cut to 16 places reads
      *>        0.125 exactly, a tie, and NEAREST-EVEN gave 0.12)
      *>   6  X PROHIBITED = NUM2 / DEN where the quotient is
      *>        0.1200000000000000000000001: not representable in
      *>        PIC 9V99, so rule 7 raises the size error and X is
      *>        unchanged (the cut quotient read 0.12, exact); Y,
      *>        the receiver to the right, still truncates to 0.12
      *>        (14.7.7 rule 4 b): only that item remains unchanged
      *>        and processing proceeds to the next one to the right)
      *>   cite.py --check 14.7.7 "only that data item remains
      *>   unchanged and processing proceeds to the next resulting
      *>   data item to the right" -> OK 14.7.7 4) b)
      *>   7  DIVIDE .. GIVING FE ROUNDED X (the sibling statement,
      *>        which already formed its quotient per receiver)
      *>   cite.py --check 14.7.4.3 "If the NEAREST-EVEN phrase is
      *>   specified and the arithmetic value cannot be exactly
      *>   represented in the resultant identifier" -> OK 14.7.4.3 5)
      *>   cite.py --check 14.7.4.3 "If the PROHIBITED phrase is
      *>   specified, and the arithmetic value cannot be represented
      *>   exactly in the resultant identifier" -> OK 14.7.4.3 7)
      *>   cite.py --check 14.7.7 "The initial evaluation of the
      *>   statement is done and the result of this operation is
      *>   placed in an intermediate data item" -> OK 14.7.7 4) a)
      *>   cite.py --check 14.6.8.4 "the exponent and significand of
      *>   the value are adjusted such that the most significant digit
      *>   of the significand is not zero" -> OK 14.6.8.4 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2694MR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FE   PIC +9.99E+99.
       01 X    PIC 9V99.
       01 Y    PIC 9V99.
       01 HUGE PIC 9(25) VALUE 3000000000000000000000000.
       01 NUM  PIC 9(25) VALUE 1000000000000000000000001.
       01 DEN  PIC 9(25) VALUE 8000000000000000000000000.
       01 NUM2 PIC 9(25) VALUE 120000000000000000000001.
       01 DEN2 PIC 9(25) VALUE 1000000000000000000000000.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0.55 TO X.
           COMPUTE FE ROUNDED, X = 1 / HUGE.
           DISPLAY "1 [" FE "] [" X "]".
           MOVE 0.55 TO X.
           COMPUTE FE ROUNDED, X = 1 / HUGE + 0.
           DISPLAY "2 [" FE "] [" X "]".
           MOVE 0.55 TO X.
           COMPUTE FE, X = 1 / HUGE.
           DISPLAY "3 [" FE "] [" X "]".
           COMPUTE X ROUNDED MODE IS NEAREST-EVEN, Y ROUNDED = 1 / 8.
           DISPLAY "4 [" X "] [" Y "]".
           COMPUTE X ROUNDED MODE IS NEAREST-EVEN, Y ROUNDED
               = NUM / DEN.
           DISPLAY "5 [" X "] [" Y "]".
           MOVE 0.55 TO X.
           COMPUTE X ROUNDED MODE IS PROHIBITED, Y = NUM2 / DEN2
               ON SIZE ERROR DISPLAY "6 size error"
               NOT ON SIZE ERROR DISPLAY "6 stored"
           END-COMPUTE.
           DISPLAY "6 [" X "] [" Y "]".
           DIVIDE 1 BY HUGE GIVING FE ROUNDED X.
           DISPLAY "7 [" FE "] [" X "]".
           STOP RUN.
