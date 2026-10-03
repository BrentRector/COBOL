      *> kb/Work PB1705 (owner decision R56) - a debugging line may
      *> begin a paragraph: the paragraph name stands in Area A of the
      *> D line (the NIST debug-module test DB101A writes
      *> DDEBUG-LINE-TEST-03-A. this way). Under WITH DEBUGGING MODE
      *> the line is source, so DBG-P exists and GO TO reaches it;
      *> the marker the lexer reads in front of the line is no program
      *> text, so the name is still the first thing on its line. Each
      *> leg can fail: a paragraph name judged "not at line start"
      *> is a parse error, a line kept as a comment leaves GO TO
      *> without its target.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1705G.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
000600 PROCEDURE DIVISION.
000700 MAIN-P.
000800     DISPLAY "A".
000900     GO TO DBG-P.
001000 SKIPPED-P.
001100     DISPLAY "SKIPPED".
001200DDBG-P.    DISPLAY "DEBUG-PARAGRAPH".
001300     STOP RUN.
