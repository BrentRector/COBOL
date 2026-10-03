      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB791 - ISO 12.3.7.3 SR18: "Literal-7 shall be an alphanumeric or national literal that is not a
      *> figurative constant" (cite.py --check 12.3.7.3 "Literal-7 shall be an alphanumeric or national literal that
      *> is not a figurative constant"). SPACE is a figurative constant, so the clause is refused UNDER SR18. It used
      *> to draw SR22's one-character rule, a message false about the source (LiteralCharsOf decoded the word SPACE
      *> as its own five-letter spelling).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB791A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS SPACE.
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
