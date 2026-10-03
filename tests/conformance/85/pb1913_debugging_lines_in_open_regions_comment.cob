      *> kb/Work PB1913, owner decisions R56 and R61 - the twin of
      *> pb1913_debugging_lines_in_open_regions WITHOUT the
      *> SOURCE-COMPUTER clause: every debugging line is a comment, so
      *> the PICTURE string is the one the line after it writes (X(2),
      *> holding "AB") and each subscript is the one written without
      *> its debugging line: W-E(1) = 1 and W-E(W-I) = 1. Had the D
      *> line of the PICTURE been read as source the clause would be
      *> PIC X(5) X(2), a syntax error.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1913B.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. IBM-PC.
000600 DATA DIVISION.
000700 WORKING-STORAGE SECTION.
000800 01  W-T PIC 9(3) VALUE 123.
000900 01  W-TR REDEFINES W-T.
001000     05  W-E PIC 9 OCCURS 3.
001100 01  W-PIC PIC
001200D        X(5)
001300         X(2) VALUE "AB".
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
