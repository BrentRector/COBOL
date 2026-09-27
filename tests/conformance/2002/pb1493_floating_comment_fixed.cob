      *> kb/Work PB1493 (+ PB1491 rule 3) - the floating comment
      *>   indicator is recognized in ONE place, the §6.5 logical
      *>   conversion, so no later stage sees comment-text.
      *> THE RULES (each run through cite.py --check):
      *>   §6.2.3.1: *> indicates "a comment line when specified as the
      *>     first character-string in the program-text area" -> OK.
      *>   §6.2.3.2 1) "a space is implied immediately following a
      *>     floating comment indicator" -> OK §6.2.3.2 1).
      *>   §6.5 3) "If the line contains an inline comment, the inline
      *>     comment is replaced by spaces and processing of that line
      *>     continues." -> OK §6.5 3) - on EVERY line, a continuation
      *>     line included.
      *>   §6.3.7.3 "All characters following the floating comment
      *>     indicator up to margin R are comment-text." -> OK.
      *> W-P: a * comment line AND a *> comment line between PIC and
      *>   its character-string (the lexer picture mode never sees
      *>   them). W-L: *> inside a literal is literal content. W-C: a
      *>   continuation line carrying an inline comment (with a quote
      *>   in it) that is itself continued - the period arrives on the
      *>   next continuation line. W-D: *> directly followed by text.
      *> REPLACE: an inline comment between the operands; BY is on the
      *>   next line, so the statement is complete.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1493FX.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500  01 W-P PIC
000600* a fixed comment line between PIC and its string
000700      *> a floating comment line there too
000800      X(4) VALUE "PQRS".
000900  01 W-L PIC X(6) VALUE "A*>B". *> literal content first
001000  01 W-C PIC X(80) VALUE "AB
001100-    "CD" *> an inline comment "with a quote
001200-    .
001300  01 W-D PIC X(3) VALUE 'D'. *>directly followed
001400 PROCEDURE DIVISION.
001500     REPLACE ==ZZZ== *> an inline comment in REPLACE
001600         BY ==W-P==.
001700     DISPLAY "[" ZZZ "]".
001800     DISPLAY "[" W-L "]".
001900     DISPLAY "[" W-C "]".
002000     DISPLAY "[" W-D "]". *> "an unbalanced quote
002100     STOP RUN.
