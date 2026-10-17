      *>--- conformance-case
      *> id: l1c06_currency_non_cobol_equivalence_85
      *> rules: [DOC-A.1-43]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.3 SR20 (A.1 item 43) — equivalence of a non-COBOL
      *>   currency symbol, bare form
      *> Rule: "If the character specified as the currency symbol is not
      *>   a character in the
      *> COBOL character repertoire, any equivalence between that
      *>   character and any other
      *> character in the computer's compile-time coded character set is
      *>   defined by the
      *> implementor."
      *>   cite.py --check 12.3.7.3 "any equivalence between that
      *>     character and any other
      *>   character in the computer's compile-time coded character set
      *>     is defined by the
      *>   implementor"  -> OK  §12.3.7.3 20)  (Syntax rules)
      *> Implementor definition (docs/CONFORMANCE.md row DOC-A.1-43): a
      *>   non-COBOL currency
      *> symbol is equivalent to exactly the characters with the same
      *>   .NET invariant
      *> uppercase mapping, so 'é' (U+00E9) and 'É' (U+00C9) are
      *>   equivalent.
      *> Without PICTURE SYMBOL the literal is both currency string and
      *>   symbol:
      *>   cite.py --check 12.3.7.3 "If the PICTURE SYMBOL phrase is not
      *>     specified,
      *>   literal-7 is both the currency string and the currency
      *>     symbol."
      *>   -> OK  §12.3.7.3 22)  (Syntax rules)
      *> Floating insertion needs equivalent occurrences (§13.18.40.3
      *>   SR28):
      *>   cite.py --check 13.18.40.3 "all occurrences of the currency
      *>     symbol within
      *>   character-string-1 shall be equivalent characters"  -> OK
      *>     §13.18.40.3 28)
      *>   cite.py --check 13.18.40.5 "a single occurrence of the
      *>     replacement
      *>   character(s) is (are) placed into the character position(s)
      *>     immediately
      *>   preceding"  -> OK  §13.18.40.5 6)  (Editing rules)
      *> Derivation of each expected line:
      *>   UP=[é1.50]   PIC É9.99: the upper-case 'É' is the currency
      *>     symbol by
      *>                equivalence; 1.5 edits as 1.50 and the currency
      *>                  STRING, "é"
      *>                as written, is inserted: é1.50. (Were 'É' not
      *>                  equivalent, the
      *>                PICTURE would be illegal and the program would
      *>                  not compile.)
      *>   LO=[é1.50]   PIC é9.99, the symbol as written: the same
      *>     image.
      *>   FL=[ é2.50]  PIC Éé9.99: the mixed pair is a floating
      *>     insertion string of
      *>                two equivalent symbols (SR28 satisfied); the
      *>                  second is the
      *>                leftmost digit position, 2.5 -> digits 2.50, so
      *>                  one "é" lands
      *>                immediately before the '2' and the position
      *>                  before it is a space.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "é".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-UP PIC É9.99.
       01 WS-LO PIC é9.99.
       01 WS-FL PIC Éé9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1.5 TO WS-UP.
           MOVE 1.5 TO WS-LO.
           MOVE 2.5 TO WS-FL.
           DISPLAY "UP=[" WS-UP "]".
           DISPLAY "LO=[" WS-LO "]".
           DISPLAY "FL=[" WS-FL "]".
           STOP RUN.
