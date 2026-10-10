      *> kb/Work PB2698 - the unary exact functions ABS, SIGN, INTEGER,
      *>   INTEGER-PART and FRACTION-PART over the two 16-byte binary
      *>   values the Int128 intermediate cannot hold: -2**127 (a signed
      *>   item's container minimum) and an unsigned item above 2**127-1.
      *>   13.18.60.4 GR12: "The implementor may allow a wider range"
      *>   (cite.py --check 13.18.60.4 "The implementor may allow a
      *>   wider range" -> OK 13.18.60.4 12)).
      *>
      *>   ABS, 15.7.4 r1: the equivalent arithmetic expression is
      *>   (argument-1) for zero or positive and (- (argument-1)) for
      *>   negative.  -(-2**127) = 2**127 is outside the native
      *>   intermediate data item (a scaled Int128), whose range is
      *>   CHECKED, so 14.7.5 case 5 makes it the size error condition:
      *>   "if native arithmetic is in effect and the implementor defines
      *>   that the range of values allowed for the intermediate data
      *>   item is to be checked" (cite.py --check 14.7.5 -> OK 5)).
      *>   The pre-fix runtime wrapped -v to itself and returned -2**127.
      *>   ABS of -2**127+1 is 2**127-1, which fits and is stored.
      *>
      *>   The unsigned operand U2 PIC 9(17)V99 COMP-5 holds 2**127+1 at
      *>   scale 2, i.e. 1701411834604692317316873037158841057.29 (a
      *>   value only the unsigned-wide lane holds); before the fix every
      *>   one of these five functions given it was a Roslyn CS1503.
      *>   ABS: |U2| = U2 (15.7.4 r1 a), stored truncated at the
      *>   receiver's scale 0.  SIGN: +1 (15.81.4 r1 a, "When the value
      *>   of argument-1 is greater than zero").
      *>   INTEGER: the greatest integer not above the argument,
      *>   1701411834604692317316873037158841057 = X"0147AE14..." (15.44.4
      *>   r1).  INTEGER-PART agrees for a positive argument (15.49.4:
      *>   SIGN * INTEGER(ABS)).  FRACTION-PART is argument-1 less
      *>   INTEGER-PART(argument-1) = .29 (15.42.4 r1).
      *>   cite.py --check 15.81.4 "When the value of argument-1 is
      *>   greater than zero" -> OK 15.81.4 1) a)
      *>   cite.py --check 15.44.4 "The returned value is the greatest
      *>   integer less than or equal to the value of argument-1" -> OK
      *>   cite.py --check 15.49.4 "FUNCTION SIGN (argument-1)" -> OK
      *>   cite.py --check 15.42.4 "argument-1 - FUNCTION INTEGER-PART
      *>   (argument-1)" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2698UF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 S PIC S9(19) COMP-5.
       01 GX REDEFINES G PIC X(16).
       01 H.
          05 U2 PIC 9(17)V99 COMP-5.
       01 HX REDEFINES H PIC X(16).
       01 R.
          05 UR PIC 9(19) COMP-5 VALUE 7.
       01 RX REDEFINES R PIC X(16).
       01 W.
          05 V PIC 9(19) COMP-5.
       01 WX REDEFINES W PIC X(16).
       01 SG PIC 9 VALUE 5.
       01 FR PIC 9V99 VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           MOVE X"80000000000000000000000000000000" TO GX.
           MOVE X"80000000000000000000000000000001" TO HX.
           MOVE X"7FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" TO WX.
           PERFORM SHOW-ABS-MIN.
           MOVE X"80000000000000000000000000000001" TO GX.
           PERFORM SHOW-ABS-NEAR-MIN.
           PERFORM SHOW-ABS-U.
           PERFORM SHOW-SIGN-U.
           PERFORM SHOW-INTEGER-U.
           PERFORM SHOW-INTEGER-PART-U.
           PERFORM SHOW-FRACTION-PART-U.
           STOP RUN.

       SHOW-ABS-MIN.
           MOVE X"00000000000000000000000000000007" TO RX.
           COMPUTE UR = FUNCTION ABS(S)
               ON SIZE ERROR DISPLAY "ABS(-2**127): size error"
               NOT ON SIZE ERROR DISPLAY "ABS(-2**127): stored"
           END-COMPUTE.
           IF RX = X"00000000000000000000000000000007"
               DISPLAY "UR unchanged".

       SHOW-ABS-NEAR-MIN.
           COMPUTE UR = FUNCTION ABS(S)
               ON SIZE ERROR DISPLAY "ABS(-2**127+1): size error"
               NOT ON SIZE ERROR DISPLAY "ABS(-2**127+1): stored"
           END-COMPUTE.
           IF UR = V
               DISPLAY "UR holds 2**127-1"
           ELSE
               DISPLAY "UR wrong".

       SHOW-ABS-U.
           MOVE X"00000000000000000000000000000007" TO RX.
           COMPUTE UR = FUNCTION ABS(U2)
               ON SIZE ERROR DISPLAY "ABS(U2): size error"
               NOT ON SIZE ERROR DISPLAY "ABS(U2): stored"
           END-COMPUTE.
           IF RX = X"0147AE147AE147AE147AE147AE147AE1"
               DISPLAY "UR holds U2 truncated to an integer"
           ELSE
               DISPLAY "UR wrong".

       SHOW-SIGN-U.
           COMPUTE SG = FUNCTION SIGN(U2)
               ON SIZE ERROR DISPLAY "SIGN(U2): size error"
               NOT ON SIZE ERROR DISPLAY "SIGN(U2) = " SG
           END-COMPUTE.

       SHOW-INTEGER-U.
           MOVE X"00000000000000000000000000000007" TO RX.
           COMPUTE UR = FUNCTION INTEGER(U2)
               ON SIZE ERROR DISPLAY "INTEGER(U2): size error"
               NOT ON SIZE ERROR DISPLAY "INTEGER(U2): stored"
           END-COMPUTE.
           IF RX = X"0147AE147AE147AE147AE147AE147AE1"
               DISPLAY "UR holds the integer part"
           ELSE
               DISPLAY "UR wrong".

       SHOW-INTEGER-PART-U.
           MOVE X"00000000000000000000000000000007" TO RX.
           COMPUTE UR = FUNCTION INTEGER-PART(U2)
               ON SIZE ERROR DISPLAY "INTEGER-PART(U2): size error"
               NOT ON SIZE ERROR DISPLAY "INTEGER-PART(U2): stored"
           END-COMPUTE.
           IF RX = X"0147AE147AE147AE147AE147AE147AE1"
               DISPLAY "UR holds the integer part"
           ELSE
               DISPLAY "UR wrong".

       SHOW-FRACTION-PART-U.
           COMPUTE FR = FUNCTION FRACTION-PART(U2)
               ON SIZE ERROR DISPLAY "FRACTION-PART(U2): size error"
               NOT ON SIZE ERROR DISPLAY "FRACTION-PART(U2) = " FR
           END-COMPUTE.
