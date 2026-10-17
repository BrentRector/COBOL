      *>--- conformance-case
      *> id: l1c09_overlap_same_entry
      *> rules: [GR-14.6.10-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.6.10 2) — overlapping operands described by the same
      *> data description entry give the no-sharing result.
      *> Rule: "When the data items are described by the same data
      *> description entry, the result of the statement is the same as
      *> if the data items shared no part of their respective storage
      *> areas."
      *> cite.py: OK  §14.6.10 2)  (Overlapping operands)
      *> cite.py: OK  §14.9.2.4 1)  (General rules) ADD format 1: "the
      *>          sum of such operands. The sum of the initial
      *>            evaluation
      *>          and the value of the data item referenced by
      *>          identifier-2 is stored as the new value"
      *> Each statement below names ONE entry as both sending and
      *> receiving operand; the expected value is what the statement
      *> yields when the sending operand is a separate copy:
      *>   A=7:  ADD A TO A          -> 7 + 7 = 14          "A=0014"
      *>   B=3:  MULTIPLY B BY B     -> 3 * 3 = 9           "B=0009"
      *>   N=10: ADD N N N TO N      -> initial evaluation
      *>         10+10+10 = 30, + 10 = 40 (a store after each
      *>         addend would give 80)                      "N=0040"
      *>   N=40: DIVIDE N INTO N     -> 40 / 40 = 1         "N=0001"
      *>   N=10: SUBTRACT N FROM N   -> 10 - 10 = 0         "N=0000"
      *>   C=5:  COMPUTE C = C * 2 + C / 5 -> 10 + 1 = 11.00
      *>         (PIC 9(3)V99 DISPLAY shows digits only)    "C=01100"
      *>   E(2)="XYZ042", I=J=2: MOVE E (I) TO E (J) -> the
      *>         same occurrence, unchanged                 "E=XYZ042"
      *>   G="ABCD1234": MOVE G TO G -> unchanged           "G=ABCD1234"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 7.
       01 B PIC 9(4) VALUE 3.
       01 N PIC 9(4) VALUE 10.
       01 C PIC 9(3)V99 VALUE 5.
       01 I PIC 9 VALUE 2.
       01 J PIC 9 VALUE 2.
       01 TBL.
          05 E PIC X(6) OCCURS 3 TIMES.
       01 G PIC X(8) VALUE "ABCD1234".
       PROCEDURE DIVISION.
       MAIN-P.
           ADD A TO A.
           DISPLAY "A=" A.
           MULTIPLY B BY B.
           DISPLAY "B=" B.
           ADD N N N TO N.
           DISPLAY "N=" N.
           DIVIDE N INTO N.
           DISPLAY "N=" N.
           MOVE 10 TO N.
           SUBTRACT N FROM N.
           DISPLAY "N=" N.
           COMPUTE C = C * 2 + C / 5.
           DISPLAY "C=" C.
           MOVE "ABC001" TO E (1).
           MOVE "XYZ042" TO E (2).
           MOVE "QQQ999" TO E (3).
           MOVE E (I) TO E (J).
           DISPLAY "E=" E (2).
           MOVE G TO G.
           DISPLAY "G=" G.
           STOP RUN.
