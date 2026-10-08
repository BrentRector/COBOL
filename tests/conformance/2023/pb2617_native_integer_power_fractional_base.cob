      *> PB2617 - native exponentiation (8.8.1.3: native arithmetic is an implementor-defined method of evaluation;
      *> CLAUDE.md rule 1 follows GnuCOBOL, whose cob_decimal_pow raises a decimal to an INTEGER exponent exactly by
      *> mpz_pow_ui and adds scale * n) of a FRACTIONAL base to an integer exponent is the exact power, not a binary64
      *> approximation: A ** 2 stores what A * A stores, for a literal exponent, a data-item exponent, a negative
      *> exponent (8.8.1.5.4 r3: 1 / (A ** 2)) and a power past 77 digits. Expected values are exact decimal
      *> arithmetic by hand: 1.1234567891 ** 2 = 1.26215515697488187881; ** 3 truncated to 30 places =
      *> 1.417976780001007264919657928971; ** 8 truncated to 20 places = 2.53776255119685728762; (-1.5) ** 3 =
      *> -3.375 and ** 2 = 2.25; 2.0 ** -2 = 0.25; A ** -2 truncated to 20 places = 0.79229561791498584935. A ** 2 -
      *> A * A is exactly zero (a binary64 power left 1.9E-18). 8.8.1.2 rule 6a: a zero base needs an exponent
      *> greater than zero, so 0 ** 0 is EC-SIZE-EXPONENTIATION, the size error condition of ON SIZE ERROR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2617A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9V9(10) VALUE 1.1234567891.
       01 N PIC 9 VALUE 2.
       01 EIGHT PIC 9 VALUE 8.
       01 NEG PIC S9 VALUE -2.
       01 ZERO-N PIC 9 VALUE 0.
       01 H PIC S9V9 VALUE -1.5.
       01 B2 PIC 9V9 VALUE 2.0.
       01 Z PIC 9V9 VALUE 0.
       01 R2 PIC 9V9(20).
       01 R3 PIC 9V9(30).
       01 R8 PIC 9V9(20).
       01 RDIFF PIC S9V9(20) SIGN LEADING SEPARATE.
       01 RM PIC S9V999 SIGN LEADING SEPARATE.
       01 RQ PIC 9V9(5).
       PROCEDURE DIVISION.
           COMPUTE R2 = A ** 2
           DISPLAY "A ** 2       " R2
           COMPUTE R2 = A ** N
           DISPLAY "A ** N       " R2
           COMPUTE R2 = A * A
           DISPLAY "A * A        " R2
           COMPUTE R3 = A ** 3
           DISPLAY "A ** 3       " R3
           COMPUTE R8 = A ** 8
           DISPLAY "A ** 8       " R8
           COMPUTE R8 = A ** EIGHT
           DISPLAY "A ** EIGHT   " R8
           COMPUTE RDIFF = A ** 2 - A * A
           DISPLAY "A**2 - A*A   " RDIFF
           COMPUTE RM = H ** 3
           DISPLAY "(-1.5) ** 3  " RM
           COMPUTE RM = H ** 2
           DISPLAY "(-1.5) ** 2  " RM
           COMPUTE RQ = B2 ** NEG
           DISPLAY "2.0 ** NEG   " RQ
           COMPUTE R2 = A ** NEG
           DISPLAY "A ** NEG     " R2
           COMPUTE R2 = A ** ZERO-N
           DISPLAY "A ** ZERO-N  " R2
           COMPUTE R2 = Z ** 2
           DISPLAY "Z ** 2       " R2
           MOVE 7 TO R2
           COMPUTE R2 = Z ** ZERO-N
               ON SIZE ERROR DISPLAY "0 ** 0 SIZE ERROR " R2
               NOT ON SIZE ERROR DISPLAY "0 ** 0 STORED " R2
           END-COMPUTE
           IF A ** 2 = A * A
               DISPLAY "A ** 2 EQUAL A * A"
           ELSE
               DISPLAY "A ** 2 NOT EQUAL A * A"
           END-IF
           STOP RUN.
