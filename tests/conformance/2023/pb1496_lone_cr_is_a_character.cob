*> options: source-format=free
*> ISO/IEC 1989:2023 6.1 3) b): the implementor specifies the control characters that
*> terminate a free-form line and whether they may appear in comments and literals.
*> DOC-A.1-156: LINE FEED, or CR LF, terminates a line; a CR NOT followed by a LINE FEED is a
*> character of the line (GnuCOBOL's reader). kb/Work PB1496: the lexer ended a comment and
*> a literal at a lone CR, the directive stage did not.
*> C: the lone CR in the comment below does not end it - DISPLAY "HIDDEN" is comment-text.
*> L: the lone CR in the literal is its second character, so F(2:1) = X"0D".
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1496CR.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 F PIC X(3) VALUE "AB".
PROCEDURE DIVISION.
    *> a commentDISPLAY "HIDDEN".
    IF F(2:1) = X"0D" DISPLAY "CR-KEPT" END-IF.
    DISPLAY "END".
    STOP RUN.
