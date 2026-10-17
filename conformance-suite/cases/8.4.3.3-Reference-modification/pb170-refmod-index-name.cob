*>--- conformance-case
*> id: pb170-refmod-index-name
*> rules: [SR-8.4.3.3.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.38.3 syntax rule 7 lists "as a subscript" among the five
      *> contexts that may reference an index-name and does NOT list a
      *> reference-modification position. ResolveSubscriptName's index-name fast
      *> path returned the index field regardless of the position, so W(IX:2)
      *> compiled clean. ONE renderer, TWO positions, ONE answer - the two-arm
      *> shape again.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB170N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W  PIC X(5) VALUE "ABCDE".
       01 R  PIC X(2).
       01 T.
          05 E PIC X OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
           SET IX TO 2
           MOVE W(IX:2) TO R
           STOP RUN.
