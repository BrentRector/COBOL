      *>--- conformance-case
      *> id: l1c04_arith_mixed_descriptions
      *> rules: [GR-14.7.7-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.7.7 rule 1 — operands of DIFFERENT descriptions need
      *>   no user conversion; alignment is supplied by the statement.
      *> "The data descriptions of the operands need not be the same;
      *>  any necessary conversion and decimal point alignment is
      *>  supplied throughout the calculation."
      *> cite.py --check 14.7.7 "The data descriptions of the operands
      *>   need not be the same; any necessary conversion and decimal
      *>   point alignment is supplied throughout the calculation"
      *>   -> OK §14.7.7 1)
      *> Four operands, four different descriptions (usage, scale,
      *> sign form): A S9(3)V9 DISPLAY = -12.5; B 9V99 BINARY = 3.75;
      *> C 99V999 PACKED-DECIMAL = 10.125; D S99 SIGN LEADING SEPARATE
      *> = -7. Every result is exact decimal arithmetic after
      *> alignment, then stored per the (edited) receiver:
      *>   "1    -5.625"  ADD A B C D GIVING ----9.999:
      *>                  -12.5 + 3.75 + 10.125 - 7 = -5.625.
      *>   "2 37.97"      MULTIPLY B BY C GIVING 99.99 ROUNDED:
      *>                  3.75 x 10.125 = 37.96875 -> 37.97.
      *>   "3 -3.33"      COMPUTE -9.99 = A / B: -3.333.. truncated.
      *>   "4 17.1"       SUBTRACT D FROM C GIVING 99.9:
      *>                  10.125 - (-7) = 17.125 truncated -> 17.1.
      *>   "5 +0091.25"   COMPUTE +9(4).99 = A * D + B:
      *>                  87.5 + 3.75 = 91.25.
      *>   "6 -01.45"     DIVIDE D INTO C GIVING -99.99 ROUNDED:
      *>                  10.125 / -7 = -1.4464.. -> -1.45.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC S9(3)V9 VALUE -12.5.
       01 B  PIC 9V99 BINARY VALUE 3.75.
       01 C  PIC 99V999 PACKED-DECIMAL VALUE 10.125.
       01 D  PIC S99 SIGN LEADING SEPARATE VALUE -7.
       01 R1 PIC ----9.999.
       01 R2 PIC 99.99.
       01 R3 PIC -9.99.
       01 R4 PIC 99.9.
       01 R5 PIC +9(4).99.
       01 R6 PIC -99.99.
       PROCEDURE DIVISION.
       MAIN.
           ADD A B C D GIVING R1.
           DISPLAY "1 " R1.
           MULTIPLY B BY C GIVING R2 ROUNDED.
           DISPLAY "2 " R2.
           COMPUTE R3 = A / B.
           DISPLAY "3 " R3.
           SUBTRACT D FROM C GIVING R4.
           DISPLAY "4 " R4.
           COMPUTE R5 = A * D + B.
           DISPLAY "5 " R5.
           DIVIDE D INTO C GIVING R6 ROUNDED.
           DISPLAY "6 " R6.
           STOP RUN.
