      *> reject-at: 2023
      *> 7.3.11.4 GR2: following a DEFINE with the OFF phrase the name
      *> shall not be used except in a defined condition; >>DISPLAY X is
      *> a use of it (kb/Work PB1368).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1368D.
000300 >>DEFINE X AS 1
000400 >>DEFINE X OFF
000500 PROCEDURE DIVISION.
000600 >>DISPLAY X
000700     STOP RUN.
