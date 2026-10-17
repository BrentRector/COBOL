      *>--- conformance-case
      *> id: l1c34_type_group_hierarchy
      *> rules: [GR-13.18.57.4-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.57.4 GR2 — a group TYPE subject below level 1
      *>   keeps the
      *> type's names, descriptions and hierarchy, levels adjusted
      *>   (past 49)
      *> "If type-name-1 describes a group item: a) the subject of the
      *>   entry is
      *> a group whose subordinate elements have the same names,
      *>   descriptions,
      *> and hierarchy as the subordinate elements of type-name-1, b)
      *>   the
      *> level-numbers of items subordinate to that group are
      *>   adjusted, if
      *> necessary, to preserve the hierarchy of type-name-1, c)
      *>   level-numbers
      *> in the resulting hierarchy may exceed 49, d) the subject of
      *>   the entry
      *> is aligned as though it were a level 1 item."
      *> cite.py --check 13.18.57.4 "the subject of the entry is a
      *>   group whose
      *>   subordinate elements have the same names, descriptions, and
      *>     hierarchy
      *>   as the subordinate elements of type-name-1" -> OK
      *>     §13.18.57.4 2) a)
      *> cite.py --check 13.18.57.4 "the level-numbers of items
      *>   subordinate to
      *>   that group are adjusted, if necessary, to preserve the
      *>     hierarchy of
      *>   type-name-1" -> OK §13.18.57.4 2) b)
      *> cite.py --check 13.18.57.4 "level-numbers in the resulting
      *>   hierarchy
      *>   may exceed 49" -> OK §13.18.57.4 2) c)
      *> cite.py --check 13.18.57.4 "the subject of the entry is
      *>   aligned as
      *>   though it were a level 1 item" -> OK §13.18.57.4 2) d)
      *> T's members are written at 05/10. X is a level-49 subject, so
      *>   its
      *> members must sit at levels > 49 (b, c); P is a level-05
      *>   subject with a
      *> level-05 sibling Q, so its 05/10 members must be pushed below
      *>   05 or
      *> they would become siblings of P and Q (b). d) is observable
      *>   through bit alignment (kb/Work PB1569): §8.5.1.6.3 —
      *>   "Alignment of elementary bit data items of level 1 or
      *>   level 77 and a level 1 bit group are at the first bit of
      *>   a byte" (cite.py --check 8.5.1.6.3 -> OK), where a bit
      *>   group after a same-level bit item would otherwise go at
      *>   the next bit. So a level-05 subject of the bit-group type
      *>   BT after the level-05 bit item F starts a fresh byte (d).
      *> DERIVATION (T = A X(2), B{ C X(3), D 9(2) OCCURS 2 } = 9
      *>   characters):
      *>  X after A="AA", C="CCC", D(1)=12, D(2)=34     -> X=[AACCC1234]
      *>  LENGTH OF X = 2 + 3 + 2*2 = 9 (a)              -> LX=09
      *>  D OF B OF X (2) is addressable by the type's own names (a)
      *>    -> D2=34
      *>  B OF X := "XXXXXXX" (7): B holds C and D only, A untouched
      *>                                                 ->
      *>                                                   X=[AAXXXXXXX]
      *>  REC2 = P(9) + Q(2); P := ALL "P" leaves Q      ->
      *>    R2=[PPPPPPPPPQQ]
      *>  LENGTH OF REC2 = 9 + 2: P's members did not become level-05
      *>  siblings of Q                                  -> LR2=11
      *>  REC3 = F(1) | filler(7) | S{B1 B2}(2) | filler(6) = 2 bytes,
      *>  not the 1 byte F and S would share if S were placed in
      *>  line (d)                                       -> BLR3=02
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF.
           05  A           PIC X(2).
           05  B.
               10  C       PIC X(3).
               10  D       PIC 9(2) OCCURS 2.
       01  REC.
           49  X TYPE T.
       01  REC2.
           05  P TYPE T.
           05  Q           PIC X(2) VALUE "QQ".
       01  BT TYPEDEF GROUP-USAGE BIT.
           05  B1          PIC 1 USAGE BIT.
           05  B2          PIC 1 USAGE BIT.
       01  REC3.
           05  F           PIC 1 USAGE BIT.
           05  S TYPE BT.
       01  WS-N            PIC 99.
       PROCEDURE DIVISION.
           MOVE "AA" TO A OF X.
           MOVE "CCC" TO C OF X.
           MOVE 12 TO D OF X (1).
           MOVE 34 TO D OF X (2).
           DISPLAY "X=[" X "]".
           MOVE FUNCTION LENGTH (X) TO WS-N.
           DISPLAY "LX=" WS-N.
           DISPLAY "D2=" D OF B OF X (2).
           MOVE "XXXXXXX" TO B OF X.
           DISPLAY "X=[" X "]".
           MOVE ALL "P" TO P.
           DISPLAY "R2=[" REC2 "]".
           MOVE FUNCTION LENGTH (REC2) TO WS-N.
           DISPLAY "LR2=" WS-N.
           MOVE FUNCTION BYTE-LENGTH (REC3) TO WS-N.
           DISPLAY "BLR3=" WS-N.
           STOP RUN.
