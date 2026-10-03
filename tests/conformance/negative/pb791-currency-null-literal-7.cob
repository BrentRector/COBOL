      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB791 - NULL is no literal at all (ISO 8.4.3.1.2 Format 8 predefined-address; 8.4.3.10.3 SR1: it may be
      *> used only as a sending operand in an INITIALIZE or a SET statement, as an argument in a program-prototype
      *> CALL, ..., or in a pointer-or-object-reference relation condition; cite.py --check 8.4.3.10.3 "it may be used
      *> only as a sending operand in an INITIALIZE or a SET statement"). A CURRENCY SIGN literal-7 is none of those, so
      *> the one SR1 refusal (COBOLNET2576) answers it - it used to draw SR22's length rule over the five letters N U L L.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB791E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS NULL.
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
