      *> reject-at: 2002 2014 2023
      *> ISO 12.3.7.3 SR14 b1 (cite.py --check 12.3.7.3 "Each numeric literal
      *> shall be an unsigned integer and shall have a value within the range
      *> of one through the maximum number of characters in the native
      *> alphanumeric character set" -> OK 12.3.7.3 14) 1.). The native set has
      *> 65,536 characters, so the ordinal 99999999999 does not exist in it.
      *>
      *> kb/Work PB1091: the literal-phrase decoder asked int.TryParse for the
      *> ordinal, so an integer too long for a 32-bit int was "not an integer"
      *> and fell to the noninteger-literal CLASS rule (SR14 b2) - a diagnostic
      *> false about the source. The ordinal has one reader now, and the .err
      *> pins the ORDINAL rule's own diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1091ALPHA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AX IS 99999999999.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
