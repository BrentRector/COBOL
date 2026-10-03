      *> reject-at: 2002 2014 2023
      *> kb/Work PB791 - ISO 12.3.7.3 SR18 in the PICTURE SYMBOL form: "Literal-7 shall be an alphanumeric or national
      *> literal that is not a figurative constant" (cite.py --check 12.3.7.3 "Literal-7 shall be an alphanumeric or
      *> national literal that is not a figurative constant"). This is the form that was a WRONG ANSWER: the bare form
      *> was refused only by accident (SR22's length rule), but with PICTURE SYMBOL the word SPACE decoded to its own
      *> spelling and bound the currency string "SPACE" - PICTURE #9.99 then displayed SPACE1.50. SR18 refuses it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB791B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS SPACE PICTURE SYMBOL "#".
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
