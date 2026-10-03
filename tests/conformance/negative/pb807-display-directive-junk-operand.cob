      *> reject-at: 2023
      *> 7.3.12.2: the operand is an arithmetic expression, a boolean
      *> expression, a literal or a PARAMETER phrase; ")( 3 +" is none of
      *> the four, so it is refused (kb/Work PB807).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB807J.
000300 PROCEDURE DIVISION.
000400 >>DISPLAY )( 3 +
000500     STOP RUN.
