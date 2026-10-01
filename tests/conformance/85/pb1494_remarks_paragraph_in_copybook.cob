      *> kb/Work PB1494 - the obsolete comment-entry paragraphs (AUTHOR,
      *> INSTALLATION, DATE-WRITTEN, DATE-COMPILED, SECURITY, REMARKS) exist
      *> only in the IDENTIFICATION DIVISION. Library text is converted in
      *> the division its COPY statement stands in, so a PROCEDURE DIVISION
      *> paragraph named REMARKS, copied from a library, is an ordinary
      *> paragraph (6.5 5): its program text is copied, not discarded).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1494RM.
000300 PROCEDURE DIVISION.
000400 MAIN-PARA.
000500     PERFORM REMARKS.
000600     STOP RUN.
000700     COPY PB1494RM.
