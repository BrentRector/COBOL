      *> kb/Work PB1951 - THE FORMAT-2 TABLE SORT OF A BASED TABLE WHOSE ELEMENT HOLDS A POINTER TABLE.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.4 "The sorted table elements of the table referenced by data-name-2 are placed in
      *>     the table referenced by data-name-2." -> OK §14.9.40.4 24)
      *>   cite.py --check 8.4.2.3.3 "the number of subscripts shall equal the number of OCCURS clauses in the
      *>     description of the table element being referenced" -> OK §8.4.2.3.3 3)
      *>   cite.py --check 13.18.60.3 "may be specified only for an elementary data item at level 1 or an elementary
      *>     data item subordinate to a type declaration that includes the STRONG phrase" -> OK §13.18.60.3 14)
      *> A table element is the whole of its description, so every occurrence of a pointer table inside it is part of
      *> what GR24 places: each inner pointer travels with its element. A pointer's value is a managed reference in
      *> the BASED area's slot, one slot per inner occurrence, not part of the element's byte image (kb/Work PB231,
      *> PB1922). Before this change an element holding a pointer TABLE was deferred (COBOLNET1756) and the statement
      *> aborted the run unit, though an element holding an elementary pointer sorted (kb/Work PB1922).
      *> USAGE POINTER is admitted below level 1 only inside a STRONG type declaration (§13.18.60.3 SR14), so each
      *> table is a strongly-typed group and the BASED record takes its type.
      *>
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   1  keys 3, 1, 2; element n holds pointers EP(n, 1) and EP(n, 2) to two distinct items. SORT E ASCENDING
      *>        KEY EK places the elements in key order, both pointers with their own element: 1 CCCC DDDD,
      *>        2 EEEE FFFF, 3 AAAA BBBB. A sort that carried only the first inner occurrence would print the old
      *>        second pointers (BBBB, DDDD, FFFF) in place; one that moved only the bytes would print A..F in order.
      *>   2  two inner levels inside a table inside ROW OCCURS 2: element E2(r, e) holds GP(r, e, g, p) for g, p in
      *>        1..2. ROW(2) holds keys 4, 3 with pointers A B C D and E F G H; ROW(1) holds keys 2, 1 with first
      *>        pointers HHHH, GGGG. SORT E2(2) ASCENDING KEY EK2 sorts ROW(2) only (§8.4.2.3.3 SR5 e)): ROW(2) reads
      *>        3 EEEE FFFF GGGG HHHH, 4 AAAA BBBB CCCC DDDD, and ROW(1) stays 2 HHHH, 1 GGGG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1951PT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-TAB IS TYPEDEF STRONG.
          05 E OCCURS 3.
             10 EK PIC 9.
             10 EP USAGE POINTER OCCURS 2.
       01 T-TAB2 IS TYPEDEF STRONG.
          05 ROW OCCURS 2.
             10 E2 OCCURS 2.
                15 EK2 PIC 9.
                15 G OCCURS 2.
                   20 GP USAGE POINTER OCCURS 2.
       01 BR TYPE T-TAB BASED.
       01 BR2 TYPE T-TAB2 BASED.
       01 W PIC X(4) BASED.
       01 WS-A PIC X(4) VALUE "AAAA".
       01 WS-B PIC X(4) VALUE "BBBB".
       01 WS-C PIC X(4) VALUE "CCCC".
       01 WS-D PIC X(4) VALUE "DDDD".
       01 WS-E PIC X(4) VALUE "EEEE".
       01 WS-F PIC X(4) VALUE "FFFF".
       01 WS-G PIC X(4) VALUE "GGGG".
       01 WS-H PIC X(4) VALUE "HHHH".
       01 I PIC 9.
       01 J PIC 9.
       01 K PIC 9.
       01 OUT-LINE PIC X(30).
       01 OUT-POS PIC 99.
       PROCEDURE DIVISION.
       MAIN.
      *> Leg 1: one inner OCCURS.
           ALLOCATE BR
           MOVE 3 TO EK OF E(1)
           MOVE 1 TO EK OF E(2)
           MOVE 2 TO EK OF E(3)
           SET EP(1, 1) TO ADDRESS OF WS-A
           SET EP(1, 2) TO ADDRESS OF WS-B
           SET EP(2, 1) TO ADDRESS OF WS-C
           SET EP(2, 2) TO ADDRESS OF WS-D
           SET EP(3, 1) TO ADDRESS OF WS-E
           SET EP(3, 2) TO ADDRESS OF WS-F
           SORT E ASCENDING KEY EK
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               MOVE SPACES TO OUT-LINE
               MOVE EK OF E(I) TO OUT-LINE(1:1)
               MOVE 3 TO OUT-POS
               PERFORM VARYING J FROM 1 BY 1 UNTIL J > 2
                   SET ADDRESS OF W TO EP(I, J)
                   MOVE W TO OUT-LINE(OUT-POS:4)
                   ADD 5 TO OUT-POS
               END-PERFORM
               DISPLAY "1 " OUT-LINE(1:11)
           END-PERFORM
      *> Leg 2: two inner OCCURS levels, one row of an enclosing table.
           ALLOCATE BR2
           MOVE 2 TO EK2 OF E2(1, 1)
           MOVE 1 TO EK2 OF E2(1, 2)
           SET GP(1, 1, 1, 1) TO ADDRESS OF WS-H
           SET GP(1, 2, 1, 1) TO ADDRESS OF WS-G
           MOVE 4 TO EK2 OF E2(2, 1)
           MOVE 3 TO EK2 OF E2(2, 2)
           SET GP(2, 1, 1, 1) TO ADDRESS OF WS-A
           SET GP(2, 1, 1, 2) TO ADDRESS OF WS-B
           SET GP(2, 1, 2, 1) TO ADDRESS OF WS-C
           SET GP(2, 1, 2, 2) TO ADDRESS OF WS-D
           SET GP(2, 2, 1, 1) TO ADDRESS OF WS-E
           SET GP(2, 2, 1, 2) TO ADDRESS OF WS-F
           SET GP(2, 2, 2, 1) TO ADDRESS OF WS-G
           SET GP(2, 2, 2, 2) TO ADDRESS OF WS-H
           SORT E2(2) ASCENDING KEY EK2
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
               MOVE SPACES TO OUT-LINE
               MOVE EK2 OF E2(2, I) TO OUT-LINE(1:1)
               MOVE 3 TO OUT-POS
               PERFORM VARYING J FROM 1 BY 1 UNTIL J > 2
                   PERFORM VARYING K FROM 1 BY 1 UNTIL K > 2
                       SET ADDRESS OF W TO GP(2, I, J, K)
                       MOVE W TO OUT-LINE(OUT-POS:4)
                       ADD 5 TO OUT-POS
                   END-PERFORM
               END-PERFORM
               DISPLAY "2 " OUT-LINE(1:21)
           END-PERFORM
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
               SET ADDRESS OF W TO GP(1, I, 1, 1)
               DISPLAY "2 ROW1 " EK2 OF E2(1, I) " " W
           END-PERFORM
           STOP RUN.
