      *> reject-at: 2002 2014 2023
      *> kb/Work PB1942 - a constant-name stands for its literal (13.10.3 SR2; 13.10.4 GR1 "as if literal-1 ...
      *> were written where constant-name-1 is written"), so it meets the rules of the literal it stands for:
      *> 12.3.7.3 SR18 "Literal-7 shall be an alphanumeric or national literal that is not a figurative
      *> constant", and KN stands for the NUMERIC literal 5. The constant-name is admitted at the position
      *> (conformance/2002/pb1942_special_names_constant_literals) and refused by the literal's own rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1942NUM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS KN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KN CONSTANT AS 5.
       PROCEDURE DIVISION.
           DISPLAY KN
           STOP RUN.
