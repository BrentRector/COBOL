*>--- conformance-case
*> id: pb2966_sdidi_unsigned_wide_operand_and_landing
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2966 - a 16-byte UNSIGNED binary container value in
      *>   [2**127, 2**128) as an operand of, and as the result of, a
      *>   standard-decimal arithmetic expression.
      *>
      *>   8.8.1.5.2 r1: "Any operand of an arithmetic expression that is
      *>   not already in SDIDI form is converted into this form. If the
      *>   operand value cannot be expressed exactly in an SDIDI, the
      *>   value is rounded according to the rules in 11.9.11,
      *>   INTERMEDIATE ROUNDING clause." (cite.py --check 8.8.1.5.2
      *>   "If the operand value cannot be expressed exactly in an SDIDI,
      *>   the value is rounded according to the rules in 11.9.11" -> OK
      *>   8.8.1.5.2 1)).  An operand is therefore ROUNDED to 34 digits,
      *>   never refused: the operand of an expression is not a result.
      *>
      *>   14.7.5 3): "if, after radix point alignment and any applicable
      *>   rounding specifications, the result of an arithmetic statement
      *>   is further from zero than permitted for the associated
      *>   resultant data item" (cite.py --check 14.7.5 -> OK 14.7.5 3)).
      *>   A PIC 9(19) COMP-5 item owns its whole 16-byte container
      *>   (13.18.60.4 GR12, cite.py OK 13.18.60.4 12)), so a result in
      *>   [0, 2**128) is NOT further from zero than the receiver permits.
      *>
      *>   W = 2**128 - 1 = 340282366920938463463374607431768211455 (39
      *>   digits) is the SDIDI 340282366920938463463374607431768200000
      *>   (the 5 dropped digits 11455 begin with 1, so the nearest value
      *>   is below in magnitude, in every nearest mode).  W2 = 2**127 =
      *>   170141183460469231731687303715884105728 is the SDIDI
      *>   170141183460469231731687303715884100000 (05728 begins with 0).
      *>   Q PIC 9(19) shows the LOW 19 digits of its value and QDD the
      *>   value divided by 10**11 truncated (its HIGH 28 digits).
      *>
      *>   W + 0, W - 1, W * 1, W ** 1, W2 + W2 and W2 * 2 are all the SDIDI
      *>   340282366920938463463374607431768200000 (inside the container):
      *>   low 3374607431768200000, high 3402823669209384634633746074.
      *>   ADD W2 TO Q (Q = 7) is 170141183460469231731687303715884100007,
      *>   rounded to 34 digits 170141183460469231731687303715884100000:
      *>   low 1687303715884100000, high 1701411834604692317316873037.
      *>   R PIC S9(19) COMP-5 holds [-2**127, 2**127): R = W2 + 0 is the
      *>   SDIDI 170141183460469231731687303715884100000 (below 2**127 -
      *>   1), stored; R = -W2 is its negative, stored; R = W / 4 is
      *>   85070591730234615865843651857942052863.75 rounded to 34 digits
      *>   85070591730234615865843651857942050000.
      *>   W + W = 680564733841876926926749214863536400000 and W * 2 are
      *>   past 2**128: size error, Q unchanged (storing rule: the
      *>   resultant keeps its value).  R = W + 0 is past 2**127 - 1:
      *>   size error, R unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2966A.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V.
          05 W PIC 9(19) COMP-5.
       01 VX REDEFINES V PIC X(16).
       01 V2.
          05 W2 PIC 9(19) COMP-5.
       01 V2X REDEFINES V2 PIC X(16).
       01 R PIC S9(19) COMP-5 VALUE 7.
       01 Q PIC 9(19) COMP-5 VALUE 7.
       01 QDD PIC 9(28).
       01 RDD PIC S9(28) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN.
           MOVE X"FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" TO VX.
           MOVE X"80000000000000000000000000000000" TO V2X.
           COMPUTE Q = W + 0
               ON SIZE ERROR DISPLAY "W+0 size error"
               NOT ON SIZE ERROR DISPLAY "W+0 ok"
           END-COMPUTE.
           PERFORM SHOWQ.
           MOVE 7 TO Q.
           COMPUTE Q = W + 0.
           PERFORM SHOWQ.
           MOVE 7 TO Q.
           COMPUTE Q = W - 1
               ON SIZE ERROR DISPLAY "W-1 size error"
               NOT ON SIZE ERROR DISPLAY "W-1 ok"
           END-COMPUTE.
           PERFORM SHOWQ.
           MOVE 7 TO Q.
           COMPUTE Q = W ** 1
               ON SIZE ERROR DISPLAY "W**1 size error"
               NOT ON SIZE ERROR DISPLAY "W**1 ok"
           END-COMPUTE.
           PERFORM SHOWQ.
           MOVE 7 TO Q.
           COMPUTE Q = W2 + W2
               ON SIZE ERROR DISPLAY "W2+W2 size error"
               NOT ON SIZE ERROR DISPLAY "W2+W2 ok"
           END-COMPUTE.
           PERFORM SHOWQ.
           MOVE 7 TO Q.
           COMPUTE Q = W2 * 2
               ON SIZE ERROR DISPLAY "W2*2 size error"
               NOT ON SIZE ERROR DISPLAY "W2*2 ok"
           END-COMPUTE.
           PERFORM SHOWQ.
           MOVE 7 TO Q.
           MULTIPLY W BY 1 GIVING Q
               ON SIZE ERROR DISPLAY "MULTIPLY size error"
               NOT ON SIZE ERROR DISPLAY "MULTIPLY ok"
           END-MULTIPLY.
           PERFORM SHOWQ.
           MOVE 7 TO Q.
           SUBTRACT 1 FROM W GIVING Q
               ON SIZE ERROR DISPLAY "SUBTRACT size error"
               NOT ON SIZE ERROR DISPLAY "SUBTRACT ok"
           END-SUBTRACT.
           PERFORM SHOWQ.
           MOVE 7 TO Q.
           DIVIDE W BY 1 GIVING Q
               ON SIZE ERROR DISPLAY "DIVIDE size error"
               NOT ON SIZE ERROR DISPLAY "DIVIDE ok"
           END-DIVIDE.
           PERFORM SHOWQ.
           MOVE 7 TO Q.
           ADD W2 TO Q
               ON SIZE ERROR DISPLAY "ADD size error"
               NOT ON SIZE ERROR DISPLAY "ADD ok"
           END-ADD.
           PERFORM SHOWQ.
           MOVE 7 TO Q.
           COMPUTE Q = W + W
               ON SIZE ERROR DISPLAY "W+W size error"
               NOT ON SIZE ERROR DISPLAY "W+W ok"
           END-COMPUTE.
           PERFORM SHOWQ.
           COMPUTE Q = W * 2
               ON SIZE ERROR DISPLAY "W*2 size error"
               NOT ON SIZE ERROR DISPLAY "W*2 ok"
           END-COMPUTE.
           PERFORM SHOWQ.
           COMPUTE R = W2 + 0
               ON SIZE ERROR DISPLAY "R=W2 size error"
               NOT ON SIZE ERROR DISPLAY "R=W2 ok"
           END-COMPUTE.
           PERFORM SHOWR.
           COMPUTE R = - W2
               ON SIZE ERROR DISPLAY "R=-W2 size error"
               NOT ON SIZE ERROR DISPLAY "R=-W2 ok"
           END-COMPUTE.
           PERFORM SHOWR.
           COMPUTE R = W / 4
               ON SIZE ERROR DISPLAY "R=W/4 size error"
               NOT ON SIZE ERROR DISPLAY "R=W/4 ok"
           END-COMPUTE.
           PERFORM SHOWR.
           MOVE 7 TO R.
           COMPUTE R = W + 0
               ON SIZE ERROR DISPLAY "R=W size error"
               NOT ON SIZE ERROR DISPLAY "R=W ok"
           END-COMPUTE.
           PERFORM SHOWR.
           STOP RUN.
       SHOWQ.
           COMPUTE QDD = Q / 100000000000.
           DISPLAY "  Q " Q " high " QDD.
       SHOWR.
           COMPUTE RDD = R / 100000000000.
           DISPLAY "  R " R " high " RDD.
