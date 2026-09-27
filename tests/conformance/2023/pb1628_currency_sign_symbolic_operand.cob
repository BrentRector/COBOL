      *> kb/Work PB1628 - CURRENCY SIGN literal-7 may be a concatenation
      *> expression whose operand is a symbolic-character declared in the
      *> same SPECIAL-NAMES paragraph, even when the SYMBOLIC CHARACTERS
      *> clause is written after it. 8.8.3.3 GR3: a concatenation expression
      *> "may be used anywhere a literal of that class may be used";
      *> 12.3.7.3 SR18 bars only a figurative constant AS literal-7, and SR11
      *> (no symbolic-character) names literal-1..6 and 9, not literal-7.
      *> SYM-D IS 37 is ordinal 37 of the native alphanumeric set: "$".
      *> The currency string is "US$", inserted at the fixed position of
      *> the PICTURE SYMBOL "$". Before the fix: COBOLNET2473.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1628C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "US" & SYM-D PICTURE SYMBOL "$"
           SYMBOLIC CHARACTERS SYM-D IS 37.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AMT PIC $9(3).99 VALUE 12.5.
       PROCEDURE DIVISION.
           DISPLAY "[" AMT "]"
           STOP RUN.
