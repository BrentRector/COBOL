      *> reject-at: 85 2002 2014 2023
      *> A SIGN SEPARATED FROM ITS DIGITS IN A LEVEL-88 VALUE (kb/Work PB1445).
      *> ISO/IEC 1989:2023 §8.3.3.3.2 2): "If a sign is used, it shall appear as the
      *> leftmost character of the literal", and a space is a separator (§8.3.5 1)), so
      *> `- 5` is a sign and then the literal 5 — not the literal -5 — and a VALUE
      *> position admits no expression. It used to be taken silently as -5 (C1 true for
      *> B = -5); now COBOLNET2155.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1445CV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC S9.
          88 C1 VALUE - 5.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
