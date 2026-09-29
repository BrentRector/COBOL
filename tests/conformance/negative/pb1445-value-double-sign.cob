      *> reject-at: 85 2002 2014 2023
      *> TWO SIGN CHARACTERS ON A VALUE LITERAL (kb/Work PB1445).
      *> ISO/IEC 1989:2023 §8.3.3.3.2 2): "A literal shall not contain more than one sign
      *> character." Every VALUE format writes literal-n (§13.18.63.2) and admits no
      *> arithmetic expression, so `--5` is no literal. Until PB1445 the VALUE reader
      *> stripped any number of signs and this reached the C# backend as a decrement
      *> operator (CS1059, exit 70); now COBOLNET2155 names the rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1445DS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC S9 VALUE --5.
       PROCEDURE DIVISION.
           DISPLAY B
           STOP RUN.
