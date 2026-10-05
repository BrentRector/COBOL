      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB529 - SR14's LOWER BOUND. ISO 1989:2023 13.18.40.3 SR14: "For data items of category
      *> numeric, and for fixed-point data items of category numeric-edited, the number of digit positions
      *> described by character-string-1 shall range from 1 through 31." A string with NO digit position is
      *> therefore illegal source at every edition. 13.18.40.4 GR14 row P counts the symbols that ARE digit
      *> positions - "The symbol 'P' is not counted in the size of the item, but each symbol 'P' is counted in
      *> the maximum number of digit positions" - and 'V', 'S', a single '$' or a single '+' are none of them.
      *> The digit-capacity screen once skipped exactly this count (its guard was DigitPositions > 0), so only
      *> the composition validator stood between these pictures and a ZERO-LENGTH category-numeric item whose
      *> every store is discarded; the screen now asks the lower bound itself (EditionContext.CheckDigitCapacity,
      *> COBOLNET2882) and the composition validator reports the same pictures first as COBOLNET1934.
      *>
      *> The rule is the same at every edition (SR14 and SR12 a are 1985 rules too), so all four reject.
      *>
      *> NC1  V   - an assumed decimal point and nothing to align.
      *> NC2  $   - ONE currency symbol: a floating string needs two, so it adds no digit position.
      *> NC3  +   - ONE sign symbol: the same.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB529NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NC1 PIC V.
       01 NC2 PIC $.
       01 NC3 PIC +.
       PROCEDURE DIVISION.
           STOP RUN.
