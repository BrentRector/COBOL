      *> kb/Work PB1304 - ISO 13.18.58.4 GR1: "Subordinate data description entries, condition-name entries, and
      *>   RENAMES clauses are part of the type declaration of that type. The data-names of the items described in
      *>   these subordinate entries may be referenced only as subordinate items of groups defined using the
      *>   type-name. If there is more than one such group, qualification with the name of the group is
      *>   necessary."
      *>   cite.py: OK  13.18.58.4 1)  (General rules)
      *> So the level-66 entries of T are part of T: every group defined with TYPE T has its OWN AB over its OWN
      *> A..B (13.18.45.4 GR2: an alphanumeric group item over the storage from A through B) and its own AA
      *> (GR1: all the attributes and the storage of A). R1 and R2 are records of the type, so a bare AB would be
      *> ambiguous and every reference below is qualified (the level-88 clones behave the same way); GRP holds
      *> two groups of the type inside one record, where the alias belongs to each group, not to the record.
      *>   cite.py: OK  13.18.45.4 2)  (General rules)
      *> The values are those the MOVEs store: R1 AB "XY" sets R1's A and B only; R2 is untouched until its own AB
      *> is written (the clones share nothing); G1 and G2 of GRP are written through their own aliases; AA of R1
      *> forwards to A (a numeric PIC 9 here would stay numeric - GR1 - so AA is a PIC X(1) and a MOVE through it
      *> is visible in A). U and V nest one type in another: V's P is a U, so P carries U's XY (a group INSIDE
      *> a record owns an alias, XY OF P OF W1) and V's own PQ spans P through Q.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1304A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 A PIC X.
          05 B PIC X.
          66 AB RENAMES A THRU B.
          66 AA RENAMES A.
       01 R1 TYPE T.
       01 R2 TYPE T.
       01 GRP.
          05 G1 TYPE T.
          05 G2 TYPE T.
       01 U TYPEDEF.
          05 X PIC X.
          05 Y PIC X.
          66 XY RENAMES X THRU Y.
       01 V TYPEDEF.
          05 P TYPE U.
          05 Q PIC X.
          66 PQ RENAMES P THRU Q.
       01 W1 TYPE V.
       01 W2 TYPE V.
       PROCEDURE DIVISION.
       M1.
           MOVE "-" TO A OF R2
           MOVE "-" TO B OF R2
           MOVE "XY" TO AB OF R1
           DISPLAY A OF R1 B OF R1 A OF R2 B OF R2
           MOVE "PQ" TO AB OF R2
           MOVE "z" TO AA OF R1
           DISPLAY A OF R1 B OF R1 A OF R2 B OF R2
           DISPLAY "[" AB OF R1 "][" AB OF R2 "]"
           MOVE "12" TO AB OF G1
           MOVE "34" TO AB OF G2
           DISPLAY A OF G1 B OF G1 A OF G2 B OF G2 "[" AB OF G1 "]["
                   AB OF G2 "]"
           MOVE "ab" TO XY OF P OF W1
           MOVE "c" TO Q OF W1
           MOVE "dd" TO XY OF P OF W2
           DISPLAY "[" PQ OF W1 "][" PQ OF W2 "]"
           DISPLAY XY OF P OF W1 XY OF P OF W2
           STOP RUN.
