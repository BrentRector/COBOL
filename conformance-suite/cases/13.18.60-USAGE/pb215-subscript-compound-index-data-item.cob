*>--- conformance-case
*> id: pb215-subscript-compound-index-data-item
*> rules: [SR-13.18.60.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB215. ISO 13.18.60.3 SR10's closed list ("a SEARCH or SET
      *> statement, a relation condition, an intrinsic function argument" ...)
      *> has no subscript entry; 13.18.38.3 r7 admits only an index-NAME "as a
      *> subscript". A SIMPLE subscript E(IDX) was already refused; a COMPOUND
      *> segment the renderer hands to the materializer (a depth-0 slash) was
      *> bound under the context that admitted an index DATA item, and computed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB215N2.
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
           MOVE E(IDX / 1) TO R
           DISPLAY "R=" R
           STOP RUN.
