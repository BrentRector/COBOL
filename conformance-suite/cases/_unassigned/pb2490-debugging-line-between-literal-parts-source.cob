      *>--- conformance-case
      *> id: pb2490-debugging-line-between-literal-parts-source
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2490 - the SOURCE-COMPUTER paragraph below says WITH DEBUGGING MODE, so the debugging
      *> line (indicator D) between the parts of the continued literal is a SOURCE line, not a comment line.
      *> ISO/IEC 1989:2023 6.3.5 2) lets only 'Comment lines and blank lines' be interspersed among lines
      *> containing the parts of a literal; GnuCOBOL 3.2.0 with -fdebugging-line rejects the program
      *> ('continuation character expected'). The same program without the clause is the positive golden
      *> 85/pb1914_debugging_line_between_literal_parts (the line is a comment and the literal is joined).
000010 IDENTIFICATION DIVISION.
000020 PROGRAM-ID. PB2490N.
000030 ENVIRONMENT DIVISION.
000040 CONFIGURATION SECTION.
000050 SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
000060 DATA DIVISION.
000070 WORKING-STORAGE SECTION.
000080 01  V PIC X(80).
000090 PROCEDURE DIVISION.
000100     MOVE "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
000110D    DISPLAY "DEBUG-LINE".
000120-    "TAIL" TO V.
000130     DISPLAY V.
000140     STOP RUN.
