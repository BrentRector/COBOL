      *> reject-at: 2002 2014 2023
      *> ALL ZERO IN THE VALUE CLAUSE OF A NUMERIC ITEM (kb/Work PB1415).
      *> ISO/IEC 1989:2023 §8.3.3.6.3 SR1 a): "If the literal is restricted to a numeric
      *> literal, the only figurative constant permitted is ZERO (ZEROS, ZEROES) without
      *> the ALL phrase." §13.18.63.3 SR2 restricts the VALUE literals of a numeric
      *> subject to numeric ones, so V6's VALUE and C4's level-88 VALUE are both barred.
      *> Edge DERIVED at 2002 (VCR Table 7 row 7.28): accepted at 85, where the 85
      *> twin is conformance/85/pb1415_all_zero_numeric_85. Refused, COBOLNET0902.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1415VZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V6 PIC 9(3) VALUE ALL ZEROES.
       01 V4 PIC 9.
          88 C4 VALUE ALL ZERO.
       PROCEDURE DIVISION.
           DISPLAY V6
           STOP RUN.
