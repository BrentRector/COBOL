*>--- conformance-case
*> id: pb866_float_edited_is_form_comma
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB866 - the IS-form EDITING literal under DECIMAL-POINT IS COMMA. 13.18.40.2 SR13
      *> exchanges the ROLES of the symbols '.' and ',', never the characters of a literal: the
      *> literal "." inserted at T stays a period while the ',' symbol is the decimal point.
      *> 12,345 => "+1.2,345E+00"; the de-edit returns 12,345 exactly => +0012,3450 (DISPLAY of a
      *> numeric item shows its digits, not a point).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB866FLOATEDITEDISFORMCOMMA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FT PIC +9T9,9(3)E+99 EDITING T IS ".".
       01 N  PIC S9(4)V9(4) SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
           MOVE 12,345 TO FT
           DISPLAY "FT=[" FT "]"
           MOVE FT TO N
           DISPLAY "N=[" N "]"
           STOP RUN.
