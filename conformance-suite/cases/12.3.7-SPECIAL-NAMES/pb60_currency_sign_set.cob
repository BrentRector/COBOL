*>--- conformance-case
*> id: pb60_currency_sign_set
*> rules: [SR-12.3.7.3-23, SR-12.3.7.3-25, SR-12.3.7.3-26, SR-12.3.7.3-27, GR-12.3.7.4-13, GR-13.18.40.4-14, AR-15.68.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB60 (AR-15.68.3-3) - the CURRENCY SIGN SET and the multi-character currency string. 12.3.7.3 r23:
      *> "If the PICTURE SYMBOL phrase is specified, literal-7 is the currency string and literal-8 is the
      *> associated currency symbol. Literal-7 may have any length"; r25: unless a clause names '$' as
      *> literal-7 or literal-8, "the clause CURRENCY SIGN '$' PICTURE SYMBOL '$' is implied for that source
      *> unit"; 13.18.40.4 GR14: "The first occurrence of the currency symbol adds the number of characters in
      *> the currency string to the size of the item. Each subsequent occurrence of the currency symbol adds
      *> one to the size of the item." Before this landing the binder held ONE symbol and ONE string per unit:
      *> a multi-character literal-7 was refused (COBOLNET0896 "not yet supported"), a second clause overwrote
      *> the first, and the implied '$' died the moment any clause bound another symbol.
      *> FU:  fixed insertion (13.18.40.5 r5) - PIC U9.99 with "USD" is 7 characters: USD5.25.
      *> FLU: floating insertion (r6a) - PIC UUU9 is 6 characters; the ONE rendered occurrence sits before the
      *>      first nonzero digit ( USD12 / USD123) or, for zero, before the '9' position (  USD0).
      *> FLZ: all-floating (r6b) - a zero value is ALL spaces at the physical width (6); 7 -> "  USD7".
      *> FDL: the r25 implied '$' beside two declared symbols - PIC $$9.99 stays legal:  $1.50.
      *> FG:  the second clause's symbol edits with ITS string: GBP7.
      *> DEEDIT: 12.3.7.4 GR13 - the string is de-edited from a numeric-edited sender (5.25 -> 525 in 9V99;
      *>      the zero image -> 000; USD123 -> 300 by MOVE truncation into 9V99).
      *> GRP: a group image carries the physical width (USD5.25X). BW: BLANK WHEN ZERO blanks the physical width
      *>      (5 spaces for PIC UU9); 42 -> USD42. NG: a fixed '-' before a floating string: "- USD12".
      *> NVC: NUMVAL-C with an explicit multi-character argument-2 (15.68.3 r2) - 1234.56.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60CURSET.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "U"
           CURRENCY SIGN IS "GBP" WITH PICTURE SYMBOL "G".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FU   PIC U9.99.
       01 FLU  PIC UUU9.
       01 FLZ  PIC UUUU.
       01 FDL   PIC $$9.99.
       01 FG   PIC G9.
       01 N    PIC 9V99.
       01 GRP.
          05 GA PIC U9.99.
          05 GB PIC X VALUE "X".
       01 X8   PIC X(8).
       01 BW   PIC UU9 BLANK WHEN ZERO.
       01 NG   PIC -UUU9.
       01 R    PIC S9(9)V99.
       PROCEDURE DIVISION.
           MOVE 5.25 TO FU.
           DISPLAY "FU=[" FU "]".
           MOVE 12 TO FLU.
           DISPLAY "FLU12=[" FLU "]".
           MOVE 123 TO FLU.
           DISPLAY "FLU123=[" FLU "]".
           MOVE 0 TO FLU.
           DISPLAY "FLU0=[" FLU "]".
           MOVE 0 TO FLZ.
           DISPLAY "FLZ0=[" FLZ "]".
           MOVE 7 TO FLZ.
           DISPLAY "FLZ7=[" FLZ "]".
           MOVE 1.5 TO FDL.
           DISPLAY "FDL=[" FDL "]".
           MOVE 7 TO FG.
           DISPLAY "FG=[" FG "]".
           MOVE FU TO N.
           DISPLAY "DEEDIT-FU=" N.
           MOVE FLU TO N.
           DISPLAY "DEEDIT-FLU0=" N.
           MOVE 123 TO FLU.
           MOVE FLU TO N.
           DISPLAY "DEEDIT-FLU123=" N.
           MOVE 5.25 TO GA.
           MOVE GRP TO X8.
           DISPLAY "GRP=[" X8 "]".
           MOVE 0 TO BW.
           DISPLAY "BW0=[" BW "]".
           MOVE 42 TO BW.
           DISPLAY "BW42=[" BW "]".
           MOVE -12 TO NG.
           DISPLAY "NG=[" NG "]".
           COMPUTE R = FUNCTION NUMVAL-C("USD1,234.56", "USD").
           DISPLAY "NVC=" R.
           STOP RUN.
       END PROGRAM PB60CURSET.
