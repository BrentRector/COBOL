      *> kb/Work PB791 - ISO 12.3.7.3 SR18/SR19/SR23/SR26: what a CURRENCY SIGN clause's literals may be.
      *> The CURRENCY SIGN clause's literal-7 is a currency STRING and its literal-8 a one-character currency SYMBOL.
      *> This program is the positive side of the SR18/SR26 screens (the figurative / boolean / numeric / hexadecimal
      *> refusals are the negative corpus, pb791-*): it proves the screens do not over-fire on what the rules admit.
      *>   SR23 (cite.py --check 12.3.7.3 "Literal-7 may have any length"): literal-7 "USD" is a three-character
      *>   string, the symbol "U" is literal-8 (SR27: U is not in the forbidden set), so PICTURE U9.99 edits 1.5 as
      *>   USD1.50 - the symbol is replaced by the whole STRING.
      *>   SR19 NOTE 1 (cite.py --check 12.3.7.3 "the limitations as to which characters are permitted in a currency
      *>   string at compile time do not apply to the alphanumeric or national characters represented by hexadecimal
      *>   literals used as currency strings"): X"2B" is the character '+', which SR23 b forbids in a currency string
      *>   written as an ordinary literal, but a hexadecimal literal-7 is interpreted at runtime as written, so
      *>   PICTURE #9.99 edits 1.5 as +1.50. SR19 itself requires the PICTURE SYMBOL phrase for it (present).
      *> What each line proves:
      *>   E1 - the multi-character currency string of an ordinary literal.
      *>   E2 - the hexadecimal literal-7 is accepted and its decoded character is the currency string.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB791POS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN "USD" PICTURE SYMBOL "U"
           CURRENCY SIGN X"2B" PICTURE SYMBOL "#".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E1 PIC U9.99.
       01  E2 PIC #9.99.
       PROCEDURE DIVISION.
           MOVE 1.5 TO E1
           MOVE 1.5 TO E2
           DISPLAY "E1=[" E1 "]"
           DISPLAY "E2=[" E2 "]"
           STOP RUN.
