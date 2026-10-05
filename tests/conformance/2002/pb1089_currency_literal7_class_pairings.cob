      *> kb/Work PB1089 - 12.3.7.3 SR28's CONFORMING pairings. "If literal-7 is of class alphanumeric, the associated
      *> currency symbol may be used only to define a numeric-edited item with usage display. If literal-7 is of
      *> class national, the associated currency symbol may be used only to define a numeric-edited item with
      *> usage national." Each symbol below is used only the way its literal-7's class allows:
      *>   '#' = N"EUR"  national      -> EN, a USAGE NATIONAL numeric-edited item
      *>   'U' = "USD"   alphanumeric  -> UD, a USAGE DISPLAY numeric-edited item
      *>   '$'  (SR25's implied CURRENCY SIGN '$' PICTURE SYMBOL '$', an alphanumeric literal-7) -> DD, display
      *> 13.18.40.4 GR14 puts the currency STRING where the symbol stands, so 1.5 in a picture of one fixed
      *> insertion symbol, one integer digit, a point and two decimals reads EUR1.50 / USD1.50 / $1.50. The national
      *> item is displayed through its alphanumeric image, so it prints the same characters.
      *> Introduced in COBOL-2002 (the PICTURE SYMBOL phrase and national literals).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1089POS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS N"EUR" WITH PICTURE SYMBOL "#"
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EN PIC #9.99 USAGE NATIONAL.
       01 UD PIC U9.99.
       01 DD PIC $9.99.
       PROCEDURE DIVISION.
           MOVE 1.5 TO EN UD DD.
           DISPLAY "EN=[" EN "] UD=[" UD "] DD=[" DD "]".
           STOP RUN.
