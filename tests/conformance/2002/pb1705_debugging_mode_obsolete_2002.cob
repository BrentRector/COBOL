      *> kb/Work PB1705, R56 and R61 - WITH DEBUGGING MODE and the
      *> debugging lines it keeps are accepted at COBOL-85, OBSOLETE
      *> at COBOL-2002 (the obsolete-element warning COBOLNET0903 on
      *> the clause and on the line, never an error) and removed at
      *> 2014 (negative/debugging-mode, negative/pb1494-debugging-
      *> line-at-2014). At 2002 the clause still keeps the line: it
      *> runs.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1705F.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
000600 PROCEDURE DIVISION.
000700     DISPLAY "A".
000800D    DISPLAY "DEBUG-LINE".
000900     DISPLAY "B".
001000     STOP RUN.
