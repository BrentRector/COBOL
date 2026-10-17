      *>--- conformance-case
      *> id: pb3168-continuation-area-a-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3168 - ANSI X3.23-1985: "Area A of a continuation
      *> line must be blank"; ISO 2002 removed the areas (6.3.1). The
      *> opening quotation symbol of the continuation line below stands
      *> in column 11, area A, so at --std 85 it is COBOLNET3735 (the
      *> COBOL-2002 positive is 2002/pb3168_continuation_area_a).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB3168NB.
000300 PROCEDURE DIVISION.
000400 P1.
000500     DISPLAY "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ
000600-   "JK".
000700     STOP RUN.
