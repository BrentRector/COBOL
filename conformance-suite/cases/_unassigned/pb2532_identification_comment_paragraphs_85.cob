      *>--- conformance-case
      *> id: pb2532_identification_comment_paragraphs_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2532 - the COBOL-85 identification division prints
      *>   PROGRAM-ID, then AUTHOR, INSTALLATION, DATE-WRITTEN,
      *>   DATE-COMPILED and SECURITY, each in its own bracket with the
      *>   ellipsis on the comment-entry, never on the paragraph: each
      *>   paragraph at most once, in that sequence. DERIVED, NOT QUOTED
      *>   (the repository holds no ANSI X3.23-1985 text;
      *>   docs/VERSION_CHANGE_REFERENCE.md row 7.10). The positive
      *>   control: all five, once each, in the printed sequence, with a
      *>   comment-entry running over two lines, compile and run. The
      *>   negatives below 2002 are
      *>   negative/pb2532-comment-paragraph-repeated and
      *>   negative/pb2532-comment-paragraph-out-of-order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2532P.
       AUTHOR. A PROGRAMMER.
       INSTALLATION. THE INSTALLATION
           ON TWO LINES.
       DATE-WRITTEN. 13 OCTOBER 2026.
       DATE-COMPILED.
       SECURITY. NONE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PB2532 COMMENT PARAGRAPHS IN SEQUENCE".
           STOP RUN.
