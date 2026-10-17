*>--- conformance-case
*> id: pb170-subscript-index-data-item
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.60.3 syntax rule 10: "An index data item may be referenced
      *> explicitly only in a SEARCH or SET statement, a relation condition, an
      *> intrinsic function argument" - a closed list with no subscript entry.
      *> 8.5.2.1 Table 2 puts it in class INDEX, so 8.8.1.1 excludes it too.
      *> An index DATA item's PicInfo carries category NUMERIC for the storage
      *> model, which is exactly why the old category switch could not see it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB170N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IDX USAGE INDEX.
       01 R  PIC X.
       01 T.
          05 E PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO T
           SET IDX TO 2
           MOVE E(IDX) TO R
           STOP RUN.
