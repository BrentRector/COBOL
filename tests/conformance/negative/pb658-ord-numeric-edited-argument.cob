      *> reject-at: 85 2002 2014 2023
      *> ISO §15.70.3 r1: "Argument-1 shall be one character position in length and shall be of category
      *> alphabetic, alphanumeric, or national" (cite.py --check 15.70.3 OK, §15.70.3 rule 1).
      *>
      *> CATEGORY-WORDED, SO A ONE-POSITION NUMERIC-EDITED ITEM IS OUT (kb/Work PB658's sibling sweep). PIC Z is one
      *> character position and class alphanumeric (§8.5.2.1 Table 2), which is why the class screen and the width
      *> predicate both let it through; its CATEGORY is numeric-edited, and §8.5.2.1's closing sentence ("refers to
      *> the category unless class is specifically indicated", cite.py --check 8.5.2.1 OK) makes r1 a category rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB658NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED PIC Z.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           MOVE 5 TO ED
           COMPUTE R = FUNCTION ORD(ED)
           STOP RUN.
