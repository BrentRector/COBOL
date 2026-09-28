      *> reject-at: 2002 2014 2023
      *> AN INTEGER CONSTANT-NAME BEYOND THE IMPLEMENTATION LIMIT AS AN OCCURS
      *> BOUND (kb/Work PB1579). ISO/IEC 1989:2023 §13.10.4 GR1: "the effect of
      *> specifying constant-name-1 in other than this entry is as if literal-1
      *> ... were written where constant-name-1 is written", so OCCURS K is
      *> OCCURS 77777777777 — and that written literal is COBOLNET2427, beyond the
      *> 2,147,483,647 this implementation lays out (ISO §4.5; docs/CONFORMANCE.md
      *> §3 "Integer operands and host carriers"). The substituted constant meets
      *> the same verdict. Before PB1579 the constant's integer text was read by
      *> int.TryParse and K was refused as "not an INTEGER constant-name" — a
      *> false sentence (§13.10.4 GR2: K's class and category are literal-1's,
      *> and literal-1 is an integer literal).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1579KB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS 77777777777.
       01 T.
          05 E PIC X OCCURS K.
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
