      *> kb/Work PB1492 - the cross-line literal scan ends a literal
      *> only at the quotation symbol that OPENED it. 8.3.5 5): 'The
      *> closing delimiters of literals are: a quotation mark when the
      *> opening delimiter uses a quotation mark; an apostrophe when
      *> the opening delimiter uses an apostrophe' - so an apostrophe
      *> inside a quotation-mark literal (and the reverse) is content,
      *> also on the line a fixed continuation indicator continues.
      *> 6.3.5 2): the spaces at the end of the continued line are part
      *> of the literal.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1492OQ.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 X PIC X(80) VALUE "IT'S A                                     
000600-    "BC".
000700 01 Y PIC X(80) VALUE 'SAY "HI                                    
000800-    'THERE'.
000900 PROCEDURE DIVISION.
001000     DISPLAY "[" X "]".
001100     DISPLAY "[" Y "]".
001200     STOP RUN.
