      *>--- conformance-case
      *> id: pb1494_comment_entry_in_copybook
      *> rules: [GR-6.5-5]
      *> accept: [85]
      *> files: [PB1494CE.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1494 - the converse: a copybook of COBOL-85 comment-entry
      *> paragraphs copied INTO the IDENTIFICATION DIVISION (the COPY
      *> statement stands there) keeps its paragraph headers and discards
      *> the entries, exactly as the same paragraphs written in the program
      *> do (accepted at 85, removed from 2002 - kb/Work R61).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1494CE.
000300     COPY PB1494CE.
000400 PROCEDURE DIVISION.
000500     DISPLAY "OK".
000600     STOP RUN.
