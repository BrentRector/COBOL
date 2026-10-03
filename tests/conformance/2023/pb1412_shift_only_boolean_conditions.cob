      *> kb/Work PB1412. ISO 1989:2023 8.8.2: a boolean expression is also "a boolean expression and an integer
      *> operand separated by a boolean shift operator" (rule 8, COBOL-2023), so a SHIFT-ONLY expression - no
      *> B-AND, B-OR, B-XOR or B-NOT anywhere in it - is a boolean expression, and 8.8.4.2.2 Format 2 (boolean
      *> relation) and 8.8.4.3 (simple boolean condition) take any boolean expression.
      *>   cite.py --check 8.8.2 "Boolean shift operations shall be performed without regard for the usage of
      *>     the first operand." -> OK 8)
      *> Rule 8 and 9 give the values (the result is as long as the FIRST operand; logical shifts fill boolean
      *> zero, circular shifts rotate). Every leg fails on the discriminator the binder used to apply, which named
      *> only the four 2002 operator tokens and so took each of these for a bare operand:
      *>   A = 1100.  A B-SHIFT-L 1 = 1000 = B"1000"                       -> Y1
      *>   F1 = 1.    F1 B-SHIFT-L 1 = 0 (shifted out), false                -> N2
      *>   BW = 1.    BW B-SHIFT-LC 1 = 1 (a circular shift of one position) -> T1
      *>              BW B-SHIFT-L 1 = 0 = B"0"                              -> T5
      *>              NOT of the condition BW B-SHIFT-LC 1 (true) is false   -> F3
      *>              (BW B-SHIFT-RC 1) is the same condition in parentheses -> T4
      *>   A B-SHIFT-R 1 = 0110 and BW B-SHIFT-LC 1 is true                  -> T6
      *>   B"1" B-SHIFT-L 1 = 0, F1 = 1, so unequal                          -> N7
      *> Rule 9 / rule 8, the two halves a relation makes observable: the result is as long as the FIRST item, and
      *> the shift is made without regard to that item's usage.
      *>   cite.py --check 8.8.2 "the operation shall proceed as though the shorter operand were extended on the
      *>     right by a sufficient number of boolean zeros" -> OK 9)
      *>   S2 = 10 (two positions). S2 B-SHIFT-LC 1 rotates TWO positions: 01. A comparison extends the shorter
      *>   operand on the right with zeros, so it equals B"01" (Y8) and does NOT equal B"000001" (N9, 010000 against
      *>   000001) - a shift made over a longer extended item would answer the opposite on both.
      *>   UB = 1100 as USAGE BIT: UB B-SHIFT-R 1 = 0110 whatever the usage                 -> Y10
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1412POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC 1(4) VALUE B"1100".
       01 F1 PIC 1 VALUE B"1".
       01 BW PIC 1 VALUE B"1".
       01 S2 PIC 1(2) VALUE B"10".
       01 UB PIC 1(4) USAGE BIT VALUE B"1100".
       PROCEDURE DIVISION.
       MAIN.
           IF A B-SHIFT-L 1 = B"1000" DISPLAY "Y1" ELSE DISPLAY "N1".
           IF F1 B-SHIFT-L 1 DISPLAY "Y2" ELSE DISPLAY "N2".
           IF BW B-SHIFT-LC 1 DISPLAY "T1" ELSE DISPLAY "F1".
           IF BW B-SHIFT-L 1 = B"0" DISPLAY "T5" ELSE DISPLAY "F5".
           IF NOT BW B-SHIFT-LC 1 DISPLAY "T3" ELSE DISPLAY "F3".
           IF (BW B-SHIFT-RC 1) DISPLAY "T4" ELSE DISPLAY "F4".
           IF A B-SHIFT-R 1 = B"0110" AND BW B-SHIFT-LC 1
              DISPLAY "T6" ELSE DISPLAY "F6".
           IF B"1" B-SHIFT-L 1 = F1 DISPLAY "Y7" ELSE DISPLAY "N7".
           IF S2 B-SHIFT-LC 1 = B"01" DISPLAY "Y8" ELSE DISPLAY "N8".
           IF S2 B-SHIFT-LC 1 = B"000001" DISPLAY "Y9" ELSE DISPLAY "N9".
           IF UB B-SHIFT-R 1 = B"0110" DISPLAY "Y10" ELSE DISPLAY "N10".
           STOP RUN.
