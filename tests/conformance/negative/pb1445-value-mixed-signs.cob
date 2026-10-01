      *> reject-at: 85 2002 2014 2023
      *> ISO 8.3.3.3.2 2): "A literal shall not contain more than one sign character. If a sign is
      *> used, it shall appear as the leftmost character of the literal." A literal is ONE
      *> character-string and a space is a separator (8.3.5 1)), so a sign separated from its digits,
      *> or a second sign, is not part of any literal (kb/Work PB1445).
      *> NEGATIVE: a VALUE literal with two DIFFERENT signs (`+-5`).
      *> Not a literal, so refused COBOLNET2155 at every edition (the rule has no introducing edition).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1445N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC S9 VALUE +-5.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
