      *>--- conformance-case
      *> id: l1c32_relation_alphabetic_alphanumeric
      *> rules: [GR-8.8.4.2.1-2, GR-8.8.4.2.1-3, GR-8.8.4.2.1-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.8.4.2.1 2) 3) 7) — relations between alphabetic and alphanumeric operands
      *>
      *> THE RULES. §8.8.4.2.1: "Comparisons are defined for the following:"
      *>   2) "Two operands of class alphabetic."
      *>   3) "Two operands of class alphanumeric."
      *>   7) "Two operands of different classes where each operand is from the set of
      *>      classes alphanumeric, alphabetic, or national."
      *> and, same clause: "For comparison, an alphanumeric group item shall be treated as
      *> an elementary alphanumeric data item. A class alphabetic operand shall be treated
      *> as though it were an operand of class alphanumeric."
      *> The comparison itself is §8.8.4.2.7 (standard comparison; no PROGRAM COLLATING
      *> SEQUENCE is declared, and only SPACE and the letters A-D are compared, which are
      *> ordered SPACE < A < B < C < D in the native sequence):
      *>   1) equal length: "The operand that contains the character that is positioned
      *>      higher in the alphanumeric collating sequence is the greater operand."
      *>   2) unequal length: "comparison proceeds as though the shorter operand were
      *>      extended on the right by sufficient alphanumeric spaces".
      *> The NOTE of §8.8.4.2.1: "All comparisons involving numeric-edited data items are
      *> alphanumeric or national comparisons".
      *>
      *> cite.py (NOTE PB1554: cite.py labels the unnumbered group/alphabetic sentence of
      *> §8.8.4.2.1 as list item "13)"; it is the paragraph after item 13):
      *>   OK  §8.8.4.2.1 2)  (General)
      *>   OK  §8.8.4.2.1 3)  (General)
      *>   OK  §8.8.4.2.1 7)  (General)
      *>   OK  §8.8.4.2.1 13)  (General)  [the group/alphabetic sentence]
      *>   OK  §8.8.4.2.7 1)  (Comparison of alphanumeric operands)
      *>   OK  §8.8.4.2.7 2)  (Comparison of alphanumeric operands)
      *>
      *> DERIVATION of every expected line (T = condition true, F = false):
      *>  item 2 (alphabetic vs alphabetic)
      *>   R2-1 A3 "ABC" = A5 "ABC  ": A3 extended to "ABC  ", all equal -> T
      *>   R2-2 A3 "ABC" < A5B "ABC A": "ABC  " vs "ABC A", 5th SPACE < A -> T
      *>   R2-3 A3 > A5: they are equal -> F
      *>   R2-4 A3 < A3D "ABD": 3rd C < D -> T
      *>  item 3 (alphanumeric vs alphanumeric, incl. group and numeric-edited)
      *>   R3-1 X3 "ABC" = X5 "ABC  " -> T (space extension)
      *>   R3-2 X5 "ABC  " < X4 "ABCA": X4 extended "ABCA ", 4th SPACE < A -> T
      *>   R3-3 group G1 ("AB" + 9(2) 12) = "AB12" -> T (group is elementary X(4))
      *>   R3-4 G1 "AB12" > "AB1": "AB1" extended "AB1 ", 4th 2 > SPACE -> T
      *>   R3-5 group G2 (9(3) 5 = "005") = "005" -> T
      *>   R3-6 G2 = "5": "005" vs "5  " -> F (alphanumeric, NOT algebraic)
      *>   R3-7 NE ZZ9 after MOVE 12 = " 12" -> T
      *>   R3-8 NE " 12" = "12": "12 " -> F (character compare, not numeric)
      *>  item 7 (alphabetic vs alphanumeric, the 1985 members of the set)
      *>   R7-1 A3 "ABC" = X5 "ABC  " -> T
      *>   R7-2 A3 "ABC" < X4 "ABCA": "ABC " vs "ABCA" -> T
      *>   R7-3 X2 "AB" < A3 "ABC": "AB " vs "ABC", SPACE < C -> T
      *>   R7-4 A5 "ABC  " = literal "ABC" -> T
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C32A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A3           PIC A(3) VALUE "ABC".
       01 A3D          PIC A(3) VALUE "ABD".
       01 A5           PIC A(5) VALUE "ABC".
       01 A5B          PIC A(5) VALUE "ABC A".
       01 X2           PIC X(2) VALUE "AB".
       01 X3           PIC X(3) VALUE "ABC".
       01 X4           PIC X(4) VALUE "ABCA".
       01 X5           PIC X(5) VALUE "ABC".
       01 G1.
          05 G1-A      PIC X(2) VALUE "AB".
          05 G1-N      PIC 9(2) VALUE 12.
       01 G2.
          05 G2-N      PIC 9(3) VALUE 5.
       01 NE           PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 12 TO NE.
           IF A3 = A5 DISPLAY "R2-1 T" ELSE DISPLAY "R2-1 F".
           IF A3 < A5B DISPLAY "R2-2 T" ELSE DISPLAY "R2-2 F".
           IF A3 > A5 DISPLAY "R2-3 T" ELSE DISPLAY "R2-3 F".
           IF A3 < A3D DISPLAY "R2-4 T" ELSE DISPLAY "R2-4 F".
           IF X3 = X5 DISPLAY "R3-1 T" ELSE DISPLAY "R3-1 F".
           IF X5 < X4 DISPLAY "R3-2 T" ELSE DISPLAY "R3-2 F".
           IF G1 = "AB12" DISPLAY "R3-3 T" ELSE DISPLAY "R3-3 F".
           IF G1 > "AB1" DISPLAY "R3-4 T" ELSE DISPLAY "R3-4 F".
           IF G2 = "005" DISPLAY "R3-5 T" ELSE DISPLAY "R3-5 F".
           IF G2 = "5" DISPLAY "R3-6 T" ELSE DISPLAY "R3-6 F".
           IF NE = " 12" DISPLAY "R3-7 T" ELSE DISPLAY "R3-7 F".
           IF NE = "12" DISPLAY "R3-8 T" ELSE DISPLAY "R3-8 F".
           IF A3 = X5 DISPLAY "R7-1 T" ELSE DISPLAY "R7-1 F".
           IF A3 < X4 DISPLAY "R7-2 T" ELSE DISPLAY "R7-2 F".
           IF X2 < A3 DISPLAY "R7-3 T" ELSE DISPLAY "R7-3 F".
           IF A5 = "ABC" DISPLAY "R7-4 T" ELSE DISPLAY "R7-4 F".
           STOP RUN.
