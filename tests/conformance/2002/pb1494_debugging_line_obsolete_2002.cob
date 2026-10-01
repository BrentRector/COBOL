      *> kb/Work PB1494, R61 - a D in the indicator area is a debugging
      *> line: OBSOLETE at COBOL-2002 (the obsolete-element warning
      *> COBOLNET0903; kb/Work R61). Without WITH DEBUGGING MODE the
      *> line is compiled as a comment, so it never runs. It is removed
      *> at 2014 (negative/pb1494-debugging-line-at-2014).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1494D02.
000300 PROCEDURE DIVISION.
000400     DISPLAY "A".
000500D    DISPLAY "DEBUG".
000600     DISPLAY "B".
000700     STOP RUN.
