      *> kb/Work PB1263 - SR4 asks only about a group BETWEEN the table entry and its KEY. When the
      *> KEY is the table's own subject (SR6 admits that), no group lies between them, so an
      *> OCCURS group that ENCLOSES the table is irrelevant: T is its own key inside OUTER OCCURS 2.
      *> Fails if the key is refused (COBOLNET2787) or the search misses the element.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1263OUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 OUTER OCCURS 2.
               10 T PIC 9 OCCURS 3 ASCENDING KEY IS T INDEXED BY IX.
       PROCEDURE DIVISION.
           MOVE "123456" TO R
           SEARCH ALL T (2) AT END DISPLAY "NONE"
               WHEN T (2 IX) = 5 DISPLAY "FOUND 5"
           END-SEARCH
           STOP RUN.
