      *>--- conformance-case
      *> id: l1c04_numeric_category_bwz
      *> rules: [GR-8.5.2.12-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.2.12 GR1 — numeric PICTURE is category numeric only
      *>   WITHOUT BLANK WHEN ZERO; with it the item is numeric-edited.
      *> cite.py --check 8.5.2.12 "An elementary data item described as
      *>   numeric by its PICTURE character-string and not described
      *>   with a BLANK WHEN ZERO clause" -> OK §8.5.2.12 1)
      *> cite.py --check 8.5.2.13 "A data item described as numeric by
      *>   its PICTURE character-string and described with a BLANK WHEN
      *>   ZERO clause" -> OK §8.5.2.13 2)
      *> The observable: INITIALIZE REPLACING selects receivers by
      *> CATEGORY -
      *> cite.py --check 14.9.20.4 "The keywords in category-name
      *>   correspond to a category of data as specified in 8.5.2"
      *>   -> OK §14.9.20.4 2)
      *> cite.py --check 14.9.20.4 "The REPLACING phrase is specified
      *>   and the category of the elementary data item is one of the
      *>   categories specified in the REPLACING phrase"
      *>   -> OK §14.9.20.4 5) c) 2.
      *> and an item that is not a receiving-operand is not moved to.
      *> N is PIC 9(3) (category numeric by 8.5.2.12 1)); B is PIC 9(3)
      *> BLANK WHEN ZERO (category numeric-edited by 8.5.2.13 2)).
      *> Both start at 42 (MOVE 42 -> "042").
      *>   "1 N=007 B=042" INITIALIZE G REPLACING NUMERIC DATA BY 7:
      *>                   only N is category numeric -> 7; B untouched.
      *>   "2 N=007 B=005" INITIALIZE G REPLACING NUMERIC-EDITED DATA
      *>                   BY 5: only B -> 5 (no zero, so not blanked).
      *>   "3 N=008"       ADD 1 TO N - a numeric data item operand.
      *> The refusal of B as an arithmetic operand (the exclusion arm as
      *> a rejection) is negative/l1c04-bwz-not-numeric-operand.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 N PIC 9(3).
          05 B PIC 9(3) BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 42 TO N.
           MOVE 42 TO B.
           INITIALIZE G REPLACING NUMERIC DATA BY 7.
           DISPLAY "1 N=" N " B=" B.
           INITIALIZE G REPLACING NUMERIC-EDITED DATA BY 5.
           DISPLAY "2 N=" N " B=" B.
           ADD 1 TO N.
           DISPLAY "3 N=" N.
           STOP RUN.
