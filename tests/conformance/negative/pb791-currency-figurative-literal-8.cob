      *> reject-at: 2002 2014 2023
      *> kb/Work PB791 - ISO 12.3.7.3 SR26: "Literal-8 shall be an alphanumeric or national literal consisting of a
      *> single character ... It shall be neither a figurative constant nor a hexadecimal literal" (cite.py --check
      *> 12.3.7.3 "It shall be neither a figurative constant nor a hexadecimal literal"). QUOTE is a figurative
      *> constant (one character, the quotation mark - which SR27 would forbid as a symbol anyway); the screen answers
      *> SR26 first because it asks what the operand IS as written, not what it decodes to.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB791C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN "USD" PICTURE SYMBOL QUOTE.
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
