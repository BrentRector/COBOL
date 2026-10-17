      *>--- conformance-case
      *> id: w73a-directive-hex-literal-non-hex-digit
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1441 - a compiler-directive operand's literal is
      *>  lexed apart from the compilation group, so the unit's literal
      *>  screen never sees it; since kb/Work PB1616 the directive
      *>  stage's fragment funnels ask LiteralScreen, the unit's own
      *>  body, for the 8.3.3 rules. ISO
      *>  8.3.3.2.3 SR5: "Hex-character-sequence-1 shall be composed of
      *>  hexadecimal digits". X"GG" as a >>DEFINE value is refused by
      *>  name (COBOLNET2630 naming SR5) instead of decoding to an empty
      *>  value that >>IF would then compare.
       >>DEFINE V AS X"GG"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGW73ADIR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X VALUE "A".
       PROCEDURE DIVISION.
           >>IF V = "A"
           DISPLAY "EQ"
           >>ELSE
           DISPLAY "NE"
           >>END-IF
           STOP RUN.
