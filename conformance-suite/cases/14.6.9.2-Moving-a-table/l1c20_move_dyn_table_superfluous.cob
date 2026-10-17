      *>--- conformance-case
      *> id: l1c20_move_dyn_table_superfluous
      *> rules: [GR-14.6.9.2-1]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.6.9.2 GR1 — MOVE of a dynamic-capacity table into a
      *> fixed or occurs-depending table of lower capacity: the
      *> superfluous elements are not moved and no exception exists.
      *>
      *> THE RULES.
      *> §14.6.9.2: "A dynamic-capacity table may be moved as part of a
      *>   variable-length group to or from another table" ... "If the
      *>   receiving table is not a dynamic-capacity table: 1) If the
      *>   sending table has a higher current capacity than the
      *>   receiving table, superfluous elements are not moved and no
      *>   exception exists,"
      *>   OK  §14.6.9.2   (Moving a table)
      *>   OK  §14.6.9.2 1)  (Moving a table)
      *> §14.6.9.1: a fixed table is "a dynamic capacity table that has
      *>   a current capacity equal to the fixed number of elements";
      *>   an occurs-depending table one "that has a current capacity
      *>   equal to the value of the corresponding DEPENDING operand".
      *>   OK  §14.6.9.1   (General)  [both sentences]
      *> §8.5.1.12.2 / §8.5.1.12.3: the groups are compatible - the
      *>   tables "occupy the same relative byte positions within
      *>   their groups" (byte 3) and "the byte length of their
      *>   elements is equal" (3).
      *>   OK  §8.5.1.12.2   (Positional correspondence)
      *>   OK  §8.5.1.12.3   (Matching)
      *> §14.9.39.4 GR30 a): SET SC TO 3 makes the new capacity 3.
      *>   OK  §14.9.39.4 30)  (General rules)
      *> "no exception exists" is observed with every EC checked
      *> (>>TURN EC-ALL CHECKING ON): FUNCTION EXCEPTION-STATUS stays
      *> spaces (31 characters between the brackets).
      *>
      *> DERIVATION.
      *> S: S-H "HH", SE(1..4) "AAA" "BBB" "CCC" "DDD" (capacity 4 by
      *>   implicit creation, §8.5.1.9.3).
      *> R=[HHAAABBB]    R's table is fixed at 2: elements 1-2 are moved
      *>                 (MOVE rules, element by element), 3-4 are
      *>                 superfluous and not moved; R has no room
      *>                 beyond them, and R-H takes "HH".
      *> X1=[<31 spaces>] no exception.
      *> SET SC TO 3 -> capacity 3. R2's ODO table is filled OL1..OL4
      *>   with N2 = 4, then N2 = 2 (capacity 2) for the MOVE:
      *>   elements 1-2 of S replace OL1, OL2; element 3 is
      *>   superfluous and is not moved, so OL3 survives.
      *> R2=[HHAAABBBOL3OL4] displayed after N2 is set back to 4.
      *> X2=[<31 spaces>] no exception.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S.
          05 S-H  PIC XX.
          05 SE  PIC XXX OCCURS DYNAMIC CAPACITY IN SC FROM 1 TO 9.
       01 R.
          05 R-H  PIC XX.
          05 RE  PIC XXX OCCURS 2 TIMES.
       01 N2     PIC 9.
       01 R2.
          05 R2-H PIC XX.
          05 R2E PIC XXX OCCURS 1 TO 5 TIMES DEPENDING ON N2.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "HH" TO S-H.
           MOVE "AAA" TO SE (1).
           MOVE "BBB" TO SE (2).
           MOVE "CCC" TO SE (3).
           MOVE "DDD" TO SE (4).
           MOVE ALL "." TO R.
           MOVE S TO R.
           DISPLAY "R=[" R "]".
           DISPLAY "X1=[" FUNCTION EXCEPTION-STATUS "]".
           MOVE 4 TO N2.
           MOVE ".." TO R2-H.
           MOVE "OL1" TO R2E (1).
           MOVE "OL2" TO R2E (2).
           MOVE "OL3" TO R2E (3).
           MOVE "OL4" TO R2E (4).
           SET SC TO 3.
           MOVE 2 TO N2.
           MOVE S TO R2.
           MOVE 4 TO N2.
           DISPLAY "R2=[" R2 "]".
           DISPLAY "X2=[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
