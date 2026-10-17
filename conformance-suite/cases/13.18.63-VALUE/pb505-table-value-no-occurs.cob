*>--- conformance-case
*> id: pb505-table-value-no-occurs
*> rules: [SR-13.18.63.3-18]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR18: "A data description entry that contains the VALUE clause shall contain an
      *> OCCURS clause or be subordinate to a data description entry that contains an OCCURS clause."
      *> X carries neither, so the FROM phrase's subscript-1 identifies no table element at all.
      *> WHAT THIS FIXTURE PINS IS THE ATTRIBUTION (kb/Work PB505).  This program used to draw
      *> COBOLNET0899 - "a Format 2 (table) VALUE clause is recognized but currently supported only on a
      *> single-dimension table's own OCCURS entry ... not yet implemented" - a not-implemented NOTICE
      *> that sent the programmer to wait for a feature instead of to their own declaration, and the SAME
      *> text answered for the CONFORMING subordinate shape SR18 expressly permits.  SR18 carries no
      *> version proviso, so it is reported at all four editions; at 85 the COBOLNET0900 introduction
      *> gate for the format-2 VALUE is reported ALONGSIDE it, not instead of it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB505N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(2) VALUE "AB" FROM (1).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY X
           STOP RUN.
