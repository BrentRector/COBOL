      *> reject-at: 2002 2014 2023
      *> 7.3.13.3 SR2: 'EVALUATE operand-1 shall begin on a new line and
      *> shall be specified entirely on that line.' The operand below is
      *> written as 1 on the directive line and + 1 on the next: the
      *> second line is program text between >>EVALUATE and its first
      *> >>WHEN, which 7.3.13.2 admits no text between. It is refused by
      *> name rather than read as part of the operand (kb/Work PB1360).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1360EV.
000300 PROCEDURE DIVISION.
000400 >>EVALUATE 1
000500 + 1
000600 >>WHEN 1
000700     DISPLAY "ONE".
000800 >>END-EVALUATE
000900     STOP RUN.
