      *> reject-at: 2023
      *> kb/Work PB1660 - ISO 8.3.5 1): the COBOL character space is the
      *> only separator, so the no-break space (U+00A0) in the operand
      *> below does not separate AS from 1: the operand is the one
      *> character-string AS(U+00A0)1, which is no compile-time expression.
      *> It was read as AS 1 and accepted.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1660N1.
000300 >>DEFINE FLAG AS 1
000400 PROCEDURE DIVISION.
000500     STOP RUN.
