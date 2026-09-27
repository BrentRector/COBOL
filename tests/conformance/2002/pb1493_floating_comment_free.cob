>>SOURCE FORMAT FREE
*> kb/Work PB1493 - free form: a comment line between PIC and its
*> character-string, *> directly followed by text (§6.2.3.2 1) "a space
*> is implied immediately following a floating comment indicator"), and
*> quotation symbols in comment-text; §6.5 2)/3) remove every comment
*> before any later stage (the lexer picture mode included) sees it.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1493FR.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-P PIC
*> a comment line between PIC and its character-string
   X(4) VALUE "PQRS". *>directly followed by comment-text
01 W-L PIC X(4) VALUE '*>''X'. *> a quote " in comment-text
PROCEDURE DIVISION.
    DISPLAY "[" W-P "]" *> an inline comment
    DISPLAY "[" W-L "]".
    STOP RUN.
