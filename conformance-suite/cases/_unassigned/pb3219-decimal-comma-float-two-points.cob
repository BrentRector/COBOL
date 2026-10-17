*>--- conformance-case
*> id: pb3219-decimal-comma-float-two-points
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3219 - ISO 1989:2023 13.18.40.3 SR12 b: "Each of the symbols from the set 'CR', 'DB', 'E', 'S',
      *> 'V' '.' may appear only once in character-string-1", read through SR13 ("The rules for the symbol period
      *> apply to the symbol comma, and the rules for the symbol comma apply to the symbol period"): under
      *> DECIMAL-POINT IS COMMA the COMMA is the decimal point and may appear once in the significand of a
      *> floating-point numeric-edited picture. The compiler counted periods only, so this picture with two
      *> decimal points bound and printed "+1,5,0E+00", an image no rule defines. Positive sibling:
      *> 2023/pb3219_decimal_point_comma_sr12_point_2023 (two PERIODS are legal there, as grouping symbols).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3219NFT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC +9,9,9E+99.
       PROCEDURE DIVISION.
           STOP RUN.
