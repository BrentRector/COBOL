*>--- conformance-case
*> id: pb124-move-index-function
*> rules: [SR-14.9.25.3-1, GR-15.2-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.2 item 6: "Index functions. These are of the class and category index." MAX over index
      *> arguments is one (15.59.1's Index row), and 14.9.25.3 SR1 bars class index from MOVE - the result's
      *> STORAGE category (numeric) made it indistinguishable from a numeric sender before, so the move
      *> silently stored an occurrence-number image (kb/Work PB124 wave 5b, GR-15.2-6).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IX1 USAGE INDEX.
       01 IX2 USAGE INDEX.
       01 R PIC 9(9).
       01 RS PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION MAX(IX1 IX2) TO RS
           STOP RUN.
