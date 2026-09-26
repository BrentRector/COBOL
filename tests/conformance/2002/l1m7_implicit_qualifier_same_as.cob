      *> ISO §8.4.2.2.1 rule 5 — implicit qualifiers for the data-name
      *> of a SAME AS clause whose subject shares a group with it.
      *> Rule 5: "The name is a data-name referenced in a data
      *> description entry clause whose subject is subordinate to the
      *> same group item as that data-name. In this case, the names of
      *> any group items superordinate to both the data-name and the
      *> subject of the data description entry clause are used as
      *> implicit qualifiers for the reference, in addition to any
      *> explicit qualifiers needed to establish uniqueness within that
      *> group."   cite.py: OK  §8.4.2.2.1 5)
      *> §13.18.49.4 GR1: "The effect of the SAME AS clause is as though
      *> the data description identified by data-name-1 had been coded
      *> in place of the SAME AS clause" — so U takes S's PICTURE.
      *> cite.py: OK  §13.18.49.4 1)
      *> S is declared twice (X(3) in S1, X(5) in S2).  U OF S1 names
      *> S with implicit qualifier S1, so U OF S1 is X(3); U OF S2 is
      *> X(5).  MOVE "ABCDEFG" truncates on the right to each size.
      *> S (elementary, no OCCURS) satisfies §13.18.49.3 SR1/SR5/SR7.
      *> EXPECTED:  S1-U=[ABC]   S2-U=[ABCDE]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7IQS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1.
          05 S PIC X(3).
          05 U SAME AS S.
       01 S2.
          05 S PIC X(5).
          05 U SAME AS S.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCDEFG" TO U OF S1
           MOVE "ABCDEFG" TO U OF S2
           DISPLAY "S1-U=[" U OF S1 "]"
           DISPLAY "S2-U=[" U OF S2 "]"
           STOP RUN.
