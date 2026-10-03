      *> kb/Work PB1705, owner decisions R56 and R61 - the COBOL-85
      *> debug module: a D in the indicator area makes a debugging line,
      *> and WITH DEBUGGING MODE in the SOURCE-COMPUTER paragraph makes
      *> every debugging line of the program SOURCE (without the clause
      *> it is a comment, golden pb1494_debugging_line_85). The lines
      *> run here, in the DATA DIVISION (the D item is declared) and in
      *> the PROCEDURE DIVISION, where two consecutive debugging lines
      *> carry one statement. Each leg can fail on its own: the item
      *> missing makes W-DBG unresolved, a line kept as a comment drops
      *> its DISPLAY.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1705A.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
000600 DATA DIVISION.
000700 WORKING-STORAGE SECTION.
000800 01  W-LIVE PIC X(4) VALUE "LIVE".
000900D01  W-DBG  PIC X(5) VALUE "DEBUG".
001000 PROCEDURE DIVISION.
001100     DISPLAY "A".
001200D    DISPLAY W-DBG " " W-LIVE.
001300D    DISPLAY "LINE "
001400D        "TWO".
001500     DISPLAY "B".
001600     STOP RUN.
