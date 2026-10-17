      *>--- conformance-case
      *> id: pb1384-directive-within-replace
      *> rules: [SR-7.2.4.3-10, SR-7.3.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
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
