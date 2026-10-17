*>--- conformance-case
*> id: pb531-picture-repetition-factor-zero
*> rules: [SR-13.18.40.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR6 - "An unsigned nonzero integer that is enclosed in parentheses indicates
      *> the number of consecutive occurrences of the symbol that immediately precedes the left
      *> parenthesis."  UNSIGNED and NONZERO are both load-bearing, and the rule is ALL FORMATS, so it
      *> reads identically at every edition.
      *> A ZERO factor is excluded by "NONZERO".  It is doubly excluded for a numeric character-string by
      *> SR14 ("the number of digit positions described by character-string-1 shall range from 1 through
      *> 31"), but the rule that is broken FIRST, and for every category alike, is SR6 - which is why the
      *> diagnostic names it rather than a category rule that happens to catch the shape.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB531PICTUREREPETITIONFACTOR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1 PIC X(0).
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
