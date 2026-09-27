      *> kb/Work PB1491 - fixed-form literal continuation joins the
      *>   LATEST LOGICAL LINE; comment and blank lines interspersed
      *>   among the parts of a literal are logically discarded.
      *> THE RULES (each run through cite.py --check):
      *>   §6.3.5 "Comment lines and blank lines may be interspersed
      *>     among lines containing the parts of a literal." and "the
      *>     next line that is not a comment line or a blank line is the
      *>     continuation line" -> OK §6.3.5 2).
      *>   §6.3.5 "any spaces at the end of the fixed-form continued
      *>     line are part of the literal" -> OK §6.3.5 2).
      *>   §6.5 2) "If the line is a comment line or a blank line, that
      *>     line is logically discarded." -> OK §6.5 2).
      *>   §6.5 6) a) "... is appended immediately to the right of the
      *>     last character in the latest logical line of the resultant
      *>     compilation group." -> OK §6.5 6) a).
      *>   §6.3.6 "A blank line is one that contains only space
      *>     characters between margin C and margin R." -> OK §6.3.6.
      *> DOC-A.1-157: a line shorter than margin R (column 72) is read
      *>   as space-filled to it, so W-B and W-C carry every position
      *>   from the end of their record through column 72.
      *> W-A: across a * comment line and a / comment line whose text
      *>   holds unbalanced quotation symbols (the literal fills
      *>   exactly to column 72, so only the join is under test).
      *> W-B: across an EMPTY line and a sequence-number-only line.
      *>   Before PB1491 this CRASHED the compiler (origin tracking).
      *> W-C: a short record continued with nothing between.
      *> W-D: continued TWICE, a comment line before each join.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1491LL.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500  01 W-A PIC X(80) VALUE "ALPHAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
000600* a comment line with a "quote and an 'apostrophe
000700/ a page-eject comment line "
000800-    "OMEGA".
000900  01 W-B PIC X(80) VALUE "BRAVO

000950
001100-    "ZULU".
001200  01 W-C PIC X(80) VALUE "AB
001300-    "CD".
001400  01 W-D PIC X(160) VALUE "D1
001500* first interspersed comment
001600-    "D2
001700* second interspersed comment
001800-    "D3".
001900 PROCEDURE DIVISION.
002000     DISPLAY "[" W-A "]".
002100     DISPLAY "[" W-B "]".
002200     DISPLAY "[" W-C "]".
002300     DISPLAY "[" W-D "]".
002400     STOP RUN.
