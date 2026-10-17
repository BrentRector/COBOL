      *>--- conformance-case
      *> id: pb2532-comment-paragraph-out-of-order
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2532: the COBOL-85 identification division prints
      *> AUTHOR, INSTALLATION, DATE-WRITTEN, DATE-COMPILED and SECURITY
      *> in that sequence and no rule frees it (derived, not quoted: the
      *> repository holds no ANSI X3.23-1985 text). SECURITY written
      *> before AUTHOR compiled clean. From 2002 both paragraphs are
      *> removed constructs (COBOLNET0902).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2532NO.
       SECURITY. X.
       AUTHOR. A.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
