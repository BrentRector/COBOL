      *> kb/Work PB1914 - a debugging line written between the parts of a continued literal.
      *> The SOURCE-COMPUTER paragraph has no WITH DEBUGGING MODE, so a debugging line is a comment line
      *> (docs/CONFORMANCE.md D-DEBUG) and 'Comment lines and blank lines may be interspersed among lines
      *> containing the parts of a literal' (ISO/IEC 1989:2023 6.3.5 2)): the continuation line after the
      *> first debugging line continues the MOVE's literal, giving A...ATAIL. The second debugging line is
      *> itself continued, and the whole of it is the comment.
000010 IDENTIFICATION DIVISION.
000020 PROGRAM-ID. PB1914A.
000030 DATA DIVISION.
000040 WORKING-STORAGE SECTION.
000050 01  V PIC X(80).
000060 PROCEDURE DIVISION.
000070     MOVE "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
000080D    DISPLAY "DEBUG-LINE".
000090-    "TAIL" TO V.
000095     DISPLAY V.
000600D    DISPLAY "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC
000110-    "D".
000120     DISPLAY "END".
000130     STOP RUN.
