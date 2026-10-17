      *>--- conformance-case
      *> id: l1c26_relational_operator_not_equal_symbol
      *> rules: [SR-8.3.2.4.2-2, SR-8.7.5.2-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.7.5.2 SR11 — <> means NOT EQUAL
      *> SR11: "<> is an abbreviation for NOT EQUAL."
      *> cite.py --check 8.7.5.2 "<> is an abbreviation for NOT EQUAL"
      *>   prints "OK  §8.7.5.2 8)": cite.py's text match cannot tell
      *>   SR11 from SR8 ("NOT = is an abbreviation for NOT EQUAL."),
      *>   whose words are identical; the SR11 line was read directly
      *>   in specs/ISO_COBOL.md §8.7.5.2 as "11\) <> is an abbreviation
      *>   for NOT EQUAL."
      *>   OK  §8.8.4.12.4 1)  (General rule)  abbreviated combined
      *>       relation: "the last stated relational operator were
      *>       inserted in place of the omitted relational operator"
      *> §8.7.5.1 Format 2 lists "IS <>" with IS optional.
      *> Placed in 2002: the 1985 text is not in the repository, so the
      *> edition at which <> entered cannot be derived here; 2002 is the
      *> lowest directory where it is certain to exist (see the row
      *> notes).
      *> A = 5 is compared with X = 3, 5, 7; each line shows the truth
      *> value at X = 3, 5, 7. <> means exactly NOT EQUAL:
      *>   A NOT EQUAL X : 5 vs 3 T, 5 vs 5 F, 5 vs 7 T          -> TFT
      *>   A <> X, A IS <> X : identical                          -> TFT
      *>   A <> X AND 9 = (A <> X) AND (A <> 9); A <> 9 is T      -> TFT
      *>   A <> X OR 5  = (A <> X) OR (A <> 5); A <> 5 is F       -> TFT
      *>   NOT A <> X   = NOT (A <> X), i.e. A = X                -> FTF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C26D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A                       PIC 9 VALUE 5.
       01  X                       PIC 9.
       01  I                       PIC 9.
       01  RT.
           05  RROW OCCURS 6.
               10  RC              PIC X OCCURS 3.
       PROCEDURE DIVISION.
       M-1.
           PERFORM EVAL-ALL VARYING I FROM 1 BY 1 UNTIL I > 3
           DISPLAY "NE NOT EQUAL    " RROW(1)
           DISPLAY "NE <>           " RROW(2)
           DISPLAY "NE IS <>        " RROW(3)
           DISPLAY "AB <> X AND 9   " RROW(4)
           DISPLAY "AB <> X OR 5    " RROW(5)
           DISPLAY "NOT A <> X      " RROW(6)
           STOP RUN.
       EVAL-ALL.
           COMPUTE X = 2 * I + 1
           IF A NOT EQUAL X MOVE "T" TO RC(1, I)
               ELSE MOVE "F" TO RC(1, I) END-IF
           IF A <> X MOVE "T" TO RC(2, I)
               ELSE MOVE "F" TO RC(2, I) END-IF
           IF A IS <> X MOVE "T" TO RC(3, I)
               ELSE MOVE "F" TO RC(3, I) END-IF
           IF A <> X AND 9 MOVE "T" TO RC(4, I)
               ELSE MOVE "F" TO RC(4, I) END-IF
           IF A <> X OR 5 MOVE "T" TO RC(5, I)
               ELSE MOVE "F" TO RC(5, I) END-IF
           IF NOT A <> X MOVE "T" TO RC(6, I)
               ELSE MOVE "F" TO RC(6, I) END-IF.
