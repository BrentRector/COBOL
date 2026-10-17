      *>--- conformance-case
      *> id: l1c27_same_as_group_hierarchy_past_49
      *> rules: [GR-13.18.49.4-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.49.4 2) — SAME AS a group: names, hierarchy, levels
      *> > 49
      *> "If data-name-1 describes a group item: a) the subject of the
      *> entry is a group whose subordinate elements have the same
      *> names, descriptions, and hierarchy as the subordinate elements
      *> of data-name-1, b) the level-numbers of items subordinate to
      *> that group are adjusted, if necessary, to preserve the
      *> hierarchy of data-name-1, c) level-numbers in the resulting
      *> hierarchy may exceed 49."
      *> cite.py --check 13.18.49.4 "the subject of the entry is a group
      *>   whose subordinate elements have the same names, descriptions,
      *>   and hierarchy as the subordinate elements of data-name-1"
      *>   -> OK §13.18.49.4 2) a)
      *> cite.py --check 13.18.49.4 "the level-numbers of items
      *>   subordinate to that group are adjusted, if necessary, to
      *>   preserve the hierarchy of data-name-1"
      *>   -> OK §13.18.49.4 2) b)
      *> cite.py --check 13.18.49.4 "level-numbers in the resulting
      *>   hierarchy may exceed 49" -> OK §13.18.49.4 2) c)
      *> Legality: §13.18.49.3 7) "Data-name-1 shall reference an
      *> elementary item or a level 1 group item" (--check OK): T is a
      *> level 1 group.
      *> WHY b) AND c) ARE FORCED: the subject S is at level 48 and T
      *> has two levels below its 01 (G, then G1/G2). Preserving G > S
      *> and G1 > G needs level-numbers of at least 49 and 50; S can be
      *> written only because c) allows that.
      *>
      *> DERIVATION (S holds a copy of T: G{G1 X, G2 X}, H 99 with 88
      *> H-TEN VALUE 10):
      *>   MOVE 55 TO H OF S, then MOVE "XY" TO G OF S: G is a
      *>     2-character group holding G1 then G2 (hierarchy kept), and
      *>     H is not inside G, so H keeps 55
      *>                                       -> "G1=X G2=Y H=55"
      *>   MOVE 7 TO H OF S; SET H-TEN OF S TO TRUE: the copied 88 sets
      *>     H to 10                           -> "H=10"
      *>   H-TEN OF S is true (H = 10)         -> "H-TEN"
      *>   MOVE "AB" TO G OF T: T's items are not S's
      *>                                       -> "T-G1=A S-G1=X"
      *>   LENGTH OF S = 2 + 2 = 4 (same descriptions: X, X and 99); O
      *>   holds P, which holds only S, so LENGTH OF O = 4
      *>                                       -> "LS=04 LO=04"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C27L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 G.
             10 G1 PIC X.
             10 G2 PIC X.
          05 H PIC 99.
             88 H-TEN VALUE 10.
       01 O.
          05 P.
             48 S SAME AS T.
       01 WS-LEN PIC 99.
       01 WS-LO  PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 55 TO H OF S.
           MOVE "XY" TO G OF S.
           DISPLAY "G1=" G1 OF S " G2=" G2 OF S " H=" H OF S.
           MOVE 7 TO H OF S.
           SET H-TEN OF S TO TRUE.
           DISPLAY "H=" H OF S.
           IF H-TEN OF S
               DISPLAY "H-TEN"
           ELSE
               DISPLAY "NOT H-TEN"
           END-IF.
           MOVE "AB" TO G OF T.
           DISPLAY "T-G1=" G1 OF T " S-G1=" G1 OF S.
           MOVE FUNCTION LENGTH(S) TO WS-LEN.
           MOVE FUNCTION LENGTH(O) TO WS-LO.
           DISPLAY "LS=" WS-LEN " LO=" WS-LO.
           STOP RUN.
