      *>--- conformance-case
      *> id: l1c04-bwz-not-numeric-operand
      *> rules: [GR-8.5.2.12-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.2.12 GR1 — a numeric PICTURE with BLANK WHEN ZERO is
      *>   NOT a numeric data item, so it is no arithmetic operand.
      *> cite.py --check 8.5.2.12 "An elementary data item described as
      *>   numeric by its PICTURE character-string and not described
      *>   with a BLANK WHEN ZERO clause" -> OK §8.5.2.12 1)
      *> cite.py --check 8.5.2.13 "A data item described as numeric by
      *>   its PICTURE character-string and described with a BLANK WHEN
      *>   ZERO clause" -> OK §8.5.2.13 2)
      *> cite.py --check 8.8.1.1 "An arithmetic expression may be an
      *>   identifier referencing a numeric data item" -> OK §8.8.1.1
      *> B is category numeric-edited, not numeric, so "B + 1" is not
      *> an arithmetic expression 8.8.1.1 admits.
      *> COBOLNET0844 = arithmetic-expression operand not numeric.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 9(3) BLANK WHEN ZERO.
       01 R PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 5 TO B.
           COMPUTE R = B + 1.
           DISPLAY R.
           STOP RUN.
