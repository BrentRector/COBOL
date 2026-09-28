      *> reject-at: 2002 2014 2023
      *> A 31-DIGIT NUMERIC LITERAL AS THE CURRENCY SIGN (kb/Work PB1579).
      *> ISO/IEC 1989:2023 §12.3.7.3 SR18: "Literal-7 shall be an alphanumeric or
      *> national literal that is not a figurative constant", and §8.3.3.3.1:
      *> "Numeric literals are of the class and category numeric" — so this is
      *> refused as a NUMERIC literal (COBOLNET0892). Before PB1579 the class was
      *> decided by int.TryParse || decimal.TryParse over the text, and a literal
      *> too long for System.Decimal read as "not numeric", fell into the
      *> alphanumeric path and was refused under the wrong rule (SR22, "shall
      *> consist of a single character"). 31 digits is the §8.3.3.3.2 maximum,
      *> from COBOL 2002 on.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1579CS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS 1234567890123456789012345678901.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
