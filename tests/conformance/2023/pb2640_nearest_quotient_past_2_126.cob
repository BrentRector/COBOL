      *> kb/Work PB2640 - 14.7.4.3 GR4/GR5/GR6: a NEAREST-* ROUNDED phrase rounds the quotient to the nearest
      *> representable value.  The quotient of the statements below is 9.025E37 / 9.409E37 = 0.95920...: the dividend
      *> and the divisor are both bare 38-digit native products, the REMAINDER of the integer division is the
      *> dividend itself (9.025E37, above 2**126 = 8.5E37), and a rounding kernel that DOUBLED the remainder to
      *> compare it with the divisor wrapped negative on the 128-bit carrier and stored 0 with no condition.
      *> EXACT DECIMAL ARITHMETIC, hand-derived (B = 9.5E18, C = D = 9.7E18):
      *>   A = 9.5E18:  A * B / (C * D) = 0.9592...   nearest-away 1, NEAREST-EVEN 1, NEAREST-TOWARD-ZERO 1,
      *>                                              TRUNCATION 0, AWAY-FROM-ZERO 1
      *>   A = -9.5E18: the negative twin = -0.9592... -1 in every NEAREST mode
      *>   the control, A = 6.0E18 and B = 6.5E18: 3.9E37 / 9.409E37 = 0.4145...  0 in every NEAREST mode
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2640QR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC S9(19) VALUE 9500000000000000000.
       01 B  PIC S9(19) VALUE 9500000000000000000.
       01 C  PIC S9(19) VALUE 9700000000000000000.
       01 D  PIC S9(19) VALUE 9700000000000000000.
       01 Q  PIC S9.
       01 QE PIC -9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE Q ROUNDED = (A * B) / (C * D)
           MOVE Q TO QE
           DISPLAY "T1 nearest away " QE
           COMPUTE Q ROUNDED MODE IS NEAREST-EVEN = (A * B) / (C * D)
           MOVE Q TO QE
           DISPLAY "T2 nearest even " QE
           COMPUTE Q ROUNDED MODE IS NEAREST-TOWARD-ZERO =
               (A * B) / (C * D)
           MOVE Q TO QE
           DISPLAY "T3 nearest toward zero " QE
           COMPUTE Q = (A * B) / (C * D)
           MOVE Q TO QE
           DISPLAY "T4 truncation " QE
           COMPUTE Q ROUNDED MODE IS AWAY-FROM-ZERO = (A * B) / (C * D)
           MOVE Q TO QE
           DISPLAY "T5 away from zero " QE
           MOVE -9500000000000000000 TO A
           COMPUTE Q ROUNDED = (A * B) / (C * D)
           MOVE Q TO QE
           DISPLAY "T6 negative nearest away " QE
           COMPUTE Q ROUNDED MODE IS NEAREST-EVEN = (A * B) / (C * D)
           MOVE Q TO QE
           DISPLAY "T7 negative nearest even " QE
           COMPUTE Q ROUNDED MODE IS NEAREST-TOWARD-ZERO =
               (A * B) / (C * D)
           MOVE Q TO QE
           DISPLAY "T8 negative nearest toward zero " QE
           MOVE 6000000000000000000 TO A
           MOVE 6500000000000000000 TO B
           COMPUTE Q ROUNDED = (A * B) / (C * D)
           MOVE Q TO QE
           DISPLAY "T9 below half " QE
           STOP RUN.
