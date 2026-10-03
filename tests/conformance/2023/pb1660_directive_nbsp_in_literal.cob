      *> kb/Work PB1660 - ISO 8.3.5 1): the COBOL character space is a
      *> separator, and the only one. A no-break space (U+00A0) inside the
      *> literal of a directive operand is literal CONTENT (8.3.3.1: the
      *> characters of which the literal is composed), never a
      *> separator between two words, so >>DEFINE reads ONE literal and
      *> >>IF compares it equal to the same literal. Runs of ordinary spaces
      *> around the words still separate them. A split on the no-break space
      *> would print NBSP-SPLIT.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1660A.
000300 >>DEFINE   LBL   AS   "A B"   OVERRIDE
000400 PROCEDURE DIVISION.
000500 >>IF LBL = "A B"
000600     DISPLAY "NBSP-IS-CONTENT".
000700 >>ELSE
000800     DISPLAY "NBSP-SPLIT".
000900 >>END-IF
001000     STOP RUN.
