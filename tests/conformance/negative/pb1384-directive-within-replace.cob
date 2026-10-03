      *> reject-at: 2002 2014 2023
      *> 7.3.3 SR8 b): a compiler directive is not specified within a
      *> source text manipulation statement. The >>DEFINE below stands
      *> inside the REPLACE statement, between BY and its second
      *> pseudo-text (kb/Work PB1384).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1384R.
000300 PROCEDURE DIVISION.
000400     REPLACE ==AA== BY
000500 >>DEFINE VV AS 1
000600         ==BB==.
000700     DISPLAY "AA".
000800     STOP RUN.
