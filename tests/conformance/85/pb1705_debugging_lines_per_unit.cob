      *> kb/Work PB1705 (owner decision R56), PB1088 - the clause
      *> belongs to a SOURCE UNIT. ISO 12.3.5.4 GR1: all clauses of
      *> the SOURCE-COMPUTER paragraph apply to the source unit in
      *> which they are specified and to any source unit contained
      *> within it. So the container's debugging line runs, the line
      *> of the program CONTAINED in it runs (12.3.3 SR1: the
      *> configuration section shall not be specified in a program
      *> contained within another program, so inheritance is its only
      *> route), and the line of the separate top-level program that
      *> names no clause is a comment. Expected output:
      *> MAIN, MAIN-DEBUG, INNER, INNER-DEBUG, SIBLING (and no
      *> SIBLING-DEBUG).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1705B.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
000600 PROCEDURE DIVISION.
000700 P-MAIN.
000800     DISPLAY "MAIN".
000900D    DISPLAY "MAIN-DEBUG".
001000     CALL "PB1705C".
001100     CALL "PB1705D".
001200     STOP RUN.
001300 IDENTIFICATION DIVISION.
001400 PROGRAM-ID. PB1705C.
001500 PROCEDURE DIVISION.
001600 P-INNER.
001700     DISPLAY "INNER".
001800D    DISPLAY "INNER-DEBUG".
001900     EXIT PROGRAM.
002000 END PROGRAM PB1705C.
002100 END PROGRAM PB1705B.
002200 IDENTIFICATION DIVISION.
002300 PROGRAM-ID. PB1705D.
002400 PROCEDURE DIVISION.
002500 P-SIB.
002600     DISPLAY "SIBLING".
002700D    DISPLAY "SIBLING-DEBUG".
002800     EXIT PROGRAM.
002900 END PROGRAM PB1705D.
