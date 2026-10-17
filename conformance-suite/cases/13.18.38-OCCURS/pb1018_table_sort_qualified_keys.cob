      *>--- conformance-case
      *> id: pb1018_table_sort_qualified_keys
      *> rules: [SR-13.18.38.3-3, SR-14.9.40.3-14]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
      *> kb/Work PB1018 - the Format-2 table SORT (ISO 14.9.40, COBOL
      *> 2002) resolves every name it is given with its qualifiers:
      *>  - no KEY phrase: the table's own OCCURS KEY IS K OF B (GR21),
      *>  - a written key K OF B / K OF A (SR14 a, 8.4.2.2.3 SR1),
      *>  - the subject data-name-2 E OF G2 (SR13) - E is also in G1.
      *> Each used to take the FIRST same-named item. Elements are
      *> (A.K,B.K) = (7,3) (8,1) (9,2); T displays A.K B.K per element.
       PROGRAM-ID. PB1018-TABLE-SORT-QUALIFIED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 ASCENDING KEY IS K OF B.
             10 A.
                15 K PIC 9.
             10 B.
                15 K PIC 9.
       01 G1.
          05 E OCCURS 3 PIC 9.
       01 G2.
          05 E OCCURS 3 PIC 9.
       PROCEDURE DIVISION.
       P0.
           MOVE "738192" TO T.
           SORT E OF T.
           DISPLAY "OCCURS KEY K OF B ASC  " T.
           SORT E OF T DESCENDING K OF B.
           DISPLAY "K OF B DESC            " T.
           SORT E OF T ASCENDING K OF A.
           DISPLAY "K OF A ASC             " T.
           MOVE "321" TO G1. MOVE "654" TO G2.
           SORT E OF G2 ASCENDING.
           DISPLAY "G1=" G1 " G2=" G2.
           STOP RUN.
