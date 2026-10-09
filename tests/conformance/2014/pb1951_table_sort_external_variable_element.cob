      *> kb/Work PB1951 (sibling) - THE FORMAT-2 TABLE SORT OF AN EXTERNAL TABLE WHOSE ELEMENT HOLDS
      *> VARIABLE-LENGTH COMPONENTS: two dynamic-length items and a dynamic-capacity table.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.4 "The sorted table elements of the table referenced by data-name-2 are placed in
      *>     the table referenced by data-name-2." -> OK §14.9.40.4 24)
      *>   cite.py --check 8.5.1.11.2 "behaves in all respects as though it were in fact contiguous with its
      *>     neighbors" -> OK §8.5.1.11.2
      *> A table element is the whole of its description, and GR24 places the sorted ELEMENTS: each element's
      *> dynamic-length items keep their own content and its dynamic-capacity table keeps its occurrences. An
      *> EXTERNAL record's variable-length components live apart from its fixed run (the record's storage cell),
      *> so the sort must move each element's components with it. Before this change the element was moved as one
      *> contiguous image and cut back by length: the first dynamic-length item took every character of the
      *> second, and the dynamic-capacity table's occurrences were dropped.
      *>
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   keys 3, 1, 2; ED1/ED2 hold "THREE"/"3", "ONE"/"1", "TWO"/"2"; EP holds "Z" "Y" in the key-3 element and
      *>   "Q" in the key-1 element, none in the key-2 element. SORT E ASCENDING KEY EK places the elements in key
      *>   order, each with its components:
      *>     1 [ONE] [1], 2 [TWO] [2], 3 [THREE] [3]  - a sort cutting the image back printed [ONE1] [] and so on.
      *>     EP 1 Q, EP 3 ZY                          - the table's occurrences travel with their element (an
      *>                                                image move left them empty).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1951EV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BR EXTERNAL.
          05 E OCCURS 3.
             10 EK PIC 9.
             10 ED1 PIC X DYNAMIC LENGTH.
             10 ED2 PIC X DYNAMIC LENGTH.
             10 EP PIC X OCCURS DYNAMIC CAPACITY IN ECAP.
       01 I PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 3 TO EK OF E(1)
           MOVE 1 TO EK OF E(2)
           MOVE 2 TO EK OF E(3)
           MOVE "THREE" TO ED1(1)
           MOVE "3" TO ED2(1)
           MOVE "Z" TO EP(1, 1)
           MOVE "Y" TO EP(1, 2)
           MOVE "ONE" TO ED1(2)
           MOVE "1" TO ED2(2)
           MOVE "Q" TO EP(2, 1)
           MOVE "TWO" TO ED1(3)
           MOVE "2" TO ED2(3)
           SORT E ASCENDING KEY EK
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               DISPLAY EK OF E(I) " [" ED1(I) "] [" ED2(I) "]"
           END-PERFORM
           DISPLAY "EP 1 " EP(1, 1)
           DISPLAY "EP 3 " EP(3, 1) EP(3, 2)
           STOP RUN.
