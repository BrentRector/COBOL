      *> ISO §8.4.2.2.1 rule 5 — implicit qualifiers for a data-name
      *> referenced in a data description entry clause (OCCURS
      *> DEPENDING ON) whose subject shares a group with that name.
      *> Rule 5: "The name is a data-name referenced in a data
      *> description entry clause whose subject is subordinate to the
      *> same group item as that data-name. In this case, the names of
      *> any group items superordinate to both the data-name and the
      *> subject of the data description entry clause are used as
      *> implicit qualifiers for the reference, in addition to any
      *> explicit qualifiers needed to establish uniqueness within that
      *> group."   cite.py: OK  §8.4.2.2.1 5)
      *> §13.18.38.4 GR8 b): with the ODO object in the same group, a
      *> SENDING group uses the occurrences given by the object's value
      *> and a RECEIVING group its maximum length.
      *> cite.py: OK  §13.18.38.4 8) b)
      *> CNT is declared SIX times, so no ODO below could name it
      *> without rule 5.  Each record is first filled at its maximum
      *> (both counts 5, so GR8 b) gives the full length either way),
      *> then the counts are set with FULL explicit qualification, and
      *> the record is MOVEd (sending: current length) to W X(8).
      *> G1 / G2  sibling records; T1's CNT is implicitly CNT OF G1,
      *>          T2's is CNT OF G2.  G1 length 1+2=3: "2AB";
      *>          G2 length 1+3=4: "3VWX".  (G1 using G2's CNT would
      *>          give "2ABC".)
      *> G3       T3 DEPENDING ON CNT OF B: B alone is not unique (H has
      *>          a B too); implicit G3 + explicit B = G3.B.CNT (4).
      *>          Length 1+1+4=6: "14ABCD".  (A.CNT would give "14A".)
      *> H        T4 is in H.B beside H.B.CNT: the groups superordinate
      *>          to both are B and H, so CNT means CNT OF B OF H (1);
      *>          H.A.CNT shares only H and is excluded.  Length
      *>          1+1+1=3: "31A".  (H.A.CNT would give "31ABC".)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7IQO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 CNT PIC 9.
          05 T1 PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
       01 G2.
          05 CNT PIC 9.
          05 T2 PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
       01 G3.
          05 A.
             10 CNT PIC 9.
          05 B.
             10 CNT PIC 9.
          05 T3 PIC X OCCURS 1 TO 5 DEPENDING ON CNT OF B.
       01 H.
          05 A.
             10 CNT PIC 9.
          05 B.
             10 CNT PIC 9.
             10 T4 PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
       01 W PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 5 TO CNT OF G1
           MOVE "5ABCDE" TO G1
           MOVE 2 TO CNT OF G1
           MOVE 5 TO CNT OF G2
           MOVE "5VWXYZ" TO G2
           MOVE 3 TO CNT OF G2
           MOVE 5 TO CNT OF A OF G3 CNT OF B OF G3
           MOVE "55ABCDE" TO G3
           MOVE 1 TO CNT OF A OF G3
           MOVE 4 TO CNT OF B OF G3
           MOVE 5 TO CNT OF A OF H CNT OF B OF H
           MOVE "55ABCDE" TO H
           MOVE 3 TO CNT OF A OF H
           MOVE 1 TO CNT OF B OF H
           MOVE G1 TO W
           DISPLAY "G1=[" W "]"
           MOVE G2 TO W
           DISPLAY "G2=[" W "]"
           MOVE G3 TO W
           DISPLAY "G3=[" W "]"
           MOVE H TO W
           DISPLAY "H=[" W "]"
           STOP RUN.
