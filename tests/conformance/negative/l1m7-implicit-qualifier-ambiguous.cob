      *> reject-at: 85 2002 2014 2023
      *> ISO §8.4.2.2.1 rule 5 + §8.4.2.1 — an ODO object name that the
      *> implicit qualifiers leave NON-unique within the shared group.
      *> Rule 5: "... the names of any group items superordinate to
      *> both the data-name and the subject of the data description
      *> entry clause are used as implicit qualifiers for the
      *> reference, in addition to any explicit qualifiers needed to
      *> establish uniqueness within that group."
      *> cite.py: OK  §8.4.2.2.1 5)
      *> §8.4.2.1: "In order to use a resource, a statement shall
      *> contain a reference that uniquely identifies that resource."
      *> cite.py: OK  §8.4.2.1
      *> Both CNTs share only G with T, so the implicit qualifier G
      *> matches both; rule 5 still requires the explicit qualifier (A
      *> or B) that would establish uniqueness within G, and none is
      *> written.  No other exemption (rules 1-4, 6) applies.
      *> Expected: COBOLNET1639 (undefined-reference: "an unqualified
      *> ambiguity leaves [the name] unidentified").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7IQX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A.
             10 CNT PIC 9.
          05 B.
             10 CNT PIC 9.
          05 T PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO CNT OF A
           MOVE 2 TO CNT OF B
           STOP RUN.
