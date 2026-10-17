*>--- conformance-case
*> id: pb505-table-value-subscript-count
*> rules: [SR-13.18.63.3-20]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR20: "In one FROM phrase, there shall be one subscript-1 specified for each
      *> OCCURS clause for the subject of the entry or superordinate to that entry, specified in the same
      *> order as a subscripted reference to the subject of the entry would be specified."
      *> C is two-dimensional - R's OCCURS and its own - so a one-subscript FROM phrase has no
      *> correspondence to check anything else against.  The rule had NO SITE (kb/Work PB505): a wrong
      *> count reached the landable-scope stage, COBOLNET0899, and was reported as a compiler limitation.
      *> SR20 carries no version proviso; at 85 COBOLNET0900 is reported alongside.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB505N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
           05 R OCCURS 2.
               10 C PIC X OCCURS 3 VALUE "A" FROM (1).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY C(1 1)
           STOP RUN.
