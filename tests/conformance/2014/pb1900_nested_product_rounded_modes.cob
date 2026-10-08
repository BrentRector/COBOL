      *> kb/Work PB1900 - the ROUNDED MODE of the final transfer sees the EXACT value of a nested native product
      *>   difference (14.7.4.3; the ROUNDED MODE IS phrase is a 2014 feature).  U = 10**10+10**-10, V = 10**10+3*10**-10,
      *>   W = X = 10**10+2*10**-10 (PIC 9(11)V9(10)): U*V - W*X = -1e-20 EXACTLY, a tail 19 places below the receiver
      *>   PIC S9V9(19).  Each mode rounds that ONE exact value once (Python fractions):
      *>   - TRUNCATION and NEAREST-AWAY-FROM-ZERO and NEAREST-EVEN  -> 0
      *>   - AWAY-FROM-ZERO and TOWARD-LESSER (down)                 -> -1e-19
      *>   - TOWARD-GREATER (up)                                     -> 0
      *>   and PROHIBITED, the value not being representable at scale 19, is the size error condition with the
      *>   receiver unchanged (14.7.4.3 r7).  Positive twin (W*X - U*V = +1e-20): AWAY-FROM-ZERO and TOWARD-GREATER
      *>   -> +1e-19, TOWARD-LESSER -> 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1900M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  PIC 9(11)V9(10) VALUE 10000000000.0000000001.
       01 V  PIC 9(11)V9(10) VALUE 10000000000.0000000003.
       01 W  PIC 9(11)V9(10) VALUE 10000000000.0000000002.
       01 X  PIC 9(11)V9(10) VALUE 10000000000.0000000002.
       01 A  PIC 9(21) VALUE 100000000000000000001.
       01 B  PIC 9(21) VALUE 100000000000000000001.
       01 C  PIC 9(21) VALUE 100000000000000000000.
       01 D  PIC 9(21) VALUE 100000000000000000002.
       01 F19 PIC S9V9(19) VALUE 7.
       01 E19 PIC +9.9(19).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE F19 ROUNDED MODE IS TRUNCATION = U * V - W * X
           MOVE F19 TO E19
           DISPLAY "TRUNCATION=" E19
           COMPUTE F19 ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO
               = U * V - W * X
           MOVE F19 TO E19
           DISPLAY "NEAREST-AWAY=" E19
           COMPUTE F19 ROUNDED MODE IS NEAREST-EVEN = U * V - W * X
           MOVE F19 TO E19
           DISPLAY "NEAREST-EVEN=" E19
           COMPUTE F19 ROUNDED MODE IS AWAY-FROM-ZERO = U * V - W * X
           MOVE F19 TO E19
           DISPLAY "AWAY=" E19
           COMPUTE F19 ROUNDED MODE IS TOWARD-LESSER = U * V - W * X
           MOVE F19 TO E19
           DISPLAY "TOWARD-LESSER=" E19
           COMPUTE F19 ROUNDED MODE IS TOWARD-GREATER = U * V - W * X
           MOVE F19 TO E19
           DISPLAY "TOWARD-GREATER=" E19
           COMPUTE F19 ROUNDED MODE IS AWAY-FROM-ZERO = W * X - U * V
           MOVE F19 TO E19
           DISPLAY "POSITIVE-AWAY=" E19
           COMPUTE F19 ROUNDED MODE IS TOWARD-GREATER = W * X - U * V
           MOVE F19 TO E19
           DISPLAY "POSITIVE-TOWARD-GREATER=" E19
           COMPUTE F19 ROUNDED MODE IS TOWARD-LESSER = W * X - U * V
           MOVE F19 TO E19
           DISPLAY "POSITIVE-TOWARD-LESSER=" E19
      *> A nested product difference as a FUNCTION argument (a receiver-less render, lowered to the SDIDI): A*B - C*D =
      *> 10**40+2*10**20+1 - (10**40+2*10**20) = 1 EXACTLY, and the lowered value is that 1, so ABS is 1.
           DISPLAY "ABS=" FUNCTION ABS(A * B - C * D)
           MOVE 7 TO F19
           COMPUTE F19 ROUNDED MODE IS PROHIBITED = U * V - W * X
               ON SIZE ERROR DISPLAY "SIZE-ERROR-PROHIBITED"
               NOT ON SIZE ERROR DISPLAY "PROHIBITED-MISSED"
           END-COMPUTE
           MOVE F19 TO E19
           DISPLAY "PROHIBITED-UNCHANGED=" E19
           STOP RUN.
