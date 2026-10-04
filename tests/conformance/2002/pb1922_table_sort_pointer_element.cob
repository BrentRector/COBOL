      *> kb/Work PB1922 - THE FORMAT-2 TABLE SORT OF A BASED TABLE WHOSE ELEMENT HOLDS A POINTER.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.4 "The sorted table elements of the table referenced by data-name-2 are placed in
      *>     the table referenced by data-name-2." -> OK §14.9.40.4 24)
      *>   cite.py --check 14.9.40.3 "Data-name-2 shall have an OCCURS clause in its data description entry." -> OK
      *>     §14.9.40.3 13)
      *>   cite.py --check 13.18.60.3 "A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER,
      *>     or PROGRAM-POINTER phrase may be specified only for an elementary data item at level 1 or an elementary
      *>     data item subordinate to a type declaration that includes the STRONG phrase" -> OK §13.18.60.3 14)
      *> A table element is the whole of its description, pointer and all: the sorted elements are what GR24 places, so a
      *> pointer travels with the key beside it. The pointer's value is a managed reference in the BASED area's slot,
      *> not part of the byte image the element's other items occupy (kb/Work PB231); before this change the statement
      *> was deferred (COBOLNET1756) and aborted the run unit, though every other shared-storage table sorted.
      *> The pointer is spelled legally: USAGE POINTER is admitted below level 1 only inside a STRONG type declaration
      *> (§13.18.60.3 SR14), so the table is a strongly-typed group and the BASED record takes its type.
      *>
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   1  keys 3, 1, 2 hold pointers to "AAAA", "BBBB", "CCCC". SORT E ASCENDING KEY EK puts the elements in key
      *>        order, each with its own pointer: 1 BBBB, 2 CCCC, 3 AAAA (a sort that moved only the bytes would leave
      *>        the pointers at their old occurrences and print 1 AAAA, 2 BBBB, 3 CCCC).
      *>   2  DESCENDING over the same table: 3 AAAA, 2 CCCC, 1 BBBB.
      *>   3  a table inside ROW OCCURS 2 sorts one row: ROW(1) holds keys 2, 1 with pointers "AAAA", "BBBB", ROW(2) holds
      *>        keys 4, 3 with pointers "CCCC", "DDDD". SORT E2(2) ASCENDING KEY EK2 sorts ROW(2) only (§8.4.2.3.3 SR5 e):
      *>        ROW(1) stays 2 AAAA, 1 BBBB and ROW(2) reads 3 DDDD, 4 CCCC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1922PT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-TAB IS TYPEDEF STRONG.
          05 E OCCURS 3.
             10 EK PIC 9.
             10 EP USAGE POINTER.
       01 T-TAB2 IS TYPEDEF STRONG.
          05 ROW OCCURS 2.
             10 E2 OCCURS 2.
                15 EK2 PIC 9.
                15 EP2 USAGE POINTER.
       01 BR TYPE T-TAB BASED.
       01 BR2 TYPE T-TAB2 BASED.
       01 W PIC X(4) BASED.
       01 WS-A PIC X(4) VALUE "AAAA".
       01 WS-B PIC X(4) VALUE "BBBB".
       01 WS-C PIC X(4) VALUE "CCCC".
       01 WS-D PIC X(4) VALUE "DDDD".
       01 PT USAGE POINTER.
       01 I PIC 9.
       01 J PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE BR
           MOVE 3 TO EK OF E(1)
           MOVE 1 TO EK OF E(2)
           MOVE 2 TO EK OF E(3)
           SET ADDRESS OF W TO ADDRESS OF WS-A
           SET PT TO ADDRESS OF W
           SET EP OF E(1) TO PT
           SET ADDRESS OF W TO ADDRESS OF WS-B
           SET PT TO ADDRESS OF W
           SET EP OF E(2) TO PT
           SET ADDRESS OF W TO ADDRESS OF WS-C
           SET PT TO ADDRESS OF W
           SET EP OF E(3) TO PT
           SORT E ASCENDING KEY EK
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
              SET ADDRESS OF W TO EP OF E(I)
              DISPLAY "1 " EK OF E(I) " " W
           END-PERFORM
           SORT E DESCENDING KEY EK
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
              SET ADDRESS OF W TO EP OF E(I)
              DISPLAY "2 " EK OF E(I) " " W
           END-PERFORM
           ALLOCATE BR2
           MOVE 2 TO EK2(1, 1)
           MOVE 1 TO EK2(1, 2)
           MOVE 4 TO EK2(2, 1)
           MOVE 3 TO EK2(2, 2)
           SET ADDRESS OF W TO ADDRESS OF WS-A
           SET PT TO ADDRESS OF W
           SET EP2(1, 1) TO PT
           SET ADDRESS OF W TO ADDRESS OF WS-B
           SET PT TO ADDRESS OF W
           SET EP2(1, 2) TO PT
           SET ADDRESS OF W TO ADDRESS OF WS-C
           SET PT TO ADDRESS OF W
           SET EP2(2, 1) TO PT
           SET ADDRESS OF W TO ADDRESS OF WS-D
           SET PT TO ADDRESS OF W
           SET EP2(2, 2) TO PT
           SORT E2(2) ASCENDING KEY EK2
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
              PERFORM VARYING J FROM 1 BY 1 UNTIL J > 2
                 SET ADDRESS OF W TO EP2(I, J)
                 DISPLAY "3 " EK2(I, J) " " W
              END-PERFORM
           END-PERFORM
           STOP RUN.
