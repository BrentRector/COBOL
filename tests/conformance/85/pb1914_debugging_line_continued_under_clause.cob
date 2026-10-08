      *> kb/Work PB1914 - the continuation of a DEBUGGING line belongs to it. With WITH DEBUGGING MODE the
      *> debugging line is source (docs/CONFORMANCE.md D-DEBUG), so the continuation line after it continues
      *> it (6.5 6)): the literal's second part CC joins the first. Only a literal left open by the line
      *> BEFORE a debugging line gives its continuation to that line instead (kb/Work PB1914).
000010 IDENTIFICATION DIVISION.
000020 PROGRAM-ID. PB1914B.
000030 ENVIRONMENT DIVISION.
000040 CONFIGURATION SECTION.
000050 SOURCE-COMPUTER. SYS-A WITH DEBUGGING MODE.
000060 PROCEDURE DIVISION.
000070     DISPLAY "ONE".
000080D    DISPLAY "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC
000090-    "CC".
000100     DISPLAY "TWO".
000110     STOP RUN.
