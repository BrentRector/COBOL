      *> reject-at: 2002 2014 2023
      *> kb/Work PB1441 - a compiler-directive operand's literal is
      *>  lexed apart from the compilation group, so the unit's literal
      *>  screen never sees it; the compile-time evaluator asks the same
      *>  8.3.3 rules through the one CobolLiteral.SyntaxViolation. ISO
      *>  8.3.3.2.3 SR5: "Hex-character-sequence-1 shall be composed of
      *>  hexadecimal digits". X"GG" as a >>DEFINE value is refused by
      *>  name (COBOLNET1619 naming SR5) instead of decoding to an empty
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
