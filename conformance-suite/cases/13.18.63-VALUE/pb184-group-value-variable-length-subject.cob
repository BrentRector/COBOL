*>--- conformance-case
*> id: pb184-group-value-variable-length-subject
*> rules: [SR-13.18.63.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR1, SECOND SHAPE: "The subject of the entry shall not be a strongly-typed group item
      *> or a variable-length group."  8.5.1.12.1 defines the term: "A variable-length group is a group item
      *> whose data description has at least one dynamic-length elementary item or dynamic-capacity table as a
      *> subordinate item."  GV has a dynamic-capacity table subordinate to it and specifies a group-level
      *> VALUE clause.
      *> ⛔ NOT an occurs-depending group.  8.5.1.12.1 names dynamic-LENGTH elementary items and
      *> dynamic-CAPACITY tables only; an OCCURS ... DEPENDING ON group is a fixed-length group whose size is
      *> its maximum, so `01 GV VALUE "ABCDE". 05 GB PIC 9(4) COMP OCCURS 1 TO 5 DEPENDING ON NN.` is still an
      *> alphanumeric group item and still draws the SR14 usage verdict, not this one.  The distinction was
      *> measured on both shapes before the predicate was written.
      *> Twin: pb184-group-value-strong-subject covers SR1's other shape.
      *> Edition band: OCCURS ... DYNAMIC CAPACITY is COBOL-2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB184N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GV VALUE "ABCDE".
          05 GB PIC X OCCURS DYNAMIC CAPACITY IN GCAP FROM 1 TO 5.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY GCAP
           STOP RUN.
