      *>--- conformance-case
      *> id: pb462_set_condition_group_odo_length
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.39.4 GR6 - SET condition-name-1 TO TRUE over a GROUP conditional variable, and the two halves
      *> of the rule that no test reached (kb/Work PB462):
      *>   "the literal in the VALUE clause associated with condition-name-1 is placed in the conditional
      *>    variable according to the rules for the VALUE clause, except that when the conditional variable is an
      *>    alphanumeric group item, bit group item, or national group item to which a table is subordinate, its
      *>    length is determined as specified in 13.18.38, OCCURS clause" - so the receiving LENGTH is the
      *>    table's CURRENT extent (13.18.38.4 GR7/GR8: data-name-1's value), not its maximum; and
      *>   "If the length of the conditional variable is zero, the SET statement leaves it unchanged."
      *> 13.18.60.3 SR11 bars only four ELEMENTARY classes from being a conditional variable, never a group, so
      *> the group case is named by the rule and required.
      *>
      *> EXPECTED VALUES, DERIVED FROM THE RULES, NOT MEASURED:
      *>   WS-N = 3 -> the group is 3 character positions, so "ABCDE" is placed by the VALUE-clause rules for an
      *>               alphanumeric group receiver (13.18.63.4 GR5 - left-justified, excess truncated) = "ABC".
      *>   WS-N = 0 -> the length is zero, so the SET leaves the conditional variable UNCHANGED; restoring the
      *>               extent therefore shows the value written before it, "ZZZ", and not "ABC".
      *>   WS-N = 5 -> the group is 5 positions and the whole literal fits = "ABCDE".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB462-SET-COND-ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 3.
       01 CV.
          88 CV-YES VALUE "ABCDE".
          05 T PIC X OCCURS 0 TO 5 TIMES DEPENDING ON WS-N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 3 TO WS-N.
           SET CV-YES TO TRUE.
           DISPLAY "LEN3[" CV "]".
           MOVE "ZZZ" TO CV.
           MOVE 0 TO WS-N.
           SET CV-YES TO TRUE.
           MOVE 3 TO WS-N.
           DISPLAY "LEN0[" CV "]".
           MOVE 5 TO WS-N.
           SET CV-YES TO TRUE.
           DISPLAY "LEN5[" CV "]".
           STOP RUN.
