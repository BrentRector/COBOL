      *> reject-at: 85 2002 2014 2023
      *> ISO 8.3.3.3.2 2): "A literal shall not contain more than one sign character. If a sign is
      *> used, it shall appear as the leftmost character of the literal." A literal is ONE
      *> character-string and a space is a separator (8.3.5 1)), so a sign separated from its digits,
      *> or a second sign, is not part of any literal (kb/Work PB1445).
      *> NEGATIVE: an ADD literal whose sign is separated (`ADD - .5`).
      *> Not a literal, so refused COBOLNET2155 at every edition (the rule has no introducing edition).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1445N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC S9V9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD - .5 TO B
           STOP RUN.
