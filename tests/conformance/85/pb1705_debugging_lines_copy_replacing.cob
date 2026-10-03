      *> kb/Work PB1705 (owner decision R56) - the COBOL-85 rule that
      *> a debugging line takes part in COPY REPLACING as if its D
      *> were absent, and that it is source under WITH DEBUGGING MODE:
      *> the debugging line of the copybook PB1705CP is rewritten by
      *> REPLACING (TAGX becomes W-TAG) and the REPLACED text runs.
      *> The plain line beside it runs either way.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1705E.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
000600 DATA DIVISION.
000700 WORKING-STORAGE SECTION.
000800 01  W-TAG PIC X(8) VALUE "REPLACED".
000900 PROCEDURE DIVISION.
001000 P-1.
001100     DISPLAY "A".
001200     COPY PB1705CP REPLACING ==TAGX== BY ==W-TAG==.
001300     DISPLAY "B".
001400     STOP RUN.
