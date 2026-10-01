      *> kb/Work PB1359 - the FLOATING literal continuation indicator,
      *> fixed form. THE RULES (each run through cite.py --check):
      *> 6.2.3.1: the indicator is a quotation symbol and a hyphen,
      *> 'continuation of a literal when specified in an unterminated
      *> literal with the same quotation symbol in its opening
      *> delimiter'. 6.5 4): 'the end of the program text area is set
      *> to immediately follow the character preceding the continuation
      *> indicator'. 6.5 8): the continuation line's content,
      *> 'beginning with the first character after the initial
      *> quotation symbol, is appended immediately to the right of the
      *> last character in the latest logical line'. 6.3.5: 'Comment
      *> lines and blank lines may be interspersed among lines
      *> containing the parts of a literal'; 'National literals may be
      *> continued only with a floating literal continuation
      *> indicator'. W-A: ABC + DEF = ABCDEF. W-B: an apostrophe
      *> literal with a doubled apostrophe, a comment line and a blank
      *> line between the parts. W-N: a national literal. W-X:
      *> hexadecimal 4142 + 43 = ABC. W-S: the spaces BEFORE the
      *> indicator are literal content. REPLACE: a literal continued
      *> inside pseudo-text (7.2.4.3 8): pseudo-text may be continued
      *> by the rules of reference format).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1359FX.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 W-A PIC X(20) VALUE "ABC"-
000600     "DEF".
000700 01 W-B PIC X(12) VALUE 'IT''S'-
000800*   a comment line between the parts
000900
001000     ' OK'.
001100 01 W-N PIC N(6) VALUE N"AB"-
001200     "CD".
001300 01 W-X PIC X(3) VALUE X"4142"-
001400     "43".
001500 01 W-S PIC X(12) VALUE "AB  "-
001600     "CD".
001700 PROCEDURE DIVISION.
001800     DISPLAY "[" W-A "]".
001900     DISPLAY "[" W-B "]".
002000     IF W-N = N"ABCD" DISPLAY "N-OK".
002100     DISPLAY "[" W-X "]".
002200     DISPLAY "[" W-S "]".
002300     REPLACE ==XX1== BY ==MOVE "HELLO-WO"-
002400         "RLD" TO W-A==.
002500     XX1.
002600     DISPLAY "[" W-A "]".
002700     STOP RUN.
