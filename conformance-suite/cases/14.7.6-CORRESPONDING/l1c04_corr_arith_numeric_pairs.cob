      *>--- conformance-case
      *> id: l1c04_corr_arith_numeric_pairs
      *> rules: [GR-14.7.6-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.7.6 rule 3 — ADD/SUBTRACT CORRESPONDING pairs a
      *>   same-named item only when BOTH are numeric data items.
      *> cite.py --check 14.7.6 "In an ADD or SUBTRACT statement, both
      *>   of the data items are numeric data items" -> OK §14.7.6 3)
      *> Categories of the namesakes (G1 sender / G2 receiver):
      *>   A  9(3) / 9(3)            both numeric      -> pair
      *>   C  X(3) / 9(3)            sender alphanumeric
      *>   D  9(3) / ZZ9             receiver numeric-edited
      *>   E  9(3) / 9(3) BLANK WHEN ZERO  receiver numeric-edited
      *> cite.py --check 8.5.2.12 "An elementary data item described as
      *>   numeric by its PICTURE character-string and not described
      *>   with a BLANK WHEN ZERO clause" -> OK §8.5.2.12 1)
      *> cite.py --check 8.5.2.3 "An elementary data item described as
      *>   alphanumeric by its PICTURE character-string"
      *>   -> OK §8.5.2.3 1)
      *> cite.py --check 8.5.2.13 "A data item described as
      *>   numeric-edited by its PICTURE character-string"
      *>   -> OK §8.5.2.13 1)
      *> cite.py --check 8.5.2.13 "A data item described as numeric by
      *>   its PICTURE character-string and described with a BLANK WHEN
      *>   ZERO clause" -> OK §8.5.2.13 2)
      *> Start: G1 A=5 C="007" D=5 E=5; G2 A=10 C=10 D=" 10" E=10.
      *>   "ADD A=015 C=010 D= 10 E=010"  only A pairs: 10+5 = 15.
      *>   "SUB A=010 C=010 D= 10 E=010"  SUBTRACT CORRESPONDING G1
      *>                                  FROM G2: only A, 15-5 = 10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 A PIC 9(3).
          05 C PIC X(3).
          05 D PIC 9(3).
          05 E PIC 9(3).
       01 G2.
          05 A PIC 9(3).
          05 C PIC 9(3).
          05 D PIC ZZ9.
          05 E PIC 9(3) BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 5 TO A OF G1 D OF G1 E OF G1.
           MOVE "007" TO C OF G1.
           MOVE 10 TO A OF G2 C OF G2 D OF G2 E OF G2.
           ADD CORRESPONDING G1 TO G2.
           DISPLAY "ADD A=" A OF G2 " C=" C OF G2 " D=" D OF G2
                   " E=" E OF G2.
           SUBTRACT CORRESPONDING G1 FROM G2.
           DISPLAY "SUB A=" A OF G2 " C=" C OF G2 " D=" D OF G2
                   " E=" E OF G2.
           STOP RUN.
