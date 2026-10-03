      *> kb/Work PB1913, owner decisions R56 and R61 - the COBOL-85
      *> debug module (ISO 12.3.5.4 GR1 scopes WITH DEBUGGING MODE to the
      *> source unit). A debugging line that stands INSIDE an open
      *> subscript or an open PICTURE clause is source under the clause;
      *> the lexer used to skip it as a comment in those two modes. Each
      *> leg fails on its own: with the D line of the PICTURE dropped the
      *> compiler reads VALUE as the PICTURE string, and with the D line
      *> of a subscript dropped W-E(1) answers 1 where W-E(1 + 2) is 3.
      *> The second subscript is a general expression, which is re-read
      *> by the fragment parser (the D18 route) from the region's text.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1913A.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
000600 DATA DIVISION.
000700 WORKING-STORAGE SECTION.
000800 01  W-T PIC 9(3) VALUE 123.
000900 01  W-TR REDEFINES W-T.
001000     05  W-E PIC 9 OCCURS 3.
001100 01  W-PIC PIC
001200D        X(5)
001300         VALUE "ABCDE".
001400 01  W-I PIC 9 VALUE 1.
001500 01  W-R PIC 9.
001600 PROCEDURE DIVISION.
001700     MOVE W-E(1
001800D        + 2
001900         ) TO W-R.
002000     DISPLAY W-R.
002100     MOVE W-E(W-I
002200D        + 1
002300         ) TO W-R.
002400     DISPLAY W-R.
002500     DISPLAY W-PIC.
002600     STOP RUN.
