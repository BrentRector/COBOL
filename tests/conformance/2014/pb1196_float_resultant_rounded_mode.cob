      *> kb/Work PB1196 + PB1147 - the ROUNDED phrase and the size error
      *> condition on a FLOATING-POINT resultant identifier.
      *> 14.7.4.3 rules 3-10 speak of "the resultant identifier" with no
      *> floating-point exemption, and 0.1 / 0.7 / 16777217 are not
      *> FLOAT-SHORT values, so each mode picks a binary32 neighbour:
      *>   r10 (implied, r2): "rounded to the nearest value nearer to
      *>       zero"; r8 TOWARD-GREATER "the nearest larger value";
      *>   r3 AWAY-FROM-ZERO; r4/r5/r6 the NEAREST-* tie rules;
      *>   r7 PROHIBITED: EC-SIZE-TRUNCATION, the size error condition,
      *>       "the content of the resultant identifier is unchanged".
      *> A bare ROUNDED is NEAREST-AWAY-FROM-ZERO (11.9.6.3 r2).
      *> 14.7.5 case 3: a value further from zero than the resultant
      *> permits is the size error condition (no-phrase rule 4 names
      *> EC-SIZE-TRUNCATION).
      *> Expected values from the binary32 grid (Python struct):
      *>   0.1: neighbours 0.0999999940395 / 0.100000001490 (above)
      *>   0.7: neighbours 0.699999988079 (nearer) / 0.700000047684
      *>   16777217 = 2**24+1: midpoint of 16777216 (even) / 16777218
      *>   16777219: midpoint of 16777218 / 16777220 (even)
      *> and the binary64 grid: 0.1's neighbours 0.0999999999999999916
      *> and 0.1000000000000000055 (above); 0.5 is exact.
      *> PB1147: an outermost quotient under PROHIBITED (1 / 30 into
      *> PIC 9V9) is 14.7.4.3 r7's EC-SIZE-TRUNCATION, not OVERFLOW.
      *> Unchecked (EC-SIZE off, no phrase) an inexact PROHIBITED store
      *> lands TRUNCATED - the 14.6.13.1.3 item 8 disposition
      *> CONFORMANCE.md DOC-A.1-70 documents for the fixed-point arm.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1196FLTRND14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1  USAGE FLOAT-SHORT VALUE 5.
       01 D1  USAGE FLOAT-LONG.
       01 D   PIC 9V9 VALUE 5.
       01 W   PIC -9.9(11).
       01 WD  PIC -9.9(17).
       01 WB  PIC -9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE F1 = 0.1
           MOVE F1 TO W DISPLAY "F1 = 0.1          " W
           COMPUTE F1 ROUNDED MODE IS TOWARD-LESSER = 0.1
           MOVE F1 TO W DISPLAY "F1 TL 0.1         " W
           COMPUTE F1 ROUNDED MODE IS TOWARD-GREATER = 0.1
           MOVE F1 TO W DISPLAY "F1 TG 0.1         " W
           COMPUTE F1 ROUNDED MODE IS TOWARD-GREATER = 0.7
           MOVE F1 TO W DISPLAY "F1 TG 0.7         " W
           COMPUTE F1 ROUNDED MODE IS AWAY-FROM-ZERO = 0.7
           MOVE F1 TO W DISPLAY "F1 AFZ 0.7        " W
           COMPUTE F1 ROUNDED MODE IS NEAREST-EVEN = 0.7
           MOVE F1 TO W DISPLAY "F1 NE 0.7         " W
           COMPUTE F1 = -0.1
           MOVE F1 TO W DISPLAY "F1 = -0.1         " W
           COMPUTE F1 ROUNDED MODE IS TOWARD-LESSER = -0.1
           MOVE F1 TO W DISPLAY "F1 TL -0.1        " W
           COMPUTE F1 ROUNDED = 16777217
           MOVE F1 TO WB DISPLAY "F1 ROUNDED 2**24+1" WB
           COMPUTE F1 ROUNDED MODE IS NEAREST-TOWARD-ZERO = 16777217
           MOVE F1 TO WB DISPLAY "F1 NTZ 2**24+1    " WB
           COMPUTE F1 ROUNDED MODE IS NEAREST-EVEN = 16777217
           MOVE F1 TO WB DISPLAY "F1 NE 2**24+1     " WB
           COMPUTE F1 ROUNDED MODE IS NEAREST-EVEN = 16777219
           MOVE F1 TO WB DISPLAY "F1 NE 2**24+3     " WB
           COMPUTE F1 ROUNDED MODE IS NEAREST-TOWARD-ZERO = 16777219
           MOVE F1 TO WB DISPLAY "F1 NTZ 2**24+3    " WB
           MOVE 5 TO F1
           COMPUTE F1 ROUNDED MODE IS PROHIBITED = 16777217
               ON SIZE ERROR DISPLAY "F1 PROHIB INEXACT SIZE ERROR "
                   FUNCTION TRIM (FUNCTION EXCEPTION-STATUS)
               NOT ON SIZE ERROR DISPLAY "F1 PROHIB INEXACT NOT ON"
           END-COMPUTE
           MOVE F1 TO WB DISPLAY "F1 UNCHANGED      " WB
           COMPUTE F1 ROUNDED MODE IS PROHIBITED = 16777216
               ON SIZE ERROR DISPLAY "F1 PROHIB EXACT SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "F1 PROHIB EXACT NOT ON"
           END-COMPUTE
           MOVE 5 TO F1
           COMPUTE F1 = 1.0E39
               ON SIZE ERROR DISPLAY "F1 1E39 SIZE ERROR "
                   FUNCTION TRIM (FUNCTION EXCEPTION-STATUS)
               NOT ON SIZE ERROR DISPLAY "F1 1E39 NOT ON"
           END-COMPUTE
           MOVE F1 TO WB DISPLAY "F1 UNCHANGED      " WB
           COMPUTE D1 = 0.1
           MOVE D1 TO WD DISPLAY "D1 = 0.1          " WD
           COMPUTE D1 ROUNDED MODE IS TOWARD-GREATER = 0.1
           MOVE D1 TO WD DISPLAY "D1 TG 0.1         " WD
           COMPUTE D1 ROUNDED MODE IS PROHIBITED = 0.5
               ON SIZE ERROR DISPLAY "D1 PROHIB 0.5 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "D1 PROHIB 0.5 NOT ON"
           END-COMPUTE
           COMPUTE D1 ROUNDED MODE IS PROHIBITED = 0.1
               ON SIZE ERROR DISPLAY "D1 PROHIB 0.1 SIZE ERROR "
                   FUNCTION TRIM (FUNCTION EXCEPTION-STATUS)
               NOT ON SIZE ERROR DISPLAY "D1 PROHIB 0.1 NOT ON"
           END-COMPUTE
           DIVIDE 1 BY 30 GIVING D ROUNDED MODE IS PROHIBITED
               ON SIZE ERROR DISPLAY "D DIVIDE 1/30 SIZE ERROR "
                   FUNCTION TRIM (FUNCTION EXCEPTION-STATUS)
           END-DIVIDE
           COMPUTE D ROUNDED MODE IS PROHIBITED = 1 / 30
               ON SIZE ERROR DISPLAY "D COMPUTE 1/30 SIZE ERROR "
                   FUNCTION TRIM (FUNCTION EXCEPTION-STATUS)
           END-COMPUTE
           DISPLAY "D UNCHANGED       " D
       >>TURN EC-SIZE CHECKING OFF
           COMPUTE F1 ROUNDED MODE IS PROHIBITED = 0.1
           MOVE F1 TO W DISPLAY "F1 UNCHECKED      " W
           STOP RUN.
