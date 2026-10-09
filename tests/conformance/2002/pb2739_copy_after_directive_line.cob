      *> kb/Work PB2739 - the division a COPY statement stands in is a fact
      *> of the whole text before it, not of the block between two
      *> compiler directive lines: a COPY written after a >>DEFINE line in
      *> the PROCEDURE DIVISION still copies a paragraph named REMARKS as
      *> program text (6.5 5).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB2739DL.
000300 PROCEDURE DIVISION.
000400 MAIN-PARA.
000500 >>DEFINE PB2739 AS 1
000600     PERFORM REMARKS.
000700     STOP RUN.
000800     COPY PB2739DL.
