      *> kb/Work PB599 - A FORMAT-2 TABLE SORT WHOSE KEY LIES BEHIND A REDEFINES VIEW OF THE ELEMENT.
      *>   cite.py --check 14.9.40.3 "The data item identified by a key data-name shall be the same as, or subordinate
      *>     to, the data item referenced by data-name-2." -> OK §14.9.40.3 14) a)
      *>   cite.py --check 14.9.40.4 "The sorted table elements of the table referenced by data-name-2 are placed in
      *>     the table referenced by data-name-2." -> OK §14.9.40.4 24)
      *>   cite.py --check 14.9.40.4 "To determine the relative order in which the table elements are stored after
      *>     sorting, the contents of corresponding key data items are compared according to the rules for comparison
      *>     of" -> OK §14.9.40.4 19)  (the relation-condition rules)
      *>   cite.py --check 13.18.44.4 "When the same storage area is defined by more than one data description entry,
      *>     the data-name associated with any of those data description entries may be used to reference that storage
      *>     area." -> OK §13.18.44.4 2)
      *> The key KN redefines KA, so it has no field of its own on the element: the sort reads it through the view's
      *> window (a numeric item over two character positions) and orders by its numeric VALUE (GR19 -> the
      *> relation-condition rules, §8.8.4.2.4), never by the bytes.
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   1  T1 = "10a02b31c", elements (10,a) (02,b) (31,c); SORT E1 ASCENDING KEY KN -> numeric order 2, 10, 31:
      *>        02b10a31c
      *>   2  the same data, DESCENDING KEY KN -> 31, 10, 2: 31c10a02b
      *>   3  a table inside ROW OCCURS 2, ROW(1) = "10a02b31c", ROW(2) = "30d20e10f"; SORT E2(2) ASCENDING KEY KN2
      *>        sorts ROW(2) only (the window is placed by the written outer subscript): ROW(1) is untouched and
      *>        ROW(2) reads 10f20e30d, so the display is 10a02b31c10f20e30d
      *>   4  two keys, the first one a view: T4 = "05a05c07b", ASCENDING KEY KN4 then DESCENDING KEY V4: KN4 = 5, 5, 7
      *>        and the tie between (05,a) and (05,c) goes to V4 descending, c before a: 05c05a07b
      *>   5  the table's OWN KEY phrase names the view: SORT E5 with `OCCURS 3 ASCENDING KEY IS KN5` on "30x10y20z"
      *>        -> 10y20z30x
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB599VK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1 OCCURS 3.
             10 KA1 PIC XX.
             10 KN1 REDEFINES KA1 PIC 99.
             10 V1 PIC X.
       01 T3.
          05 ROW OCCURS 2.
             10 E3 OCCURS 3.
                15 KA3 PIC XX.
                15 KN3 REDEFINES KA3 PIC 99.
                15 V3 PIC X.
       01 T4.
          05 E4 OCCURS 3.
             10 KA4 PIC XX.
             10 KN4 REDEFINES KA4 PIC 99.
             10 V4 PIC X.
       01 T5.
          05 E5 OCCURS 3 ASCENDING KEY IS KN5.
             10 KA5 PIC XX.
             10 KN5 REDEFINES KA5 PIC 99.
             10 V5 PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "10a02b31c" TO T1
           SORT E1 ASCENDING KEY KN1
           DISPLAY "1 " T1
           MOVE "10a02b31c" TO T1
           SORT E1 DESCENDING KEY KN1
           DISPLAY "2 " T1
           MOVE "10a02b31c" TO ROW(1)
           MOVE "30d20e10f" TO ROW(2)
           SORT E3(2) ASCENDING KEY KN3
           DISPLAY "3 " T3
           MOVE "05a05c07b" TO T4
           SORT E4 ASCENDING KEY KN4 DESCENDING KEY V4
           DISPLAY "4 " T4
           MOVE "30x10y20z" TO T5
           SORT E5
           DISPLAY "5 " T5
           STOP RUN.
