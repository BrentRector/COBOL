      *> reject-at: 2014 2023
      *> kb/Work PB2689 (sibling arm) - a dynamic-capacity table that starts
      *> INSIDE the shorter group is not "beyond the last character of the
      *> shorter group" (ISO 8.5.1.12.2), so the latitude of a space-filled
      *> counterpart does not apply: T2 starts at relative byte position 2,
      *> where G1 has only the fixed item X1, so G1 has no corresponding table
      *> and the groups are not compatible (ISO 8.5.1.12.1 rule 1). The walk
      *> used to stop checking positions once G1's atoms were spent and
      *> accepted this MOVE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2689TAIL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 X1 PIC X(5).
       01 G2.
          05 Y2 PIC X(2).
          05 T2 PIC X OCCURS DYNAMIC CAPACITY IN C2 FROM 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE G2 TO G1
           DISPLAY "G1=[" G1 "]"
           STOP RUN.
