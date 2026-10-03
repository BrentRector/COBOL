      *> reject-at: 2002 2014 2023
      *> kb/Work PB791 - ISO 12.3.7.3 SR18: literal-7 "shall be an alphanumeric or national literal" (cite.py --check
      *> 12.3.7.3 "Literal-7 shall be an alphanumeric or national literal that is not a figurative constant"). A
      *> BOOLEAN literal is of neither class (8.3.3.4), but the screen asked only "is it numeric?", so B"1" reached the
      *> currency-string content rule and was refused there only because its decoded character is a digit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB791F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN B"1" PICTURE SYMBOL "#".
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
