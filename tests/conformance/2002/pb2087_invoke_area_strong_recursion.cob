      *> kb/Work PB2087 (the INVOKE arm, its last three shapes) - ISO
      *> 1989:2023 14.2.3 GR8: "If the argument is passed by reference,
      *> the activated runtime element operates as if the formal
      *> parameter occupies the same storage area as the argument."
      *> cite.py --check 14.2.3 "If the argument is passed by reference,
      *>   the activated runtime element operates as if the formal
      *>   parameter occupies the same storage area as the argument"
      *>   -> OK 14.2.3 8)
      *> cite.py --check 14.8.2.2 "If either the formal parameter or the
      *>   corresponding argument is a strongly-typed group item, both
      *>   shall be of the same type" -> OK 14.8.2.2 2)
      *> cite.py --check 14.6.4 "the identifiers within a statement are
      *>   evaluated in left to right order as the first operation of
      *>   the execution of that statement" -> OK 14.6.4 7)
      *> (1) MS: a strongly-typed group with an object-reference leaf, a
      *>     COMP leaf and a table, passed twice (G G): the two formals
      *>     ARE G, so every store through one is seen through the other
      *>     and by the caller - XYZ, 15, 17, and TA set to NULL.
      *> (2) MK: the same G BY CONTENT (14.2.3 GR9: its own record) -
      *>     the method sees XYZ, 7, 15 and the object; its stores do not
      *>     come back: G keeps XYZ and its object.
      *> (3) RECUR: methods are recursive; a LOCAL-STORAGE item passed BY
      *>     REFERENCE to the method itself is, in the inner activation,
      *>     the OUTER activation's item (8.6.4 gives each activation its
      *>     own LOCAL-STORAGE, and GR8 the formal its argument's storage
      *>     as identified when the INVOKE began, 14.6.4 7)):
      *>     N=3 WG="9w" -> RD-IN 3 9w / RD-IN 2 3. / RD-IN 1 2. /
      *>     RD-OUT 6 2X / RD-BACK 2 6 2X / RD-OUT 7 3X / RD-BACK 3 7 3X /
      *>     RD-OUT 8 9X / RD N 8 9X.
      *> (4) CALLR: the CALL twin - a LOCAL-STORAGE item passed to a program
      *>     that INVOKEs the method again: the inner activation's formal
      *>     is still the outer activation's item -> CR-IN 2 / T-IN 1 /
      *>     CR-IN 1 / CR-OUT 4 / T-OUT 4 / CR-BACK 4 / CR-OUT 5 / CR N 5.
      *> Before the fix, (1) and (2) did not compile (a cell-backed group
      *> had no leaf vector) and in (3) and (4) the inner activation read
      *> its OWN LOCAL-STORAGE through the outer argument (RD-IN 0 ...).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087I03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P2087K03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE.
          05 TC PIC S9(4) COMP.
          05 TX PIC X(3).
          05 TN PIC 9 OCCURS 2.
       01 G TYPE T.
       01 N PIC 9 VALUE 3.
       01 WG.
          05 WG1 PIC 9 VALUE 9.
          05 WG2 PIC X VALUE "w".
       01 N3 PIC 9 VALUE 2.
       01 O USAGE OBJECT REFERENCE P2087K03.
       PROCEDURE DIVISION.
           INVOKE P2087K03 "NEW" RETURNING O
           MOVE 10 TO TC OF G
           MOVE "ABC" TO TX OF G
           MOVE 1 TO TN OF G (1) TN OF G (2)
           SET TA OF G TO O
           INVOKE O "MS" USING G G
           DISPLAY "MS G " TX OF G " " TN OF G (1) TN OF G (2)
           IF TC OF G = 15 DISPLAY "G TC 15" ELSE DISPLAY "G TC ?"
           END-IF
           IF TA OF G = NULL DISPLAY "G TA NULL" ELSE DISPLAY "G TA SET"
           END-IF
           SET TA OF G TO O
           INVOKE O "MK" USING BY CONTENT G
           DISPLAY "MK G " TX OF G
           IF TA OF G = O DISPLAY "G TA O" ELSE DISPLAY "G TA ?"
           END-IF
           INVOKE O "RECUR" USING N WG
           DISPLAY "RD N " N " " WG
           INVOKE O "CALLR" USING N3
           DISPLAY "CR N " N3
           STOP RUN.
       END PROGRAM P2087I03.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087T03.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LT-N PIC 9.
       01 LT-O USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION USING LT-N LT-O.
           DISPLAY "T-IN " LT-N
           INVOKE LT-O "CALLR" USING LT-N
           DISPLAY "T-OUT " LT-N
           GOBACK.
       END PROGRAM P2087T03.

       IDENTIFICATION DIVISION.
       CLASS-ID. P2087K03 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE.
          05 TC PIC S9(4) COMP.
          05 TX PIC X(3).
          05 TN PIC 9 OCCURS 2.
       01 ME USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       METHOD-ID. MS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS-1 TYPE T.
       01 LS-2 TYPE T.
       PROCEDURE DIVISION USING LS-1 LS-2.
           MOVE "XYZ" TO TX OF LS-1
           DISPLAY "MS LS-2 TX " TX OF LS-2
           ADD 5 TO TC OF LS-2
           IF TC OF LS-1 = 15 DISPLAY "MS LS-1 TC 15"
           ELSE DISPLAY "MS LS-1 TC ?" END-IF
           MOVE 7 TO TN OF LS-1 (2)
           DISPLAY "MS LS-2 TN " TN OF LS-2 (1) TN OF LS-2 (2)
           IF TA OF LS-2 = NULL DISPLAY "MS TA NULL"
           ELSE DISPLAY "MS TA SET" END-IF
           SET TA OF LS-1 TO NULL
           IF TA OF LS-2 = NULL DISPLAY "MS TA2 NULL"
           ELSE DISPLAY "MS TA2 SET" END-IF
           GOBACK.
       END METHOD MS.
       METHOD-ID. MK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK TYPE T.
       PROCEDURE DIVISION USING LK.
           DISPLAY "MK " TX OF LK " " TN OF LK (2)
           IF TC OF LK = 15 DISPLAY "MK TC 15" ELSE DISPLAY "MK TC ?"
           END-IF
           IF TA OF LK = NULL DISPLAY "MK TA NULL"
           ELSE DISPLAY "MK TA SET" END-IF
           MOVE "QQQ" TO TX OF LK
           SET TA OF LK TO NULL
           GOBACK.
       END METHOD MK.
       METHOD-ID. RECUR.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LS-NEXT PIC 9 VALUE 0.
       01 LS-G.
          05 LS-G1 PIC 9 VALUE 0.
          05 LS-G2 PIC X VALUE ".".
       LINKAGE SECTION.
       01 LK-N PIC 9.
       01 LK-G.
          05 LK-G1 PIC 9.
          05 LK-G2 PIC X.
       PROCEDURE DIVISION USING LK-N LK-G.
           DISPLAY "RD-IN " LK-N " " LK-G
           IF LK-N > 1
               SUBTRACT 1 FROM LK-N GIVING LS-NEXT
               MOVE LK-N TO LS-G1
               INVOKE SELF "RECUR" USING LS-NEXT LS-G
               DISPLAY "RD-BACK " LK-N " " LS-NEXT " " LS-G
           END-IF
           ADD 5 TO LK-N
           MOVE "X" TO LK-G2
           DISPLAY "RD-OUT " LK-N " " LK-G
           GOBACK.
       END METHOD RECUR.
       METHOD-ID. CALLR.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LS-NEXT PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 LK-N PIC 9.
       PROCEDURE DIVISION USING LK-N.
           DISPLAY "CR-IN " LK-N
           IF LK-N > 1
               SUBTRACT 1 FROM LK-N GIVING LS-NEXT
               SET ME TO SELF
               CALL "P2087T03" USING LS-NEXT BY CONTENT ME
               DISPLAY "CR-BACK " LS-NEXT
           END-IF
           ADD 3 TO LK-N
           DISPLAY "CR-OUT " LK-N
           GOBACK.
       END METHOD CALLR.
       END OBJECT.
       END CLASS P2087K03.
