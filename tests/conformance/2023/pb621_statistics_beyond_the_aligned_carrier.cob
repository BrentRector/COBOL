      *> kb/Work PB621 - the arithmetic-statistics functions SUM (15.88.4), RANGE (15.76.4), MEAN (15.60.4),
      *>   MEDIAN (15.61.4) and MIDRANGE (15.62.4) are equal to their equivalent arithmetic expressions
      *>   (15.4.1 gives native arithmetic an implementor-defined approximation of them, never a REFUSAL
      *>   of a value the receiver holds).  The native arms aligned every argument to the LIST's maximum
      *>   scale on the Int128 carrier (max 1.7e38), and alignment multiplies: a 31-digit argument beside
      *>   a PIC SV9(8) item needs 39 digits, so the function raised EC-SIZE-OVERFLOW (14.7.5 rule 5) for
      *>   values a 31-digit receiver holds - and the receiver-scale alignment the note first proposed is
      *>   unsound (RANGE(0.6, -0.5) into a scale-0 receiver is 1.1, i.e. 1, but each argument cut to
      *>   scale 0 first is 0 - 0).  The arguments keep their own digits and the wider carrier (the SDIDI,
      *>   exponent carried at run time) is the one that changes.  H = 1.7e30, L = -1.7e30, X = 9e30,
      *>   S = 0.12345678, S7 = 0.1234567 - each leg was chosen so that the aligned Int128 value PASSES
      *>   1.7e38 (so the leg fails on the unfixed arm), and each expected value is exact arithmetic
      *>   (Python fractions), truncated into the receiver (14.7.4.3 r2):
      *>   - RANGE(H L S) = MAX - MIN = H - L = 3.4e30 (aligned: 1.7e38 - (-1.7e38) = 3.4e38);
      *>   - SUM(H H S) = 3.4e30 + 0.12345678 = 3400000000000000000000000000000.12345678, truncated to the
      *>     integer 3400000000000000000000000000000 (aligned: 1.7e38 + 1.7e38);
      *>   - MEAN(X X X S7) = (3 * 9e30 + 0.1234567) / 4 = 6750000000000000000000000000000.030864175 -> the
      *>     integer 6750000000000000000000000000000: every argument aligns to 38 digits (31 + 7) so the
      *>     WIDEST argument fits and it is the three-term SUM (2.7e38) that leaves the carrier - the
      *>     accumulation allowance, not the widest argument, routes this leg;
      *>   - MEDIAN(X H S L), an even count: the middle two of L < S < H < X are S and H, so
      *>     (S + H) / 2 = 850000000000000000000000000000.06172839, truncated at one fraction digit to
      *>     850000000000000000000000000000.0;
      *>   - MIDRANGE(X H S L) = (MAX + MIN) / 2 = (X + L) / 2 = (9e30 - 1.7e30) / 2 = 3.65e30;
      *>   - control: SUM(S S) = 0.24691356.
      *>   cite.py --check 15.76.4 "(FUNCTION MAX (argument-list) - FUNCTION MIN (argument-list))"
      *>     -> OK 15.76.4 1)   [en dash in the source]
      *>   cite.py --check 15.4.1 "the value returned is an implementor-defined approximation of the
      *>     value of that expression" -> OK 15.4.1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB621STA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H  PIC S9(31) VALUE 1700000000000000000000000000000.
       01 L  PIC S9(31) VALUE -1700000000000000000000000000000.
       01 X  PIC S9(31) VALUE 9000000000000000000000000000000.
       01 S  PIC SV9(8) VALUE 0.12345678.
       01 S7 PIC SV9(7) VALUE 0.1234567.
       01 RW PIC S9(31).
       01 RV PIC S9(30)V9.
       01 RS PIC S9(3)V9(8).
       01 EW PIC +9(31).
       01 EV PIC +9(30).9.
       01 ES PIC -9(3).9(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE RW = FUNCTION RANGE(H L S)
           MOVE RW TO EW
           DISPLAY "RANGE=" EW
           COMPUTE RW = FUNCTION SUM(H H S)
           MOVE RW TO EW
           DISPLAY "SUM=" EW
           COMPUTE RW = FUNCTION MEAN(X X X S7)
           MOVE RW TO EW
           DISPLAY "MEAN=" EW
           COMPUTE RV = FUNCTION MEDIAN(X H S L)
           MOVE RV TO EV
           DISPLAY "MEDIAN=" EV
           COMPUTE RW = FUNCTION MIDRANGE(X H S L)
           MOVE RW TO EW
           DISPLAY "MIDRANGE=" EW
           COMPUTE RS = FUNCTION SUM(S S)
           MOVE RS TO ES
           DISPLAY "SUM-SMALL=" ES
           STOP RUN.
