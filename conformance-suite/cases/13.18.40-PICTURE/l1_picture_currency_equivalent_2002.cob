      *>--- conformance-case
      *> id: l1_picture_currency_equivalent_2002
      *> rules: [SR-13.18.40.3-28]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.40.3 SR28 PICTURE clause — "For floating insertion,
      *> when the currency symbol is used as the floating insertion
      *> symbol, all occurrences of the currency symbol within
      *> character-string-1 shall be equivalent characters."
      *>   python scripts/spec/cite.py --check 13.18.40.3 "For floating
      *>   insertion, when the currency symbol is used as the floating
      *>   insertion symbol, all occurrences of the currency symbol
      *>   within character-string-1 shall be equivalent characters."
      *>   -> OK  §13.18.40.3 28)  (Syntax rules)
      *>
      *> THE COBOL-2002 LEG of tests/conformance/2023/
      *> l1_picture_currency_equivalent.cob, which carries the full
      *> derivation. The row spans 85/2002/2014/2023 and the rule text
      *> is unchanged across them, but the DISTINGUISHABLE case needs a
      *> currency symbol that is a letter, and CURRENCY SIGN ... WITH
      *> PICTURE SYMBOL enters at COBOL-2002; at COBOL-85 the only
      *> currency symbol is '$', whose occurrences are always the same
      *> character. This fixture pins the earliest edition where the
      *> rule has content, so the equivalence cannot silently become a
      *> 2023-only behaviour.
      *>
      *> DERIVATION IN BRIEF. §13.18.40.3 SR3 -> §12.3.7.3 SR20 ->
      *> §8.1.3.2 GR3 make 'w' and 'W' equivalent basic letters, so the
      *> three occurrences form ONE floating insertion string
      *> (§13.18.40.5 rule 6). Its second symbol is the leftmost limit
      *> of the numeric data, so the digit positions are 2, 3 and the
      *> '9' at 4; 12 aligns as 012; rule 6a puts one occurrence of the
      *> currency string immediately preceding the first nonzero
      *> numeric character (position 3) and spaces before it. The
      *> inserted characters are literal-7 = "W" (§12.3.7.3 SR23).
      *> Hence [ W12], identically for the all-uppercase control.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PCEQ2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "W" WITH PICTURE SYMBOL "W".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MIX PIC wwW9.
       01 WS-UPP PIC WWW9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 12 TO WS-MIX.
           MOVE 12 TO WS-UPP.
           DISPLAY "MIX=[" WS-MIX "]".
           DISPLAY "UPP=[" WS-UPP "]".
           STOP RUN.
