      *> kb/Work PB2739 - the division a text stands in is read from its
      *> text-words wherever they begin on a line (6.3.1: the program-text
      *> area has no Area A / Area B line), so a PROCEDURE DIVISION header
      *> written in column 12 leaves the IDENTIFICATION DIVISION and the
      *> paragraph named REMARKS after it is an ordinary paragraph, not
      *> the obsolete comment-entry paragraph.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB2739IH.
000300     PROCEDURE DIVISION.
000400 MAIN-PARA.
000500     PERFORM REMARKS.
000600     STOP RUN.
000700 REMARKS.
000800     DISPLAY "IN-REMARKS".
